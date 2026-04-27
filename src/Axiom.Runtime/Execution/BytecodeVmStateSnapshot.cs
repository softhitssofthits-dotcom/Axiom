using Axiom.Bytecode;
using Axiom.Core;

namespace Axiom.Runtime;

internal sealed record BytecodeVmStateSnapshot(
    BytecodeVmExecutionSnapshot ExecutionSnapshot,
    IReadOnlyDictionary<int, RuntimeValue> Globals,
    IReadOnlyList<BytecodeCallFrameSnapshot> CallFramesBottomToTop,
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
    int InstructionPointer);

internal sealed record BytecodeVariableSlotSnapshot(RuntimeValue Value, int? FixedStringLength);
