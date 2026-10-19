using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static RuntimeValue EvaluateRepeat(string text, int count)
    {
        return BuiltinStringHelpers.EvaluateRepeat(text, count);
    }

    private static RuntimeValue EvaluateSpace(int count)
    {
        return BuiltinStringHelpers.EvaluateSpace(count);
    }

    private string EvaluateStringFill(int count, string fill, TextLocation location)
    {
        if (count == 0)
        {
            return string.Empty;
        }

        return new string(RequireFillCharacter("STRING", fill, location), RuntimeStringLimits.Check(count));
    }

    private static RuntimeValue EvaluatePadLeftBuiltin(RuntimeValue value, int width)
    {
        var text = GetStringUnchecked(value);
        return text.Length >= width
            ? value
            : RuntimeValue.FromString(text.PadLeft(width));
    }

    private RuntimeValue EvaluatePadLeftFillBuiltin(RuntimeValue value, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("PADLEFT", fill, location);
        var text = GetStringUnchecked(value);
        return text.Length >= width
            ? value
            : RuntimeValue.FromString(text.PadLeft(width, fillCharacter));
    }

    private static RuntimeValue EvaluatePadRightBuiltin(RuntimeValue value, int width)
    {
        var text = GetStringUnchecked(value);
        return text.Length >= width
            ? value
            : RuntimeValue.FromString(text.PadRight(width));
    }

    private RuntimeValue EvaluatePadRightFillBuiltin(RuntimeValue value, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("PADRIGHT", fill, location);
        var text = GetStringUnchecked(value);
        return text.Length >= width
            ? value
            : RuntimeValue.FromString(text.PadRight(width, fillCharacter));
    }

    private static RuntimeValue EvaluateCenterBuiltin(RuntimeValue value, int width)
    {
        var text = GetStringUnchecked(value);
        if (text.Length >= width)
        {
            return value;
        }

        var padLeft = (width - text.Length) / 2;
        return RuntimeValue.FromString(
            text.PadLeft(text.Length + padLeft)
                .PadRight(width));
    }

    private RuntimeValue EvaluateCenterFillBuiltin(RuntimeValue value, int width, string fill, TextLocation location)
    {
        var fillCharacter = RequireFillCharacter("CENTER", fill, location);
        var text = GetStringUnchecked(value);
        if (text.Length >= width)
        {
            return value;
        }

        var padLeft = (width - text.Length) / 2;
        return RuntimeValue.FromString(
            text.PadLeft(text.Length + padLeft, fillCharacter)
                .PadRight(width, fillCharacter));
    }
}
