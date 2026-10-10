namespace Axiom.Ide.Models;

public enum ProgramCompilationStatus
{
    Success,
    SyntaxFailure,
    SemanticFailure,
    BackendFailure,
    InfrastructureFailure
}
