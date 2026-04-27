using System.Text;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static string EvaluateLeft(string text, int count)
    {
        return count >= text.Length
            ? text
            : text.Substring(0, count);
    }

    private static string EvaluateRight(string text, int count)
    {
        return count >= text.Length
            ? text
            : text.Substring(text.Length - count, count);
    }

    private static string EvaluateMid(string text, int start, int count)
    {
        if (start > text.Length)
        {
            return string.Empty;
        }

        var zeroBasedStart = start - 1;
        var available = text.Length - zeroBasedStart;
        var length = Math.Min(count, available);
        return text.Substring(zeroBasedStart, length);
    }

    private static string EvaluateTrimWhitespace(string text) => text.Trim();

    private static string EvaluateTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: true, trimRight: true);

    private static string EvaluateLeftTrimWhitespace(string text) => text.TrimStart();

    private static string EvaluateLeftTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: true, trimRight: false);

    private static string EvaluateRightTrimWhitespace(string text) => text.TrimEnd();

    private static string EvaluateRightTrimCharacters(string text, string chars) => TrimByCharacters(text, chars, trimLeft: false, trimRight: true);

    private static string EvaluateReverse(string text)
    {
        if (text.Length <= 1)
        {
            return text;
        }

        var buffer = text.ToCharArray();
        Array.Reverse(buffer);
        return new string(buffer);
    }

    private static bool EvaluateContains(string text, string fragment)
    {
        return text.Contains(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateStartsWith(string text, string fragment)
    {
        return text.StartsWith(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateEndsWith(string text, string fragment)
    {
        return text.EndsWith(fragment, StringComparison.Ordinal);
    }

    private string EvaluateReplace(string text, string oldText, string newText, TextLocation location)
    {
        if (oldText.Length == 0)
        {
            ReportBuiltInArgumentError("REPLACE", "oldText", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        return text.Replace(oldText, newText, StringComparison.Ordinal);
    }

    private static int EvaluateInStr(int start, string text, string fragment)
    {
        if (start > text.Length + 1)
        {
            return 0;
        }

        var index = text.IndexOf(fragment, start - 1, StringComparison.Ordinal);
        return index < 0 ? 0 : index + 1;
    }

    private static int EvaluateInStrRev(int start, string text, string fragment)
    {
        var searchStart = Math.Min(start, text.Length);

        if (fragment.Length == 0)
        {
            return searchStart + 1;
        }

        if (text.Length == 0)
        {
            return 0;
        }

        var index = text.LastIndexOf(fragment, searchStart - 1, StringComparison.Ordinal);
        return index < 0 ? 0 : index + 1;
    }

    private int EvaluateSubStrCount(string text, string fragment, TextLocation location)
    {
        if (fragment.Length == 0)
        {
            ReportBuiltInArgumentError("SUBSTRCOUNT", "fragment", "не может быть пустым.", location);
            throw new RuntimeFailureException();
        }

        var count = 0;
        var startIndex = 0;

        while (startIndex <= text.Length - fragment.Length)
        {
            var index = text.IndexOf(fragment, startIndex, StringComparison.Ordinal);

            if (index < 0)
            {
                break;
            }

            count++;
            startIndex = index + fragment.Length;
        }

        return count;
    }

    private int EvaluateAsc(string text, TextLocation location)
    {
        if (text.Length == 0)
        {
            ReportBuiltInArgumentError("ASC", "text", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        using var enumerator = text.EnumerateRunes();
        enumerator.MoveNext();
        return enumerator.Current.Value;
    }

    private static string EvaluateChr(int code)
    {
        return new Rune(code).ToString();
    }

    private RuntimeValue EvaluateSplitBuiltin(string text, string separator, TextLocation location)
    {
        if (separator.Length == 0)
        {
            ReportBuiltInArgumentError("SPLIT", "separator", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        var parts = text.Length == 0
            ? []
            : text.Split([separator], StringSplitOptions.None);

        return CreateStringArrayValue(parts);
    }

    private static RuntimeValue EvaluateJoinBuiltin(RuntimeArrayValue array, string separator)
    {
        if (!array.IsAllocated || array.Length == 0)
        {
            return RuntimeValue.FromString(string.Empty);
        }

        var values = new string[array.Length];

        for (var index = 0; index < array.Length; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index).AsString();
        }

        return RuntimeValue.FromString(string.Join(separator, values));
    }
}
