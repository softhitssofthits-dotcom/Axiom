using Axiom.Runtime;

namespace Axiom.Ide.Graphics;

public sealed class ScreenTextBuffer
{
    private readonly List<List<ScreenTextCell>> _lines = [];

    public int LineCount => _lines.Count;

    public void Clear()
    {
        _lines.Clear();
    }

    public void Write(int row, int column, string text, int foreground, int? background, int? maxColumns = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        maxColumns = Math.Min(maxColumns ?? GraphicsResourceLimits.MaximumTextColumns, GraphicsResourceLimits.MaximumTextColumns);

        var line = GetOrCreateLine(Math.Max(1, row));
        var columnIndex = Math.Max(0, column - 1);
        if (maxColumns.HasValue && columnIndex >= maxColumns.Value)
        {
            return;
        }

        while (line.Count < columnIndex)
        {
            line.Add(new ScreenTextCell(' ', foreground, background));
        }

        var visibleLength = maxColumns.HasValue
            ? Math.Min(text.Length, maxColumns.Value - columnIndex)
            : text.Length;
        for (var index = 0; index < visibleLength; index++)
        {
            var cell = new ScreenTextCell(text[index], foreground, background);
            var targetIndex = columnIndex + index;
            if (targetIndex < line.Count)
            {
                line[targetIndex] = cell;
            }
            else
            {
                line.Add(cell);
            }
        }
    }

    public void EnsureRow(int row)
    {
        GetOrCreateLine(Math.Max(1, row));
    }

    public void ClearRow(int row)
    {
        if (row < 1)
        {
            return;
        }

        GetOrCreateLine(row).Clear();
    }

    public void ScrollRowsUp(int topRow, int bottomRow)
    {
        if (topRow < 1 || bottomRow < topRow)
        {
            return;
        }

        GetOrCreateLine(bottomRow);
        for (var row = topRow; row < bottomRow; row++)
        {
            _lines[row - 1] = [.. _lines[row]];
        }

        _lines[bottomRow - 1] = [];
    }

    public void TrimTopRows(int maximumRows)
    {
        if (maximumRows <= 0)
        {
            _lines.Clear();
            return;
        }

        var excessRows = _lines.Count - maximumRows;
        if (excessRows > 0)
        {
            _lines.RemoveRange(0, excessRows);
        }
    }

    public IReadOnlyList<IReadOnlyList<ScreenTextCell>> Lines => _lines;

    public string ToPlainText()
    {
        return string.Join(Environment.NewLine, _lines.Select(line => new string(line.Select(cell => cell.Character).ToArray())));
    }

    public string ToPlainText(int firstRow, int maxRows, int? maxColumns = null)
    {
        firstRow = Math.Max(1, firstRow);
        if (maxRows <= 0 || _lines.Count == 0 || firstRow > _lines.Count)
        {
            return string.Empty;
        }

        var rows = new List<string>(Math.Min(maxRows, _lines.Count - firstRow + 1));
        var lastRow = Math.Min(_lines.Count, firstRow + maxRows - 1);
        for (var row = firstRow; row <= lastRow; row++)
        {
            var line = _lines[row - 1];
            var length = maxColumns.HasValue
                ? Math.Min(line.Count, Math.Max(0, maxColumns.Value))
                : line.Count;
            rows.Add(new string(line.Take(length).Select(static cell => cell.Character).ToArray()));
        }

        return string.Join(Environment.NewLine, rows);
    }

    private List<ScreenTextCell> GetOrCreateLine(int row)
    {
        if (row > GraphicsResourceLimits.MaximumTextRows)
            throw new ArgumentOutOfRangeException(nameof(row), "Text row exceeds the graphics history limit.");
        while (_lines.Count < row)
        {
            _lines.Add([]);
        }

        return _lines[row - 1];
    }
}

public readonly record struct ScreenTextCell(char Character, int Foreground, int? Background);
