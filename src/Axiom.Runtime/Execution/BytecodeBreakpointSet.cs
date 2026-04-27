namespace Axiom.Runtime;

public sealed class BytecodeBreakpointSet
{
    private readonly HashSet<int> _lines;
    private readonly HashSet<SourcePosition> _positions;

    public BytecodeBreakpointSet(IEnumerable<BytecodeSourceBreakpoint> breakpoints)
    {
        ArgumentNullException.ThrowIfNull(breakpoints);

        _lines = [];
        _positions = [];

        foreach (var breakpoint in breakpoints)
        {
            if (breakpoint.Column is { } column)
            {
                _positions.Add(new SourcePosition(breakpoint.Line, column));
            }
            else
            {
                _lines.Add(breakpoint.Line);
            }
        }
    }

    public bool IsEmpty => _lines.Count == 0 && _positions.Count == 0;

    public static BytecodeBreakpointSet FromLines(params int[] lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        return new BytecodeBreakpointSet(lines.Select(static line => new BytecodeSourceBreakpoint(line)));
    }

    public static BytecodeBreakpointSet FromPositions(params (int Line, int Column)[] positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        return new BytecodeBreakpointSet(positions.Select(static position =>
            new BytecodeSourceBreakpoint(position.Line, position.Column)));
    }

    public bool ShouldStopBefore(BytecodeVmExecutionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return ShouldStopBeforeLineBreakpoint(snapshot)
            || ShouldStopBeforePositionBreakpoint(snapshot);
    }

    public bool ShouldStopBeforeLineBreakpoint(BytecodeVmExecutionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return _lines.Contains(snapshot.SourceLocation.Line);
    }

    public bool ShouldStopBeforePositionBreakpoint(BytecodeVmExecutionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var location = snapshot.SourceLocation;
        return _positions.Contains(new SourcePosition(location.Line, location.Column));
    }

    private readonly record struct SourcePosition(int Line, int Column);
}
