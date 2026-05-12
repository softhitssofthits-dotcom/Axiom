namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetCharStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Asc, "ASC", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.AscW, "ASCW", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Chr, "CHR", TypeSymbol.String, [Parameter("code", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.ChrW, "CHRW", TypeSymbol.String, [Parameter("code", TypeSymbol.Integer)])
        ];
    }
}
