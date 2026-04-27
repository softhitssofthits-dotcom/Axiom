namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetStringArrayDescriptors()
    {
        var stringArrayType = ArrayTypeSymbol.Create(TypeSymbol.String);

        return
        [
            CreateDefault(BuiltinFunctionId.Split, "SPLIT", stringArrayType, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.SplitSeparator, "SPLIT", stringArrayType, [Parameter("text", TypeSymbol.String), Parameter("separator", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Join, "JOIN", TypeSymbol.String, [Parameter("items", stringArrayType)]),
            CreateDefault(BuiltinFunctionId.JoinSeparator, "JOIN", TypeSymbol.String, [Parameter("items", stringArrayType), Parameter("separator", TypeSymbol.String)])
        ];
    }
}
