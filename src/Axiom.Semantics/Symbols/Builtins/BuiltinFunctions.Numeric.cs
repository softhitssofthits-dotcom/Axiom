namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetNumericDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Rnd, "RND", TypeSymbol.Double, []),
            CreateNumeric(BuiltinFunctionId.Abs, "ABS", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Sgn, "SGN", TypeSymbol.Integer, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.Integer, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Min, "MIN", TypeSymbol.Double, BuiltinOperandBindingPolicy.PromoteNumericArguments, BuiltinResultTypePolicy.PromotedNumeric, [Parameter("left", TypeSymbol.Double), Parameter("right", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Max, "MAX", TypeSymbol.Double, BuiltinOperandBindingPolicy.PromoteNumericArguments, BuiltinResultTypePolicy.PromotedNumeric, [Parameter("left", TypeSymbol.Double), Parameter("right", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Clamp, "CLAMP", TypeSymbol.Double, BuiltinOperandBindingPolicy.PromoteNumericArguments, BuiltinResultTypePolicy.PromotedNumeric, [Parameter("value", TypeSymbol.Double), Parameter("low", TypeSymbol.Double), Parameter("high", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Sin, "SIN", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Cos, "COS", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Tan, "TAN", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Atn, "ATN", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Atn2, "ATN2", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("y", TypeSymbol.Double), Parameter("x", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Sqr, "SQR", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Cbrt, "CBRT", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Exp, "EXP", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Log, "LOG", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Log10, "LOG10", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Log2, "LOG2", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Pow, "POW", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double), Parameter("power", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Floor, "FLOOR", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Ceil, "CEIL", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Round, "ROUND", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Fix, "FIX", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Asin, "ASIN", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Acos, "ACOS", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Asinh, "ASINH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Acosh, "ACOSH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Atanh, "ATANH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Sinh, "SINH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Cosh, "COSH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.Tanh, "TANH", TypeSymbol.Double, BuiltinOperandBindingPolicy.ConvertNumericArgumentsToDouble, BuiltinResultTypePolicy.Double, [Parameter("value", TypeSymbol.Double)]),
            CreateNumeric(BuiltinFunctionId.SetSign, "SETSIGN", TypeSymbol.Double, BuiltinOperandBindingPolicy.PreserveNumericArguments, BuiltinResultTypePolicy.SameAsFirstArgument, [Parameter("value", TypeSymbol.Double), Parameter("signFrom", TypeSymbol.Double)])
        ];
    }
}
