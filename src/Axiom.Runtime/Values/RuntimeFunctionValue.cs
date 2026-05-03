using Axiom.Bytecode;
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
        : this(function, capturedSlots, bytecodeBuiltinBindingId: null)
    {
    }

    internal RuntimeFunctionValue(BytecodeRoutine routine, BytecodeVariableSlot[] capturedSlots)
        : this(routine, capturedSlots, bytecodeBuiltinBindingId: null)
    {
    }

    internal RuntimeFunctionValue(
        FunctionSymbol function,
        IReadOnlyList<BytecodeVariableSlot> capturedSlots,
        int? bytecodeBuiltinBindingId)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedSlots);

        Function = function;
        CapturedScope = null!;
        BytecodeCapturedSlots = capturedSlots;
        BytecodeBuiltinBindingId = bytecodeBuiltinBindingId;
    }

    internal RuntimeFunctionValue(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] capturedSlots,
        int? bytecodeBuiltinBindingId)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(routine.Symbol);
        ArgumentNullException.ThrowIfNull(capturedSlots);

        Function = routine.Symbol;
        CapturedScope = null!;
        BytecodeRoutine = routine;
        BytecodeCapturedSlots = capturedSlots;
        BytecodeBuiltinBindingId = bytecodeBuiltinBindingId;
    }

    public FunctionSymbol Function { get; }

    public RuntimeScope CapturedScope { get; }

    internal BytecodeRoutine? BytecodeRoutine { get; }

    internal IReadOnlyList<BytecodeVariableSlot>? BytecodeCapturedSlots { get; }

    internal int? BytecodeBuiltinBindingId { get; }
}
