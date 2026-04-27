using Axiom.Ide.Documents;
using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void UseClassicFormatting()
    {
        _traceService.Write("format.style-switch", "style=Classic");
        ApplyFormattingStyle(CodeFormattingStyle.Classic);
    }

    private void UseModernFormatting()
    {
        _traceService.Write("format.style-switch", "style=Modern");
        ApplyFormattingStyle(CodeFormattingStyle.Modern);
    }

    private void ToggleSmartIdentifier()
    {
        IsSmartIdentifierEnabled = !IsSmartIdentifierEnabled;
        _traceService.Write("editor.smart-identifier.toggle", $"enabled={IsSmartIdentifierEnabled}");
    }

    private void ApplyFormattingStyle(CodeFormattingStyle style)
    {
        SelectedFormattingStyle = style;
        ApplyFormattingStyleToActiveDocument();
    }

    private void ApplyFormattingStyleToActiveDocument()
    {
        if (Documents.ActiveDocument is { } document)
        {
            ApplyFormattingStyleToDocument(document);
        }
    }

    private void ApplyFormattingStyleToDocument(DocumentTabViewModel document)
    {
        if (!DocumentFormattingPolicy.CanApplyFormatting(document))
        {
            _traceService.Write(
                "format.document.skip",
                $"{BuildDocumentTrace(document)} | reason=not-formattable");
            return;
        }

        var originalText = document.SourceText;
        var formattedText = _codeFormattingService.FormatDocument(document.SourceText, SelectedFormattingStyle);
        if (IsSmartIdentifierEnabled)
        {
            formattedText = _smartIdentifierService.NormalizeDocument(formattedText);
        }

        if (!string.Equals(document.SourceText, formattedText, StringComparison.Ordinal))
        {
            document.SourceText = formattedText;
            _traceService.Write(
                "format.document.reformatted",
                $"{BuildDocumentTrace(document)} | style={SelectedFormattingStyle} | beforeChars={originalText.Length} | afterChars={formattedText.Length} | preview=\"{BuildSourcePreview(formattedText)}\"");
            return;
        }

        _traceService.Write(
            "format.document.unchanged",
            $"{BuildDocumentTrace(document)} | style={SelectedFormattingStyle} | chars={formattedText.Length}");
    }
}
