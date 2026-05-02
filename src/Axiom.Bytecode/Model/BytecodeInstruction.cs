using System.Runtime.CompilerServices;

namespace Axiom.Bytecode;

public sealed class BytecodeInstruction
{
    private readonly int[] _operands;

    public BytecodeInstruction(Opcode opcode, IReadOnlyList<int>? operands = null)
    {
        Opcode = opcode;
        _operands = operands switch
        {
            null => Array.Empty<int>(),
            int[] array => array,
            _ => operands.ToArray()
        };
    }

    public Opcode Opcode { get; }

    public IReadOnlyList<int> Operands => _operands;

    public int OperandCount => _operands.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOperand(int index) => _operands[index];
}
