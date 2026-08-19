using Axiom.Ide.Services.Execution;

namespace Axiom.Ide.Models;

public sealed class ProgramRunResult
{
    public ProgramRunResult(
        ProgramRunStatus status,
        IReadOnlyList<ProgramDiagnosticItem> diagnostics,
        string outputText,
        TimeSpan preparationDuration = default,
        TimeSpan executionDuration = default,
        bool usedCompiledCache = false,
        Axiom.Execution.ExecutionMode requestedExecutionMode = Axiom.Execution.ExecutionMode.Bytecode,
        Axiom.Execution.ExecutionEngineKind? actualEngine = null,
        bool usedInterpreterFallback = false,
        string? interpreterFallbackReason = null,
        TimeSpan loweringDuration = default,
        TimeSpan bytecodeEmissionDuration = default,
        bool usedLoweredCache = false,
        bool usedBytecodeCache = false,
        IProgramDebuggerState? debuggerState = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(outputText);

        Status = status;
        Diagnostics = diagnostics;
        OutputText = outputText;
        PreparationDuration = preparationDuration;
        ExecutionDuration = executionDuration;
        UsedCompiledCache = usedCompiledCache;
        RequestedExecutionMode = requestedExecutionMode;
        ActualEngine = actualEngine;
        UsedInterpreterFallback = usedInterpreterFallback;
        InterpreterFallbackReason = interpreterFallbackReason;
        LoweringDuration = loweringDuration;
        BytecodeEmissionDuration = bytecodeEmissionDuration;
        UsedLoweredCache = usedLoweredCache;
        UsedBytecodeCache = usedBytecodeCache;
        DebuggerState = debuggerState;
    }

    public ProgramRunStatus Status { get; }

    public IReadOnlyList<ProgramDiagnosticItem> Diagnostics { get; }

    public string OutputText { get; }

    public TimeSpan PreparationDuration { get; }

    public TimeSpan ExecutionDuration { get; }

    public TimeSpan LoweringDuration { get; }

    public TimeSpan BytecodeEmissionDuration { get; }

    public TimeSpan TotalDuration =>
        PreparationDuration
        + LoweringDuration
        + BytecodeEmissionDuration
        + ExecutionDuration;

    public bool UsedCompiledCache { get; }

    public bool UsedLoweredCache { get; }

    public bool UsedBytecodeCache { get; }

    public Axiom.Execution.ExecutionMode RequestedExecutionMode { get; }

    public Axiom.Execution.ExecutionEngineKind? ActualEngine { get; }

    public bool UsedInterpreterFallback { get; }

    public string? InterpreterFallbackReason { get; }

    public IProgramDebuggerState? DebuggerState { get; }

    public DebuggerStopInfo? StoppedAt => DebuggerState?.StopInfo;

    public bool Stopped => DebuggerState is not null;

    public bool CanStepBack => DebuggerState?.StopInfo.CanStepBack == true;

    public bool Cancelled => Status == ProgramRunStatus.Cancelled;

    public bool Succeeded => Status == ProgramRunStatus.Success || Status == ProgramRunStatus.Cancelled;
}
