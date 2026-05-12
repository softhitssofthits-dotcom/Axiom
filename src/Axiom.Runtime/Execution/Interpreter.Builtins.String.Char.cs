using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private int EvaluateAsc(BoundCallExpression expression, string text)
    {
        return EvaluateFirstRuneCode(expression, text, "ASC", RuntimeCharacterEncoding.Asc);
    }

    private int EvaluateAscW(BoundCallExpression expression, string text)
    {
        return EvaluateFirstRuneCode(expression, text, "ASCW", RuntimeCharacterEncoding.AscW);
    }

    private int EvaluateFirstRuneCode(
        BoundCallExpression expression,
        string text,
        string functionName,
        Func<Rune, int> getCode)
    {
        if (text.Length == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                "text",
                "не может быть пустой строкой.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        using var enumerator = text.EnumerateRunes();
        enumerator.MoveNext();
        return getCode(enumerator.Current);
    }

    private static string EvaluateChr(BoundCallExpression expression, int code)
    {
        _ = expression;
        return RuntimeCharacterEncoding.Chr(code);
    }

    private static string EvaluateChrW(BoundCallExpression expression, int code)
    {
        _ = expression;
        return RuntimeCharacterEncoding.ChrW(code);
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
