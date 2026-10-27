using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
    private ElementStorage CreateDefaultElements(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var length = CalculateLength(lowerBounds, upperBounds);
        var elements = CreateElementStorage(length);

        for (var index = 0; index < elements.Length; index++)
        {
            elements[index] = RuntimeValue.Default(ElementType, FixedStringLength);
        }

        return elements;
    }

    private ElementStorage CreateElementStorage(int length)
    {
        try
        {
            return new ElementStorage(length, ElementType == TypeSymbol.Integer);
        }
        catch (OutOfMemoryException)
        {
            throw new ArrayAllocationFailureException(
                $"Не удалось выделить массив из {length} элементов: недостаточно памяти.");
        }
    }

    private static int ValidateElementCount(long length)
    {
        if (length > MaxElementCount)
        {
            throw new ArrayAllocationFailureException(
                $"Не удалось выделить массив: число элементов превышает предел {MaxElementCount}.");
        }

        return (int)length;
    }

    private int GrowCapacity(int minimum)
    {
        var grown = _capacity <= 0 ? 4L : (long)_capacity * 2;

        if (grown < minimum)
        {
            grown = minimum;
        }

        if (grown > MaxElementCount)
        {
            grown = MaxElementCount;
        }

        return (int)grown;
    }

    private void AttachStorage(ElementStorage elements)
    {
        InvalidateStorageLifetime();
        _elements = elements;
        _capacity = elements.Length;
        _shapeRevision = unchecked(_shapeRevision + 1);
        _storageLifetime = new StorageLifetime(_shapeRevision);
    }

    private void InvalidateStorageLifetime()
    {
        if (_storageLifetime is { } lifetime)
        {
            lifetime.Invalidated = true;
        }
    }

    private void RenewStorageLifetime()
    {
        InvalidateStorageLifetime();
        _shapeRevision = unchecked(_shapeRevision + 1);
        _storageLifetime = new StorageLifetime(_shapeRevision);
    }

    private void EnsureAllocated()
    {
        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }

        if (_storageLifetime is { Invalidated: true })
        {
            throw new ExpiredArrayViewException();
        }
    }

    /// <summary>
    /// Общий признак жизни хранилища: корень заменяет его при выделении,
    /// перевыделении и освобождении, а представления сохраняют ссылку на прежний.
    /// </summary>
    private sealed class StorageLifetime
    {
        public StorageLifetime(long revision = 0)
        {
            Revision = revision;
        }

        public bool Invalidated;

        public long Revision;
    }

    private void EnsureResizableRoot()
    {
        if (IsView)
        {
            throw new InvalidOperationException("Array view cannot be resized or deallocated.");
        }
    }
    private sealed class ElementStorage
    {
        private readonly int[]? _integers;
        private readonly RuntimeValue[]? _values;
        public ElementStorage(int length, bool integers)
        {
            if (integers) _integers = new int[length];
            else _values = new RuntimeValue[length];
        }
        public int Length => _integers?.Length ?? _values!.Length;
        public int ReadInteger(int index) => _integers![index];
        public void WriteInteger(int index, int value) => _integers![index] = value;
        public void Clear(int index)
        {
            if (_integers is not null) _integers[index] = 0;
            else _values![index] = RuntimeValue.Void;
        }
        public RuntimeValue this[int index]
        {
            get => _integers is null ? _values![index] : RuntimeValue.FromInteger(_integers[index]);
            set { if (_integers is null) _values![index] = value; else _integers[index] = value.AsInteger(); }
        }
        public void CopyTo(int sourceIndex, RuntimeValue[] target, int targetIndex, int count)
        {
            if (_values is not null) Array.Copy(_values, sourceIndex, target, targetIndex, count);
            else for (var i = 0; i < count; i++) target[targetIndex + i] = RuntimeValue.FromInteger(_integers![sourceIndex + i]);
        }
        public static void Copy(ElementStorage source, int sourceIndex, ElementStorage target, int targetIndex, int count)
        {
            if (source._integers is not null && target._integers is not null) Array.Copy(source._integers, sourceIndex, target._integers, targetIndex, count);
            else if (source._values is not null && target._values is not null) Array.Copy(source._values, sourceIndex, target._values, targetIndex, count);
            else for (var i = 0; i < count; i++) target[targetIndex + i] = source[sourceIndex + i];
        }
    }

}
