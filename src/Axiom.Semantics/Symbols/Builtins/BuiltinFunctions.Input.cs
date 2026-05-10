namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetInputDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.InKey, "INKEY", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.KeyDown, "KEYDOWN", TypeSymbol.Boolean, [Parameter("keyText", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.KeyPress, "KEYPRESS", TypeSymbol.Boolean, [Parameter("keyText", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MouseX, "MOUSEX", TypeSymbol.Long, []),
            CreateDefault(BuiltinFunctionId.MouseY, "MOUSEY", TypeSymbol.Long, []),
            CreateDefault(BuiltinFunctionId.MouseButton, "MOUSEBUTTON", TypeSymbol.Boolean, [Parameter("buttonNumber", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.MousePress, "MOUSEPRESS", TypeSymbol.Boolean, [Parameter("buttonNumber", TypeSymbol.Integer)])
        ];
    }
}
