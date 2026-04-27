using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private int RequireNonNegativeCount(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int value)
    {
        if (value < 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return value;
    }

    private char RequireFillCharacter(
        BoundCallExpression expression,
        string functionName,
        int argumentIndex,
        string fill)
    {
        if (fill.Length == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                "fill",
                "не может быть пустой строкой.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return fill[0];
    }

    private static string TrimByCharacters(string text, string chars, bool trimLeft, bool trimRight)
    {
        if (text.Length == 0 || chars.Length == 0)
        {
            return text;
        }

        var trimChars = chars.ToCharArray();
        var result = text;

        if (trimLeft)
        {
            result = result.TrimStart(trimChars);
        }

        if (trimRight)
        {
            result = result.TrimEnd(trimChars);
        }

        return result;
    }
}
