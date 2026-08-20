using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

internal sealed class BytecodeEvaluationStack
{
    private RuntimeValue[] _items;
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

        _items[Count++] = value;
        if (Count > _highWaterMark)
        {
            _highWaterMark = Count;
        }
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
        return _items[--Count];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue PeekUnchecked(int depthFromTop = 0)
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
        Array.Copy(_items, copy, Count);
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
