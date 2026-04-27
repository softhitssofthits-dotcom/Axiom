using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeArrayValue : IRuntimeStoredValue
{
    private RuntimeValue[]? _elements;
    private readonly int[] _unallocatedLowerBounds;
    private readonly int[] _unallocatedUpperBounds;
    private int[]? _lowerBounds;
    private int[]? _upperBounds;

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

    public TypeSymbol ElementType { get; }

    public int? FixedStringLength { get; }

    public int Rank { get; }

    public IReadOnlyList<int> LowerBounds => IsAllocated ? _lowerBounds! : _unallocatedLowerBounds;

    public IReadOnlyList<int> UpperBounds => IsAllocated ? _upperBounds! : _unallocatedUpperBounds;

    public int LowerBound => LowerBounds[0];

    public int UpperBound => UpperBounds[0];

    public bool IsAllocated => _elements is not null;

    public int Length => IsAllocated ? _elements!.Length : 0;

    public bool ContainsIndex(int index) => Rank == 1 && ContainsIndices([index]);

    public bool ContainsIndices(IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        if (!IsAllocated || indices.Count != Rank)
        {
            return false;
        }

        for (var dimension = 0; dimension < Rank; dimension++)
        {
            if (indices[dimension] < _lowerBounds![dimension] || indices[dimension] > _upperBounds![dimension])
            {
                return false;
            }
        }

        return true;
    }

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

        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }

        return _elements![GetLinearIndex(indices, _lowerBounds!, _upperBounds!)];
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

        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }

        _elements![GetLinearIndex(indices, _lowerBounds!, _upperBounds!)] = value.CopyForStorage(FixedStringLength);
    }

    public void Allocate(int lowerBound, int upperBound)
    {
        EnsureOneDimensional();
        Allocate([lowerBound], [upperBound]);
    }

    public void Allocate(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        ValidateBounds(lowerBounds, upperBounds, Rank);

        _lowerBounds = lowerBounds.ToArray();
        _upperBounds = upperBounds.ToArray();
        _elements = CreateDefaultElements(_lowerBounds, _upperBounds);
    }

    public void AllocatePreserving(int lowerBound, int upperBound)
    {
        EnsureOneDimensional();
        AllocatePreserving([lowerBound], [upperBound]);
    }

    public void AllocatePreserving(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        ValidateBounds(lowerBounds, upperBounds, Rank);

        var previousElements = _elements;
        var previousLowerBounds = _lowerBounds;
        var previousUpperBounds = _upperBounds;
        var nextLowerBounds = lowerBounds.ToArray();
        var nextUpperBounds = upperBounds.ToArray();
        var newElements = CreateDefaultElements(nextLowerBounds, nextUpperBounds);

        if (previousElements is not null && previousLowerBounds is not null && previousUpperBounds is not null)
        {
            CopyIntersection(previousElements, previousLowerBounds, previousUpperBounds, newElements, nextLowerBounds, nextUpperBounds);
        }

        _lowerBounds = nextLowerBounds;
        _upperBounds = nextUpperBounds;
        _elements = newElements;
    }

    public void Deallocate()
    {
        _lowerBounds = null;
        _upperBounds = null;
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
        clone._elements = new RuntimeValue[_elements!.Length];

        for (var index = 0; index < _elements.Length; index++)
        {
            clone._elements[index] = _elements[index].CopyForStorage(clone.FixedStringLength);
        }

        return clone;
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

        var indices = new int[Rank];
        CopyIntersectionRecursive(
            dimension: 0,
            indices,
            sourceElements,
            sourceLowerBounds,
            sourceUpperBounds,
            targetElements,
            targetLowerBounds,
            targetUpperBounds,
            copyLowerBounds,
            copyUpperBounds);
    }

    private void CopyIntersectionRecursive(
        int dimension,
        int[] indices,
        RuntimeValue[] sourceElements,
        IReadOnlyList<int> sourceLowerBounds,
        IReadOnlyList<int> sourceUpperBounds,
        RuntimeValue[] targetElements,
        IReadOnlyList<int> targetLowerBounds,
        IReadOnlyList<int> targetUpperBounds,
        IReadOnlyList<int> copyLowerBounds,
        IReadOnlyList<int> copyUpperBounds)
    {
        if (dimension == Rank)
        {
            var sourceIndex = GetLinearIndex(indices, sourceLowerBounds, sourceUpperBounds);
            var targetIndex = GetLinearIndex(indices, targetLowerBounds, targetUpperBounds);
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
                sourceUpperBounds,
                targetElements,
                targetLowerBounds,
                targetUpperBounds,
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
        if (IsEmptyRange(lowerBound, upperBound))
        {
            return;
        }

        if (upperBound < lowerBound)
        {
            throw new ArgumentOutOfRangeException(
                nameof(upperBound),
                upperBound,
                "Upper bound must not be less than lower bound, except for the canonical empty range 1 TO 0.");
        }
    }

    private static bool IsEmptyRange(int lowerBound, int upperBound) => lowerBound == 1 && upperBound == 0;

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

    private int GetLinearIndex(
        IReadOnlyList<int> indices,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds)
    {
        if (indices.Count != Rank)
        {
            throw new ArgumentException("Index count must match array rank.", nameof(indices));
        }

        var linearIndex = 0;

        for (var dimension = 0; dimension < Rank; dimension++)
        {
            var extent = IsEmptyRange(lowerBounds[dimension], upperBounds[dimension])
                ? 0
                : checked(upperBounds[dimension] - lowerBounds[dimension] + 1);
            linearIndex = checked(linearIndex * extent + (indices[dimension] - lowerBounds[dimension]));
        }

        return linearIndex;
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
