using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using AxiomTextLocation = Axiom.Core.TextLocation;
using Axiom.Ide.Editor.Breakpoints;
using Axiom.Ide.Editor.Navigation;
using Axiom.Ide.Models;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide;

public partial class MainWindow
{
    private void OnBottomPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BottomPaneViewModel.FocusRequestVersion))
        {
            Dispatcher.BeginInvoke(FocusRequestedBottomPaneTarget);
            return;
        }

        if (e.PropertyName == nameof(BottomPaneViewModel.SelectedDiagnostic) &&
            _bottomPane?.SelectedDiagnostic is null)
        {
            Dispatcher.BeginInvoke(ClearActiveEditorDiagnosticHighlight);
        }
    }

    private void OnBottomPaneSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, BottomPaneTabs))
        {
            return;
        }

        Dispatcher.BeginInvoke(FocusSelectedBottomPaneContent);
    }

    private void OnDiagnosticsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, DiagnosticsDataGrid) ||
            DiagnosticsDataGrid.SelectedItem is not ProgramDiagnosticItem diagnostic)
        {
            return;
        }

        var editor = GetActiveEditor();

        if (editor is null)
        {
            return;
        }

        if (!AvalonEditDiagnosticNavigation.NavigateToDiagnostic(editor, diagnostic))
        {
            return;
        }

        SetDiagnosticLineHighlight(editor, true);
        UpdateCaretStatusFromActiveEditor();
    }

    private void NavigateToStoppedRunLine()
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            viewModel.StoppedRunLine is not { } line)
        {
            ClearStoppedRunLineHighlights();
            return;
        }

        var editor = GetActiveEditor();

        if (editor is null)
        {
            return;
        }

        ClearStoppedRunLineHighlights();

        if (!AvalonEditDiagnosticNavigation.NavigateToLine(editor, line, column: 1))
        {
            return;
        }

        SetStoppedRunLineHighlight(editor, true);
        var stopInfo = viewModel.StoppedRunState?.StopInfo;
        if (stopInfo?.SourceLocation is { } sourceLocation && sourceLocation.Span.Length > 0)
        {
            SetStoppedRunExecutionSpan(editor, sourceLocation);
        }
        TraceEditorEvent("editor.breakpoint.stopped-line", editor, $"line={line} | stopInfo=({stopInfo?.Line}, {stopInfo?.Column})");
        UpdateCaretStatusFromActiveEditor();
    }

    private void OnDiagnosticsDataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(sender, DiagnosticsDataGrid) ||
            DiagnosticsDataGrid.SelectedItem is not ProgramDiagnosticItem diagnostic)
        {
            return;
        }

        CopyTextToClipboard(BuildDiagnosticClipboardText(diagnostic));
    }

    private void OnDiagnosticsCopyMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (DiagnosticsDataGrid?.SelectedItem is not ProgramDiagnosticItem diagnostic)
        {
            return;
        }

        CopyTextToClipboard(BuildDiagnosticClipboardText(diagnostic));
    }

    private void OnToggleCurrentBreakpointMenuItemClick(object sender, RoutedEventArgs e)
    {
        ToggleSourceBreakpointAtEditorCaret(GetActiveEditor());
        e.Handled = true;
    }

    private void OnClearAllBreakpointsMenuItemClick(object sender, RoutedEventArgs e)
    {
        var editor = GetActiveEditor();
        if (DataContext is MainWindowViewModel viewModel
            && viewModel.ClearAllSourceBreakpointsCommand.CanExecute(null))
        {
            viewModel.ClearAllSourceBreakpointsCommand.Execute(null);
            if (editor is not null)
            {
                TraceEditorEvent("editor.breakpoint.clear-all", editor, "source=context-menu");
            }
        }

        e.Handled = true;
    }

    private void ToggleSourceBreakpointAtEditorCaret(TextEditor? editor)
    {
        if (editor is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var line = Math.Max(editor.TextArea.Caret.Line, 1);
        if (!viewModel.ToggleCurrentSourceBreakpointCommand.CanExecute(line))
        {
            return;
        }

        viewModel.ToggleCurrentSourceBreakpointCommand.Execute(line);
        TraceEditorEvent("editor.breakpoint.toggle", editor, $"line={line}");
    }

    private void ClearActiveEditorDiagnosticHighlight()
    {
        var editor = GetActiveEditor();

        if (editor is null)
        {
            return;
        }

        SetDiagnosticLineHighlight(editor, false);
    }

    private void SetDiagnosticLineHighlight(TextEditor editor, bool enabled)
    {
        if (enabled)
        {
            _diagnosticHighlightedEditors.Add(editor);
        }
        else
        {
            _diagnosticHighlightedEditors.Remove(editor);
        }

        ApplyCurrentLineHighlightStyle(editor, enabled);
    }

    private void SetStoppedRunLineHighlight(TextEditor editor, bool enabled)
    {
        if (enabled)
        {
            _stoppedRunHighlightedEditors.Add(editor);
        }
        else
        {
            _stoppedRunHighlightedEditors.Remove(editor);
        }

        ApplyCurrentLineHighlightStyle(editor, _diagnosticHighlightedEditors.Contains(editor));
        if (!enabled)
        {
            SetStoppedRunExecutionSpan(editor, null);
        }
    }

    private void ClearStoppedRunLineHighlights()
    {
        foreach (var editor in _stoppedRunHighlightedEditors.ToArray())
        {
            SetStoppedRunLineHighlight(editor, false);
        }
    }

    private void EnsureDebugExecutionSpanRenderer(TextEditor editor)
    {
        if (_debugExecutionSpanRenderersByEditor.ContainsKey(editor))
        {
            return;
        }

        var renderer = new DebugExecutionSpanRenderer(this, editor);
        _debugExecutionSpanRenderersByEditor[editor] = renderer;
        editor.TextArea.TextView.BackgroundRenderers.Add(renderer);
    }

    private void RemoveDebugExecutionSpanRenderer(TextEditor editor)
    {
        if (!_debugExecutionSpanRenderersByEditor.Remove(editor, out var renderer))
        {
            return;
        }

        editor.TextArea.TextView.BackgroundRenderers.Remove(renderer);
        renderer.SetLocation(null);
    }

    private void SetStoppedRunExecutionSpan(TextEditor editor, AxiomTextLocation? location)
    {
        EnsureDebugExecutionSpanRenderer(editor);
        _debugExecutionSpanRenderersByEditor[editor].SetLocation(location);
    }
}
