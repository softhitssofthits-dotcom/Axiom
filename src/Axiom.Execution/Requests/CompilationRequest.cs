using Axiom.Semantics;

namespace Axiom.Execution;

public sealed class CompilationRequest
{
    public CompilationRequest(
        string sourceText,
        IProgress<int>? progressReporter = null,
        IDiagnosticSink? diagnosticSink = null,
        string? bytecodeSnapshotPath = null,
        bool includeUsageWarnings = false,
        InitialGraphicsContext initialGraphicsContext = InitialGraphicsContext.None)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        SourceText = sourceText;
        ProgressReporter = progressReporter;
        DiagnosticSink = diagnosticSink ?? NullDiagnosticSink.Instance;
        BytecodeSnapshotPath = bytecodeSnapshotPath;
        IncludeUsageWarnings = includeUsageWarnings;
        InitialGraphicsContext = initialGraphicsContext;
    }

    public string SourceText { get; }

    public IProgress<int>? ProgressReporter { get; }

    public IDiagnosticSink DiagnosticSink { get; }

    public string? BytecodeSnapshotPath { get; }

    public bool IncludeUsageWarnings { get; }

    public InitialGraphicsContext InitialGraphicsContext { get; }
}
