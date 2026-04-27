using Axiom.Core;
using Axiom.Runtime;

namespace Axiom.Execution;

public sealed class ExecutionResult
{
    public ExecutionResult(
        ExecutionStatus status,
        IReadOnlyList<Diagnostic> diagnostics,
        IReadOnlyList<string> output,
        ExecutionMode requestedMode = ExecutionMode.Bytecode,
        ExecutionEngineKind? actualEngine = null,
        bool usedInterpreterFallback = false,
        string? interpreterFallbackReason = null,
        TimeSpan frontendPreparationDuration = default,
        TimeSpan loweringDuration = default,
        TimeSpan bytecodeEmissionDuration = default,
        TimeSpan runtimeExecutionDuration = default,
        bool usedCompiledCache = false,
        bool usedLoweredCache = false,
        bool usedBytecodeCache = false,
        BytecodeVmStopState? stopState = null,
        string pendingOutput = "")
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(pendingOutput);

        Status = status;
        Diagnostics = diagnostics;
        Output = output;
        RequestedMode = requestedMode;
        ActualEngine = actualEngine;
        UsedInterpreterFallback = usedInterpreterFallback;
        InterpreterFallbackReason = interpreterFallbackReason;
        FrontendPreparationDuration = frontendPreparationDuration;
        LoweringDuration = loweringDuration;
        BytecodeEmissionDuration = bytecodeEmissionDuration;
        RuntimeExecutionDuration = runtimeExecutionDuration;
        UsedCompiledCache = usedCompiledCache;
        UsedLoweredCache = usedLoweredCache;
        UsedBytecodeCache = usedBytecodeCache;
        StopState = stopState;
        PendingOutput = pendingOutput;
    }

    public ExecutionStatus Status { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public IReadOnlyList<string> Output { get; }

    public ExecutionMode RequestedMode { get; }

    public ExecutionEngineKind? ActualEngine { get; }

    public bool UsedInterpreterFallback { get; }

    public string? InterpreterFallbackReason { get; }

    public TimeSpan FrontendPreparationDuration { get; }

    public TimeSpan LoweringDuration { get; }

    public TimeSpan BytecodeEmissionDuration { get; }

    public TimeSpan RuntimeExecutionDuration { get; }

    public TimeSpan TotalDuration =>
        FrontendPreparationDuration
        + LoweringDuration
        + BytecodeEmissionDuration
        + RuntimeExecutionDuration;

    public bool UsedCompiledCache { get; }

    public bool UsedLoweredCache { get; }

    public bool UsedBytecodeCache { get; }

    public BytecodeVmStopState? StopState { get; }

    public string PendingOutput { get; }

    public BytecodeVmExecutionSnapshot? StoppedAt => StopState?.Snapshot;

    public bool Stopped => StopState is not null;

    public bool CanStepBack => StopState?.CanStepBack == true;

    public bool Succeeded => Status == ExecutionStatus.Success;
}
