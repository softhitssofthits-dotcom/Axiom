namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetBasicStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Len, "LEN", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Left, "LEFT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Right, "RIGHT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Mid, "MID", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("start", TypeSymbol.Integer), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.LCase, "LCASE", TypeSymbol.String, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UCase, "UCASE", TypeSymbol.String, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Reverse, "REVERSE", TypeSymbol.String, [Parameter("text", TypeSymbol.String)])
        ];
    }
}
