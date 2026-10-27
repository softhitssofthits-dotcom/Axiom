using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

internal sealed class BytecodeEvaluationStack
{
    private RuntimeValue[] _items;
    private int[] _integers;
    private int _highWaterMark;

    public BytecodeEvaluationStack(int initialCapacity = 16)
    {
        if (initialCapacity < 1)
        {
            initialCapacity = 1;
        }

        _items = new RuntimeValue[initialCapacity];
        _integers = new int[initialCapacity];
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
        _integers[Count++] = value;
        if (Count > _highWaterMark) _highWaterMark = Count;
    }

    public bool TryGetIntegerOperands(out int left, out int right)
    {
        left = right = 0;
        if (Count < 2) return false;
        var a = _items[Count - 2];
        var b = _items[Count - 1];
        if (a is not null && b is not null) return false;
        if (a is not null && a.Type != Axiom.Semantics.TypeSymbol.Integer) return false;
        if (b is not null && b.Type != Axiom.Semantics.TypeSymbol.Integer) return false;
        left = a is null ? _integers[Count - 2] : a.AsInteger();
        right = b is null ? _integers[Count - 1] : b.AsInteger();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetImmediateInteger(out int value)
    {
        var index = Count - 1;
        if (index >= 0 && _items[index] is null)
        {
            value = _integers[index];
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceImmediateInteger(int value) => _integers[Count - 1] = value;

    public void DiscardIntegerOperands()
    {
        Count -= 2;
    }

    public void ReplaceIntegerOperands(int result)
    {
        Count -= 2;
        PushInteger(result);
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
        return _items[index] ?? RuntimeValue.FromInteger(_integers[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue PeekUnchecked(int depthFromTop = 0)
    {
        var index = Count - 1 - depthFromTop;
        return _items[index] ?? RuntimeValue.FromInteger(_integers[index]);
    }

    public RuntimeValue[] ToBottomToTopArray()
    {
        if (Count == 0)
        {
            return Array.Empty<RuntimeValue>();
        }

        var copy = new RuntimeValue[Count];
        for (var index = 0; index < Count; index++)
            copy[index] = _items[index] ?? RuntimeValue.FromInteger(_integers[index]);
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
        Array.Resize(ref _integers, newCapacity);
    }
}
