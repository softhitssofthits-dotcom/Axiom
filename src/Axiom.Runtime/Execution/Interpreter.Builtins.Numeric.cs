using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateRndBuiltin()
    {
        return BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble());
    }

    private RuntimeValue EvaluateCintBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return BuiltinNumericHelpers.EvaluateCint(value, expression.Location, ReportRuntimeDiagnostic);
    }

    private RuntimeValue EvaluateClngBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return BuiltinNumericHelpers.EvaluateClng(value, expression.Location, ReportRuntimeDiagnostic);
    }

    private RuntimeValue EvaluateCdblBuiltin(RuntimeValue value)
    {
        return BuiltinNumericHelpers.EvaluateCdbl(value);
    }

    private RuntimeValue EvaluateCdecBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return BuiltinNumericHelpers.EvaluateCdec(value, expression.Location, ReportRuntimeDiagnostic);
    }

    private int EvaluateIntBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var floored = value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Floor(value.AsDouble()), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Floor(value.AsDecimal()), GetBuiltInArgumentLocation(expression, 0), "Result of INT(value)"),
            _ => ReportInvalidRuntimeState<int>($"Unsupported INT argument type '{value.Type.Name}'.", expression.Location)
        };

        return floored;
    }

    private RuntimeValue EvaluateAbsBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(Math.Abs(value.AsInteger())),
                "Результат ABS",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(Math.Abs(value.AsLong())),
                "Результат ABS",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Abs(value.AsDouble()),
                "Результат ABS",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => decimal.Abs(value.AsDecimal()),
                "Результат ABS",
                GetBuiltInArgumentLocation(expression, 0)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported ABS argument type '{value.Type.Name}'.", expression.Location)
        };
    }

    private RuntimeValue EvaluateSgnBuiltin(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateSgn(value, location, (msg, loc) => ReportInvalidRuntimeState<int>(msg, loc));
    }

    private RuntimeValue EvaluateMinBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateMin(left, right, location, CompareNumericValues);
    }

    private RuntimeValue EvaluateMaxBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateMax(left, right, location, CompareNumericValues);
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
                "low",
                "не должен быть больше аргумента 'high'.",
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
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, argumentIndex), functionName, argumentName, (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
        var result = operation(operand);
        return EnsureFiniteDoubleResult(result, $"Result of {functionName}({argumentName})", expression.Location);
    }

    private RuntimeValue EvaluateAtn2Builtin(BoundCallExpression expression, RuntimeValue y, RuntimeValue x)
    {
        var left = BuiltinNumericHelpers.ToDoubleMathOperand(y, GetBuiltInArgumentLocation(expression, 0), "ATN2", "y", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
        var right = BuiltinNumericHelpers.ToDoubleMathOperand(x, GetBuiltInArgumentLocation(expression, 1), "ATN2", "x", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
        var result = Math.Atan2(left, right);
        return EnsureFiniteDoubleResult(result, "Result of ATN2(y, x)", expression.Location);
    }

    private RuntimeValue EvaluateSqrBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "SQR", "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);

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
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), functionName, "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);

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
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), functionName, "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);

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
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "ACOSH", "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);

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
        var operand = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "ATANH", "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);

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
        var left = BuiltinNumericHelpers.ToDoubleMathOperand(value, GetBuiltInArgumentLocation(expression, 0), "POW", "value", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
        var right = BuiltinNumericHelpers.ToDoubleMathOperand(exponent, GetBuiltInArgumentLocation(expression, 1), "POW", "exponent", (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
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
        var useNegativeSign = BuiltinNumericHelpers.HasNegativeSign(signFrom, expression.Location, (msg, loc) => ReportInvalidRuntimeState<bool>(msg, loc));

        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => BuiltinNumericHelpers.ApplySign(value.AsInteger(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => BuiltinNumericHelpers.ApplySign(value.AsLong(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                BuiltinNumericHelpers.ApplySign(value.AsDouble(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                GetBuiltInArgumentLocation(expression, 0)),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => BuiltinNumericHelpers.ApplySign(value.AsDecimal(), useNegativeSign),
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
        return BuiltinNumericHelpers.CompareNumericValues(left, right, location, (msg, loc) => ReportInvalidRuntimeState<int>(msg, loc));
    }

    private double ToDoubleMathOperand(RuntimeValue value, TextLocation location, string functionName, string argumentName)
    {
        return BuiltinNumericHelpers.ToDoubleMathOperand(value, location, functionName, argumentName, (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc), ReportBuiltInArgumentError);
    }

    private bool HasNegativeSign(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.HasNegativeSign(value, location, (msg, loc) => ReportInvalidRuntimeState<bool>(msg, loc));
    }
}
