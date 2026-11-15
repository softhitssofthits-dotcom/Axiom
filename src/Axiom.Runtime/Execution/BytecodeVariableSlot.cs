using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal class BytecodeVariableSlot
{
    // Для непосредственного INTEGER без внешнего получателя null обозначает значение _integer.
    // Немедленный DOUBLE помечается отдельным флагом: null в _value при нём означает _double.
    private RuntimeValue? _value;
    private int _integer;
    private double _double;
    private bool _immediateDouble;
    // Признак права на сброс слота следующей итерацией FOR EACH.
    private bool _reusableForEach;
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

    /// <summary>
    /// Свежий слот переменной FOR EACH или объявления внутри цикла. Слот может
    /// быть сброшен следующей итерацией, пока не ушёл в замыкание или BYREF:
    /// уход сбрасывает признак, и следующая итерация создаёт новый слот.
    /// </summary>
    public static BytecodeVariableSlot AdoptForEach(RuntimeValue value, int? fixedStringLength)
    {
        var slot = new BytecodeVariableSlot(value, fixedStringLength, isForEachItem: false, isForEachItemPending: true, adoptValue: true);
        slot._reusableForEach = true;
        return slot;
    }

    // Захват и передача по BYREF могут продлить жизнь ячейки за пределы кадра.
    // После выхода ячейку больше нельзя сбросить следующей итерацией FOR EACH.
    internal void MarkEscaped()
    {
        _reusableForEach = false;
    }

    /// <summary>
    /// Готовит слот FOR EACH к новой итерации с теми же флагами, что и свежий
    /// Adopt: значение по умолчанию и ожидание первого использования. Числовые
    /// типы сбрасываются в немедленную форму, составные — значением по
    /// умолчанию.
    /// </summary>
    internal bool TryResetForEach(TypeSymbol type, int? fixedStringLength)
    {
        if (!_reusableForEach) return false;

        if (ReferenceEquals(type, TypeSymbol.Integer))
        {
            _value = null;
            _integer = 0;
            _double = 0;
            _immediateDouble = false;
        }
        else if (ReferenceEquals(type, TypeSymbol.Double))
        {
            _value = null;
            _integer = 0;
            _double = 0;
            _immediateDouble = true;
        }
        else
        {
            _value = RuntimeValue.Default(type, fixedStringLength);
            _integer = 0;
            _double = 0;
            _immediateDouble = false;
        }

        IsForEachItem = false;
        IsForEachItemPending = true;
        return true;
    }

    /// <summary>Принимает непосредственный INTEGER в целочисленном режиме слота.</summary>
    public bool TryStoreImmediateInteger(int value)
    {
        if (_getter is not null || _value is not null || _immediateDouble)
        {
            return false;
        }

        _integer = value;
        return true;
    }

    /// <summary>Принимает непосредственный DOUBLE в режиме непосредственного DOUBLE.</summary>
    public bool TryStoreImmediateDouble(double value)
    {
        if (_getter is not null || _value is not null || !_immediateDouble)
        {
            return false;
        }

        _double = value;
        return true;
    }

    public bool TryGetImmediateInteger(out int value)
    {
        if (_getter is null && _value is null && !_immediateDouble)
        {
            value = _integer;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Читает непосредственный DOUBLE слота без создания объекта; прочие режимы дают отказ.</summary>
    public bool TryGetImmediateDouble(out double value)
    {
        if (_getter is null && _value is null && _immediateDouble)
        {
            value = _double;
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
        get => _getter is null
            ? _value ?? (_immediateDouble ? RuntimeValue.FromDouble(_double) : RuntimeValue.FromInteger(_integer))
            : _getter();
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
