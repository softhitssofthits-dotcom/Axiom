namespace Axiom.Bytecode;

public sealed class BytecodeConstant
{
    public BytecodeConstant(BytecodeConstantKind kind, object? value)
    {
        Kind = kind;
        Value = value;
    }

    public BytecodeConstantKind Kind { get; }

    public object? Value { get; }
}
