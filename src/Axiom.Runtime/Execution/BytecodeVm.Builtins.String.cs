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

    private static string EvaluateTrimWhitespace(string text) => BuiltinStringHelpers.TrimWhitespace(text);

    private static RuntimeValue EvaluateTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var trimmed = BuiltinStringHelpers.TrimWhitespace(text);
        return trimmed.Length == text.Length ? value : RuntimeValue.FromString(trimmed);
    }

    private static string EvaluateTrimCharacters(string text, string chars) => BuiltinStringHelpers.TrimByCharacters(text, chars, trimStart: true, trimEnd: true);

    private static string EvaluateLeftTrimWhitespace(string text) => BuiltinStringHelpers.LeftTrimWhitespace(text);

    private static RuntimeValue EvaluateLeftTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var trimmed = BuiltinStringHelpers.LeftTrimWhitespace(text);
        return trimmed.Length == text.Length ? value : RuntimeValue.FromString(trimmed);
    }

    private static string EvaluateLeftTrimCharacters(string text, string chars) => BuiltinStringHelpers.TrimByCharacters(text, chars, trimStart: true, trimEnd: false);

    private static string EvaluateRightTrimWhitespace(string text) => BuiltinStringHelpers.RightTrimWhitespace(text);

    private static RuntimeValue EvaluateRightTrimWhitespaceBuiltin(RuntimeValue value)
    {
        var text = GetStringUnchecked(value);
        var trimmed = BuiltinStringHelpers.RightTrimWhitespace(text);
        return trimmed.Length == text.Length ? value : RuntimeValue.FromString(trimmed);
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

        return BuiltinStringHelpers.EvaluateReverse(text);
    }

    private static bool EvaluateContains(string text, string fragment)
    {
        return BuiltinStringHelpers.EvaluateContains(text, fragment).AsBoolean();
    }

    private static bool EvaluateStartsWith(string text, string fragment)
    {
        return BuiltinStringHelpers.EvaluateStartsWith(text, fragment).AsBoolean();
    }

    private static bool EvaluateEndsWith(string text, string fragment)
    {
        return BuiltinStringHelpers.EvaluateEndsWith(text, fragment).AsBoolean();
    }

    private string EvaluateReplace(string text, string oldText, string newText, TextLocation location)
    {
        if (oldText.Length == 0)
        {
            ReportBuiltInArgumentError("REPLACE", "oldText", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        return BuiltinStringHelpers.EvaluateReplace(text, oldText, newText);
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

        var replaced = BuiltinStringHelpers.EvaluateReplace(text, oldText, newText);
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
            return RuntimeValue.FromString(array.GetValue(array.LowerBound).AsString());
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
