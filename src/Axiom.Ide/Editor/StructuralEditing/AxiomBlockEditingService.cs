using ICSharpCode.AvalonEdit.Document;
using Axiom.Formatting;

namespace Axiom.Ide.Editor.StructuralEditing;

public sealed class AxiomBlockEditingService(ICodeFormattingService formattingService)
{
    private readonly BlockEditPlanner _planner = new(formattingService);
    public bool TryHandleLineTransition(TextDocument document, int previousLineNumber, int currentLineNumber,
        int caretOffset, CodeFormattingStyle style, out int newCaretOffset)
    {
        ArgumentNullException.ThrowIfNull(document);
        var plan = _planner.Plan(document.Text, previousLineNumber, currentLineNumber, caretOffset, style);
        newCaretOffset = plan.NewCaretOffset;
        if (!plan.HasChanges) return false;
        document.Replace(plan.Start, plan.Length, plan.NewText);
        return true;
    }
}
