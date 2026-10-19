using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateNegate(RuntimeValue operand, TextLocation location)
    {
        return operand.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(-operand.AsInteger()),
                "Результат операции унарного '-'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(-operand.AsLong()),
                "Результат операции унарного '-'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                -operand.AsDouble(),
                "Результат операции унарного '-'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(-operand.AsDecimal()),
                "Результат операции унарного '-'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Унарный '-' не поддерживает тип '{operand.Type.Name}'.",
                location)
        };
    }

    private void ExecuteConvert(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var value = frame.EvaluationStack.PopUnchecked();
        var typeCode = (BytecodeScalarTypeCode)instruction.GetOperand(0);

        if (typeCode == BytecodeScalarTypeCode.Integer && value.Type == TypeSymbol.Integer)
        {
            frame.EvaluationStack.PushUnchecked(value);
            return;
        }

        if (typeCode == BytecodeScalarTypeCode.Long)
        {
            if (value.Type == TypeSymbol.Long)
            {
                frame.EvaluationStack.PushUnchecked(value);
                return;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromLong(GetIntegerUnchecked(value)));
                return;
            }
        }

        if (typeCode == BytecodeScalarTypeCode.Double)
        {
            if (value.Type == TypeSymbol.Double)
            {
                frame.EvaluationStack.PushUnchecked(value);
                return;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromDouble(GetIntegerUnchecked(value)));
                return;
            }

            if (value.Type == TypeSymbol.Long)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromDouble(GetLongUnchecked(value)));
                return;
            }

            if (value.Type == TypeSymbol.Decimal)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromDouble((double)GetDecimalUnchecked(value)));
                return;
            }
        }

        if (typeCode == BytecodeScalarTypeCode.Decimal)
        {
            if (value.Type == TypeSymbol.Decimal)
            {
                frame.EvaluationStack.PushUnchecked(value);
                return;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromDecimal(GetIntegerUnchecked(value)));
                return;
            }

            if (value.Type == TypeSymbol.Long)
            {
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromDecimal(GetLongUnchecked(value)));
                return;
            }
        }

        frame.EvaluationStack.PushUnchecked(ConvertValue(value, typeCode, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private RuntimeValue ConvertValue(RuntimeValue value, BytecodeScalarTypeCode targetType, TextLocation location)
    {
        if (targetType == BytecodeScalarTypeCode.Integer && value.Type == TypeSymbol.Integer)
        {
            return value;
        }

        if (targetType == BytecodeScalarTypeCode.Long)
        {
            if (value.Type == TypeSymbol.Long)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromLong(GetIntegerUnchecked(value));
            }
        }

        if (targetType == BytecodeScalarTypeCode.Double)
        {
            if (value.Type == TypeSymbol.Double)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromDouble(GetIntegerUnchecked(value));
            }

            if (value.Type == TypeSymbol.Long)
            {
                return RuntimeValue.FromDouble(GetLongUnchecked(value));
            }

            if (value.Type == TypeSymbol.Decimal)
            {
                return RuntimeValue.FromDouble((double)GetDecimalUnchecked(value));
            }
        }

        if (targetType == BytecodeScalarTypeCode.Decimal)
        {
            if (value.Type == TypeSymbol.Decimal)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromDecimal(GetIntegerUnchecked(value));
            }

            if (value.Type == TypeSymbol.Long)
            {
                return RuntimeValue.FromDecimal(GetLongUnchecked(value));
            }
        }

        var targetTypeSymbol = targetType switch
        {
            BytecodeScalarTypeCode.Integer => TypeSymbol.Integer,
            BytecodeScalarTypeCode.Long => TypeSymbol.Long,
            BytecodeScalarTypeCode.Double => TypeSymbol.Double,
            BytecodeScalarTypeCode.Decimal => TypeSymbol.Decimal,
            BytecodeScalarTypeCode.String => TypeSymbol.String,
            BytecodeScalarTypeCode.Date => TypeSymbol.Date,
            BytecodeScalarTypeCode.Time => TypeSymbol.Time,
            BytecodeScalarTypeCode.DateTime => TypeSymbol.DateTime,
            BytecodeScalarTypeCode.UtcDateTime => TypeSymbol.UtcDateTime,
            _ => TypeSymbol.Error
        };

        var temporalFailure = BuiltinDateTimeHelpers.TemporalConversionFailure.Unsupported;
        if (targetTypeSymbol != TypeSymbol.Error
            && BuiltinDateTimeHelpers.TryConvertTemporalValue(
                value,
                targetTypeSymbol,
                out var temporalValue,
                out temporalFailure))
        {
            return temporalValue;
        }

        if (temporalFailure == BuiltinDateTimeHelpers.TemporalConversionFailure.LosesComponent)
        {
            var lostComponent = targetTypeSymbol == TypeSymbol.Date ? "time" : "date";
            var explicitFunction = targetTypeSymbol == TypeSymbol.Date ? "DATEVALUE" : "TIMEVALUE";
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Cannot convert a full date-time value to '{targetTypeSymbol.Name}': the {lostComponent} component would be truncated. Use {explicitFunction} to drop it explicitly.",
                location);
        }

        return ReportInvalidRuntimeState<RuntimeValue>(
            $"Cannot convert type: {value.Type.Name} -> {targetTypeSymbol.Name}.",
            location);
    }

    private static bool IsZero(RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => GetIntegerUnchecked(value) == 0,
            var type when type == TypeSymbol.Long => GetLongUnchecked(value) == 0L,
            var type when type == TypeSymbol.Double => GetDoubleUnchecked(value) == 0d,
            var type when type == TypeSymbol.Decimal => GetDecimalUnchecked(value) == 0m,
            _ => false
        };
    }

    private RuntimeValue EvaluateNot(RuntimeValue operand, TextLocation location)
    {
        return operand.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!operand.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(~operand.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(~operand.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Оператор NOT не поддерживает тип '{operand.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateAdd(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.String => RuntimeValue.FromString(RuntimeStringLimits.Concat(left.AsString(), right.AsString())),
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() + right.AsInteger()),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() + right.AsLong()),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() + right.AsDouble(),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() + right.AsDecimal()),
                "Результат операции '+'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '+' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateSubtract(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() - right.AsInteger()),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() - right.AsLong()),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() - right.AsDouble(),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() - right.AsDecimal()),
                "Результат операции '-'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '-' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateMultiply(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() * right.AsInteger()),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() * right.AsLong()),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() * right.AsDouble(),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() * right.AsDecimal()),
                "Результат операции '*'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '*' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluatePower(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return EvaluatePowerOperation(
            left.AsDouble(),
            right.AsDouble(),
            "Результат операции '^'",
            location);
    }

    private RuntimeValue EvaluateDivide(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type == TypeSymbol.Decimal)
        {
            var divisorDecimal = right.AsDecimal();

            if (divisorDecimal == 0m)
            {
                ReportDivisionByZero(location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedDecimalOperation(
                () => left.AsDecimal() / divisorDecimal,
                "Результат операции '/'",
                location);
        }

        var divisor = right.AsDouble();
        if (divisor == 0d)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(left.AsDouble() / divisor, "Результат операции '/'", location);
    }

    private RuntimeValue EvaluateIntegralDivide(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type == TypeSymbol.Long)
        {
            var divisor = right.AsLong();
            if (divisor == 0L)
            {
                ReportDivisionByZero(location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedLongOperation(
                () => checked(left.AsLong() / divisor),
                "Результат операции '\\'",
                location);
        }

        var integerDivisor = right.AsInteger();
        if (integerDivisor == 0)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(
            () => checked(left.AsInteger() / integerDivisor),
            "Результат операции '\\'",
            location);
    }

    private RuntimeValue EvaluateModulo(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateIntegerModulo(left.AsInteger(), right.AsInteger(), location),
            var type when type == TypeSymbol.Long => EvaluateLongModulo(left.AsLong(), right.AsLong(), location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция MOD не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateIntegerModulo(int left, int right, TextLocation location)
    {
        if (right == 0)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(() => checked(left % right), "Результат операции 'MOD'", location);
    }

    private RuntimeValue EvaluateLongModulo(long left, long right, TextLocation location)
    {
        if (right == 0L)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedLongOperation(() => checked(left % right), "Результат операции 'MOD'", location);
    }

    private RuntimeValue EvaluateShiftLeft(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        var count = GetShiftCount(right);
        if (count < 0 || count > 63)
            return ReportInvalidRuntimeState<RuntimeValue>($"Сдвиг на {count} вне допустимого диапазона 0..63.", location);

        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() << count),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() << count),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция << не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateShiftRight(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        var count = GetShiftCount(right);
        if (count < 0 || count > 63)
            return ReportInvalidRuntimeState<RuntimeValue>($"Сдвиг на {count} вне допустимого диапазона 0..63.", location);

        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() >> count),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() >> count),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция >> не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private static int GetShiftCount(RuntimeValue value)
    {
        return value.Type == TypeSymbol.Long
            ? unchecked((int)value.AsLong())
            : value.AsInteger();
    }

    private RuntimeValue EvaluateAnd(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() && right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() & right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() & right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция AND не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateOr(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() || right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() | right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() | right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция OR не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateXor(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() ^ right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() ^ right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() ^ right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция XOR не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateEqv(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() == right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(~(left.AsInteger() ^ right.AsInteger())),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(~(left.AsLong() ^ right.AsLong())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция EQV не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateImp(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!left.AsBoolean() || right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger((~left.AsInteger()) | right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong((~left.AsLong()) | right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция IMP не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        return RuntimeValueHelpers.ValuesEqual(left, right);
    }

    private int Compare(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        try
        {
            return RuntimeValueHelpers.Compare(left, right);
        }
        catch (InvalidOperationException)
        {
            return ReportInvalidRuntimeState<int>(
                $"Сравнение не поддерживается для типа '{left.Type.Name}'.",
                location);
        }
    }

    private static bool RecordsEqual(RuntimeRecordValue left, RuntimeRecordValue right)
    {
        return RuntimeValueHelpers.RecordsEqual(left, right);
    }

    private RuntimeValue EvaluateCheckedIntegerOperation(Func<int> operation, string detail, TextLocation location)
    {
        return RuntimeArithmeticHelpers.EvaluateCheckedIntegerOperation(operation, detail, location, ReportNumericOverflow);
    }

    private RuntimeValue EvaluateCheckedLongOperation(Func<long> operation, string detail, TextLocation location)
    {
        return RuntimeArithmeticHelpers.EvaluateCheckedLongOperation(operation, detail, location, ReportNumericOverflow);
    }

    private RuntimeValue EvaluateCheckedDecimalOperation(Func<decimal> operation, string detail, TextLocation location)
    {
        return RuntimeArithmeticHelpers.EvaluateCheckedDecimalOperation(operation, detail, location, ReportNumericOverflow);
    }

    private RuntimeValue EnsureFiniteDoubleResult(double value, string detail, TextLocation location)
    {
        return RuntimeArithmeticHelpers.EnsureFiniteDoubleResult(value, detail, location, ReportNumericOverflow);
    }

    private RuntimeValue EvaluatePowerOperation(double baseValue, double exponentValue, string detail, TextLocation location)
    {
        return RuntimeArithmeticHelpers.EvaluatePowerOperation(baseValue, exponentValue, detail, location, ReportRuntimeDiagnostic);
    }

}
