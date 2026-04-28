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
        RuntimeValue argument,
        int start,
        int count)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSLICE", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;

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

    private RuntimeValue EvaluateArrayToReversed(BoundCallExpression expression, RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOREVERSED", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayToSorted(BoundCallExpression expression, RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTOSORTED", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;

        if (array.Length == 0)
        {
            return CloneArrayValue(arrayType, array);
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
        RuntimeValue argument,
        int count)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYTAKE", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;

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

    private RuntimeValue EvaluateArraySkip(
        BoundCallExpression expression,
        RuntimeValue argument,
        int count)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSKIP", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;

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

    private RuntimeValue EvaluateArrayDistinct(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYDISTINCT", "array", 0, argument);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexAscending(array, index =>
        {
            var candidate = array.GetValue(index);
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

        return CreateCompactArrayValue(arrayType, values);
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
        return RuntimeValue.FromInteger(FindArrayIndex(expression, "ARRAYINDEXOF", array, value, searchFromEnd: false));
    }

    private RuntimeValue EvaluateArrayLastIndexOf(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYLASTINDEXOF", "array", 0, argument);
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

    private RuntimeValue EvaluateArrayBinarySearch(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue value)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYBINARYSEARCH", "array", 0, argument);
        return RuntimeValue.FromInteger(FindArrayBinarySearchIndex(expression, "ARRAYBINARYSEARCH", array, value));
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
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYREVERSE", "array", 0, argument);
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

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArraySort(
        BoundCallExpression expression,
        RuntimeValue argument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYSORT", "array", 0, argument);

        if (array.Length <= 1)
        {
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

        ForEachArrayIndexAscending(array, index =>
        {
            if (every && !InvokeArrayPredicate(expression, predicate, array.GetValue(index)))
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

        ForEachArrayIndexAscending(array, index =>
        {
            if (!any && InvokeArrayPredicate(expression, predicate, array.GetValue(index)))
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
        var arrayType = (ArrayTypeSymbol)argument.Type;
        var predicate = RequireFunctionArgument(expression, 1, predicateArgument);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexAscending(array, index =>
        {
            var value = array.GetValue(index);

            if (InvokeArrayPredicate(expression, predicate, value))
            {
                values.Add(value);
            }
        });

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayMap(
        BoundCallExpression expression,
        RuntimeValue argument,
        RuntimeValue selectorArgument)
    {
        var array = RequireAllocatedArrayArgument(expression, "ARRAYMAP", "array", 0, argument);
        var arrayType = expression.Type as ArrayTypeSymbol ?? (ArrayTypeSymbol)argument.Type;
        var selector = RequireFunctionArgument(expression, 1, selectorArgument);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexAscending(array, index =>
            values.Add(InvokeArrayCallback(expression, selector, callbackArgumentIndex: 1, array.GetValue(index))));

        return CreateCompactArrayValue(arrayType, values);
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

        ForEachArrayIndexAscending(array, index =>
            accumulator = InvokeArrayCallback(expression, reducer, callbackArgumentIndex: 2, accumulator, array.GetValue(index)));

        return accumulator;
    }

    private RuntimeValue EvaluateArrayConcat(
        BoundCallExpression expression,
        RuntimeValue leftArgument,
        RuntimeValue rightArgument)
    {
        var left = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "left", 0, leftArgument);
        var right = RequireAllocatedArrayArgument(expression, "ARRAYCONCAT", "right", 1, rightArgument);
        var arrayType = (ArrayTypeSymbol)leftArgument.Type;
        var values = new List<RuntimeValue>(left.Length + right.Length);

        ForEachArrayIndexAscending(left, index => values.Add(left.GetValue(index)));
        ForEachArrayIndexAscending(right, index => values.Add(right.GetValue(index)));

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
}
