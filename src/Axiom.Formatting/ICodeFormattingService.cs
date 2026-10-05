namespace Axiom.Formatting;

public interface ICodeFormattingService
{
    string FormatDocument(string sourceText, CodeFormattingStyle style);
    string FormatLine(string lineText, CodeFormattingStyle style);
    bool IsLineSyntaxValid(string lineText);
}
