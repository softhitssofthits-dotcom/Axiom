namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetEditStringDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Insert, "INSERT", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("start", TypeSymbol.Integer), Parameter("fragment", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Remove, "REMOVE", TypeSymbol.String, [Parameter("text", TypeSymbol.String), Parameter("start", TypeSymbol.Integer), Parameter("count", TypeSymbol.Integer)])
        ];
    }
}
