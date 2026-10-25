using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed class BytecodeStackEffectTests
{
    private static readonly IReadOnlyDictionary<int, BytecodeRoutine> RoutinesById = new Dictionary<int, BytecodeRoutine>
    {
        [0] = CreateRoutine(0, TypeSymbol.Void, parameterCount: 0),
        [1] = CreateRoutine(1, TypeSymbol.Void, parameterCount: 2),
        [2] = CreateRoutine(2, TypeSymbol.Integer, parameterCount: 0),
        [3] = CreateRoutine(3, TypeSymbol.Integer, parameterCount: 2)
    };

    private static readonly IReadOnlyDictionary<int, BuiltinBinding> BuiltinsById = new Dictionary<int, BuiltinBinding>
    {
        [0] = new BuiltinBinding(0, BuiltinFunctionId.Abs, "NoResultZero", parameterCount: 0, returnsValue: false),
        [1] = new BuiltinBinding(1, BuiltinFunctionId.Abs, "NoResultTwo", parameterCount: 2, returnsValue: false),
        [2] = new BuiltinBinding(2, BuiltinFunctionId.Abs, "ValueZero", parameterCount: 0, returnsValue: true),
        [3] = new BuiltinBinding(3, BuiltinFunctionId.Abs, "ValueTwo", parameterCount: 2, returnsValue: true)
    };

    [Theory]
    [MemberData(nameof(StackEffectCases))]
    public void TryGet_ValidInstruction_ReturnsDeclaredStackEffect(
        string form,
        BytecodeInstruction instruction,
        int expectedPopCount,
        int expectedPushCount)
    {
        var resolved = BytecodeStackEffect.TryGet(instruction, RoutinesById, BuiltinsById, out var popCount, out var pushCount);

        Assert.True(resolved, form);
        Assert.Equal(expectedPopCount, popCount);
        Assert.Equal(expectedPushCount, pushCount);
    }

    [Fact]
    public void StackEffectCases_DeclareEveryOpcodeAsNonTerminalOrReturn()
    {
        var cases = StackEffectCases().ToArray();
        var nonTerminalOpcodes = cases.Select(static item => ((BytecodeInstruction)item[1]).Opcode).Distinct().ToArray();
        var terminalReturn = Opcode.Return;
        var declaredOpcodes = nonTerminalOpcodes.Append(terminalReturn).OrderBy(static opcode => (int)opcode).ToArray();

        Assert.DoesNotContain(terminalReturn, nonTerminalOpcodes);
        Assert.Equal(Enum.GetValues<Opcode>().OrderBy(static opcode => (int)opcode).ToArray(), declaredOpcodes);
        Assert.Equal(cases.Length, cases.Select(static item => (string)item[0]).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TryGet_Return_LeavesTerminalInstructionToRoutineValidation()
    {
        var resolved = BytecodeStackEffect.TryGet(
            new BytecodeInstruction(Opcode.Return),
            RoutinesById,
            BuiltinsById,
            out _,
            out _);

        Assert.False(resolved);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void TryGet_UnknownOpcode_ReturnsFalse(int opcode)
    {
        var resolved = BytecodeStackEffect.TryGet(
            new BytecodeInstruction((Opcode)opcode),
            RoutinesById,
            BuiltinsById,
            out _,
            out _);

        Assert.False(resolved);
    }

    public static IEnumerable<object[]> StackEffectCases()
    {
        yield return ["PushConst", new BytecodeInstruction(Opcode.PushConst, [0]), 0, 1];
        yield return ["PushIntegerZero", new BytecodeInstruction(Opcode.PushIntegerZero), 0, 1];
        yield return ["PushIntegerOne", new BytecodeInstruction(Opcode.PushIntegerOne), 0, 1];
        yield return ["LoadFunction", new BytecodeInstruction(Opcode.LoadFunction, [0]), 0, 1];
        yield return ["LoadBuiltinFunction", new BytecodeInstruction(Opcode.LoadBuiltinFunction, [0]), 0, 1];
        yield return ["LoadGlobal", new BytecodeInstruction(Opcode.LoadGlobal, [0]), 0, 1];
        yield return ["StoreGlobal", new BytecodeInstruction(Opcode.StoreGlobal, [0]), 1, 0];
        yield return ["AppendStringGlobal.String", new BytecodeInstruction(Opcode.AppendStringGlobal, [0, 0]), 1, 0];
        yield return ["AppendStringGlobal.Display", new BytecodeInstruction(Opcode.AppendStringGlobal, [0, 1]), 1, 0];
        yield return ["LoadLocal", new BytecodeInstruction(Opcode.LoadLocal, [0]), 0, 1];
        yield return ["StoreLocal", new BytecodeInstruction(Opcode.StoreLocal, [0]), 1, 0];
        yield return ["AppendStringLocal.String", new BytecodeInstruction(Opcode.AppendStringLocal, [0, 0]), 1, 0];
        yield return ["AppendStringLocal.Display", new BytecodeInstruction(Opcode.AppendStringLocal, [0, 1]), 1, 0];
        yield return ["LoadArgument", new BytecodeInstruction(Opcode.LoadArgument, [0]), 0, 1];
        yield return ["StoreArgument", new BytecodeInstruction(Opcode.StoreArgument, [0]), 1, 0];
        yield return ["AppendStringArgument.String", new BytecodeInstruction(Opcode.AppendStringArgument, [0, 0]), 1, 0];
        yield return ["AppendStringArgument.Display", new BytecodeInstruction(Opcode.AppendStringArgument, [0, 1]), 1, 0];
        yield return ["LoadCapture", new BytecodeInstruction(Opcode.LoadCapture, [0]), 0, 1];
        yield return ["StoreCapture", new BytecodeInstruction(Opcode.StoreCapture, [0]), 1, 0];
        yield return ["AppendStringCapture.String", new BytecodeInstruction(Opcode.AppendStringCapture, [0, 0]), 1, 0];
        yield return ["AppendStringCapture.Display", new BytecodeInstruction(Opcode.AppendStringCapture, [0, 1]), 1, 0];
        yield return ["Pop", new BytecodeInstruction(Opcode.Pop), 1, 0];
        yield return ["Negate", new BytecodeInstruction(Opcode.Negate), 1, 1];
        yield return ["Concat", new BytecodeInstruction(Opcode.Concat), 2, 1];
        yield return ["Add", new BytecodeInstruction(Opcode.Add), 2, 1];
        yield return ["AddLongInteger", new BytecodeInstruction(Opcode.AddLongInteger), 2, 1];
        yield return ["Subtract", new BytecodeInstruction(Opcode.Subtract), 2, 1];
        yield return ["Multiply", new BytecodeInstruction(Opcode.Multiply), 2, 1];
        yield return ["Power", new BytecodeInstruction(Opcode.Power), 2, 1];
        yield return ["Divide", new BytecodeInstruction(Opcode.Divide), 2, 1];
        yield return ["IntegralDivide", new BytecodeInstruction(Opcode.IntegralDivide), 2, 1];
        yield return ["Modulo", new BytecodeInstruction(Opcode.Modulo), 2, 1];
        yield return ["Not", new BytecodeInstruction(Opcode.Not), 1, 1];
        yield return ["And", new BytecodeInstruction(Opcode.And), 2, 1];
        yield return ["Or", new BytecodeInstruction(Opcode.Or), 2, 1];
        yield return ["Xor", new BytecodeInstruction(Opcode.Xor), 2, 1];
        yield return ["Eqv", new BytecodeInstruction(Opcode.Eqv), 2, 1];
        yield return ["Imp", new BytecodeInstruction(Opcode.Imp), 2, 1];
        yield return ["CompareEqual", new BytecodeInstruction(Opcode.CompareEqual), 2, 1];
        yield return ["CompareNotEqual", new BytecodeInstruction(Opcode.CompareNotEqual), 2, 1];
        yield return ["CompareLess", new BytecodeInstruction(Opcode.CompareLess), 2, 1];
        yield return ["CompareLessOrEqual", new BytecodeInstruction(Opcode.CompareLessOrEqual), 2, 1];
        yield return ["CompareGreater", new BytecodeInstruction(Opcode.CompareGreater), 2, 1];
        yield return ["CompareGreaterOrEqual", new BytecodeInstruction(Opcode.CompareGreaterOrEqual), 2, 1];
        yield return ["ShiftLeft", new BytecodeInstruction(Opcode.ShiftLeft), 2, 1];
        yield return ["ShiftRight", new BytecodeInstruction(Opcode.ShiftRight), 2, 1];
        yield return ["Jump", new BytecodeInstruction(Opcode.Jump, [0]), 0, 0];
        yield return ["JumpIfEqual", new BytecodeInstruction(Opcode.JumpIfEqual, [0]), 2, 0];
        yield return ["JumpIfNotEqual", new BytecodeInstruction(Opcode.JumpIfNotEqual, [0]), 2, 0];
        yield return ["JumpIfLess", new BytecodeInstruction(Opcode.JumpIfLess, [0]), 2, 0];
        yield return ["JumpIfLessOrEqual", new BytecodeInstruction(Opcode.JumpIfLessOrEqual, [0]), 2, 0];
        yield return ["JumpIfGreater", new BytecodeInstruction(Opcode.JumpIfGreater, [0]), 2, 0];
        yield return ["JumpIfGreaterOrEqual", new BytecodeInstruction(Opcode.JumpIfGreaterOrEqual, [0]), 2, 0];
        yield return ["JumpIfForDoneLocal", new BytecodeInstruction(Opcode.JumpIfForDoneLocal, [0, 1, 2, 0]), 0, 0];
        yield return ["JumpIfTrue", new BytecodeInstruction(Opcode.JumpIfTrue, [0]), 1, 0];
        yield return ["JumpIfFalse", new BytecodeInstruction(Opcode.JumpIfFalse, [0]), 1, 0];

        yield return ["Call.VoidZero", new BytecodeInstruction(Opcode.Call, [0, 0]), 0, 0];
        yield return ["Call.VoidTwo", new BytecodeInstruction(Opcode.Call, [1, 2]), 2, 0];
        yield return ["Call.ValueZero", new BytecodeInstruction(Opcode.Call, [2, 0]), 0, 1];
        yield return ["Call.ValueTwo", new BytecodeInstruction(Opcode.Call, [3, 2]), 2, 1];
        yield return ["CallByRef.VoidZero", new BytecodeInstruction(Opcode.CallByRef, [0, 0, 0, 0]), 0, 0];
        yield return ["CallByRef.VoidAllByVal", new BytecodeInstruction(Opcode.CallByRef, [1, 2, 2, 0]), 2, 0];
        yield return ["CallByRef.VoidMixed", new BytecodeInstruction(Opcode.CallByRef, [1, 2, 1, 1, 1, 0, 0]), 1, 0];
        yield return ["CallByRef.VoidAllByRef", new BytecodeInstruction(Opcode.CallByRef, [1, 2, 0, 2, 0, 0, 0, 1, 0, 1]), 0, 0];
        yield return ["CallByRef.ValueZero", new BytecodeInstruction(Opcode.CallByRef, [2, 0, 0, 0]), 0, 1];
        yield return ["CallByRef.ValueAllByVal", new BytecodeInstruction(Opcode.CallByRef, [3, 2, 2, 0]), 2, 1];
        yield return ["CallByRef.ValueMixed", new BytecodeInstruction(Opcode.CallByRef, [3, 2, 1, 1, 1, 0, 0]), 1, 1];
        yield return ["CallByRef.ValueAllByRef", new BytecodeInstruction(Opcode.CallByRef, [3, 2, 0, 2, 0, 0, 0, 1, 0, 1]), 0, 1];
        yield return ["CallValue.VoidZero", new BytecodeInstruction(Opcode.CallValue, [0, 0]), 1, 0];
        yield return ["CallValue.VoidOne", new BytecodeInstruction(Opcode.CallValue, [1, 0]), 2, 0];
        yield return ["CallValue.VoidThree", new BytecodeInstruction(Opcode.CallValue, [3, 0]), 4, 0];
        yield return ["CallValue.ValueZero", new BytecodeInstruction(Opcode.CallValue, [0, 1]), 1, 1];
        yield return ["CallValue.ValueOne", new BytecodeInstruction(Opcode.CallValue, [1, 1]), 2, 1];
        yield return ["CallValue.ValueThree", new BytecodeInstruction(Opcode.CallValue, [3, 1]), 4, 1];

        yield return ["RedimArray.RankOne", new BytecodeInstruction(Opcode.RedimArray, [0, 0, 1]), 3, 0];
        yield return ["RedimArray.RankOnePreserve", new BytecodeInstruction(Opcode.RedimArray, [1, 0, 1]), 3, 0];
        yield return ["RedimArray.RankTwo", new BytecodeInstruction(Opcode.RedimArray, [0, 0, 2]), 5, 0];
        yield return ["RedimArray.RankTwoPreserve", new BytecodeInstruction(Opcode.RedimArray, [1, 0, 2]), 5, 0];
        yield return ["EraseArray", new BytecodeInstruction(Opcode.EraseArray), 1, 0];
        yield return ["LoadElement.RankOneIndex", new BytecodeInstruction(Opcode.LoadElement, [0, 1, 0]), 2, 1];
        yield return ["LoadElement.RankOneRange", new BytecodeInstruction(Opcode.LoadElement, [0, 1, 1]), 3, 1];
        yield return ["LoadElement.RankTwoIndices", new BytecodeInstruction(Opcode.LoadElement, [0, 2, 0]), 3, 1];
        yield return ["LoadElement.RankTwoFirstRange", new BytecodeInstruction(Opcode.LoadElement, [0, 2, 1]), 4, 1];
        yield return ["LoadElement.RankTwoSecondRange", new BytecodeInstruction(Opcode.LoadElement, [0, 2, 2]), 4, 1];
        yield return ["LoadElement.RankTwoRanges", new BytecodeInstruction(Opcode.LoadElement, [0, 2, 3]), 5, 1];
        yield return ["StoreElement.RankOne", new BytecodeInstruction(Opcode.StoreElement, [0, 1]), 3, 0];
        yield return ["StoreElement.RankTwo", new BytecodeInstruction(Opcode.StoreElement, [0, 2]), 4, 0];
        yield return ["CreateRecord.Type", new BytecodeInstruction(Opcode.CreateRecord, [0]), 0, 1];
        yield return ["CreateRecord.Constructor", new BytecodeInstruction(Opcode.CreateRecord, [0, 0]), 0, 1];
        yield return ["LoadField", new BytecodeInstruction(Opcode.LoadField, [0]), 1, 1];
        yield return ["StoreField", new BytecodeInstruction(Opcode.StoreField, [0]), 2, 0];
        yield return ["CallBuiltin.VoidZero", new BytecodeInstruction(Opcode.CallBuiltin, [0, 0]), 0, 0];
        yield return ["CallBuiltin.VoidTwo", new BytecodeInstruction(Opcode.CallBuiltin, [1, 2]), 2, 0];
        yield return ["CallBuiltin.ValueZero", new BytecodeInstruction(Opcode.CallBuiltin, [2, 0]), 0, 1];
        yield return ["CallBuiltin.ValueTwo", new BytecodeInstruction(Opcode.CallBuiltin, [3, 2]), 2, 1];
        yield return ["ValidateTypeInvariant.Type", new BytecodeInstruction(Opcode.ValidateTypeInvariant, [0]), 1, 0];
        yield return ["ValidateTypeInvariant.Constructor", new BytecodeInstruction(Opcode.ValidateTypeInvariant, [0, 0]), 1, 0];
        yield return ["ValidateForStep", new BytecodeInstruction(Opcode.ValidateForStep), 1, 0];
        yield return ["StatementBoundary", new BytecodeInstruction(Opcode.StatementBoundary), 0, 0];

        yield return ["RegisterKeyHandler.NoKeyText", new BytecodeInstruction(Opcode.RegisterKeyHandler, [0]), 2, 0];
        yield return ["RegisterKeyHandler.KeyText", new BytecodeInstruction(Opcode.RegisterKeyHandler, [1]), 2, 0];
        yield return ["UnregisterKeyHandler", new BytecodeInstruction(Opcode.UnregisterKeyHandler), 1, 0];
        yield return ["PauseKeyHandler", new BytecodeInstruction(Opcode.PauseKeyHandler), 1, 0];
        yield return ["ResumeKeyHandler", new BytecodeInstruction(Opcode.ResumeKeyHandler), 1, 0];
        yield return ["RegisterMouseHandler.NoEventPress", new BytecodeInstruction(Opcode.RegisterMouseHandler, [0, 0]), 2, 0];
        yield return ["RegisterMouseHandler.EventWheel", new BytecodeInstruction(Opcode.RegisterMouseHandler, [1, 3]), 2, 0];
        yield return ["UnregisterMouseHandler.Press", new BytecodeInstruction(Opcode.UnregisterMouseHandler, [0]), 1, 0];
        yield return ["PauseMouseHandler.Press", new BytecodeInstruction(Opcode.PauseMouseHandler, [0]), 1, 0];
        yield return ["ResumeMouseHandler.Wheel", new BytecodeInstruction(Opcode.ResumeMouseHandler, [3]), 1, 0];

        yield return ["Screen", new BytecodeInstruction(Opcode.Screen), 4, 0];
        yield return ["Canvas", new BytecodeInstruction(Opcode.Canvas), 2, 0];
        yield return ["BeginFrame", new BytecodeInstruction(Opcode.BeginFrame), 0, 0];
        yield return ["Present", new BytecodeInstruction(Opcode.Present), 0, 0];
        yield return ["Camera", new BytecodeInstruction(Opcode.Camera), 2, 0];
        yield return ["Viewport.Reset", new BytecodeInstruction(Opcode.Viewport, [0]), 0, 0];
        yield return ["Viewport.Coordinates", new BytecodeInstruction(Opcode.Viewport, [4]), 4, 0];
        yield return ["ResetCamera", new BytecodeInstruction(Opcode.ResetCamera), 0, 0];
        yield return ["View.Reset", new BytecodeInstruction(Opcode.View, [0, 0]), 0, 0];
        yield return ["View.Coordinates", new BytecodeInstruction(Opcode.View, [2, 4]), 4, 0];
        yield return ["View.Fill", new BytecodeInstruction(Opcode.View, [6, 5]), 5, 0];
        yield return ["View.Border", new BytecodeInstruction(Opcode.View, [10, 5]), 5, 0];
        yield return ["View.FillBorder", new BytecodeInstruction(Opcode.View, [14, 6]), 6, 0];
        yield return ["View.ScreenFillBorder", new BytecodeInstruction(Opcode.View, [15, 6]), 6, 0];
        yield return ["ViewPrint.Reset", new BytecodeInstruction(Opcode.ViewPrint, [0]), 0, 0];
        yield return ["ViewPrint.Rows", new BytecodeInstruction(Opcode.ViewPrint, [2]), 2, 0];
        yield return ["Window.Reset", new BytecodeInstruction(Opcode.Window, [0, 0]), 0, 0];
        yield return ["Window.Coordinates", new BytecodeInstruction(Opcode.Window, [2, 4]), 4, 0];
        yield return ["Window.ScreenCoordinates", new BytecodeInstruction(Opcode.Window, [3, 4]), 4, 0];
        yield return ["Cls.Default", new BytecodeInstruction(Opcode.Cls, [0]), 0, 0];
        yield return ["Cls.Color", new BytecodeInstruction(Opcode.Cls, [1]), 1, 0];
        yield return ["Color.Foreground", new BytecodeInstruction(Opcode.Color, [0, 1]), 1, 0];
        yield return ["Color.Background", new BytecodeInstruction(Opcode.Color, [1, 2]), 2, 0];
        yield return ["Color.Transparent", new BytecodeInstruction(Opcode.Color, [2, 1]), 1, 0];
        yield return ["Locate", new BytecodeInstruction(Opcode.Locate), 2, 0];
        yield return ["Palette.Reset", new BytecodeInstruction(Opcode.Palette, [0]), 0, 0];
        yield return ["Palette.Array", new BytecodeInstruction(Opcode.Palette, [1]), 1, 0];
        yield return ["Palette.Color", new BytecodeInstruction(Opcode.Palette, [2]), 2, 0];
        yield return ["PCopy", new BytecodeInstruction(Opcode.PCopy), 2, 0];
        yield return ["PSet.Absolute", new BytecodeInstruction(Opcode.PSet, [0, 2]), 2, 0];
        yield return ["PSet.AbsoluteColor", new BytecodeInstruction(Opcode.PSet, [0, 3]), 3, 0];
        yield return ["PSet.Reset", new BytecodeInstruction(Opcode.PSet, [1, 2]), 2, 0];
        yield return ["PSet.Relative", new BytecodeInstruction(Opcode.PSet, [2, 2]), 2, 0];
        yield return ["PSet.RelativeResetColor", new BytecodeInstruction(Opcode.PSet, [3, 3]), 3, 0];
        yield return ["Get.Absolute", new BytecodeInstruction(Opcode.Get, [0, 5]), 5, 0];
        yield return ["Get.RelativeBoth", new BytecodeInstruction(Opcode.Get, [3, 5]), 5, 0];
        yield return ["Put.PSet", new BytecodeInstruction(Opcode.Put, [0, 3]), 3, 0];
        yield return ["Put.RelativePSet", new BytecodeInstruction(Opcode.Put, [1, 3]), 3, 0];
        yield return ["Put.RelativeXor", new BytecodeInstruction(Opcode.Put, [9, 3]), 3, 0];

        yield return ["Line.CurrentStart", new BytecodeInstruction(Opcode.Line, [0, 2]), 2, 0];
        yield return ["Line.CurrentStartColor", new BytecodeInstruction(Opcode.Line, [0, 3]), 3, 0];
        yield return ["Line.CurrentStartBox", new BytecodeInstruction(Opcode.Line, [1, 2]), 2, 0];
        yield return ["Line.CurrentStartFilledBox", new BytecodeInstruction(Opcode.Line, [2, 2]), 2, 0];
        yield return ["Line.RelativeEnd", new BytecodeInstruction(Opcode.Line, [16, 2]), 2, 0];
        yield return ["Line.ExplicitStart", new BytecodeInstruction(Opcode.Line, [4, 4]), 4, 0];
        yield return ["Line.ExplicitStartColor", new BytecodeInstruction(Opcode.Line, [4, 5]), 5, 0];
        yield return ["Line.RelativeStart", new BytecodeInstruction(Opcode.Line, [12, 4]), 4, 0];
        yield return ["Line.RelativeBoth", new BytecodeInstruction(Opcode.Line, [28, 4]), 4, 0];
        yield return ["Line.RelativeBothFilledBoxColor", new BytecodeInstruction(Opcode.Line, [30, 5]), 5, 0];

        yield return ["Circle.Flags0", new BytecodeInstruction(Opcode.Circle, [0, 3]), 3, 0];
        yield return ["Circle.Flags2", new BytecodeInstruction(Opcode.Circle, [2, 4]), 4, 0];
        yield return ["Circle.Flags4", new BytecodeInstruction(Opcode.Circle, [4, 4]), 4, 0];
        yield return ["Circle.Flags6", new BytecodeInstruction(Opcode.Circle, [6, 5]), 5, 0];
        yield return ["Circle.Flags8", new BytecodeInstruction(Opcode.Circle, [8, 4]), 4, 0];
        yield return ["Circle.Flags10", new BytecodeInstruction(Opcode.Circle, [10, 5]), 5, 0];
        yield return ["Circle.Flags12", new BytecodeInstruction(Opcode.Circle, [12, 5]), 5, 0];
        yield return ["Circle.Flags14", new BytecodeInstruction(Opcode.Circle, [14, 6]), 6, 0];
        yield return ["Circle.Flags16", new BytecodeInstruction(Opcode.Circle, [16, 4]), 4, 0];
        yield return ["Circle.Flags18", new BytecodeInstruction(Opcode.Circle, [18, 5]), 5, 0];
        yield return ["Circle.Flags20", new BytecodeInstruction(Opcode.Circle, [20, 5]), 5, 0];
        yield return ["Circle.Flags22", new BytecodeInstruction(Opcode.Circle, [22, 6]), 6, 0];
        yield return ["Circle.Flags24", new BytecodeInstruction(Opcode.Circle, [24, 5]), 5, 0];
        yield return ["Circle.Flags26", new BytecodeInstruction(Opcode.Circle, [26, 6]), 6, 0];
        yield return ["Circle.Flags28", new BytecodeInstruction(Opcode.Circle, [28, 6]), 6, 0];
        yield return ["Circle.Flags30", new BytecodeInstruction(Opcode.Circle, [30, 7]), 7, 0];
        yield return ["Circle.Flags31", new BytecodeInstruction(Opcode.Circle, [31, 7]), 7, 0];
        yield return ["Paint.Flags0", new BytecodeInstruction(Opcode.Paint, [0, 2]), 2, 0];
        yield return ["Paint.Flags2", new BytecodeInstruction(Opcode.Paint, [2, 3]), 3, 0];
        yield return ["Paint.Flags4", new BytecodeInstruction(Opcode.Paint, [4, 3]), 3, 0];
        yield return ["Paint.Flags6", new BytecodeInstruction(Opcode.Paint, [6, 4]), 4, 0];
        yield return ["Paint.Flags8", new BytecodeInstruction(Opcode.Paint, [8, 3]), 3, 0];
        yield return ["Paint.Flags10", new BytecodeInstruction(Opcode.Paint, [10, 4]), 4, 0];
        yield return ["Paint.Flags12", new BytecodeInstruction(Opcode.Paint, [12, 4]), 4, 0];
        yield return ["Paint.Flags14", new BytecodeInstruction(Opcode.Paint, [14, 5]), 5, 0];
        yield return ["Paint.Flags15", new BytecodeInstruction(Opcode.Paint, [15, 5]), 5, 0];

        yield return ["Draw", new BytecodeInstruction(Opcode.Draw, [1]), 1, 0];
        yield return ["DrawCircle", new BytecodeInstruction(Opcode.DrawCircle), 4, 0];
        yield return ["DrawRect", new BytecodeInstruction(Opcode.DrawRect), 5, 0];
        yield return ["DrawLine", new BytecodeInstruction(Opcode.DrawLine), 5, 0];
        yield return ["FillRect", new BytecodeInstruction(Opcode.FillRect), 5, 0];
        yield return ["FillCircle", new BytecodeInstruction(Opcode.FillCircle), 4, 0];
        yield return ["DrawImage.NaturalSize", new BytecodeInstruction(Opcode.DrawImage, [0]), 3, 0];
        yield return ["DrawImage.Scaled", new BytecodeInstruction(Opcode.DrawImage, [1]), 5, 0];
        yield return ["DrawSprite.NaturalSize", new BytecodeInstruction(Opcode.DrawSprite, [0]), 4, 0];
        yield return ["DrawSprite.Scaled", new BytecodeInstruction(Opcode.DrawSprite, [1]), 6, 0];
        yield return ["DrawTile", new BytecodeInstruction(Opcode.DrawTile), 4, 0];
        yield return ["DrawTilemap", new BytecodeInstruction(Opcode.DrawTilemap), 3, 0];
        yield return ["DrawTileLayer", new BytecodeInstruction(Opcode.DrawTileLayer), 4, 0];
        yield return ["SetMapTile", new BytecodeInstruction(Opcode.SetMapTile), 7, 0];
        yield return ["DrawText", new BytecodeInstruction(Opcode.DrawText), 4, 0];
        yield return ["AdvanceForLoopLocal", new BytecodeInstruction(Opcode.AdvanceForLoopLocal, [0, 1]), 0, 0];
        yield return ["PrintValue", new BytecodeInstruction(Opcode.PrintValue), 1, 0];
        yield return ["PrintUsing.ZeroNoSeparator", new BytecodeInstruction(Opcode.PrintUsing, [0, 0]), 1, 0];
        yield return ["PrintUsing.OneNoSeparator", new BytecodeInstruction(Opcode.PrintUsing, [1, 0]), 2, 0];
        yield return ["PrintUsing.ThreeNoSeparator", new BytecodeInstruction(Opcode.PrintUsing, [3, 0]), 4, 0];
        yield return ["PrintUsing.ThreeSemicolon", new BytecodeInstruction(Opcode.PrintUsing, [3, 1]), 4, 0];
        yield return ["PrintUsing.ThreeComma", new BytecodeInstruction(Opcode.PrintUsing, [3, 2]), 4, 0];
        yield return ["PrintSeparator.Semicolon", new BytecodeInstruction(Opcode.PrintSeparator, [0]), 0, 0];
        yield return ["PrintSeparator.Comma", new BytecodeInstruction(Opcode.PrintSeparator, [1]), 0, 0];
        yield return ["PrintLine", new BytecodeInstruction(Opcode.PrintLine), 0, 0];
        yield return ["Input.Zero", new BytecodeInstruction(Opcode.Input, [0, 0]), 0, 0];
        yield return ["Input.One", new BytecodeInstruction(Opcode.Input, [1, 0, 0, 0]), 0, 0];
        yield return ["Input.TwoWithPrompt", new BytecodeInstruction(Opcode.Input, [2, 1, 0, 0, 1, 0]), 0, 0];
        yield return ["Sleep.Key", new BytecodeInstruction(Opcode.Sleep, [0]), 0, 0];
        yield return ["Sleep.Duration", new BytecodeInstruction(Opcode.Sleep, [1]), 1, 0];
        yield return ["ReseedRandom.NoSeed", new BytecodeInstruction(Opcode.ReseedRandom, [0]), 0, 0];
        yield return ["ReseedRandom.Seed", new BytecodeInstruction(Opcode.ReseedRandom, [1]), 1, 0];
        yield return ["FormatInterpolatedPart", new BytecodeInstruction(Opcode.FormatInterpolatedPart), 3, 1];
        yield return ["OpenFile.Input", new BytecodeInstruction(Opcode.OpenFile, [0, 0]), 2, 1];
        yield return ["OpenFile.InputEncoding", new BytecodeInstruction(Opcode.OpenFile, [0, 0, 1]), 3, 1];
        yield return ["OpenFile.Output", new BytecodeInstruction(Opcode.OpenFile, [1, 0]), 2, 1];
        yield return ["OpenFile.OutputEncoding", new BytecodeInstruction(Opcode.OpenFile, [1, 0, 1]), 3, 1];
        yield return ["OpenFile.Append", new BytecodeInstruction(Opcode.OpenFile, [2, 0]), 2, 1];
        yield return ["OpenFile.AppendEncoding", new BytecodeInstruction(Opcode.OpenFile, [2, 0, 1]), 3, 1];
        yield return ["CloseFile", new BytecodeInstruction(Opcode.CloseFile), 1, 1];
        yield return ["Convert.Integer", new BytecodeInstruction(Opcode.Convert, [(int)BytecodeScalarTypeCode.Integer]), 1, 1];
        yield return ["Convert.String", new BytecodeInstruction(Opcode.Convert, [(int)BytecodeScalarTypeCode.String]), 1, 1];
        yield return ["PushErrorHandler", new BytecodeInstruction(Opcode.PushErrorHandler, [-1, -1, 0, -1, -1]), 0, 0];
        yield return ["PopErrorHandler", new BytecodeInstruction(Opcode.PopErrorHandler), 0, 0];
        yield return ["RaiseError.Code", new BytecodeInstruction(Opcode.RaiseError, [0]), 1, 0];
        yield return ["RaiseError.CodeMessage", new BytecodeInstruction(Opcode.RaiseError, [1]), 2, 0];
        yield return ["RaiseError.Existing", new BytecodeInstruction(Opcode.RaiseError, [2]), 1, 0];
        yield return ["EndFinally", new BytecodeInstruction(Opcode.EndFinally), 0, 0];
        yield return ["Smooth.Off", new BytecodeInstruction(Opcode.Smooth, [0]), 0, 0];
        yield return ["Smooth.On", new BytecodeInstruction(Opcode.Smooth, [1]), 0, 0];
        yield return ["SnapshotValue", new BytecodeInstruction(Opcode.SnapshotValue), 1, 1];
        yield return ["NewArray", new BytecodeInstruction(Opcode.NewArray, [0, 1]), 1, 1];
        yield return ["GetArrayStructuralVersion", new BytecodeInstruction(Opcode.GetArrayStructuralVersion), 1, 1];
        yield return ["FreshLocal", new BytecodeInstruction(Opcode.FreshLocal, [0]), 0, 0];
        yield return ["BindArrayElement", new BytecodeInstruction(Opcode.BindArrayElement, [0, 0, 0, 1]), 0, 0];
        yield return ["ClearForEachItem", new BytecodeInstruction(Opcode.ClearForEachItem, [0]), 0, 0];
    }

    private static BytecodeRoutine CreateRoutine(int id, TypeSymbol resultType, int parameterCount)
    {
        return new BytecodeRoutine(
            id,
            $"Routine{id}",
            new TextLocation(new TextSpan(0, 0), 1, 1),
            resultType,
            parameterCount,
            localSlotCount: 0,
            instructions: []);
    }
}
