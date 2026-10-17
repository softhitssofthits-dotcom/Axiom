using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;
using System.Diagnostics;
using System.IO;

namespace Axiom.Ide.Services.Execution;

public sealed class ProgramRunService : IProgramRunService
{
    private readonly AxiomExecutionPipeline _pipeline;
    private readonly IProgramConsoleService _consoleService;
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private CompiledProgram? _compiledProgram;
    private readonly IProgramMessageProvider _messages;

    public ProgramRunService(
        AxiomExecutionPipeline pipeline,
        IProgramConsoleService consoleService,
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null,
        IProgramMessageProvider? messageProvider = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(consoleService);

        _messages = messageProvider ?? new DefaultProgramMessageProvider();
        _pipeline = pipeline;
        _consoleService = consoleService;
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
    }

    public ProgramCompilationResult Compile(string sourceText, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        try
        {
            var compilationResult = _pipeline.Compile(new CompilationRequest(sourceText, progress));
            var diagnostics = compilationResult.Diagnostics
                .Concat(compilationResult.BackendDiagnostics)
                .DistinctBy(static diagnostic => (diagnostic.Code, diagnostic.Location.Line, diagnostic.Location.Column, diagnostic.Location.Span, diagnostic.Message))
                .Select(ProgramDiagnosticItem.FromDiagnostic)
                .ToArray();

            _compiledProgram = compilationResult.Succeeded
                ? compilationResult.CompiledProgram
                : null;

            _traceService.Write(
                "compile.pipeline",
                $"status={compilationResult.Status} | diagnostics={diagnostics.Length} | cache={(compilationResult.Succeeded ? "updated" : "cleared")}");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Info,
                Category = "commands",
                EventName = "compile.completed",
                Source = nameof(ProgramRunService),
                CommandName = "Compile",
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["status"] = compilationResult.Status.ToString(),
                    ["diagnostics"] = diagnostics.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["compiledCache"] = compilationResult.Succeeded ? "updated" : "cleared"
                }
            });

            return new ProgramCompilationResult(MapCompilationStatus(compilationResult.Status), diagnostics);
        }
        catch (Exception exception)
        {
            _compiledProgram = null;
            _traceService.WriteException("compile.exception", exception, "Unexpected ProgramRunService compilation failure.");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Error,
                Category = "commands",
                EventName = "compile.failed",
                Source = nameof(ProgramRunService),
                CommandName = "Compile",
                TextPreview = exception.Message
            });

            return new ProgramCompilationResult(
                ProgramCompilationStatus.InfrastructureFailure,
                [ProgramDiagnosticItem.CreateInfrastructure(
                    "IDE0003",
                    _messages.Format(
                        "Compile.Infrastructure.UnexpectedError",
                        "Unexpected compilation error: {0}",
                        exception.Message))]);
        }
    }

    public ProgramRunResult Run(
        string sourceText,

        IEnumerable<int>? sourceBreakpointLines = null,
        bool stopBeforeFirstInstruction = false,
        string? sourceFilePath = null,
        TimeSpan? timeout = null,
        IRuntimeCancellationProvider? cancellationProvider = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        try
        {
            var runBaseDirectory = ResolveResourceBaseDirectory(sourceFilePath);
            _consoleService.ResourceBaseDirectory = runBaseDirectory;
            _consoleService.BeginSession();
            var inputProvider = new IdeInputProvider(_consoleService);
            var usesCompiledCache = _compiledProgram is not null
                && string.Equals(_compiledProgram.SourceText, sourceText, StringComparison.Ordinal);
            var preparationDuration = TimeSpan.Zero;

            CompiledProgram compiledProgram;

            if (usesCompiledCache)
            {
                compiledProgram = _compiledProgram!;
            }
            else
            {
                var preparationStopwatch = Stopwatch.StartNew();
                var compilationResult = _pipeline.Compile(new CompilationRequest(sourceText));
                preparationStopwatch.Stop();
                preparationDuration = compilationResult.FrontendPreparationDuration == TimeSpan.Zero
                    ? preparationStopwatch.Elapsed
                    : compilationResult.FrontendPreparationDuration;

                if (!compilationResult.Succeeded)
                {
                    _compiledProgram = null;
                    var failedDiagnostics = compilationResult.Diagnostics
                        .Concat(compilationResult.BackendDiagnostics)
                        .DistinctBy(static diagnostic => (diagnostic.Code, diagnostic.Location.Line, diagnostic.Location.Column, diagnostic.Location.Span, diagnostic.Message))
                        .Select(ProgramDiagnosticItem.FromDiagnostic)
                        .ToArray();

                    _traceService.Write(
                        "run.pipeline",
                        $"status={compilationResult.Status} | diagnostics={failedDiagnostics.Length} | outputLines=0 | compiledCache=miss");
                    _eventLogService.Write(new IdeEventLogEntry
                    {
                        Level = IdeEventLevel.Warning,
                        Category = "debugger",
                        EventName = "run.stopped",
                        Source = nameof(ProgramRunService),
                        CommandName = "Run",
                        Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                        {
                            ["status"] = compilationResult.Status.ToString(),
                            ["diagnostics"] = failedDiagnostics.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        }
                    });

                    _consoleService.EndSession();
                    return new ProgramRunResult(
                        MapCompilationFailureStatus(compilationResult.Status),
                        failedDiagnostics,
                        string.Empty,
                        preparationDuration: preparationDuration,
                        executionDuration: TimeSpan.Zero,
                        usedCompiledCache: false);
                }

                compiledProgram = compilationResult.CompiledProgram!;
                _compiledProgram = compiledProgram;
            }

            using var timeoutCancellation = timeout is { } t && t > TimeSpan.Zero
                ? new TimeoutCancellationProvider(t)
                : null;

            var effectiveCancellation = cancellationProvider ?? timeoutCancellation;

            var executionResult = _pipeline.RunCompiled(
                compiledProgram,
                new ExecutionRequest(
                    sourceText,
                    inputProvider: inputProvider,
                    outputWriter: _consoleService,
                    cancellationProvider: effectiveCancellation,
                    sourceBreakpointLines: sourceBreakpointLines,
                    stopBeforeFirstInstruction: stopBeforeFirstInstruction,
                    resourceBaseDirectory: runBaseDirectory));

            var diagnostics = executionResult.Diagnostics
                .Select(ProgramDiagnosticItem.FromDiagnostic)
                .ToArray();
            var outputText = BuildOutputText(executionResult.Output, executionResult.PendingOutput);
            var executionDuration = executionResult.RuntimeExecutionDuration;
            if (!executionResult.Stopped && _consoleService.IsScreenActive)
            {
                _consoleService.DeactivateScreen();
            }

            if (!executionResult.Stopped)
            {
                _consoleService.EndSession();
            }

            IProgramDebuggerState? debuggerState = executionResult.StopState is not null
                ? CreateDebuggerAdapter(executionResult.StopState)
                : null;

            _traceService.Write(
                "run.pipeline",
                $"status={executionResult.Status} | actualEngine={executionResult.ActualEngine?.ToString() ?? "none"} | stopped={executionResult.Stopped} | diagnostics={diagnostics.Length} | outputLines={executionResult.Output.Count} | compiledCache={(usesCompiledCache ? "hit" : "miss")} | bytecodeCache={(executionResult.UsedBytecodeCache ? "hit" : "miss")} | preparationMs={preparationDuration.TotalMilliseconds:F3} | loweringMs={executionResult.LoweringDuration.TotalMilliseconds:F3} | bytecodeMs={executionResult.BytecodeEmissionDuration.TotalMilliseconds:F3} | executionMs={executionDuration.TotalMilliseconds:F3}");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = executionResult.Status == ExecutionStatus.Success ? IdeEventLevel.Info : IdeEventLevel.Warning,
                Category = "debugger",
                EventName = executionResult.Stopped ? "run.stopped" : "run.completed",
                Source = nameof(ProgramRunService),
                CommandName = "Run",
                DebugLine = debuggerState?.StopInfo.Line,
                Opcode = debuggerState?.StopInfo.Opcode,
                CallDepth = debuggerState?.StopInfo.CallDepth,
                CanStepBack = debuggerState?.StopInfo.CanStepBack,
                OutputLength = outputText.Length,
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["status"] = executionResult.Status.ToString(),
                    ["actualEngine"] = executionResult.ActualEngine?.ToString() ?? "none",
                    ["diagnostics"] = diagnostics.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["compiledCache"] = usesCompiledCache ? "hit" : "miss",
                    ["bytecodeCache"] = executionResult.UsedBytecodeCache ? "hit" : "miss"
                }
            });

            return new ProgramRunResult(
                MapStatus(executionResult.Status),
                diagnostics,
                outputText,
                preparationDuration: preparationDuration,
                executionDuration: executionDuration,
                usedCompiledCache: usesCompiledCache,
                actualEngine: executionResult.ActualEngine,
                loweringDuration: executionResult.LoweringDuration,
                bytecodeEmissionDuration: executionResult.BytecodeEmissionDuration,
                usedLoweredCache: executionResult.UsedLoweredCache,
                usedBytecodeCache: executionResult.UsedBytecodeCache,
                debuggerState: debuggerState);
        }
        catch (OperationCanceledException) when (cancellationProvider?.IsCancellationRequested == true || (_consoleService as IRuntimeCancellationProvider)?.IsCancellationRequested == true)
        {
            _consoleService.DeactivateScreen();
            _consoleService.EndSession();
            return new ProgramRunResult(ProgramRunStatus.Cancelled, [], GetConsoleOutputText(),
                actualEngine: ExecutionEngineKind.BytecodeVm);
        }
        catch (Exception exception)
        {
            if (_consoleService.IsScreenActive)
            {
                _consoleService.DeactivateScreen();
            }

            _consoleService.EndSession();

            _traceService.WriteException("run.exception", exception, "Unexpected ProgramRunService failure.");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Error,
                Category = "debugger",
                EventName = "run.failed",
                Source = nameof(ProgramRunService),
                CommandName = "Run",
                TextPreview = exception.Message
            });
            var diagnostics = new[]
            {
                ProgramDiagnosticItem.CreateInfrastructure(
                    "IDE0001",
                    _messages.Format(
                        "Run.Infrastructure.UnexpectedError",
                        "Unexpected execution error: {0}",
                        exception.Message))
            };

            return new ProgramRunResult(ProgramRunStatus.InfrastructureFailure, diagnostics, string.Empty);
        }
    }

    public ProgramRunResult Continue(IProgramDebuggerState debuggerState)
    {
        ArgumentNullException.ThrowIfNull(debuggerState);
        return debuggerState.Resume();
    }

    public ProgramRunResult StepOver(IProgramDebuggerState debuggerState)
    {
        ArgumentNullException.ThrowIfNull(debuggerState);
        return debuggerState.StepOver();
    }

    public ProgramRunResult StepInto(IProgramDebuggerState debuggerState)
    {
        ArgumentNullException.ThrowIfNull(debuggerState);
        return debuggerState.StepInto();
    }

    public ProgramRunResult StepOut(IProgramDebuggerState debuggerState)
    {
        ArgumentNullException.ThrowIfNull(debuggerState);
        return debuggerState.StepOut();
    }

    public ProgramRunResult StepBack(IProgramDebuggerState debuggerState)
    {
        ArgumentNullException.ThrowIfNull(debuggerState);
        return debuggerState.StepBack();
    }

    private IProgramDebuggerState CreateDebuggerAdapter(BytecodeVmStopState stopState)
    {
        return new BytecodeVmDebuggerAdapter(
            stopState,
            resume: state => ResumeStoppedRun(state, static s => s.Resume(), "run.continue", "run.continue.exception", "Unexpected ProgramRunService continuation failure."),
            stepOver: state => ResumeStoppedRun(state, static s => s.StepOver(), "run.step-over", "run.step-over.exception", "Unexpected ProgramRunService step-over failure."),
            stepInto: state => ResumeStoppedRun(state, static s => s.StepInto(), "run.step-into", "run.step-into.exception", "Unexpected ProgramRunService step-into failure."),
            stepOut: state => ResumeStoppedRun(state, static s => s.StepOut(), "run.step-out", "run.step-out.exception", "Unexpected ProgramRunService step-out failure."),
            stepBack: state => ResumeStoppedRun(state, static s => s.StepBack(), "run.step-back", "run.step-back.exception", "Unexpected ProgramRunService step-back failure."));
    }

    private ProgramRunResult ResumeStoppedRun(
        BytecodeVmStopState stopState,
        Func<BytecodeVmStopState, RuntimeResult> resume,
        string traceName,
        string exceptionTraceName,
        string exceptionTraceMessage)
    {
        ArgumentNullException.ThrowIfNull(stopState);
        ArgumentNullException.ThrowIfNull(resume);

        var executionStopwatch = Stopwatch.StartNew();

        try
        {
            var runtimeResult = resume(stopState);
            executionStopwatch.Stop();

            var diagnostics = runtimeResult.Diagnostics
                .Select(ProgramDiagnosticItem.FromDiagnostic)
                .ToArray();
            if (!runtimeResult.Stopped && _consoleService.IsScreenActive)
            {
                _consoleService.DeactivateScreen();
            }

            if (!runtimeResult.Stopped)
            {
                _consoleService.EndSession();
            }

            IProgramDebuggerState? debuggerState = runtimeResult.StopState is not null
                ? CreateDebuggerAdapter(runtimeResult.StopState)
                : null;

            _traceService.Write(
                traceName,
                $"status={(diagnostics.Length == 0 ? ProgramRunStatus.Success : ProgramRunStatus.RuntimeFailure)} | stopped={runtimeResult.Stopped} | diagnostics={diagnostics.Length} | executionMs={executionStopwatch.Elapsed.TotalMilliseconds:F3}");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = diagnostics.Length == 0 ? IdeEventLevel.Info : IdeEventLevel.Warning,
                Category = "debugger",
                EventName = traceName.Replace('.', '-'),
                Source = nameof(ProgramRunService),
                CommandName = traceName,
                DebugLine = debuggerState?.StopInfo.Line,
                Opcode = debuggerState?.StopInfo.Opcode,
                CallDepth = debuggerState?.StopInfo.CallDepth,
                CanStepBack = debuggerState?.StopInfo.CanStepBack,
                OutputLength = BuildOutputText(GetConsoleOutputText(), runtimeResult.PendingOutput).Length,
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["stopped"] = runtimeResult.Stopped.ToString(),
                    ["diagnostics"] = diagnostics.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }
            });

            return new ProgramRunResult(
                diagnostics.Length == 0 ? ProgramRunStatus.Success : ProgramRunStatus.RuntimeFailure,
                diagnostics,
                BuildOutputText(GetConsoleOutputText(), runtimeResult.PendingOutput),
                executionDuration: executionStopwatch.Elapsed,
                actualEngine: ExecutionEngineKind.BytecodeVm,
                debuggerState: debuggerState);
        }
        catch (Exception exception)
        {
            executionStopwatch.Stop();
            if (_consoleService.IsScreenActive)
            {
                _consoleService.DeactivateScreen();
            }

            _consoleService.EndSession();

            _traceService.WriteException(exceptionTraceName, exception, exceptionTraceMessage);
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Error,
                Category = "debugger",
                EventName = "resume.failed",
                Source = nameof(ProgramRunService),
                CommandName = traceName,
                TextPreview = exception.Message
            });
            var diagnostics = new[]
            {
                ProgramDiagnosticItem.CreateInfrastructure(
                    "IDE0004",
                    _messages.Format(
                        "Run.Infrastructure.UnexpectedContinueError",
                        "Unexpected continuation error: {0}",
                        exception.Message))
            };

            return new ProgramRunResult(
                ProgramRunStatus.InfrastructureFailure,
                diagnostics,
                GetConsoleOutputText(),
                executionDuration: executionStopwatch.Elapsed,
                actualEngine: ExecutionEngineKind.BytecodeVm);
        }
    }

    private string GetConsoleOutputText()
    {
        return _consoleService is ProgramConsoleService consoleService
            ? consoleService.OutputText
            : string.Empty;
    }

    private static string ResolveResourceBaseDirectory(string? sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return Environment.CurrentDirectory;
        }

        return Path.GetDirectoryName(Path.GetFullPath(sourceFilePath)) ?? Environment.CurrentDirectory;
    }

    private static string BuildOutputText(IReadOnlyList<string> output, string pendingOutput)
    {
        var flushedOutput = string.Join(Environment.NewLine, output);
        return BuildOutputText(flushedOutput, pendingOutput);
    }

    private static string BuildOutputText(string flushedOutput, string pendingOutput)
    {
        if (string.IsNullOrEmpty(pendingOutput))
        {
            return flushedOutput;
        }

        return string.IsNullOrEmpty(flushedOutput)
            ? pendingOutput
            : $"{flushedOutput}{Environment.NewLine}{pendingOutput}";
    }

    private static ProgramCompilationStatus MapCompilationStatus(CompilationStatus status)
    {
        return status switch
        {
            CompilationStatus.Success => ProgramCompilationStatus.Success,
            CompilationStatus.SyntaxFailure => ProgramCompilationStatus.SyntaxFailure,
            CompilationStatus.SemanticFailure => ProgramCompilationStatus.SemanticFailure,
            CompilationStatus.BackendFailure => ProgramCompilationStatus.BackendFailure,
            _ => ProgramCompilationStatus.InfrastructureFailure
        };
    }

    private static ProgramRunStatus MapCompilationFailureStatus(CompilationStatus status)
    {
        return status switch
        {
            CompilationStatus.SyntaxFailure => ProgramRunStatus.SyntaxFailure,
            CompilationStatus.SemanticFailure => ProgramRunStatus.SemanticFailure,
            CompilationStatus.BackendFailure => ProgramRunStatus.BackendFailure,
            CompilationStatus.Success => ProgramRunStatus.Success,
            _ => ProgramRunStatus.InfrastructureFailure
        };
    }

    private static ProgramRunStatus MapStatus(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Success => ProgramRunStatus.Success,
            ExecutionStatus.SyntaxFailure => ProgramRunStatus.SyntaxFailure,
            ExecutionStatus.SemanticFailure => ProgramRunStatus.SemanticFailure,
            ExecutionStatus.BackendFailure => ProgramRunStatus.BackendFailure,
            ExecutionStatus.RuntimeFailure => ProgramRunStatus.RuntimeFailure,
            ExecutionStatus.Cancelled => ProgramRunStatus.Cancelled,
            _ => ProgramRunStatus.InfrastructureFailure
        };
    }
}
