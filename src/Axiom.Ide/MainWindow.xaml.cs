using System.ComponentModel;
using System.IO;
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
using Axiom.Ide.Editor;
using Axiom.Ide.Editor.Breakpoints;
using Axiom.Formatting;
using Axiom.Ide.Editor.Highlighting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Editor.StructuralEditing;
using Axiom.Ide.Files;
using Axiom.Ide.Models;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Input;
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
    private readonly Dictionary<TextEditor, BreakpointMargin> _breakpointMarginsByEditor = [];
    private readonly Dictionary<TextEditor, DebugExecutionSpanRenderer> _debugExecutionSpanRenderersByEditor = [];
    private readonly HashSet<TextEditor> _diagnosticHighlightedEditors = [];
    private readonly HashSet<TextEditor> _stoppedRunHighlightedEditors = [];
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
        InitializeOutputMouseInputHandlers();
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
        if (e.PropertyName == nameof(ProgramConsoleService.IsWaitingForInput)
            && _consoleService?.IsWaitingForInput == true
            && !_consoleService.IsScreenOutputActive)
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

    private void InitializeOutputMouseInputHandlers()
    {
        AddHandler(
            UIElement.PreviewMouseMoveEvent,
            new MouseEventHandler(OnOutputTextBoxPreviewMouseMove),
            handledEventsToo: true);
        AddHandler(
            UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(OnOutputTextBoxPreviewMouseDown),
            handledEventsToo: true);
        AddHandler(
            UIElement.PreviewMouseUpEvent,
            new MouseButtonEventHandler(OnOutputTextBoxPreviewMouseUp),
            handledEventsToo: true);
        AddHandler(
            UIElement.PreviewMouseWheelEvent,
            new MouseWheelEventHandler(OnOutputTextBoxPreviewMouseWheel),
            handledEventsToo: true);
        AddHandler(
            UIElement.MouseLeaveEvent,
            new MouseEventHandler(OnOutputTextBoxMouseLeave),
            handledEventsToo: true);
        AddHandler(
            UIElement.PreviewMouseRightButtonDownEvent,
            new MouseButtonEventHandler(OnOutputTextBoxPreviewMouseRightButtonDown),
            handledEventsToo: true);
        AddHandler(
            UIElement.PreviewMouseRightButtonUpEvent,
            new MouseButtonEventHandler(OnOutputTextBoxPreviewMouseRightButtonUp),
            handledEventsToo: true);
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

        public void SetLocation(Axiom.Core.TextLocation? location)
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

        private TextSegment? CreateSegment(Axiom.Core.TextLocation? location)
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
