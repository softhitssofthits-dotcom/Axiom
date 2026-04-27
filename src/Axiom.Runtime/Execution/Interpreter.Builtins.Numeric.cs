using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateRndBuiltin()
    {
        return RuntimeValue.FromDouble(_random.NextDouble());
    }

    private RuntimeValue EvaluateAbsBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(Math.Abs(value.AsInteger())),
                "Result of ABS(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(Math.Abs(value.AsLong())),
                "Result of ABS(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Abs(value.AsDouble()),
                "Result of ABS(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => decimal.Abs(value.AsDecimal()),
                "Result of ABS(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported ABS argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateSgnBuiltin(RuntimeValue value, TextLocation location)
    {
        var sign = value.Type switch
        {
            var type when type == TypeSymbol.Integer => Math.Sign(value.AsInteger()),
            var type when type == TypeSymbol.Long => Math.Sign(value.AsLong()),
            var type when type == TypeSymbol.Double => Math.Sign(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => Math.Sign(value.AsDecimal()),
            _ => ReportInvalidRuntimeState<int>($"Unsupported SGN argument type '{value.Type.Name}'.", location)
        };

        return RuntimeValue.FromInteger(sign);
    }

    private RuntimeValue EvaluateMinBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return CompareNumericValues(left, right, location) <= 0 ? left : right;
    }

    private RuntimeValue EvaluateMaxBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return CompareNumericValues(left, right, location) >= 0 ? left : right;
    }

    private RuntimeValue EvaluateClampBuiltin(
        BoundCallExpression expression,
        RuntimeValue value,
        RuntimeValue low,
        RuntimeValue high)
    {
        if (CompareNumericValues(low, high, expression.Location) > 0)
        {
            ReportBuiltInArgumentError(
                "CLAMP",
                "high",
                "must be greater than or equal to low.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        if (CompareNumericValues(value, low, expression.Location) < 0)
        {
            return low;
        }

        if (CompareNumericValues(value, high, expression.Location) > 0)
        {
            return high;
        }

        return value;
    }

    private RuntimeValue EvaluateDoubleMathBuiltin(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        RuntimeValue value,
        Func<double, double> operation)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, argumentIndex), functionName, argumentName);
        var result = operation(operand);
        return EnsureFiniteDoubleResult(result, $"Result of {functionName}({argumentName})", expression.Location);
    }

    private RuntimeValue EvaluateAtn2Builtin(BoundCallExpression expression, RuntimeValue y, RuntimeValue x)
    {
        var left = ToDoubleMathOperand(y, GetBuiltInArgumentLocation(expression, 0), "ATN2", "y");
        var right = ToDoubleMathOperand(x, GetBuiltInArgumentLocation(expression, 1), "ATN2", "x");
        var result = Math.Atan2(left, right);
        return EnsureFiniteDoubleResult(result, "Result of ATN2(y, x)", expression.Location);
    }

    private RuntimeValue EvaluateSqrBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "SQR", "value");

        if (operand < 0d)
        {
            ReportBuiltInArgumentError(
                "SQR",
                "value",
                "must be greater than or equal to 0.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Sqrt(operand), "Result of SQR(value)", expression.Location);
    }

    private RuntimeValue EvaluateLogBuiltin(
        BoundCallExpression expression,
        string functionName,
        RuntimeValue value,
        Func<double, double> operation)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), functionName, "value");

        if (operand <= 0d)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "must be greater than 0.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(operation(operand), $"Result of {functionName}(value)", expression.Location);
    }

    private RuntimeValue EvaluateInverseTrigBuiltin(
        BoundCallExpression expression,
        string functionName,
        RuntimeValue value,
        Func<double, double> operation)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), functionName, "value");

        if (operand < -1d || operand > 1d)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "must be between -1 and 1 inclusive.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(operation(operand), $"Result of {functionName}(value)", expression.Location);
    }

    private RuntimeValue EvaluateAcoshBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "ACOSH", "value");

        if (operand < 1d)
        {
            ReportBuiltInArgumentError(
                "ACOSH",
                "value",
                "must be greater than or equal to 1.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Acosh(operand), "Result of ACOSH(value)", expression.Location);
    }

    private RuntimeValue EvaluateAtanhBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var operand = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "ATANH", "value");

        if (operand <= -1d || operand >= 1d)
        {
            ReportBuiltInArgumentError(
                "ATANH",
                "value",
                "must be greater than -1 and less than 1.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Atanh(operand), "Result of ATANH(value)", expression.Location);
    }

    private RuntimeValue EvaluatePowBuiltin(BoundCallExpression expression, RuntimeValue value, RuntimeValue exponent)
    {
        var left = ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "POW", "value");
        var right = ToDoubleMathOperand(exponent, GetBuiltInArgumentLocation(expression, 1), "POW", "exponent");
        var result = Math.Pow(left, right);

        if (double.IsNaN(result))
        {
            ReportBuiltInArgumentError(
                "POW",
                "value",
                "cannot be used with the specified exponent.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(result, "Result of POW(value, exponent)", expression.Location);
    }

    private RuntimeValue EvaluateSetSignBuiltin(BoundCallExpression expression, RuntimeValue value, RuntimeValue signFrom)
    {
        var useNegativeSign = HasNegativeSign(signFrom, expression.Location);

        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => ApplySign(value.AsInteger(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => ApplySign(value.AsLong(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                ApplySign(value.AsDouble(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => ApplySign(value.AsDecimal(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported SETSIGN value type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateFloorBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value,
            var type when type == TypeSymbol.Long => value,
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Floor(value.AsDouble()),
                "Result of FLOOR(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Floor(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported FLOOR argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateCeilBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value,
            var type when type == TypeSymbol.Long => value,
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Ceiling(value.AsDouble()),
                "Result of CEIL(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Ceiling(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported CEIL argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateRoundBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value,
            var type when type == TypeSymbol.Long => value,
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Round(value.AsDouble(), 0, MidpointRounding.AwayFromZero),
                "Result of ROUND(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(
                decimal.Round(value.AsDecimal(), 0, MidpointRounding.AwayFromZero)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported ROUND argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateFixBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value,
            var type when type == TypeSymbol.Long => value,
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Truncate(value.AsDouble()),
                "Result of FIX(value)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Truncate(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported FIX argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private int CompareNumericValues(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type != right.Type)
        {
            return ReportInvalidRuntimeState<int>(
                $"Numeric comparison requires matching runtime types, but got '{left.Type.Name}' and '{right.Type.Name}'.",
                location);
        }

        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => left.AsInteger().CompareTo(right.AsInteger()),
            var type when type == TypeSymbol.Long => left.AsLong().CompareTo(right.AsLong()),
            var type when type == TypeSymbol.Double => left.AsDouble().CompareTo(right.AsDouble()),
            var type when type == TypeSymbol.Decimal => left.AsDecimal().CompareTo(right.AsDecimal()),
            _ => ReportInvalidRuntimeState<int>($"Unsupported numeric comparison type '{left.Type.Name}'.", location)
        };
    }

    private double ToDoubleMathOperand(RuntimeValue value, TextLocation location, string functionName, string argumentName)
    {
        var numericValue = value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => value.AsLong(),
            var type when type == TypeSymbol.Double => value.AsDouble(),
            var type when type == TypeSymbol.Decimal => (double)value.AsDecimal(),
            _ => ReportInvalidRuntimeState<double>($"Unsupported numeric argument type '{value.Type.Name}'.", location)
        };

        if (!double.IsFinite(numericValue))
        {
            ReportBuiltInArgumentError(functionName, argumentName, "must be a finite numeric value.", location);
            throw new RuntimeFailureException();
        }

        return numericValue;
    }

    private bool HasNegativeSign(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() < 0,
            var type when type == TypeSymbol.Long => value.AsLong() < 0,
            var type when type == TypeSymbol.Double => double.IsNegative(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => decimal.IsNegative(value.AsDecimal()),
            _ => ReportInvalidRuntimeState<bool>($"Unsupported SETSIGN sign source type '{value.Type.Name}'.", location)
        };
    }

    private static int ApplySign(int value, bool useNegativeSign)
    {
        return useNegativeSign
            ? value <= 0 ? value : checked(-value)
            : checked(Math.Abs(value));
    }

    private static long ApplySign(long value, bool useNegativeSign)
    {
        return useNegativeSign
            ? value <= 0 ? value : checked(-value)
            : checked(Math.Abs(value));
    }

    private static double ApplySign(double value, bool useNegativeSign)
    {
        var magnitude = Math.Abs(value);
        return useNegativeSign ? -magnitude : magnitude;
    }

    private static decimal ApplySign(decimal value, bool useNegativeSign)
    {
        var magnitude = decimal.Abs(value);
        return useNegativeSign ? -magnitude : magnitude;
    }
}
