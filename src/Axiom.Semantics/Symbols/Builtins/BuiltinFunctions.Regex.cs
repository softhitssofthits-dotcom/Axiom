namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetRegexDescriptors()
    {
        var stringArrayType = ArrayTypeSymbol.Create(TypeSymbol.String);

        return
        [
            CreateDefault(BuiltinFunctionId.RegexIsMatch, "REGEXISMATCH", TypeSymbol.Boolean, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexIsMatchModes, "REGEXISMATCH", TypeSymbol.Boolean, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexMatch, "REGEXMATCH", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexMatchModes, "REGEXMATCH", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexExtract, "REGEXEXTRACT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("group", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.RegexExtractModes, "REGEXEXTRACT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("group", TypeSymbol.Integer), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexMatches, "REGEXMATCHES", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexMatchesModes, "REGEXMATCHES", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexExtractAll, "REGEXEXTRACTALL", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("group", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.RegexExtractAllModes, "REGEXEXTRACTALL", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("group", TypeSymbol.Integer), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexSplit, "REGEXSPLIT", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexSplitModes, "REGEXSPLIT", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexReplace, "REGEXREPLACE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("replacement", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexReplaceModes, "REGEXREPLACE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("replacement", TypeSymbol.String), Parameter("modes", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexReplaceCount, "REGEXREPLACE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("replacement", TypeSymbol.String), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.RegexReplaceModesCount, "REGEXREPLACE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("replacement", TypeSymbol.String), Parameter("modes", TypeSymbol.String), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.RegexCount, "REGEXCOUNT", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RegexCountModes, "REGEXCOUNT", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String), Parameter("pattern", TypeSymbol.String), Parameter("modes", TypeSymbol.String)])
        ];
    }
}
