using Axiom.Core;

namespace Axiom.Runtime;

public sealed class RuntimeResult
{
    public RuntimeResult(DiagnosticBag diagnostics, BytecodeVmStopState? stopState = null, string pendingOutput = "")
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(pendingOutput);
        Diagnostics = diagnostics;
        StopState = stopState;
        PendingOutput = pendingOutput;
    }

    public DiagnosticBag Diagnostics { get; }

    public BytecodeVmStopState? StopState { get; }

    public string PendingOutput { get; }

    public BytecodeVmExecutionSnapshot? StoppedAt => StopState?.Snapshot;

    public bool Stopped => StopState is not null;

    public bool Succeeded => Diagnostics.Count == 0;
}
