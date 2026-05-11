namespace Axiom.Ide.Graphics;

public sealed class ScreenTextBuffer
{
    private readonly List<List<ScreenTextCell>> _lines = [];

    public void Clear()
    {
        _lines.Clear();
    }

    public void Write(int row, int column, string text, int foreground, int? background)
    {
        ArgumentNullException.ThrowIfNull(text);

        var line = GetOrCreateLine(Math.Max(1, row));
        var columnIndex = Math.Max(0, column - 1);
        while (line.Count < columnIndex)
        {
            line.Add(new ScreenTextCell(' ', foreground, background));
        }

        for (var index = 0; index < text.Length; index++)
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

    public IReadOnlyList<IReadOnlyList<ScreenTextCell>> Lines => _lines;

    public string ToPlainText()
    {
        return string.Join(Environment.NewLine, _lines.Select(line => new string(line.Select(cell => cell.Character).ToArray())));
    }

    private List<ScreenTextCell> GetOrCreateLine(int row)
    {
        while (_lines.Count < row)
        {
            _lines.Add([]);
        }

        return _lines[row - 1];
    }
}

public readonly record struct ScreenTextCell(char Character, int Foreground, int? Background);
