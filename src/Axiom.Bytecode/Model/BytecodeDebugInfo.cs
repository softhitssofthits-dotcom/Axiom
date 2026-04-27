using Axiom.Core;

namespace Axiom.Bytecode;

public sealed class BytecodeDebugInfo
{
    public BytecodeDebugInfo(
        IReadOnlyList<SourceInstructionMap> instructionMap,
        IReadOnlyList<BytecodeRoutineDebugInfo> routines)
    {
        ArgumentNullException.ThrowIfNull(instructionMap);
        ArgumentNullException.ThrowIfNull(routines);

        InstructionMap = instructionMap;
        Routines = routines;
    }

    public IReadOnlyList<SourceInstructionMap> InstructionMap { get; }

    public IReadOnlyList<BytecodeRoutineDebugInfo> Routines { get; }

    public bool TryGetLocation(int routineId, int instructionIndex, out TextLocation location)
    {
        if (routineId < 0 || instructionIndex < 0)
        {
            location = default;
            return false;
        }

        SourceInstructionMap? bestMatch = null;

        foreach (var map in InstructionMap)
        {
            if (map.RoutineId != routineId || map.InstructionIndex > instructionIndex)
            {
                continue;
            }

            if (bestMatch is null || map.InstructionIndex > bestMatch.InstructionIndex)
            {
                bestMatch = map;
            }
        }

        if (bestMatch is null)
        {
            location = default;
            return false;
        }

        location = bestMatch.Location;
        return true;
    }
}
