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

    private ElementStorage? _elements;
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
        : this(elementType, fixedStringLength, rank, allocateUnallocatedBounds: true)
    {
    }

    private RuntimeArrayValue(TypeSymbol elementType, int? fixedStringLength, int rank, bool allocateUnallocatedBounds)
    {
        ArgumentNullException.ThrowIfNull(elementType);

        if (rank < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Array rank must be positive.");
        }

        ElementType = elementType;
        FixedStringLength = fixedStringLength;
        Rank = rank;
        if (allocateUnallocatedBounds)
        {
            _unallocatedLowerBounds = new int[rank];
            _unallocatedUpperBounds = new int[rank];
        }
        else
        {
            // Представление всегда размещено и не возвращает неявные границы.
            _unallocatedLowerBounds = Array.Empty<int>();
            _unallocatedUpperBounds = Array.Empty<int>();
        }
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

        var elements = array.CreateElementStorage(values.Count);
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
        var elements = array.CreateElementStorage(totalLength);
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
        ElementStorage elements,
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

    // Все массивы переданы исключительно во владение нового представления.
    private RuntimeArrayValue(RuntimeArrayValue source, int[] lowerBounds, int[] upperBounds,
        int?[] fixedStorageIndices, int[] visibleStorageDimensions)
        : this(source.ElementType, source.FixedStringLength, lowerBounds.Length, allocateUnallocatedBounds: false)
    {
        _lowerBounds = lowerBounds;
        _upperBounds = upperBounds;
        // Границы хранения корня заменяются при перевыделении, а не
        // изменяются на месте: вид разделяет массивы источника и сохраняет
        // момент создания благодаря проверке срока действия.
        _storageLowerBounds = source._storageLowerBounds!;
        _storageUpperBounds = source._storageUpperBounds!;
        _fixedStorageIndices = fixedStorageIndices;
        _visibleStorageDimensions = visibleStorageDimensions;
        _elements = source._elements;
        _capacity = source._elements!.Length;
        _storageLifetime = source._storageLifetime;
        _isView = true;
        // Границы хранения клонированы без изменений, поэтому шаги совпадают
        // с источником; вычисляются только длина и базовое смещение.
        _storageStrides = source._storageStrides;
        _visibleLength = CalculateLength(_lowerBounds, _upperBounds);
        _fixedStorageBaseOffset = _storageStrides is not null
            ? CalculateFixedStorageBaseOffset(GetFixedStorageIndices(), _storageLowerBounds, _storageStrides)
            : 0;
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

    internal int GetIntegerDirect(IReadOnlyList<int> indices)
    {
        EnsureAllocated();
        return _elements!.ReadInteger(GetLinearIndex(indices));
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
    internal bool TryGetInteger(ReadOnlySpan<int> indices, out int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex(indices, out var linearIndex))
        {
            value = 0;
            return false;
        }
        value = _elements!.ReadInteger(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetInteger3(int index0, int index1, int index2, out int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
        {
            value = 0;
            return false;
        }
        value = _elements!.ReadInteger(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetInteger2(int index0, int index1, out int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex2(index0, index1, out var linearIndex))
        {
            value = 0;
            return false;
        }
        value = _elements!.ReadInteger(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetInteger1(int index0, out int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex1(index0, out var linearIndex))
        {
            value = 0;
            return false;
        }
        value = _elements!.ReadInteger(linearIndex);
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
    internal bool TryGetValue2(int index0, int index1, out RuntimeValue value)
    {
        EnsureAllocated();

        if (!TryGetLinearIndex2(index0, index1, out var linearIndex))
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

        var fixedIndices = (int?[])GetFixedStorageIndices().Clone();
        var visibleDimensions = GetVisibleStorageDimensions();
        for (var index = 0; index < leadingIndices.Length; index++)
        {
            fixedIndices[visibleDimensions[index]] = leadingIndices[index];
        }

        var remaining = Rank - leadingIndices.Length;
        var lowerBounds = new int[remaining];
        var upperBounds = new int[remaining];
        var nextDimensions = new int[remaining];
        Array.Copy(_lowerBounds!, leadingIndices.Length, lowerBounds, 0, remaining);
        Array.Copy(_upperBounds!, leadingIndices.Length, upperBounds, 0, remaining);
        Array.Copy(visibleDimensions, leadingIndices.Length, nextDimensions, 0, remaining);
        return new RuntimeArrayValue(this, lowerBounds, upperBounds, fixedIndices, nextDimensions);
    }

    public RuntimeArrayValue CreateView(IReadOnlyList<RuntimeArrayIndexSlice> slices)
    {
        ArgumentNullException.ThrowIfNull(slices);

        EnsureAllocated();

        if (slices.Count < 1 || slices.Count > Rank)
        {
            throw new ArgumentException("Slice count must be between 1 and array rank.", nameof(slices));
        }

        if (Rank == 2 && slices is RuntimeArrayIndexSlice[] { Length: 2 } rowSlices
            && !rowSlices[0].IsRange && rowSlices[1].IsRange)
        {
            var fixedIndex = rowSlices[0].LowerBound;
            if (fixedIndex < _lowerBounds![0] || fixedIndex > _upperBounds![0])
                throw new ArgumentOutOfRangeException(nameof(slices), "Fixed index must stay within array bounds.");

            var range = rowSlices[1];
            ValidateVisibleRange(range.LowerBound, range.UpperBound, _lowerBounds[1], _upperBounds[1]);
            var dimensions = GetVisibleStorageDimensions();
            var fixedIndices = (int?[])GetFixedStorageIndices().Clone();
            fixedIndices[dimensions[0]] = fixedIndex;
            return new RuntimeArrayValue(this, [range.LowerBound], [range.UpperBound], fixedIndices, [dimensions[1]]);
        }

        if (Rank == 2 && slices is RuntimeArrayIndexSlice[] { Length: 2 } columnSlices
            && columnSlices[0].IsRange && !columnSlices[1].IsRange)
        {
            var range = columnSlices[0];
            ValidateVisibleRange(range.LowerBound, range.UpperBound, _lowerBounds![0], _upperBounds![0]);
            var fixedIndex = columnSlices[1].LowerBound;
            if (fixedIndex < _lowerBounds[1] || fixedIndex > _upperBounds[1])
                throw new ArgumentOutOfRangeException(nameof(slices), "Fixed index must stay within array bounds.");

            var dimensions = GetVisibleStorageDimensions();
            var fixedIndices = (int?[])GetFixedStorageIndices().Clone();
            fixedIndices[dimensions[1]] = fixedIndex;
            return new RuntimeArrayValue(this, [range.LowerBound], [range.UpperBound], fixedIndices, [dimensions[0]]);
        }

        var currentFixedStorageIndices = GetFixedStorageIndices();
        var currentVisibleStorageDimensions = GetVisibleStorageDimensions();
        // Размеры известны заранее: видимые измерения = ранг минус число
        // фиксированных срезов. Массивы принадлежат новому представлению.
        var fixedSliceCount = 0;
        for (var dimension = 0; dimension < slices.Count; dimension++)
        {
            if (!slices[dimension].IsRange)
            {
                fixedSliceCount++;
            }
        }

        var visibleCount = Rank - fixedSliceCount;
        if (visibleCount <= 0)
        {
            throw new ArgumentException("Slice view must keep at least one visible dimension.", nameof(slices));
        }

        var nextFixedStorageIndices = new int?[currentFixedStorageIndices.Length];
        Array.Copy(currentFixedStorageIndices, nextFixedStorageIndices, currentFixedStorageIndices.Length);
        var nextVisibleStorageDimensions = new int[visibleCount];
        var nextLowerBounds = new int[visibleCount];
        var nextUpperBounds = new int[visibleCount];
        var nextIndex = 0;

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
                    nextVisibleStorageDimensions[nextIndex] = storageDimension;
                    nextLowerBounds[nextIndex] = slice.LowerBound;
                    nextUpperBounds[nextIndex] = slice.UpperBound;
                    nextIndex++;
                    continue;
                }

                if (slice.LowerBound < lowerBound || slice.LowerBound > upperBound)
                {
                    throw new ArgumentOutOfRangeException(nameof(slices), "Fixed index must stay within array bounds.");
                }

                nextFixedStorageIndices[storageDimension] = slice.LowerBound;
                continue;
            }

            nextVisibleStorageDimensions[nextIndex] = storageDimension;
            nextLowerBounds[nextIndex] = lowerBound;
            nextUpperBounds[nextIndex] = upperBound;
            nextIndex++;
        }

        return new RuntimeArrayValue(this, nextLowerBounds, nextUpperBounds, nextFixedStorageIndices, nextVisibleStorageDimensions);
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
    internal bool TrySetInteger1(int index0, int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex1(index0, out var linearIndex))
            return false;

        _elements!.WriteInteger(linearIndex, value);
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
    internal bool TrySetInteger2(int index0, int index1, int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex2(index0, index1, out var linearIndex))
            return false;

        _elements!.WriteInteger(linearIndex, value);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetInteger3(int index0, int index1, int index2, int value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Integer || !TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
            return false;

        _elements!.WriteInteger(linearIndex, value);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetValue2(int index0, int index1, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureAllocated();

        if (!TryGetLinearIndex2(index0, index1, out var linearIndex))
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

        _elements!.CopyTo(sourceLinearIndex, destination, destinationIndex, count);
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

        ElementStorage.Copy(_elements!, sourceLinearIndex, _elements!, destinationLinearIndex, count);
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
                ElementStorage.Copy(_elements, 0, nextElements, 0, Length);
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

    /// <summary>
    /// Вставка в начало или середину одномерного корневого массива сдвигом хвоста
    /// в том же хранилище (механизм memmove списка CPython); при нехватке ёмкости
    /// буфер растёт по удвоению, а не под точный размер. Возвращает false для видов,
    /// пустого массива с неединичной нижней границей и неплотного хранилища —
    /// эти случаи обрабатывает общий путь перевыделения.
    /// </summary>
    internal bool TryInsertOneDimensionalAt(int insertionIndex, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsureResizableRoot();
        EnsureAllocated();

        if (Rank != 1 || IsView || (Length == 0 && LowerBound != 1))
        {
            return false;
        }

        if (insertionIndex < LowerBound || insertionIndex > UpperBound)
        {
            return false;
        }

        if (!TryGetDenseOneDimensionalLinearIndex(insertionIndex, out var linearIndex))
        {
            return false;
        }

        if (Length + 1 > MaxElementCount)
        {
            throw new ArrayAllocationFailureException(
                $"Не удалось расширить массив: число элементов превысит предел {MaxElementCount}.");
        }

        if (_elements!.Length < Length + 1)
        {
            var nextElements = CreateElementStorage(GrowCapacity(Length + 1));

            if (Length > 0)
            {
                ElementStorage.Copy(_elements, 0, nextElements, 0, Length);
            }

            AttachStorage(nextElements);
        }

        var tailCount = UpperBound - insertionIndex + 1;
        if (tailCount > 0)
        {
            ElementStorage.Copy(_elements, linearIndex, _elements, linearIndex + 1, tailCount);
        }

        _elements[linearIndex] = value.CopyForStorage(FixedStringLength);
        _upperBounds![0] = checked(UpperBound + 1);
        _storageUpperBounds = _upperBounds;
        _visibleLength = UpperBound - LowerBound + 1;
        _isView = false;
        RenewStorageLifetime();
        return true;
    }

    /// <summary>
    /// Удаление из начала или середины одномерного корневого массива сдвигом хвоста
    /// в том же хранилище без перевыделения; последний слот очищается. Последний
    /// элемент обрабатывает TryPopOneDimensional, поэтому сюда он не входит.
    /// </summary>
    internal bool TryRemoveOneDimensionalAt(int removalIndex, out RuntimeValue value)
    {
        EnsureResizableRoot();
        EnsureAllocated();

        if (Rank != 1 || IsView || Length <= 1 || removalIndex >= UpperBound)
        {
            value = RuntimeValue.Void;
            return false;
        }

        if (!TryGetDenseOneDimensionalLinearIndex(removalIndex, out var linearIndex))
        {
            value = RuntimeValue.Void;
            return false;
        }

        value = _elements![linearIndex];
        ElementStorage.Copy(_elements, linearIndex + 1, _elements, linearIndex, UpperBound - removalIndex);
        _elements.Clear(UpperBound - LowerBound);
        _upperBounds![0] = UpperBound - 1;
        _storageUpperBounds = _upperBounds;
        _visibleLength = UpperBound - LowerBound + 1;
        _isView = false;
        RenewStorageLifetime();
        return true;
    }

    private bool TryGetDenseOneDimensionalLinearIndex(int index, out int linearIndex)
    {
        if (!TryGetLinearIndex1(index, out linearIndex))
        {
            return false;
        }

        return linearIndex == index - _lowerBounds![0];
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
        _elements.Clear(previousLength - 1);

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
