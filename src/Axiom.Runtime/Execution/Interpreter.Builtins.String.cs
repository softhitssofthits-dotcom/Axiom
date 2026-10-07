using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static string EvaluateLeft(string text, int count)
    {
        return BuiltinStringHelpers.EvaluateLeft(text, count).AsString();
    }

    private static string EvaluateRight(string text, int count)
    {
        return BuiltinStringHelpers.EvaluateRight(text, count).AsString();
    }

    private static string EvaluateMid(string text, int start, int count)
    {
        return BuiltinStringHelpers.EvaluateMid(text, start, count).AsString();
    }

    private static string EvaluateReverse(string text)
    {
        return BuiltinStringHelpers.EvaluateReverse(text).AsString();
    }

    private static double ParseVal(string text)
    {
        return BuiltinStringHelpers.ParseVal(text);
    }
}
