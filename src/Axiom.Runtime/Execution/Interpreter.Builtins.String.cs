using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
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
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
               && double.IsFinite(value)
            ? value
            : 0d;
    }
}
