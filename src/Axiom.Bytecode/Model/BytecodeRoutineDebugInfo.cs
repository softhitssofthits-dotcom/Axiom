namespace Axiom.Bytecode;

public sealed class BytecodeRoutineDebugInfo
{
    public BytecodeRoutineDebugInfo(
        int routineId,
        IReadOnlyDictionary<int, string> parameterNames,
        IReadOnlyDictionary<int, string> localNames,
        IReadOnlyList<BytecodeBlockDebugRange> blockRanges)
    {
        ArgumentNullException.ThrowIfNull(parameterNames);
        ArgumentNullException.ThrowIfNull(localNames);
        ArgumentNullException.ThrowIfNull(blockRanges);

        RoutineId = routineId;
        ParameterNames = parameterNames;
        LocalNames = localNames;
        BlockRanges = blockRanges;
    }

    public int RoutineId { get; }

    public IReadOnlyDictionary<int, string> ParameterNames { get; }

    public IReadOnlyDictionary<int, string> LocalNames { get; }

    public IReadOnlyList<BytecodeBlockDebugRange> BlockRanges { get; }
}
