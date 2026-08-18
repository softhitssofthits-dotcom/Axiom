using System.Text;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static RuntimeValue EvaluateLeft(string text, int count)
    {
        return BuiltinStringHelpers.EvaluateLeft(text, count);
    }

    private static RuntimeValue EvaluateRight(string text, int count)
    {
        return BuiltinStringHelpers.EvaluateRight(text, count);
    }

    private static RuntimeValue EvaluateRightBuiltin(RuntimeValue value, int count)
    {
        var text = GetStringUnchecked(value);
        return count >= text.Length
            ? value
            : RuntimeValue.FromString(text.Substring(text.Length - count, count));
    }

    private static RuntimeValue EvaluateMid(string text, int start, int count)
    {
        return BuiltinStringHelpers.EvaluateMid(text, start, count);
    }

    private static string EvaluateTrimWhitespace(string text)
    {
        var start = FindTrimStart(text);
        if (start == text.Length)
        {
            return string.Empty;
        }

        var end = FindTrimEnd(text, start);
        return start == 0 && end == text.Length - 1
            ? text
            : text.Substring(start, end - start + 1);
    }

    private static RuntimeValue EvaluateTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var start = FindTrimStart(text);
        if (start == text.Length)
        {
            return RuntimeValue.FromString(string.Empty);
        }

        var end = FindTrimEnd(text, start);
        return start == 0 && end == text.Length - 1
            ? value
            : RuntimeValue.FromString(text.Substring(start, end - start + 1));
    }

    private static string EvaluateTrimCharacters(string text, string chars) => BuiltinStringHelpers.TrimByCharacters(text, chars, trimStart: true, trimEnd: true);

    private static string EvaluateLeftTrimWhitespace(string text)
    {
        var start = FindTrimStart(text);
        return start == 0
            ? text
            : start == text.Length
                ? string.Empty
                : text[start..];
    }

    private static RuntimeValue EvaluateLeftTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var start = FindTrimStart(text);
        return start == 0
            ? value
            : RuntimeValue.FromString(start == text.Length ? string.Empty : text[start..]);
    }

    private static string EvaluateLeftTrimCharacters(string text, string chars) => BuiltinStringHelpers.TrimByCharacters(text, chars, trimStart: true, trimEnd: false);

    private static string EvaluateRightTrimWhitespace(string text)
    {
        var end = FindTrimEnd(text, start: 0);
        return end == text.Length - 1
            ? text
            : end < 0
                ? string.Empty
                : text[..(end + 1)];
    }

    private static RuntimeValue EvaluateRightTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var end = FindTrimEnd(text, start: 0);
        return end == text.Length - 1
            ? value
            : RuntimeValue.FromString(end < 0 ? string.Empty : text[..(end + 1)]);
    }

    private static string EvaluateRightTrimCharacters(string text, string chars) => BuiltinStringHelpers.TrimByCharacters(text, chars, trimStart: false, trimEnd: true);

    private static RuntimeValue EvaluateReverse(string text)
    {
        return BuiltinStringHelpers.EvaluateReverse(text);
    }

    private static RuntimeValue EvaluateReverseBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        if (text.Length <= 1)
        {
            return value;
        }

        var buffer = text.ToCharArray();
        Array.Reverse(buffer);
        return RuntimeValue.FromString(new string(buffer));
    }

    private static bool EvaluateContains(string text, string fragment)
    {
        if (fragment.Length == 0)
        {
            return true;
        }

        if (text.Length == 0 || fragment.Length > text.Length)
        {
            return false;
        }

        if (ReferenceEquals(text, fragment))
        {
            return true;
        }

        if (fragment.Length == text.Length)
        {
            return string.Equals(text, fragment, StringComparison.Ordinal);
        }

        if (fragment.Length == 1)
        {
            return text.IndexOf(fragment[0]) >= 0;
        }

        return text.Contains(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateStartsWith(string text, string fragment)
    {
        if (fragment.Length == 0)
        {
            return true;
        }

        if (fragment.Length > text.Length)
        {
            return false;
        }

        if (ReferenceEquals(text, fragment))
        {
            return true;
        }

        if (fragment.Length == text.Length)
        {
            return string.Equals(text, fragment, StringComparison.Ordinal);
        }

        return text.StartsWith(fragment, StringComparison.Ordinal);
    }

    private static bool EvaluateEndsWith(string text, string fragment)
    {
        if (fragment.Length == 0)
        {
            return true;
        }

        if (fragment.Length > text.Length)
        {
            return false;
        }

        if (ReferenceEquals(text, fragment))
        {
            return true;
        }

        if (fragment.Length == text.Length)
        {
            return string.Equals(text, fragment, StringComparison.Ordinal);
        }

        return text.EndsWith(fragment, StringComparison.Ordinal);
    }

    private static int FindTrimStart(string text)
    {
        var start = 0;

        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        return start;
    }

    private static int FindTrimEnd(string text, int start)
    {
        var end = text.Length - 1;

        while (end >= start && char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        return end;
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

    private RuntimeValue EvaluateReplaceBuiltin(
        RuntimeValue textValue,
        RuntimeValue oldTextValue,
        RuntimeValue newTextValue,
        TextLocation location)
    {
        var text = GetStringUnchecked(textValue);
        var oldText = GetStringUnchecked(oldTextValue);
        var newText = GetStringUnchecked(newTextValue);

        if (oldText.Length == 0)
        {
            ReportBuiltInArgumentError("REPLACE", "oldText", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        if (ReferenceEquals(oldText, newText) || string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            return textValue;
        }

        if (text.Length == 0 || oldText.Length > text.Length)
        {
            return textValue;
        }

        var firstMatchIndex = text.IndexOf(oldText, StringComparison.Ordinal);
        if (firstMatchIndex < 0)
        {
            return textValue;
        }

        string replaced;
        if (oldText.Length == 1 && newText.Length == 1)
        {
            replaced = text.Replace(oldText[0], newText[0]);
        }
        else if (firstMatchIndex == 0 && oldText.Length == text.Length)
        {
            replaced = newText;
        }
        else
        {
            replaced = text.Replace(oldText, newText, StringComparison.Ordinal);
        }

        return string.Equals(replaced, text, StringComparison.Ordinal)
            ? textValue
            : RuntimeValue.FromString(replaced);
    }

    private RuntimeValue EvaluateReplaceManyBuiltin(
        RuntimeValue textValue,
        RuntimeValue oldItemsValue,
        RuntimeValue newItemsValue,
        TextLocation location)
    {
        var text = GetStringUnchecked(textValue);
        var oldRules = ReadReplaceManyRules("oldItems", oldItemsValue.AsArray(), location);
        var newRules = ReadReplaceManyRules("newItems", newItemsValue.AsArray(), location);

        if (oldRules.Length != newRules.Length)
        {
            ReportBuiltInArgumentError("REPLACEMANY", "newItems", "должен иметь ту же длину, что и oldItems.", location);
            throw new RuntimeFailureException();
        }

        for (var index = 0; index < oldRules.Length; index++)
        {
            if (oldRules[index].Length == 0)
            {
                ReportBuiltInArgumentError("REPLACEMANY", "oldItems", "не должен содержать пустые строки.", location);
                throw new RuntimeFailureException();
            }
        }

        var replaced = RuntimeStringReplaceMany.Replace(text, oldRules, newRules);
        return ReferenceEquals(replaced, text)
            ? textValue
            : RuntimeValue.FromString(replaced);
    }

    private string[] ReadReplaceManyRules(string argumentName, RuntimeArrayValue array, TextLocation location)
    {
        if (!array.IsAllocated)
        {
            return [];
        }

        if (array.Rank != 1)
        {
            ReportBuiltInArgumentError("REPLACEMANY", argumentName, "должен быть одномерным массивом строк.", location);
            throw new RuntimeFailureException();
        }

        var values = new string[array.Length];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index).AsString();
        }

        return values;
    }

    private static int EvaluateInStr(int start, string text, string fragment)
    {
        return BuiltinStringHelpers.EvaluateInStr(start, text, fragment);
    }

    private static int EvaluateInStrRev(int start, string text, string fragment)
    {
        return BuiltinStringHelpers.EvaluateInStrRev(start, text, fragment);
    }

    private int EvaluateSubStrCount(string text, string fragment, TextLocation location)
    {
        if (fragment.Length == 0)
        {
            ReportBuiltInArgumentError("SUBSTRCOUNT", "fragment", "не может быть пустым.", location);
            throw new RuntimeFailureException();
        }

        return BuiltinStringHelpers.EvaluateSubStrCount(text, fragment);
    }

    private int EvaluateAsc(string text, TextLocation location)
    {
        return EvaluateFirstRuneCode(text, "ASC", location, RuntimeCharacterEncoding.Asc);
    }

    private int EvaluateAscW(string text, TextLocation location)
    {
        return EvaluateFirstRuneCode(text, "ASCW", location, RuntimeCharacterEncoding.AscW);
    }

    private int EvaluateFirstRuneCode(
        string text,
        string functionName,
        TextLocation location,
        Func<Rune, int> getCode)
    {
        if (text.Length == 0)
        {
            ReportBuiltInArgumentError(functionName, "text", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        using var enumerator = text.EnumerateRunes();
        enumerator.MoveNext();
        return getCode(enumerator.Current);
    }

    private static string EvaluateChr(int code)
    {
        return RuntimeCharacterEncoding.Chr(code);
    }

    private static string EvaluateChrW(int code)
    {
        return RuntimeCharacterEncoding.ChrW(code);
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

        if (array.Length == 1)
        {
            return array.GetValue(array.LowerBound);
        }

        var values = new string[array.Length];

        for (var index = 0; index < array.Length; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index).AsString();
        }

        return RuntimeValue.FromString(string.Join(separator, values));
    }

    private static RuntimeValue EvaluateLowerCaseBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var lowered = text.ToLowerInvariant();
        return string.Equals(lowered, text, StringComparison.Ordinal)
            ? value
            : RuntimeValue.FromString(lowered);
    }

    private static RuntimeValue EvaluateUpperCaseBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var uppered = text.ToUpperInvariant();
        return string.Equals(uppered, text, StringComparison.Ordinal)
            ? value
            : RuntimeValue.FromString(uppered);
    }
}
