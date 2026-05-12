using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal class BytecodeVariableSlot
{
    private RuntimeValue _value;
    private readonly Func<RuntimeValue>? _getter;
    private readonly Action<RuntimeValue>? _setter;

    public BytecodeVariableSlot(RuntimeValue value, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        FixedStringLength = fixedStringLength;
        _value = fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
            ? value
            : NormalizeForStorage(value, fixedStringLength);
    }

    public BytecodeVariableSlot(Func<RuntimeValue> getter, Action<RuntimeValue> setter, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        FixedStringLength = fixedStringLength;
        _getter = getter;
        _setter = setter;
        _value = RuntimeValue.Void;
    }

    public int? FixedStringLength { get; }

    public virtual RuntimeValue Value
    {
        get => _getter is null ? _value : _getter();
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var normalizedValue = FixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
                ? value
                : NormalizeForStorage(value, FixedStringLength);

            if (_setter is null)
            {
                _value = normalizedValue;
                return;
            }

            _setter(normalizedValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = FixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
            ? value
            : NormalizeForStorage(value, FixedStringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreAppendStringValue(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedValue = FixedStringLength is null
            ? value
            : NormalizeForStorage(value, FixedStringLength);

        if (_setter is null)
        {
            _value = normalizedValue;
            return;
        }

        _setter(normalizedValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RuntimeValue NormalizeForStorage(RuntimeValue value, int? fixedStringLength)
    {
        if (fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage)
        {
            return value;
        }

        return value.CopyForStorage(fixedStringLength);
    }
}
