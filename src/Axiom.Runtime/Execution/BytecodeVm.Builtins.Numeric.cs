using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateRndBuiltin()
    {
        return BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble());
    }

    private RuntimeValue EvaluateCintBuiltin(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateCint(value, location, ReportRuntimeDiagnostic);
    }

    private RuntimeValue EvaluateClngBuiltin(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateClng(value, location, ReportRuntimeDiagnostic);
    }

    private RuntimeValue EvaluateCdblBuiltin(RuntimeValue value)
    {
        return BuiltinNumericHelpers.EvaluateCdbl(value);
    }

    private RuntimeValue EvaluateCdecBuiltin(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.EvaluateCdec(value, location, ReportRuntimeDiagnostic);
    }

    private int EvaluateIntBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), location, "Result of INT(value)"),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Floor(value.AsDouble()), location, "Result of INT(value)"),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Floor(value.AsDecimal()), location, "Result of INT(value)"),
            _ => ReportInvalidRuntimeState<int>($"Unsupported INT argument type '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateDoubleMathBuiltin(
        string functionName,
        string argumentName,
        RuntimeValue value,
        Func<double, double> operation,
        TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, functionName, argumentName);
        var result = operation(operand);
        return EnsureFiniteDoubleResult(result, $"Result of {functionName}({argumentName})", location);
    }

    private RuntimeValue EvaluateAtn2Builtin(RuntimeValue y, RuntimeValue x, TextLocation location)
    {
        var left = ToDoubleMathOperand(y, location, "ATN2", "y");
        var right = ToDoubleMathOperand(x, location, "ATN2", "x");
        var result = Math.Atan2(left, right);
        return EnsureFiniteDoubleResult(result, "Result of ATN2(y, x)", location);
    }

    private RuntimeValue EvaluateSqrBuiltin(RuntimeValue value, TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, "SQR", "value");

        if (operand < 0d)
        {
            ReportBuiltInArgumentError("SQR", "value", "must be greater than or equal to 0.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Sqrt(operand), "Result of SQR(value)", location);
    }

    private RuntimeValue EvaluateLogBuiltin(
        string functionName,
        RuntimeValue value,
        Func<double, double> operation,
        TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, functionName, "value");

        if (operand <= 0d)
        {
            ReportBuiltInArgumentError(functionName, "value", "must be greater than 0.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(operation(operand), $"Result of {functionName}(value)", location);
    }

    private RuntimeValue EvaluateInverseTrigBuiltin(
        string functionName,
        RuntimeValue value,
        Func<double, double> operation,
        TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, functionName, "value");

        if (operand < -1d || operand > 1d)
        {
            ReportBuiltInArgumentError(functionName, "value", "must be between -1 and 1 inclusive.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(operation(operand), $"Result of {functionName}(value)", location);
    }

    private RuntimeValue EvaluateAcoshBuiltin(RuntimeValue value, TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, "ACOSH", "value");

        if (operand < 1d)
        {
            ReportBuiltInArgumentError("ACOSH", "value", "must be greater than or equal to 1.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Acosh(operand), "Result of ACOSH(value)", location);
    }

    private RuntimeValue EvaluateAtanhBuiltin(RuntimeValue value, TextLocation location)
    {
        var operand = ToDoubleMathOperand(value, location, "ATANH", "value");

        if (operand <= -1d || operand >= 1d)
        {
            ReportBuiltInArgumentError("ATANH", "value", "must be greater than -1 and less than 1.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(Math.Atanh(operand), "Result of ATANH(value)", location);
    }

    private RuntimeValue EvaluatePowBuiltin(RuntimeValue value, RuntimeValue exponent, TextLocation location)
    {
        var left = ToDoubleMathOperand(value, location, "POW", "value");
        var right = ToDoubleMathOperand(exponent, location, "POW", "exponent");
        var result = Math.Pow(left, right);

        if (double.IsNaN(result))
        {
            ReportBuiltInArgumentError("POW", "value", "cannot be used with the specified exponent.", location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(result, "Result of POW(value, exponent)", location);
    }

    private RuntimeValue EvaluateSetSignBuiltin(RuntimeValue value, RuntimeValue signFrom, TextLocation location)
    {
        var useNegativeSign = HasNegativeSign(signFrom, location);

        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => ApplySign(value.AsInteger(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => ApplySign(value.AsLong(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                ApplySign(value.AsDouble(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => ApplySign(value.AsDecimal(), useNegativeSign),
                "Result of SETSIGN(value, signFrom)",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported SETSIGN value type '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateFloorBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.CopyForStorage(),
            var type when type == TypeSymbol.Long => value.CopyForStorage(),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Floor(value.AsDouble()),
                "Result of FLOOR(value)",
                location),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Floor(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported FLOOR argument type '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateCeilBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.CopyForStorage(),
            var type when type == TypeSymbol.Long => value.CopyForStorage(),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Ceiling(value.AsDouble()),
                "Result of CEIL(value)",
                location),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Ceiling(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported CEIL argument type '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateRoundBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.CopyForStorage(),
            var type when type == TypeSymbol.Long => value.CopyForStorage(),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Round(value.AsDouble(), 0, MidpointRounding.AwayFromZero),
                "Result of ROUND(value)",
                location),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(
                decimal.Round(value.AsDecimal(), 0, MidpointRounding.AwayFromZero)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported ROUND argument type '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateFixBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.CopyForStorage(),
            var type when type == TypeSymbol.Long => value.CopyForStorage(),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Truncate(value.AsDouble()),
                "Result of FIX(value)",
                location),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromDecimal(decimal.Truncate(value.AsDecimal())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Unsupported FIX argument type '{value.Type.Name}'.", location)
        };
    }

    private double ToDoubleMathOperand(RuntimeValue value, TextLocation location, string functionName, string argumentName)
    {
        return BuiltinNumericHelpers.ToDoubleMathOperand(value, location, functionName, argumentName,
            (msg, loc) => ReportInvalidRuntimeState<double>(msg, loc),
            ReportBuiltInArgumentError);
    }

    private int ToInt32Checked(double value, TextLocation location, string detail)
    {
        return RuntimeArithmeticHelpers.ToInt32Checked(value, location, detail, ReportRuntimeDiagnostic);
    }

    private int ToInt32Checked(long value, TextLocation location, string detail)
    {
        return RuntimeArithmeticHelpers.ToInt32Checked(value, location, detail, ReportRuntimeDiagnostic);
    }

    private int ToInt32Checked(decimal value, TextLocation location, string detail)
    {
        return RuntimeArithmeticHelpers.ToInt32Checked(value, location, detail, ReportRuntimeDiagnostic);
    }

    private bool HasNegativeSign(RuntimeValue value, TextLocation location)
    {
        return BuiltinNumericHelpers.HasNegativeSign(value, location, (msg, loc) => ReportInvalidRuntimeState<bool>(msg, loc));
    }

    private static int ApplySign(int value, bool useNegativeSign)
    {
        return BuiltinNumericHelpers.ApplySign(value, useNegativeSign);
    }

    private static long ApplySign(long value, bool useNegativeSign)
    {
        return BuiltinNumericHelpers.ApplySign(value, useNegativeSign);
    }

    private static double ApplySign(double value, bool useNegativeSign)
    {
        return BuiltinNumericHelpers.ApplySign(value, useNegativeSign);
    }

    private static decimal ApplySign(decimal value, bool useNegativeSign)
    {
        return BuiltinNumericHelpers.ApplySign(value, useNegativeSign);
    }
}
