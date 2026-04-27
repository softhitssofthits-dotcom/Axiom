using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeFunctionValue
{
    public RuntimeFunctionValue(FunctionSymbol function, RuntimeScope capturedScope)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedScope);

        Function = function;
        CapturedScope = capturedScope;
    }

    internal RuntimeFunctionValue(FunctionSymbol function, IReadOnlyList<BytecodeVariableSlot> capturedSlots)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedSlots);

        Function = function;
        CapturedScope = null!;
        BytecodeCapturedSlots = capturedSlots;
    }

    public FunctionSymbol Function { get; }

    public RuntimeScope CapturedScope { get; }

    internal IReadOnlyList<BytecodeVariableSlot>? BytecodeCapturedSlots { get; }
}
