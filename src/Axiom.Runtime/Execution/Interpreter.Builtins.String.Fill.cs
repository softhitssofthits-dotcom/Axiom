using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static string EvaluateRepeat(string text, int count)
    {
        return count == 0 || text.Length == 0
            ? string.Empty
            : string.Concat(Enumerable.Repeat(text, count));
    }

    private static string EvaluateSpace(int count)
    {
        return count == 0
            ? string.Empty
            : new string(' ', count);
    }

    private string EvaluateStringFill(BoundCallExpression expression, int count, string fill)
    {
        if (count == 0)
        {
            return string.Empty;
        }

        return new string(RequireFillCharacter(expression, "STRING", 1, fill), count);
    }

    private string EvaluatePadLeft(BoundCallExpression expression, string text, int width, string fill)
    {
        var fillCharacter = RequireFillCharacter(expression, "PADLEFT", 2, fill);
        return text.PadLeft(width, fillCharacter);
    }

    private string EvaluatePadRight(BoundCallExpression expression, string text, int width, string fill)
    {
        var fillCharacter = RequireFillCharacter(expression, "PADRIGHT", 2, fill);
        return text.PadRight(width, fillCharacter);
    }

    private string EvaluateCenter(BoundCallExpression expression, string text, int width, string fill)
    {
        var fillCharacter = RequireFillCharacter(expression, "CENTER", 2, fill);

        if (text.Length >= width)
        {
            return text;
        }

        var padLeft = (width - text.Length) / 2;
        return text
            .PadLeft(text.Length + padLeft, fillCharacter)
            .PadRight(width, fillCharacter);
    }
}
