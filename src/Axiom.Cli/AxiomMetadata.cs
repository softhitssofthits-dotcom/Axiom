using System.Reflection;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Cli;

/// <summary>
/// Реестр языка для IDE (команда `axiom metadata`): ключевые слова и типы — из
/// <see cref="AxiomLanguageKeywords"/>, встроенные функции с сигнатурами — из реестра
/// <see cref="BuiltinFunctions"/>. Док-строк у встроенных функций нет, hover в IDE
/// строится по сигнатурам. sourceRevision — git-ревизия сборки для freshness-guard IDE.
/// </summary>
internal static class AxiomMetadata
{
    private const string Q = "\"";

    public static string ToJson()
    {
        var builder = new StringBuilder(32 * 1024);

        builder.Append('{').Append(Q).Append("keywords").Append(Q).Append(":[");
        AppendSortedWords(builder, AxiomLanguageKeywords.Keywords);
        builder.Append("],").Append(Q).Append("typeNames").Append(Q).Append(":[");
        AppendSortedWords(builder, AxiomLanguageKeywords.TypeNames);
        builder.Append("],").Append(Q).Append("builtins").Append(Q).Append(":[");

        var descriptors = new List<(string Name, BuiltinFunctionDescriptor Descriptor)>();
        foreach (var function in BuiltinFunctions.All)
        {
            if (BuiltinFunctions.TryGetDescriptors(function.Name, out var family))
            {
                foreach (var descriptor in family)
                {
                    descriptors.Add((function.Name, descriptor));
                }
            }
        }

        foreach (var (name, descriptor) in descriptors
                     .OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static entry => entry.Descriptor.Parameters.Count))
        {
            builder.Append('{')
                .Append(Q).Append("name").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(name)).Append(Q)
                .Append(',').Append(Q).Append("params").Append(Q).Append(":[");
            for (var index = 0; index < descriptor.Parameters.Count; index++)
            {
                var parameter = descriptor.Parameters[index];
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{')
                    .Append(Q).Append("name").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(parameter.Name)).Append(Q)
                    .Append(',').Append(Q).Append("type").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(parameter.Type.Name)).Append(Q)
                    .Append('}');
            }

            builder.Append("],").Append(Q).Append("returnType").Append(Q).Append(':').Append(Q)
                .Append(JsonText.Escape(descriptor.DeclaredReturnType.Name)).Append(Q)
                .Append("},");
        }

        if (descriptors.Count > 0)
        {
            builder.Length--; // убрать завершающую запятую
        }

        builder.Append("],").Append(Q).Append("sourceRevision").Append(Q).Append(':').Append(Q)
            .Append(JsonText.Escape(SourceRevision)).Append(Q)
            .Append('}');
        return builder.ToString();
    }

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

    private static void AppendSortedWords(StringBuilder builder, IEnumerable<string> words)
    {
        var isFirst = true;
        foreach (var word in words.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static word => word, StringComparer.OrdinalIgnoreCase))
        {
            if (!isFirst)
            {
                builder.Append(',');
            }

            builder.Append(Q).Append(JsonText.Escape(word)).Append(Q);
            isFirst = false;
        }
    }
}
