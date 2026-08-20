using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Axiom.Ide.Documents;
using Axiom.Ide.Editor;
using Axiom.Ide.Editor.Breakpoints;
using Axiom.Ide.Editor.Formatting;
using Axiom.Ide.Editor.StructuralEditing;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide;

public partial class MainWindow
{
    private readonly Dictionary<TextEditor, int> _lastCaretLineByEditor = [];
    private bool _enterKeyInProgress;

    private void OnEditorLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        try
        {
            AvalonEditTextBinding.EnsureAttached(editor);
            RemoveDecorativeLineMargins(editor);
            EnsureBreakpointMargin(editor);
            EnsureDebugExecutionSpanRenderer(editor);
            ApplyEditorTheme(editor, CurrentTheme);
            RefreshBreakpointMargin(editor);
            SetDiagnosticLineHighlight(editor, false);
            editor.TextArea.Caret.PositionChanged += OnEditorCaretPositionChanged;
            editor.TextArea.SelectionChanged += OnEditorSelectionChanged;
            editor.TextChanged += OnEditorTextChanged;
            editor.PreviewKeyDown += OnEditorPreviewKeyDown;
            editor.PreviewMouseLeftButtonUp += OnEditorPreviewMouseLeftButtonUp;
            _lastCaretLineByEditor[editor] = Math.Max(editor.TextArea.Caret.Line, 1);
            TraceEditorEvent("editor.loaded", editor, $"theme={CurrentTheme}");
            UpdateCaretStatusFromActiveEditor();
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.loaded.exception", editor, exception);
        }
    }

    private void OnEditorUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        try
        {
            editor.TextArea.Caret.PositionChanged -= OnEditorCaretPositionChanged;
            editor.TextArea.SelectionChanged -= OnEditorSelectionChanged;
            editor.TextChanged -= OnEditorTextChanged;
            editor.PreviewKeyDown -= OnEditorPreviewKeyDown;
            editor.PreviewMouseLeftButtonUp -= OnEditorPreviewMouseLeftButtonUp;
            RemoveDebugExecutionSpanRenderer(editor);
            RemoveBreakpointMargin(editor);
            _lastCaretLineByEditor.Remove(editor);
            _diagnosticHighlightedEditors.Remove(editor);
            _stoppedRunHighlightedEditors.Remove(editor);
            _viewModel?.FlushEditorActivityEvents();
            TraceEditorEvent("editor.unloaded", editor);
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.unloaded.exception", editor, exception);
        }
    }

    private void OnEditorGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        try
        {
            _lastCaretLineByEditor[editor] = Math.Max(editor.TextArea.Caret.Line, 1);
            UpdateCurrentLineHighlightVisibility(editor);
            TraceEditorEvent("editor.focus.gained", editor);
            UpdateCaretStatusFromActiveEditor();
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.focus.gained.exception", editor, exception);
        }
    }

    private void OnEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        try
        {
            UpdateCurrentLineHighlightVisibility(editor);

            if (!ShouldAutoFormatEditor(editor))
            {
                TraceEditorEvent("editor.focus.lost", editor, "autoFormat=false");
                _viewModel?.FlushEditorActivityEvents();
                return;
            }

            TraceEditorEvent("editor.focus.lost", editor, "autoFormat=true");
            TryFormatEditorLine(editor, Math.Max(editor.TextArea.Caret.Line, 1));
            _lastCaretLineByEditor[editor] = Math.Max(editor.TextArea.Caret.Line, 1);
            _viewModel?.FlushEditorActivityEvents();
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.focus.lost.exception", editor, exception);
        }
    }

    private void OnEditorPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        if (e.Key == Key.F9)
        {
            ToggleSourceBreakpointAtEditorCaret(editor);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Return || !ShouldAutoFormatEditor(editor))
        {
            return;
        }

        var previousLine = Math.Max(editor.TextArea.Caret.Line, 1);
        TraceEditorEvent("editor.enter.pressed", editor, $"line={previousLine}");

        _enterKeyInProgress = true;
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                TryApplyBlockEditingAfterEnter(editor, previousLine);
            }
            catch (Exception exception)
            {
                HandleEditorInteractionException("editor.enter.block-edit.exception", editor, exception);
            }
            finally
            {
                _enterKeyInProgress = false;
            }
        });
    }

    private void OnEditorPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextEditor editor || !ShouldAutoFormatEditor(editor))
        {
            return;
        }

        try
        {
            var currentLine = Math.Max(editor.TextArea.Caret.Line, 1);
            var previousLine = _lastCaretLineByEditor.TryGetValue(editor, out var trackedLine)
                ? trackedLine
                : currentLine;

            if (previousLine != currentLine)
            {
                TraceEditorEvent("editor.mouse.line-transition", editor, $"fromLine={previousLine} | toLine={currentLine}");
                TryFormatEditorLine(editor, previousLine);
                currentLine = Math.Max(editor.TextArea.Caret.Line, 1);
            }

            _lastCaretLineByEditor[editor] = currentLine;
            UpdateCaretStatusFromActiveEditor();
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.mouse.line-transition.exception", editor, exception);
        }
    }

    private void OnEditorCaretPositionChanged(object? sender, EventArgs e)
    {
        var activeEditor = GetActiveEditor();
        var editor = sender as TextEditor ?? activeEditor;

        if (editor is null)
        {
            return;
        }

        try
        {
            if (_isApplyingLineFormatting)
            {
                UpdateCaretStatusFromActiveEditor();
                return;
            }

            if (_enterKeyInProgress)
            {
                var enterCurrentLine = Math.Max(editor.TextArea.Caret.Line, 1);
                _lastCaretLineByEditor[editor] = enterCurrentLine;
                UpdateCaretStatusFromActiveEditor();
                return;
            }

            if (ReferenceEquals(editor, activeEditor) && ShouldAutoFormatEditor(editor))
            {
                var currentLine = Math.Max(editor.TextArea.Caret.Line, 1);
                var previousLine = _lastCaretLineByEditor.TryGetValue(editor, out var trackedLine)
                    ? trackedLine
                    : currentLine;

                if (previousLine != currentLine)
                {
                    TraceEditorEvent("editor.caret.line-transition", editor, $"fromLine={previousLine} | toLine={currentLine}");
                    TryFormatEditorLine(editor, previousLine);
                    currentLine = Math.Max(editor.TextArea.Caret.Line, 1);
                }

                _lastCaretLineByEditor[editor] = currentLine;
            }

            UpdateCaretStatusFromActiveEditor();
            if (ReferenceEquals(editor, activeEditor) &&
                DataContext is MainWindowViewModel viewModel &&
                editor.DataContext is DocumentTabViewModel document)
            {
                viewModel.ReportEditorCaretActivity(
                    document,
                    Math.Max(editor.TextArea.Caret.Line, 1),
                    Math.Max(editor.TextArea.Caret.Column, 1));
            }
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.caret.line-transition.exception", editor, exception);
        }
    }

    private void OnEditorSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is not TextEditor editor ||
            !ReferenceEquals(editor, GetActiveEditor()) ||
            DataContext is not MainWindowViewModel viewModel ||
            editor.DataContext is not DocumentTabViewModel document)
        {
            return;
        }

        try
        {
            viewModel.ReportEditorSelectionActivity(
                document,
                Math.Max(editor.SelectionStart, 0),
                Math.Max(editor.SelectionLength, 0));
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.selection.changed.exception", editor, exception);
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isApplyingLineFormatting ||
            sender is not TextEditor editor ||
            !ReferenceEquals(editor, GetActiveEditor()) ||
            DataContext is not MainWindowViewModel viewModel ||
            editor.DataContext is not DocumentTabViewModel document)
        {
            return;
        }

        try
        {
            viewModel.ReportEditorTextActivity(
                document,
                editor.Document?.TextLength ?? 0);

            if (!viewModel.StatusBar.IsRunning)
            {
                viewModel.BottomPane.ClearDiagnostics();
            }
        }
        catch (Exception exception)
        {
            HandleEditorInteractionException("editor.text.changed.exception", editor, exception);
        }
    }

    private void OnDocumentTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, DocumentTabs))
        {
            return;
        }

        _viewModel?.FlushEditorActivityEvents();
        Dispatcher.BeginInvoke(UpdateCaretStatusFromActiveEditor);
    }

    private void OnStatusBarPathTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 ||
            sender is not TextBlock textBlock)
        {
            return;
        }

        CopyTextToClipboard(textBlock.Text);
        e.Handled = true;
    }

    private void OnStatusBarPathCopyMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Parent: ContextMenu { PlacementTarget: TextBlock textBlock } })
        {
            return;
        }

        CopyTextToClipboard(textBlock.Text);
        e.Handled = true;
    }

    private static void CopyTextToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // Clipboard can fail in some environments
        }
    }

    private void UpdateCaretStatusFromActiveEditor()
    {
        var editor = GetActiveEditor();

        if (editor is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.StatusBar.UpdateLineColumn(
            Math.Max(editor.TextArea.Caret.Line, 1),
            Math.Max(editor.TextArea.Caret.Column, 1));
    }

    private bool ShouldAutoFormatEditor(TextEditor editor)
    {
        return !_isApplyingLineFormatting
            && DataContext is MainWindowViewModel viewModel
            && !editor.IsReadOnly
            && DocumentFormattingPolicy.CanApplyFormatting(viewModel.Documents.ActiveDocument)
            && editor.TextArea.Selection.IsEmpty
            && Mouse.LeftButton != MouseButtonState.Pressed;
    }

    private void TryApplyBlockEditingAfterEnter(TextEditor editor, int previousLineNumber)
    {
        if (!ReferenceEquals(editor, GetActiveEditor()) || !ShouldAutoFormatEditor(editor))
        {
            return;
        }

        var currentLineNumber = Math.Max(editor.TextArea.Caret.Line, 1);
        if (currentLineNumber == previousLineNumber)
        {
            return;
        }

        TraceEditorEvent("editor.enter.line-transition", editor, $"fromLine={previousLineNumber} | toLine={currentLineNumber}");
        TryApplyBlockEditing(editor, previousLineNumber, currentLineNumber);
    }

    private void TryApplyBlockEditing(TextEditor editor, int previousLineNumber, int currentLineNumber)
    {
        if (editor.Document is null)
        {
            return;
        }

        var style = DataContext is MainWindowViewModel vm2 ? vm2.SelectedFormattingStyle : CodeFormattingStyle.Classic;
        var caretOffset = editor.TextArea.Caret.Offset;

        var changed = _blockEditingService.TryHandleLineTransition(
            editor.Document,
            previousLineNumber,
            currentLineNumber,
            caretOffset,
            style,
            out var newCaretOffset);

        if (!changed)
        {
            return;
        }

        _isApplyingLineFormatting = true;
        try
        {
            editor.TextArea.Caret.Offset = newCaretOffset;
        }
        finally
        {
            _isApplyingLineFormatting = false;
        }
    }

    private void TryFormatEditorLine(TextEditor editor, int lineNumber)
    {
        if (editor.Document is null || lineNumber < 1 || lineNumber > editor.Document.LineCount)
        {
            return;
        }

        var line = editor.Document.GetLineByNumber(lineNumber);
        var lineText = editor.Document.GetText(line);

        if (string.IsNullOrWhiteSpace(lineText))
        {
            return;
        }

        if (!_codeFormattingService.IsLineSyntaxValid(lineText))
        {
            return;
        }

        var style = DataContext is MainWindowViewModel vm ? vm.SelectedFormattingStyle : CodeFormattingStyle.Classic;
        var formattedText = _codeFormattingService.FormatLine(lineText, style);

        if (formattedText == lineText)
        {
            return;
        }

        _isApplyingLineFormatting = true;
        try
        {
            var caretOffset = editor.TextArea.Caret.Offset;
            var lineStartOffset = line.Offset;
            var relativeCaret = caretOffset - lineStartOffset;

            editor.Document.Replace(line, formattedText);

            var newLine = editor.Document.GetLineByNumber(lineNumber);
            var newCaretOffset = newLine.Offset + Math.Min(relativeCaret, newLine.Length);
            editor.TextArea.Caret.Offset = newCaretOffset;
        }
        finally
        {
            _isApplyingLineFormatting = false;
        }
    }

    private IIdeTraceService TraceService =>
        _viewModel?.TraceService ?? NullIdeTraceService.Instance;

    private void TraceEditorEvent(string category, TextEditor editor, string? message = null)
    {
        var details = BuildEditorTraceDetails(editor);

        if (!string.IsNullOrWhiteSpace(message))
        {
            details = $"{details} | {message}";
        }

        TraceService.Write(category, details);
    }

    private void HandleEditorInteractionException(string category, TextEditor editor, Exception exception)
    {
        TraceService.WriteException(category, exception, BuildEditorTraceDetails(editor));

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusBar.StatusText = $"Ошибка редактора. Подробности в логе: {TraceService.CurrentSessionLogPath}";
        }
    }

    private static string BuildEditorTraceDetails(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var document = editor.DataContext as DocumentTabViewModel;
        return $"document={document?.DisplayName ?? "<unknown>"} | path={document?.FilePath ?? "<untitled>"} | caret={editor.CaretOffset} | line={Math.Max(editor.TextArea.Caret.Line, 1)} | column={Math.Max(editor.TextArea.Caret.Column, 1)} | textLength={editor.Document?.TextLength ?? 0}";
    }

    private static string BuildTracePreview(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= 160
            ? normalized
            : normalized[..160];
    }
}
