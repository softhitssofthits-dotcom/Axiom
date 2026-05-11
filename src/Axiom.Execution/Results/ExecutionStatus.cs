namespace Axiom.Execution;

public enum ExecutionStatus
{
    Success,
    SyntaxFailure,
    SemanticFailure,
    BackendFailure,
    RuntimeFailure,
    Cancelled
}
