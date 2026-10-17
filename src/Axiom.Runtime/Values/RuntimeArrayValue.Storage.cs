using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
    private RuntimeValue[] CreateDefaultElements(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var length = CalculateLength(lowerBounds, upperBounds);
        var elements = CreateElementStorage(length);

        for (var index = 0; index < elements.Length; index++)
        {
            elements[index] = RuntimeValue.Default(ElementType, FixedStringLength);
        }

        return elements;
    }

    private static RuntimeValue[] CreateElementStorage(int length)
    {
        try
        {
            return new RuntimeValue[length];
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

    private void AttachStorage(RuntimeValue[] elements)
    {
        InvalidateStorageLifetime();
        _elements = elements;
        _capacity = elements.Length;
        _storageLifetime = new StorageLifetime();
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
        _storageLifetime = new StorageLifetime();
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
        public bool Invalidated;
    }

    private void EnsureResizableRoot()
    {
        if (IsView)
        {
            throw new InvalidOperationException("Array view cannot be resized or deallocated.");
        }
    }
}
