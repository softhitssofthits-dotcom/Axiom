namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static string EvaluateInsert(string text, int start, string fragment)
    {
        if (fragment.Length == 0)
        {
            return text;
        }

        if (text.Length == 0)
        {
            return fragment;
        }

        if (start == 1)
        {
            return string.Concat(fragment, text);
        }

        if (start > text.Length)
        {
            return string.Concat(text, fragment);
        }

        return text.Insert(start - 1, fragment);
    }

    private static RuntimeValue EvaluateInsertBuiltin(RuntimeValue textValue, int start, RuntimeValue fragmentValue)
    {
        var text = GetStringUnchecked(textValue);
        var fragment = GetStringUnchecked(fragmentValue);

        if (fragment.Length == 0)
        {
            return textValue;
        }

        if (text.Length == 0)
        {
            return fragmentValue;
        }

        if (start == 1)
        {
            return RuntimeValue.FromString(string.Concat(fragment, text));
        }

        if (start > text.Length)
        {
            return RuntimeValue.FromString(string.Concat(text, fragment));
        }

        return RuntimeValue.FromString(text.Insert(start - 1, fragment));
    }

    private static string EvaluateRemove(string text, int start, int count)
    {
        if (count == 0 || start > text.Length)
        {
            return text;
        }

        var removeIndex = start - 1;
        var availableCount = text.Length - removeIndex;
        var actualCount = Math.Min(count, availableCount);
        return text.Remove(removeIndex, actualCount);
    }
}
