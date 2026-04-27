namespace Axiom.Runtime;

public readonly record struct BytecodeSourceBreakpoint
{
    public BytecodeSourceBreakpoint(int line, int? column = null)
    {
        if (line < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "Line must be greater than or equal to 1.");
        }

        if (column is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be greater than or equal to 1.");
        }

        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int? Column { get; }

    public bool Matches(BytecodeVmExecutionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.SourceLocation.Line == Line
            && (Column is null || snapshot.SourceLocation.Column == Column.Value);
    }
}
