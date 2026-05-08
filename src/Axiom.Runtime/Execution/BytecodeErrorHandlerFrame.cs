namespace Axiom.Runtime;

internal sealed class BytecodeErrorHandlerFrame
{
    public BytecodeErrorHandlerFrame(
        int catchTarget,
        int finallyTarget,
        int endTarget,
        int catchTargetKind,
        int catchTargetSlot)
    {
        CatchTarget = catchTarget;
        FinallyTarget = finallyTarget;
        EndTarget = endTarget;
        CatchTargetKind = catchTargetKind;
        CatchTargetSlot = catchTargetSlot;
    }

    public int CatchTarget { get; }

    public int FinallyTarget { get; }

    public int EndTarget { get; }

    public int CatchTargetKind { get; }

    public int CatchTargetSlot { get; }

    public BytecodeErrorHandlerState State { get; set; }

    public bool HasPendingUnhandledFailure { get; set; }

    public RuntimeValue? PendingReturnValue { get; set; }

    public bool HasPendingReturn { get; set; }

    public BytecodeErrorHandlerFrame Clone()
    {
        return new BytecodeErrorHandlerFrame(CatchTarget, FinallyTarget, EndTarget, CatchTargetKind, CatchTargetSlot)
        {
            State = State,
            HasPendingUnhandledFailure = HasPendingUnhandledFailure,
            PendingReturnValue = PendingReturnValue,
            HasPendingReturn = HasPendingReturn
        };
    }
}

internal enum BytecodeErrorHandlerState
{
    Active,
    ExecutingCatch,
    ExecutingFinally
}
