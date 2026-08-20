namespace Axiom.Runtime;

internal sealed class EvaluationStackUnderflowException : Exception
{
    public EvaluationStackUnderflowException(string message)
        : base(message)
    {
    }
}
