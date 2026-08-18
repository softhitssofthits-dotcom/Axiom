using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinArrayHelpers
{
    public static RuntimeValue CreateUnallocatedArrayValue(ArrayTypeSymbol arrayType)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        return RuntimeValue.FromArray(arrayType, new RuntimeArrayValue(arrayType.ElementType, rank: arrayType.Rank));
    }

    public static RuntimeValue CreateArrayValue(
        ArrayTypeSymbol arrayType,
        int lowerBound,
        int upperBound,
        Func<int, RuntimeValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var array = new RuntimeArrayValue(arrayType.ElementType, rank: arrayType.Rank);
        array.Allocate(lowerBound, upperBound);

        ForEachIndexAscending(lowerBound, upperBound, index => array.SetValue(index, valueFactory(index)));

        return RuntimeValue.FromArray(arrayType, array);
    }

    public static RuntimeValue CreateCompactArrayValue(ArrayTypeSymbol arrayType, IReadOnlyList<RuntimeValue> values)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(values);

        return CreateArrayValue(
            arrayType,
            lowerBound: 1,
            upperBound: values.Count == 0 ? 0 : values.Count,
            index => values[index - 1]);
    }

    public static RuntimeValue CreateVisibleShapeArrayValue(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        Func<IReadOnlyList<int>, RuntimeValue> valueFactory)
    {
        return CreateVisibleShapeArrayValue(arrayType, source.LowerBounds, source.UpperBounds, valueFactory);
    }

    public static RuntimeValue CreateVisibleShapeArrayValue(
        ArrayTypeSymbol arrayType,
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds,
        Func<IReadOnlyList<int>, RuntimeValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var array = RuntimeArrayValue.CreateVisibleShape(
            arrayType.ElementType,
            fixedStringLength: null,
            lowerBounds,
            upperBounds);

        ForEachIndexSetAscending(lowerBounds, upperBounds, indices => array.SetValue(indices, valueFactory(indices)));

        return RuntimeValue.FromArray(arrayType, array);
    }

    public static RuntimeValue CloneArrayValue(ArrayTypeSymbol arrayType, RuntimeArrayValue source)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(source);

        if (!source.IsAllocated)
        {
            return CreateUnallocatedArrayValue(arrayType);
        }

        return source.Rank == 1
            ? CreateArrayValue(arrayType, source.LowerBound, source.UpperBound, source.GetValue)
            : RuntimeValue.FromArray(arrayType, source.Clone());
    }

    public static RuntimeValue CreateArrayWindowCopy(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        (int[] LowerBounds, int[] UpperBounds) bounds)
    {
        return CreateVisibleShapeArrayValue(arrayType, bounds.LowerBounds, bounds.UpperBounds, source.GetValue);
    }

    public static RuntimeValue CreateReversedArrayCopy(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        int dimension)
    {
        var selectedIndex = dimension - 1;
        var lowerBounds = source.LowerBounds.ToArray();
        var upperBounds = source.UpperBounds.ToArray();
        var lowerBound = lowerBounds[selectedIndex];
        var upperBound = upperBounds[selectedIndex];

        return CreateVisibleShapeArrayValue(
            arrayType,
            lowerBounds,
            upperBounds,
            indices =>
            {
                var sourceIndices = indices.ToArray();
                var offset = indices[selectedIndex] - lowerBound;
                sourceIndices[selectedIndex] = upperBound - offset;
                return source.GetValue(sourceIndices);
            });
    }

    public static void ReverseOneDimensionalArray(RuntimeArrayValue array)
    {
        var left = array.LowerBound;
        var right = array.UpperBound;

        while (left < right)
        {
            var leftValue = array.GetValue(left);
            var rightValue = array.GetValue(right);
            array.SetValue(left, rightValue);
            array.SetValue(right, leftValue);
            left++;
            right--;
        }
    }

    public static void ReverseArrayAlongDimension(RuntimeArrayValue array, int dimension)
    {
        var selectedIndex = dimension - 1;
        var lowerBounds = array.LowerBounds.ToArray();
        var upperBounds = array.UpperBounds.ToArray();
        var lowerBound = lowerBounds[selectedIndex];
        var upperBound = upperBounds[selectedIndex];

        if (lowerBound >= upperBound)
        {
            return;
        }

        var fixedLowerBounds = lowerBounds.ToArray();
        var fixedUpperBounds = upperBounds.ToArray();
        fixedLowerBounds[selectedIndex] = lowerBound;
        fixedUpperBounds[selectedIndex] = lowerBound;

        ForEachIndexSetAscending(fixedLowerBounds, fixedUpperBounds, baseIndices =>
        {
            var left = lowerBound;
            var right = upperBound;

            while (left < right)
            {
                var leftIndices = baseIndices.ToArray();
                var rightIndices = baseIndices.ToArray();
                leftIndices[selectedIndex] = left;
                rightIndices[selectedIndex] = right;

                var leftValue = array.GetValue(leftIndices);
                var rightValue = array.GetValue(rightIndices);
                array.SetValue(leftIndices, rightValue);
                array.SetValue(rightIndices, leftValue);
                left++;
                right--;
            }
        });
    }

    public static int GetDimensionLength(RuntimeArrayValue array, int dimension)
    {
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        return upperBound < lowerBound ? 0 : upperBound - lowerBound + 1;
    }

    public static (int[] LowerBounds, int[] UpperBounds) CreateAdjustedBounds(
        RuntimeArrayValue array,
        int dimension,
        int selectedLowerBound,
        int selectedUpperBound)
    {
        var lowerBounds = array.LowerBounds.ToArray();
        var upperBounds = array.UpperBounds.ToArray();
        var selectedIndex = dimension - 1;
        lowerBounds[selectedIndex] = selectedLowerBound;
        upperBounds[selectedIndex] = selectedUpperBound;
        return (lowerBounds, upperBounds);
    }

    public static (int[] LowerBounds, int[] UpperBounds) CreateReducedBounds(RuntimeArrayValue array, int dimension)
    {
        var lowerBounds = new int[array.Rank - 1];
        var upperBounds = new int[array.Rank - 1];
        var next = 0;

        for (var currentDimension = 1; currentDimension <= array.Rank; currentDimension++)
        {
            if (currentDimension == dimension)
            {
                continue;
            }

            lowerBounds[next] = array.GetLowerBound(currentDimension);
            upperBounds[next] = array.GetUpperBound(currentDimension);
            next++;
        }

        return (lowerBounds, upperBounds);
    }

    public static (int[] LowerBounds, int[] UpperBounds) CreateBoundsAfterDimensionShrink(RuntimeArrayValue array, int dimension)
    {
        var lowerBounds = array.LowerBounds.ToArray();
        var upperBounds = array.UpperBounds.ToArray();
        var selectedIndex = dimension - 1;

        if (GetDimensionLength(array, dimension) == 1)
        {
            lowerBounds[selectedIndex] = 1;
            upperBounds[selectedIndex] = 0;
            return (lowerBounds, upperBounds);
        }

        upperBounds[selectedIndex]--;
        return (lowerBounds, upperBounds);
    }

    public static int[] RemoveSelectedDimensionIndices(IReadOnlyList<int> indices, int dimension)
    {
        var result = new int[indices.Count - 1];
        var next = 0;

        for (var index = 0; index < indices.Count; index++)
        {
            if (index == dimension - 1)
            {
                continue;
            }

            result[next++] = indices[index];
        }

        return result;
    }

    public static int[] ExpandReducedIndices(IReadOnlyList<int> reducedIndices, int rank, int dimension, int selectedValue)
    {
        var result = new int[rank];
        var reducedIndex = 0;

        for (var index = 0; index < rank; index++)
        {
            if (index == dimension - 1)
            {
                result[index] = selectedValue;
                continue;
            }

            result[index] = reducedIndices[reducedIndex++];
        }

        return result;
    }

    public static bool ArrayContainsValue(RuntimeArrayValue array, RuntimeValue value)
    {
        var found = false;

        ForEachArrayIndexSetAscending(array, indices =>
        {
            if (!found && ValuesEqual(array.GetValue(indices), value))
            {
                found = true;
            }
        });

        return found;
    }

    public static void ForEachArrayLineBaseIndicesAscending(RuntimeArrayValue array, int dimension, Action<IReadOnlyList<int>> action)
    {
        if (array.Length == 0)
        {
            return;
        }

        var selectedIndex = dimension - 1;
        var fixedLowerBounds = array.LowerBounds.ToArray();
        var fixedUpperBounds = array.UpperBounds.ToArray();
        fixedLowerBounds[selectedIndex] = array.GetLowerBound(dimension);
        fixedUpperBounds[selectedIndex] = array.GetLowerBound(dimension);
        ForEachIndexSetAscending(fixedLowerBounds, fixedUpperBounds, action);
    }

    public static void ForEachArrayLineBaseIndicesForResize(RuntimeArrayValue array, int dimension, Action<IReadOnlyList<int>> action)
    {
        var selectedIndex = dimension - 1;
        var fixedLowerBounds = array.LowerBounds.ToArray();
        var fixedUpperBounds = array.UpperBounds.ToArray();
        fixedLowerBounds[selectedIndex] = array.GetLowerBound(dimension);
        fixedUpperBounds[selectedIndex] = array.GetLowerBound(dimension);
        ForEachIndexSetAscending(fixedLowerBounds, fixedUpperBounds, action);
    }

    public static void ForEachArrayIndexAscending(RuntimeArrayValue array, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        ForEachIndexAscending(array.LowerBound, array.UpperBound, action);
    }

    public static void ForEachArrayIndexDescending(RuntimeArrayValue array, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        ForEachIndexDescending(array.UpperBound, array.LowerBound, action);
    }

    public static void ForEachArrayIndexSetAscending(RuntimeArrayValue array, Action<IReadOnlyList<int>> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        array.ForEachVisibleIndexSetAscending(action);
    }

    public static void ForEachIndexAscending(int lowerBound, int upperBound, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (upperBound < lowerBound)
        {
            return;
        }

        var index = lowerBound;

        while (true)
        {
            action(index);

            if (index == upperBound)
            {
                break;
            }

            index++;
        }
    }

    public static void ForEachIndexDescending(int upperBound, int lowerBound, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (upperBound < lowerBound)
        {
            return;
        }

        var index = upperBound;

        while (true)
        {
            action(index);

            if (index == lowerBound)
            {
                break;
            }

            index--;
        }
    }

    public static void ForEachIndexSetAscending(
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds,
        Action<IReadOnlyList<int>> action)
    {
        ArgumentNullException.ThrowIfNull(lowerBounds);
        ArgumentNullException.ThrowIfNull(upperBounds);
        ArgumentNullException.ThrowIfNull(action);

        if (lowerBounds.Count != upperBounds.Count)
        {
            throw new ArgumentException("Lower and upper bounds must have the same rank.");
        }

        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (upperBounds[dimension] < lowerBounds[dimension])
            {
                return;
            }
        }

        var indices = new int[lowerBounds.Count];
        ForEachIndexSetAscendingRecursive(lowerBounds, upperBounds, indices, dimension: 0, action);
    }

    public static void ForEachIndexSetAscendingRecursive(
        IReadOnlyList<int> lowerBounds,
        IReadOnlyList<int> upperBounds,
        int[] indices,
        int dimension,
        Action<IReadOnlyList<int>> action)
    {
        if (dimension == indices.Length)
        {
            action(indices);
            return;
        }

        var index = lowerBounds[dimension];
        var upperBound = upperBounds[dimension];

        while (true)
        {
            indices[dimension] = index;
            ForEachIndexSetAscendingRecursive(lowerBounds, upperBounds, indices, dimension + 1, action);

            if (index == upperBound)
            {
                break;
            }

            index++;
        }
    }

    public static void SortArrayAlongDimension(
        RuntimeArrayValue array,
        int dimension,
        Func<RuntimeValue, RuntimeValue, int> compare)
    {
        if (array.Length <= 1 || GetDimensionLength(array, dimension) <= 1)
        {
            return;
        }

        ForEachArrayLineBaseIndicesAscending(array, dimension, baseIndices =>
        {
            var sortedLine = GetStableSortedLine(array, dimension, baseIndices, compare);
            var selectedIndex = dimension - 1;
            var lowerBound = array.GetLowerBound(dimension);

            for (var offset = 0; offset < sortedLine.Count; offset++)
            {
                var indices = baseIndices.ToArray();
                indices[selectedIndex] = lowerBound + offset;
                array.SetValue(indices, sortedLine[offset]);
            }
        });
    }

    public static List<RuntimeValue> GetStableSortedLine(
        RuntimeArrayValue array,
        int dimension,
        IReadOnlyList<int> baseIndices,
        Func<RuntimeValue, RuntimeValue, int> compare)
    {
        var values = new List<(RuntimeValue Value, int OriginalOrder)>(GetDimensionLength(array, dimension));
        var selectedIndex = dimension - 1;
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        var originalOrder = 0;

        for (var index = lowerBound; index <= upperBound; index++)
        {
            var indices = baseIndices.ToArray();
            indices[selectedIndex] = index;
            values.Add((array.GetValue(indices), originalOrder));
            originalOrder++;
        }

        values.Sort((left, right) =>
        {
            var comparison = compare(left.Value, right.Value);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        return values.Select(static item => item.Value).ToList();
    }

    public static int FindArrayBinarySearchIndex(
        RuntimeArrayValue array,
        RuntimeValue value,
        Func<RuntimeValue, RuntimeValue, int> compare)
    {
        long left = array.LowerBound;
        long right = array.UpperBound;
        int? foundIndex = null;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var middleIndex = (int)middle;
            var comparison = compare(array.GetValue(middleIndex), value);

            if (comparison < 0)
            {
                left = middle + 1;
                continue;
            }

            if (comparison == 0)
            {
                foundIndex = middleIndex;
            }

            right = middle - 1;
        }

        return foundIndex ?? checked(array.LowerBound - 1);
    }

    public static int? FindArrayBinarySearchIndex(
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        IReadOnlyList<int> baseIndices,
        Func<RuntimeValue, RuntimeValue, int> compare)
    {
        var selectedIndex = dimension - 1;
        long left = array.GetLowerBound(dimension);
        long right = array.GetUpperBound(dimension);
        int? foundIndex = null;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var middleIndex = (int)middle;
            var indices = baseIndices.ToArray();
            indices[selectedIndex] = middleIndex;
            var comparison = compare(array.GetValue(indices), value);

            if (comparison < 0)
            {
                left = middle + 1;
                continue;
            }

            if (comparison == 0)
            {
                foundIndex = middleIndex;
            }

            right = middle - 1;
        }

        return foundIndex;
    }

    public static int[]? FindArrayBinarySearchCoordinateSet(
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        Func<RuntimeValue, RuntimeValue, int> compare)
    {
        if (array.Length == 0 || GetDimensionLength(array, dimension) == 0)
        {
            return null;
        }

        int[]? foundIndices = null;
        ForEachArrayLineBaseIndicesAscending(array, dimension, baseIndices =>
        {
            if (foundIndices is not null)
            {
                return;
            }

            var foundIndex = FindArrayBinarySearchIndex(array, value, dimension, baseIndices, compare);
            if (foundIndex is null)
            {
                return;
            }

            foundIndices = baseIndices.ToArray();
            foundIndices[dimension - 1] = foundIndex.Value;
        });

        return foundIndices;
    }

    private static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        if (left.Type != right.Type)
        {
            return false;
        }

        return left.Type switch
        {
            var t when t == TypeSymbol.Integer => left.AsInteger() == right.AsInteger(),
            var t when t == TypeSymbol.Long => left.AsLong() == right.AsLong(),
            var t when t == TypeSymbol.Double => left.AsDouble() == right.AsDouble(),
            var t when t == TypeSymbol.Decimal => left.AsDecimal() == right.AsDecimal(),
            var t when t == TypeSymbol.Boolean => left.AsBoolean() == right.AsBoolean(),
            var t when t == TypeSymbol.String => string.Equals(left.AsString(), right.AsString(), StringComparison.Ordinal),
            _ => false
        };
    }
}
