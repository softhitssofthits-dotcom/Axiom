using System.Linq;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
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

    private string EvaluateStringFill(int count, string fill, TextLocation location)
    {
        if (count == 0)
        {
            return string.Empty;
        }

        return new string(RequireFillCharacter("STRING", fill, location), count);
    }

    private string EvaluatePadLeft(string text, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("PADLEFT", fill, location);
        return text.PadLeft(width, fillCharacter);
    }

    private string EvaluatePadRight(string text, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("PADRIGHT", fill, location);
        return text.PadRight(width, fillCharacter);
    }

    private string EvaluateCenter(string text, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("CENTER", fill, location);

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
