using Axiom.Core;

namespace Axiom.Ide.Models;

public sealed record ProgramDiagnosticItem(
    string Code,
    string Stage,
    string Message,
    int? Line,
    int? Column,
    int? Length)
{
    public string Position =>
        Line.HasValue && Column.HasValue
            ? $"{Line}:{Column}"
            : "-";

    public static ProgramDiagnosticItem FromDiagnostic(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new ProgramDiagnosticItem(
            diagnostic.Code,
            ResolveStage(diagnostic.Code),
            diagnostic.Message,
            diagnostic.Location.Line,
            diagnostic.Location.Column,
            diagnostic.Location.Length);
    }

    public static ProgramDiagnosticItem CreateInfrastructure(string code, string message)
    {
        return new ProgramDiagnosticItem(code, "Infrastructure", message, null, null, null);
    }

    private static string ResolveStage(string code)
    {
        if (code.StartsWith("MB0", StringComparison.Ordinal) || code.StartsWith("MB1", StringComparison.Ordinal))
        {
            return "Syntax";
        }

        if (code.StartsWith("MB2", StringComparison.Ordinal))
        {
            return "Semantic";
        }

        if (code.StartsWith("MB3", StringComparison.Ordinal))
        {
            return "Runtime";
        }

        if (code.StartsWith("MB4", StringComparison.Ordinal))
        {
            return "Backend";
        }

        return "General";
    }
}
