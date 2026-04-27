namespace Axiom.Bytecode;

public sealed class BytecodeBlockDebugRange
{
    public BytecodeBlockDebugRange(string name, int startInstructionIndex, int endInstructionIndexExclusive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        StartInstructionIndex = startInstructionIndex;
        EndInstructionIndexExclusive = endInstructionIndexExclusive;
    }

    public string Name { get; }

    public int StartInstructionIndex { get; }

    public int EndInstructionIndexExclusive { get; }
}
