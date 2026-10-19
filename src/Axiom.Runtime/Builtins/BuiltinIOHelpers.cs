using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinIOHelpers
{


    public static RuntimeValue? TryParseInputField(TypeSymbol type, ReadOnlySpan<char> text)
    {
        if (type == TypeSymbol.Integer)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue)
                ? RuntimeValue.FromInteger(integerValue)
                : null;
        }

        if (type == TypeSymbol.Long)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue)
                ? RuntimeValue.FromLong(longValue)
                : null;
        }

        if (type == TypeSymbol.Double)
        {
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var doubleValue)
                   && double.IsFinite(doubleValue)
                ? RuntimeValue.FromDouble(doubleValue)
                : null;
        }

        if (type == TypeSymbol.Decimal)
        {
            return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalValue)
                ? RuntimeValue.FromDecimal(decimalValue)
                : null;
        }

        if (type == TypeSymbol.String)
        {
            return RuntimeValue.FromString(text.ToString());
        }

        if (type == TypeSymbol.Boolean)
        {
            if (text.Equals("TRUE".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(true);
            }

            if (text.Equals("FALSE".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(false);
            }

            return null;
        }

        // Временной ввод использует те же канонические формы, что и присваивание:
        // строку не угадывают, поэтому пользователь вводит точную запись.
        if (type == TypeSymbol.Date)
        {
            return DateOnly.TryParseExact(text, "yyyy-MM-dd".AsSpan(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? RuntimeValue.FromDate(date)
                : null;
        }

        if (type == TypeSymbol.Time)
        {
            return TimeOnly.TryParseExact(text, ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
                ? RuntimeValue.FromTime(time)
                : null;
        }

        if (type == TypeSymbol.DateTime)
        {
            return DateTime.TryParseExact(text, ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)
                ? RuntimeValue.FromDateTime(dateTime)
                : null;
        }

        if (type == TypeSymbol.UtcDateTime)
        {
            return BuiltinDateTimeHelpers.TryParseCanonicalInstant(text.ToString(), out var instant)
                ? RuntimeValue.FromUtcDateTime(instant)
                : null;
        }

        return null;
    }

    public static string NormalizeInputField(string text, bool trim)
    {
        return trim ? text.Trim() : text;
    }

    public static ReadOnlySpan<char> NormalizeInputField(ReadOnlySpan<char> text, bool trim)
    {
        if (!trim)
        {
            return text;
        }

        var start = 0;
        var end = text.Length - 1;

        while (start <= end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end >= start && char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        return start > end
            ? ReadOnlySpan<char>.Empty
            : text.Slice(start, end - start + 1);
    }

    public static int CountInputFields(string text)
    {
        var count = 1;

        foreach (var ch in text)
        {
            if (ch == ',')
            {
                count++;
            }
        }

        return count;
    }
}
