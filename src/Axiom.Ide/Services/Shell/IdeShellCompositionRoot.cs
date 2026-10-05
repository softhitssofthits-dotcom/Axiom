using Axiom.Execution;
using Axiom.Formatting;
using Axiom.Ide.Editor.Identifiers;
using Axiom.Ide.Editor.StructuralEditing;
using Axiom.Ide.Files;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.Documents;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Prompts;
using Axiom.Ide.ViewModels;


namespace Axiom.Ide.Services.Shell;

public sealed class IdeShellCompositionRoot
{
    public IdeShellCompositionRoot(IIdeTraceService? traceService = null)
    {
        TraceService = traceService ?? new FileIdeTraceService();
        EventLogService = new FileIdeEventLogService();
        EventLogViewerService = new WpfIdeEventLogViewerService();
    }

    public IIdeTraceService TraceService { get; }

    public IIdeEventLogService EventLogService { get; }

    public IIdeEventLogViewerService EventLogViewerService { get; }

    public MainWindow CreateMainWindow()
    {
        var pipeline = new AxiomExecutionPipeline();
        var consoleService = new ProgramConsoleService(TraceService, EventLogService);
        var runService = new ProgramRunService(pipeline, consoleService, TraceService, EventLogService);
        var documentFileService = new DocumentFileService();
        var recentFilesService = new RecentFilesService();
        var promptService = new WpfDialogPromptService();
        var codeFormattingService = new AxiomCodeFormattingService();
        var smartIdentifierService = new AxiomSmartIdentifierService(
            new IdentifierNormalizationService());
        var fileTreeService = new FileTreeService();
        var fileOperationsService = new FileOperationsService();
        var externalNavigationService = new ExternalNavigationService();
        var blockEditingService = new AxiomBlockEditingService(codeFormattingService);

        var viewModel = new MainWindowViewModel(
            runService,
            documentFileService,
            recentFilesService,
            consoleService,
            promptService,
            codeFormattingService,
            smartIdentifierService,
            fileTreeService,
            fileOperationsService,
            TraceService,
            externalNavigationService,
            eventLogService: EventLogService,
            eventLogViewerService: EventLogViewerService);

        return new MainWindow(codeFormattingService, smartIdentifierService, blockEditingService)
        {
            DataContext = viewModel
        };
    }
}
