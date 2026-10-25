namespace Axiom.Runtime;

public sealed record BytecodeVmForEachItem(string Name, string Type, string Value);

public sealed class BytecodeVmStopState
{
    private readonly BytecodeVm? _vm;
    private bool _resumed;

    internal BytecodeVmStopState(
        BytecodeVmExecutionSnapshot snapshot,
        BytecodeVm? vm = null,
        BytecodeVmStateSnapshot? previousStateSnapshot = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Snapshot = snapshot;
        _vm = vm;
        PreviousStateSnapshot = previousStateSnapshot;
        CurrentForEachItem = vm?.GetCurrentForEachItem(snapshot.RoutineId);
    }

    public BytecodeVmExecutionSnapshot Snapshot { get; }

    public BytecodeVmForEachItem? CurrentForEachItem { get; }

    public bool CanResume => _vm is not null && !_resumed;

    public bool CanStepBack => _vm is not null && !_resumed && _vm.CanStepBackFrom(this);

    internal BytecodeVmStateSnapshot? PreviousStateSnapshot { get; }

    public RuntimeResult StepBack()
    {
        if (!CanStepBack)
        {
            throw new InvalidOperationException("Stopped bytecode VM run cannot step back.");
        }

        _resumed = true;
        return _vm!.StepBackFrom(this);
    }

    public RuntimeResult Resume()
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("Stopped bytecode VM run cannot be resumed.");
        }

        _resumed = true;
        return _vm!.ResumeFrom(this);
    }

    public RuntimeResult StepOver()
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("Stopped bytecode VM run cannot be resumed.");
        }

        _resumed = true;
        return _vm!.StepOverFrom(this);
    }

    public RuntimeResult StepInto()
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("Stopped bytecode VM run cannot be resumed.");
        }

        _resumed = true;
        return _vm!.StepIntoFrom(this);
    }

    public RuntimeResult StepOut()
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("Stopped bytecode VM run cannot be resumed.");
        }

        _resumed = true;
        return _vm!.StepOutFrom(this);
    }
}
