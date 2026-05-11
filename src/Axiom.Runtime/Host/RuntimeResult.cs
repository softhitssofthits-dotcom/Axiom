using Axiom.Core;

namespace Axiom.Runtime;

public sealed class RuntimeResult
{
    public RuntimeResult(DiagnosticBag diagnostics, BytecodeVmStopState? stopState = null, string pendingOutput = "", bool cancelled = false)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(pendingOutput);
        Diagnostics = diagnostics;
        StopState = stopState;
        PendingOutput = pendingOutput;
        Cancelled = cancelled;
    }

    public DiagnosticBag Diagnostics { get; }

    public BytecodeVmStopState? StopState { get; }

    public string PendingOutput { get; }

    public bool Cancelled { get; }

    public BytecodeVmExecutionSnapshot? StoppedAt => StopState?.Snapshot;

    public bool Stopped => StopState is not null;

    public bool Succeeded => !Cancelled && Diagnostics.Count == 0;
}
