using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
    // Представление при копировании материализуется в независимый корень
    // с теми же видимыми границами; служебный снимок сохраняет представления.
    internal RuntimeArrayValue CreateEmptyForValueCopy(int? fixedStringLength)
    {
        var copy = new RuntimeArrayValue(ElementType, fixedStringLength ?? FixedStringLength, Rank);
        if (IsAllocated)
        {
            EnsureAllocated();
            copy.Allocate(_lowerBounds!, _upperBounds!);
        }
        return copy;
    }

    // A root can retain its physical buffer after a length change while older
    // views still point at that buffer with an invalidated lifetime. Snapshot
    // identity follows the lifetime generation, not only the array buffer.
    internal object SnapshotStorageIdentity => (object?)_storageLifetime ?? (object?)_elements ?? this;

    /// <summary>
    /// Создаёт корневое хранилище снимка для массива или его представления.
    /// Представления одного массива используют общую физическую область.
    /// </summary>
    internal RuntimeArrayValue CreateEmptyStorageForSnapshot()
    {
        if (!IsAllocated)
        {
            return CreateEmptyForSnapshot();
        }

        var lowerBounds = _storageLowerBounds!.ToArray();
        var upperBounds = _storageUpperBounds!.ToArray();
        var clone = new RuntimeArrayValue(ElementType, FixedStringLength, lowerBounds.Length)
        {
            _lowerBounds = lowerBounds,
            _upperBounds = upperBounds,
            _storageLowerBounds = lowerBounds,
            _storageUpperBounds = upperBounds,
            _fixedStorageIndices = CreateEmptyFixedStorageIndices(lowerBounds.Length),
            _visibleStorageDimensions = CreateIdentityStorageDimensions(lowerBounds.Length),
            _elements = CreateDefaultElements(lowerBounds, upperBounds),
            _capacity = CalculateLength(lowerBounds, upperBounds),
            _isView = false,
            _shapeRevision = _storageLifetime?.Revision ?? _shapeRevision,
            _storageLifetime = new StorageLifetime(_storageLifetime?.Revision ?? _shapeRevision)
        };
        clone._storageLifetime!.Invalidated = _storageLifetime is { Invalidated: true };
        clone.RefreshShapeMetadata();
        return clone;
    }

    internal RuntimeArrayValue CreateViewForSnapshot(RuntimeArrayValue snapshotStorage)
    {
        ArgumentNullException.ThrowIfNull(snapshotStorage);
        if (!IsAllocated || !_isView || !snapshotStorage.IsAllocated)
        {
            throw new InvalidOperationException("A valid array view and allocated snapshot storage are required.");
        }

        return new RuntimeArrayValue(
            ElementType,
            FixedStringLength,
            _lowerBounds!,
            _upperBounds!,
            snapshotStorage._elements!,
            snapshotStorage._storageLowerBounds!,
            snapshotStorage._storageUpperBounds!,
            _fixedStorageIndices!,
            _visibleStorageDimensions!,
            snapshotStorage._storageLifetime!);
    }

    internal void FillStorageSnapshotInto(RuntimeArrayValue target, Func<RuntimeValue, RuntimeValue> snapshotElement)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(snapshotElement);
        if (_elements is null || target._elements is null)
        {
            return;
        }

        // Copy the physical backing storage directly. A stale view deliberately
        // fails normal reads and iteration, but its bytes still need to be copied
        // into an invalidated snapshot so a debugger can preserve the same error.
        var length = Math.Min(_elements.Length, target._elements.Length);
        if ((ElementType == TypeSymbol.Integer || ElementType == TypeSymbol.Long)
            && target.ElementType == ElementType)
        {
            ElementStorage.Copy(_elements, 0, target._elements, 0, length);
            return;
        }
        for (var index = 0; index < length; index++)
        {
            target._elements[index] = snapshotElement(_elements[index]);
        }
    }

    public RuntimeArrayValue Clone(int? fixedStringLengthOverride = null)
    {
        if (ElementType is UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol)
        {
            var type = (ArrayTypeSymbol)ArrayTypeSymbol.Create(ElementType, Rank);
            return RuntimeValueCopier.Copy(RuntimeValue.FromArray(type, this), fixedStringLengthOverride).AsArray();
        }

        var clone = new RuntimeArrayValue(ElementType, fixedStringLengthOverride ?? FixedStringLength, Rank);

        if (!IsAllocated)
        {
            return clone;
        }

        clone._lowerBounds = _lowerBounds!.ToArray();
        clone._upperBounds = _upperBounds!.ToArray();
        clone._storageLowerBounds = clone._lowerBounds;
        clone._storageUpperBounds = clone._upperBounds;
        clone._fixedStorageIndices = CreateEmptyFixedStorageIndices(clone.Rank);
        clone._visibleStorageDimensions = CreateIdentityStorageDimensions(clone.Rank);
        clone.AttachStorage(clone.CreateDefaultElements(clone._storageLowerBounds, clone._storageUpperBounds));
        clone.RefreshShapeMetadata();

        if (RequiresElementCopy(clone.FixedStringLength) || !TryCopyVisibleValuesToClone(clone))
        {
            clone.CopyVisibleValuesFrom(this);
        }

        return clone;
    }

    /// <summary>
    /// Готовит пустой массив той же формы для снимка состояния. Заполнение
    /// выполняется отдельно через <see cref="FillForSnapshot"/>, чтобы
    /// вызывающая сторона успела зарегистрировать объект до обхода элементов.
    /// </summary>
    internal RuntimeArrayValue CreateEmptyForSnapshot()
    {
        var clone = new RuntimeArrayValue(ElementType, FixedStringLength, Rank);
        clone._shapeRevision = _storageLifetime?.Revision ?? _shapeRevision;

        if (!IsAllocated)
        {
            return clone;
        }

        clone._lowerBounds = _lowerBounds!.ToArray();
        clone._upperBounds = _upperBounds!.ToArray();
        clone._storageLowerBounds = clone._lowerBounds;
        clone._storageUpperBounds = clone._upperBounds;
        clone._fixedStorageIndices = CreateEmptyFixedStorageIndices(clone.Rank);
        clone._visibleStorageDimensions = CreateIdentityStorageDimensions(clone.Rank);
        clone.AttachStorage(clone.CreateDefaultElements(clone._storageLowerBounds, clone._storageUpperBounds));
        clone.RefreshShapeMetadata();
        clone._shapeRevision = _storageLifetime?.Revision ?? _shapeRevision;
        clone._storageLifetime!.Revision = clone._shapeRevision;
        clone._storageLifetime.Invalidated = _storageLifetime is { Invalidated: true };
        return clone;
    }

    /// <summary>
    /// Заполняет заготовку снимка независимыми значениями элементов этого массива
    /// без правил присваивания.
    /// </summary>
    internal void FillSnapshotInto(RuntimeArrayValue target, Func<RuntimeValue, RuntimeValue> snapshotElement,
        Action<int> beforeNumericCopy)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(snapshotElement);
        ArgumentNullException.ThrowIfNull(beforeNumericCopy);

        if (!IsAllocated
            || _storageLifetime is { Invalidated: true }
            || !target.IsAllocated
            || target.Length != Length)
        {
            return;
        }

        if (TryCopyNumericValuesTo(target, beforeNumericCopy))
        {
            return;
        }

        ForEachVisibleIndexSetAscending(indices =>
        {
            target._elements![target.GetLinearIndex(indices)] = snapshotElement(GetValue(indices));
        });
    }

    /// <summary>
    /// Прямое копирование числового буфера в независимую заготовку копии.
    /// Применимо только к одинаковым INTEGER/INTEGER или LONG/LONG массивам
    /// ранга 1, видимая область которых непрерывна в физическом хранилище;
    /// цель должна быть независимым корнем. Преобразователь элементов для
    /// этих типов не меняет скалярное значение, поэтому буфер переносится
    /// как есть; прочие случаи остаются поэлементному обходу.
    /// </summary>
    private bool TryCopyNumericValuesTo(RuntimeArrayValue target, Action<int> beforeNumericCopy)
    {
        if ((ElementType != TypeSymbol.Integer && ElementType != TypeSymbol.Long)
            || target.ElementType != ElementType
            || Rank != 1
            || target.Rank != 1
            || target.IsView
            || !IsAllocated
            || !target.IsAllocated
            || target.Length != Length
            || Length == 0)
        {
            return false;
        }

        if (!TryGetLinearIndex1(LowerBound, out var first)
            || !TryGetLinearIndex1(UpperBound, out var last)
            || last - first + 1 != Length)
        {
            return false;
        }

        if (!target.TryGetLinearIndex1(target.LowerBound, out var targetFirst))
        {
            return false;
        }

        beforeNumericCopy(Length);
        ElementStorage.Copy(_elements!, first, target._elements!, targetFirst, Length);
        return true;
    }

    private bool RequiresElementCopy(int? fixedStringLength)
    {
        return fixedStringLength is not null || ElementType is UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol;
    }

    private void CopyVisibleValuesFrom(RuntimeArrayValue source)
    {
        if (!source.IsAllocated || !IsAllocated || source.Length == 0)
        {
            return;
        }

        source.ForEachVisibleIndexSetAscending(indices => SetValue(indices, source.GetValue(indices)));
    }

    private bool TryCopyVisibleValuesToClone(RuntimeArrayValue clone)
    {
        if (Rank != 1
            || !IsAllocated
            || !clone.IsAllocated
            || Length == 0
            || clone.Length != Length)
        {
            return false;
        }

        if (!TryGetLinearIndex1(LowerBound, out var first)
            || !TryGetLinearIndex1(UpperBound, out var last)
            || last - first + 1 != Length)
        {
            return false;
        }

        ElementStorage.Copy(_elements!, first, clone._elements!, 0, Length);
        return true;
    }

    private void CopyIntersection(
        ElementStorage sourceElements,
        IReadOnlyList<int> sourceLowerBounds,
        IReadOnlyList<int> sourceUpperBounds,
        ElementStorage targetElements,
        IReadOnlyList<int> targetLowerBounds,
        IReadOnlyList<int> targetUpperBounds)
    {
        if (sourceElements.Length == 0 || targetElements.Length == 0)
        {
            return;
        }

        if (Rank == 1)
        {
            var copyLowerBound = Math.Max(sourceLowerBounds[0], targetLowerBounds[0]);
            var copyUpperBound = Math.Min(sourceUpperBounds[0], targetUpperBounds[0]);

            if (copyUpperBound < copyLowerBound)
            {
                return;
            }

            var copyCount = copyUpperBound - copyLowerBound + 1;
            var sourceOffset = copyLowerBound - sourceLowerBounds[0];
            var targetOffset = copyLowerBound - targetLowerBounds[0];
            ElementStorage.Copy(sourceElements, sourceOffset, targetElements, targetOffset, copyCount);
            return;
        }

        var copyLowerBounds = new int[Rank];
        var copyUpperBounds = new int[Rank];

        for (var dimension = 0; dimension < Rank; dimension++)
        {
            copyLowerBounds[dimension] = Math.Max(sourceLowerBounds[dimension], targetLowerBounds[dimension]);
            copyUpperBounds[dimension] = Math.Min(sourceUpperBounds[dimension], targetUpperBounds[dimension]);

            if (copyUpperBounds[dimension] < copyLowerBounds[dimension])
            {
                return;
            }
        }

        var sourceStrides = CalculateStrides(sourceLowerBounds, sourceUpperBounds);
        var targetStrides = CalculateStrides(targetLowerBounds, targetUpperBounds);
        var indices = new int[Rank];
        CopyIntersectionRecursive(
            dimension: 0,
            indices,
            sourceElements,
            sourceLowerBounds,
            sourceStrides,
            targetElements,
            targetLowerBounds,
            targetStrides,
            copyLowerBounds,
            copyUpperBounds);
    }

    private void CopyIntersectionRecursive(
        int dimension,
        int[] indices,
        ElementStorage sourceElements,
        IReadOnlyList<int> sourceLowerBounds,
        IReadOnlyList<int> sourceStrides,
        ElementStorage targetElements,
        IReadOnlyList<int> targetLowerBounds,
        IReadOnlyList<int> targetStrides,
        IReadOnlyList<int> copyLowerBounds,
        IReadOnlyList<int> copyUpperBounds)
    {
        if (dimension == Rank)
        {
            var sourceIndex = GetLinearIndexWithStrides(indices, sourceLowerBounds, sourceStrides);
            var targetIndex = GetLinearIndexWithStrides(indices, targetLowerBounds, targetStrides);
            targetElements[targetIndex] = sourceElements[sourceIndex];
            return;
        }

        var index = copyLowerBounds[dimension];

        while (true)
        {
            indices[dimension] = index;
            CopyIntersectionRecursive(
                dimension + 1,
                indices,
                sourceElements,
                sourceLowerBounds,
                sourceStrides,
                targetElements,
                targetLowerBounds,
                targetStrides,
                copyLowerBounds,
                copyUpperBounds);

            if (index == copyUpperBounds[dimension])
            {
                break;
            }

            index++;
        }
    }
}
