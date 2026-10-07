using System.Globalization;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue ExecuteBuiltin(BuiltinBinding builtin, ReadOnlySpan<RuntimeValue> arguments, TextLocation location)
    {
        return ExecuteBuiltin(builtin, (IReadOnlyList<RuntimeValue>)arguments.ToArray(), location);
    }

    private RuntimeValue ExecuteBuiltin(BuiltinBinding builtin, IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        // Этот фолбэк достижим только для 15 variadic-ID массивных операций
        // (см. BytecodeVm.BuiltinDispatch): все остальные built-in исполняются
        // специализированными делегатами dispatch-таблицы.
        return builtin.CanonicalId switch
        {
            BuiltinFunctionId.LBound => RuntimeValue.FromInteger(EvaluateLBoundBuiltin(arguments, location)),
            BuiltinFunctionId.UBound => RuntimeValue.FromInteger(EvaluateUBoundBuiltin(arguments, location)),
            BuiltinFunctionId.ArraySlice => EvaluateArraySliceBuiltin(arguments, location),
            BuiltinFunctionId.ArrayToSorted => EvaluateArrayToSortedBuiltin(arguments, location),
            BuiltinFunctionId.ArrayToReversed => EvaluateArrayToReversedBuiltin(arguments, location),
            BuiltinFunctionId.ArrayTake => EvaluateArrayTakeBuiltin(arguments, location),
            BuiltinFunctionId.ArraySkip => EvaluateArraySkipBuiltin(arguments, location),
            BuiltinFunctionId.ArrayConcat => EvaluateArrayConcatBuiltin(arguments, location),
            BuiltinFunctionId.ArrayReverse => EvaluateArrayReverseBuiltin(arguments, location),
            BuiltinFunctionId.ArraySort => EvaluateArraySortBuiltin(arguments, location),
            BuiltinFunctionId.ArrayPush => EvaluateArrayPushBuiltin(arguments, location),
            BuiltinFunctionId.ArrayPop => EvaluateArrayPopBuiltin(arguments, location),
            BuiltinFunctionId.ArrayInsert => EvaluateArrayInsertBuiltin(arguments, location),
            BuiltinFunctionId.ArrayRemove => EvaluateArrayRemoveBuiltin(arguments, location),
            BuiltinFunctionId.ArrayBinarySearch => EvaluateArrayBinarySearchBuiltin(arguments, location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Built-in '{builtin.Name}' ({builtin.CanonicalId}) не поддерживает фолбэк-путь Bytecode VM.",
                location)
        };
    }

    private RuntimeValue EvaluateAbsBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(Math.Abs(value.AsInteger())),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(Math.Abs(value.AsLong())),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Abs(value.AsDouble()),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => decimal.Abs(value.AsDecimal()),
                "Результат ABS",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"ABS не поддерживает тип '{value.Type.Name}'.", location)
        };
    }

    private int EvaluateSgnBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => Math.Sign(value.AsInteger()),
            var type when type == TypeSymbol.Long => Math.Sign(value.AsLong()),
            var type when type == TypeSymbol.Double => Math.Sign(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => Math.Sign(value.AsDecimal()),
            _ => ReportInvalidRuntimeState<int>($"SGN не поддерживает тип '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateMinBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (Compare(left, right, location) <= 0)
        {
            return left.CopyForStorage();
        }

        return right.CopyForStorage();
    }

    private RuntimeValue EvaluateMaxBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (Compare(left, right, location) >= 0)
        {
            return left.CopyForStorage();
        }

        return right.CopyForStorage();
    }

    private RuntimeValue EvaluateClampBuiltin(RuntimeValue value, RuntimeValue minValue, RuntimeValue maxValue, TextLocation location)
    {
        if (Compare(minValue, maxValue, location) > 0)
        {
            ReportBuiltInArgumentError("CLAMP", "low", "не должен быть больше аргумента 'high'.", location);
            throw new RuntimeFailureException();
        }

        if (Compare(value, minValue, location) < 0)
        {
            return minValue.CopyForStorage();
        }

        if (Compare(value, maxValue, location) > 0)
        {
            return maxValue.CopyForStorage();
        }

        return value.CopyForStorage();
    }
}
