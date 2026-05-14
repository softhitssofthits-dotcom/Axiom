namespace Axiom.Runtime;

public sealed class GraphicsPaintRequest
{
    private GraphicsPaintRequest(
        long x,
        long y,
        int borderColor,
        int? solidFillColor,
        byte[]? fillPatternRows,
        byte[]? backgroundPatternRows,
        int patternForegroundColor,
        int patternBackgroundColor)
    {
        X = x;
        Y = y;
        BorderColor = borderColor;
        SolidFillColor = solidFillColor;
        FillPatternRows = fillPatternRows;
        BackgroundPatternRows = backgroundPatternRows;
        PatternForegroundColor = patternForegroundColor;
        PatternBackgroundColor = patternBackgroundColor;
    }

    public long X { get; }

    public long Y { get; }

    public int BorderColor { get; }

    public int? SolidFillColor { get; }

    public byte[]? FillPatternRows { get; }

    public byte[]? BackgroundPatternRows { get; }

    public int PatternForegroundColor { get; }

    public int PatternBackgroundColor { get; }

    public bool UsesPattern => FillPatternRows is not null;

    public static GraphicsPaintRequest CreateSolid(long x, long y, int fillColor, int borderColor) =>
        new(x, y, borderColor, fillColor, fillPatternRows: null, backgroundPatternRows: null, patternForegroundColor: 0, patternBackgroundColor: 0);

    public static GraphicsPaintRequest CreatePattern(
        long x,
        long y,
        byte[] fillPatternRows,
        int borderColor,
        int patternForegroundColor,
        int patternBackgroundColor,
        byte[]? backgroundPatternRows = null)
    {
        ArgumentNullException.ThrowIfNull(fillPatternRows);

        return new(
            x,
            y,
            borderColor,
            solidFillColor: null,
            fillPatternRows: (byte[])fillPatternRows.Clone(),
            backgroundPatternRows: backgroundPatternRows is null ? null : (byte[])backgroundPatternRows.Clone(),
            patternForegroundColor,
            patternBackgroundColor);
    }
}
