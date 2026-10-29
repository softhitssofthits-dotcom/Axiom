using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

internal sealed class BytecodeEvaluationStack
{
    private RuntimeValue[] _items;
    private int[]? _integers;
    private long[]? _longs;
    private int _highWaterMark;

    public BytecodeEvaluationStack(int initialCapacity = 16)
    {
        if (initialCapacity < 1)
        {
            initialCapacity = 1;
        }

        _items = new RuntimeValue[initialCapacity];
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
        if ((uint)Count >= (uint)_items.Length)
        {
            Grow();
        }

        // Вычисленный операнд больше не принадлежит изменяемому слоту.
        // Последующий вызов может дописать его накопитель через BYREF/замыкание.
        var snapshot = value.RequiresCopyForStorage ? value.CopyForStorage() : value;
        _items[Count++] = snapshot;
        if (Count > _highWaterMark)
        {
            _highWaterMark = Count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushInteger(int value)
    {
        if ((uint)Count >= (uint)_items.Length) Grow();
        _items[Count] = null!;
        (_integers ??= new int[_items.Length])[Count++] = value;
        if (Count > _highWaterMark) _highWaterMark = Count;
    }

    /// <summary>
    /// Помещает непосредственный LONG без создания RuntimeValue.
    /// Слот отмечается маркером; значение покидает стек только
    /// материализованным через PopUnchecked/PeekUnchecked/снимки.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushLong(long value)
    {
        if ((uint)Count >= (uint)_items.Length) Grow();
        _items[Count] = RuntimeValue.ImmediateLongMarker;
        (_longs ??= new long[_items.Length])[Count++] = value;
        if (Count > _highWaterMark) _highWaterMark = Count;
    }

    public bool TryGetIntegerOperands(out int left, out int right)
    {
        left = right = 0;
        if (Count < 2) return false;
        var a = _items[Count - 2];
        var b = _items[Count - 1];
        // Материализованный INTEGER-объект участвует в прямом пути наравне
        // с непосредственным значением: результат остаётся распакованным.
        if (a is not null && a.Type != Axiom.Semantics.TypeSymbol.Integer) return false;
        if (b is not null && b.Type != Axiom.Semantics.TypeSymbol.Integer) return false;
        left = a is null ? _integers![Count - 2] : a.AsInteger();
        right = b is null ? _integers![Count - 1] : b.AsInteger();
        return true;
    }

    /// <summary>
    /// Читает два операнда как LONG, когда оба представлены LONG-значением
    /// (непосредственным или материализованным). INTEGER и прочие типы
    /// отвергаются: смешанные сочетания обрабатывает прежний путь.
    /// </summary>
    public bool TryGetLongOperands(out long left, out long right)
    {
        left = right = 0;
        if (Count < 2) return false;
        if (!TryReadLongSlot(Count - 2, out left)) return false;
        if (!TryReadLongSlot(Count - 1, out right)) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryReadLongSlot(int index, out long value)
    {
        var item = _items[index];
        if (ReferenceEquals(item, RuntimeValue.ImmediateLongMarker))
        {
            value = _longs![index];
            return true;
        }

        if (item is null || item.Type != Axiom.Semantics.TypeSymbol.Long)
        {
            value = 0;
            return false;
        }

        value = (long)item.Value!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateInteger(out int value, int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        if (index >= 0 && _items[index] is null)
        {
            value = _integers![index];
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateLong(out long value, int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        if (index >= 0 && ReferenceEquals(_items[index], RuntimeValue.ImmediateLongMarker))
        {
            value = _longs![index];
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Читает LONG-операнд (непосредственный или материализованный) на
    /// заданной глубине без создания объекта. Прочие типы отвергаются.
    /// </summary>
    public bool TryReadLong(int depthFromTop, out long value)
    {
        var index = Count - 1 - depthFromTop;
        if (index < 0)
        {
            value = 0;
            return false;
        }

        return TryReadLongSlot(index, out value);
    }

    /// <summary>
    /// Читает INTEGER-операнд (непосредственный или материализованный) на
    /// заданной глубине без создания объекта. Прочие типы отвергаются.
    /// </summary>
    public bool TryReadInteger(int depthFromTop, out int value)
    {
        value = 0;
        var index = Count - 1 - depthFromTop;
        if (index < 0)
        {
            return false;
        }

        var item = _items[index];
        if (item is null)
        {
            value = _integers![index];
            return true;
        }

        if (ReferenceEquals(item, RuntimeValue.ImmediateLongMarker)
            || item.Type != Axiom.Semantics.TypeSymbol.Integer)
        {
            return false;
        }

        value = (int)item.Value!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceImmediateInteger(int value) => _integers![Count - 1] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceImmediateLong(long value) => _longs![Count - 1] = value;

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
        var index = --Count;
        return MaterializeSlot(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue PeekUnchecked(int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        return MaterializeSlot(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RuntimeValue MaterializeSlot(int index)
    {
        var item = _items[index];
        if (item is null)
        {
            return RuntimeValue.FromInteger(_integers![index]);
        }

        return ReferenceEquals(item, RuntimeValue.ImmediateLongMarker)
            ? RuntimeValue.FromLong(_longs![index])
            : item;
    }

    public RuntimeValue[] ToBottomToTopArray()
    {
        if (Count == 0)
        {
            return Array.Empty<RuntimeValue>();
        }

        var copy = new RuntimeValue[Count];
        for (var index = 0; index < Count; index++)
            copy[index] = MaterializeSlot(index);
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
        if (_integers is not null) Array.Resize(ref _integers, newCapacity);
        if (_longs is not null) Array.Resize(ref _longs, newCapacity);
    }
}
