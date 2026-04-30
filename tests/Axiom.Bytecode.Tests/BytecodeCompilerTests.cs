using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode.Tests;

public sealed class BytecodeCompilerTests
{
    [Fact]
    public void Compile_ScalarProgram_ProducesValidatedModule()
    {
        var global = new VariableSymbol("counter", TypeSymbol.Integer, isReadOnly: false, isGlobal: true);
        var parameter = new ParameterSymbol("value", TypeSymbol.Integer, ordinal: 0);
        var temp = new VariableSymbol("temp", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var increment = new FunctionSymbol("Increment", [parameter], TypeSymbol.Integer);
        var lenParameter = new ParameterSymbol("text", TypeSymbol.String, ordinal: 0);
        var lenBuiltin = new FunctionSymbol("LEN", [lenParameter], TypeSymbol.Integer, isBuiltIn: true, builtInId: BuiltinFunctionId.Len);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 41, TypeSymbol.Integer),
                LoweredInstruction.CallFunction(Location(2), increment),
                LoweredInstruction.StoreVariable(Location(2), global),
                LoweredInstruction.LoadVariable(Location(3), global),
                LoweredInstruction.PrintValue(Location(3)),
                LoweredInstruction.PrintSeparator(Location(3), PrintSeparatorKind.Semicolon),
                LoweredInstruction.LoadLiteral(Location(4), "abcd", TypeSymbol.String),
                LoweredInstruction.CallFunction(Location(4), lenBuiltin),
                LoweredInstruction.PrintValue(Location(4)),
                LoweredInstruction.PrintLine(Location(4)),
                LoweredInstruction.Return(Location(5))
            ],
            isEntryPoint: true);

        var incrementRoutine = new LoweredRoutine(
            id: 1,
            name: increment.Name,
            location: Location(10),
            resultType: TypeSymbol.Integer,
            instructions:
            [
                LoweredInstruction.LoadVariable(Location(11), parameter),
                LoweredInstruction.LoadLiteral(Location(11), 1, TypeSymbol.Integer),
                LoweredInstruction.BinaryOperation(Location(11), SyntaxKind.PlusToken),
                LoweredInstruction.StoreVariable(Location(11), temp),
                LoweredInstruction.LoadVariable(Location(12), temp),
                LoweredInstruction.Return(Location(12))
            ],
            symbol: increment,
            localVariables: [temp]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, [incrementRoutine]);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
        Assert.NotNull(result.Module);

        var module = result.Module!;
        Assert.Equal(0, module.EntryPointRoutineId);
        Assert.Equal(2, module.Routines.Count);
        var compiledGlobal = Assert.Single(module.Globals);
        Assert.Equal("counter", compiledGlobal.Name);
        Assert.Equal(GlobalInitializationStrategy.Deferred, compiledGlobal.InitializationStrategy);
        var builtin = Assert.Single(module.Builtins);
        Assert.Equal("LEN", builtin.Name);

        var entryInstructions = module.Routines.Single(static routine => routine.Id == 0).Instructions;
        Assert.Collection(
            entryInstructions,
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.Call, instruction.Opcode);
                Assert.Equal(1, instruction.GetOperand(0));
                Assert.Equal(1, instruction.GetOperand(1));
            },
            instruction =>
            {
                Assert.Equal(Opcode.StoreGlobal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.LoadGlobal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.PrintValue, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.PrintSeparator, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.CallBuiltin, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
                Assert.Equal(1, instruction.GetOperand(1));
            },
            instruction => Assert.Equal(Opcode.PrintValue, instruction.Opcode),
            instruction => Assert.Equal(Opcode.PrintLine, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));

        var incrementInstructions = module.Routines.Single(static routine => routine.Id == 1).Instructions;
        Assert.Collection(
            incrementInstructions,
            instruction =>
            {
                Assert.Equal(Opcode.LoadArgument, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Add, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.StoreLocal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.LoadLocal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));

        var validationResult = new BytecodeModuleValidator().Validate(module);
        Assert.True(validationResult.Succeeded);
        Assert.Equal(2, module.DebugInfo!.Routines.Count);
        Assert.Contains(module.DebugInfo.InstructionMap, map => map.RoutineId == 0 && map.InstructionIndex == 0 && map.Location.Line == 2);
        Assert.Contains(module.DebugInfo.InstructionMap, map => map.RoutineId == 1 && map.InstructionIndex == 0 && map.Location.Line == 11);
    }

    [Fact]
    public void Compile_Jumps_ResolveLabelTargetsAfterRemovingLabels()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), true, TypeSymbol.Boolean),
                LoweredInstruction.JumpIfFalse(Location(2), "L_END"),
                LoweredInstruction.LoadLiteral(Location(3), 10, TypeSymbol.Integer),
                LoweredInstruction.Pop(Location(3)),
                LoweredInstruction.Jump(Location(4), "L_DONE"),
                LoweredInstruction.LabelMarker(Location(5), "L_END"),
                LoweredInstruction.LoadLiteral(Location(6), 20, TypeSymbol.Integer),
                LoweredInstruction.Pop(Location(6)),
                LoweredInstruction.LabelMarker(Location(7), "L_DONE"),
                LoweredInstruction.Return(Location(7))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
        var instructions = result.Module!.Routines.Single().Instructions;
        Assert.Equal(8, instructions.Count);
        Assert.Equal(Opcode.JumpIfFalse, instructions[1].Opcode);
        Assert.Equal(5, instructions[1].GetOperand(0));
        Assert.Equal(Opcode.Jump, instructions[4].Opcode);
        Assert.Equal(7, instructions[4].GetOperand(0));
    }

    [Fact]
    public void Compile_Jumps_ResolveLabelTargetsAfterLoweredNoOpInstructions()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 1, TypeSymbol.Integer),
                LoweredInstruction.UnaryOperation(Location(2), SyntaxKind.PlusToken),
                LoweredInstruction.Pop(Location(2)),
                LoweredInstruction.Jump(Location(2), "L_DONE"),
                LoweredInstruction.LoadLiteral(Location(3), 99, TypeSymbol.Integer),
                LoweredInstruction.Pop(Location(3)),
                LoweredInstruction.LabelMarker(Location(4), "L_DONE"),
                LoweredInstruction.Return(Location(4))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);
        var instructions = result.Module!.Routines.Single().Instructions;
        Assert.Equal(6, instructions.Count);
        Assert.Equal(Opcode.Jump, instructions[2].Opcode);
        Assert.Equal(5, instructions[2].GetOperand(0));
    }

    [Fact]
    public void Compile_DuplicateLabel_ReturnsDiagnostic()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LabelMarker(Location(2), "L_DUP"),
                LoweredInstruction.LoadLiteral(Location(3), 1, TypeSymbol.Integer),
                LoweredInstruction.Pop(Location(3)),
                LoweredInstruction.LabelMarker(Location(4), "L_DUP"),
                LoweredInstruction.Return(Location(5))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC5001", diagnostic.Code);
        Assert.Equal(4, diagnostic.Location.Line);
    }

    [Fact]
    public void Compile_ValidateForStep_EmitsRuntimeInstruction()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 1, TypeSymbol.Integer),
                LoweredInstruction.ValidateForStep(Location(2)),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);
        var instructions = result.Module!.Routines.Single().Instructions;
        Assert.Collection(
            instructions,
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(Opcode.ValidateForStep, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Theory]
    [InlineData(SyntaxKind.CaretToken, Opcode.Power)]
    [InlineData(SyntaxKind.BackslashToken, Opcode.IntegralDivide)]
    [InlineData(SyntaxKind.LessLessToken, Opcode.ShiftLeft)]
    [InlineData(SyntaxKind.GreaterGreaterToken, Opcode.ShiftRight)]
    public void Compile_RemainingArithmeticOperators_EmitRuntimeInstruction(SyntaxKind operatorKind, Opcode expectedOpcode)
    {
        var operandType = operatorKind == SyntaxKind.CaretToken ? TypeSymbol.Double : TypeSymbol.Integer;
        object leftValue = operatorKind == SyntaxKind.CaretToken ? (object)8d : 8;
        object rightValue = operatorKind == SyntaxKind.CaretToken ? (object)2d : 2;

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), leftValue, operandType),
                LoweredInstruction.LoadLiteral(Location(2), rightValue, operandType),
                LoweredInstruction.BinaryOperation(Location(2), operatorKind),
                LoweredInstruction.Pop(Location(2)),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
        Assert.Collection(
            result.Module!.Routines.Single().Instructions,
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(expectedOpcode, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Pop, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_PrintUsing_EmitsPrintUsingOperands()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), "##.##", TypeSymbol.String),
                LoweredInstruction.LoadLiteral(Location(2), 3.14159d, TypeSymbol.Double),
                LoweredInstruction.PrintUsing(Location(2), valueCount: 1, PrintSeparatorKind.Comma),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);
        var instructions = result.Module!.Routines.Single().Instructions;
        Assert.Collection(
            instructions,
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.PrintUsing, instruction.Opcode);
                Assert.Equal(1, instruction.GetOperand(0));
                Assert.Equal(2, instruction.GetOperand(1));
            },
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_Input_EmitsInputTargetsAndPromptOperand()
    {
        var firstName = new VariableSymbol("firstName", TypeSymbol.String, isReadOnly: false, isGlobal: false);
        var age = new VariableSymbol("age", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.Input(Location(2), [firstName, age], "Pair"),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true,
            localVariables: [firstName, age]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);
        var module = result.Module!;
        var instructions = module.Routines.Single().Instructions;
        var promptIndex = module.ConstantPool.Entries
            .Select((constant, index) => (constant, index))
            .Single(pair => pair.constant.Kind == BytecodeConstantKind.String && (string)pair.constant.Value! == "Pair")
            .index;

        Assert.Collection(
            instructions,
            instruction =>
            {
                Assert.Equal(Opcode.Input, instruction.Opcode);
                Assert.Equal(2, instruction.GetOperand(0));
                Assert.Equal(promptIndex + 1, instruction.GetOperand(1));
                Assert.Equal(1, instruction.GetOperand(2));
                Assert.Equal(0, instruction.GetOperand(3));
                Assert.Equal(1, instruction.GetOperand(4));
                Assert.Equal(1, instruction.GetOperand(5));
            },
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_Conversion_EmitsConvertInstruction()
    {
        var value = new VariableSymbol("value", TypeSymbol.Decimal, isReadOnly: false, isGlobal: false);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 1, TypeSymbol.Integer),
                LoweredInstruction.Convert(Location(2), TypeSymbol.Decimal),
                LoweredInstruction.StoreVariable(Location(2), value),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true,
            localVariables: [value]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);

        Assert.Collection(
            result.Module!.Routines.Single().Instructions,
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.Convert, instruction.Opcode);
                Assert.Equal((int)BytecodeScalarTypeCode.Decimal, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.StoreLocal, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_UnsupportedConversionTarget_ReturnsDiagnostic()
    {
        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 1, TypeSymbol.Integer),
                LoweredInstruction.Convert(Location(2), TypeSymbol.String),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC5008", diagnostic.Code);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Compile_LongPlusConvertedInteger_CollapsesToAddLongInteger()
    {
        var total = new VariableSymbol("total", TypeSymbol.Long, isReadOnly: false, isGlobal: false);
        var value = new VariableSymbol("value", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadVariable(Location(2), total),
                LoweredInstruction.LoadVariable(Location(2), value),
                LoweredInstruction.Convert(Location(2), TypeSymbol.Long),
                LoweredInstruction.BinaryOperation(Location(2), SyntaxKind.PlusToken),
                LoweredInstruction.StoreVariable(Location(2), total),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true,
            localVariables: [total, value]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);

        Assert.Collection(
            result.Module!.Routines.Single().Instructions,
            instruction =>
            {
                Assert.Equal(Opcode.LoadLocal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.LoadLocal, instruction.Opcode);
                Assert.Equal(1, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.AddLongInteger, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.StoreLocal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_CompareGreaterThenJumpIfFalse_CollapsesToJumpIfLessOrEqual()
    {
        var left = new VariableSymbol("left", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var right = new VariableSymbol("right", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadVariable(Location(2), left),
                LoweredInstruction.LoadVariable(Location(2), right),
                LoweredInstruction.BinaryOperation(Location(2), SyntaxKind.GreaterToken),
                LoweredInstruction.JumpIfFalse(Location(2), "L_END"),
                LoweredInstruction.LoadLiteral(Location(3), 1, TypeSymbol.Integer),
                LoweredInstruction.Pop(Location(3)),
                LoweredInstruction.LabelMarker(Location(4), "L_END"),
                LoweredInstruction.Return(Location(4))
            ],
            isEntryPoint: true,
            localVariables: [left, right]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);

        Assert.Collection(
            result.Module!.Routines.Single().Instructions,
            instruction =>
            {
                Assert.Equal(Opcode.LoadLocal, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.LoadLocal, instruction.Opcode);
                Assert.Equal(1, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.JumpIfLessOrEqual, instruction.Opcode);
                Assert.Equal(5, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.PushConst, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Pop, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    [Fact]
    public void Compile_BuiltinFunctionValue_EmitsLoadBuiltinFunction()
    {
        var textParameter = new ParameterSymbol("text", TypeSymbol.String, ordinal: 0);
        var ucaseBuiltin = new FunctionSymbol("UCASE", [textParameter], TypeSymbol.String, isBuiltIn: true, builtInId: BuiltinFunctionId.UCase);
        var selectorVariable = new VariableSymbol(
            "selector",
            FunctionTypeSymbol.Create([TypeSymbol.String], TypeSymbol.String),
            isReadOnly: false,
            isGlobal: false);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadFunction(Location(2), ucaseBuiltin),
                LoweredInstruction.StoreVariable(Location(2), selectorVariable),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true,
            localVariables: [selectorVariable]);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, []);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
        var module = result.Module!;
        var instruction = Assert.Single(module.Routines.Single().Instructions, static instruction => instruction.Opcode == Opcode.LoadBuiltinFunction);
        var builtin = Assert.Single(module.Builtins);
        Assert.Equal(BuiltinFunctionId.UCase, builtin.CanonicalId);
        Assert.Same(ucaseBuiltin, builtin.Function);
        Assert.Equal(builtin.Id, instruction.GetOperand(0));
    }

    [Fact]
    public void Compile_LocalFunctionCapture_EmitsCaptureInstructions()
    {
        var outerParameter = new ParameterSymbol("seed", TypeSymbol.Integer, ordinal: 0);
        var outerLocal = new VariableSymbol("offset", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var outerFunction = new FunctionSymbol("Outer", [outerParameter], TypeSymbol.Integer);
        var innerParameter = new ParameterSymbol("value", TypeSymbol.Integer, ordinal: 0);
        var innerFunction = new FunctionSymbol("Inner", [innerParameter], TypeSymbol.Integer, containingRoutine: outerFunction);

        var outerRoutine = new LoweredRoutine(
            id: 1,
            name: outerFunction.Name,
            location: Location(10),
            resultType: TypeSymbol.Integer,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(11), 3, TypeSymbol.Integer),
                LoweredInstruction.StoreVariable(Location(11), outerLocal),
                LoweredInstruction.LoadLiteral(Location(12), 4, TypeSymbol.Integer),
                LoweredInstruction.CallFunction(Location(12), innerFunction),
                LoweredInstruction.Return(Location(12))
            ],
            symbol: outerFunction,
            localVariables: [outerLocal]);

        var innerRoutine = new LoweredRoutine(
            id: 2,
            name: innerFunction.Name,
            location: Location(20),
            resultType: TypeSymbol.Integer,
            instructions:
            [
                LoweredInstruction.LoadVariable(Location(21), outerParameter),
                LoweredInstruction.LoadVariable(Location(21), innerParameter),
                LoweredInstruction.BinaryOperation(Location(21), SyntaxKind.StarToken),
                LoweredInstruction.LoadVariable(Location(21), outerLocal),
                LoweredInstruction.BinaryOperation(Location(21), SyntaxKind.PlusToken),
                LoweredInstruction.Return(Location(21))
            ],
            symbol: innerFunction);

        var entryPoint = new LoweredRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            location: Location(1),
            resultType: TypeSymbol.Void,
            instructions:
            [
                LoweredInstruction.LoadLiteral(Location(2), 5, TypeSymbol.Integer),
                LoweredInstruction.CallFunction(Location(2), outerFunction),
                LoweredInstruction.PrintValue(Location(2)),
                LoweredInstruction.PrintLine(Location(2)),
                LoweredInstruction.Return(Location(3))
            ],
            isEntryPoint: true);

        var loweredProgram = new LoweredProgram(Location(1), entryPoint, [outerRoutine, innerRoutine]);

        var result = new BytecodeCompiler().Compile(loweredProgram);

        Assert.True(result.Succeeded);

        var compiledInner = result.Module!.Routines.Single(static routine => routine.Id == 2);
        Assert.Equal([outerParameter, outerLocal], compiledInner.CapturedVariables);
        Assert.Collection(
            compiledInner.Instructions,
            instruction =>
            {
                Assert.Equal(Opcode.LoadCapture, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction =>
            {
                Assert.Equal(Opcode.LoadArgument, instruction.Opcode);
                Assert.Equal(0, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.Multiply, instruction.Opcode),
            instruction =>
            {
                Assert.Equal(Opcode.LoadCapture, instruction.Opcode);
                Assert.Equal(1, instruction.GetOperand(0));
            },
            instruction => Assert.Equal(Opcode.Add, instruction.Opcode),
            instruction => Assert.Equal(Opcode.Return, instruction.Opcode));
    }

    private static TextLocation Location(int line)
    {
        return new TextLocation(new TextSpan((line - 1) * 10, 1), line, 1);
    }
}
