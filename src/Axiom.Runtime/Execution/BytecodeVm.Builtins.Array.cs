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

    private RuntimeValue EvaluateArrayCloneBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireArrayArgument(argument, "ARRAYCLONE", location);
        return CloneArrayValue((ArrayTypeSymbol)argument.Type, array);
    }

    private RuntimeValue EvaluateArraySliceBuiltin(RuntimeValue argument, int start, int count, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYSLICE", location);
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
                location);
            throw new RuntimeFailureException();
        }

        var values = new RuntimeValue[count];
        for (var index = 0; index < count; index++)
        {
            values[index] = array.GetValue(start + index);
        }

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayToSortedBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYTOSORTED", location);
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

    private RuntimeValue EvaluateArrayToReversedBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYTOREVERSED", location);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexDescending(array, index => values.Add(array.GetValue(index)));

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayTakeBuiltin(RuntimeValue argument, int count, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYTAKE", location);
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

    private RuntimeValue EvaluateArraySkipBuiltin(RuntimeValue argument, int count, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYSKIP", location);
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

    private RuntimeValue EvaluateArrayDistinctBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYDISTINCT", location);
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

    private RuntimeValue EvaluateArrayConcatBuiltin(RuntimeValue leftArgument, RuntimeValue rightArgument, TextLocation location)
    {
        var left = RequireAllocatedArrayArgument(leftArgument, "ARRAYCONCAT", location);
        var right = RequireAllocatedArrayArgument(rightArgument, "ARRAYCONCAT", location);
        var arrayType = (ArrayTypeSymbol)leftArgument.Type;
        var values = new List<RuntimeValue>(left.Length + right.Length);

        ForEachArrayIndexAscending(left, index => values.Add(left.GetValue(index)));
        ForEachArrayIndexAscending(right, index => values.Add(right.GetValue(index)));

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayFillBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFILL", location);

        ForEachArrayIndexSetAscending(array, indices => array.SetValue(indices, value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayReverseBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYREVERSE", location);
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

    private RuntimeValue EvaluateArraySortBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYSORT", location);

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
            var comparison = Compare(left.Value, right.Value, location);
            return comparison != 0
                ? comparison
                : left.OriginalOrder.CompareTo(right.OriginalOrder);
        });

        ForEachArrayIndexAscending(array, index => array.SetValue(index, values[index - array.LowerBound].Value));

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPushBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYPUSH", location);
        var newUpperBound = GetExpandedArrayUpperBound("ARRAYPUSH", array, location);

        array.AllocatePreserving(array.LowerBound, newUpperBound);
        array.SetValue(newUpperBound, value);

        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayPopBuiltin(RuntimeValue argument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYPOP", location);

        if (array.Length == 0)
        {
            ReportBuiltInArgumentError(
                "ARRAYPOP",
                "array",
                "не может быть пустым выделенным массивом.",
                location);
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

    private RuntimeValue EvaluateArrayInsertBuiltin(RuntimeValue argument, int index, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYINSERT", location);
        var insertionIndex = RequireArrayInsertionIndex("ARRAYINSERT", array, index, location);
        var newUpperBound = GetExpandedArrayUpperBound("ARRAYINSERT", array, location);
        var previousUpperBound = array.UpperBound;

        array.AllocatePreserving(array.LowerBound, newUpperBound);

        for (var currentIndex = previousUpperBound; currentIndex >= insertionIndex; currentIndex--)
        {
            array.SetValue(currentIndex + 1, array.GetValue(currentIndex));
        }

        array.SetValue(insertionIndex, value);
        return RuntimeValue.Void;
    }

    private RuntimeValue EvaluateArrayRemoveBuiltin(RuntimeValue argument, int index, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYREMOVE", location);
        var removalIndex = RequireArrayRemovalIndex("ARRAYREMOVE", array, index, location);
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

    private bool EvaluateArrayContainsBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYCONTAINS", location);
        return ArrayContainsValue(array, value);
    }

    private int EvaluateArrayIndexOfBuiltin(RuntimeValue argument, RuntimeValue value, bool searchFromEnd, TextLocation location)
    {
        var functionName = searchFromEnd ? "ARRAYLASTINDEXOF" : "ARRAYINDEXOF";
        var array = RequireAllocatedArrayArgument(argument, functionName, location);

        return FindArrayIndex(functionName, array, value, searchFromEnd, location);
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

    private int EvaluateArrayBinarySearchBuiltin(RuntimeValue argument, RuntimeValue value, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYBINARYSEARCH", location);
        long left = array.LowerBound;
        long right = array.UpperBound;
        int? foundIndex = null;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var middleIndex = (int)middle;
            var comparison = Compare(array.GetValue(middleIndex), value, location);

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

        return foundIndex ?? GetArrayNotFoundIndex("ARRAYBINARYSEARCH", array, location);
    }

    private bool EvaluateArrayEveryBuiltin(RuntimeValue argument, RuntimeValue predicateArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYEVERY", location);
        var predicate = RequireFunctionArgument(predicateArgument, location);
        var every = true;

        ForEachArrayIndexAscending(array, index =>
        {
            if (every && !InvokeArrayPredicate(predicate, array.GetValue(index), location))
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
        var any = false;

        ForEachArrayIndexAscending(array, index =>
        {
            if (!any && InvokeArrayPredicate(predicate, array.GetValue(index), location))
            {
                any = true;
            }
        });

        return any;
    }

    private RuntimeValue EvaluateArrayFilterBuiltin(RuntimeValue argument, RuntimeValue predicateArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYFILTER", location);
        var arrayType = (ArrayTypeSymbol)argument.Type;
        var predicate = RequireFunctionArgument(predicateArgument, location);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexAscending(array, index =>
        {
            var value = array.GetValue(index);
            if (InvokeArrayPredicate(predicate, value, location))
            {
                values.Add(value);
            }
        });

        return CreateCompactArrayValue(arrayType, values);
    }

    private RuntimeValue EvaluateArrayMapBuiltin(RuntimeValue argument, RuntimeValue selectorArgument, TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYMAP", location);
        var selector = RequireFunctionArgument(selectorArgument, location);
        var resultType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(selector.Function.ReturnType);
        var values = new List<RuntimeValue>(array.Length);

        ForEachArrayIndexAscending(array, index =>
            values.Add(InvokeArrayCallback(selector, location, array.GetValue(index))));

        return CreateCompactArrayValue(resultType, values);
    }

    private RuntimeValue EvaluateArrayReduceBuiltin(
        RuntimeValue argument,
        RuntimeValue seed,
        RuntimeValue reducerArgument,
        TextLocation location)
    {
        var array = RequireAllocatedArrayArgument(argument, "ARRAYREDUCE", location);
        var reducer = RequireFunctionArgument(reducerArgument, location);
        var accumulator = seed;

        ForEachArrayIndexAscending(array, index =>
            accumulator = InvokeArrayCallback(reducer, location, accumulator, array.GetValue(index)));

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
        return InvokeArrayCallback(predicate, location, value).AsBoolean();
    }

    private RuntimeValue InvokeArrayCallback(RuntimeFunctionValue callback, TextLocation location, params RuntimeValue[] arguments)
    {
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

    private int GetExpandedArrayUpperBound(string functionName, RuntimeArrayValue array, TextLocation location)
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
                location);
            throw new RuntimeFailureException();
        }
    }

    private int RequireArrayInsertionIndex(string functionName, RuntimeArrayValue array, int index, TextLocation location)
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
                location);
            throw new RuntimeFailureException();
        }

        var allowedUpperBound = (long)array.UpperBound + 1;

        if (index < array.LowerBound || index > allowedUpperBound)
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

    private int RequireArrayRemovalIndex(string functionName, RuntimeArrayValue array, int index, TextLocation location)
    {
        if (array.Length == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                "index",
                "нельзя удалять элемент из пустого выделенного массива.",
                location);
            throw new RuntimeFailureException();
        }

        if (index < array.LowerBound || index > array.UpperBound)
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

        if (source.Rank != 1)
        {
            return RuntimeValue.FromArray(arrayType, source.Clone());
        }

        return CreateArrayValue(arrayType, source.LowerBound, source.UpperBound, source.GetValue);
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
