using System.Globalization;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void AppendFormattedUsing(
        string format,
        ReadOnlySpan<RuntimeValue> values,
        TextLocation statementLocation)
    {
        if (format.Length > 2048)
        {
            ReportPrintUsingError("РґР»РёРЅР° С„РѕСЂРјР°С‚РЅРѕР№ СЃС‚СЂРѕРєРё РЅРµ РґРѕР»Р¶РЅР° РїСЂРµРІС‹С€Р°С‚СЊ 2048 СЃРёРјРІРѕР»РѕРІ.", statementLocation);
            throw new RuntimeFailureException();
        }

        var tokens = GetOrParsePrintUsingFormat(format);
        var argumentIndex = 0;
        _hasPendingOutput = true;

        foreach (var token in tokens)
        {
            switch (token)
            {
                case PrintUsingLiteralToken literalToken:
                    _pendingOutput.Append(literalToken.Text);
                    break;

                case PrintUsingStringFieldToken stringFieldToken:
                {
                    var valueText = GetNextStringFormatValue(stringFieldToken, values, ref argumentIndex, statementLocation);
                    AppendStringField(stringFieldToken, valueText);
                    break;
                }

                case PrintUsingNumericFieldToken numericFieldToken:
                {
                    var value = GetNextNumericFormatValue(values, ref argumentIndex, statementLocation);
                    AppendNumericField(numericFieldToken, value);
                    break;
                }
            }
        }
    }

    private List<PrintUsingFormatToken> GetOrParsePrintUsingFormat(string format)
    {
        if (_printUsingFormatCache.TryGetValue(format, out var cachedTokens))
        {
            return cachedTokens;
        }

        var parsedTokens = ParsePrintUsingFormat(format);
        _printUsingFormatCache[format] = parsedTokens;
        return parsedTokens;
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
        ReadOnlySpan<RuntimeValue> values,
        ref int index,
        TextLocation statementLocation)
    {
        if (index >= values.Length)
        {
            return field.Kind == PrintUsingStringFieldKind.Backslash
                ? new string(' ', field.Width)
                : string.Empty;
        }

        var value = values[index++];

        if (value.Type != TypeSymbol.String)
        {
            ReportPrintUsingError(
                $"СЃС‚СЂРѕРєРѕРІРѕРµ РїРѕР»Рµ С‚СЂРµР±СѓРµС‚ Р·РЅР°С‡РµРЅРёРµ С‚РёРїР° STRING, РЅРѕ РїРѕР»СѓС‡РµРЅ {value.Type.Name}.",
                statementLocation);
            throw new RuntimeFailureException();
        }

        return value.AsString();
    }

    private RuntimeValue GetNextNumericFormatValue(
        ReadOnlySpan<RuntimeValue> values,
        ref int index,
        TextLocation statementLocation)
    {
        if (index >= values.Length)
        {
            ReportPrintUsingError("РґР»СЏ С‡РёСЃР»РѕРІРѕРіРѕ РїРѕР»СЏ РЅРµ С…РІР°С‚РёР»Рѕ Р·РЅР°С‡РµРЅРёСЏ.", statementLocation);
            throw new RuntimeFailureException();
        }

        var value = values[index++];

        if (!IsNumericType(value.Type))
        {
            ReportPrintUsingError(
                $"С‡РёСЃР»РѕРІРѕРµ РїРѕР»Рµ С‚СЂРµР±СѓРµС‚ С‡РёСЃР»РѕРІРѕРµ Р·РЅР°С‡РµРЅРёРµ, РЅРѕ РїРѕР»СѓС‡РµРЅ {value.Type.Name}.",
                statementLocation);
            throw new RuntimeFailureException();
        }

        return value;
    }

    private void AppendStringField(PrintUsingStringFieldToken field, string value)
    {
        switch (field.Kind)
        {
            case PrintUsingStringFieldKind.Bang:
                if (value.Length > 0)
                {
                    _pendingOutput.Append(value.AsSpan(0, 1));
                }

                return;

            case PrintUsingStringFieldKind.Ampersand:
                _pendingOutput.Append(value);
                return;

            case PrintUsingStringFieldKind.Backslash:
                if (value.Length >= field.Width)
                {
                    _pendingOutput.Append(value.AsSpan(0, field.Width));
                    return;
                }

                _pendingOutput.Append(value);
                _pendingOutput.Append(' ', field.Width - value.Length);
                return;

            default:
                _pendingOutput.Append(value);
                return;
        }
    }

    private void AppendNumericField(PrintUsingNumericFieldToken field, RuntimeValue value)
    {
        var info = field.Info;

        if (!info.HasDollar && !info.HasStarFill && !info.HasThousands && !info.HasExponent)
        {
            AppendNumericBasic(field.Mask, info, value);
            return;
        }

        AppendNumericExtended(field.Mask, info, value);
    }

    private void AppendNumericBasic(
        string mask,
        PrintUsingNumericFormatInfo info,
        RuntimeValue value)
    {
        var negative = IsNegative(value);

        Span<char> formattedBuffer = stackalloc char[64];
        if (TryFormatAbsoluteNumericValue(value, info.FractionDigits, formattedBuffer, out var written))
        {
            var formattedSpan = formattedBuffer[..written];
            var dotIndex = formattedSpan.IndexOf('.');
            var integerPart = dotIndex >= 0 ? formattedSpan[..dotIndex] : formattedSpan;
            var fractionPart = dotIndex >= 0 ? formattedSpan[(dotIndex + 1)..] : ReadOnlySpan<char>.Empty;

            if (integerPart.Length > info.IntegerDigits)
            {
                _pendingOutput.Append('%');
                _pendingOutput.Append(value.ToDisplayString());
                return;
            }

            var signLength = info.ForceSign || (info.NegativeSignOnly && negative) ? 1 : 0;
            var fractionLength = info.FractionDigits > 0 ? 1 + info.FractionDigits : 0;
            var totalLength = signLength + info.IntegerDigits + fractionLength;
            if (totalLength < info.TotalWidth)
            {
                _pendingOutput.Append(' ', info.TotalWidth - totalLength);
            }

            if (info.ForceSign)
            {
                _pendingOutput.Append(negative ? '-' : '+');
            }
            else if (info.NegativeSignOnly && negative)
            {
                _pendingOutput.Append('-');
            }

            _pendingOutput.Append(' ', info.IntegerDigits - integerPart.Length);
            _pendingOutput.Append(integerPart);

            if (info.FractionDigits > 0)
            {
                _pendingOutput.Append('.');
                _pendingOutput.Append(fractionPart);
                if (fractionPart.Length < info.FractionDigits)
                {
                    _pendingOutput.Append('0', info.FractionDigits - fractionPart.Length);
                }
            }

            return;
        }

        var formattedNumber = FormatAbsoluteNumericValue(value, info.FractionDigits);
        SplitNumericParts(formattedNumber, out var integerPartString, out var fractionPartString);

        var integerPartText = integerPartString;
        var fractionPartText = fractionPartString;
        if (integerPartText.Length > info.IntegerDigits)
        {
            _pendingOutput.Append('%');
            _pendingOutput.Append(value.ToDisplayString());
            return;
        }

        var fallbackSignLength = info.ForceSign || (info.NegativeSignOnly && negative) ? 1 : 0;
        var fallbackFractionLength = info.FractionDigits > 0 ? 1 + info.FractionDigits : 0;
        var fallbackTotalLength = fallbackSignLength + info.IntegerDigits + fallbackFractionLength;
        if (fallbackTotalLength < info.TotalWidth)
        {
            _pendingOutput.Append(' ', info.TotalWidth - fallbackTotalLength);
        }

        if (info.ForceSign)
        {
            _pendingOutput.Append(negative ? '-' : '+');
        }
        else if (info.NegativeSignOnly && negative)
        {
            _pendingOutput.Append('-');
        }

        _pendingOutput.Append(' ', info.IntegerDigits - integerPartText.Length);
        _pendingOutput.Append(integerPartText);

        if (info.FractionDigits > 0)
        {
            _pendingOutput.Append('.');
            _pendingOutput.Append(fractionPartText);
            if (fractionPartText.Length < info.FractionDigits)
            {
                _pendingOutput.Append('0', info.FractionDigits - fractionPartText.Length);
            }
        }
    }

    private void AppendNumericExtended(
        string mask,
        PrintUsingNumericFormatInfo info,
        RuntimeValue value)
    {
        var negative = IsNegative(value);

        if (info.HasExponent)
        {
            var exponentText = FormatExponentValue(value, info.FractionDigits);
            var core = info.ForceSign
                ? (negative ? "-" : "+") + exponentText
                : negative
                    ? "-" + exponentText
                    : " " + exponentText;

            if (core.Length > info.TotalWidth)
            {
                _pendingOutput.Append('%');
                _pendingOutput.Append(value.ToDisplayString());
                return;
            }

            _pendingOutput.Append(' ', info.TotalWidth - core.Length);
            _pendingOutput.Append(core);
            return;
        }

        Span<char> formattedBuffer = stackalloc char[64];
        if (TryFormatAbsoluteNumericValue(value, info.FractionDigits, formattedBuffer, out var written))
        {
            var formattedSpan = formattedBuffer[..written];
            var dotIndex = formattedSpan.IndexOf('.');
            var integerPart = dotIndex >= 0 ? formattedSpan[..dotIndex] : formattedSpan;
            var fractionPart = dotIndex >= 0 ? formattedSpan[(dotIndex + 1)..] : ReadOnlySpan<char>.Empty;

            if (info.HasDollar && !info.HasStarFill && !info.HasThousands)
            {
                var signLength = info.ForceSign || negative ? 1 : 0;
                var fractionLength = info.FractionDigits > 0 ? 1 + fractionPart.Length : 0;
                var totalLength = signLength + 1 + integerPart.Length + fractionLength;

                if (totalLength > info.TotalWidth)
                {
                    _pendingOutput.Append('%');
                    _pendingOutput.Append(value.ToDisplayString());
                    return;
                }

                _pendingOutput.Append(' ', info.TotalWidth - totalLength);

                if (info.ForceSign)
                {
                    _pendingOutput.Append(negative ? '-' : '+');
                }
                else if (negative)
                {
                    _pendingOutput.Append('-');
                }

                _pendingOutput.Append('$');
                _pendingOutput.Append(integerPart);

                if (info.FractionDigits > 0)
                {
                    _pendingOutput.Append('.');
                    _pendingOutput.Append(fractionPart);
                    if (fractionPart.Length < info.FractionDigits)
                    {
                        _pendingOutput.Append('0', info.FractionDigits - fractionPart.Length);
                    }
                }

                return;
            }
        }

        var formattedNumber = FormatAbsoluteNumericValue(value, info.FractionDigits);
        SplitNumericParts(formattedNumber, out var integerPartString, out var fractionPartString);
        var integerPartText = info.HasThousands
            ? InsertThousandsSeparators(integerPartString)
            : integerPartString;
        var fractionPartText = fractionPartString;
        var signText = info.ForceSign
            ? negative ? "-" : "+"
            : negative
                ? "-"
                : string.Empty;

        var fractionText = info.FractionDigits > 0
            ? "." + fractionPartText.PadRight(info.FractionDigits, '0')
            : string.Empty;

        if (info.HasDollar && info.HasStarFill)
        {
            var full = signText + "$" + integerPartText + fractionText;
            if (full.Length > info.TotalWidth)
            {
                _pendingOutput.Append('%');
                _pendingOutput.Append(value.ToDisplayString());
                return;
            }

            _pendingOutput.Append('*', info.TotalWidth - full.Length);
            _pendingOutput.Append(full);
            return;
        }

        if (info.HasStarFill)
        {
            var full = signText + integerPartText + fractionText;
            if (full.Length > info.TotalWidth)
            {
                _pendingOutput.Append('%');
                _pendingOutput.Append(value.ToDisplayString());
                return;
            }

            _pendingOutput.Append('*', info.TotalWidth - full.Length);
            _pendingOutput.Append(full);
            return;
        }

        if (info.HasDollar)
        {
            var full = signText + "$" + integerPartText + fractionText;
            if (full.Length > info.TotalWidth)
            {
                _pendingOutput.Append('%');
                _pendingOutput.Append(value.ToDisplayString());
                return;
            }

            _pendingOutput.Append(' ', info.TotalWidth - full.Length);
            _pendingOutput.Append(full);
            return;
        }

        var result = signText + integerPartText + fractionText;
        if (result.Length > info.TotalWidth)
        {
            _pendingOutput.Append('%');
            _pendingOutput.Append(value.ToDisplayString());
            return;
        }

        _pendingOutput.Append(' ', info.TotalWidth - result.Length);
        _pendingOutput.Append(result);
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

    private static bool TryFormatAbsoluteNumericValue(
        RuntimeValue value,
        int fractionDigits,
        Span<char> destination,
        out int written)
    {
        Span<char> formatBuffer = stackalloc char[8];
        formatBuffer[0] = 'F';
        if (!fractionDigits.TryFormat(formatBuffer[1..], out var digitsWritten, provider: CultureInfo.InvariantCulture))
        {
            written = 0;
            return false;
        }

        var format = formatBuffer[..(digitsWritten + 1)];

        if (TryGetDecimal(value, out var decimalValue))
        {
            var absoluteValue = Math.Abs(decimalValue);
            var rounded = Math.Round(absoluteValue, fractionDigits, MidpointRounding.AwayFromZero);
            return rounded.TryFormat(destination, out written, format, CultureInfo.InvariantCulture);
        }

        var absoluteDouble = Math.Abs(value.AsDouble());
        var roundedDouble = Math.Round(absoluteDouble, fractionDigits, MidpointRounding.AwayFromZero);
        return roundedDouble.TryFormat(destination, out written, format, CultureInfo.InvariantCulture);
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

    private static void SplitNumericParts(ReadOnlySpan<char> text, out ReadOnlySpan<char> integerPart, out ReadOnlySpan<char> fractionPart)
    {
        var dotIndex = text.IndexOf('.');

        if (dotIndex >= 0)
        {
            integerPart = text[..dotIndex];
            fractionPart = text[(dotIndex + 1)..];
            return;
        }

        integerPart = text;
        fractionPart = ReadOnlySpan<char>.Empty;
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

