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

    private int EvaluateIntBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var floored = value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Floor(value.AsDouble()), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Floor(value.AsDecimal()), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            _ => ReportInvalidRuntimeState<int>($"Unsupported INT argument type '{value.Type.Name}'.", expression.Location)
        };

        return floored;
    }

    private static double ParseVal(string text)
    {
        return BuiltinStringHelpers.ParseVal(text);
    }
}
