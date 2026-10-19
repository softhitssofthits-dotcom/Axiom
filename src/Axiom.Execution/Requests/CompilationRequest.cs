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
        InitialGraphicsContext initialGraphicsContext = InitialGraphicsContext.None,
        string? sourceName = null,
        IEnumerable<SourceDocument>? additionalDocuments = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        SourceText = sourceText;
        ProgressReporter = progressReporter;
        DiagnosticSink = diagnosticSink ?? NullDiagnosticSink.Instance;
        BytecodeSnapshotPath = bytecodeSnapshotPath;
        IncludeUsageWarnings = includeUsageWarnings;
        InitialGraphicsContext = initialGraphicsContext;
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? null : sourceName;
        AdditionalDocuments = additionalDocuments?.ToArray() ?? [];
    }

    public string SourceText { get; }

    /// <summary>
    /// Имя основного документа. Для одиночной программы остаётся null,
    /// чтобы диагностика сохраняла прежний вид.
    /// </summary>
    public string? SourceName { get; }

    public IReadOnlyList<SourceDocument> AdditionalDocuments { get; }

    public string EntryModuleName => Path.GetFileNameWithoutExtension(SourceName ?? "main");

    public IProgress<int>? ProgressReporter { get; }

    public IDiagnosticSink DiagnosticSink { get; }

    public string? BytecodeSnapshotPath { get; }

    public bool IncludeUsageWarnings { get; }

    public InitialGraphicsContext InitialGraphicsContext { get; }
}
