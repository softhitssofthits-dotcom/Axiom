using Axiom.Runtime.Builtins;
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

    private RuntimeValue EvaluateArraySumBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSUM", "array", 0, value);
        return BuiltinArrayAggregateHelpers.Sum(
            array,
            expression.Location,
            (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYSUM", "array", detail, errorLocation),
            (message, errorLocation) => ReportResultOverflow(message, errorLocation));
    }

    private RuntimeValue EvaluateArrayMinBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYMIN", "array", 0, value);
        return BuiltinArrayAggregateHelpers.Min(array, expression.Location, (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYMIN", "array", detail, errorLocation));
    }

    private RuntimeValue EvaluateArrayMaxBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYMAX", "array", 0, value);
        return BuiltinArrayAggregateHelpers.Max(array, expression.Location, (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYMAX", "array", detail, errorLocation));
    }

    private RuntimeValue EvaluateArrayAverageBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYAVERAGE", "array", 0, value);
        return BuiltinArrayAggregateHelpers.Average(
            array,
            expression.Location,
            (detail, errorLocation) => ReportBuiltInArgumentError("ARRAYAVERAGE", "array", detail, errorLocation),
            (message, errorLocation) => ReportResultOverflow(message, errorLocation));
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

    private RuntimeValue EvaluateArrayRank(RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);
        return RuntimeValue.FromInteger(array.Rank);
    }

    private RuntimeValue EvaluateArrayBounds(BoundCallExpression expression, RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYBOUNDS", "array", 0, argument);
        var resultType = (ArrayTypeSymbol)(expression.Type as ArrayTypeSymbol ?? ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2));
        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultType,
            lowerBounds: [1, 1],
            upperBounds: [array.Rank, 2],
            indices => indices[1] == 1
                ? RuntimeValue.FromInteger(array.GetLowerBound(indices[0]))
                : RuntimeValue.FromInteger(array.GetUpperBound(indices[0])));
    }

    private RuntimeValue EvaluateArrayClone(RuntimeValue argument)
    {
        var array = RequireArrayArgument(argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        return BuiltinArrayHelpers.CloneArrayValue(arrayType, array);
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
            if (arguments.Count > 3)
                _ = ResolveTransformDimension(expression, "ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3);

            return EvaluateOneDimensionalArraySlice(expression, arrayType, array, start, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYSLICE", array, arguments, dimensionArgumentIndex: 3);

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
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        return BuiltinArrayHelpers.CreateArrayWindowCopy(
            arrayType,
            array,
            BuiltinArrayHelpers.CreateAdjustedBounds(array, dimension, start, (int)end));
    }

    private RuntimeValue EvaluateArrayFlatten(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFLATTEN", "array", 0, argument);
        var sourceArrayType = (ArrayTypeSymbol)argument.Type;
        var resultArrayType = (ArrayTypeSymbol)(expression.Type as ArrayTypeSymbol ?? ArrayTypeSymbol.Create(sourceArrayType.ElementType));
        var values = new List<RuntimeValue>(array.Length);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices => values.Add(array.GetValue(indices)));

        return BuiltinArrayHelpers.CreateCompactArrayValue(resultArrayType, values);
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

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(source, indices => values.Add(source.GetValue(indices)));

        var next = 0;
        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultArrayType,
            template,
            _ => values[next++]);
    }

    private RuntimeValue EvaluateArrayToReversed(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOREVERSED", "array", 0, arguments[0]);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension(expression, "ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1)
            : (int?)null;
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Rank == 1)
        {
            var values = new List<RuntimeValue>(array.Length);

            BuiltinArrayHelpers.ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

            return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, values);
        }

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYTOREVERSED", array, arguments, dimensionArgumentIndex: 1);
        return BuiltinArrayHelpers.CreateReversedArrayCopy(arrayType, array, dimension);
    }

    private RuntimeValue EvaluateArrayToSorted(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOSORTED", "array", 0, arguments[0]);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension(expression, "ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1)
            : (int?)null;
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (array.Length == 0)
        {
            return BuiltinArrayHelpers.CloneArrayValue(arrayType, array);
        }

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYTOSORTED", array, arguments, dimensionArgumentIndex: 1);
            var clone = BuiltinArrayHelpers.CloneArrayValue(arrayType, array).AsArray();
            SortArrayAlongDimension(clone, dimension);
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
            var comparison = Compare(left.Value, right.Value);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        return BuiltinArrayHelpers.CreateArrayValue(
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
            if (arguments.Count > 2)
                _ = ResolveTransformDimension(expression, "ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2);

            return EvaluateOneDimensionalArrayTake(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYTAKE", array, arguments, dimensionArgumentIndex: 2);
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

    private RuntimeValue EvaluateArraySkip(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSKIP", "array", 0, arguments[0]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;
        var count = RequireNonNegativeCount(expression, "ARRAYSKIP", "count", 1, arguments[1].AsInteger());

        if (array.Rank == 1)
        {
            if (arguments.Count > 2)
                _ = ResolveTransformDimension(expression, "ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2);

            return EvaluateOneDimensionalArraySkip(arrayType, array, count);
        }

        var dimension = ResolveTransformDimension(expression, "ARRAYSKIP", array, arguments, dimensionArgumentIndex: 2);
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

    private RuntimeValue EvaluateArrayDistinct(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYDISTINCT", "array", 0, argument);
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

        return BuiltinArrayHelpers.CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayContains(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYCONTAINS", "array", 0, argument);
        return RuntimeValue.FromBoolean(BuiltinArrayHelpers.ArrayContainsValue(array, value));
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

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
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

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index =>
        {
            if (ValuesEqual(array.GetValue(index), value))
            {
                indices.Add(RuntimeValue.FromInteger(index));
            }
        });

        return BuiltinArrayHelpers.CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), indices);
    }

    private RuntimeValue EvaluateArrayCoordinateIndexOf(
        RuntimeArrayValue array,
        RuntimeValue value,
        bool searchFromEnd)
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
            ? BuiltinArrayHelpers.CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
            : BuiltinArrayHelpers.CreateCompactArrayValue(
                (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                foundIndices.Select(RuntimeValue.FromInteger).ToArray());
    }

    private RuntimeValue EvaluateArrayCoordinateFindAll(RuntimeArrayValue array, RuntimeValue value)
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

    private RuntimeValue EvaluateArrayBinarySearch(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYBINARYSEARCH", "array", 0, arguments[0]);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension(expression, "ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2)
            : (int?)null;

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYBINARYSEARCH", array, arguments, dimensionArgumentIndex: 2);
            return FindArrayBinarySearchCoordinates(array, arguments[1], dimension);
        }

        return RuntimeValue.FromInteger(FindArrayBinarySearchIndex(expression, "ARRAYBINARYSEARCH", array, arguments[1]));
    }

    private RuntimeValue EvaluateArrayFill(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYFILL", "array", 0, argument);

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices => array.SetValue(indices, value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayReverse(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYREVERSE", "array", 0, arguments[0]);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension(expression, "ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1)
            : (int?)null;

        if (array.Rank == 1)
        {
            BuiltinArrayHelpers.ReverseOneDimensionalArray(array);
            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYREVERSE", array, arguments, dimensionArgumentIndex: 1);
        BuiltinArrayHelpers.ReverseArrayAlongDimension(array, dimension);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArraySort(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSORT", "array", 0, arguments[0]);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension(expression, "ARRAYSORT", array, arguments, dimensionArgumentIndex: 1)
            : (int?)null;

        if (array.Length <= 1)
        {
            return RuntimeValue.Void;
        }

        if (array.Rank != 1)
        {
            var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYSORT", array, arguments, dimensionArgumentIndex: 1);
            SortArrayAlongDimension(array, dimension);
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
            var comparison = Compare(left.Value, right.Value);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        BuiltinArrayHelpers.ForEachArrayIndexAscending(array, index => array.SetValue(index, values[index - array.LowerBound].Value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPush(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireResizableArrayArgument(expression, "ARRAYPUSH", arguments[0]);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension(expression, "ARRAYPUSH", array, arguments, dimensionArgumentIndex: 2)
            : (int?)null;

        if (array.Rank == 1)
        {
            var newUpperBound = GetExpandedArrayUpperBound(expression, "ARRAYPUSH", array, dimension: 1);
            array.AllocatePreserving(array.LowerBound, newUpperBound);
            array.SetValue(newUpperBound, arguments[1]);
            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYPUSH", array, arguments, dimensionArgumentIndex: 2);
        var valueArray = RequireAllocatedArrayArgument(expression, "ARRAYPUSH", "value", 1, arguments[1]);
        EnsureCompatibleDynamicArrayValueShape(expression, "ARRAYPUSH", array, valueArray, dimension, valueArgumentIndex: 1);

        var expandedUpperBound = GetExpandedArrayUpperBound(expression, "ARRAYPUSH", array, dimension);
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

    private RuntimeValue EvaluateArrayPop(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireResizableArrayArgument(expression, "ARRAYPOP", arguments[0]);
        var explicitDimension = arguments.Count > 1
            ? ResolveTransformDimension(expression, "ARRAYPOP", array, arguments, dimensionArgumentIndex: 1)
            : (int?)null;

        if (array.Rank == 1)
        {
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

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYPOP", array, arguments, dimensionArgumentIndex: 1);
        var removalIndex = RequireArrayRemovalIndex(expression, "ARRAYPOP", array, array.GetUpperBound(dimension), dimension, argumentIndex: 0, emptyParameterName: "array");
        var resultType = (ArrayTypeSymbol)expression.Type;
        var resultBounds = BuiltinArrayHelpers.CreateReducedBounds(array, dimension);
        var removed = BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
            resultType,
            resultBounds.LowerBounds,
            resultBounds.UpperBounds,
            reducedIndices => array.GetValue(BuiltinArrayHelpers.ExpandReducedIndices(reducedIndices, array.Rank, dimension, removalIndex)));

        array.AllocatePreserving(BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension).LowerBounds, BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension).UpperBounds);
        return removed;
    }

    private RuntimeValue EvaluateArrayInsert(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireResizableArrayArgument(expression, "ARRAYINSERT", arguments[0]);
        var explicitDimension = arguments.Count > 3
            ? ResolveTransformDimension(expression, "ARRAYINSERT", array, arguments, dimensionArgumentIndex: 3)
            : (int?)null;
        var index = arguments[1].AsInteger();

        if (array.Rank == 1)
        {
            var insertionIndex = RequireArrayInsertionIndex(expression, "ARRAYINSERT", array, index, dimension: 1, argumentIndex: 1);
            var isEmpty = array.Length == 0;
            var newUpperBound = isEmpty ? 1 : GetExpandedArrayUpperBound(expression, "ARRAYINSERT", array, dimension: 1);
            var previousUpperBound = isEmpty ? 0 : array.UpperBound;

            array.AllocatePreserving(isEmpty ? 1 : array.LowerBound, newUpperBound);

            for (var currentIndex = previousUpperBound; currentIndex >= insertionIndex; currentIndex--)
            {
                array.SetValue(currentIndex + 1, array.GetValue(currentIndex));
            }

            array.SetValue(insertionIndex, arguments[2]);
            return RuntimeValue.Void;
        }

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYINSERT", array, arguments, dimensionArgumentIndex: 3);
        var valueArray = RequireAllocatedArrayArgument(expression, "ARRAYINSERT", "value", 2, arguments[2]);
        EnsureCompatibleDynamicArrayValueShape(expression, "ARRAYINSERT", array, valueArray, dimension, valueArgumentIndex: 2);
        var insertionDimensionIndex = RequireArrayInsertionIndex(expression, "ARRAYINSERT", array, index, dimension, argumentIndex: 1);
        var selectedDimensionIsEmpty = BuiltinArrayHelpers.GetDimensionLength(array, dimension) == 0;
        var previousDimensionUpperBound = selectedDimensionIsEmpty ? 0 : array.GetUpperBound(dimension);
        var expandedUpperBound = selectedDimensionIsEmpty ? 1 : GetExpandedArrayUpperBound(expression, "ARRAYINSERT", array, dimension);
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

    private RuntimeValue EvaluateArrayRemove(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var array = RequireResizableArrayArgument(expression, "ARRAYREMOVE", arguments[0]);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension(expression, "ARRAYREMOVE", array, arguments, dimensionArgumentIndex: 2)
            : (int?)null;
        var index = arguments[1].AsInteger();

        if (array.Rank == 1)
        {
            var removalIndex = RequireArrayRemovalIndex(expression, "ARRAYREMOVE", array, index, dimension: 1, argumentIndex: 1, emptyParameterName: "index");
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

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYREMOVE", array, arguments, dimensionArgumentIndex: 2);
        var multidimensionalRemovalIndex = RequireArrayRemovalIndex(expression, "ARRAYREMOVE", array, index, dimension, argumentIndex: 1, emptyParameterName: "index");
        var resultType = (ArrayTypeSymbol)expression.Type;
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

        array.AllocatePreserving(BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension).LowerBounds, BuiltinArrayHelpers.CreateBoundsAfterDimensionShrink(array, dimension).UpperBounds);
        return removed;
    }

    private RuntimeValue EvaluateArrayEvery(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue predicateArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYEVERY", "array", 0, argument);
        var predicate = RequireFunctionArgument(expression, 1, predicateArgument);

        var every = true;

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
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

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
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

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
        {
            var value = array.GetValue(indices);

            if (InvokeArrayPredicate(expression, predicate, value))
            {
                values.Add(value);
            }
        });

        return BuiltinArrayHelpers.CreateCompactArrayValue(resultArrayType, values);
    }

    private RuntimeValue EvaluateArrayMap(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue selectorArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYMAP", "array", 0, argument);
        var arrayType = expression.Type as ArrayTypeSymbol ?? (ArrayTypeSymbol)argument.Type;
        var selector = RequireFunctionArgument(expression, 1, selectorArgument);
        return BuiltinArrayHelpers.CreateVisibleShapeArrayValue(
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

        BuiltinArrayHelpers.ForEachArrayIndexSetAscending(array, indices =>
            accumulator = InvokeArrayCallback(expression, reducer, callbackArgumentIndex: 2, accumulator, array.GetValue(indices)));

        return accumulator;
    }

    private RuntimeValue EvaluateArrayConcat(
        BoundCallExpression expression,
        IReadOnlyList<RuntimeValue> arguments)
    {
        var left = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "left", 0, arguments[0]);
        var explicitDimension = arguments.Count > 2
            ? ResolveTransformDimension(expression, "ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2)
            : (int?)null;
        var right = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "right", 1, arguments[1]);
        var arrayType = (ArrayTypeSymbol)arguments[0].Type;

        if (left.Rank == 1)
        {
            if (left.Length == 0)
            {
                return BuiltinArrayHelpers.CloneArrayValue(arrayType, right);
            }

            if (right.Length == 0)
            {
                return BuiltinArrayHelpers.CloneArrayValue(arrayType, left);
            }

            var values = new List<RuntimeValue>(left.Length + right.Length);
            BuiltinArrayHelpers.ForEachArrayIndexAscending(left, index => values.Add(left.GetValue(index)));
            BuiltinArrayHelpers.ForEachArrayIndexAscending(right, index => values.Add(right.GetValue(index)));
            return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, values);
        }

        var dimension = explicitDimension ?? ResolveTransformDimension(expression, "ARRAYCONCAT", left, arguments, dimensionArgumentIndex: 2);
        EnsureCompatibleConcatShapes(expression, left, right, dimension);

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
                GetBuiltInArgumentLocation(expression, 1));
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

    private RuntimeValue EvaluateOneDimensionalArraySlice(
        BoundCallExpression expression,
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue array,
        int start,
        int count)
    {
        if (count == 0)
        {
            return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, []);
        }

        var end = (long)start + count - 1L;

        if (start < array.LowerBound || end > array.UpperBound)
        {
            ReportBuiltInArgumentError(
                "ARRAYSLICE",
                "start",
                "вместе с 'count' задаёт диапазон вне границ исходного массива.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        var values = new RuntimeValue[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = array.GetValue(start + index);
        }

        return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArrayTake(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count == 0 || array.Length == 0)
        {
            return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, []);
        }

        var takeCount = Math.Min(count, array.Length);
        var values = new RuntimeValue[takeCount];

        for (var index = 0; index < takeCount; index++)
        {
            values[index] = array.GetValue(array.LowerBound + index);
        }

        return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, values);
    }

    private static RuntimeValue EvaluateOneDimensionalArraySkip(ArrayTypeSymbol arrayType, RuntimeArrayValue array, int count)
    {
        if (count >= array.Length)
        {
            return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, []);
        }

        var remainingCount = array.Length - count;
        var values = new RuntimeValue[remainingCount];

        for (var index = 0; index < remainingCount; index++)
        {
            values[index] = array.GetValue(array.LowerBound + count + index);
        }

        return BuiltinArrayHelpers.CreateCompactArrayValue(arrayType, values);
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
                $"для массива ранга {array.Rank} нужно явно указать измерение.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        var dimension = ToArrayBound(
            arguments[dimensionArgumentIndex],
            GetBuiltInArgumentLocation(expression, dimensionArgumentIndex),
            $"Измерение для {functionName}");

        if (dimension < 1 || dimension > array.Rank)
        {
            ReportBuiltInArgumentError(
                functionName,
                "dimension",
                $"должен быть в диапазоне от 1 до {array.Rank}.",
                GetBuiltInArgumentLocation(expression, dimensionArgumentIndex));
            throw new RuntimeFailureException();
        }

        return dimension;
    }

    private static RuntimeValue FindArrayBinarySearchCoordinates(
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension)
    {
        var foundIndices = BuiltinArrayHelpers.FindArrayBinarySearchCoordinateSet(array, value, dimension, Compare);
        return foundIndices is null
            ? BuiltinArrayHelpers.CreateCompactArrayValue((ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer), [])
            : BuiltinArrayHelpers.CreateCompactArrayValue(
                (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.Integer),
                foundIndices.Select(RuntimeValue.FromInteger).ToArray());
    }

    private static void SortArrayAlongDimension(RuntimeArrayValue array, int dimension)
    {
        BuiltinArrayHelpers.SortArrayAlongDimension(array, dimension, Compare);
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
                "должен иметь тот же ранг, что и аргумент 'left'.",
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
                    "должен иметь совместимую видимую форму по всем измерениям, кроме выбранного.",
                    GetBuiltInArgumentLocation(expression, 1));
                throw new RuntimeFailureException();
            }
        }
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

        if (foundIndex.HasValue)
        {
            return foundIndex.Value;
        }

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

    private RuntimeArrayValue RequireResizableArrayArgument(
        BoundCallExpression expression,
        string functionName,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, functionName, "array", 0, argument);

        if (array.IsView)
        {
            ReportBuiltInArgumentError(
                functionName,
                "array",
                "не поддерживает подмассивы-представления при изменении размера.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return array;
    }

    private int GetExpandedArrayUpperBound(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        int dimension)
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
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private int RequireArrayInsertionIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        int index,
        int dimension,
        int argumentIndex)
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
                GetBuiltInArgumentLocation(expression, argumentIndex));
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
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return index;
    }

    private int RequireArrayRemovalIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        int index,
        int dimension,
        int argumentIndex,
        string emptyParameterName)
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
                GetBuiltInArgumentLocation(expression, argumentIndex));
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
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return index;
    }

    private void EnsureCompatibleDynamicArrayValueShape(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue source,
        RuntimeArrayValue value,
        int dimension,
        int valueArgumentIndex)
    {
        if (value.Rank != source.Rank - 1)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "должен иметь ранг на единицу меньше исходного массива.",
                GetBuiltInArgumentLocation(expression, valueArgumentIndex));
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
                    GetBuiltInArgumentLocation(expression, valueArgumentIndex));
                throw new RuntimeFailureException();
            }

            valueDimension++;
        }
    }

    private int FindArrayBinarySearchIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value)
    {
        try
        {
            return BuiltinArrayHelpers.FindArrayBinarySearchIndex(array, value, Compare);
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

    private int? FindArrayBinarySearchIndex(
        BoundCallExpression expression,
        string functionName,
        RuntimeArrayValue array,
        RuntimeValue value,
        int dimension,
        IReadOnlyList<int> baseIndices)
    {
        return BuiltinArrayHelpers.FindArrayBinarySearchIndex(array, value, dimension, baseIndices, Compare);
    }

}
