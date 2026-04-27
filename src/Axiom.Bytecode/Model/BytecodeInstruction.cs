namespace Axiom.Bytecode;

public sealed class BytecodeInstruction
{
    public BytecodeInstruction(Opcode opcode, IReadOnlyList<int>? operands = null)
    {
        Opcode = opcode;
        Operands = operands ?? Array.Empty<int>();
    }

    public Opcode Opcode { get; }

    public IReadOnlyList<int> Operands { get; }

    public int OperandCount => Operands.Count;

    public int GetOperand(int index) => Operands[index];
}
