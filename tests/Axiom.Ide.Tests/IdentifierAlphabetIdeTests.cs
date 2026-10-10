using Axiom.Formatting;
using Axiom.Ide.Editor.Highlighting;
using Axiom.Ide.Services.Persistence;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Axiom.Ide.Tests;

public sealed class IdentifierAlphabetIdeTests
{
    [Theory]
    [InlineData(IdeTheme.Light)]
    [InlineData(IdeTheme.Dark)]
    public void Highlighting_MarksRussianCodeButKeepsStringsAndComments(IdeTheme theme)
    {
        var text = "DIM aя AS INTEGER\nPRINT \"я\" ' я";
        var document = new TextDocument(text);
        using var highlighter = new DocumentHighlighter(document, AxiomHighlighting.GetDefinition(theme));
        Assert.Contains(highlighter.HighlightLine(1).Sections, s => s.Color.Name == "Invalid" && document.GetText(s.Offset,s.Length) == "я");
        Assert.DoesNotContain(highlighter.HighlightLine(2).Sections, s => s.Color.Name == "Invalid");
    }

    [Theory]
    [InlineData(CodeFormattingStyle.Classic)]
    [InlineData(CodeFormattingStyle.Modern)]
    public void Formatting_RejectsRussianNamesAndPreservesTheirSource(CodeFormattingStyle style)
    {
        var service = new AxiomCodeFormattingService();
        const string source = "dim aя as integer";
        Assert.False(service.IsLineSyntaxValid(source));
        Assert.Equal(source, service.FormatLine(source,style));
        Assert.True(service.IsLineSyntaxValid("print \"Русский текст\" ' Комментарий"));
    }
}
