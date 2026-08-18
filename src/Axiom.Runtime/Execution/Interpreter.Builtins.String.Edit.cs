using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static string EvaluateInsert(string text, int start, string fragment)
    {
        return BuiltinStringHelpers.EvaluateInsert(text, start - 1, fragment).AsString();
    }

    private static string EvaluateRemove(string text, int start, int count)
    {
        return BuiltinStringHelpers.EvaluateRemove(text, start - 1, count).AsString();
    }
}
