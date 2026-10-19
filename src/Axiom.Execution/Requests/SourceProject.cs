using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Execution;

/// <summary>Загрузка явно импортированных файлов из папки точки входа.</summary>
public static class SourceProject
{
    public static IReadOnlyList<SourceDocument> LoadDependencies(string entryText, string? entryPath,
        IEnumerable<SourceDocument>? openDocuments = null)
    {
        ArgumentNullException.ThrowIfNull(entryText);
        if (string.IsNullOrWhiteSpace(entryPath))
        {
            return [];
        }

        var path = Path.GetFullPath(entryPath);
        var directory = Path.GetDirectoryName(path)!;
        var replacements = new Dictionary<string, SourceDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in openDocuments ?? [])
        {
            var documentPath = Path.GetFullPath(document.Name, directory);
            if (!string.Equals(Path.GetDirectoryName(documentPath), directory, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(documentPath), ".axm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(documentPath, path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!replacements.TryAdd(documentPath, document))
            {
                throw new ArgumentException($"Документ '{documentPath}' передан в снимке несколько раз.", nameof(openDocuments));
            }
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFileNameWithoutExtension(path) };
        var pending = new Queue<string>();
        var documents = new List<SourceDocument>();
        EnqueueImports(entryText);
        var files = (pending.Count > 0 && Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory).Where(file => string.Equals(Path.GetExtension(file), ".axm", StringComparison.OrdinalIgnoreCase))
                : Enumerable.Empty<string>())
            .Concat(replacements.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToLookup(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        while (pending.TryDequeue(out var name))
        {
            if (!seen.Add(name))
            {
                continue;
            }

            foreach (var modulePath in files[name])
            {
                var text = replacements.TryGetValue(modulePath, out var document) ? document.Text : File.ReadAllText(modulePath);
                documents.Add(new SourceDocument(modulePath, text));
                EnqueueImports(text);
            }
        }

        return documents;

        void EnqueueImports(string text)
        {
            var syntax = Parser.Parse(text, out _);
            foreach (var import in syntax.Members.OfType<ImportStatementSyntax>())
            {
                if (import.NameToken.Kind == SyntaxKind.IdentifierToken && !string.IsNullOrEmpty(import.ModuleName))
                {
                    pending.Enqueue(import.ModuleName);
                }
            }
        }
    }
}
