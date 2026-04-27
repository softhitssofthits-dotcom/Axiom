using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private int EvaluateAsc(BoundCallExpression expression, string text)
    {
        if (text.Length == 0)
        {
            ReportBuiltInArgumentError(
                "ASC",
                "text",
                "не может быть пустой строкой.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        using var enumerator = text.EnumerateRunes();
        enumerator.MoveNext();
        return enumerator.Current.Value;
    }

    private static string EvaluateChr(BoundCallExpression expression, int code)
    {
        _ = expression;
        return new Rune(code).ToString();
    }

    private int RequireValidUnicodeCodePoint(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int value)
    {
        if (!Rune.IsValid(value))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "должен быть допустимым кодом символа Unicode.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return value;
    }
}
