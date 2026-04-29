using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateLBound(BoundCallExpression expression, IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "LBOUND", "array", 0, arguments[0]);
        var dimension = ResolveArrayInfoDimension(expression, "LBOUND", array, arguments);
        return RuntimeValue.FromInteger(array.GetLowerBound(dimension));
    }

    private RuntimeValue EvaluateUBound(BoundCallExpression expression, IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "UBOUND", "array", 0, arguments[0]);
        var dimension = ResolveArrayInfoDimension(expression, "UBOUND", array, arguments);
        return RuntimeValue.FromInteger(array.GetUpperBound(dimension));
    }

    private RuntimeValue EvaluateArrayLength(RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);
        return RuntimeValue.FromInteger(array.Length);
    }

    private RuntimeValue EvaluateArrayIsAllocated(RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);
        return RuntimeValue.FromBoolean(array.IsAllocated);
    }

    private RuntimeValue EvaluateArrayClone(RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        return CloneArrayValue(arrayType, array);
    }

    private RuntimeValue EvaluateArraySlice(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSLICE", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var start = arguments[1].AsInteger();
        var count = RequireNonNegativeCount(expression, "ARRAYSLICE", "count", 2, arguments[2].AsInteger());

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArraySlice(expression, arrayType, array, start, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3);

        if (count == 0)
        {
            return CreateArrayWindowCopy(
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
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return CreateArrayWindowCopy(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, start, (int)end));
    }

    private RuntimeValue EvaluateArrayFlatten(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFLATTEN", "array", 0, argument);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)(expression.Type as ArrayTypeSymbol ?? ArrayTypeSymbol.Create(sourceArrayType.ElementType));
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexSetAscending(array, indices => values.Add(array.GetValue(indices)));

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayReshape(
        BoundCallExpression expression,
        RuntimeValue sourceArgument,
        RuntimeValue templateArgument)
    {
        var source = RequireAllocatedArrayArgument(expression, "ARRAYRESHAPE", "source", 0, sourceArgument);
        var template = RequireAllocatedArrayArgument(expression, "ARRAYRESHAPE", "template", 1, templateArgument);

        if (source.Length != template.Length)
        {
            ReportBuiltInArgumentError(
                "ARRAYRESHAPE",
                "template",
                "должен задавать то же число видимых элементов, что и 'source'.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        var resultArrayType = (ArrayTypeSymbol)(expression.Type as ArrayTypeSymbol ?? templateArgument.Type);
        var values = new List<RuntimeValue>(source.Length);

        ForEachArrayIndexSetAscending(source, indices => values.Add(source.GetValue(indices)));

        var next = 0;
        return CreateVisibleShapeArrayValue(
            resultArrayType,
            template,
            _ => values[next++]);
    }

    private RuntimeValue EvaluateArrayToReversed(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOREVERSED", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Rank == 1)
        {
            var values = new List<RuntimeValue>(array.Length);

            ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

            return CreateCompactArrayValue(arrayType, values);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1);
        return CreateReversedArrayCopy(arrayType, array, dimension);
    }

    private RuntimeValue EvaluateArrayToSorted(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOSORTED", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Length == 0)
        {
            return CloneArrayValue(arrayType, array);
        }

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension(expression, "ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1);
            var clone = CloneArrayValue(arrayType, array).AsArray();
            SortArrayAlongDimension(clone, dimension);
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
            var comparison = Compare(left.Value, right.Value);
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

    private RuntimeValue EvaluateArrayTake(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTAKE", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount(expression, "ARRAYTAKE", "count", 1, arguments[1].AsInteger());

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArrayTake(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2);
        var lowerBound = array.GetLowerBound(dimension);
        var extent = GetDimensionLength(array, dimension);
        var takeCount = Math.Min(count, extent);
        var upperBound = takeCount == 0
            ? lowerBound - 1
            : lowerBound + takeCount - 1;

        return CreateArrayWindowCopy(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
    }

    private RuntimeValue EvaluateArraySkip(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSKIP", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount(expression, "ARRAYSKIP", "count", 1, arguments[1].AsInteger());

        if (array.Rank == 1)
        {
            return EvaluateOneDimensionalArraySkip(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2);
        var lowerBound = array.GetLowerBound(dimension);
        var upperBound = array.GetUpperBound(dimension);
        var extent = GetDimensionLength(array, dimension);

        if (count <= 0)
        {
            return CreateArrayWindowCopy(
                arrayType,
                array,
                CreateAdjustedBounds(array, dimension, lowerBound, upperBound));
        }

        if (count >= extent)
        {
            return CreateArrayWindowCopy(
                arrayType,
                array,
                CreateAdjustedBounds(array, dimension, upperBound + 1, upperBound));
        }

        return CreateArrayWindowCopy(
            arrayType,
            array,
            CreateAdjustedBounds(array, dimension, lowerBound + count, upperBound));
    }

    private RuntimeValue EvaluateArrayDistinct(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYDISTINCT", "array", 0, argument);
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

    private RuntimeValue EvaluateArrayContains(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYCONTAINS", "array", 0, argument);
        return RuntimeValue.FromBoolean(ArrayContainsValue(array, value));
    }

    private RuntimeValue EvaluateArrayIndexOf(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYINDEXOF", "array", 0, argument);

        if (array.Rank != 1)
        {
            return EvaluateArrayCoordinateIndexOf(array, value, searchFromEnd: false);
        }

        return RuntimeValue.FromInteger(FindArrayIndex(expression, "ARRAYINDEXOF", array, value, searchFromEnd: false));
    }

    private RuntimeValue EvaluateArrayLastIndexOf(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYLASTINDEXOF", "array", 0, argument);

        if (array.Rank != 1)
        {
            return EvaluateArrayCoordinateIndexOf(array, value, searchFromEnd: true);
        }

        return RuntimeValue.FromInteger(FindArrayIndex(expression, "ARRAYLASTINDEXOF", array, value, searchFromEnd: true));
    }

    private RuntimeValue EvaluateArrayCount(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYCOUNT", "array", 0, argument);
        var count = 0;

        ForEachArrayIndexSetAscending(array, indices =>
        {
            if (ValuesEqual(array.GetValue(indices), value))
            {
                count++;
            }
        });

        return RuntimeValue.FromInteger(count);
    }

    private RuntimeValue EvaluateArrayFindAll(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFINDALL", "array", 0, argument);

        if (array.Rank != 1)
        {
            return EvaluateArrayCoordinateFindAll(array, value);
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

    private RuntimeValue EvaluateArrayCoordinateIndexOf(
        RuntimeArrayValue array,
        RuntimeValue value,
        bool searchFromEnd)
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

    private RuntimeValue EvaluateArrayCoordinateFindAll(RuntimeArrayValue array, RuntimeValue value)
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

    private RuntimeValue EvaluateArrayBinarySearch(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYBINARYSEARCH", "array", 0, arguments[0]);

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension(expression, "ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2);
            return FindArrayBinarySearchCoordinates(expression, "ARRAYBINARYSEARCH", array, arguments[1], dimension);
        }

        return RuntimeValue.FromInteger(FindArrayBinarySearchIndex(expression, "ARRAYBINARYSEARCH", array, arguments[1]));
    }

    private RuntimeValue EvaluateArrayFill(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFILL", "array", 0, argument);

        ForEachArrayIndexSetAscending(array, indices => array.SetValue(indices, value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayReverse(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYREVERSE", "array", 0, arguments[0]);

        if (array.Rank == 1)
        {
            ReverseOneDimensionalArray(array);
            return RuntimeValue.Void;
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1);
        ReverseArrayAlongDimension(array, dimension);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArraySort(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSORT", "array", 0, arguments[0]);

        if (array.Length <= 1)
        {
            return RuntimeValue.Void;
        }

        if (array.Rank != 1)
        {
            var dimension = ResolveTransformDimension(expression, "ARRAYSORT", array, arguments, dimensionArgumentIndex: 1);
            SortArrayAlongDimension(array, dimension);
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
            var comparison = Compare(left.Value, right.Value);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        ForEachArrayIndexAscending(array, index => array.SetValue(index, values[index - array.LowerBound].Value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPush(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYPUSH", "array", 0, argument);
        var newUpperBound = GetExpandedArrayUpperBound(expression, "ARRAYPUSH", array);

        array.AllocatePreserving(array.LowerBound, newUpperBound);
        array.SetValue(newUpperBound, value);

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPop(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYPOP", "array", 0, argument);

        if (array.Length == 0)
        {
            ReportBuiltInArgumentError(
                "ARRAYPOP",
                "array",
                "не может быть пустым выделенным массивом.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        var poppedValue = array.GetValue(array.UpperBound);

        if (array.Length == 1)
        {
            array.Allocate(1, 0);
            return poppedValue;
        }

        array.AllocatePreserving(array.LowerBound, array.UpperBound - 1);
        return poppedValue;
    }

    private RuntimeValue EvaluateArrayInsert(
        BoundCallExpression expression,
        RuntimeValue argument,
        int index,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYINSERT", "array", 0, argument);
        var insertionIndex = RequireArrayInsertionIndex(expression, "ARRAYINSERT", array, index);
        var newUpperBound = GetExpandedArrayUpperBound(expression, "ARRAYINSERT", array);
        var previousUpperBound = array.UpperBound;

        array.AllocatePreserving(array.LowerBound, newUpperBound);

        for (var currentIndex = previousUpperBound; currentIndex >= insertionIndex; currentIndex--)
        {
            array.SetValue(currentIndex + 1, array.GetValue(currentIndex));
        }

        array.SetValue(insertionIndex, value);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayRemove(
        BoundCallExpression expression,
        RuntimeValue argument,
        int index)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYREMOVE", "array", 0, argument);
        var removalIndex = RequireArrayRemovalIndex(expression, "ARRAYREMOVE", array, index);
        var removedValue = array.GetValue(removalIndex);

        if (array.Length == 1)
        {
            array.Allocate(1, 0);
            return removedValue;
        }

        for (var currentIndex = removalIndex; currentIndex < array.UpperBound; currentIndex++)
        {
            array.SetValue(currentIndex, array.GetValue(currentIndex + 1));
        }

        array.AllocatePreserving(array.LowerBound, array.UpperBound - 1);
        return removedValue;
    }

    private RuntimeValue EvaluateArrayEvery(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue predicateArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYEVERY", "array", 0, argument);
        var predicate = RequireFunctionArgument(expression, 1, predicateArgument);

        var every = true;

        ForEachArrayIndexSetAscending(array, indices =>
        {
            if (every && !InvokeArrayPredicate(expression, predicate, array.GetValue(indices)))
            {
                every = false;
            }
        });

        return RuntimeValue.FromBoolean(every);
    }

    private RuntimeValue EvaluateArraySome(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue predicateArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSOME", "array", 0, argument);
        var predicate = RequireFunctionArgument(expression, 1, predicateArgument);

        var any = false;

        ForEachArrayIndexSetAscending(array, indices =>
        {
            if (!any && InvokeArrayPredicate(expression, predicate, array.GetValue(indices)))
            {
                any = true;
            }
        });

        return RuntimeValue.FromBoolean(any);
    }

    private RuntimeValue EvaluateArrayFilter(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue predicateArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFILTER", "array", 0, argument);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = expression.Type as ArrayTypeSymbol ?? (ArrayTypeSymbol)ArrayTypeSymbol.Create(sourceArrayType.ElementType);
        var predicate = RequireFunctionArgument(expression, 1, predicateArgument);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexSetAscending(array, indices =>
        {
            var value = array.GetValue(indices);

            if (InvokeArrayPredicate(expression, predicate, value))
            {
                values.Add(value);
            }
        });

        return CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayMap(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue selectorArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYMAP", "array", 0, argument);
        var arrayType = expression.Type as ArrayTypeSymbol ?? (ArrayTypeSymbol)argument.Type;
        var selector = RequireFunctionArgument(expression, 1, selectorArgument);
        return CreateVisibleShapeArrayValue(
            arrayType,
            array,
            indices => InvokeArrayCallback(expression, selector, callbackArgumentIndex: 1, array.GetValue(indices)));
    }

    private RuntimeValue EvaluateArrayReduce(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue seed,
        RuntimeValue reducerArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYREDUCE", "array", 0, argument);
        var reducer = RequireFunctionArgument(expression, 2, reducerArgument);
        var accumulator = seed;

        ForEachArrayIndexSetAscending(array, indices =>
            accumulator = InvokeArrayCallback(expression, reducer, callbackArgumentIndex: 2, accumulator, array.GetValue(indices)));

        return accumulator;
    }

    private RuntimeValue EvaluateArrayConcat(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var left = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "left", 0, arguments[0]);
        var right = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "right", 1, arguments[1]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (left.Rank == 1)
        {
            var values = new List<RuntimeValue>(left.Length + right.Length);
            ForEachArrayIndexAscending(left, index => values.Add(left.GetValue(index)));
            ForEachArrayIndexAscending(right, index => values.Add(right.GetValue(index)));
            return CreateCompactArrayValue(arrayType, values);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2);
        EnsureCompatibleConcatShapes(expression, left, right, dimension);

        if (GetDimensionLength(left, dimension) == 0)
        {
            return CreateArrayWindowCopy(arrayType, right, (right.LowerBounds.ToArray(), right.UpperBounds.ToArray()));
        }

        if (GetDimensionLength(right, dimension) == 0)
        {
            return CreateArrayWindowCopy(arrayType, left, (left.LowerBounds.ToArray(), left.UpperBounds.ToArray()));
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

    private RuntimeValue EvaluateOneDimensionalArraySlice(
        BoundCallExpression expression,
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue array,
        int start,
        int count)
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
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        var values = new RuntimeValue[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = array.GetValue(start + index);
        }

        return CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArrayTake(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count == 0 || array.Length == 0)
        {
            return CreateCompactArrayValue(arrayType, []);
        }

        var takeCount = Math.Min(count, array.Length);
        var values = new RuntimeValue[takeCount];

        for (var index = 0; index < takeCount; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index);
        }

        return CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArraySkip(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count >= array.Length)
        {
            return CreateCompactArrayValue(arrayType, []);
        }

        var remainingCount = array.Length - count;
        var values = new RuntimeValue[remainingCount];

        for (var index = 0; index < remainingCount; index++)
        {
            values[index] = array.GetValue(array.LowerBound + count + index);
        }

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeArrayValue RequireAllocatedArrayArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);

        if (!array.IsAllocated)
        {
            ReportInvalidRuntimeState(
                $"Функция {functionName}: аргумент '{argumentName}' содержит массив, который ещё не выделен.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return array;
    }

    private static RuntimeArrayValue RequireArrayArgument(RuntimeValue argument)
    {
        return argument.Type is ArrayTypeSymbol
            ? argument.AsArray()
            : throw new InvalidOperationException($"Value of type {argument.Type.Name} is not an array.");
    }

    private RuntimeFunctionValue RequireFunctionArgument(
        BoundCallExpression expression,
        int argumentIndex,
        RuntimeValue argument)
    {
        var functionValue = argument.AsFunction();

        if (functionValue is not null)
        {
            return functionValue;
        }

        ReportEmptyFunctionValue(GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private bool InvokeArrayPredicate(
        BoundCallExpression expression,
        RuntimeFunctionValue predicate,
        RuntimeValue value)
    {
        return InvokeArrayCallback(expression, predicate, callbackArgumentIndex: 1, value).AsBoolean();
    }

    private RuntimeValue InvokeArrayCallback(
        BoundCallExpression expression,
        RuntimeFunctionValue callback,
        int callbackArgumentIndex,
        params RuntimeValue[] arguments)
    {
        if (callback.Function.IsBuiltIn)
        {
            return InvokeBuiltIn(
                expression,
                callback.Function,
                arguments);
        }

        return InvokeUserRoutine(
            callback,
            arguments,
            GetBuiltInArgumentLocation(expression, callbackArgumentIndex));
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

        var array = new RuntimeArrayValue(arrayType.ElementType, rank: arrayType.Rank);
        array.Allocate(lowerBound, upperBound);

        ForEachIndexAscending(lowerBound, upperBound, index => array.SetValue(index, valueFactory(index)));

        return RuntimeValue.FromArray(arrayType, array);
    }

    private static RuntimeValue CreateCompactArrayValue(ArrayTypeSymbol arrayType, IReadOnlyList<RuntimeValue> values)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(values);

        return CreateArrayValue(
            arrayType,
            lowerBound: 1,
            upperBound: values.Count == 0 ? 0 : values.Count,
            index => values[index - 1]);
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

        return source.Rank == 1
            ? CreateArrayValue(arrayType, source.LowerBound, source.UpperBound, source.GetValue)
            : RuntimeValue.FromArray(arrayType, source.Clone());
    }

    private int ResolveArrayInfoDimension(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        IReadOnlyList<RuntimeValue> arguments)
    {
        if (arguments.Count == 1)
        {
            return 1;
        }

        var dimension = ToArrayBound(
            arguments[1],
            GetBuiltInArgumentLocation(expression, 1),
            $"Измерение для {functionName}");

        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"должен быть в диапазоне от 1 до {array.Rank}.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private int ResolveTransformDimension(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        IReadOnlyList<RuntimeValue> arguments,
        int dimensionArgumentIndex)
    {
        if (arguments.Count <= dimensionArgumentIndex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"РґР»СЏ РјР°СЃСЃРёРІР° СЂР°РЅРіР° {array.Rank} РЅСѓР¶РЅРѕ СЏРІРЅРѕ СѓРєР°Р·Р°С‚СЊ РёР·РјРµСЂРµРЅРёРµ.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        var dimension = ToArrayBound(
            arguments[dimensionArgumentIndex],
            GetBuiltInArgumentLocation(expression, dimensionArgumentIndex),
            $"РР·РјРµСЂРµРЅРёРµ РґР»СЏ {functionName}");

        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РІ РґРёР°РїР°Р·РѕРЅРµ РѕС‚ 1 РґРѕ {array.Rank}.",
                GetBuiltInArgumentLocation(expression, dimensionArgumentIndex));
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private RuntimeValue CreateArrayWindowCopy(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue source,
        (int[] LowerBounds, int[] UpperBounds) bounds)
    {
        return CreateVisibleShapeArrayValue(arrayType, bounds.LowerBounds, bounds.UpperBounds, source.GetValue);
    }

    private RuntimeValue CreateReversedArrayCopy(
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

    private RuntimeValue FindArrayBinarySearchCoordinates(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension)
    {
        var foundIndices = FindArrayBinarySearchCoordinateSet(expression, functionName, array, value, dimension);
        return foundIndices is null
            ? CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
            : CreateCompactArrayValue(
                (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                foundIndices.Select(RuntimeValue.FromInteger).ToArray());
    }

    private int[]? FindArrayBinarySearchCoordinateSet(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension)
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

            var foundIndex = FindArrayBinarySearchIndex(expression, functionName, array, value, dimension, baseIndices);
            if (foundIndex is null)
            {
                return;
            }

            foundIndices = baseIndices.ToArray();
            foundIndices[dimension - 1] = foundIndex.Value;
        });

        return foundIndices;
    }

    private void SortArrayAlongDimension(RuntimeArrayValue array, int dimension)
    {
        if (array.Length <= 1 || GetDimensionLength(array, dimension) <= 1)
        {
            return;
        }

        ForEachArrayLineBaseIndicesAscending(array, dimension, baseIndices =>
        {
            var sortedLine = GetStableSortedLine(array, dimension, baseIndices);
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
        IReadOnlyList<int> baseIndices)
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
            var comparison = Compare(left.Value, right.Value);
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

    private void EnsureCompatibleConcatShapes(
        BoundCallExpression expression,
        RuntimeArrayValue left,
        RuntimeArrayValue right,
        int dimension)
    {
        if (left.Rank != right.Rank)
        {
            ReportBuiltInArgumentError(
                "ARRAYCONCAT",
                "right",
                "РґРѕР»Р¶РµРЅ РёРјРµС‚СЊ С‚РѕС‚ Р¶Рµ СЂР°РЅРі, С‡С‚Рѕ Рё Р°СЂРіСѓРјРµРЅС‚ 'left'.",
                GetBuiltInArgumentLocation(expression, 1));
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
                    GetBuiltInArgumentLocation(expression, 1));
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
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        bool searchFromEnd)
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

        return foundIndex ?? GetArrayNotFoundIndex(expression, functionName, array);
    }

    private int GetExpandedArrayUpperBound(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array)
    {
        try
        {
            return checked(array.UpperBound + 1);
        }
        catch (OverflowException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "array",
                "не может быть расширен дальше текущей верхней границы.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private int RequireArrayInsertionIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        int index)
    {
        if (array.Length == 0)
        {
            if (index == 1)
            {
                return index;
            }

            ReportBuiltInArgumentError(
                functionName,
                "index",
                "для пустого выделенного массива должен быть равен 1.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        var allowedUpperBound = (long)array.UpperBound + 1;

        if (index < array.LowerBound || index > allowedUpperBound)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "должен попадать в диапазон от текущей нижней границы до позиции сразу после текущего последнего элемента.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return index;
    }

    private int RequireArrayRemovalIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        int index)
    {
        if (array.Length == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "нельзя удалять элемент из пустого выделенного массива.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        if (index < array.LowerBound || index > array.UpperBound)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "должен указывать на существующий логический индекс массива.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return index;
    }

    private int FindArrayBinarySearchIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value)
    {
        long left = array.LowerBound;
        long right = array.UpperBound;
        int? foundIndex = null;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var middleIndex = (int)middle;
            var comparison = Compare(array.GetValue(middleIndex), value);

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

        return foundIndex ?? GetArrayNotFoundIndex(expression, functionName, array);
    }

    private int? FindArrayBinarySearchIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        IReadOnlyList<int> baseIndices)
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
            var comparison = Compare(array.GetValue(indices), value);

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

    private int GetArrayNotFoundIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array)
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
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
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
