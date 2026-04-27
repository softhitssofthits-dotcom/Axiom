namespace Axiom.Bytecode;

public sealed class ConstantPool
{
    public ConstantPool(IReadOnlyList<BytecodeConstant> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
    }

    public IReadOnlyList<BytecodeConstant> Entries { get; }

    public int Count => Entries.Count;

    public BytecodeConstant this[int index] => Entries[index];
}
