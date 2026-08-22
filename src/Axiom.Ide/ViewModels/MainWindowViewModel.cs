using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Axiom.Execution;
using Axiom.Ide.Commands;
using Axiom.Ide.Documents;
using Axiom.Ide.Editor.Formatting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Files;
using Axiom.Ide.Models;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Documents;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Prompts;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.Status;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.ViewModels;

/// <summary>
/// Корневая shell-модель представления нового дизайна с композицией дочерних моделей.
/// Пока сохраняет обратную совместимость со старым UI.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDocumentFileService _documentFileService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly ProgramConsoleService _programConsoleService;
    private readonly IProgramRunService _programRunService;
    private readonly IDialogPromptService _promptService;
    private readonly IFileTreeService _fileTreeService;
    private readonly IFileOperationsService _fileOperationsService;
    private readonly IExternalNavigationService _externalNavigationService;
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly IIdeEventLogViewerService _eventLogViewerService;
    private readonly Func<string?> _manualPathResolver;
    private readonly ICodeFormattingService _codeFormattingService;
    private readonly AxiomSmartIdentifierService _smartIdentifierService;
    private CodeFormattingStyle _selectedFormattingStyle = CodeFormattingStyle.Classic;
    private IdeWindowState _preferredWindowState;
    private IdeTheme _selectedTheme = IdeTheme.Light;
    private IdeLanguage _selectedLanguage = IdeLanguage.Russian;
    private ExecutionMode _selectedExecutionMode = ExecutionMode.Bytecode;
    private bool _isSmartIdentifierEnabled;
    private string? _filesPaneClipboardPath;
    private bool _filesPaneClipboardIsCut;
    private bool _filesPaneClipboardIsDirectory;
    private IProgramDebuggerState? _stoppedRunState;
    private DocumentTabViewModel? _stoppedRunDocument;
    private int? _stoppedRunLine;
    private ManualCancellationProvider? _runCancellationProvider;
    private readonly object _editorNoiseSync = new();
    private readonly EditorNoiseBurst _caretNoiseBurst = new("editor.caret.burst", "Caret");
    private readonly EditorNoiseBurst _selectionNoiseBurst = new("editor.selection.burst", "Selection");
    private readonly EditorNoiseBurst _textNoiseBurst = new("editor.text.burst", "TextChanged");

    public MainWindowViewModel(
        IProgramRunService programRunService,
        IDocumentFileService documentFileService,
        IRecentFilesService recentFilesService,
        ProgramConsoleService programConsoleService,
        IDialogPromptService promptService,
        ICodeFormattingService codeFormattingService,
        AxiomSmartIdentifierService smartIdentifierService,
        IFileTreeService fileTreeService,
        IFileOperationsService fileOperationsService,
        IIdeTraceService traceService,
        IExternalNavigationService externalNavigationService,
        IIdeEventLogService? eventLogService = null,
        IIdeEventLogViewerService? eventLogViewerService = null,
        Func<string?>? manualPathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(programRunService);
        ArgumentNullException.ThrowIfNull(documentFileService);
        ArgumentNullException.ThrowIfNull(recentFilesService);
        ArgumentNullException.ThrowIfNull(programConsoleService);
        ArgumentNullException.ThrowIfNull(promptService);
        ArgumentNullException.ThrowIfNull(codeFormattingService);
        ArgumentNullException.ThrowIfNull(smartIdentifierService);
        ArgumentNullException.ThrowIfNull(fileTreeService);
        ArgumentNullException.ThrowIfNull(fileOperationsService);
        ArgumentNullException.ThrowIfNull(traceService);
        ArgumentNullException.ThrowIfNull(externalNavigationService);

        _programRunService = programRunService;
        _documentFileService = documentFileService;
        _recentFilesService = recentFilesService;
        _programConsoleService = programConsoleService;
        _promptService = promptService;
        _codeFormattingService = codeFormattingService;
        _smartIdentifierService = smartIdentifierService;
        _fileTreeService = fileTreeService;
        _fileOperationsService = fileOperationsService;
        _traceService = traceService;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
        _eventLogViewerService = eventLogViewerService ?? NullIdeEventLogViewerService.Instance;
        _externalNavigationService = externalNavigationService;
        _manualPathResolver = manualPathResolver ?? ResolveManualPath;

        // Child viewmodels
        Documents = new DocumentWorkspaceService(documentFileService, recentFilesService, () => SelectedLanguage);
        Documents.DocumentOpened += FormatDocumentAfterOpen;
        StatusBar = new StatusBarViewModel();
        BottomPane = new BottomPaneViewModel();
        FilesPane = new FilesPaneViewModel();
        RecentFiles = [];
        _preferredWindowState = IdeWindowState.Normal;

        // Создаём стартовую пустую вкладку
        Documents.CreateNewDocument();
        RestoreShellPreferences();
        RefreshRecentFiles();
        UpdateFilesPaneRoot();

        // Подписываемся на изменения active document для статуса
        Documents.PropertyChanged += OnDocumentsPropertyChanged;
        Documents.Documents.CollectionChanged += OnDocumentsCollectionChanged;
        FilesPane.PropertyChanged += OnFilesPanePropertyChanged;
        BottomPane.PropertyChanged += OnBottomPanePropertyChanged;
        StatusBar.PropertyChanged += OnStatusBarPropertyChanged;

        foreach (var document in Documents.Documents)
        {
            AttachDocument(document);
        }

        // Подписываемся на изменения console output для bottom pane
        _programConsoleService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProgramConsoleService.OutputText))
            {
                BottomPane.OutputText = _programConsoleService.OutputText;
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.IsWaitingForInput))
            {
                ApplyConsoleInputStateToBottomPane();
                if (BottomPane.IsWaitingForInput)
                {
                    BottomPane.SelectOutput();
                    BottomPane.RequestOutputFocus();
                }

                RaiseInputCommandCanExecuteChanged();
                RaiseShellCommandCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.IsScreenOutputActive))
            {
                ApplyConsoleInputStateToBottomPane();
                RaiseInputCommandCanExecuteChanged();
                RaiseShellCommandCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.CurrentPromptText))
            {
                BottomPane.CurrentPromptText = _programConsoleService.IsScreenOutputActive
                    ? string.Empty
                    : _programConsoleService.CurrentPromptText;
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.CurrentInputText))
            {
                BottomPane.CurrentInputText = _programConsoleService.IsScreenOutputActive
                    ? string.Empty
                    : _programConsoleService.CurrentInputText;
            }
        };

        // Commands
        NewCommand = new RelayCommand(NewDocument, CanModifyDocuments);
        OpenCommand = new RelayCommand(OpenDocument, CanModifyDocuments);
        OpenRecentCommand = new RelayCommand(OpenRecentDocument, CanOpenRecentDocument);
        OpenFilesPaneNodeCommand = new RelayCommand(OpenFilesPaneNode, CanOpenFilesPaneNode);
        NavigateFilesPaneNodeCommand = new RelayCommand(NavigateFilesPaneNode, CanNavigateFilesPaneNode);
        OpenFilesPaneNodeExternallyCommand = new RelayCommand(OpenFilesPaneNodeExternally, CanOpenFilesPaneNodeExternally);
        NewFilesPaneFileCommand = new RelayCommand(CreateFilesPaneFile, CanCreateFilesPaneItem);
        NewFilesPaneDirectoryCommand = new RelayCommand(CreateFilesPaneDirectory, CanCreateFilesPaneItem);
        CutFilesPaneNodeCommand = new RelayCommand(CutFilesPaneNode, CanMutateFilesPaneNode);
        CopyFilesPaneNodeCommand = new RelayCommand(CopyFilesPaneNode, CanMutateFilesPaneNode);
        PasteIntoFilesPaneCommand = new RelayCommand(PasteIntoFilesPane, CanPasteIntoFilesPane);
        RenameFilesPaneNodeCommand = new RelayCommand(RenameFilesPaneNode, CanMutateFilesPaneNode);
        MoveFilesPaneNodeToRecycleBinCommand = new RelayCommand(MoveFilesPaneNodeToRecycleBin, CanMutateFilesPaneNode);
        ShowFilesPaneNodePropertiesCommand = new RelayCommand(ShowFilesPaneNodeProperties, CanShowFilesPaneNodeProperties);
        ExpandFilesPaneNodeCommand = new RelayCommand(ExpandFilesPaneNode);
        CloseFilesPaneCommand = new RelayCommand(CloseFilesPane, CanCloseFilesPane);
        UseClassicFormattingCommand = new RelayCommand(UseClassicFormatting);
        UseModernFormattingCommand = new RelayCommand(UseModernFormatting);
        ToggleSmartIdentifierCommand = new RelayCommand(ToggleSmartIdentifier);
        UseLightThemeCommand = new RelayCommand(UseLightTheme);
        UseDarkThemeCommand = new RelayCommand(UseDarkTheme);
        UseRussianLanguageCommand = new RelayCommand(UseRussianLanguage);
        UseEnglishLanguageCommand = new RelayCommand(UseEnglishLanguage);
        UseInterpreterExecutionModeCommand = new RelayCommand(UseInterpreterExecutionMode);
        UseBytecodeExecutionModeCommand = new RelayCommand(UseBytecodeExecutionMode);
        ShowOutputCommand = new RelayCommand(ShowOutputPane);
        ShowDiagnosticsCommand = new RelayCommand(ShowDiagnosticsPane);
        ShowIdeEventLogViewerCommand = new RelayCommand(ShowIdeEventLogViewer, CanUseIdeEventLogCommands);
        OpenIdeEventLogFileCommand = new RelayCommand(OpenIdeEventLogFile, CanUseIdeEventLogCommands);
        OpenIdeEventLogFolderCommand = new RelayCommand(OpenIdeEventLogFolder, CanUseIdeEventLogCommands);
        ClearIdeEventLogsCommand = new RelayCommand(ClearIdeEventLogs, CanUseIdeEventLogCommands);
        CloseOutputPaneCommand = new RelayCommand(CloseOutputPane, CanCloseOutputPane);
        CloseDiagnosticsPaneCommand = new RelayCommand(CloseDiagnosticsPane, CanCloseDiagnosticsPane);
        SaveCommand = new RelayCommand(SaveDocument, CanSaveDocument);
        SaveAsCommand = new RelayCommand(SaveDocumentAs, CanSaveAsDocument);
        SaveAllCommand = new RelayCommand(SaveAllDocuments, CanSaveAllDocuments);
        CloseTabCommand = new RelayCommand(CloseTab, CanCloseTab);
        CompileCommand = new RelayCommand(async () => await CompileProgramAsync(), CanCompileProgram);
        RunCommand = new RelayCommand(async () => await RunProgramAsync(useSourceBreakpoints: false), CanRunProgram);
        CancelRunCommand = new RelayCommand(CancelRunningProgram, CanCancelRunningProgram);
        ContinueRunCommand = new RelayCommand(async () => await ContinueProgramAsync(), CanContinueProgram);
        StepOverCommand = new RelayCommand(async () => await StepOverProgramAsync(), CanStepOverProgram);
        StepIntoCommand = new RelayCommand(async () => await StepIntoProgramAsync(), CanStepIntoProgram);
        StepOutCommand = new RelayCommand(async () => await StepOutProgramAsync(), CanStepOutProgram);
        StepBackCommand = new RelayCommand(async () => await StepBackProgramAsync(), CanStepBackProgram);
        RunOrContinueCommand = new RelayCommand(async () => await RunOrContinueProgramAsync(), CanRunOrContinueProgram);
        StopDebuggingCommand = new RelayCommand(StopDebugging, CanStopDebugging);
        RestartDebuggingCommand = new RelayCommand(RestartDebugging, CanRestartDebugging);
        ClearAllSourceBreakpointsCommand = new RelayCommand(
            ClearAllSourceBreakpoints,
            CanClearAllSourceBreakpoints);
        ToggleCurrentSourceBreakpointCommand = new RelayCommand(
            ToggleCurrentSourceBreakpoint,
            CanToggleCurrentSourceBreakpoint);
        SubmitInputCommand = new RelayCommand(SubmitInput, () => BottomPane.IsWaitingForInput);
        OpenManualCommand = new RelayCommand(OpenManual);
        AboutCommand = new RelayCommand(ShowAbout);
        ExitCommand = new RelayCommand(ExitApplication, CanModifyDocuments);

        // Подписка на изменения для CanExecute
        OnActiveDocumentChanged();
    }

    // Child viewmodels
    public DocumentWorkspaceService Documents { get; }
    public StatusBarViewModel StatusBar { get; }
    public BottomPaneViewModel BottomPane { get; }
    public FilesPaneViewModel FilesPane { get; }
    public ObservableCollection<RecentFileItem> RecentFiles { get; }
    public bool HasRecentFiles => RecentFiles.Count > 0;
    public IdeWindowState PreferredWindowState => _preferredWindowState;
    public CodeFormattingStyle SelectedFormattingStyle
    {
        get => _selectedFormattingStyle;
        private set
        {
            if (!SetProperty(ref _selectedFormattingStyle, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsClassicFormattingEnabled));
            OnPropertyChanged(nameof(IsModernFormattingEnabled));
        }
    }

    public bool IsClassicFormattingEnabled
    {
        get => SelectedFormattingStyle == CodeFormattingStyle.Classic;
    }

    public bool IsModernFormattingEnabled
    {
        get => SelectedFormattingStyle == CodeFormattingStyle.Modern;
    }

    public bool IsSmartIdentifierEnabled
    {
        get => _isSmartIdentifierEnabled;
        private set
        {
            if (!SetProperty(ref _isSmartIdentifierEnabled, value))
            {
                return;
            }

            PersistShellPreferences();
        }
    }

    public IdeTheme SelectedTheme
    {
        get => _selectedTheme;
        private set
        {
            if (!SetProperty(ref _selectedTheme, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsLightThemeEnabled));
            OnPropertyChanged(nameof(IsDarkThemeEnabled));
            PersistShellPreferences();
        }
    }

    public bool IsLightThemeEnabled => SelectedTheme == IdeTheme.Light;

    public bool IsDarkThemeEnabled => SelectedTheme == IdeTheme.Dark;

    public IdeLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        private set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsRussianLanguageEnabled));
            OnPropertyChanged(nameof(IsEnglishLanguageEnabled));
            PersistShellPreferences();
        }
    }

    public bool IsRussianLanguageEnabled => SelectedLanguage == IdeLanguage.Russian;

    public bool IsEnglishLanguageEnabled => SelectedLanguage == IdeLanguage.English;

    public ExecutionMode SelectedExecutionMode
    {
        get => _selectedExecutionMode;
        private set
        {
            if (!SetProperty(ref _selectedExecutionMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsInterpreterExecutionModeEnabled));
            OnPropertyChanged(nameof(IsBytecodeExecutionModeEnabled));
            PersistShellPreferences();
        }
    }

    public bool IsInterpreterExecutionModeEnabled => SelectedExecutionMode == ExecutionMode.Interpreter;

    public bool IsBytecodeExecutionModeEnabled => SelectedExecutionMode == ExecutionMode.Bytecode;

    public IReadOnlyList<int> SourceBreakpointLines =>
        Documents.ActiveDocument?.SourceBreakpointLines ?? [];

    public IProgramDebuggerState? StoppedRunState =>
        IsStoppedRunForActiveDocument ? _stoppedRunState : null;

    public bool HasStoppedRun => IsStoppedRunForActiveDocument && _stoppedRunState?.CanResume == true;

    public int? StoppedRunLine => IsStoppedRunForActiveDocument ? _stoppedRunLine : null;

    internal IIdeTraceService TraceService => _traceService;

    // Commands
    public RelayCommand NewCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand OpenRecentCommand { get; }
    public RelayCommand OpenFilesPaneNodeCommand { get; }
    public RelayCommand NavigateFilesPaneNodeCommand { get; }
    public RelayCommand OpenFilesPaneNodeExternallyCommand { get; }
    public RelayCommand NewFilesPaneFileCommand { get; }
    public RelayCommand NewFilesPaneDirectoryCommand { get; }
    public RelayCommand CutFilesPaneNodeCommand { get; }
    public RelayCommand CopyFilesPaneNodeCommand { get; }
    public RelayCommand PasteIntoFilesPaneCommand { get; }
    public RelayCommand RenameFilesPaneNodeCommand { get; }
    public RelayCommand MoveFilesPaneNodeToRecycleBinCommand { get; }
    public RelayCommand ShowFilesPaneNodePropertiesCommand { get; }
    public RelayCommand ExpandFilesPaneNodeCommand { get; }
    public RelayCommand CloseFilesPaneCommand { get; }
    public RelayCommand UseClassicFormattingCommand { get; }
    public RelayCommand UseModernFormattingCommand { get; }
    public RelayCommand ToggleSmartIdentifierCommand { get; }
    public RelayCommand UseLightThemeCommand { get; }
    public RelayCommand UseDarkThemeCommand { get; }
    public RelayCommand UseRussianLanguageCommand { get; }
    public RelayCommand UseEnglishLanguageCommand { get; }
    public RelayCommand UseInterpreterExecutionModeCommand { get; }
    public RelayCommand UseBytecodeExecutionModeCommand { get; }
    public RelayCommand ShowOutputCommand { get; }
    public RelayCommand ShowDiagnosticsCommand { get; }
    public RelayCommand ShowIdeEventLogViewerCommand { get; }
    public RelayCommand OpenIdeEventLogFileCommand { get; }
    public RelayCommand OpenIdeEventLogFolderCommand { get; }
    public RelayCommand ClearIdeEventLogsCommand { get; }
    public RelayCommand CloseOutputPaneCommand { get; }
    public RelayCommand CloseDiagnosticsPaneCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SaveAsCommand { get; }
    public RelayCommand SaveAllCommand { get; }
    public RelayCommand CloseTabCommand { get; }
    public RelayCommand CompileCommand { get; }
    public RelayCommand RunCommand { get; }
    public RelayCommand CancelRunCommand { get; }
    public RelayCommand ContinueRunCommand { get; }
    public RelayCommand StepOverCommand { get; }
    public RelayCommand StepIntoCommand { get; }
    public RelayCommand StepOutCommand { get; }
    public RelayCommand StepBackCommand { get; }
    public RelayCommand RunOrContinueCommand { get; }
    public RelayCommand StopDebuggingCommand { get; }
    public RelayCommand RestartDebuggingCommand { get; }
    public RelayCommand ClearAllSourceBreakpointsCommand { get; }
    public RelayCommand ToggleCurrentSourceBreakpointCommand { get; }
    public RelayCommand SubmitInputCommand { get; }
    public RelayCommand OpenManualCommand { get; }
    public RelayCommand AboutCommand { get; }
    public RelayCommand ExitCommand { get; }

    // Legacy properties для обратной совместимости со старым UI
    public ProgramConsoleService InputConsole => _programConsoleService;

    public string SourceText
    {
        get => Documents.ActiveDocument?.SourceText ?? string.Empty;
        set
        {
            if (Documents.ActiveDocument is not null)
            {
                Documents.ActiveDocument.SourceText = value;
            }
        }
    }

    public string WindowTitle
    {
        get
        {
            if (Documents.ActiveDocument is null)
            {
                return GetLocalizedString("Window.Title.Default", "Axiom IDE");
            }

            var fileName = Documents.ActiveDocument.DisplayName;
            var dirty = Documents.ActiveDocument.IsDirty ? "*" : string.Empty;
            return GetLocalizedFormattedString("Window.Title.WithDocument", "{0}{1} - Axiom IDE", fileName, dirty);
        }
    }

    private void NewDocument()
    {
        Documents.CreateNewDocument();
        UpdateTitle();
        StatusBar.StatusText = GetLocalizedString("Documents.New.StatusCreated", "New document created.");
        LogIdeEvent(
            IdeEventLevel.Info,
            "documents",
            "document.created",
            commandName: "New",
            document: Documents.ActiveDocument);
    }

    private void OpenDocument()
    {
        var suggestedDir = Documents.GetSuggestedDirectoryPath(Documents.ActiveDocument?.FilePath);
        var document = Documents.OpenDocument(suggestedDir, out var statusMessage);

        if (document is not null)
        {
            StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusOpened", "Opened {0}.", document.DisplayName);
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.opened",
                commandName: "Open",
                document: document);
        }
        else
        {
            StatusBar.StatusText = statusMessage ?? GetLocalizedString("Documents.Open.StatusCancelled", "Open cancelled.");
        }

        UpdateTitle();
    }

    private void SaveDocument()
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Documents.Common.StatusNoActive", "No active document.");
            return;
        }

        var saved = Documents.SaveDocument(Documents.ActiveDocument, out var statusMessage);

        if (saved)
        {
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.saved",
                commandName: "Save",
                document: Documents.ActiveDocument);
        }

        StatusBar.StatusText = statusMessage ?? GetLocalizedString("Documents.Save.StatusCompleted", "Save completed.");
        UpdateTitle();
    }

    private void SaveDocumentAs()
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Documents.Common.StatusNoActive", "No active document.");
            return;
        }

        var saved = Documents.SaveDocumentAs(Documents.ActiveDocument, out var statusMessage);

        if (saved)
        {
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.saved-as",
                commandName: "SaveAs",
                document: Documents.ActiveDocument);
        }

        StatusBar.StatusText = statusMessage ?? GetLocalizedString("Documents.SaveAs.StatusCompleted", "Save As completed.");
        UpdateTitle();
    }

    private void SaveAllDocuments()
    {
        var saved = Documents.SaveAllDocuments(out var statusMessage);

        if (saved)
        {
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "documents.saved-all",
                commandName: "SaveAll",
                document: Documents.ActiveDocument);
        }

        StatusBar.StatusText = statusMessage ?? GetLocalizedString("Documents.SaveAll.StatusCompleted", "Save all completed.");
        UpdateTitle();
    }

    private void CloseTab(object? parameter)
    {
        var document = parameter as DocumentTabViewModel ?? Documents.ActiveDocument;

        if (document is null)
        {
            return;
        }

        var documentName = document.DisplayName;
        var closed = Documents.CloseDocument(document, PromptSaveDiscardCancel);

        if (closed)
        {
            StatusBar.StatusText = GetLocalizedFormattedString("Close.StatusClosed", "Closed {0}.", documentName);
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.closed",
                commandName: "CloseTab",
                document: document);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Close.StatusCancelled", "Close cancelled.");
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.close-cancelled",
                commandName: "CloseTab",
                document: document);
        }

        UpdateTitle();
    }

    private void ExitApplication()
    {
        var closed = Documents.CloseAllDocuments(PromptSaveDiscardCancel);

        if (!closed)
        {
            StatusBar.StatusText = GetLocalizedString("Exit.StatusCancelled", "Exit cancelled.");
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "command.cancelled",
                commandName: "Exit",
                menuName: "File",
                document: Documents.ActiveDocument);
            return;
        }

        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "Exit",
            menuName: "File",
            document: Documents.ActiveDocument);
        System.Windows.Application.Current.Shutdown();
    }

    private bool? PromptSaveDiscardCancel(string fileName)
    {
        return _promptService.ShowSaveDiscardCancel(fileName);
    }

    private void ShowAbout()
    {
        StatusBar.StatusText = GetLocalizedString("About.StatusOpened", "About dialog opened.");
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "About",
            menuName: "Help",
            document: Documents.ActiveDocument);
        _promptService.ShowInfo(
            GetLocalizedString(
                "About.Message",
                "Axiom IDE\r\n\r\nLightweight editor and interpreter shell for the Axiom language.\r\nDesigned around a small QuickBASIC-inspired workflow with a modern WPF UI."),
            GetLocalizedString("About.Title", "About Axiom IDE"));
        StatusBar.StatusText = GetLocalizedString("About.StatusClosed", "About dialog closed.");
    }

    private string GetLocalizedString(string key, string fallback)
    {
        return IdeLanguageResourceService.GetString(SelectedLanguage, key, fallback);
    }

    private string GetLocalizedFormattedString(string key, string fallbackFormat, params object?[] args)
    {
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, GetLocalizedString(key, fallbackFormat), args);
    }

    private void OpenManual()
    {
        const string manualRelativePath = @"docs\manual\index.html";
        var manualPath = _manualPathResolver();

        if (string.IsNullOrWhiteSpace(manualPath))
        {
            var title = GetLocalizedString("Manual.ErrorTitle", "Open Manual");
            _promptService.ShowError(
                GetLocalizedFormattedString("Manual.ErrorMissing", "Could not find local manual file '{0}'.", manualRelativePath),
                title);
            StatusBar.StatusText = GetLocalizedString("Manual.StatusNotFound", "Manual not found.");
            return;
        }

        try
        {
            _externalNavigationService.OpenPath(manualPath);
            StatusBar.StatusText = GetLocalizedString("Manual.StatusOpened", "Manual opened.");
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "command.executed",
                commandName: "OpenManual",
                menuName: "Help",
                document: Documents.ActiveDocument,
                textPreview: manualPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("Manual.ErrorTitle", "Open Manual"));
            StatusBar.StatusText = GetLocalizedString("Manual.StatusOpenFailed", "Manual open failed.");
        }
    }

    private void OpenRecentDocument(object? parameter)
    {
        if (parameter is not RecentFileItem recentFile)
        {
            StatusBar.StatusText = GetLocalizedString("Recent.StatusUnavailable", "Recent file is not available.");
            return;
        }

        var document = Documents.OpenDocumentFromPath(recentFile.FilePath, out var statusMessage);

        if (document is not null)
        {
            StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusOpened", "Opened {0}.", document.DisplayName);
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            UpdateTitle();
            LogIdeEvent(
                IdeEventLevel.Info,
                "documents",
                "document.opened-recent",
                commandName: "OpenRecent",
                document: document);
            return;
        }

        _recentFilesService.RemoveFile(recentFile.FilePath);
        RefreshRecentFiles();
        StatusBar.StatusText = GetLocalizedFormattedString(
            "Recent.StatusMissing",
            "Recent file is no longer available: {0}.",
            recentFile.FileName);
    }

    private bool CanOpenRecentDocument(object? parameter) =>
        parameter is RecentFileItem && CanModifyDocuments();
    private bool CanCloseFilesPane() => FilesPane.IsVisible;
    private bool CanCloseOutputPane() => BottomPane.IsOutputVisible && !BottomPane.IsWaitingForInput;
    private bool CanCloseDiagnosticsPane() => BottomPane.IsDiagnosticsVisible;
    private bool CanSaveDocument() =>
        Documents.ActiveDocument is { IsReadOnly: false } && CanModifyDocuments();
    private bool CanSaveAsDocument() =>
        Documents.ActiveDocument is { IsReadOnly: false } && CanModifyDocuments();
    private bool CanSaveAllDocuments() =>
        Documents.Documents.Any(document => document.IsDirty && !document.IsReadOnly) && CanModifyDocuments();
    private bool CanCompileProgram() => Documents.ActiveDocument is not null && !IsShellInteractionLocked();
    private bool CanRunProgram() => Documents.ActiveDocument is not null && !IsShellInteractionLocked();
    private bool CanContinueProgram() => HasStoppedRun && !IsShellInteractionLocked();
    private bool CanStepOverProgram() => (HasStoppedRun || Documents.ActiveDocument is not null) && !IsShellInteractionLocked();
    private bool CanStepIntoProgram() => (HasStoppedRun || Documents.ActiveDocument is not null) && !IsShellInteractionLocked();
    private bool CanStepOutProgram() => HasStoppedRun && !IsShellInteractionLocked();
    private bool CanStepBackProgram() => _stoppedRunState?.CanStepBack == true && !IsShellInteractionLocked();
    private bool CanRunOrContinueProgram() => HasStoppedRun ? CanContinueProgram() : CanRunProgram();
    private bool CanStopDebugging() => HasStoppedRun && !IsShellInteractionLocked();
    private bool CanRestartDebugging() => HasStoppedRun && !IsShellInteractionLocked();
    private bool CanCloseTab(object? parameter) =>
        (parameter is null
            ? Documents.ActiveDocument is not null
            : parameter is DocumentTabViewModel)
        && CanModifyDocuments();
    private bool CanNavigateFilesPaneNode(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is { IsPlaceholder: false } && CanModifyDocuments();
    private bool CanOpenFilesPaneNodeExternally(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is { IsPlaceholder: false } && CanModifyDocuments();
    private bool CanCreateFilesPaneItem(object? parameter) =>
        GetFilesPaneTargetDirectoryPath(parameter) is not null && CanModifyDocuments();
    private bool CanMutateFilesPaneNode(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is { IsPlaceholder: false, IsDrive: false } && CanModifyDocuments();
    private bool CanPasteIntoFilesPane(object? parameter) =>
        !string.IsNullOrWhiteSpace(_filesPaneClipboardPath)
        && GetFilesPaneTargetDirectoryPath(parameter) is not null
        && CanModifyDocuments();
    private bool CanShowFilesPaneNodeProperties(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is { IsPlaceholder: false };

    private void OnActiveDocumentChanged()
    {
        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        SaveAllCommand.RaiseCanExecuteChanged();
        CloseTabCommand.RaiseCanExecuteChanged();
        RaiseShellCommandCanExecuteChanged();
        UpdateStatusBarDocumentInfo();
        UpdateTitle();
        UpdateFilesPaneRoot();
        OnPropertyChanged(nameof(SourceText));
        OnPropertyChanged(nameof(SourceBreakpointLines));
        NotifyStoppedRunPresentationChanged();
        LogIdeEvent(
            IdeEventLevel.Info,
            "documents",
            "document.activated",
            commandName: "ActivateDocument",
            document: Documents.ActiveDocument);
    }

    private void UpdateTitle()
    {
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void UpdateFilesPaneRoot()
    {
        var activeDoc = Documents.ActiveDocument;

        if (activeDoc is null || string.IsNullOrWhiteSpace(activeDoc.FilePath))
        {
            // Untitled - используем last-used directory или MyDocuments
            var snapshot = _recentFilesService.GetSnapshot();
            FilesPane.RootPath = IdeWorkspaceDefaults.ResolveRootPath(snapshot.LastDirectoryPath);
        }
        else
        {
            FilesPane.RootPath = Path.GetDirectoryName(activeDoc.FilePath) ?? string.Empty;
        }

        if (FilesPane.Nodes.Count == 0)
        {
            ReloadFilesPane();
        }
    }

    private void OnDocumentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentWorkspaceService.ActiveDocument))
        {
            OnActiveDocumentChanged();
        }
    }

    private void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<DocumentTabViewModel>())
            {
                DetachDocument(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<DocumentTabViewModel>())
            {
                AttachDocument(item);
            }
        }

        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        SaveAllCommand.RaiseCanExecuteChanged();
        CloseTabCommand.RaiseCanExecuteChanged();
        RaiseShellCommandCanExecuteChanged();
    }

    private void AttachDocument(DocumentTabViewModel document)
    {
        document.PropertyChanged += OnDocumentPropertyChanged;
    }

    private void DetachDocument(DocumentTabViewModel document)
    {
        document.PropertyChanged -= OnDocumentPropertyChanged;
        if (ReferenceEquals(_stoppedRunDocument, document))
        {
            SetStoppedRunState(null, null);
        }
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DocumentTabViewModel document || document != Documents.ActiveDocument)
        {
            return;
        }

        if (e.PropertyName is nameof(DocumentTabViewModel.IsDirty)
            or nameof(DocumentTabViewModel.IsReadOnly)
            or nameof(DocumentTabViewModel.DisplayName)
            or nameof(DocumentTabViewModel.FilePath))
        {
            if (e.PropertyName is nameof(DocumentTabViewModel.DisplayName)
                or nameof(DocumentTabViewModel.FilePath))
            {
                UpdateStatusBarDocumentInfo();
            }

            UpdateTitle();
        }

        if (e.PropertyName is nameof(DocumentTabViewModel.DisplayName)
            or nameof(DocumentTabViewModel.FilePath))
        {
            UpdateFilesPaneRoot();
        }

        if (e.PropertyName == nameof(DocumentTabViewModel.SourceText))
        {
            OnPropertyChanged(nameof(SourceText));
        }

        if (e.PropertyName == nameof(DocumentTabViewModel.SourceBreakpointLines))
        {
            OnPropertyChanged(nameof(SourceBreakpointLines));
            ClearAllSourceBreakpointsCommand.RaiseCanExecuteChanged();
        }

        if (e.PropertyName is nameof(DocumentTabViewModel.IsDirty)
            or nameof(DocumentTabViewModel.IsReadOnly))
        {
            SaveCommand.RaiseCanExecuteChanged();
            SaveAsCommand.RaiseCanExecuteChanged();
            SaveAllCommand.RaiseCanExecuteChanged();
        }
    }

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();

        foreach (var filePath in _recentFilesService.GetSnapshot().RecentFiles)
        {
            RecentFiles.Add(RecentFileItem.FromPath(filePath));
        }

        OnPropertyChanged(nameof(HasRecentFiles));
    }

    private void UpdateStatusBarDocumentInfo()
    {
        var activeDocument = Documents.ActiveDocument;

        StatusBar.PathText = activeDocument switch
        {
            null => string.Empty,
            { FilePath: { Length: > 0 } filePath } => filePath,
            _ => activeDocument.DisplayName
        };
    }

    private FileTreeNode? ReloadFilesPane(
        string? preferredSelectionPath = null,
        string? remappedPathOld = null,
        string? remappedPathNew = null,
        bool remapDescendants = false,
        string? removedPath = null,
        bool removeDescendants = false)
    {
        var expandedDirectoryPaths = CollectExpandedDirectoryPaths(FilesPane.Nodes);
        var selectedPath = preferredSelectionPath ?? FilesPane.SelectedNode?.FullPath;

        if (!string.IsNullOrWhiteSpace(remappedPathOld) && !string.IsNullOrWhiteSpace(remappedPathNew))
        {
            expandedDirectoryPaths = RemapTrackedPaths(
                expandedDirectoryPaths,
                remappedPathOld,
                remappedPathNew,
                remapDescendants);
            selectedPath = RemapTrackedPath(selectedPath, remappedPathOld, remappedPathNew, remapDescendants);
        }

        if (!string.IsNullOrWhiteSpace(removedPath))
        {
            RemoveTrackedPaths(expandedDirectoryPaths, removedPath, removeDescendants);

            if (!string.IsNullOrWhiteSpace(selectedPath)
                && IsPathEqualOrDescendant(selectedPath, removedPath, removeDescendants))
            {
                selectedPath = Path.GetDirectoryName(removedPath) ?? FilesPane.RootPath;
            }
        }

        ExpandAncestorDirectoryPaths(expandedDirectoryPaths, selectedPath);
        FilesPane.ReplaceNodes(_fileTreeService.BuildTree(FilesPane.RootPath, FilesPane.ShowHiddenFiles));
        RestoreExpandedDirectoryPaths(FilesPane.Nodes, expandedDirectoryPaths);

        var selectedNode = FindNodeByPath(FilesPane.Nodes, selectedPath);
        FilesPane.SelectedNode = selectedNode;
        return selectedNode;
    }

    private void ShowOutputPane()
    {
        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "ShowOutputPane",
            menuName: "View",
            document: Documents.ActiveDocument,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["pane"] = "Output",
                ["selectedTabIndex"] = BottomPane.SelectedTabIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private void ShowDiagnosticsPane()
    {
        BottomPane.ShowDiagnostics();
        BottomPane.RequestDiagnosticsFocus();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "ShowDiagnosticsPane",
            menuName: "View",
            document: Documents.ActiveDocument,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["pane"] = "Diagnostics",
                ["selectedTabIndex"] = BottomPane.SelectedTabIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private FileTreeNode? GetTargetFilesPaneNode(object? parameter) =>
        parameter as FileTreeNode ?? FilesPane.SelectedNode;

    private string? GetFilesPaneTargetDirectoryPath(object? parameter)
    {
        var node = GetTargetFilesPaneNode(parameter);

        if (node is null)
        {
            return Directory.Exists(FilesPane.RootPath)
                ? FilesPane.RootPath
                : null;
        }

        if (node.IsPlaceholder)
        {
            return null;
        }

        return node.IsDirectory
            ? node.FullPath
            : Path.GetDirectoryName(node.FullPath);
    }

    private static string BuildRunRequestTrace(DocumentTabViewModel? document, string sourceText)
    {
        if (document is null)
        {
            return "document=<none>";
        }

        return $"document={document.DisplayName} | path={document.FilePath ?? "<untitled>"} | dirty={document.IsDirty} | readOnly={document.IsReadOnly} | chars={sourceText.Length} | preview=\"{BuildSourcePreview(sourceText)}\"";
    }

    private void LogIdeEvent(
        IdeEventLevel level,
        string category,
        string eventName,
        string? commandName = null,
        string? menuName = null,
        DocumentTabViewModel? document = null,
        int? line = null,
        int? column = null,
        int? outputLength = null,
        string? textPreview = null,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        FlushEditorActivityEvents();

        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = level,
            Category = category,
            EventName = eventName,
            Source = nameof(MainWindowViewModel),
            CommandName = commandName,
            MenuName = menuName,
            DocumentPath = document?.FilePath,
            DocumentName = document?.DisplayName,
            Line = line,
            Column = column,
            OutputLength = outputLength,
            TextLength = textPreview?.Length,
            TextPreview = textPreview,
            StatusBar = BuildStatusBarSnapshot(),
            Properties = properties
        });
    }

    private void RecordEditorNoise(
        EditorNoiseBurst burst,
        DocumentTabViewModel? document,
        IReadOnlyDictionary<string, string?> properties)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_editorNoiseSync)
        {
            if (burst.HasPending && !burst.CanMerge(document))
            {
                FlushEditorNoiseBurst(burst);
            }

            burst.Append(document, now, properties);
            if (burst.ShouldFlush(now))
            {
                FlushEditorNoiseBurst(burst);
            }
        }
    }

    private void ApplyConsoleInputStateToBottomPane()
    {
        var useBottomPaneInput = _programConsoleService.IsWaitingForInput && !_programConsoleService.IsScreenOutputActive;
        BottomPane.IsWaitingForInput = useBottomPaneInput;
        if (!useBottomPaneInput)
        {
            BottomPane.CurrentPromptText = string.Empty;
            BottomPane.CurrentInputText = string.Empty;
        }
    }

    private void FlushEditorNoiseBurst(EditorNoiseBurst burst)
    {
        if (!burst.HasPending)
        {
            return;
        }

        var durationMs = Math.Max(
            0,
            (long)Math.Round(
                (burst.LastTimestampUtc - burst.FirstTimestampUtc).TotalMilliseconds,
                MidpointRounding.AwayFromZero));

        var eventProperties = new Dictionary<string, string?>(burst.Properties, StringComparer.Ordinal)
        {
            ["eventsCount"] = burst.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["firstTimestampUtc"] = burst.FirstTimestampUtc.ToString("O"),
            ["lastTimestampUtc"] = burst.LastTimestampUtc.ToString("O"),
            ["durationMs"] = durationMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Trace,
            Category = "editor",
            EventName = burst.EventName,
            Source = nameof(MainWindowViewModel),
            CommandName = burst.CommandName,
            DocumentPath = burst.DocumentPath,
            DocumentName = burst.DocumentName,
            StatusBar = BuildStatusBarSnapshot(),
            Properties = eventProperties
        });

        burst.Reset();
    }

    private IdeEventStatusBarSnapshot BuildStatusBarSnapshot()
    {
        return new IdeEventStatusBarSnapshot
        {
            StatusText = StatusBar.StatusText,
            LineColumnText = StatusBar.LineColumnText,
            PathText = StatusBar.PathText,
            CompilationTimingText = StatusBar.CompilationTimingText,
            PreparationTimingText = StatusBar.PreparationTimingText,
            ExecutionTimingText = StatusBar.ExecutionTimingText,
            TotalTimingText = StatusBar.TotalTimingText,
            CacheTimingText = StatusBar.CacheTimingText,
            EngineTimingText = StatusBar.EngineTimingText,
            IsRunning = StatusBar.IsRunning,
            IsVisible = StatusBar.IsVisible,
            IsCompilationProgressVisible = StatusBar.IsCompilationProgressVisible,
            CompilationProgressPercent = StatusBar.CompilationProgressPercent
        };
    }

    private static string BuildDocumentTrace(DocumentTabViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return $"document={document.DisplayName} | path={document.FilePath ?? "<untitled>"} | dirty={document.IsDirty} | readOnly={document.IsReadOnly}";
    }

    private static string BuildRunResultTrace(ProgramRunResult result)
    {
        var diagnosticsPreview = result.Diagnostics.Count == 0
            ? "none"
            : string.Join(
                " || ",
                result.Diagnostics.Take(3).Select(static diagnostic =>
                    $"{diagnostic.Code}@{diagnostic.Line}:{diagnostic.Column} {diagnostic.Message}"));

        return $"status={result.Status} | requestedMode={result.RequestedExecutionMode} | actualEngine={result.ActualEngine?.ToString() ?? "none"} | stopped={result.Stopped} | diagnostics={result.Diagnostics.Count} | outputChars={result.OutputText.Length} | preparationMs={result.PreparationDuration.TotalMilliseconds:F3} | loweringMs={result.LoweringDuration.TotalMilliseconds:F3} | bytecodeMs={result.BytecodeEmissionDuration.TotalMilliseconds:F3} | executionMs={result.ExecutionDuration.TotalMilliseconds:F3} | totalMs={result.TotalDuration.TotalMilliseconds:F3} | compiledCache={(result.UsedCompiledCache ? "hit" : "miss")} | bytecodeCache={(result.UsedBytecodeCache ? "hit" : "miss")} | output=\"{BuildSourcePreview(result.OutputText)}\" | diagnosticsPreview=\"{BuildSourcePreview(diagnosticsPreview)}\"";
    }

    private string FormatStoppedStatus(ProgramRunResult result)
    {
        var line = result.StoppedAt?.Line;

        return line is > 0
            ? GetLocalizedFormattedString("Run.StatusStoppedAtBreakpoint", "Run stopped at line {0}.", line.Value)
            : GetLocalizedString("Run.StatusStoppedAtBreakpointUnknown", "Run stopped at breakpoint.");
    }

    private void UpdateRunTiming(ProgramRunResult result)
    {
        var cacheText = result.UsedCompiledCache
            ? GetLocalizedString("Run.Metrics.CacheHit", "yes")
            : GetLocalizedString("Run.Metrics.CacheMiss", "no");
        var engineText = FormatExecutionEngine(result.ActualEngine);

        StatusBar.ShowRunTiming(
            GetLocalizedFormattedString(
                "Run.Metrics.Preparation",
                "Preparation: {0} ms",
                FormatMilliseconds(result.PreparationDuration)),
            GetLocalizedFormattedString(
                "Run.Metrics.Execution",
                "Execution: {0} ms",
                FormatMilliseconds(result.ExecutionDuration)),
            GetLocalizedFormattedString(
                "Run.Metrics.Total",
                "Total time: {0} ms",
                FormatMilliseconds(result.TotalDuration)),
            GetLocalizedFormattedString(
                "Run.Metrics.Cache",
                "Cache used: {0}",
                cacheText),
            GetLocalizedFormattedString(
                "Run.Metrics.Engine",
                "Engine: {0}",
                engineText));
    }

    private string FormatExecutionEngine(Axiom.Execution.ExecutionEngineKind? engine)
    {
        return engine switch
        {
            Axiom.Execution.ExecutionEngineKind.Interpreter => GetLocalizedString("Run.Engine.Interpreter", "interpreter"),
            Axiom.Execution.ExecutionEngineKind.BytecodeVm => GetLocalizedString("Run.Engine.BytecodeVm", "bytecode VM"),
            _ => GetLocalizedString("Run.Engine.None", "none")
        };
    }

    private string BuildCompilationTimingText(TimeSpan elapsed)
    {
        return GetLocalizedFormattedString(
            "Compile.Metrics.Elapsed",
            "Compilation time: {0} ms",
            FormatMilliseconds(elapsed));
    }

    private static long FormatMilliseconds(TimeSpan value)
    {
        return (long)Math.Round(value.TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    private static string BuildSourcePreview(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= 220
            ? normalized
            : normalized[..220];
    }

    private static Task<TResult> ExecuteRunOperationAsync<TResult>(Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return System.Windows.Application.Current is null
            ? Task.FromResult(operation())
            : Task.Run(operation);
    }

    private sealed class EditorNoiseBurst
    {
        private static readonly TimeSpan MaxWindow = TimeSpan.FromMilliseconds(900);

        public EditorNoiseBurst(string eventName, string commandName)
        {
            EventName = eventName;
            CommandName = commandName;
        }

        public string EventName { get; }

        public string CommandName { get; }

        public bool HasPending => Count > 0;

        public int Count { get; private set; }

        public string? DocumentPath { get; private set; }

        public string? DocumentName { get; private set; }

        public DateTimeOffset FirstTimestampUtc { get; private set; }

        public DateTimeOffset LastTimestampUtc { get; private set; }

        public Dictionary<string, string?> Properties { get; } = new(StringComparer.Ordinal);

        public bool CanMerge(DocumentTabViewModel? document)
        {
            var documentPath = document?.FilePath;
            return string.Equals(DocumentPath, documentPath, StringComparison.OrdinalIgnoreCase);
        }

        public void Append(
            DocumentTabViewModel? document,
            DateTimeOffset timestampUtc,
            IReadOnlyDictionary<string, string?> properties)
        {
            if (Count == 0)
            {
                FirstTimestampUtc = timestampUtc;
                DocumentPath = document?.FilePath;
                DocumentName = document?.DisplayName;
            }

            Count++;
            LastTimestampUtc = timestampUtc;
            DocumentPath = document?.FilePath;
            DocumentName = document?.DisplayName;

            foreach (var pair in properties)
            {
                Properties[pair.Key] = pair.Value;
            }
        }

        public bool ShouldFlush(DateTimeOffset nowUtc)
        {
            if (Count >= 20)
            {
                return true;
            }

            return Count > 0 && nowUtc - FirstTimestampUtc >= MaxWindow;
        }

        public void Reset()
        {
            Count = 0;
            DocumentPath = null;
            DocumentName = null;
            FirstTimestampUtc = default;
            LastTimestampUtc = default;
            Properties.Clear();
        }
    }

    internal static string? ResolveManualPath()
    {
        return ResolveRepositoryFile(@"docs\manual\index.html");
    }

    internal static string? ResolveRepositoryFile(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var examinedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var directory = new DirectoryInfo(Path.GetFullPath(root));

            while (directory is not null && examinedRoots.Add(directory.FullName))
            {
                var candidatePath = Path.Combine(directory.FullName, relativePath);

                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
