using Axiom.Core;
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
            return EvaluateOneDimensionalArraySliceBuiltin(arrayType, array, start, count, location);
        }

        var dimension = ResolveTransformDimension("ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3, location);

        if (count == 0)
        {
            return CreateArrayWindowCopyBuiltin(
                arrayType,
                array,
                CreateAdjustedBounds(array, dimension, start, start - 1));
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

        return CreateArrayWindowCopyBuiltin(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, start, (int)end));
    }

    private RuntimeValue EvaluateArrayFlattenBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFLATTEN", location);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexSetAscending(array, indices => values.Add(array.GetValue(indices)));

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

        ForEachArrayIndexSetAscending(source, indices => values.Add(source.GetValue(indices)));

        var next = 0;
        return CreateVisibleShapeArrayValue(
            resultArrayType,
            template,
            _ => values[next++]);
    }

    private RuntimeValue EvaluateArrayToSortedBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYTOSORTED", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Length == 0)
        {
            return CloneArrayValue(arrayType, array);
        }

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension("ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1, location);
            var clone = CloneArrayValue(arrayType, array).AsArray();
            SortArrayAlongDimension(clone, dimension, location);
            return RuntimeValue.FromArray(arrayType, clone);
        }

        var values = new List<(RuntimeValue Value, int OriginalOrder)>(array.Length);
        var originalOrder = 0;

        ForEachArrayIndexAscending(array, index =>
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
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Rank == 1)
        {
            var values = new List<RuntimeValue>(array.Length);

            ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

            return CreateCompactArrayValue(arrayType, values);
        }

        var dimension = ResolveTransformDimension("ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1, location);
        return CreateReversedArrayCopyBuiltin(arrayType, array, dimension);
    }

    private RuntimeValue EvaluateArrayTakeBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYTAKE", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount("ARRAYTAKE", "count", arguments[1].AsInteger(), location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayTakeBuiltin(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension("ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2, location);
        var lowerBound = array.GetLowerBound(dimension);
        var extent = GetDimensionLength(array, dimension);
        var takeCount = Math.Min(count, extent);
        var upperBound = takeCount == 0
            ? lowerBound - 1
            : lowerBound + takeCount - 1;

        return CreateArrayWindowCopyBuiltin(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
    }

    private RuntimeValue EvaluateArraySkipBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYSKIP", location);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount("ARRAYSKIP", "count", arguments[1].AsInteger(), location);

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArraySkipBuiltin(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension("ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2, location);
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        var extent = GetDimensionLength(array, dimension);

        if (count <= 0)
        {
            return CreateArrayWindowCopyBuiltin(
                arrayType,
                array,
                CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
        }

        if (count >= extent)
        {
            return CreateArrayWindowCopyBuiltin(
                arrayType,
                array,
                CreateAdjustedBounds(array, dimension, upperBound + 1, upperBound));
        }

        return CreateArrayWindowCopyBuiltin(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, lowerBound + count, upperBound));
    }

    private RuntimeValue EvaluateArrayDistinctBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYDISTINCT", location);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexSetAscending(array, indices =>
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

        var dimension = ResolveTransformDimension("ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2, location);
        EnsureCompatibleConcatShapes(left, right, dimension, location);

        if (GetDimensionLength(left, dimension) == 0)
        {
            return CreateArrayWindowCopyBuiltin(arrayType, right, (right.LowerBounds.ToArray(), right.UpperBounds.ToArray()));
        }

        if (GetDimensionLength(right, dimension) == 0)
        {
            return CreateArrayWindowCopyBuiltin(arrayType, left, (left.LowerBounds.ToArray(), left.UpperBounds.ToArray()));
        }

        var resultLowerBounds = left.LowerBounds.ToArray();
        var resultUpperBounds = left.UpperBounds.ToArray();
        var selectedIndex = dimension - 1;
        resultUpperBounds[selectedIndex] = checked(resultLowerBounds[selectedIndex] + GetDimensionLength(left, dimension) + GetDimensionLength(right, dimension) - 1);
        var leftLength = GetDimensionLength(left, dimension);
        var leftLowerBound = left.GetLowerBound(dimension);
        var rightLowerBound = right.GetLowerBound(dimension);
        var resultLowerBound = resultLowerBounds[selectedIndex];

        return CreateVisibleShapeArrayValue(
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
                "РІРјРµСЃС‚Рµ СЃ 'count' Р·Р°РґР°С‘С‚ РґРёР°РїР°Р·РѕРЅ РІРЅРµ РіСЂР°РЅРёС† РёСЃС…РѕРґРЅРѕРіРѕ РјР°СЃСЃРёРІР°.",
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

        ForEachArrayIndexSetAscending(array, indices => array.SetValue(indices, value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayReverseBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYREVERSE", location);

        if (array.Rank == 1)
        {
            ReverseOneDimensionalArray(array);
            return RuntimeValue.Void;
        }

        var dimension = ResolveTransformDimension("ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1, location);
        ReverseArrayAlongDimension(array, dimension);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArraySortBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYSORT", location);

        if (array.Length <= 1)
        {
            return RuntimeValue.Void;
        }

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension("ARRAYSORT", array, arguments, dimensionArgumentIndex: 1, location);
            SortArrayAlongDimension(array, dimension, location);
            return RuntimeValue.Void;
        }

        var values = new List<(RuntimeValue Value, int OriginalOrder)>(array.Length);
        var originalOrder = 0;

        ForEachArrayIndexAscending(array, index =>
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

        ForEachArrayIndexAscending(array, index => array.SetValue(index, values[index - array.LowerBound].Value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPushBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYPUSH", location);

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

        var dimension = ResolveTransformDimension("ARRAYPUSH", array, arguments, dimensionArgumentIndex: 2, location);
        var valueArray = RequireAllocatedArrayArgument(arguments[1], "ARRAYPUSH", location);
        EnsureCompatibleDynamicArrayValueShape("ARRAYPUSH", array, valueArray, dimension, location);

        var expandedUpperBound = GetExpandedArrayUpperBound("ARRAYPUSH", array, dimension, location);
        var nextLowerBounds = array.LowerBounds.ToArray();
        var nextUpperBounds = array.UpperBounds.ToArray();
        nextUpperBounds[dimension - 1] = expandedUpperBound;
        array.AllocatePreserving(nextLowerBounds, nextUpperBounds);

        ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
        {
            var targetIndices = baseIndices.ToArray();
            targetIndices[dimension - 1] = expandedUpperBound;
            array.SetValue(targetIndices, valueArray.GetValue(RemoveSelectedDimensionIndices(baseIndices, dimension)));
        });

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPopBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYPOP", location);

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

        var dimension = ResolveTransformDimension("ARRAYPOP", array, arguments, dimensionArgumentIndex: 1, location);
        var removalIndex = RequireArrayRemovalIndex("ARRAYPOP", array, array.GetUpperBound(dimension), dimension, location, emptyParameterName: "array");
        var sourceType = (ArrayTypeSymbol)arguments[0].Type;
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceType.ElementType, sourceType.Rank - 1);
        var resultBounds = CreateReducedBounds(array, dimension);
        var removed = CreateVisibleShapeArrayValue(
            resultType,
            resultBounds.LowerBounds,
            resultBounds.UpperBounds,
            reducedIndices => array.GetValue(ExpandReducedIndices(reducedIndices, array.Rank, dimension, removalIndex)));

        var shrunkBounds = CreateBoundsAfterDimensionShrink(array, dimension);
        array.AllocatePreserving(shrunkBounds.LowerBounds, shrunkBounds.UpperBounds);
        return removed;
    }

    private RuntimeValue EvaluateArrayInsertBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYINSERT", location);
        var index = arguments[1].AsInteger();

        if (array.Rank == 1)
        {
            var insertionIndex = RequireArrayInsertionIndex("ARRAYINSERT", array, index, dimension: 1, location);
            var newUpperBound = GetExpandedArrayUpperBound("ARRAYINSERT", array, dimension: 1, location);
            var previousUpperBound = array.UpperBound;

            array.AllocatePreserving(array.LowerBound, newUpperBound);

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

        var dimension = ResolveTransformDimension("ARRAYINSERT", array, arguments, dimensionArgumentIndex: 3, location);
        var valueArray = RequireAllocatedArrayArgument(arguments[2], "ARRAYINSERT", location);
        EnsureCompatibleDynamicArrayValueShape("ARRAYINSERT", array, valueArray, dimension, location);
        var insertionDimensionIndex = RequireArrayInsertionIndex("ARRAYINSERT", array, index, dimension, location);
        var previousDimensionUpperBound = array.GetUpperBound(dimension);
        var expandedUpperBound = GetExpandedArrayUpperBound("ARRAYINSERT", array, dimension, location);
        var nextLowerBounds = array.LowerBounds.ToArray();
        var nextUpperBounds = array.UpperBounds.ToArray();
        nextUpperBounds[dimension - 1] = expandedUpperBound;
        array.AllocatePreserving(nextLowerBounds, nextUpperBounds);

        ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
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
            array.SetValue(insertedIndices, valueArray.GetValue(RemoveSelectedDimensionIndices(baseIndices, dimension)));
        });

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayRemoveBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireResizableArrayArgument(arguments[0], "ARRAYREMOVE", location);
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

        var dimension = ResolveTransformDimension("ARRAYREMOVE", array, arguments, dimensionArgumentIndex: 2, location);
        var multidimensionalRemovalIndex = RequireArrayRemovalIndex("ARRAYREMOVE", array, index, dimension, location, emptyParameterName: "index");
        var sourceType = (ArrayTypeSymbol)arguments[0].Type;
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceType.ElementType, sourceType.Rank - 1);
        var resultBounds = CreateReducedBounds(array, dimension);
        var removed = CreateVisibleShapeArrayValue(
            resultType,
            resultBounds.LowerBounds,
            resultBounds.UpperBounds,
            reducedIndices => array.GetValue(ExpandReducedIndices(reducedIndices, array.Rank, dimension, multidimensionalRemovalIndex)));

        var upperBound = array.GetUpperBound(dimension);
        ForEachArrayLineBaseIndicesForResize(array, dimension, baseIndices =>
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

        var shrunkBounds = CreateBoundsAfterDimensionShrink(array, dimension);
        array.AllocatePreserving(shrunkBounds.LowerBounds, shrunkBounds.UpperBounds);
        return removed;
    }

    private bool EvaluateArrayContainsBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYCONTAINS", location);
        return ArrayContainsValue(array, value);
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

        ForEachArrayIndexSetAscending(array, indices =>
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

        ForEachArrayIndexAscending(array, index =>
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

        ForEachArrayIndexSetAscending(array, indices =>
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

        ForEachArrayIndexSetAscending(array, indices =>
        {
            if (ValuesEqual(array.GetValue(indices), value))
            {
                matches.Add(indices.ToArray());
            }
        });

        var resultArrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2);
        var lowerBounds = new[] { 1, 1 };
        var upperBounds = new[] { matches.Count == 0 ? 0 : matches.Count, array.Rank };
        return CreateVisibleShapeArrayValue(
            resultArrayType,
            lowerBounds,
            upperBounds,
            indices => RuntimeValue.FromInteger(matches[indices[0] - 1][indices[1] - 1]));
    }

    private RuntimeValue EvaluateArrayBinarySearchBuiltin(IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(arguments[0], "ARRAYBINARYSEARCH", location);

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension("ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2, location);
            return FindArrayBinarySearchCoordinatesBuiltin("ARRAYBINARYSEARCH", array, arguments[1], dimension, location);
        }

        long left = array.LowerBound;
        long right = array.UpperBound;
        int? foundIndex = null;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var middleIndex = (int)middle;
            var comparison = Compare(array.GetValue(middleIndex), arguments[1], location);

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

        return RuntimeValue.FromInteger(foundIndex ?? GetArrayNotFoundIndex("ARRAYBINARYSEARCH", array, location));
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

        ForEachArrayIndexSetAscending(array, indices =>
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

        ForEachArrayIndexSetAscending(array, indices =>
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

        ForEachArrayIndexSetAscending(array, indices =>
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

        return CreateVisibleShapeArrayValue(
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

        ForEachArrayIndexSetAscending(array, indices =>
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
        if (GetDimensionLength(array, dimension) == 0)
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
        if (GetDimensionLength(array, dimension) == 0)
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

    private static (int[] LowerBounds, int[] UpperBounds) CreateReducedBounds(RuntimeArrayValue array, int dimension)
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

    private static (int[] LowerBounds, int[] UpperBounds) CreateBoundsAfterDimensionShrink(RuntimeArrayValue array, int dimension)
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

    private void ForEachArrayLineBaseIndicesForResize(RuntimeArrayValue array, int dimension, Action<IReadOnlyList<int>> action)
    {
        var selectedIndex = dimension - 1;
        var fixedLowerBounds = array.LowerBounds.ToArray();
        var fixedUpperBounds = array.UpperBounds.ToArray();
        fixedLowerBounds[selectedIndex] = array.GetLowerBound(dimension);
        fixedUpperBounds[selectedIndex] = array.GetLowerBound(dimension);
        ForEachIndexSetAscending(fixedLowerBounds, fixedUpperBounds, action);
    }

    private static int[] RemoveSelectedDimensionIndices(IReadOnlyList<int> indices, int dimension)
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

    private static int[] ExpandReducedIndices(IReadOnlyList<int> reducedIndices, int rank, int dimension, int selectedValue)
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

    private bool ArrayContainsValue(RuntimeArrayValue array, RuntimeValue value)
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
            ForEachArrayIndexDescending(array, index =>
            {
                if (foundIndex is null && ValuesEqual(array.GetValue(index), value))
                {
                    foundIndex = index;
                }
            });
        }
        else
        {
            ForEachArrayIndexAscending(array, index =>
            {
                if (foundIndex is null && ValuesEqual(array.GetValue(index), value))
                {
                    foundIndex = index;
                }
            });
        }

        return foundIndex ?? GetArrayNotFoundIndex(functionName, array, location);
    }

    private int? FindArrayBinarySearchIndexBuiltin(
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        IReadOnlyList<int> baseIndices,
        TextLocation location)
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
            var comparison = Compare(array.GetValue(indices), value, location);

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

    private static RuntimeValue CreateUnallocatedArrayValue(ArrayTypeSymbol arrayType)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        return RuntimeValue.FromArray(arrayType, new RuntimeArrayValue(arrayType.ElementType, rank: arrayType.Rank));
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

        ForEachIndexAscending(lowerBound, upperBound, index => array.SetValue(index, valueFactory(index)));

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

    private static RuntimeValue CreateVisibleShapeArrayValue(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        Func<IReadOnlyList<int>, RuntimeValue> valueFactory)
    {
        return CreateVisibleShapeArrayValue(arrayType, source.LowerBounds, source.UpperBounds, valueFactory);
    }

    private static RuntimeValue CreateVisibleShapeArrayValue(
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

    private static RuntimeValue CloneArrayValue(ArrayTypeSymbol arrayType, RuntimeArrayValue source)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(source);

        if (!source.IsAllocated)
        {
            return CreateUnallocatedArrayValue(arrayType);
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
                $"должен быть целым числом от 1 до {array.Rank}.",
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
                $"РґР»СЏ РјР°СЃСЃРёРІР° СЂР°РЅРіР° {array.Rank} РЅСѓР¶РЅРѕ СЏРІРЅРѕ СѓРєР°Р·Р°С‚СЊ РёР·РјРµСЂРµРЅРёРµ.",
                location);
            throw new RuntimeFailureException();
        }

        var dimension = ToArrayBound(arguments[dimensionArgumentIndex], location, $"РќРѕРјРµСЂ РёР·РјРµСЂРµРЅРёСЏ С„СѓРЅРєС†РёРё {functionName}");
        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ С†РµР»С‹Рј С‡РёСЃР»РѕРј РѕС‚ 1 РґРѕ {array.Rank}.",
                location);
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private RuntimeValue CreateArrayWindowCopyBuiltin(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        (int[] LowerBounds, int[] UpperBounds) bounds)
    {
        return CreateVisibleShapeArrayValue(arrayType, bounds.LowerBounds, bounds.UpperBounds, source.GetValue);
    }

    private RuntimeValue CreateReversedArrayCopyBuiltin(
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

    private RuntimeValue FindArrayBinarySearchCoordinatesBuiltin(
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        TextLocation location)
    {
        var foundIndices = FindArrayBinarySearchCoordinateSetBuiltin(functionName, array, value, dimension, location);
        return foundIndices is null
            ? CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
            : CreateCompactArrayValue(
                (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                foundIndices.Select(RuntimeValue.FromInteger).ToArray());
    }

    private int[]? FindArrayBinarySearchCoordinateSetBuiltin(
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        TextLocation location)
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

            var foundIndex = FindArrayBinarySearchIndexBuiltin(functionName, array, value, dimension, baseIndices, location);
            if (foundIndex is null)
            {
                return;
            }

            foundIndices = baseIndices.ToArray();
            foundIndices[dimension - 1] = foundIndex.Value;
        });

        return foundIndices;
    }

    private void SortArrayAlongDimension(RuntimeArrayValue array, int dimension, TextLocation location)
    {
        if (array.Length <= 1 || GetDimensionLength(array, dimension) <= 1)
        {
            return;
        }

        ForEachArrayLineBaseIndicesAscending(array, dimension, baseIndices =>
        {
            var sortedLine = GetStableSortedLine(array, dimension, baseIndices, location);
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

    private List<RuntimeValue> GetStableSortedLine(
        RuntimeArrayValue array,
        int dimension,
        IReadOnlyList<int> baseIndices,
        TextLocation location)
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
            var comparison = Compare(left.Value, right.Value, location);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        return values.Select(static item => item.Value).ToList();
    }

    private void ForEachArrayLineBaseIndicesAscending(RuntimeArrayValue array, int dimension, Action<IReadOnlyList<int>> action)
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

    private static void ReverseOneDimensionalArray(RuntimeArrayValue array)
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

    private static void ReverseArrayAlongDimension(RuntimeArrayValue array, int dimension)
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

    private void EnsureCompatibleConcatShapes(RuntimeArrayValue left, RuntimeArrayValue right, int dimension, TextLocation location)
    {
        if (left.Rank != right.Rank)
        {
            ReportBuiltInArgumentError(
                "ARRAYCONCAT",
                "right",
                "РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ С‚РѕС‚ Р¶Рµ СЂР°РЅРі, С‡С‚Рѕ Рё Р°СЂРіСѓРјРµРЅС‚ 'left'.",
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
                    "РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ СЃРѕРІРјРµСЃС‚РёРјСѓСЋ РІРёРґРёРјСѓСЋ С„РѕСЂРјСѓ РїРѕ РІСЃРµРј РёР·РјРµСЂРµРЅРёСЏРј, РєСЂРѕРјРµ РІС‹Р±СЂР°РЅРЅРѕРіРѕ.",
                    location);
                throw new RuntimeFailureException();
            }
        }
    }

    private static int GetDimensionLength(RuntimeArrayValue array, int dimension)
    {
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        return upperBound < lowerBound ? 0 : upperBound - lowerBound + 1;
    }

    private static (int[] LowerBounds, int[] UpperBounds) CreateAdjustedBounds(
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

    private static void ForEachArrayIndexAscending(RuntimeArrayValue array, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        ForEachIndexAscending(array.LowerBound, array.UpperBound, action);
    }

    private static void ForEachArrayIndexDescending(RuntimeArrayValue array, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        ForEachIndexDescending(array.UpperBound, array.LowerBound, action);
    }

    private static void ForEachArrayIndexSetAscending(RuntimeArrayValue array, Action<IReadOnlyList<int>> action)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(action);
        array.ForEachVisibleIndexSetAscending(action);
    }

    private static void ForEachIndexAscending(int lowerBound, int upperBound, Action<int> action)
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

    private static void ForEachIndexDescending(int upperBound, int lowerBound, Action<int> action)
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

    private static void ForEachIndexSetAscending(
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

    private static void ForEachIndexSetAscendingRecursive(
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
}
