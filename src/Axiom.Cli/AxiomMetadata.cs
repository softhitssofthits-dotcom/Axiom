using System.Reflection;
using Axiom.Semantics;
namespace Axiom.Cli;
internal static class AxiomMetadata
{
    public static string ToJson() => AxiomLanguageMetadata.ToJson(SourceRevision);
    public static string SourceRevision { get; } = LoadSourceRevision();
    private static string LoadSourceRevision()
    {
        var attributes = typeof(AxiomMetadata).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.Key, "AxiomSourceRevision", StringComparison.Ordinal))
            {
                return attribute.Value ?? "unknown";
            }
        }

        return "unknown";
    }

}
