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
/// Root shell ViewModel РЅРѕРІРѕРіРѕ РґРёР·Р°Р№РЅР° СЃ РєРѕРјРїРѕР·РёС†РёРµР№ child viewmodels.
/// РџРѕРєР° СЃРѕС…СЂР°РЅСЏРµС‚ РѕР±СЂР°С‚РЅСѓСЋ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚СЊ СЃРѕ СЃС‚Р°СЂС‹Рј UI.
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
    private Axiom.Runtime.BytecodeVmStopState? _stoppedRunState;
    private DocumentTabViewModel? _stoppedRunDocument;
    private int? _stoppedRunLine;
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
        StatusBar = new StatusBarViewModel();
        BottomPane = new BottomPaneViewModel();
        FilesPane = new FilesPaneViewModel();
        RecentFiles = [];
        _preferredWindowState = IdeWindowState.Normal;

        // РЎРѕР·РґР°С‘Рј СЃС‚Р°СЂС‚РѕРІСѓСЋ РїСѓСЃС‚СѓСЋ РІРєР»Р°РґРєСѓ
        Documents.CreateNewDocument();
        RestoreShellPreferences();
        RefreshRecentFiles();
        UpdateFilesPaneRoot();

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° РёР·РјРµРЅРµРЅРёСЏ active document РґР»СЏ СЃС‚Р°С‚СѓСЃР°
        Documents.PropertyChanged += OnDocumentsPropertyChanged;
        Documents.Documents.CollectionChanged += OnDocumentsCollectionChanged;
        FilesPane.PropertyChanged += OnFilesPanePropertyChanged;
        BottomPane.PropertyChanged += OnBottomPanePropertyChanged;
        StatusBar.PropertyChanged += OnStatusBarPropertyChanged;

        foreach (var document in Documents.Documents)
        {
            AttachDocument(document);
        }

        // РџРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° РёР·РјРµРЅРµРЅРёСЏ console output РґР»СЏ bottom pane
        _programConsoleService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProgramConsoleService.OutputText))
            {
                BottomPane.OutputText = _programConsoleService.OutputText;
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.IsWaitingForInput))
            {
                BottomPane.IsWaitingForInput = _programConsoleService.IsWaitingForInput;
                if (_programConsoleService.IsWaitingForInput)
                {
                    BottomPane.SelectOutput();
                    BottomPane.RequestOutputFocus();
                }

                RaiseInputCommandCanExecuteChanged();
                RaiseShellCommandCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.CurrentPromptText))
            {
                BottomPane.CurrentPromptText = _programConsoleService.CurrentPromptText;
            }
            else if (e.PropertyName == nameof(ProgramConsoleService.CurrentInputText))
            {
                BottomPane.CurrentInputText = _programConsoleService.CurrentInputText;
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
        RunCommand = new RelayCommand(RunProgram, CanRunProgram);
        ContinueRunCommand = new RelayCommand(ContinueProgram, CanContinueProgram);
        StepOverCommand = new RelayCommand(StepOverProgram, CanStepOverProgram);
        StepIntoCommand = new RelayCommand(StepIntoProgram, CanStepIntoProgram);
        StepOutCommand = new RelayCommand(StepOutProgram, CanStepOutProgram);
        StepBackCommand = new RelayCommand(StepBackProgram, CanStepBackProgram);
        RunOrContinueCommand = new RelayCommand(RunOrContinueProgram, CanRunOrContinueProgram);
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

        // РџРѕРґРїРёСЃРєР° РЅР° РёР·РјРµРЅРµРЅРёСЏ РґР»СЏ CanExecute
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

    public Axiom.Runtime.BytecodeVmStopState? StoppedRunState =>
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

    // Legacy properties РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё СЃРѕ СЃС‚Р°СЂС‹Рј UI
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

    private bool CanUseIdeEventLogCommands() =>
        !IsShellInteractionLocked() && !string.IsNullOrWhiteSpace(_eventLogService.LogsDirectoryPath);

    private void ShowIdeEventLogViewer()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var logPath = _eventLogService.CurrentSessionLogPath;
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusCurrentMissing", "Current IDE event log is not available.");
                return;
            }

            _eventLogViewerService.Show(logPath);
            StatusBar.StatusText = GetLocalizedString("EventLog.Viewer.StatusOpened", "IDE event log viewer opened.");
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.viewer-opened",
                commandName: "ShowIdeEventLogViewer",
                textPreview: logPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.Viewer.StatusOpenFailed", "Could not open IDE event log viewer.");
        }
    }

    private void OpenIdeEventLogFile()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var logPath = _eventLogService.CurrentSessionLogPath;
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusCurrentMissing", "Current IDE event log is not available.");
                return;
            }

            _externalNavigationService.OpenPath(logPath);
            StatusBar.StatusText = GetLocalizedFormattedString("EventLog.StatusFileOpened", "IDE event log opened: {0}.", logPath);
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.file-opened",
                commandName: "OpenIdeEventLogFile",
                textPreview: logPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.StatusOpenFailed", "Could not open IDE event log.");
        }
    }

    private void OpenIdeEventLogFolder()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var directoryPath = _eventLogService.LogsDirectoryPath;
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusFolderMissing", "IDE event log folder is not available.");
                return;
            }

            Directory.CreateDirectory(directoryPath);
            _externalNavigationService.OpenPath(directoryPath);
            StatusBar.StatusText = GetLocalizedFormattedString("EventLog.StatusFolderOpened", "IDE event log folder opened: {0}.", directoryPath);
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.folder-opened",
                commandName: "OpenIdeEventLogFolder",
                textPreview: directoryPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.StatusOpenFolderFailed", "Could not open IDE event log folder.");
        }
    }

    private void ClearIdeEventLogs()
    {
        var title = GetLocalizedString("EventLog.Clear.Title", "Clear IDE Event Logs");
        var confirmed = _promptService.ShowConfirmation(
            GetLocalizedString(
                "EventLog.Clear.Confirm",
                "Delete inactive IDE event log files? The current session log will be kept."),
            title);

        if (!confirmed)
        {
            StatusBar.StatusText = GetLocalizedString("EventLog.Clear.Cancelled", "IDE event log cleanup cancelled.");
            return;
        }

        var deleted = _eventLogService.DeleteInactiveLogs();
        StatusBar.StatusText = GetLocalizedFormattedString("EventLog.Clear.Completed", "Deleted IDE event logs: {0}.", deleted);
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "event-log.cleaned",
            commandName: "ClearIdeEventLogs",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["deleted"] = deleted.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private void RunProgram()
    {
        RunProgram(useSourceBreakpoints: false);
    }

    private void RunProgramWithDebugging()
    {
        RunProgram(useSourceBreakpoints: true);
    }

    private void RunProgram(bool useSourceBreakpoints)
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoActiveDocument", "No active document to run.");
            return;
        }

        if (!TryPrepareRunDocument(Documents.ActiveDocument, out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString("Run.StatusRunning", "Running...");
        StatusBar.ClearTimingText();
        SetStoppedRunState(null, null);
        RaiseShellCommandCanExecuteChanged();

        // Auto-switch to output
        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();
        BottomPane.ClearOutput();

        _traceService.Write(
            "run.request",
            BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
        var runDocument = Documents.ActiveDocument;
        var sourceBreakpointLines = useSourceBreakpoints
            ? SourceBreakpointLines
            : Array.Empty<int>();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: useSourceBreakpoints ? "RunDebug" : "Run",
            document: Documents.ActiveDocument,
            textPreview: documentContent.SourceText,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = SelectedExecutionMode.ToString(),
                ["useSourceBreakpoints"] = useSourceBreakpoints.ToString(),
                ["breakpoints"] = sourceBreakpointLines.Count().ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

        var result = _programRunService.Run(documentContent.SourceText, SelectedExecutionMode, sourceBreakpointLines);

        ApplyRunOutputFromResult(result, "run.output-sync");

        BottomPane.ReplaceDiagnostics(
            result.Diagnostics.Select(d => d));

        _traceService.Write(
            "run.result",
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            result.Stopped ? "debugger.stopped" : "debugger.run-finished",
            commandName: useSourceBreakpoints ? "RunDebug" : "Run",
            document: runDocument,
            line: result.StoppedAt?.SourceLocation.Line,
            column: result.StoppedAt?.SourceLocation.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["status"] = result.Status.ToString(),
                ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["canStepBack"] = (result.CanStepBack).ToString()
            });

        SetStoppedRunState(useSourceBreakpoints ? result.StopState : null, useSourceBreakpoints ? runDocument : null);
        UpdateRunTiming(result);

        if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void RunOrContinueProgram()
    {
        if (HasStoppedRun)
        {
            ContinueProgram();
            return;
        }

        RunProgramWithDebugging();
    }

    private void ContinueProgram()
    {
        ResumeStoppedProgram(
            _programRunService.Continue,
            "Run.StatusContinuing",
            "Continuing...",
            "run.continue.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "Continue", document: Documents.ActiveDocument);
    }

    private void StepOverProgram()
    {
        if (!HasStoppedRun)
        {
            StartSteppingProgram("Run.StatusSteppingOver", "Stepping over...", "run.step-over.start");
            LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOver.Start", document: Documents.ActiveDocument);
            return;
        }

        ResumeStoppedProgram(
            _programRunService.StepOver,
            "Run.StatusSteppingOver",
            "Stepping over...",
            "run.step-over.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOver", document: Documents.ActiveDocument);
    }

    private void StepIntoProgram()
    {
        if (!HasStoppedRun)
        {
            StartSteppingProgram("Run.StatusSteppingInto", "Stepping into...", "run.step-into.start");
            LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepInto.Start", document: Documents.ActiveDocument);
            return;
        }

        ResumeStoppedProgram(
            _programRunService.StepInto,
            "Run.StatusSteppingInto",
            "Stepping into...",
            "run.step-into.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepInto", document: Documents.ActiveDocument);
    }

    private void StepOutProgram()
    {
        ResumeStoppedProgram(
            _programRunService.StepOut,
            "Run.StatusSteppingOut",
            "Stepping out...",
            "run.step-out.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOut", document: Documents.ActiveDocument);
    }

    private void StepBackProgram()
    {
        ResumeStoppedProgram(
            _programRunService.StepBack,
            "Run.StatusSteppingBack",
            "Stepping back...",
            "run.step-back.result",
            static state => state.CanStepBack);
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepBack", document: Documents.ActiveDocument);
    }

    private void StartSteppingProgram(
        string runningStatusKey,
        string runningStatusFallback,
        string resultTraceName)
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoActiveDocument", "No active document to run.");
            return;
        }

        if (!TryPrepareRunDocument(Documents.ActiveDocument, out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString(runningStatusKey, runningStatusFallback);
        StatusBar.ClearTimingText();
        SetStoppedRunState(null, null);
        RaiseShellCommandCanExecuteChanged();

        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();
        BottomPane.ClearOutput();

        _traceService.Write(
            "run.step-start.request",
            BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "StepStart",
            document: Documents.ActiveDocument,
            textPreview: documentContent.SourceText,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = ExecutionMode.Bytecode.ToString()
            });

        var runDocument = Documents.ActiveDocument;
        var result = _programRunService.Run(
            documentContent.SourceText,
            ExecutionMode.Bytecode,
            SourceBreakpointLines,
            stopBeforeFirstInstruction: true);

        ApplyRunOutputFromResult(result, "run.output-sync");

        BottomPane.ReplaceDiagnostics(
            result.Diagnostics.Select(d => d));

        _traceService.Write(
            resultTraceName,
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            "debugger.step-start-result",
            commandName: "StepStart",
            document: runDocument,
            line: result.StoppedAt?.SourceLocation.Line,
            column: result.StoppedAt?.SourceLocation.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["trace"] = resultTraceName,
                ["status"] = result.Status.ToString(),
                ["stopped"] = result.Stopped.ToString()
            });

        SetStoppedRunState(result.StopState, runDocument);
        UpdateRunTiming(result);

        if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void ResumeStoppedProgram(
        Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult> resume,
        string runningStatusKey,
        string runningStatusFallback,
        string resultTraceName,
        Func<Axiom.Runtime.BytecodeVmStopState, bool>? canResume = null)
    {
        canResume ??= static state => state.CanResume;

        if (!HasStoppedRun || _stoppedRunState is null || !canResume(_stoppedRunState))
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRun", "No stopped program to continue.");
            return;
        }

        var stoppedRunDocument = _stoppedRunDocument;
        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString(runningStatusKey, runningStatusFallback);
        StatusBar.ClearTimingText();
        RaiseShellCommandCanExecuteChanged();

        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();

        var result = resume(_stoppedRunState);

        ApplyRunOutputFromResult(result, "run.continue.output-sync");

        BottomPane.ReplaceDiagnostics(result.Diagnostics.Select(d => d));

        _traceService.Write(
            resultTraceName,
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            "debugger.resume-result",
            commandName: resultTraceName,
            document: stoppedRunDocument,
            line: result.StoppedAt?.SourceLocation.Line,
            column: result.StoppedAt?.SourceLocation.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["status"] = result.Status.ToString(),
                ["stopped"] = result.Stopped.ToString(),
                ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["canStepBack"] = result.CanStepBack.ToString()
            });

        SetStoppedRunState(result.StopState, stoppedRunDocument);
        UpdateRunTiming(result);

        if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void StopDebugging()
    {
        if (!HasStoppedRun)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRunToStop", "No stopped program to stop.");
            return;
        }

        SetStoppedRunState(null, null);
        StatusBar.IsRunning = false;
        StatusBar.StatusText = GetLocalizedString("Run.StatusDebuggingStopped", "Debugging stopped.");
        LogIdeEvent(IdeEventLevel.Info, "debugger", "debugger.stopped-by-user", commandName: "StopDebugging", document: Documents.ActiveDocument);
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void RestartDebugging()
    {
        if (!HasStoppedRun)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRunToRestart", "No stopped program to restart.");
            return;
        }

        SetStoppedRunState(null, null);
        StatusBar.StatusText = GetLocalizedString("Run.StatusRestartingDebugging", "Restarting debugging...");
        LogIdeEvent(IdeEventLevel.Info, "debugger", "debugger.restarting", commandName: "RestartDebugging", document: Documents.ActiveDocument);
        RunProgramWithDebugging();
    }

    private async Task CompileProgramAsync()
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Compile.StatusNoActiveDocument", "No active document to compile.");
            return;
        }

        if (!TryPrepareExecutionDocument(
                Documents.ActiveDocument,
                "Compile",
                "Compile",
                "The document must be saved before compilation. Save it now?",
                "The document has unsaved changes. Save before compilation?",
                "Compilation cancelled. Save the document before compilation.",
                "Compilation cancelled.",
                "Compilation failed: {0}",
                out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.ShowCompilationProgress();
        StatusBar.ClearTimingText();
        RaiseShellCommandCanExecuteChanged();
        await Task.Yield();

        var compilationStopwatch = Stopwatch.StartNew();
        var progress = new Progress<int>(percent => StatusBar.CompilationProgressPercent = percent);
        var compilationTask = Task.Run(() => _programRunService.Compile(documentContent.SourceText, progress));

        try
        {
            _traceService.Write(
                "compile.request",
                BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "command.executed",
                commandName: "Compile",
                document: Documents.ActiveDocument,
                textPreview: documentContent.SourceText,
                properties: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["executionMode"] = SelectedExecutionMode.ToString()
                });

            while (!compilationTask.IsCompleted)
            {
                StatusBar.ShowCompilationTiming(BuildCompilationTimingText(compilationStopwatch.Elapsed));
                await Task.Delay(50);
            }

            var result = await compilationTask;
            compilationStopwatch.Stop();
            StatusBar.ShowCompilationTiming(BuildCompilationTimingText(compilationStopwatch.Elapsed));
            BottomPane.ReplaceDiagnostics(result.Diagnostics);

            _traceService.Write(
                "compile.result",
                $"status={result.Status} | diagnostics={result.Diagnostics.Count}");
            LogIdeEvent(
                !result.Succeeded || result.Diagnostics.Count > 0 ? IdeEventLevel.Warning : IdeEventLevel.Info,
                "commands",
                "compile.result",
                commandName: "Compile",
                document: Documents.ActiveDocument,
                properties: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["status"] = result.Status.ToString(),
                    ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

            if (!result.Succeeded || result.Diagnostics.Count > 0)
            {
                BottomPane.ShowDiagnostics();
                BottomPane.RequestDiagnosticsFocus();
                StatusBar.StatusText = GetLocalizedFormattedString("Compile.StatusStopped", "Compilation stopped: {0} diagnostics.", result.Diagnostics.Count);
            }
            else
            {
                StatusBar.StatusText = GetLocalizedString("Compile.StatusSucceeded", "Compilation succeeded.");
            }
        }
        finally
        {
            StatusBar.HideCompilationProgress();
            StatusBar.IsRunning = false;
            RaiseShellCommandCanExecuteChanged();
            UpdateTitle();
        }
    }

    private bool TryPrepareRunDocument(DocumentTabViewModel document, out DocumentFileContent documentContent)
    {
        return TryPrepareExecutionDocument(
            document,
            "Run",
            "Run",
            "The document must be saved before running. Save it now?",
            "The document has unsaved changes. Save before running?",
            "Run cancelled. Save the document before running.",
            "Run cancelled.",
            "Run failed: {0}",
            out documentContent);
    }

    public void SetSourceBreakpointLines(IEnumerable<int> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (Documents.ActiveDocument is not { } document)
        {
            return;
        }

        document.SetSourceBreakpointLines(lines);
    }

    public void ClearSourceBreakpointLines()
    {
        SetSourceBreakpointLines([]);
    }

    private void ClearAllSourceBreakpoints()
    {
        if (Documents.ActiveDocument is null)
        {
            return;
        }

        var count = SourceBreakpointLines.Count;
        var hadStoppedRun = HasStoppedRun;
        ClearSourceBreakpointLines();

        if (hadStoppedRun)
        {
            SetStoppedRunState(null, null);
            StatusBar.IsRunning = false;
        }

        if (hadStoppedRun && count > 0)
        {
            StatusBar.StatusText = GetLocalizedFormattedString(
                "Run.Breakpoints.ClearedAllAndStoppedDebugging",
                "Cleared {0} breakpoints. Debugging stopped.",
                count);
        }
        else
        {
            StatusBar.StatusText = count > 0
                ? GetLocalizedFormattedString("Run.Breakpoints.ClearedAll", "Cleared {0} breakpoints.", count)
                : GetLocalizedString("Run.Breakpoints.NoBreakpointsToClear", "No breakpoints to clear.");
        }
    }

    public void ToggleSourceBreakpointLine(int line)
    {
        if (line <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "Source breakpoint line must be greater than zero.");
        }

        if (Documents.ActiveDocument is null)
        {
            return;
        }

        Documents.ActiveDocument.ToggleSourceBreakpointLine(line);
    }

    private void ToggleCurrentSourceBreakpoint(object? parameter)
    {
        if (!TryGetPositiveLine(parameter, out var line))
        {
            StatusBar.StatusText = GetLocalizedString("Run.Breakpoints.InvalidLine", "Cannot toggle breakpoint for the current line.");
            return;
        }

        var hadBreakpoint = SourceBreakpointLines.Contains(line);
        ToggleSourceBreakpointLine(line);

        StatusBar.StatusText = hadBreakpoint
            ? GetLocalizedFormattedString("Run.Breakpoints.Removed", "Breakpoint removed from line {0}.", line)
            : GetLocalizedFormattedString("Run.Breakpoints.Added", "Breakpoint added to line {0}.", line);
    }

    private bool CanToggleCurrentSourceBreakpoint(object? parameter) =>
        !IsShellInteractionLocked() && TryGetPositiveLine(parameter, out _);

    private bool CanClearAllSourceBreakpoints() =>
        Documents.ActiveDocument is not null
        && SourceBreakpointLines.Count > 0
        && !IsShellInteractionLocked();

    private bool IsStoppedRunForActiveDocument =>
        _stoppedRunDocument is not null
        && ReferenceEquals(_stoppedRunDocument, Documents.ActiveDocument);

    private void SetStoppedRunState(
        Axiom.Runtime.BytecodeVmStopState? stopState,
        DocumentTabViewModel? document)
    {
        var normalizedDocument = stopState is null ? null : document;
        if (ReferenceEquals(_stoppedRunState, stopState)
            && ReferenceEquals(_stoppedRunDocument, normalizedDocument))
        {
            return;
        }

        _stoppedRunState = stopState;
        _stoppedRunDocument = normalizedDocument;
        SetStoppedRunLine(stopState?.Snapshot.SourceLocation.Line);
        TraceStoppedRunState(stopState, normalizedDocument);
        NotifyStoppedRunPresentationChanged();
    }

    private void ApplyRunOutputFromResult(ProgramRunResult result, string traceName)
    {
        if (BottomPane.IsWaitingForInput
            || string.Equals(BottomPane.OutputText, result.OutputText, StringComparison.Ordinal))
        {
            return;
        }

        BottomPane.OutputText = result.OutputText;
        _traceService.Write(
            traceName,
            $"Applied output from ProgramRunResult. outputChars={result.OutputText.Length} | output=\"{BuildSourcePreview(result.OutputText)}\"");
    }

    private void TraceStoppedRunState(
        Axiom.Runtime.BytecodeVmStopState? stopState,
        DocumentTabViewModel? document)
    {
        if (stopState is null)
        {
            _traceService.Write(
                "run.stop-state",
                $"stopped=false | document={document?.DisplayName ?? "<none>"} | outputChars={BottomPane.OutputText.Length} | output=\"{BuildSourcePreview(BottomPane.OutputText)}\"");
            LogIdeEvent(
                IdeEventLevel.Info,
                "debugger",
                "debugger.stop-state-cleared",
                commandName: "StopState",
                document: document,
                outputLength: BottomPane.OutputText.Length,
                textPreview: BottomPane.OutputText);
            return;
        }

        var snapshot = stopState.Snapshot;
        _traceService.Write(
            "run.stop-state",
            $"stopped=true | line={snapshot.SourceLocation.Line} | column={snapshot.SourceLocation.Column} | opcode={snapshot.Opcode} | callDepth={snapshot.CallDepth} | canResume={stopState.CanResume} | canStepBack={stopState.CanStepBack} | document={document?.DisplayName ?? "<none>"} | outputChars={BottomPane.OutputText.Length} | output=\"{BuildSourcePreview(BottomPane.OutputText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "debugger",
            EventName = "debugger.stop-state-changed",
            Source = nameof(MainWindowViewModel),
            CommandName = "StopState",
            DocumentPath = document?.FilePath,
            DocumentName = document?.DisplayName,
            Line = snapshot.SourceLocation.Line,
            Column = snapshot.SourceLocation.Column,
            DebugLine = snapshot.SourceLocation.Line,
            Opcode = snapshot.Opcode.ToString(),
            CallDepth = snapshot.CallDepth,
            CanStepBack = stopState.CanStepBack,
            OutputLength = BottomPane.OutputText.Length,
            TextLength = BottomPane.OutputText.Length,
            TextPreview = BottomPane.OutputText,
            StatusBar = BuildStatusBarSnapshot(),
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["canResume"] = stopState.CanResume.ToString()
            }
        });
    }

    private void SetStoppedRunLine(int? line)
    {
        var normalizedLine = line is > 0 ? line : null;
        if (_stoppedRunLine == normalizedLine)
        {
            return;
        }

        _stoppedRunLine = normalizedLine;
        OnPropertyChanged(nameof(StoppedRunLine));
    }

    private void NotifyStoppedRunPresentationChanged()
    {
        OnPropertyChanged(nameof(StoppedRunState));
        OnPropertyChanged(nameof(HasStoppedRun));
        OnPropertyChanged(nameof(StoppedRunLine));
        ContinueRunCommand.RaiseCanExecuteChanged();
        StepOverCommand.RaiseCanExecuteChanged();
        StepIntoCommand.RaiseCanExecuteChanged();
        StepOutCommand.RaiseCanExecuteChanged();
        StepBackCommand.RaiseCanExecuteChanged();
        RunOrContinueCommand.RaiseCanExecuteChanged();
        StopDebuggingCommand.RaiseCanExecuteChanged();
        RestartDebuggingCommand.RaiseCanExecuteChanged();
        ShowIdeEventLogViewerCommand.RaiseCanExecuteChanged();
        OpenIdeEventLogFileCommand.RaiseCanExecuteChanged();
        OpenIdeEventLogFolderCommand.RaiseCanExecuteChanged();
        ClearIdeEventLogsCommand.RaiseCanExecuteChanged();
        ClearAllSourceBreakpointsCommand.RaiseCanExecuteChanged();
    }

    private static bool TryGetPositiveLine(object? value, out int line)
    {
        switch (value)
        {
            case int intValue:
                line = intValue;
                return line > 0;
            case string text when int.TryParse(text, out var parsedValue):
                line = parsedValue;
                return line > 0;
            default:
                line = 0;
                return false;
        }
    }

    private bool TryPrepareExecutionDocument(
        DocumentTabViewModel document,
        string actionKeyPrefix,
        string actionTitleFallback,
        string savePromptUntitledFallback,
        string savePromptDirtyFallback,
        string cancelledNeedSaveFallback,
        string cancelledFallback,
        string loadFailedFallback,
        out DocumentFileContent documentContent)
    {
        ArgumentNullException.ThrowIfNull(document);

        documentContent = default!;

        if (document.IsUntitled || document.IsDirty)
        {
            var promptMessage = document.IsUntitled
                ? GetLocalizedString($"{actionKeyPrefix}.SavePromptUntitled", savePromptUntitledFallback)
                : GetLocalizedString($"{actionKeyPrefix}.SavePromptDirty", savePromptDirtyFallback);

            if (!_promptService.ShowConfirmation(promptMessage, GetLocalizedString($"{actionKeyPrefix}.Title", actionTitleFallback)))
            {
                StatusBar.StatusText = GetLocalizedString($"{actionKeyPrefix}.StatusCancelledNeedSave", cancelledNeedSaveFallback);
                _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"User declined save-before-{actionKeyPrefix.ToLowerInvariant()} prompt for '{document.DisplayName}'.");
                return false;
            }

            if (!Documents.SaveDocument(document, out var saveStatus))
            {
                StatusBar.StatusText = saveStatus ?? GetLocalizedString($"{actionKeyPrefix}.StatusCancelled", cancelledFallback);
                _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"Save-before-{actionKeyPrefix.ToLowerInvariant()} failed for '{document.DisplayName}': {saveStatus ?? "<none>"}");
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            StatusBar.StatusText = GetLocalizedString($"{actionKeyPrefix}.StatusCancelledNeedSave", cancelledNeedSaveFallback);
            _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"Document '{document.DisplayName}' has no file path for {actionKeyPrefix.ToLowerInvariant()}.");
            return false;
        }

        try
        {
            documentContent = _documentFileService.LoadDocument(document.FilePath);
            return true;
        }
        catch (Exception exception)
        {
            var message = GetLocalizedFormattedString($"{actionKeyPrefix}.LoadFailed", loadFailedFallback, exception.Message);
            BottomPane.ReplaceDiagnostics(
                [ProgramDiagnosticItem.CreateInfrastructure("IDE0002", message)]);
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = message;
            _traceService.WriteException($"{actionKeyPrefix.ToLowerInvariant()}.load-failed", exception, $"Could not load '{document.FilePath}' for {actionKeyPrefix.ToLowerInvariant()}.");
            return false;
        }
    }

    private void SubmitInput()
    {
        _programConsoleService.SubmitInput();
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

    private void CloseFilesPane()
    {
        FilesPane.IsVisible = false;
    }

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

    private void OpenFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsDirectory || node.IsPlaceholder)
        {
            return;
        }

        OpenFilesPaneNodeInEditor(node);
        LogIdeEvent(
            IdeEventLevel.Info,
            "files",
            "file.opened-from-tree",
            commandName: "OpenFromFileManager",
            document: Documents.ActiveDocument,
            textPreview: node.FullPath);
    }

    private void NavigateFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        if (!node.IsDirectory)
        {
            OpenFilesPaneNodeInEditor(node);
            return;
        }

        FilesPane.RootPath = node.FullPath;
        ReloadFilesPane(preferredSelectionPath: node.FullPath);
        FilesPane.IsVisible = true;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Navigate.Status", "Navigated to {0}.", node.FullPath);
    }

    private void OpenFilesPaneNodeExternally(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        try
        {
            _externalNavigationService.OpenPath(node.FullPath);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.External.StatusOpened", "Opened externally: {0}.", node.DisplayName);
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.opened-externally",
                commandName: "OpenExternal",
                document: Documents.ActiveDocument,
                textPreview: node.FullPath);
        }
        catch (Exception ex)
        {
            var message = GetLocalizedFormattedString(
                "Files.External.ErrorMessage",
                "Could not open '{0}' in the default application.\r\n\r\n{1}",
                node.FullPath,
                ex.Message);
            _promptService.ShowError(message, GetLocalizedString("Files.Title", "File Manager"));
            StatusBar.StatusText = GetLocalizedFormattedString("Files.External.StatusFailed", "Open failed: {0}.", node.DisplayName);
        }
    }

    private void CreateFilesPaneFile(object? parameter)
    {
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Common.TargetDirectoryUnavailable", "Target directory is not available.");
            return;
        }

        var fileName = _promptService.ShowTextInput(
            GetLocalizedString("Files.NewFile.Prompt", "Enter the name of the new file."),
            GetLocalizedString("Files.NewFile.Title", "New File"),
            GetLocalizedString("Files.NewFile.DefaultName", "New file.axm"),
            GetLocalizedString("Files.NewItem.CreateButton", "Create"));

        if (fileName is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.NewFile.StatusCancelled", "File creation cancelled.");
            return;
        }

        try
        {
            var createdPath = _fileOperationsService.CreateEmptyFile(targetDirectoryPath, fileName);
            var createdNode = ReloadFilesPane(preferredSelectionPath: createdPath);
            OpenFilesPaneNodeInEditor(createdNode ?? new FileTreeNode
            {
                FullPath = createdPath,
                DisplayName = Path.GetFileName(createdPath),
                IsDirectory = false
            });
            StatusBar.StatusText = GetLocalizedFormattedString("Files.NewFile.StatusCreated", "Created file: {0}.", Path.GetFileName(createdPath));
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.created",
                commandName: "NewFile",
                document: Documents.ActiveDocument,
                textPreview: createdPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.NewFile.Title", "New File"));
            StatusBar.StatusText = GetLocalizedString("Files.NewFile.StatusFailed", "File creation failed.");
        }
    }

    private void CreateFilesPaneDirectory(object? parameter)
    {
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Common.TargetDirectoryUnavailable", "Target directory is not available.");
            return;
        }

        var directoryName = _promptService.ShowTextInput(
            GetLocalizedString("Files.NewDirectory.Prompt", "Enter the name of the new folder."),
            GetLocalizedString("Files.NewDirectory.Title", "New Folder"),
            GetLocalizedString("Files.NewDirectory.DefaultName", "New Folder"),
            GetLocalizedString("Files.NewItem.CreateButton", "Create"));

        if (directoryName is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.NewDirectory.StatusCancelled", "Folder creation cancelled.");
            return;
        }

        try
        {
            var createdPath = _fileOperationsService.CreateDirectory(targetDirectoryPath, directoryName);
            ReloadFilesPane(preferredSelectionPath: createdPath);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.NewDirectory.StatusCreated", "Created folder: {0}.", Path.GetFileName(createdPath));
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "directory.created",
                commandName: "NewDirectory",
                document: Documents.ActiveDocument,
                textPreview: createdPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.NewDirectory.Title", "New Folder"));
            StatusBar.StatusText = GetLocalizedString("Files.NewDirectory.StatusFailed", "Folder creation failed.");
        }
    }

    private void CutFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        _filesPaneClipboardPath = node.FullPath;
        _filesPaneClipboardIsCut = true;
        _filesPaneClipboardIsDirectory = node.IsDirectory;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Clipboard.StatusCut", "Cut: {0}.", node.DisplayName);
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private void CopyFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        _filesPaneClipboardPath = node.FullPath;
        _filesPaneClipboardIsCut = false;
        _filesPaneClipboardIsDirectory = node.IsDirectory;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Clipboard.StatusCopied", "Copied: {0}.", node.DisplayName);
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private void PasteIntoFilesPane(object? parameter)
    {
        var sourcePath = _filesPaneClipboardPath;
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (string.IsNullOrWhiteSpace(sourcePath) || targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusNothing", "Nothing to paste.");
            return;
        }

        if (!_fileOperationsService.Exists(sourcePath))
        {
            ClearFilesPaneClipboard();
            _promptService.ShowError(
                GetLocalizedString("Files.Paste.ErrorMissingSource", "The selected file or folder is no longer available."),
                GetLocalizedString("Files.Context.Paste", "Paste"));
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusFailed", "Paste failed.");
            return;
        }

        try
        {
            var isCutOperation = _filesPaneClipboardIsCut;
            var clipboardContainsDirectory = _filesPaneClipboardIsDirectory;
            var targetPath = _filesPaneClipboardIsCut
                ? _fileOperationsService.MoveToDirectory(sourcePath, targetDirectoryPath)
                : _fileOperationsService.CopyToDirectory(sourcePath, targetDirectoryPath);

            if (isCutOperation)
            {
                RetargetOpenDocuments(sourcePath, targetPath, clipboardContainsDirectory);
                ClearFilesPaneClipboard();
                StatusBar.StatusText = GetLocalizedFormattedString("Files.Paste.StatusMoved", "Moved to {0}.", targetDirectoryPath);
                LogIdeEvent(
                    IdeEventLevel.Info,
                    "files",
                    "file.moved",
                    commandName: "PasteMove",
                    document: Documents.ActiveDocument,
                    textPreview: targetPath);
            }
            else
            {
                StatusBar.StatusText = GetLocalizedFormattedString("Files.Paste.StatusCopied", "Copied to {0}.", targetDirectoryPath);
                LogIdeEvent(
                    IdeEventLevel.Info,
                    "files",
                    "file.copied",
                    commandName: "PasteCopy",
                    document: Documents.ActiveDocument,
                    textPreview: targetPath);
            }

            ReloadFilesPane(
                preferredSelectionPath: targetPath,
                remappedPathOld: isCutOperation && clipboardContainsDirectory ? sourcePath : null,
                remappedPathNew: isCutOperation && clipboardContainsDirectory ? targetPath : null,
                remapDescendants: isCutOperation && clipboardContainsDirectory);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Context.Paste", "Paste"));
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusFailed", "Paste failed.");
        }
    }

    private void RenameFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        BeginFilesPaneNodeRename(node);
    }

    private void MoveFilesPaneNodeToRecycleBin(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        var confirmationMessage = node.IsDirectory
            ? GetLocalizedFormattedString("Files.Delete.ConfirmDirectory", "Delete folder '{0}'?", node.DisplayName)
            : GetLocalizedFormattedString("Files.Delete.ConfirmFile", "Delete file '{0}'?", node.DisplayName);

        if (!_promptService.ShowConfirmation(confirmationMessage, GetLocalizedString("Files.Delete.Title", "Delete")))
        {
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusCancelled", "Delete cancelled.");
            return;
        }

        if (!CloseDocumentsAffectedByPath(node.FullPath, node.IsDirectory))
        {
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusCancelled", "Delete cancelled.");
            return;
        }

        try
        {
            _fileOperationsService.SendToRecycleBin(node.FullPath);

            if (!string.IsNullOrWhiteSpace(_filesPaneClipboardPath)
                && IsPathEqualOrDescendant(_filesPaneClipboardPath!, node.FullPath, node.IsDirectory))
            {
                ClearFilesPaneClipboard();
            }

            var parentDirectoryPath = Path.GetDirectoryName(node.FullPath) ?? FilesPane.RootPath;
            ReloadFilesPane(
                preferredSelectionPath: parentDirectoryPath,
                removedPath: node.FullPath,
                removeDescendants: node.IsDirectory);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.Delete.StatusRecycled", "Moved to Recycle Bin: {0}.", node.DisplayName);
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.deleted",
                commandName: "Delete",
                document: Documents.ActiveDocument,
                textPreview: node.FullPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Delete.ErrorTitle", "Recycle Bin"));
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusFailed", "Delete failed.");
        }
    }

    public void BeginFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        foreach (var treeNode in EnumerateFilesPaneNodes(FilesPane.Nodes))
        {
            if (!ReferenceEquals(treeNode, node))
            {
                treeNode.IsRenaming = false;
            }
        }

        node.RenameText = node.DisplayName;
        node.IsRenaming = true;
    }

    public void CancelFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null)
        {
            return;
        }

        node.RenameText = node.DisplayName;
        node.IsRenaming = false;
        StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusCancelled", "Rename cancelled.");
    }

    public void CommitFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null || !node.IsRenaming)
        {
            return;
        }

        var newName = node.RenameText.Trim();
        node.IsRenaming = false;

        if (string.IsNullOrWhiteSpace(newName))
        {
            node.RenameText = node.DisplayName;
            StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusCancelled", "Rename cancelled.");
            return;
        }

        if (string.Equals(newName, node.DisplayName, StringComparison.Ordinal))
        {
            node.RenameText = node.DisplayName;
            return;
        }

        try
        {
            var targetPath = _fileOperationsService.Rename(node.FullPath, newName);
            RetargetOpenDocuments(node.FullPath, targetPath, node.IsDirectory);
            ReloadFilesPane(
                preferredSelectionPath: targetPath,
                remappedPathOld: node.IsDirectory ? node.FullPath : null,
                remappedPathNew: node.IsDirectory ? targetPath : null,
                remapDescendants: node.IsDirectory);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.Rename.StatusSucceeded", "Renamed to {0}.", Path.GetFileName(targetPath));
        }
        catch (Exception ex)
        {
            node.RenameText = node.DisplayName;
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Context.Rename", "Rename"));
            StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusFailed", "Rename failed.");
        }
    }

    private void ShowFilesPaneNodeProperties(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        _promptService.ShowInfo(
            BuildFilesPaneNodePropertiesText(node),
            GetLocalizedString("Files.Context.Properties", "Properties"));
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Properties.StatusShown", "Properties: {0}.", node.DisplayName);
    }

    private void ExpandFilesPaneNode(object? parameter)
    {
        if (parameter is not FileTreeNode node || !node.IsDirectory || node.IsLoaded)
        {
            return;
        }

        var children = _fileTreeService.BuildChildNodes(node.FullPath, FilesPane.ShowHiddenFiles);
        node.Children.Clear();

        foreach (var child in children)
        {
            node.Children.Add(child);
        }

        node.IsLoaded = true;
    }

    private bool CanOpenFilesPaneNode(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is FileTreeNode { IsDirectory: false, IsPlaceholder: false } node
        && !IsExecutableFile(node.FullPath)
        && CanModifyDocuments();

    private static bool IsExecutableFile(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase);

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
            // Untitled вЂ” РёСЃРїРѕР»СЊР·СѓРµРј last-used directory РёР»Рё MyDocuments
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

    private void OpenFilesPaneNodeInEditor(FileTreeNode node)
    {
        FilesPane.SelectedNode = node;

        if (IsExecutableFile(node.FullPath))
        {
            StatusBar.StatusText = GetLocalizedString("Files.OpenExecutableBlocked", "Executable .exe files are not opened in editor.");
            return;
        }

        var document = Documents.OpenDocumentFromPath(node.FullPath, out var statusMessage);

        if (document is not null)
        {
            StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusOpened", "Opened {0}.", document.DisplayName);
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            UpdateTitle();
            return;
        }

        StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusFailedByName", "Open failed: {0}.", Path.GetFileName(node.FullPath));
    }

    private string BuildFilesPaneNodePropertiesText(FileTreeNode node)
    {
        var lines = new List<string>
        {
            $"{GetLocalizedString("Files.Properties.Name", "Name")}: {node.DisplayName}",
            $"{GetLocalizedString("Files.Properties.Type", "Type")}: {GetLocalizedString(node.IsDirectory ? "Files.Properties.TypeDirectory" : "Files.Properties.TypeFile", node.IsDirectory ? "Folder" : "File")}",
            $"{GetLocalizedString("Files.Properties.Path", "Path")}: {node.FullPath}"
        };

        try
        {
            if (node.IsDirectory)
            {
                var directory = new DirectoryInfo(node.FullPath);
                if (directory.Exists)
                {
                    lines.Add($"{GetLocalizedString("Files.Properties.Modified", "Modified")}: {directory.LastWriteTime}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Attributes", "Attributes")}: {directory.Attributes}");
                }
            }
            else
            {
                var file = new FileInfo(node.FullPath);
                if (file.Exists)
                {
                    lines.Add(
                        $"{GetLocalizedString("Files.Properties.Size", "Size")}: {file.Length:N0} {GetLocalizedString("Files.Properties.Bytes", "bytes")}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Modified", "Modified")}: {file.LastWriteTime}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Attributes", "Attributes")}: {file.Attributes}");
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return string.Join(Environment.NewLine, lines);
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
        var line = result.StoppedAt?.SourceLocation.Line;

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

    private void RaiseFilesPaneCommandCanExecuteChanged()
    {
        NewFilesPaneFileCommand.RaiseCanExecuteChanged();
        NewFilesPaneDirectoryCommand.RaiseCanExecuteChanged();
        CutFilesPaneNodeCommand.RaiseCanExecuteChanged();
        CopyFilesPaneNodeCommand.RaiseCanExecuteChanged();
        PasteIntoFilesPaneCommand.RaiseCanExecuteChanged();
        RenameFilesPaneNodeCommand.RaiseCanExecuteChanged();
        MoveFilesPaneNodeToRecycleBinCommand.RaiseCanExecuteChanged();
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

    private void ClearFilesPaneClipboard()
    {
        _filesPaneClipboardPath = null;
        _filesPaneClipboardIsCut = false;
        _filesPaneClipboardIsDirectory = false;
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private bool CloseDocumentsAffectedByPath(string path, bool includeDescendants)
    {
        var affectedDocuments = GetDocumentsAffectedByPath(path, includeDescendants).ToList();

        foreach (var document in affectedDocuments)
        {
            var closed = Documents.CloseDocument(document, PromptSaveDiscardCancel);

            if (!closed)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<DocumentTabViewModel> GetDocumentsAffectedByPath(string sourcePath, bool includeDescendants)
    {
        foreach (var document in Documents.Documents)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                continue;
            }

            if (IsPathEqualOrDescendant(document.FilePath, sourcePath, includeDescendants))
            {
                yield return document;
            }
        }
    }

    private void RetargetOpenDocuments(string oldPath, string newPath, bool includeDescendants)
    {
        foreach (var document in GetDocumentsAffectedByPath(oldPath, includeDescendants))
        {
            if (document.FilePath is null)
            {
                continue;
            }

            document.FilePath = includeDescendants
                ? ReplacePathPrefix(document.FilePath, oldPath, newPath)
                : newPath;
        }
    }

    private static string ReplacePathPrefix(string currentPath, string oldPrefix, string newPrefix)
    {
        if (string.Equals(currentPath, oldPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return newPrefix;
        }

        var suffix = currentPath[oldPrefix.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(newPrefix, suffix);
    }

    private static HashSet<string> CollectExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes)
    {
        var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectExpandedDirectoryPaths(nodes, expandedPaths);
        return expandedPaths;
    }

    private static void CollectExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes, ISet<string> expandedPaths)
    {
        foreach (var node in nodes)
        {
            if (!node.IsDirectory || !node.IsExpanded)
            {
                continue;
            }

            expandedPaths.Add(node.FullPath);
            CollectExpandedDirectoryPaths(node.Children, expandedPaths);
        }
    }

    private static IEnumerable<FileTreeNode> EnumerateFilesPaneNodes(IEnumerable<FileTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;

            foreach (var child in EnumerateFilesPaneNodes(node.Children))
            {
                yield return child;
            }
        }
    }

    private void RestoreExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes, ISet<string> expandedPaths)
    {
        foreach (var node in nodes)
        {
            if (!node.IsDirectory || !expandedPaths.Contains(node.FullPath))
            {
                continue;
            }

            node.IsExpanded = true;

            if (!node.IsLoaded)
            {
                var children = _fileTreeService.BuildChildNodes(node.FullPath, FilesPane.ShowHiddenFiles);
                node.Children.Clear();

                foreach (var child in children)
                {
                    node.Children.Add(child);
                }

                node.IsLoaded = true;
            }

            RestoreExpandedDirectoryPaths(node.Children, expandedPaths);
        }
    }

    private static void ExpandAncestorDirectoryPaths(ISet<string> expandedPaths, string? selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        var currentPath = Path.GetDirectoryName(selectedPath);

        while (!string.IsNullOrWhiteSpace(currentPath))
        {
            expandedPaths.Add(currentPath);
            currentPath = Path.GetDirectoryName(
                currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }

    private static HashSet<string> RemapTrackedPaths(
        IEnumerable<string> trackedPaths,
        string oldPath,
        string newPath,
        bool includeDescendants)
    {
        var remappedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var trackedPath in trackedPaths)
        {
            remappedPaths.Add(RemapTrackedPath(trackedPath, oldPath, newPath, includeDescendants) ?? trackedPath);
        }

        return remappedPaths;
    }

    private static string? RemapTrackedPath(
        string? trackedPath,
        string oldPath,
        string newPath,
        bool includeDescendants)
    {
        if (string.IsNullOrWhiteSpace(trackedPath)
            || !IsPathEqualOrDescendant(trackedPath, oldPath, includeDescendants))
        {
            return trackedPath;
        }

        return includeDescendants
            ? ReplacePathPrefix(trackedPath, oldPath, newPath)
            : newPath;
    }

    private static void RemoveTrackedPaths(ISet<string> trackedPaths, string removedPath, bool includeDescendants)
    {
        foreach (var trackedPath in trackedPaths
                     .Where(path => IsPathEqualOrDescendant(path, removedPath, includeDescendants))
                     .ToArray())
        {
            trackedPaths.Remove(trackedPath);
        }
    }

    private static FileTreeNode? FindNodeByPath(IEnumerable<FileTreeNode> nodes, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return null;
        }

        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var childMatch = FindNodeByPath(node.Children, targetPath);
            if (childMatch is not null)
            {
                return childMatch;
            }
        }

        return null;
    }

    private static bool IsPathEqualOrDescendant(string candidatePath, string parentPath, bool includeDescendants)
    {
        if (string.Equals(candidatePath, parentPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!includeDescendants)
        {
            return false;
        }

        var normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedCandidate.StartsWith(
            normalizedParent + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
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
