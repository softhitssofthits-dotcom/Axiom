namespace Axiom.Runtime;

/// <summary>
/// Живой адрес с явными зависимостями. При снимке копируется сам адрес,
/// а не только прочитанное через него значение.
/// </summary>
internal sealed class BytecodeAddressVariableSlot : BytecodeVariableSlot
{
    private readonly Func<BytecodeVariableSlot, BytecodeAddressStep[], RuntimeValue> _read;
    private readonly Action<BytecodeVariableSlot, BytecodeAddressStep[], RuntimeValue> _write;

    public BytecodeAddressVariableSlot(
        BytecodeVariableSlot baseSlot,
        BytecodeAddressStep[] steps,
        Func<BytecodeVariableSlot, BytecodeAddressStep[], RuntimeValue> read,
        Action<BytecodeVariableSlot, BytecodeAddressStep[], RuntimeValue> write)
        : base(() => read(baseSlot, steps), value => write(baseSlot, steps, value))
    {
        BaseSlot = baseSlot;
        Steps = steps;
        _read = read;
        _write = write;
    }

    public BytecodeVariableSlot BaseSlot { get; }

    public IReadOnlyList<BytecodeAddressStep> Steps { get; }

    public BytecodeAddressVariableSlot Rebind(BytecodeVariableSlot baseSlot, BytecodeAddressStep[] steps) =>
        new(baseSlot, steps, _read, _write);
}

internal readonly record struct BytecodeAddressStep(
    int FieldNameConstantIndex,
    BytecodeVariableSlot[] IndexSlots,
    bool IsDictionaryValue = false);
