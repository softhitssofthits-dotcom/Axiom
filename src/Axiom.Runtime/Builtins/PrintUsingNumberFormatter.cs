using System.Globalization;
using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static partial class BuiltinPrintUsingHelpers
{
    public static string FormatNumericField(PrintUsingNumericFieldToken field, RuntimeValue value)
    {
        var info = field.Info;

        if (!info.HasDollar && !info.HasStarFill && !info.HasThousands && !info.HasExponent)
        {
            return FormatNumericBasic(field.Mask, info, value);
        }

        return FormatNumericExtended(field.Mask, info, value);
    }

    public static string FormatNumericBasic(string mask, PrintUsingNumericFormatInfo info, RuntimeValue value)
    {
        var negative = IsNegative(value);
        var fullInvariant = value.ToDisplayString();
        var formattedNumber = FormatAbsoluteNumericValue(value, info.FractionDigits);
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

    public static string FormatNumericExtended(string mask, PrintUsingNumericFormatInfo info, RuntimeValue value)
    {
        var negative = IsNegative(value);
        var fullInvariant = value.ToDisplayString();

        if (info.HasExponent)
        {
            var exponentText = FormatExponentValue(value, info.FractionDigits);
            var core = info.ForceSign
                ? (negative ? "-" : "+") + exponentText
                : negative
                    ? "-" + exponentText
                    : " " + exponentText;

            return core.Length > info.TotalWidth
                ? "%" + fullInvariant
                : core.PadLeft(info.TotalWidth, ' ');
        }

        var formattedNumber = FormatAbsoluteNumericValue(value, info.FractionDigits);
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

    public static string FormatAbsoluteNumericValue(RuntimeValue value, int fractionDigits)
    {
        if (TryGetDecimal(value, out var decimalValue))
        {
            var absoluteValue = Math.Abs(decimalValue);
            var rounded = fractionDigits <= 28
                ? Math.Round(absoluteValue, fractionDigits, MidpointRounding.AwayFromZero)
                : absoluteValue;
            return rounded.ToString(fractionDigits > 0 ? $"F{fractionDigits}" : "F0", CultureInfo.InvariantCulture);
        }

        var absoluteDouble = Math.Abs(value.AsDouble());
        var roundedDouble = fractionDigits <= 15
            ? Math.Round(absoluteDouble, fractionDigits, MidpointRounding.AwayFromZero)
            : absoluteDouble;
        return roundedDouble.ToString(fractionDigits > 0 ? $"F{fractionDigits}" : "F0", CultureInfo.InvariantCulture);
    }

    public static string FormatExponentValue(RuntimeValue value, int fractionDigits)
    {
        var precision = fractionDigits > 0 ? fractionDigits : 2;
        var format = $"E{precision}";
        var text = TryGetDecimal(value, out var decimalValue)
            ? Math.Abs(decimalValue).ToString(format, CultureInfo.InvariantCulture)
            : Math.Abs(value.AsDouble()).ToString(format, CultureInfo.InvariantCulture);
        var exponentIndex = text.IndexOf('E');

        if (exponentIndex < 0 || exponentIndex + 2 >= text.Length)
        {
            return text;
        }

        var mantissa = text[..exponentIndex];
        var exponentDigits = text[(exponentIndex + 1)..];

        if (!int.TryParse(exponentDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exponentValue))
        {
            return text;
        }

        var normalizedExponent = Math.Abs(exponentValue).ToString("00", CultureInfo.InvariantCulture);
        var sign = exponentValue >= 0 ? '+' : '-';
        return mantissa + "E" + sign + normalizedExponent;
    }

    public static string InsertThousandsSeparators(string integerPart)
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

    public static void SplitNumericParts(string text, out string integerPart, out string fractionPart)
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

    public static bool IsNegative(RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() < 0,
            var type when type == TypeSymbol.Long => value.AsLong() < 0,
            var type when type == TypeSymbol.Decimal => value.AsDecimal() < 0m,
            _ => value.AsDouble() < 0d
        };
    }

    public static bool IsNumericType(TypeSymbol type)
    {
        return type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal;
    }

    public static bool TryGetDecimal(RuntimeValue value, out decimal decimalValue)
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
}
