using System.Globalization;

namespace Axiom.Core;

/// <summary>Единый строгий разбор канонического временного текста для связывания и исполнения.</summary>
public static class CanonicalTemporalText
{
    private static readonly string[] TimeFormats = ["HH:mm:ss", "HH:mm:ss.FFFFFFF"];
    private static readonly string[] DateTimeFormats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFFFFFF"];
    private static readonly string[] ExchangeFormats = ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"];
    private static readonly string[] InstantFormats = ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];

    public static bool TryParseDate(ReadOnlySpan<char> text, out DateOnly value)
    {
        value = default;
        return text.Length == 10 && text.IndexOf('\0') < 0
            && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    public static bool TryParseTime(ReadOnlySpan<char> text, out TimeOnly value)
    {
        value = default;
        return HasCanonicalLengthAndFraction(text, 8)
            && TimeOnly.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    public static bool TryParseDateTime(ReadOnlySpan<char> text, out DateTime value)
    {
        value = default;
        return HasCanonicalLengthAndFraction(text, 19)
            && DateTime.TryParseExact(text, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    public static bool TryParseExchangeDateTime(ReadOnlySpan<char> text, out DateTime value)
    {
        value = default;
        return HasCanonicalLengthAndFraction(text, 19)
            && DateTime.TryParseExact(text, ExchangeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    public static bool TryParseInstant(ReadOnlySpan<char> text, out DateTime value)
    {
        value = default;
        if (text.IsEmpty || text[^1] != 'Z' || !HasCanonicalLengthAndFraction(text[..^1], 19)) return false;
        return DateTime.TryParseExact(text, InstantFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out value);
    }

    private static bool HasCanonicalLengthAndFraction(ReadOnlySpan<char> text, int wholeLength)
    {
        if (text.IndexOf('\0') >= 0) return false;
        if (text.Length == wholeLength) return true;
        // Платформенный формат F допускает точку без цифр; договор языка — нет.
        if (text.Length < wholeLength + 2 || text.Length > wholeLength + 8 || text[wholeLength] != '.') return false;
        foreach (var digit in text[(wholeLength + 1)..])
            if (!char.IsAsciiDigit(digit)) return false;
        return true;
    }
}
