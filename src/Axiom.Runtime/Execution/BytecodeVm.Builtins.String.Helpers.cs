using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private int RequirePositiveCount(string functionName, string argumentName, int value, TextLocation location)
    {
        if (value == 0)
        {
            ReportBuiltInArgumentError(functionName, argumentName, "должен быть больше 0.", location);
            throw new RuntimeFailureException();
        }

        if (value < 0)
        {
            ReportBuiltInArgumentError(functionName, argumentName, "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        return value;
    }

    private int RequireNonNegativeCount(string functionName, string argumentName, int value, TextLocation location)
    {
        if (value < 0)
        {
            ReportBuiltInArgumentError(functionName, argumentName, "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        return argumentName == "width" ? RuntimeStringLimits.Check(value) : value;
    }

    private int RequirePositiveStart(string functionName, int value, TextLocation location)
    {
        if (value < 1)
        {
            ReportBuiltInArgumentError(functionName, "start", "должен быть больше или равен 1.", location);
            throw new RuntimeFailureException();
        }

        return value;
    }

    private char RequireFillCharacter(string functionName, string fill, TextLocation location)
    {
        if (fill.Length == 0)
        {
            ReportBuiltInArgumentError(functionName, "fill", "не может быть пустой строкой.", location);
            throw new RuntimeFailureException();
        }

        return fill[0];
    }

    private int RequireValidUnicodeCodePoint(string functionName, string argumentName, int value, TextLocation location)
    {
        if (!System.Text.Rune.IsValid(value))
        {
            ReportBuiltInArgumentError(functionName, argumentName, "должен быть допустимым кодом символа Unicode.", location);
            throw new RuntimeFailureException();
        }

        return value;
    }
}
