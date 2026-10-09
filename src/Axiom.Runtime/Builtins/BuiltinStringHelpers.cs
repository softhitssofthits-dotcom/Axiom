using System.Text;
using System.Text.RegularExpressions;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinStringHelpers
{
    public static RuntimeValue EvaluateLeft(string text, int count)
    {
        if (count <= 0) return RuntimeValue.FromString(string.Empty);
        if (count >= text.Length) return RuntimeValue.FromString(text);
        return RuntimeValue.FromString(text[..count]);
    }

    public static RuntimeValue EvaluateRight(string text, int count)
    {
        if (count <= 0) return RuntimeValue.FromString(string.Empty);
        if (count >= text.Length) return RuntimeValue.FromString(text);
        return RuntimeValue.FromString(text[^count..]);
    }

    public static RuntimeValue EvaluateMid(string text, int start, int count)
    {
        if (start < 1 || start > text.Length || count <= 0)
            return RuntimeValue.FromString(string.Empty);

        var startIndex = start - 1;
        var available = text.Length - startIndex;
        var length = Math.Min(count, available);
        return RuntimeValue.FromString(text.Substring(startIndex, length));
    }

    public static RuntimeValue EvaluateReverse(string text)
    {
        var result = new StringBuilder(text.Length);
        for (var index = text.Length - 1; index >= 0; index--)
        {
            if (char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1]))
            {
                result.Append(text[index - 1]).Append(text[index]);
                index--;
            }
            else
            {
                // Изолированные единицы UTF-16 сохраняются без подмены U+FFFD.
                result.Append(text[index]);
            }
        }
        return RuntimeValue.FromString(result.ToString());
    }

    public static RuntimeValue EvaluateInsert(string text, int start, string insert)
    {
        if (start < 0) start = 0;
        if (start > text.Length) start = text.Length;
        return RuntimeValue.FromString(text.Insert(start, insert));
    }

    public static RuntimeValue EvaluateRemove(string text, int start, int count)
    {
        if (start < 0 || start >= text.Length || count <= 0)
            return RuntimeValue.FromString(text);

        var length = Math.Min(count, text.Length - start);
        return RuntimeValue.FromString(text.Remove(start, length));
    }

    public static RuntimeValue EvaluateRepeat(string text, int count)
    {
        if (count <= 0 || text.Length == 0)
            return RuntimeValue.FromString(string.Empty);

        return RuntimeValue.FromString(string.Concat(Enumerable.Repeat(text, count)));
    }

    public static RuntimeValue EvaluateSpace(int count)
    {
        if (count <= 0) return RuntimeValue.FromString(string.Empty);
        return RuntimeValue.FromString(new string(' ', count));
    }

    public static int EvaluateInStr(int start, string text, string fragment)
    {
        if (start < 1 || start > text.Length + 1) return 0;
        if (fragment.Length == 0) return start;
        var index = text.IndexOf(fragment, start - 1, StringComparison.Ordinal);
        return index >= 0 ? index + 1 : 0;
    }

    public static int EvaluateInStrRev(int start, string text, string fragment)
    {
        if (start < 1) return 0;
        if (fragment.Length == 0) return Math.Min(start, text.Length) + 1;
        if (text.Length == 0) return 0;
        var clampedStart = Math.Min(start, text.Length);
        var index = text.LastIndexOf(fragment, clampedStart - 1, StringComparison.Ordinal);
        return index >= 0 ? index + 1 : 0;
    }

    public static int EvaluateSubStrCount(string text, string fragment)
    {
        if (fragment.Length == 0) return 0;
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }
        return count;
    }

    public static string TrimByCharacters(string text, string characters, bool trimStart, bool trimEnd)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(characters))
            return text;

        var charSet = new HashSet<char>(characters);
        var start = 0;
        var end = text.Length - 1;

        if (trimStart)
        {
            while (start <= end && charSet.Contains(text[start])) start++;
        }

        if (trimEnd)
        {
            while (end >= start && charSet.Contains(text[end])) end--;
        }

        if (start > end) return string.Empty;
        if (start == 0 && end == text.Length - 1) return text;
        return text.Substring(start, end - start + 1);
    }

    public static RuntimeValue EvaluateContains(string text, string fragment)
    {
        return RuntimeValue.FromBoolean(text.Contains(fragment, StringComparison.Ordinal));
    }

    public static RuntimeValue EvaluateStartsWith(string text, string fragment)
    {
        return RuntimeValue.FromBoolean(text.StartsWith(fragment, StringComparison.Ordinal));
    }

    public static RuntimeValue EvaluateEndsWith(string text, string fragment)
    {
        return RuntimeValue.FromBoolean(text.EndsWith(fragment, StringComparison.Ordinal));
    }

    public static string EvaluateReplace(string text, string oldValue, string newValue)
    {
        return text.Replace(oldValue, newValue, StringComparison.Ordinal);
    }

    public static string TrimWhitespace(string text) => text.Trim();

    public static string LeftTrimWhitespace(string text) => text.TrimStart();

    public static string RightTrimWhitespace(string text) => text.TrimEnd();

    public static bool IsNumeric(string text) =>
        text.IndexOf('\0') < 0 && double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && double.IsFinite(value);

    public static double ParseVal(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0d;

        var trimmed = text.Trim();

        if (double.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return double.IsFinite(value) ? value : 0d;

        var i = 0;
        var hasDigits = false;

        if (i < trimmed.Length && (trimmed[i] == '+' || trimmed[i] == '-'))
            i++;

        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
        {
            hasDigits = true;
            i++;
        }

        if (i < trimmed.Length && trimmed[i] == '.')
        {
            i++;
            while (i < trimmed.Length && char.IsDigit(trimmed[i]))
            {
                hasDigits = true;
                i++;
            }
        }

        if (!hasDigits) return 0d;

        if (i < trimmed.Length && (trimmed[i] == 'e' || trimmed[i] == 'E'))
        {
            i++;
            if (i < trimmed.Length && (trimmed[i] == '+' || trimmed[i] == '-'))
                i++;

            while (i < trimmed.Length && char.IsDigit(trimmed[i]))
                i++;
        }

        if (i < trimmed.Length)
            trimmed = trimmed[..i];

        return double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed) ? parsed : 0d;
    }
}
