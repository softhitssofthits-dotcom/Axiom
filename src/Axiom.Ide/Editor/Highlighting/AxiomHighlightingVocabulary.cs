using System.Reflection;
using System.Xml.Linq;

namespace Axiom.Ide.Editor.Highlighting;

public static class AxiomHighlightingVocabulary
{
    private const string ResourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
    private static readonly Lazy<HighlightingVocabulary> VocabularyHolder = new(LoadVocabulary);

    public static IReadOnlySet<string> Keywords => VocabularyHolder.Value.Keywords;
    public static IReadOnlySet<string> TypeNames => VocabularyHolder.Value.TypeNames;
    public static IReadOnlySet<string> BuiltIns => VocabularyHolder.Value.BuiltIns;

    private static HighlightingVocabulary LoadVocabulary()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded highlighting resource '{ResourceName}' was not found.");

        var document = XDocument.Load(stream);
        var syntaxNamespace = document.Root?.Name.Namespace
            ?? throw new InvalidOperationException("Axiom highlighting definition is missing its XML namespace.");

        return new HighlightingVocabulary(
            LoadWords(document, syntaxNamespace, "Keyword"),
            LoadWords(document, syntaxNamespace, "TypeName"),
            LoadWords(document, syntaxNamespace, "BuiltIn"));
    }

    private static HashSet<string> LoadWords(XDocument document, XNamespace syntaxNamespace, string colorName)
    {
        return document
            .Descendants(syntaxNamespace + "Keywords")
            .Where(keywords => string.Equals((string?)keywords.Attribute("color"), colorName, StringComparison.Ordinal))
            .Elements(syntaxNamespace + "Word")
            .Select(word => word.Value.Trim())
            .Where(word => word.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record HighlightingVocabulary(
        HashSet<string> Keywords,
        HashSet<string> TypeNames,
        HashSet<string> BuiltIns);
}
