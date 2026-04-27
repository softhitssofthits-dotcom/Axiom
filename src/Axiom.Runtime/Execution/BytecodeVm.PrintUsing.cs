using System.Globalization;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private string FormatUsing(
        string format,
        IReadOnlyList<(RuntimeValue Value, TextLocation Location)> values,
        TextLocation statementLocation)
    {
        if (format.Length > 2048)
        {
            ReportPrintUsingError("РґР»РёРЅР° С„РѕСЂРјР°С‚РЅРѕР№ СЃС‚СЂРѕРєРё РЅРµ РґРѕР»Р¶РЅР° РїСЂРµРІС‹С€Р°С‚СЊ 2048 СЃРёРјРІРѕР»РѕРІ.", statementLocation);
            throw new RuntimeFailureException();
        }

        var tokens = ParsePrintUsingFormat(format);
        var builder = new StringBuilder();
        var argumentIndex = 0;

        foreach (var token in tokens)
        {
            switch (token)
            {
                case PrintUsingLiteralToken literalToken:
                    builder.Append(literalToken.Text);
                    break;

                case PrintUsingStringFieldToken stringFieldToken:
                {
                    var valueText = GetNextStringFormatValue(stringFieldToken, values, ref argumentIndex, statementLocation);
                    builder.Append(FormatStringField(stringFieldToken, valueText));
                    break;
                }

                case PrintUsingNumericFieldToken numericFieldToken:
                {
                    var value = GetNextNumericFormatValue(values, ref argumentIndex, statementLocation);
                    builder.Append(FormatNumericField(numericFieldToken, value));
                    break;
                }
            }
        }

        return builder.ToString();
    }

    private static List<PrintUsingFormatToken> ParsePrintUsingFormat(string format)
    {
        var tokens = new List<PrintUsingFormatToken>();
        var literal = new StringBuilder();
        var index = 0;

        while (index < format.Length)
        {
            var current = format[index];

            if (current == '_')
            {
                if (index + 1 < format.Length)
                {
                    FlushPrintUsingLiteral(tokens, literal);
                    tokens.Add(new PrintUsingLiteralToken(format[index + 1].ToString()));
                    index += 2;
                }
                else
                {
                    literal.Append(current);
                    index++;
                }

                continue;
            }

            if (current == '!')
            {
                FlushPrintUsingLiteral(tokens, literal);
                tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Bang));
                index++;
                continue;
            }

            if (current == '&')
            {
                FlushPrintUsingLiteral(tokens, literal);
                tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Ampersand));
                index++;
                continue;
            }

            if (current == '\\')
            {
                var start = index++;
                var spaces = 0;

                while (index < format.Length && format[index] == ' ')
                {
                    spaces++;
                    index++;
                }

                if (index < format.Length && format[index] == '\\')
                {
                    index++;
                    FlushPrintUsingLiteral(tokens, literal);
                    tokens.Add(new PrintUsingStringFieldToken(PrintUsingStringFieldKind.Backslash, spaces + 2));
                    continue;
                }

                literal.Append(format[start]);
                index = start + 1;
                continue;
            }

            if (IsNumericMaskCharacter(current))
            {
                FlushPrintUsingLiteral(tokens, literal);
                var start = index;

                while (index < format.Length && IsNumericMaskCharacter(format[index]))
                {
                    index++;
                }

                var mask = format[start..index];
                var info = ParseNumericMask(mask);

                if (info.IntegerDigits + info.FractionDigits == 0)
                {
                    tokens.Add(new PrintUsingLiteralToken(mask));
                }
                else
                {
                    tokens.Add(new PrintUsingNumericFieldToken(mask, info));
                }

                continue;
            }

            literal.Append(current);
            index++;
        }

        FlushPrintUsingLiteral(tokens, literal);
        return tokens;
    }

    private string GetNextStringFormatValue(
        PrintUsingStringFieldToken field,
        IReadOnlyList<(RuntimeValue Value, TextLocation Location)> values,
        ref int index,
        TextLocation statementLocation)
    {
        if (index >= values.Count)
        {
            return field.Kind == PrintUsingStringFieldKind.Backslash
                ? new string(' ', field.Width)
                : string.Empty;
        }

        var (value, location) = values[index++];

        if (value.Type != TypeSymbol.String)
        {
            ReportPrintUsingError(
                $"СЃС‚СЂРѕРєРѕРІРѕРµ РїРѕР»Рµ С‚СЂРµР±СѓРµС‚ Р·РЅР°С‡РµРЅРёРµ С‚РёРїР° STRING, РЅРѕ РїРѕР»СѓС‡РµРЅ {value.Type.Name}.",
                location);
            throw new RuntimeFailureException();
        }

        return value.AsString();
    }

    private (RuntimeValue Value, TextLocation Location) GetNextNumericFormatValue(
        IReadOnlyList<(RuntimeValue Value, TextLocation Location)> values,
        ref int index,
        TextLocation statementLocation)
    {
        if (index >= values.Count)
        {
            ReportPrintUsingError("РґР»СЏ С‡РёСЃР»РѕРІРѕРіРѕ РїРѕР»СЏ РЅРµ С…РІР°С‚РёР»Рѕ Р·РЅР°С‡РµРЅРёСЏ.", statementLocation);
            throw new RuntimeFailureException();
        }

        var entry = values[index++];

        if (!IsNumericType(entry.Value.Type))
        {
            ReportPrintUsingError(
                $"С‡РёСЃР»РѕРІРѕРµ РїРѕР»Рµ С‚СЂРµР±СѓРµС‚ С‡РёСЃР»РѕРІРѕРµ Р·РЅР°С‡РµРЅРёРµ, РЅРѕ РїРѕР»СѓС‡РµРЅ {entry.Value.Type.Name}.",
                entry.Location);
            throw new RuntimeFailureException();
        }

        return entry;
    }

    private static string FormatStringField(PrintUsingStringFieldToken field, string value)
    {
        return field.Kind switch
        {
            PrintUsingStringFieldKind.Bang => value.Length == 0 ? string.Empty : value[..1],
            PrintUsingStringFieldKind.Ampersand => value,
            PrintUsingStringFieldKind.Backslash when value.Length == field.Width => value,
            PrintUsingStringFieldKind.Backslash when value.Length < field.Width => value.PadRight(field.Width, ' '),
            PrintUsingStringFieldKind.Backslash => value[..field.Width],
            _ => value
        };
    }

    private string FormatNumericField(PrintUsingNumericFieldToken field, (RuntimeValue Value, TextLocation Location) entry)
    {
        var info = field.Info;

        if (!info.HasDollar && !info.HasStarFill && !info.HasThousands && !info.HasExponent)
        {
            return FormatNumericBasic(field.Mask, info, entry);
        }

        return FormatNumericExtended(field.Mask, info, entry);
    }

    private string FormatNumericBasic(
        string mask,
        PrintUsingNumericFormatInfo info,
        (RuntimeValue Value, TextLocation Location) entry)
    {
        var negative = IsNegative(entry.Value);
        var fullInvariant = entry.Value.ToDisplayString();
        var formattedNumber = FormatAbsoluteNumericValue(entry.Value, info.FractionDigits);
        SplitNumericParts(formattedNumber, out var integerPart, out var fractionPart);

        if (integerPart.Length > info.IntegerDigits)
        {
            return "%" + fullInvariant;
        }

        var builder = new StringBuilder();

        if (info.ForceSign)
        {
            builder.Append(negative ? '-' : '+');
        }
        else if (info.NegativeSignOnly && negative)
        {
            builder.Append('-');
        }

        builder.Append(integerPart.PadLeft(info.IntegerDigits, ' '));

        if (info.FractionDigits > 0)
        {
            builder.Append('.');
            builder.Append(fractionPart.PadRight(info.FractionDigits, '0'));
        }

        if (builder.Length < info.TotalWidth)
        {
            builder.Insert(0, new string(' ', info.TotalWidth - builder.Length));
        }

        return builder.Length > info.TotalWidth
            ? "%" + fullInvariant
            : builder.ToString();
    }

    private string FormatNumericExtended(
        string mask,
        PrintUsingNumericFormatInfo info,
        (RuntimeValue Value, TextLocation Location) entry)
    {
        var negative = IsNegative(entry.Value);
        var fullInvariant = entry.Value.ToDisplayString();

        if (info.HasExponent)
        {
            var exponentText = FormatExponentValue(entry.Value, info.FractionDigits);
            var core = info.ForceSign
                ? (negative ? "-" : "+") + exponentText
                : negative
                    ? "-" + exponentText
                    : " " + exponentText;

            return core.Length > info.TotalWidth
                ? "%" + fullInvariant
                : core.PadLeft(info.TotalWidth, ' ');
        }

        var formattedNumber = FormatAbsoluteNumericValue(entry.Value, info.FractionDigits);
        SplitNumericParts(formattedNumber, out var integerPart, out var fractionPart);

        if (info.HasThousands)
        {
            integerPart = InsertThousandsSeparators(integerPart);
        }

        var signText = info.ForceSign
            ? negative ? "-" : "+"
            : negative
                ? "-"
                : string.Empty;

        var fractionText = info.FractionDigits > 0
            ? "." + fractionPart.PadRight(info.FractionDigits, '0')
            : string.Empty;

        if (info.HasDollar && info.HasStarFill)
        {
            var full = signText + "$" + integerPart + fractionText;
            return full.Length > info.TotalWidth
                ? "%" + fullInvariant
                : new string('*', info.TotalWidth - full.Length) + full;
        }

        if (info.HasStarFill)
        {
            var full = signText + integerPart + fractionText;
            return full.Length > info.TotalWidth
                ? "%" + fullInvariant
                : new string('*', info.TotalWidth - full.Length) + full;
        }

        if (info.HasDollar)
        {
            var full = signText + "$" + integerPart + fractionText;
            return full.Length > info.TotalWidth
                ? "%" + fullInvariant
                : full.PadLeft(info.TotalWidth, ' ');
        }

        var result = signText + integerPart + fractionText;
        return result.Length > info.TotalWidth
            ? "%" + fullInvariant
            : result.PadLeft(info.TotalWidth, ' ');
    }

    private static PrintUsingNumericFormatInfo ParseNumericMask(string mask)
    {
        var info = new PrintUsingNumericFormatInfo
        {
            TotalWidth = mask.Length,
            ForceSign = mask.Contains('+'),
            NegativeSignOnly = mask.Contains('-'),
            HasThousands = mask.Contains(','),
            HasDollar = mask.Contains('$'),
            HasStarFill = mask.Contains('*'),
            HasExponent = mask.Contains('^')
        };

        var core = mask
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal);

        var dotIndex = core.IndexOf('.');

        if (dotIndex >= 0)
        {
            info.IntegerDigits = core[..dotIndex].Count(static character => character is '#' or '0');
            info.FractionDigits = core[(dotIndex + 1)..].Count(static character => character is '#' or '0');
        }
        else
        {
            info.IntegerDigits = core.Count(static character => character is '#' or '0');
        }

        return info;
    }

    private static string FormatAbsoluteNumericValue(RuntimeValue value, int fractionDigits)
    {
        if (TryGetDecimal(value, out var decimalValue))
        {
            var absoluteValue = Math.Abs(decimalValue);
            var rounded = Math.Round(absoluteValue, fractionDigits, MidpointRounding.AwayFromZero);
            return rounded.ToString(fractionDigits > 0 ? $"F{fractionDigits}" : "F0", CultureInfo.InvariantCulture);
        }

        var absoluteDouble = Math.Abs(value.AsDouble());
        var roundedDouble = Math.Round(absoluteDouble, fractionDigits, MidpointRounding.AwayFromZero);
        return roundedDouble.ToString(fractionDigits > 0 ? $"F{fractionDigits}" : "F0", CultureInfo.InvariantCulture);
    }

    private static string FormatExponentValue(RuntimeValue value, int fractionDigits)
    {
        var precision = fractionDigits > 0 ? fractionDigits : 2;
        var format = $"E{precision}";
        var absoluteValue = TryGetDecimal(value, out var decimalValue)
            ? (double)Math.Abs(decimalValue)
            : Math.Abs(value.AsDouble());
        var text = absoluteValue.ToString(format, CultureInfo.InvariantCulture);
        var exponentIndex = text.IndexOf('E');

        if (exponentIndex < 0 || exponentIndex + 2 >= text.Length)
        {
            return text;
        }

        var mantissa = text[..exponentIndex];
        var exponentDigits = text[(exponentIndex + 2)..];

        if (!int.TryParse(exponentDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exponentValue))
        {
            return text;
        }

        var normalizedExponent = Math.Abs(exponentValue).ToString("00", CultureInfo.InvariantCulture);
        var sign = exponentValue >= 0 ? '+' : '-';
        return mantissa + "E" + sign + normalizedExponent;
    }

    private static string InsertThousandsSeparators(string integerPart)
    {
        var builder = new StringBuilder();
        var count = 0;

        for (var index = integerPart.Length - 1; index >= 0; index--)
        {
            if (count > 0 && count % 3 == 0)
            {
                builder.Insert(0, ',');
            }

            builder.Insert(0, integerPart[index]);
            count++;
        }

        return builder.ToString();
    }

    private static void SplitNumericParts(string text, out string integerPart, out string fractionPart)
    {
        var dotIndex = text.IndexOf('.');

        if (dotIndex >= 0)
        {
            integerPart = text[..dotIndex];
            fractionPart = text[(dotIndex + 1)..];
            return;
        }

        integerPart = text;
        fractionPart = string.Empty;
    }

    private static bool IsNegative(RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() < 0,
            var type when type == TypeSymbol.Long => value.AsLong() < 0,
            var type when type == TypeSymbol.Decimal => value.AsDecimal() < 0m,
            _ => value.AsDouble() < 0d
        };
    }

    private static bool IsNumericType(TypeSymbol type)
    {
        return type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal;
    }

    private static bool TryGetDecimal(RuntimeValue value, out decimal decimalValue)
    {
        decimalValue = 0m;

        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => TryAssignDecimal(value.AsInteger(), out decimalValue),
            var type when type == TypeSymbol.Long => TryAssignDecimal(value.AsLong(), out decimalValue),
            var type when type == TypeSymbol.Decimal => TryAssignDecimal(value.AsDecimal(), out decimalValue),
            _ => false
        };
    }

    private static bool TryAssignDecimal(int value, out decimal decimalValue)
    {
        decimalValue = value;
        return true;
    }

    private static bool TryAssignDecimal(long value, out decimal decimalValue)
    {
        decimalValue = value;
        return true;
    }

    private static bool TryAssignDecimal(decimal value, out decimal decimalValue)
    {
        decimalValue = value;
        return true;
    }

    private void ReportPrintUsingError(string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(BuiltinArgumentCode, $"PRINT USING: {detail}", location);
    }

    private static bool IsNumericMaskCharacter(char character)
    {
        return character is '#' or '0' or '+' or '-' or '.' or ',' or '$' or '*' or '^';
    }

    private static void FlushPrintUsingLiteral(List<PrintUsingFormatToken> tokens, StringBuilder literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        tokens.Add(new PrintUsingLiteralToken(literal.ToString()));
        literal.Clear();
    }

    private abstract record PrintUsingFormatToken;

    private sealed record PrintUsingLiteralToken(string Text) : PrintUsingFormatToken;

    private sealed record PrintUsingStringFieldToken(PrintUsingStringFieldKind Kind, int Width = 0) : PrintUsingFormatToken;

    private sealed record PrintUsingNumericFieldToken(string Mask, PrintUsingNumericFormatInfo Info) : PrintUsingFormatToken;

    private sealed class PrintUsingNumericFormatInfo
    {
        public int TotalWidth { get; init; }

        public int IntegerDigits { get; set; }

        public int FractionDigits { get; set; }

        public bool ForceSign { get; init; }

        public bool NegativeSignOnly { get; init; }

        public bool HasThousands { get; init; }

        public bool HasDollar { get; init; }

        public bool HasStarFill { get; init; }

        public bool HasExponent { get; init; }
    }

    private enum PrintUsingStringFieldKind
    {
        Bang,
        Backslash,
        Ampersand
    }
}

