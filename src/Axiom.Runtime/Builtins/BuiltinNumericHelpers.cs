using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinNumericHelpers
{
    public static RuntimeValue EvaluateRnd(Func<double> nextRandom)
    {
        return RuntimeValue.FromDouble(nextRandom());
    }

    public static RuntimeValue EvaluateAbs(RuntimeValue value, TextLocation location,
        Func<string, TextLocation, RuntimeValue> reportInvalidState,
        Func<string, TextLocation, RuntimeValue> checkedInteger,
        Func<string, TextLocation, RuntimeValue> checkedLong,
        Func<double, string, TextLocation, RuntimeValue> ensureFiniteDouble,
        Func<string, TextLocation, RuntimeValue> checkedDecimal)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => checkedInteger(
                "Result of ABS(value)", location),
            var type when type == TypeSymbol.Long => checkedLong(
                "Result of ABS(value)", location),
            var type when type == TypeSymbol.Double => ensureFiniteDouble(
                Math.Abs(value.AsDouble()), "Result of ABS(value)", location),
            var type when type == TypeSymbol.Decimal => checkedDecimal(
                "Result of ABS(value)", location),
            _ => reportInvalidState($"Unsupported ABS argument type '{value.Type.Name}'.", location)
        };
    }

    public static RuntimeValue EvaluateSgn(RuntimeValue value, TextLocation location,
        Func<string, TextLocation, int> reportInvalidState)
    {
        var sign = value.Type switch
        {
            var type when type == TypeSymbol.Integer => Math.Sign(value.AsInteger()),
            var type when type == TypeSymbol.Long => Math.Sign(value.AsLong()),
            var type when type == TypeSymbol.Double => Math.Sign(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => Math.Sign(value.AsDecimal()),
            _ => reportInvalidState($"Unsupported SGN argument type '{value.Type.Name}'.", location)
        };

        return RuntimeValue.FromInteger(sign);
    }

    public static RuntimeValue EvaluateMin(RuntimeValue left, RuntimeValue right, TextLocation location,
        Func<RuntimeValue, RuntimeValue, TextLocation, int> compare)
    {
        return compare(left, right, location) <= 0 ? left : right;
    }

    public static RuntimeValue EvaluateMax(RuntimeValue left, RuntimeValue right, TextLocation location,
        Func<RuntimeValue, RuntimeValue, TextLocation, int> compare)
    {
        return compare(left, right, location) >= 0 ? left : right;
    }

    public static int CompareNumericValues(RuntimeValue left, RuntimeValue right, TextLocation location,
        Func<string, TextLocation, int> reportInvalidState)
    {
        if (left.Type != right.Type)
        {
            return reportInvalidState(
                $"Numeric comparison requires matching runtime types, but got '{left.Type.Name}' and '{right.Type.Name}'.",
                location);
        }

        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => left.AsInteger().CompareTo(right.AsInteger()),
            var type when type == TypeSymbol.Long => left.AsLong().CompareTo(right.AsLong()),
            var type when type == TypeSymbol.Double => left.AsDouble().CompareTo(right.AsDouble()),
            var type when type == TypeSymbol.Decimal => left.AsDecimal().CompareTo(right.AsDecimal()),
            _ => reportInvalidState($"Unsupported numeric comparison type '{left.Type.Name}'.", location)
        };
    }

    public static double ToDoubleMathOperand(RuntimeValue value, TextLocation location,
        string functionName, string argumentName,
        Func<string, TextLocation, double> reportInvalidState,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        var numericValue = value.Type switch
        {
            var type when type == TypeSymbol.Integer => (double)value.AsInteger(),
            var type when type == TypeSymbol.Long => (double)value.AsLong(),
            var type when type == TypeSymbol.Double => value.AsDouble(),
            var type when type == TypeSymbol.Decimal => (double)value.AsDecimal(),
            _ => reportInvalidState($"Unsupported numeric argument type '{value.Type.Name}'.", location)
        };

        if (!double.IsFinite(numericValue))
        {
            reportArgumentError(functionName, argumentName, "must be a finite numeric value.", location);
            throw new RuntimeFailureException();
        }

        return numericValue;
    }

    public static bool HasNegativeSign(RuntimeValue value, TextLocation location,
        Func<string, TextLocation, bool> reportInvalidState)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() < 0,
            var type when type == TypeSymbol.Long => value.AsLong() < 0,
            var type when type == TypeSymbol.Double => double.IsNegative(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => decimal.IsNegative(value.AsDecimal()),
            _ => reportInvalidState($"Unsupported SETSIGN sign source type '{value.Type.Name}'.", location)
        };
    }

    public static int ApplySign(int value, bool useNegativeSign)
    {
        return useNegativeSign
            ? value <= 0 ? value : checked(-value)
            : checked(Math.Abs(value));
    }

    public static long ApplySign(long value, bool useNegativeSign)
    {
        return useNegativeSign
            ? value <= 0 ? value : checked(-value)
            : checked(Math.Abs(value));
    }

    public static double ApplySign(double value, bool useNegativeSign)
    {
        var magnitude = Math.Abs(value);
        return useNegativeSign ? -magnitude : magnitude;
    }

    public static decimal ApplySign(decimal value, bool useNegativeSign)
    {
        var magnitude = decimal.Abs(value);
        return useNegativeSign ? -magnitude : magnitude;
    }
}
