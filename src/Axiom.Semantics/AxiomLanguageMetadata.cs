using System.Text.Json;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Semantics;

/// <summary>
/// Реестр языка для IDE (команда `axiom metadata`): ключевые слова и типы — из
/// <see cref="AxiomLanguageKeywords"/>, встроенные функции с сигнатурами — из реестра
/// <see cref="BuiltinFunctions"/>. Док-строк у встроенных функций нет, hover в IDE
/// строится по сигнатурам. sourceRevision — git-ревизия сборки для freshness-guard IDE.
/// </summary>
public static class AxiomLanguageMetadata
{
    private const string Q = "\"";

    public static string ToJson(string sourceRevision = "unknown")
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
                .Append(Q).Append("name").Append(Q).Append(':').Append(Q).Append(Escape(name)).Append(Q)
                .Append(',').Append(Q).Append("params").Append(Q).Append(":[");
            for (var index = 0; index < descriptor.Parameters.Count; index++)
            {
                var parameter = descriptor.Parameters[index];
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{')
                    .Append(Q).Append("name").Append(Q).Append(':').Append(Q).Append(Escape(parameter.Name)).Append(Q)
                    .Append(',').Append(Q).Append("type").Append(Q).Append(':').Append(Q).Append(Escape(parameter.Type.Name)).Append(Q)
                    .Append(',').Append(Q).Append("displayType").Append(Q).Append(':').Append(Q)
                    .Append(Escape(ParameterDisplayType(descriptor, index))).Append(Q)
                    .Append('}');
            }

            builder.Append("],").Append(Q).Append("returnType").Append(Q).Append(':').Append(Q)
                .Append(Escape(descriptor.DeclaredReturnType.Name)).Append(Q)
                .Append(',').Append(Q).Append("bindingKind").Append(Q).Append(':').Append(Q).Append(descriptor.BindingKind).Append(Q)
                .Append(',').Append(Q).Append("operandBindingPolicy").Append(Q).Append(':').Append(Q).Append(descriptor.OperandBindingPolicy).Append(Q)
                .Append(',').Append(Q).Append("resultTypePolicy").Append(Q).Append(':').Append(Q).Append(descriptor.ResultTypePolicy).Append(Q)
                .Append(',').Append(Q).Append("displayReturnType").Append(Q).Append(':').Append(Q)
                .Append(Escape(ReturnDisplayType(descriptor))).Append(Q)
                .Append("},");
        }

        if (descriptors.Count > 0)
        {
            builder.Length--; // убрать завершающую запятую
        }

        builder.Append("],").Append(Q).Append("sourceRevision").Append(Q).Append(':').Append(Q)
            .Append(Escape(sourceRevision)).Append(Q)
            .Append(',').Append(Q).Append("languageVersion").Append(Q).Append(':').Append(Q).Append(AxiomVersionInfo.LanguageVersion).Append(Q)
            .Append(',').Append(Q).Append("compilerVersion").Append(Q).Append(':').Append(Q).Append(AxiomVersionInfo.CompilerVersion).Append(Q)
            .Append('}');
        return builder.ToString();
    }

    private static string ParameterDisplayType(BuiltinFunctionDescriptor descriptor, int index)
    {
        var type = descriptor.Parameters[index].Type;
        if (index == 0 && descriptor.BindingKind == BuiltinBindingKind.Array && type is ArrayTypeSymbol)
            return "T[]";
        if (descriptor.BindingKind == BuiltinBindingKind.Numeric
            && descriptor.OperandBindingPolicy is BuiltinOperandBindingPolicy.PreserveNumericArguments or BuiltinOperandBindingPolicy.PromoteNumericArguments)
            return "T";
        return type.Name;
    }

    private static string ReturnDisplayType(BuiltinFunctionDescriptor descriptor) => descriptor.ResultTypePolicy switch
    {
        BuiltinResultTypePolicy.SameAsFirstArgument => ParameterDisplayType(descriptor, 0),
        BuiltinResultTypePolicy.SameAsSecondArgument => ParameterDisplayType(descriptor, 1),
        BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument => "T",
        BuiltinResultTypePolicy.ArrayOfFirstArrayElementType => "T[]",
        BuiltinResultTypePolicy.ArrayDynamicSliceResult => "элемент или массив T",
        BuiltinResultTypePolicy.ArrayIndexCoordinateResult => "INTEGER или INTEGER[]",
        BuiltinResultTypePolicy.ArrayFindAllCoordinateResult => "INTEGER[] или INTEGER[,]",
        BuiltinResultTypePolicy.ArrayMapSelectorReturnArray => "массив результатов selector",
        BuiltinResultTypePolicy.ArrayReduceAccumulator => "тип начального значения",
        BuiltinResultTypePolicy.PromotedNumeric => "общий числовой тип",
        _ => descriptor.DeclaredReturnType.Name
    };

    private static string Escape(string text) => JsonEncodedText.Encode(text).ToString();

    private static void AppendSortedWords(StringBuilder builder, IEnumerable<string> words)
    {
        var isFirst = true;
        foreach (var word in words.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static word => word, StringComparer.OrdinalIgnoreCase))
        {
            if (!isFirst)
            {
                builder.Append(',');
            }

            builder.Append(Q).Append(Escape(word)).Append(Q);
            isFirst = false;
        }
    }
}
