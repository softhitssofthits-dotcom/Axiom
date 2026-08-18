using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static string EvaluateInsert(string text, int start, string fragment)
    {
        return BuiltinStringHelpers.EvaluateInsert(text, start - 1, fragment).AsString();
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

        return BuiltinStringHelpers.EvaluateInsert(text, start - 1, fragment);
    }

    private static string EvaluateRemove(string text, int start, int count)
    {
        return BuiltinStringHelpers.EvaluateRemove(text, start - 1, count).AsString();
    }
}
