namespace Axiom.Ide.Contracts;

public static class ProjectSnapshot
{
    public static IReadOnlyList<ProjectDocumentSnapshot> Validate(string? entryPath,
        IEnumerable<ProjectDocumentSnapshot>? documents, string? entryDocumentId = null)
    {
        var snapshot = documents?.ToArray() ?? [];
        if (snapshot.Length == 0) return snapshot;
        if (entryPath is null || !Path.IsPathFullyQualified(entryPath))
            throw new ArgumentException("Для проектного снимка требуется полный путь точки входа.");

        var entry = Path.GetFullPath(entryPath);
        var directory = Path.GetDirectoryName(entry);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry };
        var identities = new HashSet<string>(StringComparer.Ordinal);
        if (entryDocumentId is not null) identities.Add(entryDocumentId);
        foreach (var document in snapshot)
        {
            if (document is null || string.IsNullOrWhiteSpace(document.DocumentId) || document.DocumentId.Length > 128
                || document.DocumentRevision < 0 || document.SourceText is null
                || string.IsNullOrWhiteSpace(document.SourceFilePath) || !Path.IsPathFullyQualified(document.SourceFilePath))
                throw new ArgumentException("Некорректный документ проектного снимка.");
            var path = Path.GetFullPath(document.SourceFilePath);
            if (!string.Equals(Path.GetDirectoryName(path),directory,StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(path),".axm",StringComparison.OrdinalIgnoreCase)
                || !paths.Add(path) || !identities.Add(document.DocumentId))
                throw new ArgumentException("Документы снимка должны иметь разные имена и принадлежать папке точки входа.");
        }
        return snapshot;
    }
}
