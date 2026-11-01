using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

/// <summary>
/// Стек вычисления VM на непрерывном массиве тегированных значений
/// <see cref="VmValue"/>. Числовые виды остаются распакованными до границы:
/// материализация <see cref="RuntimeValue"/> происходит при извлечении и в
/// снимках. Представление публичного договора не меняет.
/// </summary>
internal sealed class BytecodeEvaluationStack
{
    private VmValue[] _items;
    private int _highWaterMark;

    public BytecodeEvaluationStack(int initialCapacity = 16)
    {
        if (initialCapacity < 1)
        {
            initialCapacity = 1;
        }

        _items = new VmValue[initialCapacity];
    }

    public int Count { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        PushUnchecked(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushUnchecked(RuntimeValue value)
    {
        // Вычисленный операнд больше не принадлежит изменяемому слоту.
        // Последующий вызов может дописать его накопитель через BYREF/замыкание.
        if (value.RequiresCopyForStorage)
        {
            value = value.CopyForStorage();
        }

        PushVmValue(VmValue.FromRuntime(value));
    }

    /// <summary>Помещает внутреннее значение без материализации; путь кадров и прямых операций.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushVmValue(VmValue value)
    {
        if ((uint)Count >= (uint)_items.Length)
        {
            Grow();
        }

        _items[Count++] = value;
        if (Count > _highWaterMark)
        {
            _highWaterMark = Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushInteger(int value) => PushVmValue(VmValue.FromInteger(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushLong(long value) => PushVmValue(VmValue.FromLong(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushDouble(double value) => PushVmValue(VmValue.FromDouble(value));

    public bool TryGetIntegerOperands(out int left, out int right)
    {
        var index = Count - 2;
        if (index < 0
            || _items[index].Kind != VmValueKind.Integer
            || _items[index + 1].Kind != VmValueKind.Integer)
        {
            left = right = 0;
            return false;
        }

        left = _items[index].Integer;
        right = _items[index + 1].Integer;
        return true;
    }

    /// <summary>
    /// Читает два операнда как LONG, когда оба представлены LONG-значением.
    /// INTEGER и прочие типы отвергаются: смешанные сочетания обрабатывает
    /// прежний путь.
    /// </summary>
    public bool TryGetLongOperands(out long left, out long right)
    {
        var index = Count - 2;
        if (index < 0
            || _items[index].Kind != VmValueKind.Long
            || _items[index + 1].Kind != VmValueKind.Long)
        {
            left = right = 0;
            return false;
        }

        left = _items[index].Long;
        right = _items[index + 1].Long;
        return true;
    }

    /// <summary>
    /// Читает два операнда как DOUBLE, когда оба представлены DOUBLE-значением.
    /// Прочие типы отвергаются: смешанные сочетания обрабатывает прежний путь.
    /// </summary>
    public bool TryGetDoubleOperands(out double left, out double right)
    {
        var index = Count - 2;
        if (index < 0
            || _items[index].Kind != VmValueKind.Double
            || _items[index + 1].Kind != VmValueKind.Double)
        {
            left = right = 0;
            return false;
        }

        left = _items[index].Double;
        right = _items[index + 1].Double;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateInteger(out int value, int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        if (index >= 0 && _items[index].Kind == VmValueKind.Integer)
        {
            value = _items[index].Integer;
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateLong(out long value, int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        if (index >= 0 && _items[index].Kind == VmValueKind.Long)
        {
            value = _items[index].Long;
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateDouble(out double value, int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        if (index >= 0 && _items[index].Kind == VmValueKind.Double)
        {
            value = _items[index].Double;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Читает LONG-операнд на заданной глубине без создания объекта; прочие типы отвергаются.</summary>
    public bool TryReadLong(int depthFromTop, out long value)
    {
        var index = Count - 1 - depthFromTop;
        if (index < 0 || _items[index].Kind != VmValueKind.Long)
        {
            value = 0;
            return false;
        }

        value = _items[index].Long;
        return true;
    }

    /// <summary>Читает INTEGER-операнд на заданной глубине без создания объекта; прочие типы отвергаются.</summary>
    public bool TryReadInteger(int depthFromTop, out int value)
    {
        var index = Count - 1 - depthFromTop;
        if (index < 0 || _items[index].Kind != VmValueKind.Integer)
        {
            value = 0;
            return false;
        }

        value = _items[index].Integer;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceImmediateInteger(int value) => _items[Count - 1].Integer = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceImmediateLong(long value) => _items[Count - 1].Long = value;

    /// <summary>Заменяет верхний слот неупакованным INTEGER со сменой вида значения.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceTopWithInteger(int value) => _items[Count - 1] = VmValue.FromInteger(value);

    /// <summary>Заменяет верхний слот неупакованным LONG со сменой вида значения.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceTopWithLong(long value) => _items[Count - 1] = VmValue.FromLong(value);

    public void DiscardIntegerOperands()
    {
        Count -= 2;
    }

    public void DiscardTop(int count) => Count -= count;

    public void ReplaceIntegerOperands(int result)
    {
        Count -= 2;
        PushInteger(result);
    }

    public void ReplaceLongOperands(long result)
    {
        Count -= 2;
        PushLong(result);
    }

    /// <summary>Заменяет два проверенных DOUBLE-операнда одним непосредственным DOUBLE.</summary>
    public void ReplaceDoubleOperands(double result)
    {
        Count -= 2;
        PushDouble(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue Pop()
    {
        if (Count == 0)
        {
            throw new EvaluationStackUnderflowException("Стек вычисления опустел.");
        }

        return PopUnchecked();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue PopUnchecked()
    {
        return _items[--Count].Materialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue PeekUnchecked(int depthFromTop = 0)
    {
        return _items[Count - 1 - depthFromTop].Materialize();
    }

    /// <summary>Читает слот как внутреннее значение без материализации; для прямых операций VM.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VmValue PeekVmValue(int depthFromTop = 0)
    {
        return _items[Count - 1 - depthFromTop];
    }

    public RuntimeValue[] ToBottomToTopArray()
    {
        if (Count == 0)
        {
            return Array.Empty<RuntimeValue>();
        }

        var copy = new RuntimeValue[Count];
        for (var index = 0; index < Count; index++)
        {
            copy[index] = _items[index].Materialize();
        }

        return copy;
    }

    public void Clear()
    {
        if (_highWaterMark == 0)
        {
            return;
        }

        Array.Clear(_items, 0, _highWaterMark);
        Count = 0;
        _highWaterMark = 0;
    }

    private void Grow()
    {
        var newCapacity = _items.Length < 1024
            ? _items.Length * 2
            : _items.Length + 1024;
        Array.Resize(ref _items, newCapacity);
    }
}
