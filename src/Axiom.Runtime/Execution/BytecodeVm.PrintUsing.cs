using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void AppendFormattedUsing(
        string format,
        ReadOnlySpan<RuntimeValue> values,
        TextLocation statementLocation)
    {
        var valuesArray = values.ToArray();
        var formatted = BuiltinPrintUsingHelpers.FormatUsing(
            format,
            index => valuesArray[index],
            valuesArray.Length,
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
