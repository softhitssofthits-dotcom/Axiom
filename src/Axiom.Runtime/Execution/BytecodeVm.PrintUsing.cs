using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void AppendFormattedUsing(
        string format,
        RuntimeValue[] values,
        int valueCount,
        TextLocation statementLocation)
    {
        var formatted = BuiltinPrintUsingHelpers.FormatUsing(
            format,
            values,
            valueCount,
            detail =>
            {
                ReportPrintUsingError(detail, statementLocation);
                return new RuntimeFailureException();
            });

        _hasPendingOutput = true;
        RuntimeStringLimits.Append(_pendingOutput, formatted);
    }

    private void ReportPrintUsingError(string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, $"PRINT USING: {detail}", location);
    }
}
