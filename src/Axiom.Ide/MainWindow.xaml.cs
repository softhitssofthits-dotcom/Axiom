using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using AxiomTextLocation = Axiom.Core.TextLocation;
using Axiom.Ide.Documents;
using Axiom.Ide.Editor.Breakpoints;
using Axiom.Ide.Editor.Formatting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Editor;
using Axiom.Ide.Editor.Highlighting;
using Axiom.Ide.Editor.Navigation;
using Axiom.Ide.Editor.StructuralEditing;
using Axiom.Ide.Files;
using Axiom.Ide.Models;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide;

public partial class MainWindow : Window
{
    private const int WmAppSmokeHarnessShowFindReplace = 0x8001;
    private const int WmAppSmokeHarnessCloseTab = 0x8002;
    private const int WmAppSmokeHarnessRun = 0x8003;
    private const int WmAppSmokeHarnessToggleFilesPane = 0x8004;
    private const int WmAppSmokeHarnessToggleOutputPane = 0x8005;
    private const int WmAppSmokeHarnessToggleDiagnosticsPane = 0x8006;
    private const int WmAppSmokeHarnessToggleStatusBar = 0x8007;

    private ProgramConsoleService? _consoleService;
    private BottomPaneViewModel? _bottomPane;
    private MainWindowViewModel? _viewModel;
    private FindReplaceDialog? _findReplaceDialog;
    private FindReplaceOptions _findReplaceOptions = FindReplaceOptions.Default;
    private bool _hasFindReplaceStatusMessage;
    private readonly Dictionary<TextEditor, int> _lastCaretLineByEditor = [];
    private readonly Dictionary<TextEditor, BreakpointMargin> _breakpointMarginsByEditor = [];
    private readonly Dictionary<TextEditor, DebugExecutionSpanRenderer> _debugExecutionSpanRenderersByEditor = [];
    private readonly HashSet<TextEditor> _diagnosticHighlightedEditors = [];
    private readonly HashSet<TextEditor> _stoppedRunHighlightedEditors = [];
    private bool _preserveOutputSelectionOnRightClick;
    private int _outputSelectionStart;
    private int _outputSelectionLength;
    private readonly ICodeFormattingService _codeFormattingService;
    private readonly AxiomSmartIdentifierService _smartIdentifierService;
    private readonly AxiomBlockEditingService _blockEditingService;
    private bool _isApplyingLineFormatting;
    private bool _suppressFilesTreeNodeActivation;

    public MainWindow(
        ICodeFormattingService codeFormattingService,
        AxiomSmartIdentifierService smartIdentifierService,
        AxiomBlockEditingService blockEditingService)
    {
        ArgumentNullException.ThrowIfNull(codeFormattingService);
        ArgumentNullException.ThrowIfNull(smartIdentifierService);
        ArgumentNullException.ThrowIfNull(blockEditingService);

        _codeFormattingService = codeFormattingService;
        _smartIdentifierService = smartIdentifierService;
        _blockEditingService = blockEditingService;
        InitializeComponent();
        InitializeShellInputBindings();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        PersistShellWindowState();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachConsoleService();
        DetachBottomPane();
        DetachViewModel();
        CloseFindReplaceDialog();
        base.OnClosed(e);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachConsoleService();
        DetachBottomPane();
        DetachViewModel();

        if (e.NewValue is not MainWindowViewModel viewModel)
        {
            return;
        }

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _consoleService = viewModel.InputConsole;
        _consoleService.PropertyChanged += OnConsoleServicePropertyChanged;
        _bottomPane = viewModel.BottomPane;
        _bottomPane.PropertyChanged += OnBottomPanePropertyChanged;
        ApplyLanguage(viewModel.SelectedLanguage);
        ApplyTheme(viewModel.SelectedTheme);
        ApplyPreferredWindowState(viewModel.PreferredWindowState);
        UpdateCaretStatusFromActiveEditor();
    }

    private void DetachViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedTheme) &&
            sender is MainWindowViewModel viewModel)
        {
            ApplyTheme(viewModel.SelectedTheme);
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.SourceBreakpointLines))
        {
            RefreshBreakpointMargins();
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.StoppedRunLine))
        {
            Dispatcher.BeginInvoke(NavigateToStoppedRunLine);
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.SelectedLanguage) &&
            sender is MainWindowViewModel languageViewModel)
        {
            ApplyLanguage(languageViewModel.SelectedLanguage);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (HwndSource.FromHwnd(handle) is HwndSource source)
        {
            source.AddHook(WndProc);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!TryHandleSmokeHarnessMessage(msg))
        {
            return IntPtr.Zero;
        }

        handled = true;
        return IntPtr.Zero;
    }

    private bool TryHandleSmokeHarnessMessage(int msg)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }

        switch (msg)
        {
            case WmAppSmokeHarnessShowFindReplace:
                ShowFindReplaceDialog(selectSearchPattern: true);
                return true;

            case WmAppSmokeHarnessCloseTab:
                if (viewModel.CloseTabCommand.CanExecute(null))
                {
                    viewModel.CloseTabCommand.Execute(null);
                }

                return true;

            case WmAppSmokeHarnessRun:
                if (viewModel.RunCommand.CanExecute(null))
                {
                    viewModel.RunCommand.Execute(null);
                }

                return true;

            case WmAppSmokeHarnessToggleFilesPane:
                if (viewModel.FilesPane.IsVisible)
                {
                    if (viewModel.CloseFilesPaneCommand.CanExecute(null))
                    {
                        viewModel.CloseFilesPaneCommand.Execute(null);
                    }
                }
                else
                {
                    viewModel.FilesPane.IsVisible = true;
                }

                return true;

            case WmAppSmokeHarnessToggleOutputPane:
                if (viewModel.BottomPane.IsOutputVisible)
                {
                    if (viewModel.CloseOutputPaneCommand.CanExecute(null))
                    {
                        viewModel.CloseOutputPaneCommand.Execute(null);
                    }
                }
                else if (viewModel.ShowOutputCommand.CanExecute(null))
                {
                    viewModel.ShowOutputCommand.Execute(null);
                }

                return true;

            case WmAppSmokeHarnessToggleDiagnosticsPane:
                if (viewModel.BottomPane.IsDiagnosticsVisible)
                {
                    if (viewModel.CloseDiagnosticsPaneCommand.CanExecute(null))
                    {
                        viewModel.CloseDiagnosticsPaneCommand.Execute(null);
                    }
                }
                else if (viewModel.ShowDiagnosticsCommand.CanExecute(null))
                {
                    viewModel.ShowDiagnosticsCommand.Execute(null);
                }

                return true;

            case WmAppSmokeHarnessToggleStatusBar:
                viewModel.StatusBar.IsVisible = !viewModel.StatusBar.IsVisible;
                return true;

            default:
                return false;
        }
    }

    private void OnConsoleServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgramConsoleService.IsWaitingForInput) && _consoleService?.IsWaitingForInput == true)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ConsoleInputTextBox.Focus();
                ConsoleInputTextBox.SelectAll();
            });
        }
    }

    private void DetachConsoleService()
    {
        if (_consoleService is null)
        {
            return;
        }

        _consoleService.PropertyChanged -= OnConsoleServicePropertyChanged;
        _consoleService = null;
    }

    private void DetachBottomPane()
    {
        if (_bottomPane is null)
        {
            return;
        }

        _bottomPane.PropertyChanged -= OnBottomPanePropertyChanged;
        _bottomPane = null;
    }

    private void OnFindCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = GetActiveEditor() is not null;
        e.Handled = true;
    }

    private void OnFindExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ShowFindReplaceDialog(selectSearchPattern: true);
        e.Handled = true;
    }

    private void OnFindNextExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ExecuteFind(searchForward: true);
        e.Handled = true;
    }

    private void OnFindPreviousExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ExecuteFind(searchForward: false);
        e.Handled = true;
    }

    private void InitializeShellInputBindings()
    {
        InputBindings.Add(CreateShellKeyBinding("DataContext.NewCommand", Key.N, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.OpenCommand", Key.O, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.SaveCommand", Key.S, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.SaveAsCommand", Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(CreateShellKeyBinding("DataContext.CloseTabCommand", Key.W, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.RunCommand", Key.F5, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.RunOrContinueCommand", Key.F5, ModifierKeys.None));
        InputBindings.Add(CreateShellKeyBinding("DataContext.StepBackCommand", Key.F10, ModifierKeys.Control));
        InputBindings.Add(CreateShellKeyBinding("DataContext.StepOverCommand", Key.F10, ModifierKeys.None));
        InputBindings.Add(CreateShellKeyBinding("DataContext.StepIntoCommand", Key.F11, ModifierKeys.None));
        InputBindings.Add(CreateShellKeyBinding("DataContext.StepOutCommand", Key.F11, ModifierKeys.Shift));
        InputBindings.Add(CreateShellKeyBinding("DataContext.StopDebuggingCommand", Key.F5, ModifierKeys.Shift));
        InputBindings.Add(CreateShellKeyBinding("DataContext.RestartDebuggingCommand", Key.F5, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(CreateShellKeyBinding("DataContext.ClearAllSourceBreakpointsCommand", Key.F9, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Find, Key.F, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(SearchCommands.FindNext, Key.F3, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(SearchCommands.FindPrevious, Key.F3, ModifierKeys.Shift));
    }

    private KeyBinding CreateShellKeyBinding(string path, Key key, ModifierKeys modifiers)
    {
        var binding = new KeyBinding
        {
            Key = key,
            Modifiers = modifiers
        };

        BindingOperations.SetBinding(
            binding,
            InputBinding.CommandProperty,
            new Binding(path)
            {
                Source = this
            });

        return binding;
    }

    private TextEditor? GetActiveEditor()
    {
        if (DocumentTabs is null)
        {
            return null;
        }

        if (DocumentTabs.SelectedContent is DependencyObject root)
        {
            return FindDescendant<TextEditor>(root);
        }

        return FindDescendant<TextEditor>(DocumentTabs);
    }

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
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Clipboard.SetText(text);
    }

    private void UpdateCaretStatusFromActiveEditor()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var editor = GetActiveEditor();

        if (editor is null)
        {
            viewModel.StatusBar.UpdateLineColumn(1, 1);
            return;
        }

        viewModel.StatusBar.UpdateLineColumn(
            Math.Max(editor.TextArea.Caret.Line, 1),
            Math.Max(editor.TextArea.Caret.Column, 1));
    }

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
        SetStoppedRunExecutionSpan(editor, viewModel.StoppedRunState?.Snapshot.SourceLocation);
        TraceEditorEvent("editor.breakpoint.stopped-line", editor, $"line={line} | span={viewModel.StoppedRunState?.Snapshot.SourceLocation.Span.ToString() ?? "none"}");
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

    private void OnOutputTextBoxPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(sender, OutputTextBox) || OutputTextBox.SelectionLength <= 0)
        {
            _preserveOutputSelectionOnRightClick = false;
            _outputSelectionStart = 0;
            _outputSelectionLength = 0;
            return;
        }

        _preserveOutputSelectionOnRightClick = true;
        _outputSelectionStart = OutputTextBox.SelectionStart;
        _outputSelectionLength = OutputTextBox.SelectionLength;
    }

    private void OnOutputTextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(sender, OutputTextBox) ||
            !_preserveOutputSelectionOnRightClick ||
            _outputSelectionLength <= 0)
        {
            return;
        }

        OutputTextBox.Select(_outputSelectionStart, _outputSelectionLength);
    }

    private void OnOutputTextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (_preserveOutputSelectionOnRightClick && _outputSelectionLength > 0)
        {
            OutputTextBox.Focus();
            OutputTextBox.Select(_outputSelectionStart, _outputSelectionLength);
        }

        _preserveOutputSelectionOnRightClick = false;
        _outputSelectionStart = 0;
        _outputSelectionLength = 0;
        UpdateOutputContextMenuState();
    }

    private void OnOutputCopyMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (OutputTextBox.SelectionLength <= 0)
        {
            return;
        }

        CopyTextToClipboard(OutputTextBox.SelectedText);
        e.Handled = true;
    }

    private void OnOutputClearMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (_consoleService is not null)
        {
            _consoleService.ClearOutput();
            if (_bottomPane is not null)
            {
                _bottomPane.OutputText = _consoleService.OutputText;
            }
        }
        else
        {
            _bottomPane?.ClearOutput();
        }

        OutputTextBox.Select(0, 0);
        UpdateOutputContextMenuState();
        e.Handled = true;
    }

    private void OnOutputSelectAllMenuItemClick(object sender, RoutedEventArgs e)
    {
        OutputTextBox.Focus();
        OutputTextBox.SelectAll();
        UpdateOutputContextMenuState();
        e.Handled = true;
    }

    private void UpdateOutputContextMenuState()
    {
        OutputCopyMenuItem.IsEnabled = OutputTextBox.SelectionLength > 0;
        OutputClearMenuItem.IsEnabled = !string.IsNullOrEmpty(OutputTextBox.Text);
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

    private IdeTheme CurrentTheme =>
        DataContext is MainWindowViewModel viewModel ? viewModel.SelectedTheme : IdeTheme.Light;

    private void ApplyTheme(IdeTheme theme)
    {
        try
        {
            TraceService.Write("ui.theme.apply", $"theme={theme}");
            IdeThemeResourceService.Apply(theme);
            ApplyThemeToEditors(theme);
        }
        catch (Exception exception)
        {
            TraceService.WriteException("ui.theme.apply.exception", exception, $"theme={theme}");
            throw;
        }
    }

    private void ApplyLanguage(IdeLanguage language)
    {
        IdeLanguageResourceService.Apply(language);
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RefreshLocalizedShellText();
        }
    }

    private void ApplyThemeToEditors(IdeTheme theme)
    {
        if (DocumentTabs is null)
        {
            return;
        }

        var editors = FindDescendants<TextEditor>(DocumentTabs).ToArray();
        TraceService.Write("ui.theme.apply.editors", $"theme={theme} | editors={editors.Length}");

        foreach (var editor in editors)
        {
            ApplyEditorTheme(editor, theme);
        }
    }

    private void ApplyEditorTheme(TextEditor editor, IdeTheme theme)
    {
        editor.SyntaxHighlighting = AxiomHighlighting.GetDefinition(theme);
        editor.Background = TryFindResource("EditorBackgroundBrush") as Brush ?? editor.Background;
        editor.Foreground = TryFindResource("EditorForegroundBrush") as Brush ?? editor.Foreground;
        editor.LineNumbersForeground = TryFindResource("LineNumberForegroundBrush") as Brush ?? editor.LineNumbersForeground;
        editor.TextArea.Caret.CaretBrush = TryFindResource("EditorCaretBrush") as Brush ?? editor.TextArea.Caret.CaretBrush;
        ApplyBreakpointMarginTheme(editor);
        ApplyCurrentLineHighlightStyle(editor, _diagnosticHighlightedEditors.Contains(editor));
    }

    private void ApplyCurrentLineHighlightStyle(TextEditor editor, bool useDiagnosticStyle)
    {        
        var useStoppedRunStyle = !useDiagnosticStyle && _stoppedRunHighlightedEditors.Contains(editor);
        var backgroundKey = useDiagnosticStyle
            ? "DiagnosticLineHighlightBrush"
            : useStoppedRunStyle
                ? "StoppedRunLineHighlightBrush"
                : "EditorCurrentLineHighlightBrush";
        var borderKey = useDiagnosticStyle
            ? "DiagnosticLineBorderPen"
            : useStoppedRunStyle
                ? "StoppedRunLineBorderPen"
                : "EditorCurrentLineBorderPen";

        editor.TextArea.TextView.CurrentLineBackground = TryFindResource(backgroundKey) as Brush;
        editor.TextArea.TextView.CurrentLineBorder = TryFindResource(borderKey) as Pen;
        UpdateCurrentLineHighlightVisibility(editor);
    }

    private void UpdateCurrentLineHighlightVisibility(TextEditor editor)
    {
        editor.Options.HighlightCurrentLine =
            _diagnosticHighlightedEditors.Contains(editor) ||
            _stoppedRunHighlightedEditors.Contains(editor) ||
            editor.IsKeyboardFocusWithin;
    }

    private void FocusRequestedBottomPaneTarget()
    {
        if (_bottomPane is null)
        {
            return;
        }

        switch (_bottomPane.FocusTarget)
        {
            case BottomPaneFocusTarget.Diagnostics:
                if (DiagnosticsDataGrid is not null && DiagnosticsDataGrid.Items.Count > 0)
                {
                    DiagnosticsDataGrid.Focus();
                }

                break;
            default:
                FocusOutputArea();
                break;
        }
    }

    private void FocusSelectedBottomPaneContent()
    {
        if (BottomPaneTabs?.SelectedIndex == 1)
        {
            if (DiagnosticsDataGrid is not null && DiagnosticsDataGrid.Items.Count > 0)
            {
                DiagnosticsDataGrid.Focus();
            }

            return;
        }

        FocusOutputArea();
    }

    private void FocusOutputArea()
    {
        if (_consoleService?.IsWaitingForInput == true)
        {
            ConsoleInputTextBox.Focus();
            ConsoleInputTextBox.SelectAll();
            return;
        }

        OutputTextBox?.Focus();
    }

    private static string BuildDiagnosticClipboardText(ProgramDiagnosticItem diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return diagnostic.Line.HasValue && diagnostic.Column.HasValue
            ? $"{diagnostic.Stage} {diagnostic.Code} {diagnostic.Line}:{diagnostic.Column} {diagnostic.Message}"
            : $"{diagnostic.Stage} {diagnostic.Code} {diagnostic.Message}";
    }


    private static void RemoveDecorativeLineMargins(TextEditor editor)
    {
        var leftMargins = editor.TextArea.LeftMargins;

        for (var index = leftMargins.Count - 1; index >= 0; index--)
        {
            var marginTypeName = leftMargins[index].GetType().Name;

            if (marginTypeName.Contains("DottedLineMargin", StringComparison.Ordinal))
            {
                leftMargins.RemoveAt(index);
            }
        }
    }

    private void EnsureBreakpointMargin(TextEditor editor)
    {
        if (_breakpointMarginsByEditor.ContainsKey(editor))
        {
            return;
        }

        var margin = new BreakpointMargin
        {
            TextView = editor.TextArea.TextView
        };

        editor.TextArea.LeftMargins.Insert(0, margin);
        _breakpointMarginsByEditor[editor] = margin;
        ApplyBreakpointMarginTheme(editor);
    }

    private void RemoveBreakpointMargin(TextEditor editor)
    {
        if (!_breakpointMarginsByEditor.Remove(editor, out var margin))
        {
            return;
        }

        margin.TextView = null;
        editor.TextArea.LeftMargins.Remove(margin);
    }

    private void RefreshBreakpointMargins()
    {
        foreach (var editor in _breakpointMarginsByEditor.Keys.ToArray())
        {
            RefreshBreakpointMargin(editor);
        }
    }

    private void RefreshBreakpointMargin(TextEditor editor)
    {
        if (!_breakpointMarginsByEditor.TryGetValue(editor, out var margin))
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            margin.SetBreakpointLines(viewModel.SourceBreakpointLines);
        }
        else
        {
            margin.SetBreakpointLines([]);
        }
    }

    private void ApplyBreakpointMarginTheme(TextEditor editor)
    {
        if (!_breakpointMarginsByEditor.TryGetValue(editor, out var margin))
        {
            return;
        }

        margin.MarkerBrush = TryFindResource("BreakpointMarkerBrush") as Brush ?? Brushes.Firebrick;
        margin.InvalidateVisual();
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
        _lastCaretLineByEditor[editor] = Math.Max(editor.TextArea.Caret.Line, 1);
        UpdateCaretStatusFromActiveEditor();
    }

    private void TryApplyBlockEditing(TextEditor editor, int previousLineNumber, int currentLineNumber)
    {
        if (editor.Document is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var originalCaretOffset = editor.CaretOffset;

        if (!_blockEditingService.TryHandleLineTransition(
                editor.Document,
                previousLineNumber,
                currentLineNumber,
                originalCaretOffset,
                viewModel.SelectedFormattingStyle,
                out var newCaretOffset))
        {
            return;
        }

        _isApplyingLineFormatting = true;
        try
        {
            editor.CaretOffset = Math.Max(0, Math.Min(newCaretOffset, editor.Document.TextLength));
            TraceEditorEvent(
                "editor.block-edit.applied",
                editor,
                $"fromLine={previousLineNumber} | toLine={currentLineNumber} | caretBefore={originalCaretOffset} | caretAfter={editor.CaretOffset} | style={viewModel.SelectedFormattingStyle}");
        }
        finally
        {
            _isApplyingLineFormatting = false;
        }
    }

    private void TryFormatEditorLine(TextEditor editor, int lineNumber)
    {
        if (lineNumber < 1 || editor.Document is null || lineNumber > editor.Document.LineCount)
        {
            return;
        }

        var line = editor.Document.GetLineByNumber(lineNumber);
        var originalText = editor.Document.GetText(line.Offset, line.Length);

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var formattedText = _codeFormattingService.FormatLine(originalText, viewModel.SelectedFormattingStyle);
        var currentCaretOffset = editor.CaretOffset;
        var lineChanged = false;

        _isApplyingLineFormatting = true;
        try
        {
            if (!string.Equals(originalText, formattedText, StringComparison.Ordinal))
            {
                var newCaretOffset = CalculateLineReplacementCaretOffset(
                    currentCaretOffset,
                    line.Offset,
                    line.EndOffset,
                    line.Length,
                    formattedText.Length);

                editor.Document.Replace(line.Offset, line.Length, formattedText);
                editor.CaretOffset = Math.Max(0, Math.Min(newCaretOffset, editor.Document.TextLength));
                lineChanged = true;
                TraceEditorEvent(
                    "editor.line-format.applied",
                    editor,
                    $"line={lineNumber} | caretBefore={currentCaretOffset} | caretAfter={editor.CaretOffset} | before=\"{BuildTracePreview(originalText)}\" | after=\"{BuildTracePreview(formattedText)}\"");
            }

            if (viewModel.IsSmartIdentifierEnabled)
            {
                var currentLine = editor.Document.GetLineByNumber(lineNumber);
                var currentLineText = editor.Document.GetText(currentLine.Offset, currentLine.Length);
                var normalizedLineText = _smartIdentifierService.NormalizeLine(editor.Document.Text, currentLine.Offset, currentLine.Length);

                if (!string.Equals(currentLineText, normalizedLineText, StringComparison.Ordinal))
                {
                    editor.Document.Replace(currentLine.Offset, currentLine.Length, normalizedLineText);
                    lineChanged = true;
                    TraceEditorEvent(
                        "editor.identifier-normalized",
                        editor,
                        $"line={lineNumber} | caret={editor.CaretOffset} | before=\"{BuildTracePreview(currentLineText)}\" | after=\"{BuildTracePreview(normalizedLineText)}\"");
                }
            }
        }
        finally
        {
            _isApplyingLineFormatting = false;
        }

        if (!lineChanged)
        {
            return;
        }
    }

    private static int CalculateLineReplacementCaretOffset(
        int currentCaretOffset,
        int lineStart,
        int lineEnd,
        int originalLength,
        int newLength)
    {
        var delta = newLength - originalLength;

        if (currentCaretOffset == lineEnd)
        {
            return lineStart + newLength;
        }

        if (currentCaretOffset > lineEnd)
        {
            return currentCaretOffset + delta;
        }

        if (currentCaretOffset >= lineStart)
        {
            return Math.Min(
                lineStart + newLength,
                lineStart + Math.Max(0, currentCaretOffset - lineStart));
        }

        return currentCaretOffset;
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
            viewModel.StatusBar.StatusText = $"РћС€РёР±РєР° СЂРµРґР°РєС‚РѕСЂР°. РџРѕРґСЂРѕР±РЅРѕСЃС‚Рё РІ Р»РѕРіРµ: {TraceService.CurrentSessionLogPath}";
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

    private void PersistShellWindowState()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.PersistShellState(MapWindowState(WindowState));
    }

    private void ApplyPreferredWindowState(IdeWindowState windowState)
    {
        WindowState = windowState switch
        {
            IdeWindowState.Maximized => WindowState.Maximized,
            IdeWindowState.Minimized => WindowState.Minimized,
            _ => WindowState.Normal
        };
    }

    private static IdeWindowState MapWindowState(WindowState windowState)
    {
        return windowState switch
        {
            WindowState.Maximized => IdeWindowState.Maximized,
            WindowState.Minimized => IdeWindowState.Minimized,
            _ => IdeWindowState.Normal
        };
    }

    private sealed class DebugExecutionSpanRenderer : IBackgroundRenderer
    {
        private readonly MainWindow _owner;
        private readonly TextEditor _editor;
        private TextSegment? _segment;

        public DebugExecutionSpanRenderer(MainWindow owner, TextEditor editor)
        {
            _owner = owner;
            _editor = editor;
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void SetLocation(AxiomTextLocation? location)
        {
            _segment = CreateSegment(location);
            _editor.TextArea.TextView.InvalidateLayer(Layer);
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_segment is null || !textView.VisualLinesValid)
            {
                return;
            }

            var fill = _owner.TryFindResource("StoppedRunExecutionSpanBrush") as Brush;
            var border = _owner.TryFindResource("StoppedRunExecutionSpanBorderPen") as Pen;

            if (fill is null && border is null)
            {
                return;
            }

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, _segment, extendToFullWidthAtLineEnd: false))
            {
                var adjusted = new Rect(
                    rect.Left,
                    rect.Top + 1,
                    Math.Max(1, rect.Width),
                    Math.Max(1, rect.Height - 2));
                drawingContext.DrawRoundedRectangle(fill, border, adjusted, 2, 2);
            }
        }

        private TextSegment? CreateSegment(AxiomTextLocation? location)
        {
            if (location is not { } value || value.Span.Length <= 0)
            {
                return null;
            }

            var documentLength = _editor.Document?.TextLength ?? 0;
            if (documentLength <= 0 || value.Span.Start >= documentLength)
            {
                return null;
            }

            var start = Math.Max(0, value.Span.Start);
            var length = Math.Min(value.Span.Length, documentLength - start);

            return length > 0
                ? new TextSegment { StartOffset = start, Length = length }
                : null;
        }
    }

}
