namespace Axiom.Ide.Models;

public enum ProgramRunStatus
{
    Success,
    SyntaxFailure,
    SemanticFailure,
    BackendFailure,
    RuntimeFailure,
    Cancelled,
    InfrastructureFailure
}
