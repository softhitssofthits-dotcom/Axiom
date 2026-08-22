using System.Text;

namespace Axiom.Ide.Editor.Formatting;

public sealed class AxiomCodeFormattingService : ICodeFormattingService
{
    public string FormatDocument(string sourceText, CodeFormattingStyle style)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        if (sourceText.Length == 0)
        {
            return sourceText;
        }

        var builder = new StringBuilder(sourceText.Length);
        var lineStart = 0;

        while (lineStart < sourceText.Length)
        {
            var lineEnd = lineStart;
            while (lineEnd < sourceText.Length
                   && sourceText[lineEnd] != '\r'
                   && sourceText[lineEnd] != '\n')
            {
                lineEnd++;
            }

            builder.Append(LineFormatter.Format(sourceText[lineStart..lineEnd], style));

            if (lineEnd >= sourceText.Length)
            {
                break;
            }

            if (sourceText[lineEnd] == '\r'
                && lineEnd + 1 < sourceText.Length
                && sourceText[lineEnd + 1] == '\n')
            {
                builder.Append("\r\n");
                lineStart = lineEnd + 2;
                continue;
            }

            builder.Append(sourceText[lineEnd]);
            lineStart = lineEnd + 1;
        }

        return builder.ToString();
    }

    public string FormatLine(string lineText, CodeFormattingStyle style)
    {
        ArgumentNullException.ThrowIfNull(lineText);

        return LineFormatter.Format(lineText, style);
    }

    public bool IsLineSyntaxValid(string lineText)
    {
        ArgumentNullException.ThrowIfNull(lineText);

        return LineFormatter.IsSyntaxValid(lineText);
    }
}
