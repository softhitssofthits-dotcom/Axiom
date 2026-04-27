namespace Axiom.Execution;

public sealed class CompilationRequest
{
    public CompilationRequest(
        string sourceText,
        IProgress<int>? progressReporter = null,
        IDiagnosticSink? diagnosticSink = null,
        string? bytecodeSnapshotPath = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        SourceText = sourceText;
        ProgressReporter = progressReporter;
        DiagnosticSink = diagnosticSink ?? NullDiagnosticSink.Instance;
        BytecodeSnapshotPath = bytecodeSnapshotPath;
    }

    public string SourceText { get; }

    public IProgress<int>? ProgressReporter { get; }

    public IDiagnosticSink DiagnosticSink { get; }

    public string? BytecodeSnapshotPath { get; }
}
