using Axiom.Core;

namespace Axiom.Bytecode;

public sealed class SourceInstructionMap
{
    public SourceInstructionMap(int routineId, int instructionIndex, TextLocation location)
    {
        RoutineId = routineId;
        InstructionIndex = instructionIndex;
        Location = location;
    }

    public int RoutineId { get; }

    public int InstructionIndex { get; }

    public TextLocation Location { get; }
}
