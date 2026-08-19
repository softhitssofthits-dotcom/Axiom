using System.ComponentModel;
using Axiom.Execution;
using Axiom.Ide.Documents;
using Axiom.Ide.Files;
using Axiom.Ide.Output;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Status;

namespace Axiom.Ide.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void UseLightTheme()
    {
        _traceService.Write("theme.switch", "theme=Light");
        SelectedTheme = IdeTheme.Light;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseLightTheme",
            menuName: "View",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["theme"] = SelectedTheme.ToString()
            });
    }

    private void UseDarkTheme()
    {
        _traceService.Write("theme.switch", "theme=Dark");
        SelectedTheme = IdeTheme.Dark;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseDarkTheme",
            menuName: "View",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["theme"] = SelectedTheme.ToString()
            });
    }

    private void UseRussianLanguage()
    {
        SelectedLanguage = IdeLanguage.Russian;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseRussianLanguage",
            menuName: "View",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["language"] = SelectedLanguage.ToString()
            });
    }

    private void UseEnglishLanguage()
    {
        SelectedLanguage = IdeLanguage.English;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseEnglishLanguage",
            menuName: "View",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["language"] = SelectedLanguage.ToString()
            });
    }

    public void RefreshLocalizedShellText()
    {
        StatusBar.StatusText = GetLocalizedString("Status.Ready", "Ready.");
        UpdateTitle();
    }

    private void UseInterpreterExecutionMode()
    {
        SelectedExecutionMode = ExecutionMode.Interpreter;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseInterpreterExecutionMode",
            menuName: "Run",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = SelectedExecutionMode.ToString()
            });
    }

    private void UseBytecodeExecutionMode()
    {
        SelectedExecutionMode = ExecutionMode.Bytecode;
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "UseBytecodeExecutionMode",
            menuName: "Run",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = SelectedExecutionMode.ToString()
            });
    }

    private void CloseOutputPane()
    {
        BottomPane.HideOutput();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "CloseOutputPane",
            menuName: "View",
            document: Documents.ActiveDocument,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["pane"] = "Output",
                ["visible"] = BottomPane.IsOutputVisible.ToString()
            });
    }

    private void CloseDiagnosticsPane()
    {
        BottomPane.HideDiagnostics();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "CloseDiagnosticsPane",
            menuName: "View",
            document: Documents.ActiveDocument,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["pane"] = "Diagnostics",
                ["visible"] = BottomPane.IsDiagnosticsVisible.ToString()
            });
    }

    private bool CanModifyDocuments() => !IsShellInteractionLocked();

    private bool IsShellInteractionLocked() => StatusBar.IsRunning || BottomPane.IsWaitingForInput;

    public void PersistShellState(IdeWindowState windowState)
    {
        _preferredWindowState = windowState;
        PersistShellPreferences();
    }

    public void ReportEditorCaretActivity(DocumentTabViewModel? document, int line, int column)
    {
        RecordEditorNoise(
            _caretNoiseBurst,
            document,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["line"] = Math.Max(line, 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["column"] = Math.Max(column, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    public void ReportEditorSelectionActivity(DocumentTabViewModel? document, int selectionStart, int selectionLength)
    {
        RecordEditorNoise(
            _selectionNoiseBurst,
            document,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["selectionStart"] = Math.Max(selectionStart, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["selectionLength"] = Math.Max(selectionLength, 0).ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    public void ReportEditorTextActivity(DocumentTabViewModel? document, int textLength)
    {
        RecordEditorNoise(
            _textNoiseBurst,
            document,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["textLength"] = Math.Max(textLength, 0).ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    public void FlushEditorActivityEvents()
    {
        lock (_editorNoiseSync)
        {
            FlushEditorNoiseBurst(_caretNoiseBurst);
            FlushEditorNoiseBurst(_selectionNoiseBurst);
            FlushEditorNoiseBurst(_textNoiseBurst);
        }
    }

    private void RestoreShellPreferences()
    {
        var snapshot = _recentFilesService.GetSnapshot();
        FilesPane.IsVisible = snapshot.IsFilesPaneVisible;
        StatusBar.IsVisible = snapshot.IsStatusBarVisible;
        _preferredWindowState = snapshot.WindowState;
        _selectedTheme = snapshot.Theme;
        _selectedLanguage = snapshot.Language;
        _selectedExecutionMode = snapshot.PreferredExecutionMode;
        _isSmartIdentifierEnabled = snapshot.SmartIdentifierEnabled;
    }

    private void OnFilesPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilesPaneViewModel.ShowHiddenFiles))
        {
            ReloadFilesPane();
            StatusBar.StatusText = FilesPane.ShowHiddenFiles
                ? GetLocalizedString("Files.Hidden.StatusShown", "Hidden files are now shown in File Manager.")
                : GetLocalizedString("Files.Hidden.StatusHidden", "Hidden files are hidden in File Manager.");
            return;
        }

        if (e.PropertyName == nameof(FilesPaneViewModel.SelectedNode))
        {
            RaiseFilesPaneCommandCanExecuteChanged();
            return;
        }

        if (e.PropertyName != nameof(FilesPaneViewModel.IsVisible))
        {
            return;
        }

        CloseFilesPaneCommand.RaiseCanExecuteChanged();
        PersistShellPreferences();
    }

    private void OnBottomPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BottomPaneViewModel.CurrentInputText)
            && !_programConsoleService.IsScreenOutputActive
            && !string.Equals(_programConsoleService.CurrentInputText, BottomPane.CurrentInputText, StringComparison.Ordinal))
        {
            _programConsoleService.CurrentInputText = BottomPane.CurrentInputText;
        }

        if (e.PropertyName is nameof(BottomPaneViewModel.IsOutputVisible)
            or nameof(BottomPaneViewModel.IsDiagnosticsVisible)
            or nameof(BottomPaneViewModel.IsWaitingForInput))
        {
            CloseOutputPaneCommand.RaiseCanExecuteChanged();
            CloseDiagnosticsPaneCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnStatusBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StatusBarViewModel.IsVisible))
        {
            return;
        }

        LogIdeEvent(
            IdeEventLevel.Trace,
            "status-bar",
            "status-bar.changed",
            commandName: e.PropertyName,
            document: Documents.ActiveDocument,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["property"] = e.PropertyName
            });

        PersistShellPreferences();
    }

    private void PersistShellPreferences()
    {
        _recentFilesService.UpdateShellPreferences(
            FilesPane.IsVisible,
            StatusBar.IsVisible,
            _preferredWindowState,
            SelectedTheme,
            SelectedLanguage,
            IsSmartIdentifierEnabled,
            SelectedExecutionMode);
    }

    private void RaiseShellCommandCanExecuteChanged()
    {
        NewCommand.RaiseCanExecuteChanged();
        OpenCommand.RaiseCanExecuteChanged();
        OpenRecentCommand.RaiseCanExecuteChanged();
        OpenFilesPaneNodeCommand.RaiseCanExecuteChanged();
        NavigateFilesPaneNodeCommand.RaiseCanExecuteChanged();
        OpenFilesPaneNodeExternallyCommand.RaiseCanExecuteChanged();
        ShowFilesPaneNodePropertiesCommand.RaiseCanExecuteChanged();
        RaiseFilesPaneCommandCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        SaveAllCommand.RaiseCanExecuteChanged();
        CloseTabCommand.RaiseCanExecuteChanged();
        CompileCommand.RaiseCanExecuteChanged();
        RunCommand.RaiseCanExecuteChanged();
        CancelRunCommand.RaiseCanExecuteChanged();
        ContinueRunCommand.RaiseCanExecuteChanged();
        StepOverCommand.RaiseCanExecuteChanged();
        StepIntoCommand.RaiseCanExecuteChanged();
        StepOutCommand.RaiseCanExecuteChanged();
        StepBackCommand.RaiseCanExecuteChanged();
        RunOrContinueCommand.RaiseCanExecuteChanged();
        StopDebuggingCommand.RaiseCanExecuteChanged();
        RestartDebuggingCommand.RaiseCanExecuteChanged();
        ClearAllSourceBreakpointsCommand.RaiseCanExecuteChanged();
        ToggleCurrentSourceBreakpointCommand.RaiseCanExecuteChanged();
        ExitCommand.RaiseCanExecuteChanged();
    }

    private void RaiseInputCommandCanExecuteChanged()
    {
        SubmitInputCommand.RaiseCanExecuteChanged();
    }
}
