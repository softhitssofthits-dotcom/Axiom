using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Execution;

public interface IProgramRunService
{
    ProgramCompilationResult Compile(string sourceText, IProgress<int>? progress = null);

    ProgramRunResult Run(
        string sourceText,

        IEnumerable<int>? sourceBreakpointLines = null,
        bool stopBeforeFirstInstruction = false,
        string? sourceFilePath = null,
        TimeSpan? timeout = null,
        IRuntimeCancellationProvider? cancellationProvider = null);

    ProgramRunResult Continue(IProgramDebuggerState debuggerState);

    ProgramRunResult StepOver(IProgramDebuggerState debuggerState);

    ProgramRunResult StepInto(IProgramDebuggerState debuggerState);

    ProgramRunResult StepOut(IProgramDebuggerState debuggerState);

    ProgramRunResult StepBack(IProgramDebuggerState debuggerState);
}
