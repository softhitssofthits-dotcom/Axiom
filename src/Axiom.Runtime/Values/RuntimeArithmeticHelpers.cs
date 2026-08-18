using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public static class RuntimeArithmeticHelpers
{
    public static RuntimeValue EvaluateCheckedIntegerOperation(
        Func<int> operation,
        string detail,
        TextLocation location,
        Action<string, TypeSymbol, TextLocation> reportOverflow)
    {
        try
        {
            return RuntimeValue.FromInteger(operation());
        }
        catch (OverflowException)
        {
            reportOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateCheckedLongOperation(
        Func<long> operation,
        string detail,
        TextLocation location,
        Action<string, TypeSymbol, TextLocation> reportOverflow)
    {
        try
        {
            return RuntimeValue.FromLong(operation());
        }
        catch (OverflowException)
        {
            reportOverflow(detail, TypeSymbol.Long, location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateCheckedDecimalOperation(
        Func<decimal> operation,
        string detail,
        TextLocation location,
        Action<string, TypeSymbol, TextLocation> reportOverflow)
    {
        try
        {
            return RuntimeValue.FromDecimal(operation());
        }
        catch (OverflowException)
        {
            reportOverflow(detail, TypeSymbol.Decimal, location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EnsureFiniteDoubleResult(
        double value,
        string detail,
        TextLocation location,
        Action<string, TypeSymbol, TextLocation> reportOverflow)
    {
        if (!double.IsFinite(value))
        {
            reportOverflow(detail, TypeSymbol.Double, location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromDouble(value);
    }

    public static void ReportNumericOverflow(
        string detail,
        TypeSymbol type,
        TextLocation location,
        Action<string, string, TextLocation> reportDiagnostic)
    {
        reportDiagnostic(
            DiagnosticCodes.NumericOverflow,
            $"{detail} выходит за диапазон {type.Name}.",
            location);
    }

    public static void ReportDivisionByZero(
        TextLocation location,
        Action<string, string, TextLocation> reportDiagnostic)
    {
        reportDiagnostic(
            DiagnosticCodes.DivisionByZero,
            "Деление на ноль.",
            location);
    }
}
