using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal class BytecodeVariableSlot
{
    // Для непосредственного INTEGER без внешнего получателя null обозначает значение _integer.
    private RuntimeValue? _value;
    private int _integer;
    private bool _reusableInteger;
    private bool _reusableValue;
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
        slot._reusableInteger = true;
        return slot;
    }

    /// <summary>
    /// Слот аргумента с материализованным значением, который можно вернуть
    /// в свободный кадр после очистки значения. Захват и передача по BYREF
    /// снимают право повторного использования так же, как у INTEGER-ячеек.
    /// </summary>
    public static BytecodeVariableSlot AdoptReusable(RuntimeValue value)
    {
        var slot = new BytecodeVariableSlot(value, null, false, false, adoptValue: true);
        slot._reusableValue = true;
        return slot;
    }

    // Захват и передача по BYREF могут продлить жизнь ячейки за пределы кадра.
    // После выхода ячейку больше нельзя возвращать в пул вместе с кадром.
    internal void MarkEscaped()
    {
        _reusableInteger = false;
        _reusableValue = false;
    }

    internal bool PrepareForReuse()
    {
        if (_reusableInteger)
        {
            // Не удерживать материализованное значение между вызовами.
            _value = null;
            _integer = 0;
            return true;
        }

        if (!_reusableValue) return false;
        _value = null;
        IsForEachItem = false;
        IsForEachItemPending = false;
        return true;
    }

    internal bool TryResetInteger(int value)
    {
        if (!_reusableInteger) return false;
        _value = null;
        _integer = value;
        IsForEachItem = false;
        IsForEachItemPending = false;
        return true;
    }

    internal bool TryResetValue(RuntimeValue value)
    {
        if (!_reusableValue) return false;
        // Как и Adopt, сохраняем значение без копии: при подготовке аргумента
        // снимок уже сделан тем, кто его поместил в стек.
        _value = value;
        IsForEachItem = false;
        IsForEachItemPending = false;
        return true;
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
