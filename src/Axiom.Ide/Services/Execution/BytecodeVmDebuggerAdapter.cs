using Axiom.Ide.Models;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Execution;

/// <summary>
/// Адаптер, оборачивающий BytecodeVmStopState в интерфейс IProgramDebuggerState.
/// Скрывает типы runtime от кода IDE.
/// </summary>
public sealed class BytecodeVmDebuggerAdapter : IProgramDebuggerState
{
    private readonly BytecodeVmStopState _stopState;
    private readonly Func<BytecodeVmStopState, ProgramRunResult> _resume;
    private readonly Func<BytecodeVmStopState, ProgramRunResult> _stepOver;
    private readonly Func<BytecodeVmStopState, ProgramRunResult> _stepInto;
    private readonly Func<BytecodeVmStopState, ProgramRunResult> _stepOut;
    private readonly Func<BytecodeVmStopState, ProgramRunResult> _stepBack;

    public BytecodeVmDebuggerAdapter(
        BytecodeVmStopState stopState,
        Func<BytecodeVmStopState, ProgramRunResult> resume,
        Func<BytecodeVmStopState, ProgramRunResult> stepOver,
        Func<BytecodeVmStopState, ProgramRunResult> stepInto,
        Func<BytecodeVmStopState, ProgramRunResult> stepOut,
        Func<BytecodeVmStopState, ProgramRunResult> stepBack)
    {
        ArgumentNullException.ThrowIfNull(stopState);
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(stepOver);
        ArgumentNullException.ThrowIfNull(stepInto);
        ArgumentNullException.ThrowIfNull(stepOut);
        ArgumentNullException.ThrowIfNull(stepBack);

        _stopState = stopState;
        _resume = resume;
        _stepOver = stepOver;
        _stepInto = stepInto;
        _stepOut = stepOut;
        _stepBack = stepBack;

        var snapshot = stopState.Snapshot;
        StopInfo = new DebuggerStopInfo(
            snapshot.SourceLocation.Line,
            snapshot.SourceLocation.Column,
            snapshot.Opcode.ToString(),
            snapshot.CallDepth,
            stopState.CanStepBack);
    }

    public DebuggerStopInfo StopInfo { get; }

    public bool CanResume => _stopState.CanResume;

    public bool CanStepBack => _stopState.CanStepBack;

    public ProgramRunResult Resume() => _resume(_stopState);

    public ProgramRunResult StepOver() => _stepOver(_stopState);

    public ProgramRunResult StepInto() => _stepInto(_stopState);

    public ProgramRunResult StepOut() => _stepOut(_stopState);

    public ProgramRunResult StepBack() => _stepBack(_stopState);
}
