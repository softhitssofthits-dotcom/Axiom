namespace Axiom.Runtime;

internal readonly record struct ExecutionSignal(bool HasReturn, bool HasExitFor, bool HasExitDo, RuntimeValue? Value)
{
    public static ExecutionSignal None { get; } = new(false, false, false, Value: null);

    public static ExecutionSignal Return(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ExecutionSignal(true, false, false, value);
    }

    public static ExecutionSignal ExitFor()
    {
        return new ExecutionSignal(false, true, false, Value: null);
    }

    public static ExecutionSignal ExitDo()
    {
        return new ExecutionSignal(false, false, true, Value: null);
    }
}
