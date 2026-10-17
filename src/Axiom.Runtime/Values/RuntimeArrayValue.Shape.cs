using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
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

    private static int CalculateLength(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (IsEmptyRange(lowerBounds[dimension], upperBounds[dimension]))
            {
                return 0;
            }
        }

        long length = 1;

        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            var extent = (long)upperBounds[dimension] - lowerBounds[dimension] + 1;

            if (length > MaxElementCount / extent)
            {
                throw new ArrayAllocationFailureException(
                    $"Не удалось выделить массив: число элементов превышает предел {MaxElementCount}.");
            }

            length *= extent;
        }

        return (int)length;
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

        if (upperBound < lowerBound)
        {
            throw new ArgumentOutOfRangeException(nameof(upperBound), "Range upper bound must not be less than its lower bound.");
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

        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (IsEmptyRange(lowerBounds[dimension], upperBounds[dimension]))
            {
                return strides;
            }
        }

        long stride = 1;

        for (var dimension = lowerBounds.Count - 1; dimension >= 0; dimension--)
        {
            strides[dimension] = (int)stride;
            var extent = (long)upperBounds[dimension] - lowerBounds[dimension] + 1;
            stride *= extent;
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
