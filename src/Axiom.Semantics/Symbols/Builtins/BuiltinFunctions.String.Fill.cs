namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetFillStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Repeat, "REPEAT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Space, "SPACE", TypeSymbol.String, [Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.StringFill, "STRING", TypeSymbol.String, [Parameter("count", TypeSymbol.Integer), Parameter("fill", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.PadLeft, "PADLEFT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.PadLeftFill, "PADLEFT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer), Parameter("fill", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.PadRight, "PADRIGHT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.PadRightFill, "PADRIGHT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer), Parameter("fill", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Center, "CENTER", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.CenterFill, "CENTER", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("width", TypeSymbol.Integer), Parameter("fill", TypeSymbol.String)])
        ];
    }
}
