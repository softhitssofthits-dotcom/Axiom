using Axiom.Ide.Models;
using Axiom.Ide.Editor.Formatting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Files;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Documents;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Prompts;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;
using System.Reflection;
using System.Diagnostics;

namespace Axiom.Ide.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_CreatesInitialEmptyTab()
    {
        var viewModel = CreateViewModel();

        var tab = Assert.Single(viewModel.Documents.Documents);
        Assert.True(tab.IsUntitled);
        Assert.Equal(viewModel.Documents.ActiveDocument, tab);
        Assert.Equal("Untitled.axm", viewModel.StatusBar.PathText);
        Assert.Equal("Untitled.axm - Axiom IDE", viewModel.WindowTitle);
    }

    [Fact]
    public void Constructor_RestoresShellPreferencesFromPersistenceSnapshot()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", false, IdeWindowState.Maximized, false, IdeTheme.Dark, IdeLanguage.English, true));

        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        Assert.False(viewModel.FilesPane.IsVisible);
        Assert.False(viewModel.StatusBar.IsVisible);
        Assert.Equal(IdeWindowState.Maximized, viewModel.PreferredWindowState);
        Assert.Equal(IdeTheme.Dark, viewModel.SelectedTheme);
        Assert.Equal(IdeLanguage.English, viewModel.SelectedLanguage);
        Assert.True(viewModel.IsSmartIdentifierEnabled);
    }

    [Fact]
    public void Constructor_LoadsFilesPaneNodesFromLastUsedDirectory()
    {
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = @"C:\temp\sample.axm", DisplayName = "sample.axm" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));

        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        Assert.Equal(@"C:\temp", viewModel.FilesPane.RootPath);
        Assert.Equal(@"C:\temp", fileTreeService.LastRootPath);
        Assert.Equal("sample.axm", Assert.Single(viewModel.FilesPane.Nodes).DisplayName);
    }

    [Fact]
    public void Constructor_UsesAxiomRootWhenLastUsedDirectoryMissing()
    {
        var fileTreeService = new FakeFileTreeService([]);
        var recentFilesService = new FakeRecentFilesService(RecentFilesSnapshot.Empty);

        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        Assert.Equal(IdeWorkspaceDefaults.RootPath, viewModel.FilesPane.RootPath);
        Assert.Equal(IdeWorkspaceDefaults.RootPath, fileTreeService.LastRootPath);
    }

    [Fact]
    public void Constructor_EnablesClassicFormattingByDefault()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.IsClassicFormattingEnabled);
        Assert.False(viewModel.IsModernFormattingEnabled);
        Assert.Equal(CodeFormattingStyle.Classic, viewModel.SelectedFormattingStyle);
    }

    [Fact]
    public void Constructor_DisablesSmartIdentifierByDefault()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.IsSmartIdentifierEnabled);
    }

    [Fact]
    public void UseModernFormattingCommand_SwitchesFormattingStyle()
    {
        var viewModel = CreateViewModel();

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.False(viewModel.IsClassicFormattingEnabled);
        Assert.True(viewModel.IsModernFormattingEnabled);
        Assert.Equal(CodeFormattingStyle.Modern, viewModel.SelectedFormattingStyle);
    }

    [Fact]
    public void UseModernFormattingCommand_ReformatsActiveMbasDocument()
    {
        var viewModel = CreateViewModel();
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT LEN(\"x\") + 1\r\nDIM a AS INTEGER = 1");

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Equal("Print Len(\"x\") + 1\r\nDim a As Integer = 1", viewModel.Documents.ActiveDocument!.SourceText);
        Assert.True(viewModel.Documents.ActiveDocument.IsDirty);
    }

    [Fact]
    public void UseModernFormattingCommand_ReformatsActiveUntitledDocument()
    {
        var viewModel = CreateViewModel();
        viewModel.Documents.ActiveDocument!.SourceText = "PRINT LEN(\"x\") + 1\r\nDIM a AS INTEGER = 1";
        viewModel.Documents.ActiveDocument.MarkAsSaved();

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Equal("Print Len(\"x\") + 1\r\nDim a As Integer = 1", viewModel.Documents.ActiveDocument.SourceText);
        Assert.True(viewModel.Documents.ActiveDocument.IsDirty);
    }

    [Fact]
    public void UseModernFormattingCommand_AlsoNormalizesIdentifiersWhenSmartIdentifierEnabled()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal, true, IdeTheme.Light, IdeLanguage.Russian, true));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);
        PrepareSavedActiveDocument(
            viewModel,
            @"C:\temp\program.axm",
            "DIM MyFirstDay AS INTEGER\r\nprint myFIRSTday");

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Equal("Dim MyFirstDay As Integer\r\nPrint MyFirstDay", viewModel.Documents.ActiveDocument!.SourceText);
    }

    [Fact]
    public void UseModernFormattingCommand_WritesTraceEntriesForSwitchAndReformat()
    {
        var traceService = new FakeIdeTraceService();
        var viewModel = CreateViewModel(traceService: traceService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT LEN(\"x\") + 1");

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "format.style-switch"
                && entry.Message.Contains("style=Modern", StringComparison.Ordinal));
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "format.document.reformatted"
                && entry.Message.Contains("document=program.axm", StringComparison.Ordinal)
                && entry.Message.Contains("style=Modern", StringComparison.Ordinal));
    }

    [Fact]
    public void UseModernFormattingCommand_DoesNotReformatNonMbasDocument()
    {
        var viewModel = CreateViewModel();
        PrepareSavedActiveDocument(viewModel, @"C:\temp\notes.txt", "PRINT LEN(\"x\") + 1");

        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Equal("PRINT LEN(\"x\") + 1", viewModel.Documents.ActiveDocument!.SourceText);
        Assert.False(viewModel.Documents.ActiveDocument.IsDirty);
    }

    [Fact]
    public void ActivatingMbasTab_DoesNotReformatDocumentAutomatically()
    {
        var viewModel = CreateViewModel();
        PrepareSavedActiveDocument(viewModel, @"C:\temp\first.axm", "PRINT 1");
        var firstDocument = viewModel.Documents.ActiveDocument!;

        viewModel.NewCommand.Execute(null);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\second.axm", "PRINT LEN(\"x\") + 1\r\nDIM a AS INTEGER = 1");
        var secondDocument = viewModel.Documents.ActiveDocument!;

        viewModel.Documents.ActiveDocument = firstDocument;
        viewModel.UseModernFormattingCommand.Execute(null);

        Assert.Equal("PRINT LEN(\"x\") + 1\r\nDIM a AS INTEGER = 1", secondDocument.SourceText);
        Assert.False(secondDocument.IsDirty);

        viewModel.Documents.ActiveDocument = secondDocument;

        Assert.Equal("PRINT LEN(\"x\") + 1\r\nDIM a AS INTEGER = 1", secondDocument.SourceText);
        Assert.False(secondDocument.IsDirty);
    }

    [Fact]
    public void NewCommand_CreatesNewTab()
    {
        var viewModel = CreateViewModel();

        viewModel.NewCommand.Execute(null);

        Assert.Equal(2, viewModel.Documents.Documents.Count);
        Assert.True(viewModel.Documents.ActiveDocument!.IsUntitled);
    }

    [Fact]
    public void CompileProgram_SetsDiagnosticsAndStatus()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.CompileCommand.Execute(null);
        WaitForBackgroundUiWork(viewModel);

        Assert.Equal(1, runService.CompileCallCount);
        Assert.Equal("PRINT 1", runService.LastCompiledSourceText);
        Assert.False(viewModel.StatusBar.IsRunning);
        Assert.Contains("компиляц", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("успеш", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(viewModel.BottomPane.Diagnostics);
    }

    [Fact]
    public void CompileProgram_ShowsCompilationTimingText()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnCompile = _ =>
            {
                Thread.Sleep(80);
                return new ProgramCompilationResult(ProgramCompilationStatus.Success, []);
            }
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.CompileCommand.Execute(null);
        WaitForBackgroundUiWork(viewModel);

        Assert.True(viewModel.StatusBar.IsTimingVisible);
        Assert.True(viewModel.StatusBar.IsCompilationTimingVisible);
        Assert.NotEqual(string.Empty, viewModel.StatusBar.CompilationTimingText);
        Assert.Contains("мс", viewModel.StatusBar.CompilationTimingText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompileProgram_WithDiagnostics_SelectsAndFocusesDiagnostics()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnCompile = _ => new ProgramCompilationResult(
                ProgramCompilationStatus.SyntaxFailure,
                [new ProgramDiagnosticItem("MB1001", "Syntax", "Example", 1, 1, 1)])
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.CompileCommand.Execute(null);
        WaitForBackgroundUiWork(viewModel);

        Assert.Equal(1, viewModel.BottomPane.SelectedTabIndex);
        Assert.Equal(BottomPaneFocusTarget.Diagnostics, viewModel.BottomPane.FocusTarget);
        Assert.Equal(1, viewModel.BottomPane.FocusRequestVersion);
        Assert.NotNull(viewModel.BottomPane.SelectedDiagnostic);
        Assert.Contains("диагностик", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunProgram_SetsOutputAndStatus()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var viewModel = CreateViewModel(documentService: documentService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.RunCommand.Execute(null);

        Assert.False(viewModel.StatusBar.IsRunning);
        Assert.Equal(0, viewModel.BottomPane.SelectedTabIndex);
        Assert.Equal(1, viewModel.BottomPane.FocusRequestVersion);
        Assert.Equal(BottomPaneFocusTarget.Output, viewModel.BottomPane.FocusTarget);
        Assert.Contains("успеш", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunProgram_ShowsTimingTextWithCacheState()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnRun = _ => new ProgramRunResult(
                ProgramRunStatus.Success,
                [],
                "1",
                preparationDuration: TimeSpan.FromMilliseconds(7),
                executionDuration: TimeSpan.FromMilliseconds(11),
                usedCompiledCache: true,
                actualEngine: Axiom.Execution.ExecutionEngineKind.Interpreter)
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.RunCommand.Execute(null);

        Assert.True(viewModel.StatusBar.IsTimingVisible);
        Assert.True(viewModel.StatusBar.IsRunTimingVisible);
        Assert.Contains("Подготовка", viewModel.StatusBar.PreparationTimingText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7", viewModel.StatusBar.PreparationTimingText, StringComparison.Ordinal);
        Assert.Contains("Выполнение", viewModel.StatusBar.ExecutionTimingText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("11", viewModel.StatusBar.ExecutionTimingText, StringComparison.Ordinal);
        Assert.NotEqual(string.Empty, viewModel.StatusBar.TotalTimingText);
        Assert.Contains("18", viewModel.StatusBar.TotalTimingText, StringComparison.Ordinal);
        Assert.Contains("да", viewModel.StatusBar.CacheTimingText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("интерпретатор", viewModel.StatusBar.EngineTimingText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunProgram_UsesRunResultOutputAsFallbackWhenConsoleTranscriptIsEmpty()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnRun = _ => new ProgramRunResult(
                ProgramRunStatus.Success,
                [],
                "2" + Environment.NewLine + "равно")
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.RunCommand.Execute(null);

        Assert.Equal("2" + Environment.NewLine + "равно", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void RunProgram_WritesTraceEntriesForRequestResultAndFallback()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1 + 1", false)
        };
        var traceService = new FakeIdeTraceService();
        var runService = new FakeProgramRunService
        {
            OnRun = _ => new ProgramRunResult(
                ProgramRunStatus.Success,
                [],
                "2")
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService, traceService: traceService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1 + 1");

        viewModel.RunCommand.Execute(null);

        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "run.request"
                && entry.Message.Contains("preview=\"PRINT 1 + 1\"", StringComparison.Ordinal));
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "run.output-sync");
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "run.result"
                && entry.Message.Contains("status=Success", StringComparison.Ordinal)
                && entry.Message.Contains("output=\"2\"", StringComparison.Ordinal));
    }

    [Fact]
    public void RunProgram_WithDiagnostics_SelectsAndFocusesDiagnostics()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnRun = _ => new ProgramRunResult(
                ProgramRunStatus.SyntaxFailure,
                [new ProgramDiagnosticItem("MB1001", "Syntax", "Example", 1, 1, 1)],
                string.Empty)
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.RunCommand.Execute(null);

        Assert.Equal(1, viewModel.BottomPane.SelectedTabIndex);
        Assert.Equal(BottomPaneFocusTarget.Diagnostics, viewModel.BottomPane.FocusTarget);
        Assert.Equal(2, viewModel.BottomPane.FocusRequestVersion);
        Assert.NotNull(viewModel.BottomPane.SelectedDiagnostic);
        Assert.Contains("диагностик", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowOutputCommand_SelectsOutputAndRequestsFocus()
    {
        var viewModel = CreateViewModel();
        viewModel.BottomPane.SelectDiagnostics();

        viewModel.ShowOutputCommand.Execute(null);

        Assert.Equal(0, viewModel.BottomPane.SelectedTabIndex);
        Assert.Equal(BottomPaneFocusTarget.Output, viewModel.BottomPane.FocusTarget);
        Assert.Equal(1, viewModel.BottomPane.FocusRequestVersion);
    }

    [Fact]
    public void ShowDiagnosticsCommand_SelectsDiagnosticsAndRequestsFocus()
    {
        var viewModel = CreateViewModel();

        viewModel.ShowDiagnosticsCommand.Execute(null);

        Assert.Equal(1, viewModel.BottomPane.SelectedTabIndex);
        Assert.Equal(BottomPaneFocusTarget.Diagnostics, viewModel.BottomPane.FocusTarget);
        Assert.Equal(1, viewModel.BottomPane.FocusRequestVersion);
    }

    [Fact]
    public void CloseOutputPaneCommand_HidesOutputPane()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.CloseOutputPaneCommand.CanExecute(null));

        viewModel.CloseOutputPaneCommand.Execute(null);

        Assert.False(viewModel.BottomPane.IsOutputVisible);
        Assert.False(viewModel.CloseOutputPaneCommand.CanExecute(null));
    }

    [Fact]
    public void CloseDiagnosticsPaneCommand_HidesDiagnosticsPane()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.CloseDiagnosticsPaneCommand.CanExecute(null));

        viewModel.CloseDiagnosticsPaneCommand.Execute(null);

        Assert.False(viewModel.BottomPane.IsDiagnosticsVisible);
        Assert.False(viewModel.CloseDiagnosticsPaneCommand.CanExecute(null));
    }

    [Fact]
    public void RunProgram_ReopensOutputPaneWhenPreviouslyHidden()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var viewModel = CreateViewModel(documentService: documentService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.CloseOutputPaneCommand.Execute(null);
        Assert.False(viewModel.BottomPane.IsOutputVisible);

        viewModel.RunCommand.Execute(null);

        Assert.True(viewModel.BottomPane.IsOutputVisible);
        Assert.Equal(0, viewModel.BottomPane.SelectedTabIndex);
    }

    [Fact]
    public void RunProgram_WithDiagnostics_ReopensDiagnosticsPaneWhenPreviouslyHidden()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService
        {
            OnRun = _ => new ProgramRunResult(
                ProgramRunStatus.SyntaxFailure,
                [new ProgramDiagnosticItem("MB1001", "Syntax", "Example", 1, 1, 1)],
                string.Empty)
        };
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.CloseDiagnosticsPaneCommand.Execute(null);
        Assert.False(viewModel.BottomPane.IsDiagnosticsVisible);

        viewModel.RunCommand.Execute(null);

        Assert.True(viewModel.BottomPane.IsDiagnosticsVisible);
        Assert.Equal(1, viewModel.BottomPane.SelectedTabIndex);
    }

    [Fact]
    public void CloseOutputPaneCommand_IsDisabledWhileWaitingForInput()
    {
        var consoleService = new ProgramConsoleService();
        var viewModel = CreateViewModel(consoleService: consoleService);

        SetProgramConsoleProperty(consoleService, nameof(ProgramConsoleService.IsWaitingForInput), true);

        Assert.False(viewModel.CloseOutputPaneCommand.CanExecute(null));
        Assert.True(viewModel.BottomPane.IsOutputVisible);
    }

    [Fact]
    public void RunCommand_DisablesRunAndDocumentNavigationWhileExecutionIsInProgress()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService();
        MainWindowViewModel? viewModel = null;
        runService.OnRun = _ =>
        {
            Assert.NotNull(viewModel);
            Assert.True(viewModel!.StatusBar.IsRunning);
            Assert.False(viewModel.RunCommand.CanExecute(null));
            Assert.False(viewModel.NewCommand.CanExecute(null));
            Assert.False(viewModel.OpenCommand.CanExecute(null));
            Assert.False(viewModel.CloseTabCommand.CanExecute(null));
            Assert.False(viewModel.ExitCommand.CanExecute(null));
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        };

        viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.RunCommand.Execute(null);

        Assert.True(viewModel.RunCommand.CanExecute(null));
        Assert.True(viewModel.NewCommand.CanExecute(null));
        Assert.True(viewModel.OpenCommand.CanExecute(null));
        Assert.True(viewModel.ExitCommand.CanExecute(null));
    }

    [Fact]
    public void RunProgram_PromptsToSaveDirtyDocumentBeforeRunning()
    {
        var documentService = new FakeDocumentFileService
        {
            SaveDocumentResult = @"C:\temp\program.axm"
        };
        var promptService = new FakeDialogPromptService
        {
            ConfirmationResult = true
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            runService: runService,
            promptService: promptService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");
        viewModel.Documents.ActiveDocument!.SourceText = "PRINT 2";

        viewModel.RunCommand.Execute(null);

        Assert.Contains("Сохранить", promptService.LastConfirmationMessage, StringComparison.Ordinal);
        Assert.Contains("запуск", promptService.LastConfirmationMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(@"C:\temp\program.axm", documentService.LastSavedFilePath);
        Assert.Equal("PRINT 2", documentService.LastSavedSourceText);
        Assert.Equal("PRINT 2", runService.LastSourceText);
    }

    [Fact]
    public void RunProgram_WhenUserDeclinesSaveBeforeRun_DoesNotRun()
    {
        var promptService = new FakeDialogPromptService
        {
            ConfirmationResult = false
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(
            runService: runService,
            promptService: promptService);
        viewModel.Documents.ActiveDocument!.SourceText = "PRINT 1";

        viewModel.RunCommand.Execute(null);

        Assert.Equal(0, runService.CallCount);
        Assert.Contains("сохран", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("запуск", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InputWait_SelectsOutputAndEnablesSubmitCommand()
    {
        var consoleService = new ProgramConsoleService();
        var viewModel = CreateViewModel(consoleService: consoleService);
        viewModel.BottomPane.SelectDiagnostics();

        SetProgramConsoleProperty(consoleService, nameof(ProgramConsoleService.IsWaitingForInput), true);

        Assert.True(viewModel.BottomPane.IsWaitingForInput);
        Assert.Equal(0, viewModel.BottomPane.SelectedTabIndex);
        Assert.True(viewModel.SubmitInputCommand.CanExecute(null));
        Assert.False(viewModel.RunCommand.CanExecute(null));
        Assert.False(viewModel.OpenCommand.CanExecute(null));
    }

    [Fact]
    public void SubmitInput_UsesBottomPaneCurrentInputText()
    {
        var consoleService = new ProgramConsoleService();
        var viewModel = CreateViewModel(consoleService: consoleService);
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        dispatcher.BeginInvoke(() =>
        {
            viewModel.BottomPane.CurrentInputText = "Иван";
            viewModel.SubmitInputCommand.Execute(null);
        });

        var result = consoleService.RequestInput(new ProgramInputRequest(1, null, 1));

        Assert.Equal("Иван", result);
        Assert.Equal("? Иван", consoleService.OutputText);
    }

    [Fact]
    public void SaveAsCommand_SavesUntitledDocumentWithNewPath()
    {
        var documentService = new FakeDocumentFileService
        {
            SaveDocumentResult = @"C:\temp\sample.axm"
        };

        var viewModel = CreateViewModel(documentService: documentService);
        viewModel.Documents.ActiveDocument!.SourceText = "PRINT 1";
        viewModel.Documents.ActiveDocument.IsDirty = true;

        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal(@"C:\temp\sample.axm", viewModel.Documents.ActiveDocument.FilePath);
        Assert.False(viewModel.Documents.ActiveDocument.IsDirty);
        Assert.Equal(@"C:\temp", viewModel.FilesPane.RootPath);
        Assert.Equal(@"C:\temp\sample.axm", viewModel.StatusBar.PathText);
        Assert.Contains("сохран", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaveAsCommand_UsesAxiomRootWhenUntitledDocumentHasNoKnownDirectory()
    {
        var documentService = new FakeDocumentFileService
        {
            SaveDocumentResult = @"C:\Axiom\sample.axm"
        };

        var viewModel = CreateViewModel(
            documentService: documentService,
            recentFilesService: new FakeRecentFilesService(RecentFilesSnapshot.Empty));

        viewModel.Documents.ActiveDocument!.SourceText = "PRINT 1";
        viewModel.Documents.ActiveDocument.IsDirty = true;

        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal(IdeWorkspaceDefaults.RootPath, documentService.LastSaveInitialDirectoryPath);
    }

    [Fact]
    public void EditingDocument_TracksDirtyStateAutomatically()
    {
        var viewModel = CreateViewModel();
        var document = viewModel.Documents.ActiveDocument!;

        document.SourceText = "PRINT 1";

        Assert.True(document.IsDirty);

        document.SourceText = string.Empty;

        Assert.False(document.IsDirty);
    }

    [Fact]
    public void CloseTabCommand_WithDocumentParameter_ClosesSpecifiedCleanTab()
    {
        var viewModel = CreateViewModel();
        var firstDocument = viewModel.Documents.ActiveDocument!;

        viewModel.NewCommand.Execute(null);
        var secondDocument = viewModel.Documents.ActiveDocument!;

        viewModel.CloseTabCommand.Execute(firstDocument);

        Assert.Single(viewModel.Documents.Documents);
        Assert.DoesNotContain(firstDocument, viewModel.Documents.Documents);
        Assert.Same(secondDocument, viewModel.Documents.ActiveDocument);
        Assert.Contains("закрыта вкладка", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CloseTabCommand_DirtyDocumentAndPromptCancel_KeepsTabOpen()
    {
        var promptService = new FakeDialogPromptService
        {
            SaveDiscardCancelResult = null
        };
        var viewModel = CreateViewModel(promptService: promptService);
        var document = viewModel.Documents.ActiveDocument!;
        document.SourceText = "PRINT 1";

        viewModel.CloseTabCommand.Execute(document);

        Assert.Single(viewModel.Documents.Documents);
        Assert.Same(document, viewModel.Documents.ActiveDocument);
        Assert.Equal("Untitled.axm", promptService.LastPromptedFileName);
        Assert.Contains("отмен", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CloseTabCommand_DirtyDocumentAndPromptSave_SavesThenClosesTab()
    {
        var documentService = new FakeDocumentFileService
        {
            SaveDocumentResult = @"C:\temp\saved-from-close.axm"
        };
        var promptService = new FakeDialogPromptService
        {
            SaveDiscardCancelResult = true
        };
        var viewModel = CreateViewModel(documentService: documentService, promptService: promptService);
        var dirtyDocument = viewModel.Documents.ActiveDocument!;
        dirtyDocument.SourceText = "PRINT 1";

        viewModel.NewCommand.Execute(null);
        var remainingDocument = viewModel.Documents.ActiveDocument!;

        viewModel.CloseTabCommand.Execute(dirtyDocument);

        Assert.Single(viewModel.Documents.Documents);
        Assert.DoesNotContain(dirtyDocument, viewModel.Documents.Documents);
        Assert.Same(remainingDocument, viewModel.Documents.ActiveDocument);
        Assert.Equal(@"C:\temp\saved-from-close.axm", documentService.LastSavedFilePath);
        Assert.Equal("Untitled.axm", promptService.LastPromptedFileName);
    }

    [Fact]
    public void Constructor_ProjectsRecentFilesFromPersistenceSnapshot()
    {
        var recentPath = @"C:\temp\recent.axm";
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([recentPath], @"C:\temp", true, IdeWindowState.Normal));

        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        var item = Assert.Single(viewModel.RecentFiles);
        Assert.True(viewModel.HasRecentFiles);
        Assert.Equal(recentPath, item.FilePath);
        Assert.Equal("recent.axm", item.FileName);
    }

    [Fact]
    public void OpenRecentCommand_OpensRecentFileAndUpdatesStatus()
    {
        var recentPath = @"C:\temp\recent.axm";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(recentPath, "PRINT 42", false)
        };
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([recentPath], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(documentService, recentFilesService);
        var recentItem = Assert.Single(viewModel.RecentFiles);

        viewModel.OpenRecentCommand.Execute(recentItem);

        Assert.Equal(recentPath, viewModel.Documents.ActiveDocument!.FilePath);
        Assert.Equal(@"C:\temp", viewModel.FilesPane.RootPath);
        Assert.Equal(recentPath, viewModel.StatusBar.PathText);
        Assert.Contains("открыт", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenRecentCommand_UsesEnglishStatusWhenShellLanguageIsEnglish()
    {
        var recentPath = @"C:\temp\recent.axm";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(recentPath, "PRINT 42", false)
        };
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([recentPath], @"C:\temp", true, IdeWindowState.Normal, true, IdeTheme.Light, IdeLanguage.English));
        var viewModel = CreateViewModel(documentService, recentFilesService);
        var recentItem = Assert.Single(viewModel.RecentFiles);

        viewModel.OpenRecentCommand.Execute(recentItem);

        Assert.Equal("Opened recent.axm.", viewModel.StatusBar.StatusText);
    }

    [Fact]
    public void OpenFilesPaneNodeCommand_OpensFileAndUpdatesStatus()
    {
        var path = @"C:\temp\pane-file.axm";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(path, "PRINT 10", false)
        };
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = path, DisplayName = "pane-file.axm" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            documentService,
            recentFilesService,
            fileTreeService);

        var node = Assert.Single(viewModel.FilesPane.Nodes);
        viewModel.OpenFilesPaneNodeCommand.Execute(node);

        Assert.Equal(path, viewModel.Documents.ActiveDocument!.FilePath);
        Assert.Equal(@"C:\temp", viewModel.FilesPane.RootPath);
        Assert.Contains("открыт", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExpandFilesPaneNodeCommand_LoadsChildrenForDirectoryNode()
    {
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = @"C:\temp\first.axm", DisplayName = "first.axm" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        var directoryNode = new FileTreeNode
        {
            FullPath = @"C:\temp\folder",
            DisplayName = "folder",
            IsDirectory = true
        };
        directoryNode.Children.Add(FileTreeNode.CreatePlaceholder());

        fileTreeService.NextChildren =
            [new FileTreeNode { FullPath = @"C:\temp\folder\second.axm", DisplayName = "second.axm" }];

        viewModel.ExpandFilesPaneNodeCommand.Execute(directoryNode);

        Assert.Equal(@"C:\temp\folder", fileTreeService.LastExpandedPath);
        Assert.Equal("second.axm", Assert.Single(directoryNode.Children).DisplayName);
        Assert.True(directoryNode.IsLoaded);
    }

    [Fact]
    public void NavigateFilesPaneNodeCommand_SwitchesRootToSelectedDirectory()
    {
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = @"C:\temp", DisplayName = "temp", IsDirectory = true }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        var directoryNode = new FileTreeNode
        {
            FullPath = @"C:\temp\folder",
            DisplayName = "folder",
            IsDirectory = true
        };

        fileTreeService.NextNodes =
            [new FileTreeNode { FullPath = @"C:\temp\folder\child.txt", DisplayName = "child.txt" }];

        viewModel.NavigateFilesPaneNodeCommand.Execute(directoryNode);

        Assert.Equal(@"C:\temp\folder", viewModel.FilesPane.RootPath);
        Assert.Equal(@"C:\temp\folder", fileTreeService.LastRootPath);
        Assert.Contains("переход", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenFilesPaneNodeCommand_OpensNonMbasFileAsEditableDocument()
    {
        var path = @"C:\temp\settings.json";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(path, "{ \"theme\": \"light\" }", false)
        };
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = path, DisplayName = "settings.json" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            documentService,
            recentFilesService,
            fileTreeService);

        viewModel.OpenFilesPaneNodeCommand.Execute(Assert.Single(viewModel.FilesPane.Nodes));

        Assert.False(viewModel.Documents.ActiveDocument!.IsReadOnly);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Contains("открыт", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenFilesPaneNodeCommand_DoesNotOpenExecutableFiles()
    {
        var path = @"C:\temp\app.exe";
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = path, DisplayName = "app.exe" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        var previousActiveDocument = viewModel.Documents.ActiveDocument;
        var node = Assert.Single(viewModel.FilesPane.Nodes);

        Assert.False(viewModel.OpenFilesPaneNodeCommand.CanExecute(node));

        viewModel.OpenFilesPaneNodeCommand.Execute(node);

        Assert.Same(previousActiveDocument, viewModel.Documents.ActiveDocument);
        Assert.Contains(".exe", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NewFilesPaneFileCommand_CreatesFileAndOpensItInEditor()
    {
        var promptService = new FakeDialogPromptService
        {
            TextInputResult = "fresh.axm"
        };
        var fileOperationsService = new FakeFileOperationsService();
        var documentService = new FakeDocumentFileService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            promptService: promptService,
            fileOperationsService: fileOperationsService);

        viewModel.FilesPane.SelectedNode = new FileTreeNode
        {
            FullPath = @"C:\temp",
            DisplayName = "temp",
            IsDirectory = true
        };

        viewModel.NewFilesPaneFileCommand.Execute(null);

        Assert.Equal(@"C:\temp", fileOperationsService.LastCreateFileDirectoryPath);
        Assert.Equal("fresh.axm", fileOperationsService.LastCreateFileName);
        Assert.Equal(@"C:\temp\fresh.axm", documentService.LoadedDocumentPath);
        Assert.Equal(@"C:\temp\fresh.axm", viewModel.Documents.ActiveDocument!.FilePath);
    }

    [Fact]
    public void NewFilesPaneFileCommand_PreservesExpandedTreeStateAfterReload()
    {
        var rootNode = CreateDirectoryNode(@"C:\", "Disk (C:)", isDrive: true, isExpanded: true);
        var tempNode = CreateDirectoryNode(@"C:\temp", "temp", isExpanded: true);
        tempNode.Children.Add(new FileTreeNode
        {
            FullPath = @"C:\temp\existing.axm",
            DisplayName = "existing.axm",
            IsDirectory = false
        });
        rootNode.Children.Add(tempNode);

        var reloadedRootNode = CreateDirectoryNode(@"C:\", "Disk (C:)", isDrive: true, isLoaded: false);
        var reloadedTempNode = CreateDirectoryNode(@"C:\temp", "temp", isLoaded: false);
        var fileTreeService = new FakeFileTreeService([rootNode])
        {
            NextNodes = [reloadedRootNode]
        };
        fileTreeService.SetChildren(@"C:\", [reloadedTempNode]);
        fileTreeService.SetChildren(
            @"C:\temp",
            [new FileTreeNode
            {
                FullPath = @"C:\temp\fresh.axm",
                DisplayName = "fresh.axm",
                IsDirectory = false
            }]);
        var promptService = new FakeDialogPromptService
        {
            TextInputResult = "fresh.axm"
        };
        var fileOperationsService = new FakeFileOperationsService();
        var documentService = new FakeDocumentFileService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            fileTreeService: fileTreeService,
            promptService: promptService,
            fileOperationsService: fileOperationsService);

        viewModel.FilesPane.SelectedNode = tempNode;

        viewModel.NewFilesPaneFileCommand.Execute(null);

        var restoredRootNode = Assert.Single(viewModel.FilesPane.Nodes);
        Assert.True(restoredRootNode.IsExpanded);
        var restoredTempNode = Assert.Single(restoredRootNode.Children);
        Assert.True(restoredTempNode.IsExpanded);
        Assert.Equal(@"C:\temp\fresh.axm", viewModel.FilesPane.SelectedNode!.FullPath);
    }

    [Fact]
    public void NewFilesPaneDirectoryCommand_CreatesFolderInSelectedDirectory()
    {
        var promptService = new FakeDialogPromptService
        {
            TextInputResult = "examples"
        };
        var fileOperationsService = new FakeFileOperationsService();
        var viewModel = CreateViewModel(
            promptService: promptService,
            fileOperationsService: fileOperationsService);

        viewModel.FilesPane.SelectedNode = new FileTreeNode
        {
            FullPath = @"C:\temp",
            DisplayName = "temp",
            IsDirectory = true
        };

        viewModel.NewFilesPaneDirectoryCommand.Execute(null);

        Assert.Equal(@"C:\temp", fileOperationsService.LastCreateDirectoryParentPath);
        Assert.Equal("examples", fileOperationsService.LastCreateDirectoryName);
        Assert.Contains("создан", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PasteIntoFilesPaneCommand_AfterCut_MovesNodeAndRetargetsOpenDocument()
    {
        var fileOperationsService = new FakeFileOperationsService
        {
            MoveToDirectoryResult = @"C:\temp\target\program.axm"
        };
        var documentService = new FakeDocumentFileService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            fileOperationsService: fileOperationsService);

        var sourceNode = new FileTreeNode
        {
            FullPath = @"C:\temp\program.axm",
            DisplayName = "program.axm",
            IsDirectory = false
        };
        viewModel.OpenFilesPaneNodeCommand.Execute(sourceNode);

        viewModel.CutFilesPaneNodeCommand.Execute(sourceNode);
        viewModel.FilesPane.SelectedNode = new FileTreeNode
        {
            FullPath = @"C:\temp\target",
            DisplayName = "target",
            IsDirectory = true
        };

        viewModel.PasteIntoFilesPaneCommand.Execute(null);

        Assert.Equal(@"C:\temp\program.axm", fileOperationsService.LastMoveSourcePath);
        Assert.Equal(@"C:\temp\target", fileOperationsService.LastMoveDestinationDirectoryPath);
        Assert.Equal(@"C:\temp\target\program.axm", viewModel.Documents.ActiveDocument!.FilePath);
        Assert.False(viewModel.PasteIntoFilesPaneCommand.CanExecute(null));
    }

    [Fact]
    public void RenameFilesPaneNodeCommand_StartsInlineRenameForSelectedNode()
    {
        var viewModel = CreateViewModel();

        var sourceNode = new FileTreeNode
        {
            FullPath = @"C:\temp\program.axm",
            DisplayName = "program.axm",
            IsDirectory = false
        };
        viewModel.FilesPane.SelectedNode = sourceNode;

        viewModel.RenameFilesPaneNodeCommand.Execute(null);

        Assert.True(sourceNode.IsRenaming);
        Assert.Equal("program.axm", sourceNode.RenameText);
    }

    [Fact]
    public void CommitFilesPaneNodeRename_RenamesSelectedNodeAndRetargetsOpenDocument()
    {
        var fileOperationsService = new FakeFileOperationsService
        {
            RenameResult = @"C:\temp\renamed.axm"
        };
        var documentService = new FakeDocumentFileService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            fileOperationsService: fileOperationsService);

        var sourceNode = new FileTreeNode
        {
            FullPath = @"C:\temp\program.axm",
            DisplayName = "program.axm",
            IsDirectory = false
        };
        viewModel.OpenFilesPaneNodeCommand.Execute(sourceNode);
        viewModel.FilesPane.SelectedNode = sourceNode;
        viewModel.RenameFilesPaneNodeCommand.Execute(null);
        sourceNode.RenameText = "renamed.axm";

        viewModel.CommitFilesPaneNodeRename(sourceNode);

        Assert.Equal(@"C:\temp\program.axm", fileOperationsService.LastRenameSourcePath);
        Assert.Equal("renamed.axm", fileOperationsService.LastRenameName);
        Assert.Equal(@"C:\temp\renamed.axm", viewModel.Documents.ActiveDocument!.FilePath);
    }

    [Fact]
    public void RenameFilesPaneNodeCommand_PreservesExpandedTreeStateAfterReload()
    {
        var rootNode = CreateDirectoryNode(@"C:\", "Disk (C:)", isDrive: true, isExpanded: true);
        var tempNode = CreateDirectoryNode(@"C:\temp", "temp", isExpanded: true);
        var sourceFileNode = new FileTreeNode
        {
            FullPath = @"C:\temp\program.axm",
            DisplayName = "program.axm",
            IsDirectory = false
        };
        tempNode.Children.Add(sourceFileNode);
        rootNode.Children.Add(tempNode);

        var reloadedRootNode = CreateDirectoryNode(@"C:\", "Disk (C:)", isDrive: true, isLoaded: false);
        var reloadedTempNode = CreateDirectoryNode(@"C:\temp", "temp", isLoaded: false);
        var fileTreeService = new FakeFileTreeService([rootNode])
        {
            NextNodes = [reloadedRootNode]
        };
        fileTreeService.SetChildren(@"C:\", [reloadedTempNode]);
        fileTreeService.SetChildren(
            @"C:\temp",
            [new FileTreeNode
            {
                FullPath = @"C:\temp\renamed.axm",
                DisplayName = "renamed.axm",
                IsDirectory = false
            }]);
        var fileOperationsService = new FakeFileOperationsService
        {
            RenameResult = @"C:\temp\renamed.axm"
        };
        var viewModel = CreateViewModel(
            fileTreeService: fileTreeService,
            fileOperationsService: fileOperationsService);

        viewModel.FilesPane.SelectedNode = sourceFileNode;

        viewModel.RenameFilesPaneNodeCommand.Execute(null);
        sourceFileNode.RenameText = "renamed.axm";
        viewModel.CommitFilesPaneNodeRename(sourceFileNode);

        var restoredRootNode = Assert.Single(viewModel.FilesPane.Nodes);
        Assert.True(restoredRootNode.IsExpanded);
        var restoredTempNode = Assert.Single(restoredRootNode.Children);
        Assert.True(restoredTempNode.IsExpanded);
        Assert.Equal(@"C:\temp\renamed.axm", viewModel.FilesPane.SelectedNode!.FullPath);
    }

    [Fact]
    public void MoveFilesPaneNodeToRecycleBinCommand_ConfirmsAndDeletesSelectedFile()
    {
        var promptService = new FakeDialogPromptService
        {
            ConfirmationResult = true
        };
        var fileOperationsService = new FakeFileOperationsService();
        var documentService = new FakeDocumentFileService();
        var viewModel = CreateViewModel(
            documentService: documentService,
            promptService: promptService,
            fileOperationsService: fileOperationsService);

        var sourceNode = new FileTreeNode
        {
            FullPath = @"C:\temp\program.axm",
            DisplayName = "program.axm",
            IsDirectory = false
        };
        viewModel.OpenFilesPaneNodeCommand.Execute(sourceNode);
        viewModel.FilesPane.SelectedNode = sourceNode;

        viewModel.MoveFilesPaneNodeToRecycleBinCommand.Execute(null);

        Assert.Equal("Удалить", promptService.LastConfirmationTitle);
        Assert.Contains("Удалить файл", promptService.LastConfirmationMessage, StringComparison.Ordinal);
        Assert.Equal(@"C:\temp\program.axm", fileOperationsService.LastRecycleBinPath);
        Assert.Single(viewModel.Documents.Documents);
        Assert.True(viewModel.Documents.ActiveDocument!.IsUntitled);
    }

    [Fact]
    public void FileTreeNode_IsExpanded_RaisesPropertyChanged()
    {
        var node = new FileTreeNode();
        string? changedPropertyName = null;
        node.PropertyChanged += (_, e) => changedPropertyName = e.PropertyName;

        node.IsExpanded = true;

        Assert.Equal(nameof(FileTreeNode.IsExpanded), changedPropertyName);
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void FilesPaneShowHiddenFilesChange_ReloadsTreeWithHiddenFlag()
    {
        var fileTreeService = new FakeFileTreeService(
            [new FileTreeNode { FullPath = @"C:\temp\visible.txt", DisplayName = "visible.txt" }]);
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(
            recentFilesService: recentFilesService,
            fileTreeService: fileTreeService);

        viewModel.FilesPane.ShowHiddenFiles = true;

        Assert.True(fileTreeService.LastShowHiddenFiles);
        Assert.True(viewModel.FilesPane.ShowHiddenFiles);
        Assert.Contains("скрыт", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowFilesPaneNodePropertiesCommand_UsesPromptServiceForSelectedNode()
    {
        var promptService = new FakeDialogPromptService();
        var viewModel = CreateViewModel(promptService: promptService);
        var node = new FileTreeNode
        {
            FullPath = @"C:\temp\file.txt",
            DisplayName = "file.txt",
            IsDirectory = false
        };

        viewModel.FilesPane.SelectedNode = node;
        viewModel.ShowFilesPaneNodePropertiesCommand.Execute(null);

        Assert.Equal("Свойства", promptService.LastInfoTitle);
        Assert.Contains("Имя:", promptService.LastInfoMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("file.txt", promptService.LastInfoMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(@"C:\temp\file.txt", promptService.LastInfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutCommand_ShowsAboutDialogThroughPromptService()
    {
        var promptService = new FakeDialogPromptService();
        var viewModel = CreateViewModel(promptService: promptService);

        viewModel.AboutCommand.Execute(null);

        Assert.Equal("О программе Axiom IDE", promptService.LastInfoTitle);
        Assert.Contains("Axiom IDE", promptService.LastInfoMessage, StringComparison.Ordinal);
        Assert.Contains("оболочка интерпретатора", promptService.LastInfoMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Окно «О программе» закрыто.", viewModel.StatusBar.StatusText);
    }

    [Fact]
    public void AboutCommand_UsesEnglishTextWhenEnglishLanguageSelected()
    {
        var promptService = new FakeDialogPromptService();
        var viewModel = CreateViewModel(promptService: promptService);

        viewModel.UseEnglishLanguageCommand.Execute(null);
        viewModel.AboutCommand.Execute(null);

        Assert.Equal("About Axiom IDE", promptService.LastInfoTitle);
        Assert.Contains("Axiom IDE", promptService.LastInfoMessage, StringComparison.Ordinal);
        Assert.Contains("interpreter shell", promptService.LastInfoMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("About dialog closed.", viewModel.StatusBar.StatusText);
    }

    [Fact]
    public void WindowTitle_IncludesActiveDocumentNameAndDirtyMarker()
    {
        var viewModel = CreateViewModel();

        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 0");
        var activeDocument = viewModel.Documents.ActiveDocument;
        Assert.NotNull(activeDocument);
        activeDocument.SourceText = "PRINT 1";

        Assert.Equal("program.axm* - Axiom IDE", viewModel.WindowTitle);
    }

    [Fact]
    public void OpenManualCommand_OpensManualThroughExternalNavigationService()
    {
        var promptService = new FakeDialogPromptService();
        var navigationService = new FakeExternalNavigationService();
        var expectedPath = @"C:\repo\docs\manual\index.html";
        var viewModel = CreateViewModel(
            promptService: promptService,
            externalNavigationService: navigationService,
            manualPathResolver: () => expectedPath);

        viewModel.OpenManualCommand.Execute(null);

        Assert.Equal(expectedPath, navigationService.LastOpenedPath);
        Assert.Equal("Руководство открыто.", viewModel.StatusBar.StatusText);
        Assert.Equal(string.Empty, promptService.LastErrorTitle);
    }

    [Fact]
    public void OpenManualCommand_WhenManualIsMissing_ShowsPromptError()
    {
        var promptService = new FakeDialogPromptService();
        var navigationService = new FakeExternalNavigationService();
        var viewModel = CreateViewModel(
            promptService: promptService,
            externalNavigationService: navigationService,
            manualPathResolver: static () => null);

        viewModel.OpenManualCommand.Execute(null);

        Assert.Null(navigationService.LastOpenedPath);
        Assert.Equal("Открыть руководство", promptService.LastErrorTitle);
        Assert.Contains(@"docs\manual\index.html", promptService.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Руководство не найдено.", viewModel.StatusBar.StatusText);
    }

    [Fact]
    public void ResolveManualPath_FindsRepositoryManual()
    {
        var resolvedPath = MainWindowViewModel.ResolveManualPath();

        Assert.NotNull(resolvedPath);
        Assert.EndsWith(Path.Combine("docs", "manual", "index.html"), resolvedPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(resolvedPath));
    }

    [Fact]
    public void FilesPaneVisibilityChange_PersistsShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        viewModel.FilesPane.IsVisible = false;

        Assert.False(recentFilesService.LastIsFilesPaneVisible);
        Assert.Equal(IdeWindowState.Normal, recentFilesService.LastWindowState);
    }

    [Fact]
    public void StatusBarVisibilityChange_PersistsShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal, true));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        viewModel.StatusBar.IsVisible = false;

        Assert.False(recentFilesService.LastIsStatusBarVisible);
        Assert.Equal(IdeWindowState.Normal, recentFilesService.LastWindowState);
    }

    [Fact]
    public void CloseFilesPaneCommand_HidesPaneAndPersistsShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        Assert.True(viewModel.CloseFilesPaneCommand.CanExecute(null));

        viewModel.CloseFilesPaneCommand.Execute(null);

        Assert.False(viewModel.FilesPane.IsVisible);
        Assert.False(viewModel.CloseFilesPaneCommand.CanExecute(null));
        Assert.False(recentFilesService.LastIsFilesPaneVisible);
        Assert.Equal(IdeWindowState.Normal, recentFilesService.LastWindowState);
    }

    [Fact]
    public void PersistShellState_UpdatesPersistence()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        viewModel.PersistShellState(IdeWindowState.Minimized);

        Assert.Equal(IdeWindowState.Minimized, viewModel.PreferredWindowState);
        Assert.True(recentFilesService.LastIsFilesPaneVisible);
        Assert.True(recentFilesService.LastIsStatusBarVisible);
        Assert.Equal(IdeWindowState.Minimized, recentFilesService.LastWindowState);
    }

    [Fact]
    public void ThemeCommands_SwitchThemeAndPersistShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var traceService = new FakeIdeTraceService();
        var viewModel = CreateViewModel(recentFilesService: recentFilesService, traceService: traceService);

        viewModel.UseDarkThemeCommand.Execute(null);

        Assert.Equal(IdeTheme.Dark, viewModel.SelectedTheme);
        Assert.False(viewModel.IsLightThemeEnabled);
        Assert.True(viewModel.IsDarkThemeEnabled);
        Assert.Equal(IdeTheme.Dark, recentFilesService.LastTheme);
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "theme.switch"
                && entry.Message.Contains("theme=Dark", StringComparison.Ordinal));

        viewModel.UseLightThemeCommand.Execute(null);

        Assert.Equal(IdeTheme.Light, viewModel.SelectedTheme);
        Assert.True(viewModel.IsLightThemeEnabled);
        Assert.False(viewModel.IsDarkThemeEnabled);
        Assert.Equal(IdeTheme.Light, recentFilesService.LastTheme);
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "theme.switch"
                && entry.Message.Contains("theme=Light", StringComparison.Ordinal));
    }

    [Fact]
    public void ToggleSmartIdentifierCommand_SwitchesPreferenceAndPersistsShellState()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var traceService = new FakeIdeTraceService();
        var viewModel = CreateViewModel(recentFilesService: recentFilesService, traceService: traceService);

        viewModel.ToggleSmartIdentifierCommand.Execute(null);

        Assert.True(viewModel.IsSmartIdentifierEnabled);
        Assert.True(recentFilesService.LastSmartIdentifierEnabled);
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "editor.smart-identifier.toggle"
                && entry.Message.Contains("enabled=True", StringComparison.Ordinal));

        viewModel.ToggleSmartIdentifierCommand.Execute(null);

        Assert.False(viewModel.IsSmartIdentifierEnabled);
        Assert.False(recentFilesService.LastSmartIdentifierEnabled);
    }

    [Fact]
    public void LanguageCommands_SwitchLanguageAndPersistShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        viewModel.UseEnglishLanguageCommand.Execute(null);

        Assert.Equal(IdeLanguage.English, viewModel.SelectedLanguage);
        Assert.False(viewModel.IsRussianLanguageEnabled);
        Assert.True(viewModel.IsEnglishLanguageEnabled);
        Assert.Equal(IdeLanguage.English, recentFilesService.LastLanguage);

        viewModel.UseRussianLanguageCommand.Execute(null);

        Assert.Equal(IdeLanguage.Russian, viewModel.SelectedLanguage);
        Assert.True(viewModel.IsRussianLanguageEnabled);
        Assert.False(viewModel.IsEnglishLanguageEnabled);
        Assert.Equal(IdeLanguage.Russian, recentFilesService.LastLanguage);
    }

    [Fact]
    public void ExecutionModeCommands_SwitchModeAndPersistShellPreferences()
    {
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(recentFilesService: recentFilesService);

        viewModel.UseInterpreterExecutionModeCommand.Execute(null);

        Assert.Equal(Axiom.Execution.ExecutionMode.Interpreter, viewModel.SelectedExecutionMode);
        Assert.True(viewModel.IsInterpreterExecutionModeEnabled);
        Assert.False(viewModel.IsBytecodeExecutionModeEnabled);
        Assert.Equal(Axiom.Execution.ExecutionMode.Interpreter, recentFilesService.LastPreferredExecutionMode);

        viewModel.UseBytecodeExecutionModeCommand.Execute(null);

        Assert.Equal(Axiom.Execution.ExecutionMode.Bytecode, viewModel.SelectedExecutionMode);
        Assert.False(viewModel.IsInterpreterExecutionModeEnabled);
        Assert.True(viewModel.IsBytecodeExecutionModeEnabled);
        Assert.Equal(Axiom.Execution.ExecutionMode.Bytecode, recentFilesService.LastPreferredExecutionMode);
    }

    [Fact]
    public void RunProgram_PassesSelectedExecutionModeToRunService()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.UseInterpreterExecutionModeCommand.Execute(null);
        viewModel.RunCommand.Execute(null);

        Assert.Equal(Axiom.Execution.ExecutionMode.Interpreter, runService.LastExecutionMode);
    }

    [Fact]
    public void RunProgram_IgnoresSourceBreakpointLines()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.SetSourceBreakpointLines([3, 2, 3]);
        viewModel.RunCommand.Execute(null);

        Assert.Equal([3, 2], viewModel.SourceBreakpointLines);
        Assert.Empty(runService.LastSourceBreakpointLines);
    }

    [Fact]
    public void RunOrContinueCommand_PassesSourceBreakpointLinesToRunService()
    {
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1", false)
        };
        var runService = new FakeProgramRunService();
        var viewModel = CreateViewModel(documentService: documentService, runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1");

        viewModel.SetSourceBreakpointLines([3, 2, 3]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.Equal([3, 2], viewModel.SourceBreakpointLines);
        Assert.Equal([3, 2], runService.LastSourceBreakpointLines);
    }

    [Fact]
    public void SourceBreakpointLines_AreStoredPerDocument()
    {
        var viewModel = CreateViewModel();
        PrepareSavedActiveDocument(viewModel, @"C:\temp\first.axm", "PRINT 1");
        var firstDocument = viewModel.Documents.ActiveDocument!;

        viewModel.SetSourceBreakpointLines([2, 4]);
        viewModel.NewCommand.Execute(null);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\second.axm", "PRINT 2");
        var secondDocument = viewModel.Documents.ActiveDocument!;

        Assert.Empty(viewModel.SourceBreakpointLines);

        viewModel.ToggleSourceBreakpointLine(7);

        Assert.Equal([7], secondDocument.SourceBreakpointLines);
        Assert.Equal([2, 4], firstDocument.SourceBreakpointLines);

        viewModel.Documents.ActiveDocument = firstDocument;

        Assert.Equal([2, 4], viewModel.SourceBreakpointLines);

        viewModel.Documents.ActiveDocument = secondDocument;

        Assert.Equal([7], viewModel.SourceBreakpointLines);
    }

    [Fact]
    public void StoppedRunState_IsShownOnlyForStoppedDocument()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\first.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\first.axm", "PRINT 1\r\nPRINT 2");
        var firstDocument = viewModel.Documents.ActiveDocument!;

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.NotNull(viewModel.StoppedRunState);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.True(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.True(viewModel.StepOverCommand.CanExecute(null));
        Assert.True(viewModel.StepIntoCommand.CanExecute(null));
        Assert.True(viewModel.StepOutCommand.CanExecute(null));
        Assert.True(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.True(viewModel.RestartDebuggingCommand.CanExecute(null));

        viewModel.NewCommand.Execute(null);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\second.axm", "PRINT 10");

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.False(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.True(viewModel.StepOverCommand.CanExecute(null));
        Assert.True(viewModel.StepIntoCommand.CanExecute(null));
        Assert.False(viewModel.StepOutCommand.CanExecute(null));
        Assert.False(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.False(viewModel.RestartDebuggingCommand.CanExecute(null));

        viewModel.Documents.ActiveDocument = firstDocument;

        Assert.True(viewModel.HasStoppedRun);
        Assert.NotNull(viewModel.StoppedRunState);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.True(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.True(viewModel.StepOverCommand.CanExecute(null));
        Assert.True(viewModel.StepIntoCommand.CanExecute(null));
        Assert.True(viewModel.StepOutCommand.CanExecute(null));
        Assert.True(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.True(viewModel.RestartDebuggingCommand.CanExecute(null));
    }

    [Fact]
    public void RunOrContinueCommand_OnOtherDocumentStartsNewRunAndClearsStoppedState()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\first.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\first.axm", "PRINT 1\r\nPRINT 2");
        var firstDocument = viewModel.Documents.ActiveDocument!;

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);
        Assert.True(viewModel.HasStoppedRun);

        viewModel.NewCommand.Execute(null);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\second.axm", "PRINT 10");
        documentService.DocumentToLoad = new DocumentFileContent(@"C:\temp\second.axm", "PRINT 10", false);

        viewModel.RunOrContinueCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.Equal("10", viewModel.BottomPane.OutputText);

        viewModel.Documents.ActiveDocument = firstDocument;

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
    }

    [Fact]
    public void RunOrContinueCommand_WhenBreakpointStops_StoresStoppedRunState()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.NotNull(viewModel.StoppedRunState);
        Assert.True(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.Equal(2, viewModel.StoppedRunState!.Snapshot.SourceLocation.Line);
        Assert.Equal("1", viewModel.BottomPane.OutputText);
        Assert.Contains("2", viewModel.StatusBar.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void RunCommand_IgnoresBreakpointsAndCompletesProgram()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.Equal("1" + Environment.NewLine + "2", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void ContinueRunCommand_ResumesStoredStoppedRunAndClearsStateWhenProgramCompletes()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);

        viewModel.ContinueRunCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.False(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.Equal("1" + Environment.NewLine + "2" + Environment.NewLine + "3", viewModel.BottomPane.OutputText);
        Assert.Contains("успеш", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StepOverCommand_AdvancesStoppedRunToNextSourceLine()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.True(viewModel.StepOverCommand.CanExecute(null));

        viewModel.StepOverCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(3, viewModel.StoppedRunLine);
        Assert.True(viewModel.StepOverCommand.CanExecute(null));
        Assert.Equal("1" + Environment.NewLine + "2", viewModel.BottomPane.OutputText);
        Assert.Contains("3", viewModel.StatusBar.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void StepOverCommand_WhenNoStoppedRun_StartsDebuggingAtFirstInstruction()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var sourceText = "PRINT \"first\"\r\nPRINT \"second\"";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", sourceText, false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", sourceText);

        Assert.True(viewModel.StepOverCommand.CanExecute(null));

        viewModel.StepOverCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(1, viewModel.StoppedRunLine);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);

        viewModel.StepOverCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.Equal("first", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void StepIntoCommand_WhenNoStoppedRun_StartsDebuggingAtFirstInstruction()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var sourceText = "FUNCTION AddOne(value AS INTEGER) AS INTEGER\r\n    RETURN value + 1\r\nEND FUNCTION\r\n\r\nPRINT AddOne(4)";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", sourceText, false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", sourceText);

        Assert.True(viewModel.StepIntoCommand.CanExecute(null));

        viewModel.StepIntoCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(5, viewModel.StoppedRunLine);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);

        viewModel.StepIntoCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);
    }

    [Fact]
    public void StepOverCommand_ShowsPendingTrailingCommaOutputWhileStopped()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "FOR i = 1 TO 2\r\n    PRINT i,\r\nNEXT", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "FOR i = 1 TO 2\r\n    PRINT i,\r\nNEXT");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);

        viewModel.StepOverCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal("1 ", viewModel.BottomPane.OutputText);

        viewModel.StepOverCommand.Execute(null);
        viewModel.StepOverCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal("1 2 ", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void StepIntoCommand_AdvancesStoppedRunIntoCalledRoutine()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "FUNCTION AddOne(value AS INTEGER) AS INTEGER\r\n    RETURN value + 1\r\nEND FUNCTION\r\n\r\nPRINT AddOne(4)", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "FUNCTION AddOne(value AS INTEGER) AS INTEGER\r\n    RETURN value + 1\r\nEND FUNCTION\r\n\r\nPRINT AddOne(4)");

        viewModel.SetSourceBreakpointLines([5]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.StepIntoCommand.CanExecute(null));

        viewModel.StepIntoCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void StepOutCommand_ContinuesStoppedRoutineBackToCaller()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var sourceText = "FUNCTION AddOne(value AS INTEGER) AS INTEGER\r\n    RETURN value + 1\r\nEND FUNCTION\r\n\r\nPRINT AddOne(4)\r\nPRINT \"done\"";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", sourceText, false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", sourceText);

        viewModel.SetSourceBreakpointLines([5]);
        viewModel.RunOrContinueCommand.Execute(null);
        viewModel.StepIntoCommand.Execute(null);

        Assert.True(viewModel.StepOutCommand.CanExecute(null));

        viewModel.StepOutCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.NotNull(viewModel.StoppedRunState);
        Assert.Equal(1, viewModel.StoppedRunState!.Snapshot.CallDepth);
    }

    [Fact]
    public void StepBackCommand_RestoresPreviousStoppedLineWhenSnapshotExists()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var sourceText = "DIM value AS INTEGER = 1\r\nvalue = value + 1\r\nPRINT value";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", sourceText, false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", sourceText);

        Assert.False(viewModel.StepBackCommand.CanExecute(null));

        viewModel.SetSourceBreakpointLines([3]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.StepBackCommand.CanExecute(null));

        viewModel.StepBackCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.NotNull(viewModel.StoppedRunState);
        Assert.Equal(2, viewModel.StoppedRunState!.Snapshot.SourceLocation.Line);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void StepBackCommand_WhenOutputBecomesEmpty_ClearsBottomPaneOutput()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var sourceText = "DIM result AS INTEGER\r\nPRINT \"main: start\"\r\nPRINT \"after\"";
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", sourceText, false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", sourceText);

        viewModel.SetSourceBreakpointLines([3]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.Equal("main: start", viewModel.BottomPane.OutputText);
        Assert.True(viewModel.StepBackCommand.CanExecute(null));

        viewModel.StepBackCommand.Execute(null);
        Assert.Equal("main: start", viewModel.BottomPane.OutputText);

        viewModel.StepBackCommand.Execute(null);
        Assert.Equal(string.Empty, viewModel.BottomPane.OutputText);
        Assert.Equal(2, viewModel.StoppedRunState!.Snapshot.SourceLocation.Line);
    }

    [Fact]
    public void RunOrContinueCommand_ContinuesStoppedRunWhenStopStateExists()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Equal("1" + Environment.NewLine + "2", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void StopDebuggingCommand_ClearsStoppedRunWithoutContinuing()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.True(viewModel.HasStoppedRun);

        viewModel.StopDebuggingCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.False(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.False(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.Equal("1", viewModel.BottomPane.OutputText);
        Assert.Contains("отлад", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestartDebuggingCommand_RerunsActiveStoppedDocumentWithCurrentSource()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2");

        viewModel.SetSourceBreakpointLines([2]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal("1", viewModel.BottomPane.OutputText);
        Assert.True(viewModel.RestartDebuggingCommand.CanExecute(null));

        viewModel.Documents.ActiveDocument!.SourceText = "PRINT 9\r\nPRINT 2";
        viewModel.RestartDebuggingCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.Equal(2, viewModel.StoppedRunLine);
        Assert.Equal("9", viewModel.BottomPane.OutputText);
        Assert.Equal("PRINT 9\r\nPRINT 2", documentService.LastSavedSourceText);
    }

    [Fact]
    public void ToggleCurrentSourceBreakpointCommand_TogglesProvidedLine()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.ToggleCurrentSourceBreakpointCommand.CanExecute(4));

        viewModel.ToggleCurrentSourceBreakpointCommand.Execute(4);
        viewModel.ToggleCurrentSourceBreakpointCommand.Execute("4");

        Assert.Empty(viewModel.SourceBreakpointLines);
        Assert.Contains("4", viewModel.StatusBar.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearSourceBreakpointLines_RemovesStoredBreakpointLines()
    {
        var viewModel = CreateViewModel();

        viewModel.SetSourceBreakpointLines([2, 4]);
        viewModel.ClearSourceBreakpointLines();

        Assert.Empty(viewModel.SourceBreakpointLines);
    }

    [Fact]
    public void ClearAllSourceBreakpointsCommand_RemovesActiveDocumentBreakpoints()
    {
        var viewModel = CreateViewModel();

        viewModel.SetSourceBreakpointLines([2, 4]);

        Assert.True(viewModel.ClearAllSourceBreakpointsCommand.CanExecute(null));

        viewModel.ClearAllSourceBreakpointsCommand.Execute(null);

        Assert.Empty(viewModel.SourceBreakpointLines);
        Assert.False(viewModel.ClearAllSourceBreakpointsCommand.CanExecute(null));
        Assert.Contains("2", viewModel.StatusBar.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearAllSourceBreakpointsCommand_StopsStoppedDebugging()
    {
        var consoleService = new ProgramConsoleService();
        var runService = new ProgramRunService(new Axiom.Execution.AxiomExecutionPipeline(), consoleService);
        var documentService = new FakeDocumentFileService
        {
            DocumentToLoad = new DocumentFileContent(@"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3", false)
        };
        var viewModel = CreateViewModel(
            documentService: documentService,
            consoleService: consoleService,
            runService: runService);
        PrepareSavedActiveDocument(viewModel, @"C:\temp\program.axm", "PRINT 1\r\nPRINT 2\r\nPRINT 3");

        viewModel.SetSourceBreakpointLines([2, 3]);
        viewModel.RunOrContinueCommand.Execute(null);

        Assert.True(viewModel.HasStoppedRun);
        Assert.True(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.Equal("1", viewModel.BottomPane.OutputText);

        viewModel.ClearAllSourceBreakpointsCommand.Execute(null);

        Assert.Empty(viewModel.SourceBreakpointLines);
        Assert.False(viewModel.HasStoppedRun);
        Assert.Null(viewModel.StoppedRunState);
        Assert.Null(viewModel.StoppedRunLine);
        Assert.False(viewModel.ContinueRunCommand.CanExecute(null));
        Assert.False(viewModel.StopDebuggingCommand.CanExecute(null));
        Assert.Contains("отлад", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);

        viewModel.RunOrContinueCommand.Execute(null);

        Assert.False(viewModel.HasStoppedRun);
        Assert.Equal("1" + Environment.NewLine + "2" + Environment.NewLine + "3", viewModel.BottomPane.OutputText);
    }

    [Fact]
    public void ToggleSourceBreakpointLine_AddsAndRemovesLine()
    {
        var viewModel = CreateViewModel();

        viewModel.ToggleSourceBreakpointLine(7);
        viewModel.ToggleSourceBreakpointLine(3);
        viewModel.ToggleSourceBreakpointLine(7);

        Assert.Equal([3], viewModel.SourceBreakpointLines);
    }

    [Fact]
    public void ToggleSourceBreakpointLine_RejectsInvalidLine()
    {
        var viewModel = CreateViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.ToggleSourceBreakpointLine(0));
    }

    [Fact]
    public void RestoreShellPreferences_RestoresExecutionMode()
    {
        var viewModel = CreateViewModel(
            recentFilesService: new FakeRecentFilesService(
                new RecentFilesSnapshot(
                    [],
                    @"C:\temp",
                    true,
                    IdeWindowState.Normal,
                    true,
                    IdeTheme.Light,
                    IdeLanguage.Russian,
                    false,
                    Axiom.Execution.ExecutionMode.Interpreter)));

        Assert.Equal(Axiom.Execution.ExecutionMode.Interpreter, viewModel.SelectedExecutionMode);
        Assert.True(viewModel.IsInterpreterExecutionModeEnabled);
    }

    [Fact]
    public void OpenRecentCommand_RemovesMissingRecentEntry()
    {
        var missingPath = @"C:\temp\missing.axm";
        var documentService = new FakeDocumentFileService
        {
            LoadDocumentException = new FileNotFoundException("Missing", missingPath)
        };
        var recentFilesService = new FakeRecentFilesService(
            new RecentFilesSnapshot([missingPath], @"C:\temp", true, IdeWindowState.Normal));
        var viewModel = CreateViewModel(documentService, recentFilesService);
        var recentItem = Assert.Single(viewModel.RecentFiles);

        viewModel.OpenRecentCommand.Execute(recentItem);

        Assert.Equal(missingPath, recentFilesService.RemovedFilePath);
        Assert.Empty(viewModel.RecentFiles);
        Assert.False(viewModel.HasRecentFiles);
    }

    [Fact]
    public void OpenIdeEventLogFileCommand_OpensCurrentLogFile()
    {
        var logsDirectoryPath = Path.Combine(Path.GetTempPath(), "Axiom.Ide.EventLog.Shell.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logsDirectoryPath);
        var logPath = Path.Combine(logsDirectoryPath, "ide-events-current.jsonl");
        File.WriteAllText(logPath, "{}");
        var eventLogService = new FakeIdeEventLogServiceForShell
        {
            LogsDirectoryPathValue = logsDirectoryPath,
            CurrentSessionLogPathValue = logPath
        };
        var externalNavigationService = new FakeExternalNavigationService();
        var viewModel = CreateViewModel(
            eventLogService: eventLogService,
            externalNavigationService: externalNavigationService);

        try
        {
            viewModel.OpenIdeEventLogFileCommand.Execute(null);

            Assert.True(eventLogService.BeginSessionCalled);
            Assert.True(eventLogService.FlushCalled);
            Assert.Equal(eventLogService.CurrentSessionLogPathValue, externalNavigationService.LastOpenedPath);
            Assert.Contains("журнал", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(logsDirectoryPath, recursive: true);
        }
    }

    [Fact]
    public void ShowIdeEventLogViewerCommand_OpensViewerForCurrentLogFile()
    {
        var logsDirectoryPath = Path.Combine(Path.GetTempPath(), "Axiom.Ide.EventLog.Viewer.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logsDirectoryPath);
        var logPath = Path.Combine(logsDirectoryPath, "ide-events-current.jsonl");
        File.WriteAllText(logPath, "{}");
        var eventLogService = new FakeIdeEventLogServiceForShell
        {
            LogsDirectoryPathValue = logsDirectoryPath,
            CurrentSessionLogPathValue = logPath
        };
        var viewerService = new FakeIdeEventLogViewerService();
        var viewModel = CreateViewModel(
            eventLogService: eventLogService,
            eventLogViewerService: viewerService);

        try
        {
            viewModel.ShowIdeEventLogViewerCommand.Execute(null);

            Assert.True(eventLogService.BeginSessionCalled);
            Assert.True(eventLogService.FlushCalled);
            Assert.Equal(logPath, viewerService.LastShownPath);
            Assert.Contains("журнал", viewModel.StatusBar.StatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(logsDirectoryPath, recursive: true);
        }
    }

    [Fact]
    public void OpenIdeEventLogFolderCommand_OpensLogsDirectory()
    {
        var logsDirectoryPath = Path.Combine(Path.GetTempPath(), "Axiom.Ide.EventLog.Shell.Tests", Guid.NewGuid().ToString("N"));
        var eventLogService = new FakeIdeEventLogServiceForShell
        {
            LogsDirectoryPathValue = logsDirectoryPath,
            CurrentSessionLogPathValue = Path.Combine(logsDirectoryPath, "ide-events-current.jsonl")
        };
        var externalNavigationService = new FakeExternalNavigationService();
        var viewModel = CreateViewModel(
            eventLogService: eventLogService,
            externalNavigationService: externalNavigationService);

        try
        {
            viewModel.OpenIdeEventLogFolderCommand.Execute(null);

            Assert.True(eventLogService.BeginSessionCalled);
            Assert.True(eventLogService.FlushCalled);
            Assert.Equal(eventLogService.LogsDirectoryPath, externalNavigationService.LastOpenedPath);
        }
        finally
        {
            if (Directory.Exists(logsDirectoryPath))
            {
                Directory.Delete(logsDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void ClearIdeEventLogsCommand_DeletesInactiveLogsAfterConfirmation()
    {
        var eventLogService = new FakeIdeEventLogServiceForShell
        {
            LogsDirectoryPathValue = @"C:\logs",
            DeletedInactiveLogs = 3
        };
        var promptService = new FakeDialogPromptService { ConfirmationResult = true };
        var viewModel = CreateViewModel(
            promptService: promptService,
            eventLogService: eventLogService);

        viewModel.ClearIdeEventLogsCommand.Execute(null);

        Assert.True(eventLogService.DeleteInactiveLogsCalled);
        Assert.Contains("3", viewModel.StatusBar.StatusText, StringComparison.Ordinal);
        Assert.Contains("Удалить", promptService.LastConfirmationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatusBarChanges_LogOnlyVisibilityChanges()
    {
        var eventLogService = new FakeIdeEventLogServiceForShell();
        var viewModel = CreateViewModel(eventLogService: eventLogService);
        eventLogService.Entries.Clear();

        viewModel.StatusBar.StatusText = "changed";
        viewModel.StatusBar.PathText = @"C:\Axiom\sample.axm";
        viewModel.StatusBar.LineColumnText = "Ln 10, Col 2";
        viewModel.StatusBar.CompilationProgressPercent = 50;

        Assert.DoesNotContain(eventLogService.Entries, entry => entry.EventName == "status-bar.changed");

        viewModel.StatusBar.IsVisible = false;

        var statusEvent = Assert.Single(eventLogService.Entries, entry => entry.EventName == "status-bar.changed");
        Assert.Equal("IsVisible", statusEvent.CommandName);
    }

    [Fact]
    public void ReportEditorCaretActivity_FlushesSingleBurstEvent()
    {
        var eventLogService = new FakeIdeEventLogServiceForShell();
        var viewModel = CreateViewModel(eventLogService: eventLogService);
        var document = viewModel.Documents.ActiveDocument!;

        viewModel.ReportEditorCaretActivity(document, 5, 3);
        viewModel.ReportEditorCaretActivity(document, 6, 9);
        viewModel.ReportEditorCaretActivity(document, 7, 11);
        viewModel.FlushEditorActivityEvents();

        var burst = Assert.Single(eventLogService.Entries, entry => entry.EventName == "editor.caret.burst");
        Assert.Equal("editor", burst.Category);
        Assert.Equal("Caret", burst.CommandName);
        Assert.NotNull(burst.Properties);
        Assert.Equal("3", burst.Properties!["eventsCount"]);
        Assert.Equal("7", burst.Properties["line"]);
        Assert.Equal("11", burst.Properties["column"]);
        Assert.True(burst.Properties.ContainsKey("durationMs"));
    }

    [Fact]
    public void ReportEditorCaretActivity_FlushesAutomaticallyOnThreshold()
    {
        var eventLogService = new FakeIdeEventLogServiceForShell();
        var viewModel = CreateViewModel(eventLogService: eventLogService);
        var document = viewModel.Documents.ActiveDocument!;

        for (var index = 0; index < 20; index++)
        {
            viewModel.ReportEditorCaretActivity(document, index + 1, 1);
        }

        var burst = Assert.Single(eventLogService.Entries, entry => entry.EventName == "editor.caret.burst");
        Assert.NotNull(burst.Properties);
        Assert.Equal("20", burst.Properties!["eventsCount"]);
        Assert.Equal("20", burst.Properties["line"]);
    }

    private static MainWindowViewModel CreateViewModel(
        IDocumentFileService? documentService = null,
        IRecentFilesService? recentFilesService = null,
        IFileTreeService? fileTreeService = null,
        ProgramConsoleService? consoleService = null,
        IProgramRunService? runService = null,
        IDialogPromptService? promptService = null,
        ICodeFormattingService? codeFormattingService = null,
        AxiomSmartIdentifierService? smartIdentifierService = null,
        IFileOperationsService? fileOperationsService = null,
        IIdeTraceService? traceService = null,
        IExternalNavigationService? externalNavigationService = null,
        IIdeEventLogService? eventLogService = null,
        IIdeEventLogViewerService? eventLogViewerService = null,
        Func<string?>? manualPathResolver = null)
    {
        return new MainWindowViewModel(
            runService ?? new FakeProgramRunService(),
            documentService ?? new FakeDocumentFileService(),
            recentFilesService ?? new FakeRecentFilesService(RecentFilesSnapshot.Empty),
            consoleService ?? new ProgramConsoleService(),
            promptService ?? new FakeDialogPromptService(),
            codeFormattingService ?? new AxiomCodeFormattingService(),
            smartIdentifierService ?? new AxiomSmartIdentifierService(),
            fileTreeService ?? new FakeFileTreeService([]),
            fileOperationsService ?? new FakeFileOperationsService(),
            traceService ?? NullIdeTraceService.Instance,
            externalNavigationService ?? new FakeExternalNavigationService(),
            eventLogService,
            eventLogViewerService,
            manualPathResolver: manualPathResolver);
    }

    private static FileTreeNode CreateDirectoryNode(
        string fullPath,
        string displayName,
        bool isDrive = false,
        bool isExpanded = false,
        bool isLoaded = true)
    {
        var node = new FileTreeNode
        {
            FullPath = fullPath,
            DisplayName = displayName,
            IsDirectory = true,
            IsDrive = isDrive,
            IsExpanded = isExpanded,
            IsLoaded = isLoaded
        };

        return node;
    }

    private static void PrepareSavedActiveDocument(MainWindowViewModel viewModel, string filePath, string sourceText)
    {
        var document = viewModel.Documents.ActiveDocument!;
        document.FilePath = filePath;
        document.SourceText = sourceText;
        document.MarkAsSaved(filePath);
    }

    private static void WaitForBackgroundUiWork(MainWindowViewModel viewModel, int timeoutMs = 5000)
    {
        var stopwatch = Stopwatch.StartNew();

        while (viewModel.StatusBar.IsRunning && stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            Thread.Sleep(10);
        }

        Assert.False(viewModel.StatusBar.IsRunning);
    }

    private sealed class FakeProgramRunService : IProgramRunService
    {
        public Func<string, ProgramCompilationResult>? OnCompile { get; set; }
        public Func<string, ProgramRunResult>? OnRun { get; set; }
        public Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult>? OnContinue { get; set; }
        public Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult>? OnStepOver { get; set; }
        public Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult>? OnStepInto { get; set; }
        public Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult>? OnStepOut { get; set; }
        public Func<Axiom.Runtime.BytecodeVmStopState, ProgramRunResult>? OnStepBack { get; set; }
        public int CompileCallCount { get; private set; }
        public int CallCount { get; private set; }
        public int ContinueCallCount { get; private set; }
        public int StepOverCallCount { get; private set; }
        public int StepIntoCallCount { get; private set; }
        public int StepOutCallCount { get; private set; }
        public int StepBackCallCount { get; private set; }
        public string? LastCompiledSourceText { get; private set; }
        public string? LastSourceText { get; private set; }
        public Axiom.Execution.ExecutionMode LastExecutionMode { get; private set; } = Axiom.Execution.ExecutionMode.Bytecode;
        public IReadOnlyList<int> LastSourceBreakpointLines { get; private set; } = [];
        public bool LastStopBeforeFirstInstruction { get; private set; }

        public ProgramCompilationResult Compile(string sourceText, IProgress<int>? progress = null)
        {
            CompileCallCount++;
            LastCompiledSourceText = sourceText;
            progress?.Report(100);
            return OnCompile?.Invoke(sourceText) ?? new ProgramCompilationResult(ProgramCompilationStatus.Success, []);
        }

        public ProgramRunResult Run(
            string sourceText,
            Axiom.Execution.ExecutionMode mode = Axiom.Execution.ExecutionMode.Bytecode,
            IEnumerable<int>? sourceBreakpointLines = null,
            bool stopBeforeFirstInstruction = false)
        {
            CallCount++;
            LastSourceText = sourceText;
            LastExecutionMode = mode;
            LastSourceBreakpointLines = sourceBreakpointLines?.ToArray() ?? [];
            LastStopBeforeFirstInstruction = stopBeforeFirstInstruction;
            return OnRun?.Invoke(sourceText) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult Continue(Axiom.Runtime.BytecodeVmStopState stopState)
        {
            ContinueCallCount++;
            return OnContinue?.Invoke(stopState) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepOver(Axiom.Runtime.BytecodeVmStopState stopState)
        {
            StepOverCallCount++;
            return OnStepOver?.Invoke(stopState) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepInto(Axiom.Runtime.BytecodeVmStopState stopState)
        {
            StepIntoCallCount++;
            return OnStepInto?.Invoke(stopState) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepOut(Axiom.Runtime.BytecodeVmStopState stopState)
        {
            StepOutCallCount++;
            return OnStepOut?.Invoke(stopState) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepBack(Axiom.Runtime.BytecodeVmStopState stopState)
        {
            StepBackCallCount++;
            return OnStepBack?.Invoke(stopState) ?? new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }
    }

    private sealed class FakeDocumentFileService : IDocumentFileService
    {
        public DocumentFileContent? DocumentToLoad { get; set; }
        public Exception? LoadDocumentException { get; init; }
        public string? SaveDocumentResult { get; set; }
        public string? LoadedDocumentPath { get; private set; }
        public string? LastOpenInitialDirectoryPath { get; private set; }
        public string? LastSavedFilePath { get; private set; }
        public string? LastSaveInitialDirectoryPath { get; private set; }
        public string? LastSavedSourceText { get; private set; }

        public DocumentFileContent LoadDocument(string filePath)
        {
            if (LoadDocumentException is not null)
            {
                throw LoadDocumentException;
            }

            LoadedDocumentPath = filePath;
            return DocumentToLoad
                ?? new DocumentFileContent(filePath, LastSavedSourceText ?? "PRINT 1", false);
        }

        public DocumentFileContent? OpenDocument(string? initialDirectoryPath)
        {
            LastOpenInitialDirectoryPath = initialDirectoryPath;
            return null;
        }

        public string? SaveDocument(string? currentFilePath, string sourceText, string? initialDirectoryPath)
        {
            LastSaveInitialDirectoryPath = initialDirectoryPath;
            LastSavedFilePath = SaveDocumentResult ?? currentFilePath;
            LastSavedSourceText = sourceText;

            if (!string.IsNullOrWhiteSpace(LastSavedFilePath))
            {
                DocumentToLoad = new DocumentFileContent(LastSavedFilePath, sourceText, false);
            }

            return LastSavedFilePath;
        }
    }

    private sealed class FakeRecentFilesService : IRecentFilesService
    {
        private RecentFilesSnapshot _snapshot;

        public FakeRecentFilesService(RecentFilesSnapshot snapshot)
        {
            _snapshot = snapshot;
            LastIsFilesPaneVisible = snapshot.IsFilesPaneVisible;
            LastIsStatusBarVisible = snapshot.IsStatusBarVisible;
            LastWindowState = snapshot.WindowState;
            LastTheme = snapshot.Theme;
            LastLanguage = snapshot.Language;
            LastSmartIdentifierEnabled = snapshot.SmartIdentifierEnabled;
        }

        public string? RememberedFilePath { get; private set; }
        public string? RemovedFilePath { get; private set; }
        public bool LastIsFilesPaneVisible { get; private set; } = true;
        public bool LastIsStatusBarVisible { get; private set; } = true;
        public IdeWindowState LastWindowState { get; private set; } = IdeWindowState.Normal;
        public IdeTheme LastTheme { get; private set; } = IdeTheme.Light;
        public IdeLanguage LastLanguage { get; private set; } = IdeLanguage.Russian;
        public bool LastSmartIdentifierEnabled { get; private set; }
        public Axiom.Execution.ExecutionMode LastPreferredExecutionMode { get; private set; } = Axiom.Execution.ExecutionMode.Bytecode;

        public RecentFilesSnapshot GetSnapshot() => _snapshot;

        public RecentFilesSnapshot RememberFile(string filePath)
        {
            RememberedFilePath = filePath;
            _snapshot = new RecentFilesSnapshot(
                [filePath],
                Path.GetDirectoryName(filePath),
                _snapshot.IsFilesPaneVisible,
                _snapshot.WindowState,
                _snapshot.IsStatusBarVisible,
                _snapshot.Theme,
                _snapshot.Language,
                _snapshot.SmartIdentifierEnabled,
                _snapshot.PreferredExecutionMode);
            return _snapshot;
        }

        public RecentFilesSnapshot RemoveFile(string filePath)
        {
            RemovedFilePath = filePath;
            _snapshot = RecentFilesSnapshot.Empty with
            {
                IsFilesPaneVisible = _snapshot.IsFilesPaneVisible,
                IsStatusBarVisible = _snapshot.IsStatusBarVisible,
                WindowState = _snapshot.WindowState,
                Theme = _snapshot.Theme,
                Language = _snapshot.Language,
                SmartIdentifierEnabled = _snapshot.SmartIdentifierEnabled,
                PreferredExecutionMode = _snapshot.PreferredExecutionMode
            };
            return _snapshot;
        }

        public RecentFilesSnapshot UpdateShellPreferences(
            bool isFilesPaneVisible,
            bool isStatusBarVisible,
            IdeWindowState windowState,
            IdeTheme theme,
            IdeLanguage language,
            bool smartIdentifierEnabled = false,
            Axiom.Execution.ExecutionMode preferredExecutionMode = Axiom.Execution.ExecutionMode.Bytecode)
        {
            LastIsFilesPaneVisible = isFilesPaneVisible;
            LastIsStatusBarVisible = isStatusBarVisible;
            LastWindowState = windowState;
            LastTheme = theme;
            LastLanguage = language;
            LastSmartIdentifierEnabled = smartIdentifierEnabled;
            LastPreferredExecutionMode = preferredExecutionMode;
            _snapshot = _snapshot with
            {
                IsFilesPaneVisible = isFilesPaneVisible,
                IsStatusBarVisible = isStatusBarVisible,
                WindowState = windowState,
                Theme = theme,
                Language = language,
                SmartIdentifierEnabled = smartIdentifierEnabled,
                PreferredExecutionMode = preferredExecutionMode
            };
            return _snapshot;
        }
    }

    private sealed class FakeDialogPromptService : IDialogPromptService
    {
        public bool? SaveDiscardCancelResult { get; init; } = false;
        public bool ConfirmationResult { get; init; } = true;
        public string? TextInputResult { get; init; }
        public string? LastPromptedFileName { get; private set; }
        public string LastInfoMessage { get; private set; } = string.Empty;
        public string LastInfoTitle { get; private set; } = string.Empty;
        public string LastErrorMessage { get; private set; } = string.Empty;
        public string LastErrorTitle { get; private set; } = string.Empty;
        public string LastConfirmationMessage { get; private set; } = string.Empty;
        public string LastConfirmationTitle { get; private set; } = string.Empty;
        public string LastTextInputMessage { get; private set; } = string.Empty;
        public string LastTextInputTitle { get; private set; } = string.Empty;
        public string LastTextInputInitialValue { get; private set; } = string.Empty;

        public bool? ShowSaveDiscardCancel(string fileName)
        {
            LastPromptedFileName = fileName;
            return SaveDiscardCancelResult;
        }

        public bool ShowConfirmation(string message, string? title = null)
        {
            LastConfirmationMessage = message;
            LastConfirmationTitle = title ?? "Axiom IDE";
            return ConfirmationResult;
        }

        public string? ShowTextInput(string message, string? title = null, string initialValue = "", string? confirmButtonText = null)
        {
            LastTextInputMessage = message;
            LastTextInputTitle = title ?? "Axiom IDE";
            LastTextInputInitialValue = initialValue;
            return TextInputResult;
        }

        public void ShowInfo(string message, string? title = null)
        {
            LastInfoMessage = message;
            LastInfoTitle = title ?? "Axiom IDE";
        }

        public void ShowError(string message, string? title = null)
        {
            LastErrorMessage = message;
            LastErrorTitle = title ?? "Axiom IDE";
        }
    }

    private sealed class FakeExternalNavigationService : IExternalNavigationService
    {
        public string? LastOpenedPath { get; private set; }

        public void OpenPath(string path)
        {
            LastOpenedPath = path;
        }
    }

    private sealed class FakeFileTreeService : IFileTreeService
    {
        private readonly Dictionary<string, IReadOnlyList<FileTreeNode>> _childrenByPath = new(StringComparer.OrdinalIgnoreCase);

        public FakeFileTreeService(IReadOnlyList<FileTreeNode> initialNodes)
        {
            NextNodes = initialNodes;
            NextChildren = [];
        }

        public string? LastRootPath { get; private set; }
        public string? LastExpandedPath { get; private set; }
        public bool LastShowHiddenFiles { get; private set; }
        public IReadOnlyList<FileTreeNode> NextNodes { get; set; }
        public IReadOnlyList<FileTreeNode> NextChildren { get; set; }

        public void SetChildren(string directoryPath, IReadOnlyList<FileTreeNode> children) =>
            _childrenByPath[directoryPath] = children;

        public IReadOnlyList<FileTreeNode> BuildTree(string rootPath, bool showHiddenFiles = false)
        {
            LastRootPath = rootPath;
            LastShowHiddenFiles = showHiddenFiles;
            return NextNodes;
        }

        public IReadOnlyList<FileTreeNode> BuildChildNodes(string directoryPath, bool showHiddenFiles = false)
        {
            LastExpandedPath = directoryPath;
            LastShowHiddenFiles = showHiddenFiles;
            return _childrenByPath.TryGetValue(directoryPath, out var children)
                ? children
                : NextChildren;
        }
    }

    private sealed class FakeFileOperationsService : IFileOperationsService
    {
        public string? LastCreateFileDirectoryPath { get; private set; }
        public string? LastCreateFileName { get; private set; }
        public string? LastCreateDirectoryParentPath { get; private set; }
        public string? LastCreateDirectoryName { get; private set; }
        public string? LastRenameSourcePath { get; private set; }
        public string? LastRenameName { get; private set; }
        public string? LastCopySourcePath { get; private set; }
        public string? LastCopyDestinationDirectoryPath { get; private set; }
        public string? LastMoveSourcePath { get; private set; }
        public string? LastMoveDestinationDirectoryPath { get; private set; }
        public string? LastRecycleBinPath { get; private set; }
        public string? RenameResult { get; init; }
        public string? CopyToDirectoryResult { get; init; }
        public string? MoveToDirectoryResult { get; init; }
        public HashSet<string> ExistingPaths { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string CreateEmptyFile(string directoryPath, string fileName)
        {
            LastCreateFileDirectoryPath = directoryPath;
            LastCreateFileName = fileName;
            var path = Path.Combine(directoryPath, fileName);
            ExistingPaths.Add(path);
            return path;
        }

        public string CreateDirectory(string directoryPath, string directoryName)
        {
            LastCreateDirectoryParentPath = directoryPath;
            LastCreateDirectoryName = directoryName;
            var path = Path.Combine(directoryPath, directoryName);
            ExistingPaths.Add(path);
            return path;
        }

        public string Rename(string sourcePath, string newName)
        {
            LastRenameSourcePath = sourcePath;
            LastRenameName = newName;
            var result = RenameResult ?? Path.Combine(Path.GetDirectoryName(sourcePath)!, newName);
            ExistingPaths.Remove(sourcePath);
            ExistingPaths.Add(result);
            return result;
        }

        public string CopyToDirectory(string sourcePath, string destinationDirectoryPath)
        {
            LastCopySourcePath = sourcePath;
            LastCopyDestinationDirectoryPath = destinationDirectoryPath;
            var result = CopyToDirectoryResult ?? Path.Combine(destinationDirectoryPath, Path.GetFileName(sourcePath));
            ExistingPaths.Add(result);
            return result;
        }

        public string MoveToDirectory(string sourcePath, string destinationDirectoryPath)
        {
            LastMoveSourcePath = sourcePath;
            LastMoveDestinationDirectoryPath = destinationDirectoryPath;
            var result = MoveToDirectoryResult ?? Path.Combine(destinationDirectoryPath, Path.GetFileName(sourcePath));
            ExistingPaths.Remove(sourcePath);
            ExistingPaths.Add(result);
            return result;
        }

        public void SendToRecycleBin(string path)
        {
            LastRecycleBinPath = path;
            ExistingPaths.Remove(path);
        }

        public bool Exists(string path) => ExistingPaths.Count == 0 || ExistingPaths.Contains(path);
    }

    private sealed class FakeIdeTraceService : IIdeTraceService
    {
        public List<(string Category, string Message)> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public void BeginSession(string sessionName)
        {
        }

        public void Write(string category, string message)
        {
            Entries.Add((category, message));
        }

        public void WriteException(string category, Exception exception, string? note = null)
        {
            Entries.Add((category, note ?? exception.Message));
        }
    }

    private sealed class FakeIdeEventLogServiceForShell : IIdeEventLogService
    {
        public string LogsDirectoryPathValue { get; init; } = string.Empty;
        public string CurrentSessionLogPathValue { get; init; } = string.Empty;
        public int DeletedInactiveLogs { get; init; }
        public bool BeginSessionCalled { get; private set; }
        public bool FlushCalled { get; private set; }
        public bool DeleteInactiveLogsCalled { get; private set; }
        public List<IdeEventLogEntry> Entries { get; } = [];

        public string LogsDirectoryPath => LogsDirectoryPathValue;

        public string CurrentSessionLogPath => CurrentSessionLogPathValue;

        public string SessionId => "test-session";

        public void BeginSession(string sessionName)
        {
            BeginSessionCalled = true;
        }

        public void Write(IdeEventLogEntry entry)
        {
            Entries.Add(entry);
        }

        public void Flush()
        {
            FlushCalled = true;
        }

        public int DeleteInactiveLogs()
        {
            DeleteInactiveLogsCalled = true;
            return DeletedInactiveLogs;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeIdeEventLogViewerService : IIdeEventLogViewerService
    {
        public string? LastShownPath { get; private set; }

        public void Show(string logFilePath)
        {
            LastShownPath = logFilePath;
        }
    }

    private static void SetProgramConsoleProperty(ProgramConsoleService consoleService, string propertyName, object? value)
    {
        var property = typeof(ProgramConsoleService).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(property);
        property!.SetValue(consoleService, value);
    }
}


