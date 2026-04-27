using Axiom.Ide.Models;

namespace Axiom.Ide.Editor.Navigation;

public static class DiagnosticNavigationTargetFactory
{
    public static DiagnosticNavigationTarget? Create(string sourceText, ProgramDiagnosticItem? diagnostic)
    {
        if (diagnostic is null ||
            !diagnostic.Line.HasValue ||
            !diagnostic.Column.HasValue)
        {
            return null;
        }

        var line = diagnostic.Line.Value;
        var column = diagnostic.Column.Value;

        if (line < 1 || column < 1)
        {
            return null;
        }

        sourceText ??= string.Empty;

        var lineStarts = CollectLineStarts(sourceText);

        if (line > lineStarts.Count)
        {
            return null;
        }

        var lineStart = lineStarts[line - 1];
        var lineEnd = FindLineEnd(sourceText, lineStart);
        var zeroBasedColumn = column - 1;

        if (zeroBasedColumn > lineEnd - lineStart)
        {
            return null;
        }

        var offset = lineStart + zeroBasedColumn;
        var maxLength = sourceText.Length - offset;
        var requestedLength = diagnostic.Length ?? 0;
        var safeLength = Math.Clamp(requestedLength, 0, maxLength);

        return new DiagnosticNavigationTarget(offset, safeLength, line, column);
    }

    private static List<int> CollectLineStarts(string sourceText)
    {
        var lineStarts = new List<int> { 0 };
        var index = 0;

        while (index < sourceText.Length)
        {
            if (sourceText[index] == '\r')
            {
                index++;

                if (index < sourceText.Length && sourceText[index] == '\n')
                {
                    index++;
                }

                lineStarts.Add(index);
                continue;
            }

            if (sourceText[index] == '\n')
            {
                index++;
                lineStarts.Add(index);
                continue;
            }

            index++;
        }

        return lineStarts;
    }

    private static int FindLineEnd(string sourceText, int lineStart)
    {
        var index = lineStart;

        while (index < sourceText.Length &&
               sourceText[index] != '\r' &&
               sourceText[index] != '\n')
        {
            index++;
        }

        return index;
    }
}
