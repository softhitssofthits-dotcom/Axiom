namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetTrimStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.TrimWhitespace, "TRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TrimCharacters, "TRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("chars", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.LTrimWhitespace, "LTRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.LTrimCharacters, "LTRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("chars", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RTrimWhitespace, "RTRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RTrimCharacters, "RTRIM", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("chars", TypeSymbol.String)])
        ];
    }
}
