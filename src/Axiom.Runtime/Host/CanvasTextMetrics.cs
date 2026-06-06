namespace Axiom.Runtime;

public static class CanvasTextMetrics
{
    public const int CellWidth = 8;
    public const int CellHeight = 12;

    public static int MeasureWidth(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MeasureColumns(text) * CellWidth;
    }

    public static int MeasureHeight(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return MeasureRows(text) * CellHeight;
    }

    public static int MeasureRows(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var rows = 1;
        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                rows++;
            }
        }

        return rows;
    }

    public static int MeasureColumns(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var max = 0;
        var current = 0;
        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                max = Math.Max(max, current);
                current = 0;
                continue;
            }

            current++;
        }

        return Math.Max(max, current);
    }
}
