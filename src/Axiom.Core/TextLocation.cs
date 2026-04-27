namespace Axiom.Core;

public readonly record struct TextLocation
{
    public TextLocation(TextSpan span, int line, int column)
    {
        if (line < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "Line must be greater than or equal to 1.");
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be greater than or equal to 1.");
        }

        Span = span;
        Line = line;
        Column = column;
    }

    public TextSpan Span { get; }

    public int Line { get; }

    public int Column { get; }

    public int Length => Span.Length;

    public override string ToString() => $"{Line}:{Column}";
}
