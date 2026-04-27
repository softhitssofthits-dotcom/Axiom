using Axiom.Bytecode;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed record BytecodeVmExecutionSnapshot(
    int RoutineId,
    string RoutineName,
    int InstructionIndex,
    Opcode Opcode,
    TextLocation SourceLocation,
    int CallDepth);
