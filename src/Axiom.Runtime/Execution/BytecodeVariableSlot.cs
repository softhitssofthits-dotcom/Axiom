namespace Axiom.Runtime;

internal sealed class BytecodeVariableSlot
{
    public BytecodeVariableSlot(RuntimeValue value, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        FixedStringLength = fixedStringLength;
        Value = value.CopyForStorage(fixedStringLength);
    }

    public int? FixedStringLength { get; }

    public RuntimeValue Value { get; set; }

    public void Store(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value.CopyForStorage(FixedStringLength);
    }
}
