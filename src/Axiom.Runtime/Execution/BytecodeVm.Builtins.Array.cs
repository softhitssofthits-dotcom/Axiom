using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private int EvaluateLBoundBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "LBOUND", location);
        var dimension = ResolveArrayInfoDimension("LBOUND", array, arguments, location);
        return array.GetLowerBound(dimension);
    }

    private int EvaluateUBoundBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "UBOUND", location);
        var dimension = ResolveArrayInfoDimension("UBOUND", array, arguments, location);
        return array.GetUpperBound(dimension);
    }

    private static int EvaluateArrayLengthBuiltin(RuntimeValue argument)
    {
        return argument.AsArray().Length;
    }

    private static int EvaluateArrayRankBuiltin(RuntimeValue argument)
    {
        return argument.AsArray().Rank;
    }

    private RuntimeValue EvaluateArrayBoundsBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYBOUNDS", location);
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2);
        return RuntimeValue.FromArray(
            resultType,
            RuntimeArrayValue.CreateCompactTwoDimensional(
                TypeSymbol.Integer,
                fixedStringLength: null,
                array.Rank,
                secondLength: 2,
                (dimension, column) => column == 1
                    ? RuntimeValue.FromInteger(array.GetLowerBound(dimension))
                    : RuntimeValue.FromInteger(array.GetUpperBound(dimension))));
    }

    private RuntimeValue EvaluateArrayCloneBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireArrayArgument(argument, "ARRAYCLONE", location);
        return CloneArrayValue((ArrayTypeSymbol)argument.Type, array);
    }

    private RuntimeValue EvaluateArraySliceBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYSLICE", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var start = arguments[1].AsInteger();
        var count = RequireNonNegativeCount("ARRAYSLICE", "count", arguments[2].AsInteger(), location);

        if (array.Rank == 1)
        {
            if (arguments.Count > 3)
                _ = ResolveTransformDimension("ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3, location);

            return EvaluateOneDimensionalArraySliceBuiltin(arrayType, array, start, count, location);
        }

        var dimension = ResolveTransformDimension("ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3, location);

        if (count == 0)
        {
            return BuiltinArrayHelpers.CreateArrayWindowCopy(
                arrayType,
                array,
                BuiltinArrayHelpers.CreateEmptyWindowBounds(array, dimension, start));
        }

        var end = (long)start + count - 1L;
        if (start < array.GetLowerBound(dimension) || end > array.GetUpperBound(dimension))
        {
            ReportBuiltInArgumentError(
                "ARRAYSLICE",
                "start",
                "вместе с 'count' задаёт диапазон вне границ исходного массива.",
                location);
            throw new RuntimeFailureException();
        }

        return BuiltinArrayHelpers.CreateArrayWindowCopy(
            arrayType,
            array,
            BuiltinArrayHelpers.CreateAdjustedBounds(array, dimension, start, (int)end));
    }

    private RuntimeValue EvaluateArrayFlattenBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFLATTEN", location);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var values = new List<RuntimeValue>(array.Length);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices => values.Add(array.GetValue(indices)));

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayReshapeBuiltin(RuntimeValue sourceArgument, RuntimeValue templateArgument, TextLocation location)
    {
        var source = RequireAllocatedArrayArgument(sourceArgument, "ARRAYRESHAPE", location);
        var template = RequireAllocatedArrayArgument(templateArgument, "ARRAYRESHAPE", location);

        if (source.Length != template.Length)
        {
            ReportBuiltInArgumentError(
                "ARRAYRESHAPE",
                "template",
                "должен задавать то же число видимых элементов, что и 'source'.",
                location);
            throw new RuntimeFailureException();
        }

        var resultArrayType = (ArrayTypeSymbol)templateArgument.Type;
        var values = new List<RuntimeValue>(source.Length);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(source, indices => values.Add(source.GetValue(indices)));

        var next = 0;
        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultArrayType,
            template,
            _ => values[next++]);
    }

    private RuntimeValue EvaluateArrayToSortedBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYTOSORTED", location);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension("ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1, location)
            : (int?)null;
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Length == 0)
        {
            return CloneArrayValue(arrayType, array);
        }

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1, location);
            var clone = CloneArrayValue(arrayType, array).AsArray();
            BuiltinArrayHelpers.SortArrayAlongDimension(clone, dimension, (l, r) => Compare(l, r, location));
            return RuntimeValue.FromArray(arrayType, clone);
        }

        var values = new List<(RuntimeValue Value, int OriginalOrder)>(array.Length);
        var originalOrder = 0;

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index =>
        {
            values.Add((array.GetValue(index), originalOrder));
            originalOrder++;
        });

        values.Sort((left, right) =>
        {
            var comparison = Compare(left.Value, right.Value, location);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        return CreateArrayValue(
            arrayType,
            array.LowerBound,
            array.UpperBound,
            index => values[index - array.LowerBound].Value);
    }

    private RuntimeValue EvaluateArrayToReversedBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYTOREVERSED", location);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension("ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1, location)
            : (int?)null;
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Rank == 1)
        {
            var values = new List<RuntimeValue>(array.Length);

            BuiltinArrayHelpers.ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

            return CreateCompactArrayValue(arrayType, values);
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1, location);
        return BuiltinArrayHelpers.CreateReversedArrayCopy(arrayType, array, dimension);
    }

    private RuntimeValue EvaluateArrayTakeBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYTAKE", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount("ARRAYTAKE", "count", arguments[1].AsInteger(), location);

        if (array.Rank == 1)
        {
            if (arguments.Count > 2)
                _ = ResolveTransformDimension("ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2, location);

            return EvaluateOneDimensionalArrayTakeBuiltin(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension("ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2, location);
        var lowerBound = array.GetLowerBound(dimension);
        var extent = BuiltinArrayHelpers.GetDimensionLength(array, dimension);
        var takeCount = Math.Min(count, extent);
        var upperBound = takeCount == 0
            ? lowerBound - 1
            : lowerBound + takeCount - 1;

        return BuiltinArrayHelpers.CreateArrayWindowCopy(
            arrayType,
            array,
            BuiltinArrayHelpers.CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
    }

    private RuntimeValue EvaluateArraySkipBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYSKIP", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount("ARRAYSKIP", "count", arguments[1].AsInteger(), location);

        if (array.Rank == 1)
        {
            if (arguments.Count > 2)
                _ = ResolveTransformDimension("ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2, location);

            return EvaluateOneDimensionalArraySkipBuiltin(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension("ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2, location);
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        var extent = BuiltinArrayHelpers.GetDimensionLength(array, dimension);

        if (count <= 0)
        {
            return BuiltinArrayHelpers.CreateArrayWindowCopy(
                arrayType,
                array,
                BuiltinArrayHelpers.CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
        }

        if (count >= extent)
        {
            return BuiltinArrayHelpers.CreateArrayWindowCopy(
                arrayType,
                array,
                BuiltinArrayHelpers.CreateEmptyWindowBounds(array, dimension, (long)upperBound + 1L));
        }

        return BuiltinArrayHelpers.CreateArrayWindowCopy(
            arrayType,
            array,
            BuiltinArrayHelpers.CreateAdjustedBounds(array, dimension, lowerBound + count, upperBound));
    }

    private RuntimeValue EvaluateArrayDistinctBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYDISTINCT", location);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var values = new List<RuntimeValue>(array.Length);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            var candidate = array.GetValue(indices);
            var alreadyPresent = false;

            foreach (var existing in values)
            {
                if (ValuesEqual(existing, candidate))
                {
                    alreadyPresent = true;
                    break;
                }
            }

            if (!alreadyPresent)
            {
                values.Add(candidate);
            }
        });

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayConcatBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var left = RequireAllocatedArrayArgument(arguments[0], "ARRAYCONCAT", location);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension("ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2, location)
            : (int?)null;
        var right = RequireAllocatedArrayArgument(arguments[1], "ARRAYCONCAT", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (left.Rank == 1)
        {
            if (left.Length == 0)
            {
                return CloneArrayValue(arrayType, right);
            }

            if (right.Length == 0)
            {
                return CloneArrayValue(arrayType, left);
            }

            var values = new RuntimeValue[left.Length + right.Length];
            CopyOneDimensionalArrayRange(left, left.LowerBound, left.Length, values, destinationIndex: 0);
            CopyOneDimensionalArrayRange(right, right.LowerBound, right.Length, values, destinationIndex: left.Length);
            return CreateCompactArrayValue(arrayType, values);
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2, location);
        EnsureCompatibleConcatShapes(left, right, dimension, location);

        if (BuiltinArrayHelpers.GetDimensionLength(left, dimension) == 0)
        {
            return BuiltinArrayHelpers.CreateArrayWindowCopy(arrayType, right, (right.LowerBounds.ToArray(), right.UpperBounds.ToArray()));
        }

        if (BuiltinArrayHelpers.GetDimensionLength(right, dimension) == 0)
        {
            return BuiltinArrayHelpers.CreateArrayWindowCopy(arrayType, left, (left.LowerBounds.ToArray(), left.UpperBounds.ToArray()));
        }

        var resultLowerBounds = left.LowerBounds.ToArray();
        var resultUpperBounds = left.UpperBounds.ToArray();
        var selectedIndex = dimension - 1;
        var resultUpperBound = (long)resultLowerBounds[selectedIndex]
            + BuiltinArrayHelpers.GetDimensionLength(left, dimension)
            + BuiltinArrayHelpers.GetDimensionLength(right, dimension) - 1L;
        if (resultUpperBound > int.MaxValue)
        {
            ReportBuiltInArgumentError(
                "ARRAYCONCAT",
                "right",
                "задаёт верхнюю границу результата вне диапазона INTEGER.",
                location);
            throw new RuntimeFailureException();
        }

        resultUpperBounds[selectedIndex] = (int)resultUpperBound;
        var leftLength = BuiltinArrayHelpers.GetDimensionLength(left, dimension);
        var leftLowerBound = left.GetLowerBound(dimension);
        var rightLowerBound = right.GetLowerBound(dimension);
        var resultLowerBound = resultLowerBounds[selectedIndex];

        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            arrayType,
            resultLowerBounds,
            resultUpperBounds,
            resultIndices =>
            {
                var sourceIndices = resultIndices.ToArray();
                var offset = resultIndices[selectedIndex] - resultLowerBound;

                if (offset < leftLength)
                {
                    sourceIndices[selectedIndex] = leftLowerBound + offset;
                    return left.GetValue(sourceIndices);
                }

                sourceIndices[selectedIndex] = rightLowerBound + (offset - leftLength);
                return right.GetValue(sourceIndices);
            });
    }

    private RuntimeValue EvaluateOneDimensionalArraySliceBuiltin(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue array,
        int start,
        int count,
        TextLocation location)
    {
        if (count == 0)
        {
            return CreateCompactArrayValue(arrayType, []);
        }

        var end = (long)start + count - 1L;
        if (start < array.LowerBound || end > array.UpperBound)
        {
            ReportBuiltInArgumentError(
                "ARRAYSLICE",
                "start",
                "вместе с 'count' задаёт диапазон вне границ исходного массива.",
                location);
            throw new RuntimeFailureException();
        }

        var values = new RuntimeValue[count];
        CopyOneDimensionalArrayRange(array, start, count, values, destinationIndex: 0);

        return CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArrayTakeBuiltin(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count == 0 || array.Length == 0)
        {
            return CreateCompactArrayValue(arrayType, []);
        }

        var takeCount = Math.Min(count, array.Length);
        var values = new RuntimeValue[takeCount];
        CopyOneDimensionalArrayRange(array, array.LowerBound, takeCount, values, destinationIndex: 0);

        return CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArraySkipBuiltin(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count >= array.Length)
        {
            return CreateCompactArrayValue(arrayType, []);
        }

        var remainingCount = array.Length - count;
        var values = new RuntimeValue[remainingCount];
        CopyOneDimensionalArrayRange(array, array.LowerBound + count, remainingCount, values, destinationIndex: 0);

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayFillBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFILL", location);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices => array.SetValue(indices, value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayReverseBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYREVERSE", location);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension("ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1, location)
            : (int?)null;

        if (array.Rank == 1)
        {
            BuiltinArrayHelpers.ReverseOneDimensionalArray(array);
            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1, location);
        BuiltinArrayHelpers.ReverseArrayAlongDimension(array, dimension);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArraySortBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYSORT", location);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension("ARRAYSORT", array, arguments, dimensionArgumentIndex: 1, location)
            : (int?)null;

        if (array.Length <= 1)
        {
            return RuntimeValue.Void;
        }

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYSORT", array, arguments, dimensionArgumentIndex: 1, location);
            BuiltinArrayHelpers.SortArrayAlongDimension(array, dimension, (l, r) => Compare(l, r, location));
            return RuntimeValue.Void;
        }

        var values = new List<(RuntimeValue Value, int OriginalOrder)>(array.Length);
        var originalOrder = 0;

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index =>
        {
            values.Add((array.GetValue(index), originalOrder));
            originalOrder++;
        });

        values.Sort((left, right) =>
        {
            var comparison = Compare(left.Value, right.Value, location);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index => array.SetValue(index, values[index - array.LowerBound].Value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPushBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYPUSH", location);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension("ARRAYPUSH", array, arguments, dimensionArgumentIndex: 2, location)
            : (int?)null;

        if (array.Rank == 1)
        {
            if (!array.TryAppendOneDimensional(arguments[1]))
            {
                var newUpperBound = GetExpandedArrayUpperBound("ARRAYPUSH", array, dimension: 1, location);
                array.AllocatePreserving(array.LowerBound, newUpperBound);
                array.SetValue(newUpperBound, arguments[1]);
            }

            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYPUSH", array, arguments, dimensionArgumentIndex: 2, location);
        var valueArray = RequireAllocatedArrayArgument(arguments[1], "ARRAYPUSH", location);
        EnsureCompatibleDynamicArrayValueShape("ARRAYPUSH", array, valueArray, dimension, location);

        var expandedUpperBound = GetExpandedArrayUpperBound("ARRAYPUSH", array, dimension, location);
        var nextLowerBounds = array.LowerBounds.ToArray();
        var nextUpperBounds = array.UpperBounds.ToArray();
        nextUpperBounds[dimension - 1] = expandedUpperBound;
        array.AllocatePreserving(nextLowerBounds, nextUpperBounds);

        BuiltinArrayHelpers.ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
        {
            var targetIndices = baseIndices.ToArray();
            targetIndices[dimension - 1] = expandedUpperBound;
            array.SetValue(targetIndices, valueArray.GetValue(BuiltinArrayHelpers.RemoveSelectedDimensionIndices(baseIndices, dimension)));
        });

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPopBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYPOP", location);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension("ARRAYPOP", array, arguments, dimensionArgumentIndex: 1, location)
            : (int?)null;

        if (array.Rank == 1)
        {
            if (array.Length == 0)
            {
                ReportBuiltInArgumentError(
                    "ARRAYPOP",
                    "array",
                    "не может быть пустым выделенным массивом.",
                    location);
                throw new RuntimeFailureException();
            }

            if (array.TryPopOneDimensional(out var poppedValue))
            {
                return poppedValue;
            }

            poppedValue = array.GetValue(array.UpperBound);

            if (array.Length == 1)
            {
                array.Allocate(1, 0);
                return poppedValue;
            }

            array.AllocatePreserving(array.LowerBound, array.UpperBound - 1);
            return poppedValue;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYPOP", array, arguments, dimensionArgumentIndex: 1, location);
        var removalIndex = RequireArrayRemovalIndex("ARRAYPOP", array, array.GetUpperBound(dimension), dimension, location, emptyParameterName: "array");
        var sourceType = (ArrayTypeSymbol)arguments[0].Type;
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceType.ElementType, sourceType.Rank - 1);
        var resultBounds = BuiltinArrayHelpers.CreateReducedBounds(array, dimension);
        var removed = BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultType,
            resultBounds.LowerBounds,
            resultBounds.UpperBounds,
            reducedIndices => array.GetValue(BuiltinArrayHelpers.ExpandReducedIndices(reducedIndices, array.Rank, dimension, removalIndex)));

        var shrunkBounds = BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension);
        array.AllocatePreserving(shrunkBounds.LowerBounds, shrunkBounds.UpperBounds);
        return removed;
    }

    private RuntimeValue EvaluateArrayInsertBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYINSERT", location);
        var explicitDimension = arguments.Count > 3
            ? ResolveTransformDimension("ARRAYINSERT", array, arguments, dimensionArgumentIndex: 3, location)
            : (int?)null;
        var index = arguments[1].AsInteger();

        if (array.Rank == 1)
        {
            var insertionIndex = RequireArrayInsertionIndex("ARRAYINSERT", array, index, dimension: 1, location);
            var isEmpty = array.Length == 0;
            var newUpperBound = isEmpty ? 1 : GetExpandedArrayUpperBound("ARRAYINSERT", array, dimension: 1, location);
            var previousUpperBound = isEmpty ? 0 : array.UpperBound;

            array.AllocatePreserving(isEmpty ? 1 : array.LowerBound, newUpperBound);

            if (!array.TryMoveOneDimensionalRange(
                sourceIndex: insertionIndex,
                destinationIndex: insertionIndex + 1,
                count: previousUpperBound - insertionIndex + 1))
            {
                for (var currentIndex = previousUpperBound; currentIndex >= insertionIndex; currentIndex--)
                {
                    array.SetValue(currentIndex + 1, array.GetValue(currentIndex));
                }
            }

            array.SetValue(insertionIndex, arguments[2]);
            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYINSERT", array, arguments, dimensionArgumentIndex: 3, location);
        var valueArray = RequireAllocatedArrayArgument(arguments[2], "ARRAYINSERT", location);
        EnsureCompatibleDynamicArrayValueShape("ARRAYINSERT", array, valueArray, dimension, location);
        var insertionDimensionIndex = RequireArrayInsertionIndex("ARRAYINSERT", array, index, dimension, location);
        var selectedDimensionIsEmpty = BuiltinArrayHelpers.GetDimensionLength(array, dimension) == 0;
        var previousDimensionUpperBound = selectedDimensionIsEmpty ? 0 : array.GetUpperBound(dimension);
        var expandedUpperBound = selectedDimensionIsEmpty ? 1 : GetExpandedArrayUpperBound("ARRAYINSERT", array, dimension, location);
        var nextLowerBounds = array.LowerBounds.ToArray();
        var nextUpperBounds = array.UpperBounds.ToArray();
        if (selectedDimensionIsEmpty)
        {
            nextLowerBounds[dimension - 1] = 1;
        }

        nextUpperBounds[dimension - 1] = expandedUpperBound;
        array.AllocatePreserving(nextLowerBounds, nextUpperBounds);

        BuiltinArrayHelpers.ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
        {
            for (var currentIndex = previousDimensionUpperBound; currentIndex >= insertionDimensionIndex; currentIndex--)
            {
                var sourceIndices = baseIndices.ToArray();
                var targetIndices = baseIndices.ToArray();
                sourceIndices[dimension - 1] = currentIndex;
                targetIndices[dimension - 1] = currentIndex + 1;
                array.SetValue(targetIndices, array.GetValue(sourceIndices));
            }

            var insertedIndices = baseIndices.ToArray();
            insertedIndices[dimension - 1] = insertionDimensionIndex;
            array.SetValue(insertedIndices, valueArray.GetValue(BuiltinArrayHelpers.RemoveSelectedDimensionIndices(baseIndices, dimension)));
        });

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayRemoveBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYREMOVE", location);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension("ARRAYREMOVE", array, arguments, dimensionArgumentIndex: 2, location)
            : (int?)null;
        var index = arguments[1].AsInteger();

        if (array.Rank == 1)
        {
            var removalIndex = RequireArrayRemovalIndex("ARRAYREMOVE", array, index, dimension: 1, location, emptyParameterName: "index");
            var removedValue = array.GetValue(removalIndex);

            if (array.Length == 1)
            {
                array.Allocate(1, 0);
                return removedValue;
            }

            if (!array.TryMoveOneDimensionalRange(
                sourceIndex: removalIndex + 1,
                destinationIndex: removalIndex,
                count: array.UpperBound - removalIndex))
            {
                for (var currentIndex = removalIndex; currentIndex < array.UpperBound; currentIndex++)
                {
                    array.SetValue(currentIndex, array.GetValue(currentIndex + 1));
                }
            }

            array.AllocatePreserving(array.LowerBound, array.UpperBound - 1);
            return removedValue;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYREMOVE", array, arguments, dimensionArgumentIndex: 2, location);
        var multidimensionalRemovalIndex = RequireArrayRemovalIndex("ARRAYREMOVE", array, index, dimension, location, emptyParameterName: "index");
        var sourceType = (ArrayTypeSymbol)arguments[0].Type;
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceType.ElementType, sourceType.Rank - 1);
        var resultBounds = BuiltinArrayHelpers.CreateReducedBounds(array, dimension);
        var removed = BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultType,
            resultBounds.LowerBounds,
            resultBounds.UpperBounds,
            reducedIndices => array.GetValue(BuiltinArrayHelpers.ExpandReducedIndices(reducedIndices, array.Rank, dimension, multidimensionalRemovalIndex)));

        var upperBound = array.GetUpperBound(dimension);
        BuiltinArrayHelpers.ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
        {
            for (var currentIndex = multidimensionalRemovalIndex; currentIndex < upperBound; currentIndex++)
            {
                var targetIndices = baseIndices.ToArray();
                var sourceIndices = baseIndices.ToArray();
                targetIndices[dimension - 1] = currentIndex;
                sourceIndices[dimension - 1] = currentIndex + 1;
                array.SetValue(targetIndices, array.GetValue(sourceIndices));
            }
        });

        var shrunkBounds = BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension);
        array.AllocatePreserving(shrunkBounds.LowerBounds, shrunkBounds.UpperBounds);
        return removed;
    }

    private RuntimeValue EvaluateArraySumBuiltin(RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(value, "ARRAYSUM", location);
        return BuiltinArrayAggregateHelpers.Sum(
            array,
            location,
            (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYSUM", "array", detail, errorLocation),
            (message, errorLocation) => ReportResultOverflow(message, errorLocation));
    }

    private RuntimeValue EvaluateArrayMinBuiltin(RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(value, "ARRAYMIN", location);
        return BuiltinArrayAggregateHelpers.Min(array, location, (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYMIN", "array", detail, errorLocation));
    }

    private RuntimeValue EvaluateArrayMaxBuiltin(RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(value, "ARRAYMAX", location);
        return BuiltinArrayAggregateHelpers.Max(array, location, (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYMAX", "array", detail, errorLocation));
    }

    private RuntimeValue EvaluateArrayAverageBuiltin(RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(value, "ARRAYAVERAGE", location);
        return BuiltinArrayAggregateHelpers.Average(
            array,
            location,
            (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYAVERAGE", "array", detail, errorLocation),
            (message, errorLocation) => ReportResultOverflow(message, errorLocation));
    }

    private bool EvaluateArrayContainsBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYCONTAINS", location);
        return BuiltinArrayHelpers.ArrayContainsValue(array, value);
    }

    private RuntimeValue EvaluateArrayIndexOfBuiltin(RuntimeValue argument, RuntimeValue value, bool searchFromEnd, TextLocation location)
    {
        var functionName = searchFromEnd ? "ARRAYLASTINDEXOF" : "ARRAYINDEXOF";
        var array = RequireAllocatedArrayArgument(argument, functionName, location);

        if (array.Rank != 1)
        {
            return EvaluateArrayCoordinateIndexOfBuiltin(array, value, searchFromEnd);
        }

        return RuntimeValue.FromInteger(FindArrayIndex(functionName, array, value, searchFromEnd, location));
    }

    private int EvaluateArrayCountBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYCOUNT", location);
        var count = 0;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (ValuesEqual(array.GetValue(indices), value))
            {
                count++;
            }
        });

        return count;
    }

    private RuntimeValue EvaluateArrayFindAllBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFINDALL", location);

        if (array.Rank != 1)
        {
            return EvaluateArrayCoordinateFindAllBuiltin(array, value);
        }

        var indices = new List<RuntimeValue>(array.Length);

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index =>
        {
            if (ValuesEqual(array.GetValue(index), value))
            {
                indices.Add(RuntimeValue.FromInteger(index));
            }
        });

        return CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), indices);
    }

    private RuntimeValue EvaluateArrayCoordinateIndexOfBuiltin(RuntimeArrayValue array, RuntimeValue value, bool searchFromEnd)
    {
        int[]? foundIndices = null;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (!ValuesEqual(array.GetValue(indices), value))
            {
                return;
            }

            if (searchFromEnd || foundIndices is null)
            {
                foundIndices = indices.ToArray();
            }
        });

        return foundIndices is null
            ? CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
            : CreateCompactArrayValue(
                (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                foundIndices.Select(RuntimeValue.FromInteger).ToArray());
    }

    private RuntimeValue EvaluateArrayCoordinateFindAllBuiltin(RuntimeArrayValue array, RuntimeValue value)
    {
        var matches = new List<int[]>();

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (ValuesEqual(array.GetValue(indices), value))
            {
                matches.Add(indices.ToArray());
            }
        });

        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2);
        var lowerBounds = new[] { 1, 1 };
        var upperBounds = new[] { matches.Count == 0 ? 0 : matches.Count, array.Rank };
        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultArrayType,
            lowerBounds,
            upperBounds,
            indices => RuntimeValue.FromInteger(matches[indices[0] - 1][indices[1] - 1]));
    }

    private RuntimeValue EvaluateArrayBinarySearchBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYBINARYSEARCH", location);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension("ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2, location)
            : (int?)null;

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension("ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2, location);
            var foundCoords = BuiltinArrayHelpers.FindArrayBinarySearchCoordinateSet(array, arguments[1], dimension, (l, r) => Compare(l, r, location));
            return foundCoords is null
                ? CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
                : CreateCompactArrayValue(
                    (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                    foundCoords.Select(RuntimeValue.FromInteger).ToArray());
        }

        return RuntimeValue.FromInteger(BuiltinArrayHelpers.FindArrayBinarySearchIndex(array, arguments[1], (l, r) => Compare(l, r, location)));
    }

    private bool EvaluateArrayEveryBuiltin(RuntimeValue argument, RuntimeValue predicateArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYEVERY", location);
        var predicate = RequireFunctionArgument(predicateArgument, location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayEveryBuiltin(array, predicate, location);
        }

        var every = true;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (every && !InvokeArrayPredicate(predicate, array.GetValue(indices), location))
            {
                every = false;
            }
        });

        return every;
    }

    private bool EvaluateArraySomeBuiltin(RuntimeValue argument, RuntimeValue predicateArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYSOME", location);
        var predicate = RequireFunctionArgument(predicateArgument, location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArraySomeBuiltin(array, predicate, location);
        }

        var any = false;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            if (!any && InvokeArrayPredicate(predicate, array.GetValue(indices), location))
            {
                any = true;
            }
        });

        return any;
    }

    private RuntimeValue EvaluateArrayFilterBuiltin(RuntimeValue argument, RuntimeValue predicateArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFILTER", location);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var predicate = RequireFunctionArgument(predicateArgument, location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayFilterBuiltin(array, resultArrayType, predicate, location);
        }

        var values = new List<RuntimeValue>(array.Length);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            var value = array.GetValue(indices);
            if (InvokeArrayPredicate(predicate, value, location))
            {
                values.Add(value);
            }
        });

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayMapBuiltin(RuntimeValue argument, RuntimeValue selectorArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYMAP", location);
        var selector = RequireFunctionArgument(selectorArgument, location);
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(selector.Function.ReturnType, array.Rank);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayMapBuiltin(array, resultType, selector, location);
        }

        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultType,
            array,
            indices => InvokeArrayCallback(selector, array.GetValue(indices), location));
    }

    private RuntimeValue EvaluateArrayReduceBuiltin(
        RuntimeValue argument,
        RuntimeValue seed,
        RuntimeValue reducerArgument,
        TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYREDUCE", location);
        var reducer = RequireFunctionArgument(reducerArgument, location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayReduceBuiltin(array, seed, reducer, location);
        }

        var accumulator = seed;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
            accumulator = InvokeArrayCallback(reducer, accumulator, array.GetValue(indices), location));

        return accumulator;
    }

    private bool EvaluateOneDimensionalArrayEveryBuiltin(
        RuntimeArrayValue array,
        RuntimeFunctionValue predicate,
        TextLocation location)
    {
        var index = array.LowerBound;
        var upperBound = array.UpperBound;

        while (index <= upperBound)
        {
            if (!InvokeArrayPredicate(predicate, array.GetValue(index), location))
            {
                return false;
            }

            if (index == upperBound)
            {
                break;
            }

            index++;
        }

        return true;
    }

    private bool EvaluateOneDimensionalArraySomeBuiltin(
        RuntimeArrayValue array,
        RuntimeFunctionValue predicate,
        TextLocation location)
    {
        var index = array.LowerBound;
        var upperBound = array.UpperBound;

        while (index <= upperBound)
        {
            if (InvokeArrayPredicate(predicate, array.GetValue(index), location))
            {
                return true;
            }

            if (index == upperBound)
            {
                break;
            }

            index++;
        }

        return false;
    }

    private RuntimeValue EvaluateOneDimensionalArrayFilterBuiltin(
        RuntimeArrayValue array,
        ArrayTypeSymbol resultArrayType,
        RuntimeFunctionValue predicate,
        TextLocation location)
    {
        var values = new List<RuntimeValue>(array.Length);
        var index = array.LowerBound;
        var upperBound = array.UpperBound;

        while (index <= upperBound)
        {
            var value = array.GetValue(index);
            if (InvokeArrayPredicate(predicate, value, location))
            {
                values.Add(value);
            }

            if (index == upperBound)
            {
                break;
            }

            index++;
        }

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateOneDimensionalArrayMapBuiltin(
        RuntimeArrayValue array,
        ArrayTypeSymbol resultType,
        RuntimeFunctionValue selector,
        TextLocation location)
    {
        var result = RuntimeArrayValue.CreateVisibleShape(
            resultType.ElementType,
            fixedStringLength: null,
            array.LowerBounds,
            array.UpperBounds);
        var index = array.LowerBound;
        var upperBound = array.UpperBound;

        while (index <= upperBound)
        {
            result.SetValue(index, InvokeArrayCallback(selector, array.GetValue(index), location));
            if (index == upperBound)
            {
                break;
            }

            index++;
        }

        return RuntimeValue.FromArray(resultType, result);
    }

    private RuntimeValue EvaluateOneDimensionalArrayReduceBuiltin(
        RuntimeArrayValue array,
        RuntimeValue seed,
        RuntimeFunctionValue reducer,
        TextLocation location)
    {
        var accumulator = seed;
        var index = array.LowerBound;
        var upperBound = array.UpperBound;

        while (index <= upperBound)
        {
            accumulator = InvokeArrayCallback(reducer, accumulator, array.GetValue(index), location);
            if (index == upperBound)
            {
                break;
            }

            index++;
        }

        return accumulator;
    }

    private RuntimeArrayValue RequireArrayArgument(RuntimeValue argument, string functionName, TextLocation location)
    {
        if (argument.Type is not ArrayTypeSymbol)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Функция {functionName}: аргумент 'array' не содержит массива времени выполнения.",
                location);
        }

        return argument.AsArray();
    }

    private RuntimeFunctionValue RequireFunctionArgument(RuntimeValue argument, TextLocation location)
    {
        var functionValue = argument.AsFunction();
        if (functionValue is not null)
        {
            return functionValue;
        }

        ReportEmptyFunctionValue(location);
        throw new RuntimeFailureException();
    }

    private bool InvokeArrayPredicate(RuntimeFunctionValue predicate, RuntimeValue value, TextLocation location)
    {
        return InvokeArrayCallback(predicate, value, location).AsBoolean();
    }

    private RuntimeValue InvokeArrayCallback(RuntimeFunctionValue callback, RuntimeValue value, TextLocation location)
    {
        ReadOnlySpan<RuntimeValue> arguments = [value];
        return InvokeBytecodeFunction(callback, arguments, location);
    }

    private RuntimeValue InvokeArrayCallback(
        RuntimeFunctionValue callback,
        RuntimeValue first,
        RuntimeValue second,
        TextLocation location)
    {
        ReadOnlySpan<RuntimeValue> arguments = [first, second];
        return InvokeBytecodeFunction(callback, arguments, location);
    }

    private RuntimeArrayValue RequireAllocatedArrayArgument(RuntimeValue argument, string functionName, TextLocation location)
    {
        var array = RequireArrayArgument(argument, functionName, location);
        if (!array.IsAllocated)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Массив 'array' объявлен, но ещё не выделен.",
                location);
        }

        return array;
    }

    private RuntimeArrayValue RequireResizableArrayArgument(RuntimeValue argument, string functionName, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, functionName, location);

        if (array.IsView)
        {
            ReportBuiltInArgumentError(
                functionName,
                "array",
                "не поддерживает подмассивы-представления при изменении размера.",
                location);
            throw new RuntimeFailureException();
        }

        return array;
    }

    private int GetExpandedArrayUpperBound(string functionName, RuntimeArrayValue array, int dimension, TextLocation location)
    {
        try
        {
            return checked(array.GetUpperBound(dimension) + 1);
        }
        catch (OverflowException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "array",
                "не может быть расширен дальше текущей верхней границы.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private int RequireArrayInsertionIndex(string functionName, RuntimeArrayValue array, int index, int dimension, TextLocation location)
    {
        if (BuiltinArrayHelpers.GetDimensionLength(array, dimension) == 0)
        {
            if (index == 1)
            {
                return index;
            }

            ReportBuiltInArgumentError(
                functionName,
                "index",
                dimension == 1
                    ? "для пустого выделенного массива должен быть равен 1."
                    : "для пустого выделенного диапазона выбранного измерения должен быть равен 1.",
                location);
            throw new RuntimeFailureException();
        }

        var lowerBound = array.GetLowerBound(dimension);
        var allowedUpperBound = (long)array.GetUpperBound(dimension) + 1;

        if (index < lowerBound || index > allowedUpperBound)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "должен попадать в диапазон от текущей нижней границы до позиции сразу после текущего последнего элемента.",
                location);
            throw new RuntimeFailureException();
        }

        return index;
    }

    private int RequireArrayRemovalIndex(string functionName, RuntimeArrayValue array, int index, int dimension, TextLocation location, string emptyParameterName)
    {
        if (BuiltinArrayHelpers.GetDimensionLength(array, dimension) == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                emptyParameterName,
                emptyParameterName == "array"
                    ? "не может работать с пустым выделенным диапазоном выбранного измерения."
                    : dimension == 1
                        ? "нельзя удалять элемент из пустого выделенного массива."
                        : "нельзя удалять элемент из пустого выделенного диапазона выбранного измерения.",
                location);
            throw new RuntimeFailureException();
        }

        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);

        if (index < lowerBound || index > upperBound)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "должен указывать на существующий логический индекс массива.",
                location);
            throw new RuntimeFailureException();
        }

        return index;
    }

    private void EnsureCompatibleDynamicArrayValueShape(
        string functionName,
        RuntimeArrayValue source,
        RuntimeArrayValue value,
        int dimension,
        TextLocation location)
    {
        if (value.Rank != source.Rank - 1)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "должен иметь ранг на единицу меньше исходного массива.",
                location);
            throw new RuntimeFailureException();
        }

        var valueDimension = 1;
        for (var sourceDimension = 1; sourceDimension <= source.Rank; sourceDimension++)
        {
            if (sourceDimension == dimension)
            {
                continue;
            }

            if (source.GetLowerBound(sourceDimension) != value.GetLowerBound(valueDimension)
                || source.GetUpperBound(sourceDimension) != value.GetUpperBound(valueDimension))
            {
                ReportBuiltInArgumentError(
                    functionName,
                    "value",
                    "должен иметь видимую форму, согласованную со всеми невыбранными измерениями исходного массива.",
                    location);
                throw new RuntimeFailureException();
            }

            valueDimension++;
        }
    }

    private int FindArrayIndex(
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        bool searchFromEnd,
        TextLocation location)
    {
        int? foundIndex = null;

        if (searchFromEnd)
        {
            BuiltinArrayHelpers.ForEachArrayIndexDescending(array, index =>
            {
                if (foundIndex is null && ValuesEqual(array.GetValue(index), value))
                {
                    foundIndex = index;
                }
            });
        }
        else
        {
            BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index =>
            {
                if (foundIndex is null && ValuesEqual(array.GetValue(index), value))
                {
                    foundIndex = index;
                }
            });
        }

        return foundIndex ?? GetArrayNotFoundIndex(functionName, array, location);
    }

    private int GetArrayNotFoundIndex(string functionName, RuntimeArrayValue array, TextLocation location)
    {
        try
        {
            return checked(array.LowerBound - 1);
        }
        catch (OverflowException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "array",
                "имеет нижнюю границу, для которой невозможно представить значение \"не найдено\".",
                location);
            throw new RuntimeFailureException();
        }
    }

    private static RuntimeValue CreateArrayValue(
        ArrayTypeSymbol arrayType,
        int lowerBound,
        int upperBound,
        Func<int, RuntimeValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var array = new RuntimeArrayValue(arrayType.ElementType);
        array.Allocate(lowerBound, upperBound);

        BuiltinArrayHelpers.ForEachIndexAscending(lowerBound, upperBound, index => array.SetValue(index, valueFactory(index)));

        return RuntimeValue.FromArray(arrayType, array);
    }

    private static RuntimeValue CreateCompactArrayValue(ArrayTypeSymbol arrayType, IReadOnlyList<RuntimeValue> values)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(values);

        return RuntimeValue.FromArray(
            arrayType,
            RuntimeArrayValue.CreateCompactOneDimensional(
                arrayType.ElementType,
                fixedStringLength: null,
                values));
    }

    private static RuntimeValue CloneArrayValue(ArrayTypeSymbol arrayType, RuntimeArrayValue source)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(source);

        if (!source.IsAllocated)
        {
            return BuiltinArrayHelpers.CreateUnallocatedArrayValue(arrayType);
        }

        return RuntimeValue.FromArray(arrayType, source.Clone());
    }

    private static void CopyOneDimensionalArrayRange(
        RuntimeArrayValue source,
        int sourceIndex,
        int count,
        RuntimeValue[] destination,
        int destinationIndex)
    {
        if (count <= 0)
        {
            return;
        }

        if (source.TryCopyOneDimensionalRangeTo(sourceIndex, count, destination, destinationIndex))
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            destination[destinationIndex + index] = source.GetValue(sourceIndex + index);
        }
    }

    private int ResolveArrayInfoDimension(
        string functionName,
        RuntimeArrayValue array,
        IReadOnlyList<RuntimeValue> arguments,
        TextLocation location)
    {
        if (arguments.Count < 2)
        {
            return 1;
        }

        var dimension = ToArrayBound(arguments[1], location, $"Номер измерения функции {functionName}");
        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"должен быть в диапазоне от 1 до {array.Rank}.",
                location);
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private int ResolveTransformDimension(
        string functionName,
        RuntimeArrayValue array,
        IReadOnlyList<RuntimeValue> arguments,
        int dimensionArgumentIndex,
        TextLocation location)
    {
        if (arguments.Count <= dimensionArgumentIndex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"для массива ранга {array.Rank} нужно явно указать измерение.",
                location);
            throw new RuntimeFailureException();
        }

        var dimension = ToArrayBound(arguments[dimensionArgumentIndex], location, $"Номер измерения функции {functionName}");
        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"должен быть в диапазоне от 1 до {array.Rank}.",
                location);
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private void EnsureCompatibleConcatShapes(RuntimeArrayValue left, RuntimeArrayValue right, int dimension, TextLocation location)
    {
        if (left.Rank != right.Rank)
        {
            ReportBuiltInArgumentError(
                "ARRAYCONCAT",
                "right",
                "должен иметь тот же ранг, что и аргумент 'left'.",
                location);
            throw new RuntimeFailureException();
        }

        for (var currentDimension = 1; currentDimension <= left.Rank; currentDimension++)
        {
            if (currentDimension == dimension)
            {
                continue;
            }

            if (left.GetLowerBound(currentDimension) != right.GetLowerBound(currentDimension)
                || left.GetUpperBound(currentDimension) != right.GetUpperBound(currentDimension))
            {
                ReportBuiltInArgumentError(
                    "ARRAYCONCAT",
                    "right",
                    "должен иметь совместимую видимую форму по всем измерениям, кроме выбранного.",
                    location);
                throw new RuntimeFailureException();
            }
        }
    }
}
