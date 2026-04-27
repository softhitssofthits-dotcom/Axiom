using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Axiom.Ide.Services.Persistence;

namespace Axiom.Ide.Editor.Highlighting;

public static class AxiomHighlighting
{
    private const string HighlightingName = "Axiom";
    private const string LightResourceName = "Axiom.Ide.Editor.Highlighting.Axiom.xshd";
    private const string DarkResourceName = "Axiom.Ide.Editor.Highlighting.Axiom.Dark.xshd";

    private static readonly Lazy<IHighlightingDefinition> LightDefinitionHolder = new(
        () => LoadDefinition(HighlightingName, LightResourceName, [".axm"]));
    private static readonly Lazy<IHighlightingDefinition> DarkDefinitionHolder = new(
        () => LoadDefinition("Axiom Dark", DarkResourceName, []));

    public static IHighlightingDefinition Definition => LightDefinitionHolder.Value;

    public static IHighlightingDefinition GetDefinition(IdeTheme theme) =>
        theme == IdeTheme.Dark ? DarkDefinitionHolder.Value : Definition;

    private static IHighlightingDefinition LoadDefinition(
        string highlightingName,
        string resourceName,
        string[] extensions)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded highlighting resource '{resourceName}' was not found.");
        using var reader = XmlReader.Create(stream);

        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting(highlightingName, extensions, definition);
        return definition;
    }
}
