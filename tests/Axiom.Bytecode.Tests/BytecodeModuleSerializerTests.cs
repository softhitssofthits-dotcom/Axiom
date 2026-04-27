using System.Text.Json;
using System.Text.Json.Nodes;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed class BytecodeModuleSerializerTests
{
    [Fact]
    public void ToArtifact_PreservesExecutableModuleShape()
    {
        var module = CreateModule();

        var artifact = BytecodeModuleSerializer.ToArtifact(module);

        Assert.Equal(BytecodeModuleSerializer.CurrentArtifactSchemaVersion, artifact.ArtifactSchemaVersion);
        Assert.Equal("V1", artifact.Header.FormatVersion);
        Assert.Equal("Axiom", artifact.Header.LanguageVersion);
        Assert.Equal((int)(BytecodeModuleFlags.HasDebugInfo | BytecodeModuleFlags.UsesBuiltinTable), artifact.Header.Flags);
        Assert.Equal("axiom-bytecode-v1", artifact.Header.CompatibilityToken);
        Assert.Equal(0, artifact.EntryPointRoutineId);

        var decimalConstant = Assert.Single(artifact.ConstantPool, static entry => entry.Kind == nameof(BytecodeConstantKind.Decimal));
        Assert.Equal(2, decimalConstant.Index);
        Assert.Equal("10.5", decimalConstant.Value);

        var routine = Assert.Single(artifact.Routines);
        Assert.Equal("Main", routine.Name);
        Assert.Equal("VOID", routine.ResultType);
        Assert.True(routine.IsEntryPoint);
        var routineSymbol = routine.Symbol;
        Assert.NotNull(routineSymbol);
        Assert.NotNull(routineSymbol!.SymbolId);
        Assert.Equal("Main", routineSymbol.Name);
        Assert.Empty(routine.ParameterVariables);
        Assert.Equal(["temp"], routine.LocalVariables.Select(static variable => variable.Name));
        Assert.Equal(["captured"], routine.CapturedVariables.Select(static variable => variable.Name));
        Assert.NotEqual(routine.LocalVariables[0].SymbolId, routine.CapturedVariables[0].SymbolId);
        Assert.Collection(
            routine.Instructions,
            instruction =>
            {
                Assert.Equal(0, instruction.Index);
                Assert.Equal(nameof(Opcode.PushConst), instruction.Opcode);
                Assert.Equal([0], instruction.Operands);
            },
            instruction =>
            {
                Assert.Equal(1, instruction.Index);
                Assert.Equal(nameof(Opcode.Pop), instruction.Opcode);
                Assert.Empty(instruction.Operands);
            },
            instruction =>
            {
                Assert.Equal(2, instruction.Index);
                Assert.Equal(nameof(Opcode.Return), instruction.Opcode);
                Assert.Empty(instruction.Operands);
            });

        var global = Assert.Single(artifact.Globals);
        Assert.Equal("answer", global.Name);
        Assert.Equal("INTEGER", global.Type);

        var builtin = Assert.Single(artifact.Builtins);
        Assert.Equal(nameof(BuiltinFunctionId.Len), builtin.CanonicalId);
        Assert.Equal("LEN", builtin.Function!.Name);
        Assert.NotNull(builtin.Function.SymbolId);
        Assert.NotNull(builtin.Function.Parameters[0].SymbolId);

        Assert.NotNull(artifact.DebugInfo);
        Assert.Single(artifact.DebugInfo!.InstructionMap);
        Assert.Single(artifact.DebugInfo.Routines);
    }

    [Fact]
    public void SerializeToJson_WritesStableInternalArtifact()
    {
        var module = CreateModule();

        var json = BytecodeModuleSerializer.SerializeToJson(module);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(
            BytecodeModuleSerializer.CurrentArtifactSchemaVersion,
            root.GetProperty("ArtifactSchemaVersion").GetInt32());
        Assert.Equal("V1", root.GetProperty("Header").GetProperty("FormatVersion").GetString());
        Assert.Equal(0, root.GetProperty("EntryPointRoutineId").GetInt32());
        Assert.Equal("Integer", root.GetProperty("ConstantPool")[0].GetProperty("Kind").GetString());
        Assert.Equal("PushConst", root.GetProperty("Routines")[0].GetProperty("Instructions")[0].GetProperty("Opcode").GetString());
        Assert.Equal("LEN", root.GetProperty("Builtins")[0].GetProperty("Name").GetString());
        Assert.True(root.GetProperty("Routines")[0].GetProperty("Symbol").TryGetProperty("SymbolId", out _));
        Assert.True(root.GetProperty("Routines")[0].GetProperty("LocalVariables")[0].TryGetProperty("SymbolId", out _));
        Assert.Equal(1, root.GetProperty("DebugInfo").GetProperty("InstructionMap")[0].GetProperty("Location").GetProperty("Line").GetInt32());
    }

    [Fact]
    public void DeserializeModuleFromJson_AllowsLegacyJsonWithoutArtifactSchemaVersion()
    {
        var json = BytecodeModuleSerializer.SerializeToJson(CreateModule());
        var root = JsonNode.Parse(json)!.AsObject();
        root.Remove("ArtifactSchemaVersion");

        var restoredModule = BytecodeModuleSerializer.DeserializeModuleFromJson(root.ToJsonString());

        Assert.Equal(0, restoredModule.EntryPointRoutineId);
    }

    [Fact]
    public void DeserializeModuleFromJson_RejectsUnsupportedArtifactSchemaVersion()
    {
        var json = BytecodeModuleSerializer.SerializeToJson(CreateModule());
        var root = JsonNode.Parse(json)!.AsObject();
        root["ArtifactSchemaVersion"] = BytecodeModuleSerializer.CurrentArtifactSchemaVersion + 1;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BytecodeModuleSerializer.DeserializeModuleFromJson(root.ToJsonString()));

        Assert.Contains("неподдерживаемую версию схемы", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeserializeModuleFromJson_RestoresValidatedModule()
    {
        var sourceModule = CreateModule();
        var json = BytecodeModuleSerializer.SerializeToJson(sourceModule);

        var restoredModule = BytecodeModuleSerializer.DeserializeModuleFromJson(json);

        Assert.Equal(sourceModule.EntryPointRoutineId, restoredModule.EntryPointRoutineId);
        Assert.Equal(sourceModule.Header.CompatibilityToken, restoredModule.Header.CompatibilityToken);
        Assert.Equal(sourceModule.ConstantPool.Count, restoredModule.ConstantPool.Count);
        Assert.Equal(10.5m, restoredModule.ConstantPool[2].Value);

        var routine = Assert.Single(restoredModule.Routines);
        Assert.Equal("Main", routine.Name);
        Assert.Equal(TypeSymbol.Void, routine.ResultType);
        Assert.NotNull(routine.Symbol);
        Assert.Empty(routine.ParameterVariables);
        Assert.Equal("temp", Assert.Single(routine.LocalVariables).Name);
        Assert.Equal("captured", Assert.Single(routine.CapturedVariables).Name);

        var builtin = Assert.Single(restoredModule.Builtins);
        Assert.Equal(BuiltinFunctionId.Len, builtin.CanonicalId);
        Assert.NotNull(builtin.Function);
        Assert.True(builtin.Function.IsBuiltIn);

        var validationResult = new BytecodeModuleValidator().Validate(restoredModule);
        Assert.True(
            validationResult.Succeeded,
            string.Join(" | ", validationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
    }

    [Fact]
    public void DeserializeModuleFromJson_PreservesDistinctVariablesWithSameShape()
    {
        var firstLocal = new VariableSymbol("temp", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var secondLocal = new VariableSymbol("temp", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var firstRoutine = CreateReturnOnlyRoutine(0, "First", firstLocal, isEntryPoint: true);
        var secondRoutine = CreateReturnOnlyRoutine(1, "Second", secondLocal, isEntryPoint: false);
        var module = new BytecodeModule(
            new BytecodeHeader(
                BytecodeFormatVersion.V1,
                "Axiom",
                BytecodeModuleFlags.None,
                "axiom-bytecode-v1"),
            new ConstantPool([]),
            [firstRoutine, secondRoutine],
            [],
            [],
            debugInfo: null,
            entryPointRoutineId: 0);

        var artifact = BytecodeModuleSerializer.ToArtifact(module);
        Assert.NotEqual(
            artifact.Routines[0].LocalVariables[0].SymbolId,
            artifact.Routines[1].LocalVariables[0].SymbolId);

        var restoredModule = BytecodeModuleSerializer.DeserializeModuleFromJson(
            BytecodeModuleSerializer.SerializeToJson(module));

        var firstRestoredLocal = Assert.Single(restoredModule.Routines[0].LocalVariables);
        var secondRestoredLocal = Assert.Single(restoredModule.Routines[1].LocalVariables);
        Assert.Equal("temp", firstRestoredLocal.Name);
        Assert.Equal("temp", secondRestoredLocal.Name);
        Assert.NotSame(firstRestoredLocal, secondRestoredLocal);

        var validationResult = new BytecodeModuleValidator().Validate(restoredModule);
        Assert.True(
            validationResult.Succeeded,
            string.Join(" | ", validationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
    }

    [Fact]
    public void DeserializeModuleFromJson_RestoresUserDefinedTypeFields()
    {
        var pointType = new UserDefinedTypeSymbol("Point");
        pointType.Complete(
        [
            new TypeFieldSymbol("x", TypeSymbol.Integer),
            new TypeFieldSymbol("name", TypeSymbol.String, fixedStringLength: 12)
        ]);
        var point = new VariableSymbol("point", pointType, isReadOnly: false, isGlobal: false);
        var routine = new BytecodeRoutine(
            id: 0,
            name: "Main",
            location: Location(1, 1),
            resultType: TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadField, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            symbol: new FunctionSymbol("Main", [], TypeSymbol.Void),
            parameterVariables: [],
            localVariables: [point],
            capturedVariables: []);
        var module = new BytecodeModule(
            new BytecodeHeader(
                BytecodeFormatVersion.V1,
                "Axiom",
                BytecodeModuleFlags.None,
                "axiom-bytecode-v1"),
            new ConstantPool([new BytecodeConstant(BytecodeConstantKind.String, "x")]),
            [routine],
            [],
            [],
            debugInfo: null,
            entryPointRoutineId: 0);

        var artifact = BytecodeModuleSerializer.ToArtifact(module);
        var typeDefinition = Assert.Single(artifact.TypeDefinitions);
        Assert.Equal("Point", typeDefinition.Name);
        Assert.Collection(
            typeDefinition.Fields,
            field =>
            {
                Assert.Equal("x", field.Name);
                Assert.Equal("INTEGER", field.Type);
            },
            field =>
            {
                Assert.Equal("name", field.Name);
                Assert.Equal("STRING", field.Type);
                Assert.Equal(12, field.FixedStringLength);
            });

        var restoredModule = BytecodeModuleSerializer.DeserializeModuleFromJson(
            BytecodeModuleSerializer.SerializeToJson(module));
        var restoredLocal = Assert.Single(Assert.Single(restoredModule.Routines).LocalVariables);
        var restoredType = Assert.IsType<UserDefinedTypeSymbol>(restoredLocal.Type);
        Assert.True(restoredType.IsCompleted);
        Assert.True(restoredType.TryGetField("x", out var restoredField));
        Assert.Equal(TypeSymbol.Integer, restoredField!.Type);
        Assert.True(restoredType.TryGetField("name", out var restoredFixedStringField));
        Assert.Equal(12, restoredFixedStringField!.FixedStringLength);

        var validationResult = new BytecodeModuleValidator().Validate(restoredModule);
        Assert.True(
            validationResult.Succeeded,
            string.Join(" | ", validationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
    }

    private static BytecodeModule CreateModule()
    {
        var local = new VariableSymbol("temp", TypeSymbol.String, isReadOnly: false, isGlobal: false, fixedStringLength: 8);
        var captured = new VariableSymbol("captured", TypeSymbol.Boolean, isReadOnly: false, isGlobal: false);
        var routineSymbol = new FunctionSymbol("Main", [], TypeSymbol.Void);
        var lenParameter = new ParameterSymbol("text", TypeSymbol.String, ordinal: 0);
        var lenFunction = new FunctionSymbol("LEN", [lenParameter], TypeSymbol.Integer, isBuiltIn: true, builtInId: BuiltinFunctionId.Len);

        var routine = new BytecodeRoutine(
            id: 0,
            name: "Main",
            location: Location(1, 1),
            resultType: TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            symbol: routineSymbol,
            parameterVariables: [],
            localVariables: [local],
            capturedVariables: [captured]);

        return new BytecodeModule(
            new BytecodeHeader(
                BytecodeFormatVersion.V1,
                "Axiom",
                BytecodeModuleFlags.HasDebugInfo | BytecodeModuleFlags.UsesBuiltinTable,
                "axiom-bytecode-v1"),
            new ConstantPool(
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 42),
                new BytecodeConstant(BytecodeConstantKind.String, "hello"),
                new BytecodeConstant(BytecodeConstantKind.Decimal, 10.5m)
            ]),
            [routine],
            [new BytecodeGlobal(0, "answer", TypeSymbol.Integer, isReadOnly: false, GlobalInitializationStrategy.Deferred)],
            [new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", 1, returnsValue: true, lenFunction)],
            new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(1, 7))],
                [
                    new BytecodeRoutineDebugInfo(
                        0,
                        new Dictionary<int, string>(),
                        new Dictionary<int, string> { [0] = "temp" },
                        [new BytecodeBlockDebugRange("Main", 0, 3)])
                ]),
            entryPointRoutineId: 0);
    }

    private static BytecodeRoutine CreateReturnOnlyRoutine(
        int id,
        string name,
        VariableSymbol local,
        bool isEntryPoint)
    {
        return new BytecodeRoutine(
            id,
            name,
            Location(id + 1, 1),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: isEntryPoint,
            symbol: new FunctionSymbol(name, [], TypeSymbol.Void),
            parameterVariables: [],
            localVariables: [local],
            capturedVariables: []);
    }

    private static TextLocation Location(int line, int column = 1)
    {
        return new TextLocation(new TextSpan(line * 10, 3), line, column);
    }
}
