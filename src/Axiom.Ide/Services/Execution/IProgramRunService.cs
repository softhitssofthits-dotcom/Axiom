using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Execution;

public interface IProgramRunService
{
    ProgramCompilationResult Compile(string sourceText, IProgress<int>? progress = null);

    ProgramRunResult Run(
        string sourceText,
        ExecutionMode mode = ExecutionMode.Bytecode,
        IEnumerable<int>? sourceBreakpointLines = null,
        bool stopBeforeFirstInstruction = false);

    ProgramRunResult Continue(BytecodeVmStopState stopState);

    ProgramRunResult StepOver(BytecodeVmStopState stopState);

    ProgramRunResult StepInto(BytecodeVmStopState stopState);

    ProgramRunResult StepOut(BytecodeVmStopState stopState);

    ProgramRunResult StepBack(BytecodeVmStopState stopState);
}
