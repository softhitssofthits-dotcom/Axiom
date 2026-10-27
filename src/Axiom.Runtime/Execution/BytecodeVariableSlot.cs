using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal class BytecodeVariableSlot
{
    // Для непосредственного INTEGER без внешнего получателя null обозначает значение _integer.
    private RuntimeValue? _value;
    private int _integer;
    private readonly Func<RuntimeValue>? _getter;
    private readonly Action<RuntimeValue>? _setter;

    public BytecodeVariableSlot(
        RuntimeValue value,
        int? fixedStringLength = null,
        bool isForEachItem = false,
        bool isForEachItemPending = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        FixedStringLength = fixedStringLength;
        IsForEachItem = isForEachItem;
        IsForEachItemPending = isForEachItemPending;
        _value = fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
            ? value
            : NormalizeForStorage(value, fixedStringLength);
    }

    public BytecodeVariableSlot(
        Func<RuntimeValue> getter,
        Action<RuntimeValue> setter,
        int? fixedStringLength = null,
        bool isForEachItem = false)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        FixedStringLength = fixedStringLength;
        IsForEachItem = isForEachItem;
        _getter = getter;
        _setter = setter;
        _value = RuntimeValue.Void;
    }

    private BytecodeVariableSlot(
        RuntimeValue value,
        int? fixedStringLength,
        bool isForEachItem,
        bool isForEachItemPending,
        bool adoptValue)
    {
        ArgumentNullException.ThrowIfNull(value);
        FixedStringLength = fixedStringLength;
        IsForEachItem = isForEachItem;
        IsForEachItemPending = isForEachItemPending;
        _value = value;
        _ = adoptValue;
    }

    /// <summary>
    /// Слот, который ссылается на уже подготовленное значение без копирования.
    /// Нужен для видимых переменных и восстановления снимков: копия записи здесь
    /// разорвала бы связь со значением, которое видят другие участники состояния.
    /// </summary>
    public static BytecodeVariableSlot Adopt(
        RuntimeValue value,
        int? fixedStringLength = null,
        bool isForEachItem = false,
        bool isForEachItemPending = false)
    {
        return new BytecodeVariableSlot(value, fixedStringLength, isForEachItem, isForEachItemPending, adoptValue: true);
    }

    public static BytecodeVariableSlot AdoptInteger(int value)
    {
        var slot = new BytecodeVariableSlot(RuntimeValue.Void, null, false, false, adoptValue: true);
        slot._value = null;
        slot._integer = value;
        return slot;
    }

    public bool TryGetImmediateInteger(out int value)
    {
        if (_getter is null && _value is null)
        {
            value = _integer;
            return true;
        }

        value = 0;
        return false;
    }

    public int? FixedStringLength { get; }

    public bool IsForEachItem { get; internal set; }

    public bool IsForEachItemPending { get; internal set; }

    public virtual RuntimeValue Value
    {
        get => _getter is null ? _value ?? RuntimeValue.FromInteger(_integer) : _getter();
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
