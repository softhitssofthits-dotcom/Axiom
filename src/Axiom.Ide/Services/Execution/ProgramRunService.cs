using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Input;
using Axiom.Ide.Services.Shell;
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

    public ProgramRunService(
        AxiomExecutionPipeline pipeline,
        IProgramConsoleService consoleService,
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(consoleService);

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
                    IdeLanguageResourceService.GetCurrentFormattedString(
                        "Compile.Infrastructure.UnexpectedError",
                        "Unexpected compilation error: {0}",
                        exception.Message))]);
        }
    }

    public ProgramRunResult Run(
        string sourceText,
        ExecutionMode mode = ExecutionMode.Bytecode,
        IEnumerable<int>? sourceBreakpointLines = null,
        bool stopBeforeFirstInstruction = false,
        string? sourceFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        try
        {
            _consoleService.ResourceBaseDirectory = ResolveResourceBaseDirectory(sourceFilePath);
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
                            ["diagnostics"] = failedDiagnostics.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["requestedMode"] = mode.ToString()
                        }
                    });

                    _consoleService.EndSession();
                    return new ProgramRunResult(
                        MapCompilationFailureStatus(compilationResult.Status),
                        failedDiagnostics,
                        string.Empty,
                        preparationDuration: preparationDuration,
                        executionDuration: TimeSpan.Zero,
                        usedCompiledCache: false,
                        requestedExecutionMode: mode);
                }

                compiledProgram = compilationResult.CompiledProgram!;
                _compiledProgram = compiledProgram;
            }

            var executionResult = _pipeline.RunCompiled(
                compiledProgram,
                new ExecutionRequest(
                    sourceText,
                    inputProvider: inputProvider,
                    outputWriter: _consoleService,
                    mode: mode,
                    sourceBreakpointLines: sourceBreakpointLines,
                    stopBeforeFirstInstruction: stopBeforeFirstInstruction));

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

            _traceService.Write(
                "run.pipeline",
                $"status={executionResult.Status} | requestedMode={executionResult.RequestedMode} | actualEngine={executionResult.ActualEngine?.ToString() ?? "none"} | stopped={executionResult.Stopped} | diagnostics={diagnostics.Length} | outputLines={executionResult.Output.Count} | compiledCache={(usesCompiledCache ? "hit" : "miss")} | bytecodeCache={(executionResult.UsedBytecodeCache ? "hit" : "miss")} | preparationMs={preparationDuration.TotalMilliseconds:F3} | loweringMs={executionResult.LoweringDuration.TotalMilliseconds:F3} | bytecodeMs={executionResult.BytecodeEmissionDuration.TotalMilliseconds:F3} | executionMs={executionDuration.TotalMilliseconds:F3}");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = executionResult.Status == ExecutionStatus.Success ? IdeEventLevel.Info : IdeEventLevel.Warning,
                Category = "debugger",
                EventName = executionResult.Stopped ? "run.stopped" : "run.completed",
                Source = nameof(ProgramRunService),
                CommandName = "Run",
                DebugLine = executionResult.StopState?.Snapshot.SourceLocation.Line,
                Opcode = executionResult.StopState?.Snapshot.Opcode.ToString(),
                CallDepth = executionResult.StopState?.Snapshot.CallDepth,
                CanStepBack = executionResult.StopState?.CanStepBack,
                OutputLength = outputText.Length,
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["status"] = executionResult.Status.ToString(),
                    ["requestedMode"] = executionResult.RequestedMode.ToString(),
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
                requestedExecutionMode: mode,
                actualEngine: executionResult.ActualEngine,
                usedInterpreterFallback: executionResult.UsedInterpreterFallback || mode == ExecutionMode.Bytecode && executionResult.ActualEngine == ExecutionEngineKind.Interpreter,
                interpreterFallbackReason: executionResult.InterpreterFallbackReason
                    ?? (mode == ExecutionMode.Bytecode && executionResult.ActualEngine == ExecutionEngineKind.Interpreter
                        ? "Bytecode VM does not yet cover this program."
                        : null),
                loweringDuration: executionResult.LoweringDuration,
                bytecodeEmissionDuration: executionResult.BytecodeEmissionDuration,
                usedLoweredCache: executionResult.UsedLoweredCache,
                usedBytecodeCache: executionResult.UsedBytecodeCache,
                stopState: executionResult.StopState);
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
                    IdeLanguageResourceService.GetCurrentFormattedString(
                        "Run.Infrastructure.UnexpectedError",
                        "Unexpected execution error: {0}",
                        exception.Message))
            };

            return new ProgramRunResult(ProgramRunStatus.InfrastructureFailure, diagnostics, string.Empty);
        }
    }

    public ProgramRunResult Continue(BytecodeVmStopState stopState)
    {
        return ResumeStoppedRun(
            stopState,
            static state => state.Resume(),
            "run.continue",
            "run.continue.exception",
            "Unexpected ProgramRunService continuation failure.");
    }

    public ProgramRunResult StepOver(BytecodeVmStopState stopState)
    {
        return ResumeStoppedRun(
            stopState,
            static state => state.StepOver(),
            "run.step-over",
            "run.step-over.exception",
            "Unexpected ProgramRunService step-over failure.");
    }

    public ProgramRunResult StepInto(BytecodeVmStopState stopState)
    {
        return ResumeStoppedRun(
            stopState,
            static state => state.StepInto(),
            "run.step-into",
            "run.step-into.exception",
            "Unexpected ProgramRunService step-into failure.");
    }

    public ProgramRunResult StepOut(BytecodeVmStopState stopState)
    {
        return ResumeStoppedRun(
            stopState,
            static state => state.StepOut(),
            "run.step-out",
            "run.step-out.exception",
            "Unexpected ProgramRunService step-out failure.");
    }

    public ProgramRunResult StepBack(BytecodeVmStopState stopState)
    {
        return ResumeStoppedRun(
            stopState,
            static state => state.StepBack(),
            "run.step-back",
            "run.step-back.exception",
            "Unexpected ProgramRunService step-back failure.");
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
                DebugLine = runtimeResult.StopState?.Snapshot.SourceLocation.Line,
                Opcode = runtimeResult.StopState?.Snapshot.Opcode.ToString(),
                CallDepth = runtimeResult.StopState?.Snapshot.CallDepth,
                CanStepBack = runtimeResult.StopState?.CanStepBack,
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
                requestedExecutionMode: ExecutionMode.Bytecode,
                actualEngine: ExecutionEngineKind.BytecodeVm,
                stopState: runtimeResult.StopState);
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
                    IdeLanguageResourceService.GetCurrentFormattedString(
                        "Run.Infrastructure.UnexpectedContinueError",
                        "Unexpected continuation error: {0}",
                        exception.Message))
            };

            return new ProgramRunResult(
                ProgramRunStatus.InfrastructureFailure,
                diagnostics,
                GetConsoleOutputText(),
                executionDuration: executionStopwatch.Elapsed,
                requestedExecutionMode: ExecutionMode.Bytecode,
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
            _ => ProgramCompilationStatus.InfrastructureFailure
        };
    }

    private static ProgramRunStatus MapCompilationFailureStatus(CompilationStatus status)
    {
        return status switch
        {
            CompilationStatus.SyntaxFailure => ProgramRunStatus.SyntaxFailure,
            CompilationStatus.SemanticFailure => ProgramRunStatus.SemanticFailure,
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
