namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetConversionDescriptors()
    {
        return
        [
            CreateNumeric(BuiltinFunctionId.Int, "INT", TypeSymbol.Integer, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.Integer, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Str, "STR", TypeSymbol.String, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.String, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Str, "STR$", TypeSymbol.String, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.String, [Parameter("value", TypeSymbol.Double)]),
            CreateDefault(BuiltinFunctionId.Val, "VAL", TypeSymbol.Double, [Parameter("text", TypeSymbol.String)])
        ];
    }
}
