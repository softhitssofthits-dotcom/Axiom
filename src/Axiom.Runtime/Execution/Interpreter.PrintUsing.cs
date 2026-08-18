using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private string FormatUsing(
        string format,
        IReadOnlyList<(RuntimeValue Value, TextLocation Location)> values,
        TextLocation statementLocation)
    {
        return BuiltinPrintUsingHelpers.FormatUsing(
            format,
            index => values[index].Value,
            values.Count,
            detail =>
            {
                ReportPrintUsingError(detail, statementLocation);
                return new RuntimeFailureException();
            });
    }

    private void ReportPrintUsingError(string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, $"PRINT USING: {detail}", location);
    }
}
