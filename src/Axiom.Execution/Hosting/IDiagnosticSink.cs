using Axiom.Core;

namespace Axiom.Execution;

public interface IDiagnosticSink
{
    void ReportDiagnostic(Diagnostic diagnostic);
}
