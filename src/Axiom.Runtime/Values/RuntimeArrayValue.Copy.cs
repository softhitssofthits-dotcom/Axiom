using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
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
        clone.AttachStorage(clone.CreateDefaultElements(clone._storageLowerBounds, clone._storageUpperBounds));
        clone.RefreshShapeMetadata();

        if (RequiresElementCopy(clone.FixedStringLength) || !TryCopyVisibleValuesToClone(clone))
        {
            clone.CopyVisibleValuesFrom(this);
        }

        return clone;
    }

    private bool RequiresElementCopy(int? fixedStringLength)
    {
        return fixedStringLength is not null || ElementType is UserDefinedTypeSymbol;
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

        return TryCopyOneDimensionalRangeTo(LowerBound, Length, clone._elements!, destinationIndex: 0);
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
            Array.Copy(sourceElements, sourceOffset, targetElements, targetOffset, copyCount);
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
}
