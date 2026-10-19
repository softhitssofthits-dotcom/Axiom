namespace Axiom.Ide.Contracts;

public sealed record SourceRange(int Line, int Column, int SpanStart, int SpanLength);
public sealed record DocumentSnapshot(string DocumentId, int DocumentRevision, string SourceText,
    string? SourceFilePath, string? ResourceBaseDirectory,
    IReadOnlyList<ProjectDocumentSnapshot>? ProjectDocuments = null);
public sealed record ProjectDocumentSnapshot(string DocumentId, int DocumentRevision, string SourceFilePath, string SourceText);
public sealed record TextEdit(int Start, int Length, string NewText);
public sealed record HostDiagnostic(string Code, string Stage, string Severity, string Message,
    string DocumentId, int DocumentRevision, string? SourceFilePath, SourceRange? Position);

public static class IdeProtocol
{
    public const int Version = 1;
    public const int MaximumMessageBytes = 16 * 1024 * 1024;
}
