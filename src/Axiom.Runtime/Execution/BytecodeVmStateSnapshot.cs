using Axiom.Bytecode;
using Axiom.Core;

namespace Axiom.Runtime;

internal sealed record BytecodeVmStateSnapshot(
    BytecodeVmExecutionSnapshot ExecutionSnapshot,
    IReadOnlyDictionary<int, RuntimeValue> Globals,
    IReadOnlyList<BytecodeCallFrameSnapshot> CallFramesBottomToTop,
    IReadOnlyList<Diagnostic> Diagnostics,
    Diagnostic? LastRuntimeFailureDiagnostic,
    string? LastRuntimeErrorMessage,
    string PendingOutput,
    bool HasPendingOutput,
    int? OutputCheckpoint,
    BytecodeVmStateSnapshot? PreviousStateSnapshot);

internal sealed record BytecodeCallFrameSnapshot(
    BytecodeRoutine Routine,
    IReadOnlyList<BytecodeVariableSlotSnapshot> Arguments,
    IReadOnlyList<BytecodeVariableSlotSnapshot> Locals,
    IReadOnlyList<BytecodeVariableSlotSnapshot> Captures,
    TextLocation CallSiteLocation,
    IReadOnlyList<RuntimeValue> EvaluationStackBottomToTop,
    IReadOnlyList<BytecodeErrorHandlerFrame> ErrorHandlers,
    int InstructionPointer);

internal sealed record BytecodeVariableSlotSnapshot(BytecodeVariableSlot Slot);
