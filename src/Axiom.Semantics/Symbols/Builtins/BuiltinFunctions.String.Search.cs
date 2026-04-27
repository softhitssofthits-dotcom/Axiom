namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetSearchStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Contains, "CONTAINS", TypeSymbol.Boolean, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.StartsWith, "STARTSWITH", TypeSymbol.Boolean, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.EndsWith, "ENDSWITH", TypeSymbol.Boolean, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Replace, "REPLACE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("oldText", TypeSymbol.String), Parameter("newText", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.InStr, "INSTR", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.InStrFrom, "INSTR", TypeSymbol.Integer, [Parameter("start", TypeSymbol.Integer), Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.InStrRev, "INSTRREV", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.InStrRevFrom, "INSTRREV", TypeSymbol.Integer, [Parameter("start", TypeSymbol.Integer), Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.SubStrCount, "SUBSTRCOUNT", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String), Parameter("fragment", TypeSymbol.String)])
        ];
    }
}
