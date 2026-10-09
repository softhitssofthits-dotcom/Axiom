using Axiom.Runtime;

namespace Axiom.Execution;

public sealed class ExecutionRequest
{
    public ExecutionRequest(
        string sourceText,
        IInputProvider? inputProvider = null,
        IOutputWriter? outputWriter = null,
        IDiagnosticSink? diagnosticSink = null,
        IRuntimeClock? runtimeClock = null,
        IRuntimeRandom? runtimeRandom = null,
        IRuntimeCancellationProvider? cancellationProvider = null,
        ExecutionMode mode = ExecutionMode.Bytecode,
        string? bytecodeSnapshotPath = null,
        IEnumerable<int>? sourceBreakpointLines = null,
        bool stopBeforeFirstInstruction = false,
        string? resourceBaseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        SourceText = sourceText;
        InputProvider = inputProvider ?? NullRuntimeHost.Instance;
        OutputWriter = outputWriter ?? NullRuntimeHost.Instance;
        DiagnosticSink = diagnosticSink ?? NullDiagnosticSink.Instance;
        RuntimeClock = runtimeClock ?? NullRuntimeHost.Instance;
        RuntimeRandom = runtimeRandom ?? NullRuntimeHost.Instance;
        CancellationProvider = cancellationProvider;
        Mode = mode;
        BytecodeSnapshotPath = bytecodeSnapshotPath;
        SourceBreakpointLines = NormalizeSourceBreakpointLines(sourceBreakpointLines);
        StopBeforeFirstInstruction = stopBeforeFirstInstruction;
        ResourceBaseDirectory = resourceBaseDirectory is null ? null : Path.GetFullPath(resourceBaseDirectory);
    }

    public string SourceText { get; }

    public IInputProvider InputProvider { get; }

    public IOutputWriter OutputWriter { get; }

    public IDiagnosticSink DiagnosticSink { get; }

    public IRuntimeClock RuntimeClock { get; }

    public IRuntimeRandom RuntimeRandom { get; }

    public IRuntimeCancellationProvider? CancellationProvider { get; }

    public ExecutionMode Mode { get; }

    public string? BytecodeSnapshotPath { get; }

    public IReadOnlyList<int> SourceBreakpointLines { get; }

    public bool StopBeforeFirstInstruction { get; }

    public string? ResourceBaseDirectory { get; }

    private static IReadOnlyList<int> NormalizeSourceBreakpointLines(IEnumerable<int>? lines)
    {
        if (lines is null)
        {
            return [];
        }

        var result = new List<int>();
        var seen = new HashSet<int>();

        foreach (var line in lines)
        {
            if (line <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lines), line, "Source breakpoint line must be greater than zero.");
            }

            if (seen.Add(line))
            {
                result.Add(line);
            }
        }

        return result;
    }
}
