namespace Axiom.Runtime;

internal readonly record struct ExecutionSignal(bool HasReturn, bool HasExitFor, bool HasExitDo, bool HasContinueFor, bool HasContinueWhile, bool HasContinueDo, RuntimeValue? Value)
{
    public static ExecutionSignal None { get; } = new(false, false, false, false, false, false, Value: null);

    public static ExecutionSignal Return(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ExecutionSignal(true, false, false, false, false, false, value);
    }

    public static ExecutionSignal ExitFor()
    {
        return new ExecutionSignal(false, true, false, false, false, false, Value: null);
    }

    public static ExecutionSignal ExitDo()
    {
        return new ExecutionSignal(false, false, true, false, false, false, Value: null);
    }

    public static ExecutionSignal ContinueFor()
    {
        return new ExecutionSignal(false, false, false, true, false, false, Value: null);
    }

    public static ExecutionSignal ContinueWhile()
    {
        return new ExecutionSignal(false, false, false, false, true, false, Value: null);
    }

    public static ExecutionSignal ContinueDo()
    {
        return new ExecutionSignal(false, false, false, false, false, true, Value: null);
    }
}
