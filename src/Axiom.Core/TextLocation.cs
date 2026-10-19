namespace Axiom.Core;

public readonly record struct TextLocation
{
    public TextLocation(TextSpan span, int line, int column, string? document = null)
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
        Document = string.IsNullOrWhiteSpace(document) ? null : document;
    }

    public TextSpan Span { get; }

    public int Line { get; }

    public int Column { get; }

    /// <summary>
    /// Идентичность исходного документа. Для одиночной программы остаётся null;
    /// при нескольких файлах различает одинаковые строки и столбцы разных файлов.
    /// </summary>
    public string? Document { get; }

    public int Length => Span.Length;

    public override string ToString() => Document is null ? $"{Line}:{Column}" : $"{Document}:{Line}:{Column}";
}
