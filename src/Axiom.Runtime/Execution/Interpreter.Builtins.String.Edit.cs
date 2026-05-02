using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
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
