using Axiom.Execution;
using Axiom.Formatting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Files;
using Axiom.Ide.Models;
using Axiom.Ide.Output;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.Documents;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Prompts;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Tests;

public sealed class DocumentOpenFormattingTests
{
    [Fact]
    public void ApplySmartIdentifier_NormalizesActiveEditedDocument()
    {
        var vm = CreateViewModel(string.Empty, @"C:\temp\edited.axm");
        if (!vm.IsSmartIdentifierEnabled) vm.ToggleSmartIdentifierCommand.Execute(null);
        var document = vm.Documents.CreateNewDocument();
        document.SourceText = "DIM MyScore AS INTEGER\r\nmyscore = 10\r\nPRINT MYSCORE";

        vm.ApplySmartIdentifierToActiveDocument();

        Assert.Equal("DIM MyScore AS INTEGER\r\nMyScore = 10\r\nPRINT MyScore", document.SourceText);
        Assert.True(document.IsDirty);
    }
    [Fact]
    public void DocumentOpenFormatting_FormatsInterpolatedStringExpressionsOnOpen()
    {
        const string source = "DIM name AS STRING = \"Алиса\"\r\nprint $\"Длина имени: {len(name)} символов\"\r\n";
        var viewModel = CreateViewModel(source, @"C:\temp\sample.axm");

        var document = viewModel.Documents.OpenDocumentFromPath(@"C:\temp\sample.axm", out _);

        Assert.NotNull(document);
        Assert.Contains("PRINT $\"Длина имени: {LEN(name)} символов\"", document.SourceText, StringComparison.Ordinal);
        Assert.DoesNotContain("{len(", document.SourceText, StringComparison.Ordinal);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void DocumentOpenFormatting_KeepsUnsavedEditsWhenReopeningSameFile()
    {
        var viewModel = CreateViewModel("print 1\r\n", @"C:\temp\sample.axm");
        var document = viewModel.Documents.OpenDocumentFromPath(@"C:\temp\sample.axm", out _);
        Assert.NotNull(document);
        document.SourceText = "PRINT 1 ' черновик";

        viewModel.Documents.OpenDocumentFromPath(@"C:\temp\sample.axm", out _);

        Assert.Equal("PRINT 1 ' черновик", document.SourceText);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void DocumentOpenFormatting_SkipsNonAxmDocuments()
    {
        const string source = "print \"не форматировать\"\r\n";
        var viewModel = CreateViewModel(source, @"C:\temp\readme.txt");

        var document = viewModel.Documents.OpenDocumentFromPath(@"C:\temp\readme.txt", out _);

        Assert.NotNull(document);
        Assert.Equal(source, document.SourceText);
        Assert.False(document.IsDirty);
    }

    private static MainWindowViewModel CreateViewModel(string sourceText, string filePath)
    {
        return new MainWindowViewModel(
            new StubProgramRunService(),
            new StubDocumentFileService(sourceText, filePath),
            new StubRecentFilesService(),
            new ProgramConsoleService(),
            new StubDialogPromptService(),
            new AxiomCodeFormattingService(),
            new AxiomSmartIdentifierService(new IdentifierNormalizationService()),
            new StubFileTreeService(),
            new StubFileOperationsService(),
            NullIdeTraceService.Instance,
            new StubExternalNavigationService());
    }

    private sealed class StubProgramRunService : IProgramRunService
    {
        public ProgramCompilationResult Compile(string sourceText, IProgress<int>? progress = null)
        {
            return new ProgramCompilationResult(ProgramCompilationStatus.Success, []);
        }

        public ProgramRunResult Run(
            string sourceText,
            ExecutionMode mode = ExecutionMode.Bytecode,
            IEnumerable<int>? sourceBreakpointLines = null,
            bool stopBeforeFirstInstruction = false,
            string? sourceFilePath = null,
            TimeSpan? timeout = null,
            Axiom.Runtime.IRuntimeCancellationProvider? cancellationProvider = null)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult Continue(IProgramDebuggerState debuggerState)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepOver(IProgramDebuggerState debuggerState)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepInto(IProgramDebuggerState debuggerState)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepOut(IProgramDebuggerState debuggerState)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }

        public ProgramRunResult StepBack(IProgramDebuggerState debuggerState)
        {
            return new ProgramRunResult(ProgramRunStatus.Success, [], string.Empty);
        }
    }

    private sealed class StubDocumentFileService(string sourceText, string filePath) : IDocumentFileService
    {
        public DocumentFileContent? OpenDocument(string? initialDirectoryPath)
        {
            return null;
        }

        public DocumentFileContent LoadDocument(string filePath)
        {
            return new DocumentFileContent(filePath, sourceText, false);
        }

        public string? SaveDocument(string? currentFilePath, string sourceText, string? initialDirectoryPath)
        {
            return currentFilePath;
        }
    }

    private sealed class StubRecentFilesService : IRecentFilesService
    {
        public RecentFilesSnapshot GetSnapshot()
        {
            return RecentFilesSnapshot.Empty;
        }

        public RecentFilesSnapshot RememberFile(string filePath)
        {
            return RecentFilesSnapshot.Empty;
        }

        public RecentFilesSnapshot RemoveFile(string filePath)
        {
            return RecentFilesSnapshot.Empty;
        }

        public RecentFilesSnapshot UpdateShellPreferences(
            bool isFilesPaneVisible,
            bool isStatusBarVisible,
            IdeWindowState windowState,
            IdeTheme theme,
            IdeLanguage language,
            bool smartIdentifierEnabled = false,
            ExecutionMode preferredExecutionMode = ExecutionMode.Bytecode)
        {
            return RecentFilesSnapshot.Empty;
        }
    }

    private sealed class StubDialogPromptService : IDialogPromptService
    {
        public bool? ShowSaveDiscardCancel(string fileName)
        {
            return false;
        }

        public bool ShowConfirmation(string message, string? title = null)
        {
            return true;
        }

        public string? ShowTextInput(string message, string? title = null, string initialValue = "", string? confirmButtonText = null)
        {
            return null;
        }

        public void ShowInfo(string message, string? title = null)
        {
        }

        public void ShowError(string message, string? title = null)
        {
        }
    }

    private sealed class StubFileTreeService : IFileTreeService
    {
        public IReadOnlyList<FileTreeNode> BuildTree(string rootPath, bool showHiddenFiles = false)
        {
            return [];
        }

        public IReadOnlyList<FileTreeNode> BuildChildNodes(string directoryPath, bool showHiddenFiles = false)
        {
            return [];
        }
    }

    private sealed class StubFileOperationsService : IFileOperationsService
    {
        public string CreateEmptyFile(string directoryPath, string fileName)
        {
            return Path.Combine(directoryPath, fileName);
        }

        public string CreateDirectory(string directoryPath, string directoryName)
        {
            return Path.Combine(directoryPath, directoryName);
        }

        public string Rename(string sourcePath, string newName)
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath)!, newName);
        }

        public string CopyToDirectory(string sourcePath, string destinationDirectoryPath)
        {
            return Path.Combine(destinationDirectoryPath, Path.GetFileName(sourcePath));
        }

        public string MoveToDirectory(string sourcePath, string destinationDirectoryPath)
        {
            return Path.Combine(destinationDirectoryPath, Path.GetFileName(sourcePath));
        }

        public void SendToRecycleBin(string path)
        {
        }

        public bool Exists(string path)
        {
            return false;
        }
    }

    private sealed class StubExternalNavigationService : IExternalNavigationService
    {
        public void OpenPath(string path)
        {
        }
    }
}
