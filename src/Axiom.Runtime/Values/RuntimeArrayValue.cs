using Axiom.Semantics;
using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue : IRuntimeStoredValue
{
    /// <summary>
    /// Предельное число элементов одного массива. Им ограничивается размер
    /// хранилища раньше, чем Int32-длина массива и память процесса.
    /// </summary>
    internal const int MaxElementCount = 64 * 1024 * 1024;

    private RuntimeValue[]? _elements;
    private StorageLifetime? _storageLifetime;
    private long _shapeRevision;
    private int _capacity;
    private readonly int[] _unallocatedLowerBounds;
    private readonly int[] _unallocatedUpperBounds;
    private bool _isView;
    private int[]? _lowerBounds;
    private int[]? _upperBounds;
    private int[]? _storageLowerBounds;
    private int[]? _storageUpperBounds;
    private int?[]? _fixedStorageIndices;
    private int[]? _visibleStorageDimensions;
    private int[]? _storageStrides;
    private int _visibleLength;
    private int _fixedStorageBaseOffset;

    public RuntimeArrayValue(TypeSymbol elementType, int? fixedStringLength = null, int rank = 1)
    {
        ArgumentNullException.ThrowIfNull(elementType);

        if (rank < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Array rank must be positive.");
        }

        ElementType = elementType;
        FixedStringLength = fixedStringLength;
        Rank = rank;
        _unallocatedLowerBounds = new int[rank];
        _unallocatedUpperBounds = new int[rank];
    }

    public RuntimeArrayValue(TypeSymbol elementType, int? fixedStringLength, int lowerBound, int upperBound)
        : this(elementType, fixedStringLength, rank: 1)
    {
        Allocate(lowerBound, upperBound);
    }

    public RuntimeArrayValue(
        TypeSymbol elementType,
        int? fixedStringLength,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds)
        : this(elementType, fixedStringLength, lowerBounds?.Count ?? 0)
    {
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);
        Allocate(lowerBounds, upperBounds);
    }

    public static RuntimeArrayValue CreateVisibleShape(
        TypeSymbol elementType,
        int? fixedStringLength,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);

        var array = new RuntimeArrayValue(elementType, fixedStringLength, lowerBounds.Count);

        if (!HasNonCanonicalEmptyVisibleRange(lowerBounds, upperBounds))
        {
            array.Allocate(lowerBounds, upperBounds);
            return array;
        }

        array._lowerBounds = lowerBounds.ToArray();
        array._upperBounds = upperBounds.ToArray();
        array._storageLowerBounds = NormalizeStorageLowerBounds(lowerBounds, upperBounds);
        array._storageUpperBounds = NormalizeStorageUpperBounds(lowerBounds, upperBounds);
        array._fixedStorageIndices = CreateEmptyFixedStorageIndices(array.Rank);
        array._visibleStorageDimensions = CreateIdentityStorageDimensions(array.Rank);
        array._elements = array.CreateDefaultElements(array._storageLowerBounds, array._storageUpperBounds);
        array._capacity = array._elements.Length;
        array._storageLifetime = new StorageLifetime();
        array.RefreshShapeMetadata();
        return array;
    }

    internal static RuntimeArrayValue CreateCompactOneDimensional(
        TypeSymbol elementType,
        int? fixedStringLength,
        IReadOnlyList<RuntimeValue> values)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(values);

        var array = new RuntimeArrayValue(elementType, fixedStringLength, rank: 1);
        array._lowerBounds = [1];
        array._upperBounds = [values.Count == 0 ? 0 : values.Count];
        array._storageLowerBounds = array._lowerBounds;
        array._storageUpperBounds = array._upperBounds;
        array._fixedStorageIndices = CreateEmptyFixedStorageIndices(rank: 1);
        array._visibleStorageDimensions = CreateIdentityStorageDimensions(rank: 1);
        array._isView = false;

        var elements = new RuntimeValue[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            elements[index] = values[index].CopyForStorage(fixedStringLength);
        }

        array._elements = elements;
        array._capacity = elements.Length;
        array._storageLifetime = new StorageLifetime();
        array.RefreshShapeMetadata();
        return array;
    }

    internal static RuntimeArrayValue CreateCompactTwoDimensional(
        TypeSymbol elementType,
        int? fixedStringLength,
        int firstLength,
        int secondLength,
        Func<int, int, RuntimeValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var array = new RuntimeArrayValue(elementType, fixedStringLength, rank: 2);
        array._lowerBounds = [1, 1];
        array._upperBounds = [firstLength, secondLength];
        array._storageLowerBounds = array._lowerBounds;
        array._storageUpperBounds = array._upperBounds;
        array._fixedStorageIndices = CreateEmptyFixedStorageIndices(rank: 2);
        array._visibleStorageDimensions = CreateIdentityStorageDimensions(rank: 2);
        array._isView = false;

        var totalLength = firstLength <= 0 || secondLength <= 0
            ? 0
            : ValidateElementCount((long)firstLength * secondLength);
        var elements = CreateElementStorage(totalLength);
        var elementIndex = 0;

        for (var firstIndex = 1; firstIndex <= firstLength; firstIndex++)
        {
            for (var secondIndex = 1; secondIndex <= secondLength; secondIndex++)
            {
                var value = valueFactory(firstIndex, secondIndex);
                elements[elementIndex++] = value.CopyForStorage(fixedStringLength);
            }
        }

        array._elements = elements;
        array._capacity = elements.Length;
        array._storageLifetime = new StorageLifetime();
        array.RefreshShapeMetadata();
        return array;
    }

    private RuntimeArrayValue(
        TypeSymbol elementType,
        int? fixedStringLength,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds,
        RuntimeValue[] elements,
        IReadOnlyList<int> storageLowerBounds,
        IReadOnlyList<int> storageUpperBounds,
        IReadOnlyList<int?> fixedStorageIndices,
        IReadOnlyList<int> visibleStorageDimensions,
        StorageLifetime storageLifetime)
        : this(elementType, fixedStringLength, lowerBounds.Count)
    {
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(storageLowerBounds);
        ArgumentNullException.ThrowIfNull(storageUpperBounds);
        ArgumentNullException.ThrowIfNull(fixedStorageIndices);
        ArgumentNullException.ThrowIfNull(visibleStorageDimensions);
        ArgumentNullException.ThrowIfNull(storageLifetime);

        _lowerBounds = lowerBounds.ToArray();
        _upperBounds = upperBounds.ToArray();
        _storageLowerBounds = storageLowerBounds.ToArray();
        _storageUpperBounds = storageUpperBounds.ToArray();
        _fixedStorageIndices = fixedStorageIndices.ToArray();
        _visibleStorageDimensions = visibleStorageDimensions.ToArray();
        _elements = elements;
        _capacity = elements.Length;
        _storageLifetime = storageLifetime;
        _isView = true;
        RefreshShapeMetadata();
    }

    public TypeSymbol ElementType { get; }

    public int? FixedStringLength { get; }

    public int Rank { get; }

    /// <summary>
    /// Revision of the allocated shape and backing storage. Element writes do
    /// not change it; allocation, reallocation and deallocation do.
    /// </summary>
    internal long StructuralVersion
    {
        get
        {
            if (_storageLifetime is { Invalidated: true })
            {
                throw new ExpiredArrayViewException();
            }

            return _storageLifetime?.Revision ?? _shapeRevision;
        }
    }

    internal bool IsStructuralVersionCurrent(long expectedVersion) =>
        IsAllocated
        && _storageLifetime is { Invalidated: false } lifetime
        && lifetime.Revision == expectedVersion;

    public IReadOnlyList<int> LowerBounds => IsAllocated ? _lowerBounds! : _unallocatedLowerBounds;

    public IReadOnlyList<int> UpperBounds => IsAllocated ? _upperBounds! : _unallocatedUpperBounds;

    public int LowerBound => LowerBounds[0];

    public int UpperBound => UpperBounds[0];

    public bool IsAllocated => _elements is not null;

    public bool IsView => IsAllocated && _isView;

    /// <summary>
    /// Проверяет, что два массива читают одно и то же хранилище.
    /// Нужно операциям изменения размера, которые получают срез
    /// собственного массива как значение.
    /// </summary>
    internal bool SharesStorageWith(RuntimeArrayValue other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return IsAllocated && other.IsAllocated && ReferenceEquals(_elements, other._elements);
    }

    public int Length => IsAllocated ? _visibleLength : 0;

    public bool ContainsIndex(int index) => Rank == 1 && ContainsIndices([index]);

    public bool ContainsIndices(IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        return IsWithinBounds(indices, requireFullRank: true);
    }

    public bool ContainsIndices(ReadOnlySpan<int> indices) => IsWithinBounds(indices, requireFullRank: true);

    public bool ContainsLeadingIndices(IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        return IsWithinBounds(indices, requireFullRank: false);
    }

    public bool ContainsLeadingIndices(ReadOnlySpan<int> indices) => IsWithinBounds(indices, requireFullRank: false);

    public int GetLowerBound(int dimension)
    {
        ValidateDimensionNumber(dimension);
        return LowerBounds[dimension - 1];
    }

    public int GetUpperBound(int dimension)
    {
        ValidateDimensionNumber(dimension);
        return UpperBounds[dimension - 1];
    }

    public RuntimeValue GetValue(int index)
    {
        EnsureOneDimensional();
        return GetValue([index]);
    }

    public RuntimeValue GetValue(IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        EnsureAllocated();
        return _elements![GetLinearIndex(indices)];
    }

    public RuntimeValue GetValue(ReadOnlySpan<int> indices)
    {
        EnsureAllocated();
        return _elements![GetLinearIndex(indices)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetValue(ReadOnlySpan<int> indices, out RuntimeValue value)
    {
        EnsureAllocated();

        if (!TryGetLinearIndex(indices, out var linearIndex))
        {
            value = RuntimeValue.Void;
            return false;
        }

        value = _elements![linearIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetValue1(int index0, out RuntimeValue value)
    {
        EnsureAllocated();

        if (!TryGetLinearIndex1(index0, out var linearIndex))
        {
            value = RuntimeValue.Void;
            return false;
        }

        value = _elements![linearIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetValue3(int index0, int index1, int index2, out RuntimeValue value)
    {
        EnsureAllocated();

        if (!TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
        {
            value = RuntimeValue.Void;
            return false;
        }

        value = _elements![linearIndex];
        return true;
    }

    public RuntimeArrayValue CreateView(IReadOnlyList<int> leadingIndices)
    {
        ArgumentNullException.ThrowIfNull(leadingIndices);

        EnsureAllocated();

        if (leadingIndices.Count < 1 || leadingIndices.Count >= Rank)
        {
            throw new ArgumentException("Leading index count must be between 1 and rank - 1.", nameof(leadingIndices));
        }

        if (!ContainsLeadingIndices(leadingIndices))
        {
            throw new ArgumentOutOfRangeException(nameof(leadingIndices), "Leading indices must stay within array bounds.");
        }

        var slices = leadingIndices
            .Select(static index => RuntimeArrayIndexSlice.Single(index))
            .ToArray();
        return CreateView(slices);
    }

    public RuntimeArrayValue CreateView(ReadOnlySpan<int> leadingIndices)
    {
        EnsureAllocated();

        if (leadingIndices.Length < 1 || leadingIndices.Length >= Rank)
        {
            throw new ArgumentException("Leading index count must be between 1 and rank - 1.", nameof(leadingIndices));
        }

        if (!ContainsLeadingIndices(leadingIndices))
        {
            throw new ArgumentOutOfRangeException(nameof(leadingIndices), "Leading indices must stay within array bounds.");
        }

        var slices = new RuntimeArrayIndexSlice[leadingIndices.Length];
        for (var index = 0; index < leadingIndices.Length; index++)
        {
            slices[index] = RuntimeArrayIndexSlice.Single(leadingIndices[index]);
        }

        return CreateView(slices);
    }

    public RuntimeArrayValue CreateView(IReadOnlyList<RuntimeArrayIndexSlice> slices)
    {
        ArgumentNullException.ThrowIfNull(slices);

        EnsureAllocated();

        if (slices.Count < 1 || slices.Count > Rank)
        {
            throw new ArgumentException("Slice count must be between 1 and array rank.", nameof(slices));
        }

        var currentFixedStorageIndices = GetFixedStorageIndices();
        var currentVisibleStorageDimensions = GetVisibleStorageDimensions();
        var nextFixedStorageIndices = currentFixedStorageIndices.ToArray();
        var nextVisibleStorageDimensions = new List<int>(Rank);
        var nextLowerBounds = new List<int>(Rank);
        var nextUpperBounds = new List<int>(Rank);

        for (var dimension = 0; dimension < Rank; dimension++)
        {
            var storageDimension = currentVisibleStorageDimensions[dimension];
            var lowerBound = _lowerBounds![dimension];
            var upperBound = _upperBounds![dimension];

            if (dimension < slices.Count)
            {
                var slice = slices[dimension];

                if (slice.IsRange)
                {
                    ValidateVisibleRange(slice.LowerBound, slice.UpperBound, lowerBound, upperBound);
                    nextVisibleStorageDimensions.Add(storageDimension);
                    nextLowerBounds.Add(slice.LowerBound);
                    nextUpperBounds.Add(slice.UpperBound);
                    continue;
                }

                if (slice.LowerBound < lowerBound || slice.LowerBound > upperBound)
                {
                    throw new ArgumentOutOfRangeException(nameof(slices), "Fixed index must stay within array bounds.");
                }

                nextFixedStorageIndices[storageDimension] = slice.LowerBound;
                continue;
            }

            nextVisibleStorageDimensions.Add(storageDimension);
            nextLowerBounds.Add(lowerBound);
            nextUpperBounds.Add(upperBound);
        }

        if (nextVisibleStorageDimensions.Count == 0)
        {
            throw new ArgumentException("Slice view must keep at least one visible dimension.", nameof(slices));
        }

        return new RuntimeArrayValue(
            ElementType,
            FixedStringLength,
            nextLowerBounds,
            nextUpperBounds,
            _elements!,
            _storageLowerBounds!,
            _storageUpperBounds!,
            nextFixedStorageIndices,
            nextVisibleStorageDimensions,
            _storageLifetime!);
    }

    public void SetValue(int index, RuntimeValue value)
    {
        EnsureOneDimensional();
        SetValue([index], value);
    }

    public void SetValue(IReadOnlyList<int> indices, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(value);

        EnsureAllocated();
        _elements![GetLinearIndex(indices)] = value.CopyForStorage(FixedStringLength);
    }

    public void SetValue(ReadOnlySpan<int> indices, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        EnsureAllocated();
        _elements![GetLinearIndex(indices)] = value.CopyForStorage(FixedStringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetValue(ReadOnlySpan<int> indices, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureAllocated();

        if (!TryGetLinearIndex(indices, out var linearIndex))
        {
            return false;
        }

        _elements![linearIndex] = value.CopyForStorage(FixedStringLength);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetValue1(int index0, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureAllocated();

        if (!TryGetLinearIndex1(index0, out var linearIndex))
        {
            return false;
        }

        _elements![linearIndex] = value.CopyForStorage(FixedStringLength);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetValue3(int index0, int index1, int index2, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureAllocated();

        if (!TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
        {
            return false;
        }

        _elements![linearIndex] = value.CopyForStorage(FixedStringLength);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryCopyOneDimensionalRangeTo(int sourceIndex, int count, RuntimeValue[] destination, int destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(destination);
        EnsureAllocated();

        if (Rank != 1 || count < 0 || destinationIndex < 0 || destinationIndex + count > destination.Length)
        {
            return false;
        }

        if (count == 0)
        {
            return true;
        }

        if (!TryGetLinearIndex1(sourceIndex, out var sourceLinearIndex)
            || !TryGetLinearIndex1(sourceIndex + count - 1, out var endLinearIndex)
            || endLinearIndex - sourceLinearIndex + 1 != count)
        {
            return false;
        }

        Array.Copy(_elements!, sourceLinearIndex, destination, destinationIndex, count);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryMoveOneDimensionalRange(int sourceIndex, int destinationIndex, int count)
    {
        EnsureAllocated();

        if (Rank != 1 || count < 0)
        {
            return false;
        }

        if (count == 0)
        {
            return true;
        }

        if (!TryGetLinearIndex1(sourceIndex, out var sourceLinearIndex)
            || !TryGetLinearIndex1(sourceIndex + count - 1, out var sourceEndLinearIndex)
            || !TryGetLinearIndex1(destinationIndex, out var destinationLinearIndex)
            || !TryGetLinearIndex1(destinationIndex + count - 1, out var destinationEndLinearIndex)
            || sourceEndLinearIndex - sourceLinearIndex + 1 != count
            || destinationEndLinearIndex - destinationLinearIndex + 1 != count)
        {
            return false;
        }

        Array.Copy(_elements!, sourceLinearIndex, _elements!, destinationLinearIndex, count);
        return true;
    }

    internal bool TryAppendOneDimensional(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureResizableRoot();
        EnsureAllocated();

        if (Rank != 1 || IsView)
        {
            return false;
        }

        if (Length + 1 > MaxElementCount)
        {
            throw new ArrayAllocationFailureException(
                $"Не удалось расширить массив: число элементов превысит предел {MaxElementCount}.");
        }

        var nextLowerBound = LowerBound;
        var nextUpperBound = Length == 0
            ? nextLowerBound
            : checked(UpperBound + 1);

        if (_elements!.Length < Length + 1)
        {
            var nextElements = CreateElementStorage(GrowCapacity(Length + 1));

            if (Length > 0)
            {
                Array.Copy(_elements, 0, nextElements, 0, Length);
            }

            AttachStorage(nextElements);
        }

        _elements![Length] = value.CopyForStorage(FixedStringLength);
        _lowerBounds![0] = nextLowerBound;
        _upperBounds![0] = nextUpperBound;
        _storageLowerBounds = _lowerBounds;
        _storageUpperBounds = _upperBounds;

        if (_storageStrides is null || _storageStrides[0] != 1)
        {
            _storageStrides = [1];
        }

        _fixedStorageBaseOffset = 0;
        _visibleLength = nextUpperBound - nextLowerBound + 1;
        _isView = false;
        RenewStorageLifetime();
        return true;
    }

    internal bool TryPopOneDimensional(out RuntimeValue value)
    {
        EnsureResizableRoot();
        EnsureAllocated();

        if (Rank != 1 || IsView || Length == 0)
        {
            value = RuntimeValue.Void;
            return false;
        }

        var previousLength = Length;
        value = _elements![previousLength - 1];
        _elements[previousLength - 1] = RuntimeValue.Void;

        if (previousLength == 1)
        {
            _lowerBounds = [1];
            _upperBounds = [0];
            _storageLowerBounds = _lowerBounds;
            _storageUpperBounds = _upperBounds;
            _fixedStorageIndices = CreateEmptyFixedStorageIndices(rank: 1);
            _visibleStorageDimensions = CreateIdentityStorageDimensions(rank: 1);
            _isView = false;
            RefreshShapeMetadata();
            RenewStorageLifetime();
            return true;
        }

        _upperBounds![0] = UpperBound - 1;
        _storageUpperBounds = _upperBounds;
        _visibleLength = previousLength - 1;
        _isView = false;
        RenewStorageLifetime();
        return true;
    }

    public void Allocate(int lowerBound, int upperBound)
    {
        EnsureOneDimensional();
        Allocate([lowerBound], [upperBound]);
    }

    public void Allocate(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        EnsureResizableRoot();
        ValidateBounds(lowerBounds, upperBounds, Rank);

        var nextLowerBounds = lowerBounds.ToArray();
        var nextUpperBounds = upperBounds.ToArray();
        var nextElements = CreateDefaultElements(nextLowerBounds, nextUpperBounds);

        _lowerBounds = nextLowerBounds;
        _upperBounds = nextUpperBounds;
        _storageLowerBounds = nextLowerBounds;
        _storageUpperBounds = nextUpperBounds;
        _fixedStorageIndices = CreateEmptyFixedStorageIndices(Rank);
        _visibleStorageDimensions = CreateIdentityStorageDimensions(Rank);
        _isView = false;
        AttachStorage(nextElements);
        RefreshShapeMetadata();
    }

    public void AllocatePreserving(int lowerBound, int upperBound)
    {
        EnsureOneDimensional();
        AllocatePreserving([lowerBound], [upperBound]);
    }

    public void AllocatePreserving(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        EnsureResizableRoot();
        ValidateBounds(lowerBounds, upperBounds, Rank);

        var previousElements = _elements;
        var previousLowerBounds = _storageLowerBounds;
        var previousUpperBounds = _storageUpperBounds;
        var nextLowerBounds = lowerBounds.ToArray();
        var nextUpperBounds = upperBounds.ToArray();
        var newElements = CreateDefaultElements(nextLowerBounds, nextUpperBounds);

        if (previousElements is not null && previousLowerBounds is not null && previousUpperBounds is not null)
        {
            CopyIntersection(previousElements, previousLowerBounds, previousUpperBounds, newElements, nextLowerBounds, nextUpperBounds);
        }

        _lowerBounds = nextLowerBounds;
        _upperBounds = nextUpperBounds;
        _storageLowerBounds = nextLowerBounds;
        _storageUpperBounds = nextUpperBounds;
        _fixedStorageIndices = CreateEmptyFixedStorageIndices(Rank);
        _visibleStorageDimensions = CreateIdentityStorageDimensions(Rank);
        _isView = false;
        AttachStorage(newElements);
        RefreshShapeMetadata();
    }

    public void Deallocate()
    {
        EnsureResizableRoot();
        InvalidateStorageLifetime();
        _shapeRevision = unchecked(_shapeRevision + 1);
        _lowerBounds = null;
        _upperBounds = null;
        _storageLowerBounds = null;
        _storageUpperBounds = null;
        _fixedStorageIndices = null;
        _visibleStorageDimensions = null;
        _storageStrides = null;
        _visibleLength = 0;
        _isView = false;
        _elements = null;
        _capacity = 0;
        _storageLifetime = null;
    }

    public void ForEachVisibleIndexSetAscending(Action<IReadOnlyList<int>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        EnsureAllocated();

        if (Length == 0)
        {
            return;
        }

        var indices = new int[Rank];
        ForEachVisibleIndexSetAscendingRecursive(indices, dimension: 0, action);
    }

    private void ForEachVisibleIndexSetAscendingRecursive(
        int[] indices,
        int dimension,
        Action<IReadOnlyList<int>> action)
    {
        if (dimension == Rank)
        {
            action(indices);
            return;
        }

        var index = GetLowerBound(dimension + 1);
        var upperBound = GetUpperBound(dimension + 1);

        while (true)
        {
            indices[dimension] = index;
            ForEachVisibleIndexSetAscendingRecursive(indices, dimension + 1, action);

            if (index == upperBound)
            {
                break;
            }

            index++;
        }
    }
}

public readonly record struct RuntimeArrayIndexSlice(bool IsRange, int LowerBound, int UpperBound)
{
    public static RuntimeArrayIndexSlice Single(int index) => new(false, index, index);

    public static RuntimeArrayIndexSlice Range(int lowerBound, int upperBound) => new(true, lowerBound, upperBound);
}
