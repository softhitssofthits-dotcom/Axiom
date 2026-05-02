using Axiom.Semantics;
using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

public sealed class RuntimeArrayValue : IRuntimeStoredValue
{
    private RuntimeValue[]? _elements;
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
            elements[index] = fixedStringLength is null && elementType is not UserDefinedTypeSymbol
                ? values[index]
                : values[index].CopyForStorage(fixedStringLength);
        }

        array._elements = elements;
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
            : checked(firstLength * secondLength);
        var elements = new RuntimeValue[totalLength];
        var elementIndex = 0;

        for (var firstIndex = 1; firstIndex <= firstLength; firstIndex++)
        {
            for (var secondIndex = 1; secondIndex <= secondLength; secondIndex++)
            {
                var value = valueFactory(firstIndex, secondIndex);
                elements[elementIndex++] = fixedStringLength is null && elementType is not UserDefinedTypeSymbol
                    ? value
                    : value.CopyForStorage(fixedStringLength);
            }
        }

        array._elements = elements;
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
        IReadOnlyList<int> visibleStorageDimensions)
        : this(elementType, fixedStringLength, lowerBounds.Count)
    {
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(storageLowerBounds);
        ArgumentNullException.ThrowIfNull(storageUpperBounds);
        ArgumentNullException.ThrowIfNull(fixedStorageIndices);
        ArgumentNullException.ThrowIfNull(visibleStorageDimensions);

        _lowerBounds = lowerBounds.ToArray();
        _upperBounds = upperBounds.ToArray();
        _storageLowerBounds = storageLowerBounds.ToArray();
        _storageUpperBounds = storageUpperBounds.ToArray();
        _fixedStorageIndices = fixedStorageIndices.ToArray();
        _visibleStorageDimensions = visibleStorageDimensions.ToArray();
        _elements = elements;
        _isView = true;
        RefreshShapeMetadata();
    }

    public TypeSymbol ElementType { get; }

    public int? FixedStringLength { get; }

    public int Rank { get; }

    public IReadOnlyList<int> LowerBounds => IsAllocated ? _lowerBounds! : _unallocatedLowerBounds;

    public IReadOnlyList<int> UpperBounds => IsAllocated ? _upperBounds! : _unallocatedUpperBounds;

    public int LowerBound => LowerBounds[0];

    public int UpperBound => UpperBounds[0];

    public bool IsAllocated => _elements is not null;

    public bool IsView => IsAllocated && _isView;

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
            nextVisibleStorageDimensions);
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

    public void Allocate(int lowerBound, int upperBound)
    {
        EnsureOneDimensional();
        Allocate([lowerBound], [upperBound]);
    }

    public void Allocate(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        EnsureResizableRoot();
        ValidateBounds(lowerBounds, upperBounds, Rank);

        _lowerBounds = lowerBounds.ToArray();
        _upperBounds = upperBounds.ToArray();
        _storageLowerBounds = _lowerBounds;
        _storageUpperBounds = _upperBounds;
        _fixedStorageIndices = CreateEmptyFixedStorageIndices(Rank);
        _visibleStorageDimensions = CreateIdentityStorageDimensions(Rank);
        _isView = false;
        _elements = CreateDefaultElements(_storageLowerBounds, _storageUpperBounds);
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
        _elements = newElements;
        RefreshShapeMetadata();
    }

    public void Deallocate()
    {
        EnsureResizableRoot();
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
    }

    public RuntimeArrayValue Clone(int? fixedStringLengthOverride = null)
    {
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
        clone._elements = clone.CreateDefaultElements(clone._storageLowerBounds, clone._storageUpperBounds);
        clone.RefreshShapeMetadata();
        clone.CopyVisibleValuesFrom(this);
        return clone;
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

    private void CopyVisibleValuesFrom(RuntimeArrayValue source)
    {
        if (!source.IsAllocated || !IsAllocated || source.Length == 0)
        {
            return;
        }

        source.ForEachVisibleIndexSetAscending(indices => SetValue(indices, source.GetValue(indices)));
    }

    private void CopyIntersection(
        RuntimeValue[] sourceElements,
        IReadOnlyList<int> sourceLowerBounds,
        IReadOnlyList<int> sourceUpperBounds,
        RuntimeValue[] targetElements,
        IReadOnlyList<int> targetLowerBounds,
        IReadOnlyList<int> targetUpperBounds)
    {
        if (sourceElements.Length == 0 || targetElements.Length == 0)
        {
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
        RuntimeValue[] sourceElements,
        IReadOnlyList<int> sourceLowerBounds,
        IReadOnlyList<int> sourceStrides,
        RuntimeValue[] targetElements,
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

    private static void ValidateBounds(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds, int expectedRank)
    {
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);

        if (lowerBounds.Count != expectedRank || upperBounds.Count != expectedRank)
        {
            throw new ArgumentException("Array bounds count must match array rank.");
        }

        for (var dimension = 0; dimension < expectedRank; dimension++)
        {
            ValidateDimensionBounds(lowerBounds[dimension], upperBounds[dimension]);
        }
    }

    private static void ValidateDimensionBounds(int lowerBound, int upperBound)
    {
        if (upperBound < lowerBound && !IsCanonicalEmptyRange(lowerBound, upperBound))
        {
            throw new ArgumentOutOfRangeException(
                nameof(upperBound),
                upperBound,
                "Upper bound must not be less than lower bound, except for the canonical empty range 1 TO 0.");
        }
    }

    private static bool IsCanonicalEmptyRange(int lowerBound, int upperBound) => lowerBound == 1 && upperBound == 0;

    private static bool IsEmptyRange(int lowerBound, int upperBound) => upperBound < lowerBound;

    private static bool HasNonCanonicalEmptyVisibleRange(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (IsEmptyRange(lowerBounds[dimension], upperBounds[dimension])
                && !IsCanonicalEmptyRange(lowerBounds[dimension], upperBounds[dimension]))
            {
                return true;
            }
        }

        return false;
    }

    private static int[] NormalizeStorageLowerBounds(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var normalized = new int[lowerBounds.Count];

        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            normalized[dimension] = IsEmptyRange(lowerBounds[dimension], upperBounds[dimension])
                ? 1
                : lowerBounds[dimension];
        }

        return normalized;
    }

    private static int[] NormalizeStorageUpperBounds(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var normalized = new int[upperBounds.Count];

        for (var dimension = 0; dimension < upperBounds.Count; dimension++)
        {
            normalized[dimension] = IsEmptyRange(lowerBounds[dimension], upperBounds[dimension])
                ? 0
                : upperBounds[dimension];
        }

        return normalized;
    }

    private RuntimeValue[] CreateDefaultElements(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var length = CalculateLength(lowerBounds, upperBounds);
        var elements = new RuntimeValue[length];

        for (var index = 0; index < elements.Length; index++)
        {
            elements[index] = RuntimeValue.Default(ElementType, FixedStringLength);
        }

        return elements;
    }

    private static int CalculateLength(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (IsEmptyRange(lowerBounds[dimension], upperBounds[dimension]))
            {
                return 0;
            }
        }

        var length = 1;

        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            var extent = checked(upperBounds[dimension] - lowerBounds[dimension] + 1);
            length = checked(length * extent);
        }

        return length;
    }

    private int GetLinearIndex(IReadOnlyList<int> localIndices)
    {
        if (!ContainsIndices(localIndices))
        {
            throw new ArgumentOutOfRangeException(nameof(localIndices), "Indices must stay within array bounds.");
        }

        var visibleStorageDimensions = GetVisibleStorageDimensions();
        var storageLowerBounds = _storageLowerBounds!;
        var storageStrides = _storageStrides!;
        var linearIndex = _fixedStorageBaseOffset;

        for (var index = 0; index < localIndices.Count; index++)
        {
            var storageDimension = visibleStorageDimensions[index];
            linearIndex = checked(linearIndex + ((localIndices[index] - storageLowerBounds[storageDimension]) * storageStrides[storageDimension]));
        }

        return linearIndex;
    }

    private int GetLinearIndex(ReadOnlySpan<int> localIndices)
    {
        if (!TryGetLinearIndex(localIndices, out var linearIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(localIndices), "Indices must stay within array bounds.");
        }

        return linearIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLinearIndex(ReadOnlySpan<int> localIndices, out int linearIndex)
    {
        if (localIndices.Length != Rank)
        {
            linearIndex = 0;
            return false;
        }

        var lowerBounds = _lowerBounds!;
        var upperBounds = _upperBounds!;
        var visibleStorageDimensions = GetVisibleStorageDimensions();
        var storageLowerBounds = _storageLowerBounds!;
        var storageStrides = _storageStrides!;
        linearIndex = _fixedStorageBaseOffset;

        for (var index = 0; index < localIndices.Length; index++)
        {
            var localIndex = localIndices[index];
            if (localIndex < lowerBounds[index] || localIndex > upperBounds[index])
            {
                linearIndex = 0;
                return false;
            }

            var storageDimension = visibleStorageDimensions[index];
            linearIndex = checked(linearIndex + ((localIndex - storageLowerBounds[storageDimension]) * storageStrides[storageDimension]));
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLinearIndex1(int index0, out int linearIndex)
    {
        if (Rank != 1)
        {
            linearIndex = 0;
            return false;
        }

        var lowerBounds = _lowerBounds!;
        var upperBounds = _upperBounds!;

        if (index0 < lowerBounds[0] || index0 > upperBounds[0])
        {
            linearIndex = 0;
            return false;
        }

        var storageDimension0 = GetVisibleStorageDimensions()[0];
        linearIndex = checked(
            _fixedStorageBaseOffset
            + ((index0 - _storageLowerBounds![storageDimension0]) * _storageStrides![storageDimension0]));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLinearIndex3(int index0, int index1, int index2, out int linearIndex)
    {
        if (Rank != 3)
        {
            linearIndex = 0;
            return false;
        }

        var lowerBounds = _lowerBounds!;
        var upperBounds = _upperBounds!;

        if (index0 < lowerBounds[0] || index0 > upperBounds[0]
            || index1 < lowerBounds[1] || index1 > upperBounds[1]
            || index2 < lowerBounds[2] || index2 > upperBounds[2])
        {
            linearIndex = 0;
            return false;
        }

        var visibleStorageDimensions = GetVisibleStorageDimensions();
        var storageLowerBounds = _storageLowerBounds!;
        var storageStrides = _storageStrides!;
        var storageDimension0 = visibleStorageDimensions[0];
        var storageDimension1 = visibleStorageDimensions[1];
        var storageDimension2 = visibleStorageDimensions[2];

        linearIndex = checked(
            _fixedStorageBaseOffset
            + ((index0 - storageLowerBounds[storageDimension0]) * storageStrides[storageDimension0])
            + ((index1 - storageLowerBounds[storageDimension1]) * storageStrides[storageDimension1])
            + ((index2 - storageLowerBounds[storageDimension2]) * storageStrides[storageDimension2]));
        return true;
    }

    private int?[] GetFixedStorageIndices()
    {
        return _fixedStorageIndices ?? CreateEmptyFixedStorageIndices(_storageLowerBounds?.Length ?? Rank);
    }

    private int[] GetVisibleStorageDimensions()
    {
        return _visibleStorageDimensions ?? CreateIdentityStorageDimensions(Rank);
    }

    private static int?[] CreateEmptyFixedStorageIndices(int rank) => new int?[rank];

    private static int[] CreateIdentityStorageDimensions(int rank)
    {
        var dimensions = new int[rank];

        for (var index = 0; index < dimensions.Length; index++)
        {
            dimensions[index] = index;
        }

        return dimensions;
    }

    private void RefreshShapeMetadata()
    {
        _visibleLength = _lowerBounds is not null && _upperBounds is not null
            ? CalculateLength(_lowerBounds, _upperBounds)
            : 0;
        _storageStrides = _storageLowerBounds is not null && _storageUpperBounds is not null
            ? CalculateStrides(_storageLowerBounds, _storageUpperBounds)
            : null;
        _fixedStorageBaseOffset = _storageLowerBounds is not null && _storageStrides is not null
            ? CalculateFixedStorageBaseOffset(GetFixedStorageIndices(), _storageLowerBounds, _storageStrides)
            : 0;
    }

    private static void ValidateVisibleRange(int lowerBound, int upperBound, int visibleLowerBound, int visibleUpperBound)
    {
        if (lowerBound < visibleLowerBound || lowerBound > visibleUpperBound)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerBound), "Range lower bound must stay within array bounds.");
        }

        if (upperBound < visibleLowerBound || upperBound > visibleUpperBound)
        {
            throw new ArgumentOutOfRangeException(nameof(upperBound), "Range upper bound must stay within array bounds.");
        }
    }

    private static int GetLinearIndex(
        IReadOnlyList<int> indices,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds)
    {
        return GetLinearIndexWithStrides(indices, lowerBounds, CalculateStrides(lowerBounds, upperBounds));
    }

    private static int GetLinearIndexWithStrides(
        IReadOnlyList<int> indices,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> strides)
    {
        if (indices.Count != lowerBounds.Count || indices.Count != strides.Count)
        {
            throw new ArgumentException("Index count must match array rank.", nameof(indices));
        }

        var linearIndex = 0;

        for (var dimension = 0; dimension < indices.Count; dimension++)
        {
            linearIndex = checked(linearIndex + ((indices[dimension] - lowerBounds[dimension]) * strides[dimension]));
        }

        return linearIndex;
    }

    private static int[] CalculateStrides(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        var strides = new int[lowerBounds.Count];
        var stride = 1;

        for (var dimension = lowerBounds.Count - 1; dimension >= 0; dimension--)
        {
            strides[dimension] = stride;

            if (IsEmptyRange(lowerBounds[dimension], upperBounds[dimension]))
            {
                stride = 0;
                continue;
            }

            var extent = checked(upperBounds[dimension] - lowerBounds[dimension] + 1);
            stride = checked(stride * extent);
        }

        return strides;
    }

    private static int CalculateFixedStorageBaseOffset(
        IReadOnlyList<int?> fixedStorageIndices,
        IReadOnlyList<int> storageLowerBounds,
        IReadOnlyList<int> storageStrides)
    {
        var offset = 0;

        for (var dimension = 0; dimension < storageStrides.Count; dimension++)
        {
            if (fixedStorageIndices[dimension] is { } fixedIndex)
            {
                offset = checked(offset + ((fixedIndex - storageLowerBounds[dimension]) * storageStrides[dimension]));
            }
        }

        return offset;
    }

    private bool IsWithinBounds(IReadOnlyList<int> indices, bool requireFullRank)
    {
        if (!IsAllocated)
        {
            return false;
        }

        if (requireFullRank)
        {
            if (indices.Count != Rank)
            {
                return false;
            }
        }
        else if (indices.Count < 1 || indices.Count > Rank)
        {
            return false;
        }

        for (var dimension = 0; dimension < indices.Count; dimension++)
        {
            if (indices[dimension] < _lowerBounds![dimension] || indices[dimension] > _upperBounds![dimension])
            {
                return false;
            }
        }

        return true;
    }

    private bool IsWithinBounds(ReadOnlySpan<int> indices, bool requireFullRank)
    {
        if (!IsAllocated)
        {
            return false;
        }

        if (requireFullRank)
        {
            if (indices.Length != Rank)
            {
                return false;
            }
        }
        else if (indices.Length < 1 || indices.Length > Rank)
        {
            return false;
        }

        for (var dimension = 0; dimension < indices.Length; dimension++)
        {
            if (indices[dimension] < _lowerBounds![dimension] || indices[dimension] > _upperBounds![dimension])
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureAllocated()
    {
        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }
    }

    private void EnsureResizableRoot()
    {
        if (IsView)
        {
            throw new InvalidOperationException("Array view cannot be resized or deallocated.");
        }
    }

    private void ValidateDimensionNumber(int dimension)
    {
        if (dimension < 1 || dimension > Rank)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), dimension, $"Dimension must be between 1 and {Rank}.");
        }
    }

    private void EnsureOneDimensional()
    {
        if (Rank != 1)
        {
            throw new InvalidOperationException("Single-index access is only valid for one-dimensional arrays.");
        }
    }
}

public readonly record struct RuntimeArrayIndexSlice(bool IsRange, int LowerBound, int UpperBound)
{
    public static RuntimeArrayIndexSlice Single(int index) => new(false, index, index);

    public static RuntimeArrayIndexSlice Range(int lowerBound, int upperBound) => new(true, lowerBound, upperBound);
}
