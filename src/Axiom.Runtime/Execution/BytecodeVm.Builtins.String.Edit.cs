namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static string EvaluateInsert(string text, int start, string fragment)
    {
        var insertIndex = Math.Min(start - 1, text.Length);
        return text.Insert(insertIndex, fragment);
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
