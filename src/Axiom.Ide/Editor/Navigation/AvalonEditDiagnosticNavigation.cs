using System.Windows;
using ICSharpCode.AvalonEdit;
using Axiom.Ide.Models;

namespace Axiom.Ide.Editor.Navigation;

public static class AvalonEditDiagnosticNavigation
{
    public static readonly DependencyProperty SelectedDiagnosticProperty =
        DependencyProperty.RegisterAttached(
            "SelectedDiagnostic",
            typeof(ProgramDiagnosticItem),
            typeof(AvalonEditDiagnosticNavigation),
            new PropertyMetadata(null, OnSelectedDiagnosticChanged));

    public static ProgramDiagnosticItem? GetSelectedDiagnostic(DependencyObject obj) =>
        (ProgramDiagnosticItem?)obj.GetValue(SelectedDiagnosticProperty);

    public static void SetSelectedDiagnostic(DependencyObject obj, ProgramDiagnosticItem? value) =>
        obj.SetValue(SelectedDiagnosticProperty, value);

    private static void OnSelectedDiagnosticChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not TextEditor editor ||
            editor.Document is null)
        {
            return;
        }

        NavigateToDiagnostic(editor, eventArgs.NewValue as ProgramDiagnosticItem);
    }

    public static bool NavigateToDiagnostic(TextEditor editor, ProgramDiagnosticItem? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (editor.Document is null)
        {
            return false;
        }

        var target = DiagnosticNavigationTargetFactory.Create(editor.Text, diagnostic);

        if (target is null)
        {
            return false;
        }

        Navigate(editor, target);
        return true;
    }

    public static bool NavigateToLine(TextEditor editor, int line, int column)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (editor.Document is null || line <= 0 || column <= 0)
        {
            return false;
        }

        var normalizedLine = Math.Clamp(line, 1, editor.Document.LineCount);
        var documentLine = editor.Document.GetLineByNumber(normalizedLine);
        var normalizedColumn = Math.Clamp(column, 1, documentLine.Length + 1);
        var offset = documentLine.Offset + normalizedColumn - 1;

        Navigate(
            editor,
            new DiagnosticNavigationTarget(
                offset,
                0,
                normalizedLine,
                normalizedColumn));
        return true;
    }

    private static void Navigate(TextEditor editor, DiagnosticNavigationTarget target)
    {
        var maxOffset = editor.Document!.TextLength;
        var offset = Math.Clamp(target.Offset, 0, maxOffset);
        var length = Math.Clamp(target.Length, 0, maxOffset - offset);

        editor.Focus();
        editor.TextArea.Caret.Offset = offset;
        editor.Select(offset, length);
        editor.ScrollTo(target.Line, Math.Max(target.Column, 1));
        editor.TextArea.Caret.BringCaretToView();
    }
}
