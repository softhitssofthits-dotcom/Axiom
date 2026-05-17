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
            CreateDefault(BuiltinFunctionId.MousePress, "MOUSEPRESS", TypeSymbol.Boolean, [Parameter("buttonNumber", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.ScreenIsActive, "SCREENISACTIVE", TypeSymbol.Boolean, []),
            CreateDefault(BuiltinFunctionId.ScreenMode, "SCREENMODE", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.ScreenWidth, "SCREENWIDTH", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.ScreenHeight, "SCREENHEIGHT", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.PointPosition, "POINT", TypeSymbol.Integer, [Parameter("number", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Point, "POINT", TypeSymbol.Integer, [Parameter("x", TypeSymbol.Long), Parameter("y", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.PMap, "PMAP", TypeSymbol.Double, [Parameter("coordinate", TypeSymbol.Double), Parameter("selector", TypeSymbol.Integer)])
        ];
    }
}
