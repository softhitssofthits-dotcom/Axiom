using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed class BytecodeModuleValidatorTests
{
    [Fact]
    public void Validate_WellFormedModule_Succeeds()
    {
        var module = CreateValidModule();

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Validate_SmoothWithBooleanOperand_Succeeds(int enabled)
    {
        var module = CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.Smooth, [enabled]),
            new BytecodeInstruction(Opcode.Return)
        ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Validate_SmoothWithInvalidMode_ReturnsDiagnostic(int enabled)
    {
        var module = CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.Smooth, [enabled]),
            new BytecodeInstruction(Opcode.Return)
        ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "BC4125");
    }

    [Fact]
    public void Validate_SmoothWithoutOperand_ReturnsDiagnostic()
    {
        var module = CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.Smooth),
            new BytecodeInstruction(Opcode.Return)
        ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Validate_AddLongIntegerWithLongAndInteger_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.AddLongInteger),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 2),
                new BytecodeConstant(BytecodeConstantKind.String, "value")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_AddLongIntegerWithInvalidStackTypes_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.AddLongInteger),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 10),
                new BytecodeConstant(BytecodeConstantKind.Integer, 2),
                new BytecodeConstant(BytecodeConstantKind.String, "value")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "BC3068");
    }

    [Fact]
    public void Validate_JumpIfForDoneLocalWithLocalIntegerSlots_Succeeds()
    {
        var routine = new BytecodeRoutine(
            id: 0,
            name: "Main",
            location: Location(1),
            resultType: TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 3,
            instructions:
            [
                new BytecodeInstruction(Opcode.JumpIfForDoneLocal, [0, 1, 2, 1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables:
            [
                new VariableSymbol("i", TypeSymbol.Integer, isReadOnly: false, isGlobal: false),
                new VariableSymbol("endValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: false),
                new VariableSymbol("stepValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: false)
            ]);

        var result = new BytecodeModuleValidator().Validate(CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine)));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_AdvanceForLoopLocalWithMismatchedLocalTypes_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            id: 0,
            name: "Main",
            location: Location(1),
            resultType: TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 2,
            instructions:
            [
                new BytecodeInstruction(Opcode.AdvanceForLoopLocal, [0, 1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables:
            [
                new VariableSymbol("i", TypeSymbol.Integer, isReadOnly: false, isGlobal: false),
                new VariableSymbol("stepValue", TypeSymbol.Long, isReadOnly: false, isGlobal: false)
            ]);

        var result = new BytecodeModuleValidator().Validate(CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine)));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code is "BC3024" or "BC3070");
    }

    [Fact]
    public void Validate_PushIntegerZeroWithoutOperands_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushIntegerZero),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_JumpIfEqualWithComparableOperands_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.JumpIfEqual, [4]),
                new BytecodeInstruction(Opcode.Return),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.String, "left"),
                new BytecodeConstant(BytecodeConstantKind.String, "right"),
                new BytecodeConstant(BytecodeConstantKind.Integer, 1)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_JumpIfEqualWithIncompatibleOperands_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.JumpIfEqual, [4]),
                new BytecodeInstruction(Opcode.Return),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                new BytecodeConstant(BytecodeConstantKind.Boolean, true),
                new BytecodeConstant(BytecodeConstantKind.String, "value")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "BC4116");
    }

    [Fact]
    public void Validate_JumpIfLessOrEqualWithComparableOperands_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.JumpIfLessOrEqual, [4]),
                new BytecodeInstruction(Opcode.Return),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                new BytecodeConstant(BytecodeConstantKind.Integer, 2),
                new BytecodeConstant(BytecodeConstantKind.String, "value")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_JumpIfLessOrEqualWithIncompatibleOperands_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.JumpIfLessOrEqual, [4]),
                new BytecodeInstruction(Opcode.Return),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                new BytecodeConstant(BytecodeConstantKind.Boolean, true),
                new BytecodeConstant(BytecodeConstantKind.String, "value")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "BC4116");
    }

    [Fact]
    public void Validate_HeaderWithUnsupportedFormatVersion_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(formatVersion: (BytecodeFormatVersion)999));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4068", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithUnsupportedCompatibilityToken_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(compatibilityToken: "axiom-bytecode-v999"));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4069", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithBlankCompatibilityToken_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(compatibilityToken: " "));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4069", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithBlankLanguageVersion_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(languageVersion: " "));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4100", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithUnknownModuleFlags_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(flags: BytecodeModuleFlags.HasDebugInfo | BytecodeModuleFlags.UsesBuiltinTable | (BytecodeModuleFlags)8));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4070", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithoutDebugFlagButWithDebugInfo_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(flags: BytecodeModuleFlags.UsesBuiltinTable));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4071", diagnostic.Code);
    }

    [Fact]
    public void Validate_HeaderWithoutBuiltinFlagButWithBuiltinTable_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            header: CreateHeader(flags: BytecodeModuleFlags.HasDebugInfo));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4072", diagnostic.Code);
    }

    [Fact]
    public void Validate_ModuleWithoutMarkedEntryPoint_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string>(), [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4073", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ModuleWithMultipleMarkedEntryPoints_ReturnsDiagnostic()
    {
        var secondEntryPoint = new BytecodeRoutine(
            1,
            "OtherMain",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true);
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            routines:
            [
                CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]),
                secondEntryPoint
            ],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4074", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_EntryPointRoutineNotMarked_ReturnsDiagnostic()
    {
        var selectedRoutine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var markedRoutine = new BytecodeRoutine(
            1,
            "OtherMain",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true);
        var module = CreateValidModule(
            routines: [selectedRoutine, markedRoutine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string>(), []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4075", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_EntryPointWithParameters_ReturnsDiagnostic()
    {
        var parameter = new ParameterSymbol("value", TypeSymbol.Integer, 0);
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 1,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true,
            parameterVariables: [parameter]);
        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string> { [0] = "value" }, new Dictionary<int, string>(), [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4076", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_EntryPointWithResult_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Integer,
            parameterCount: 0,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true);
        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(0, 1, Location(11))
                ],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string>(), [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4077", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ModuleWithNegativeEntryPointRoutineId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            entryPointRoutineId: -1);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4093", diagnostic.Code);
    }

    [Fact]
    public void Validate_RoutineWithNegativeId_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            -1,
            "Broken",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4094", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithNegativeParameterCount_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            1,
            "Broken",
            Location(20),
            TypeSymbol.Void,
            parameterCount: -1,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4095", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithNegativeLocalSlotCount_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            1,
            "Broken",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: -1,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4096", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_GlobalWithNegativeSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            globals:
            [
                new BytecodeGlobal(-1, "counter", TypeSymbol.Integer, isReadOnly: false, GlobalInitializationStrategy.DefaultValue)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4097", diagnostic.Code);
    }

    [Fact]
    public void Validate_GlobalWithUnknownInitializationStrategy_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            globals:
            [
                new BytecodeGlobal(0, "counter", TypeSymbol.Integer, isReadOnly: false, (GlobalInitializationStrategy)999)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4078", diagnostic.Code);
    }

    [Fact]
    public void Validate_GlobalWithVoidType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            globals:
            [
                new BytecodeGlobal(0, "broken", TypeSymbol.Void, isReadOnly: false, GlobalInitializationStrategy.DefaultValue)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4079", diagnostic.Code);
    }

    [Fact]
    public void Validate_GlobalWithInvalidFixedStringMetadata_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            globals:
            [
                new BytecodeGlobal(0, "counter", TypeSymbol.Integer, isReadOnly: false, GlobalInitializationStrategy.DefaultValue, fixedStringLength: 10)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4080", diagnostic.Code);
    }

    [Fact]
    public void Validate_InvalidConstantIndex_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [7]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4013", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeConstantIndex_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4081", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_PushConstWithMetadataConstant_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.RoutineName, "Main")
            ],
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4049", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ConstantPoolWithUnknownConstantKind_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            constants:
            [
                new BytecodeConstant((BytecodeConstantKind)999, 1)
            ],
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4102", diagnostic.Code);
    }

    [Fact]
    public void Validate_ConstantPoolWithMismatchedValueType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, "one")
            ],
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4103", diagnostic.Code);
    }

    [Fact]
    public void Validate_InputPromptWithNonStringConstant_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [1, 1, 1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4048", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_OpenFileDebugNameWithNonStringConstant_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.OpenFile, [0, 0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4048", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidBuiltinIndex_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.CallBuiltin, [5, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4020", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallWithNegativeRoutineId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Call, [-1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4104", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallWithNegativeArgumentCount_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Call, [0, -1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4105", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadFunctionWithNegativeRoutineId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadFunction, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4104", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadFunctionWithoutFunctionSymbol_ReturnsDiagnostic()
    {
        var targetRoutine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);
        var module = CreateValidModule(
            routines:
            [
                CreateEntryPoint(
                    [
                        new BytecodeInstruction(Opcode.LoadFunction, [1]),
                        new BytecodeInstruction(Opcode.Pop),
                        new BytecodeInstruction(Opcode.Return)
                    ]),
                targetRoutine
            ],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4106", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallBuiltinWithNegativeId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.CallBuiltin, [-1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4107", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallBuiltinWithNegativeArgumentCount_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.CallBuiltin, [0, -1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4108", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidLoadBuiltinFunctionIndex_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadBuiltinFunction, [5]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4024", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadBuiltinFunctionWithNegativeId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadBuiltinFunction, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4107", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadBuiltinFunctionWithoutFunctionSymbol_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadBuiltinFunction, [0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4109", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_BuiltinWithUnknownCanonicalId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, (BuiltinFunctionId)999, "BROKEN", parameterCount: 0, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4062", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithNegativeId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(-1, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4098", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithNegativeParameterCount_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: -1, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4099", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithNonBuiltinFunctionSymbol_ReturnsDiagnostic()
    {
        var function = new FunctionSymbol("USERLEN", [new ParameterSymbol("text", TypeSymbol.String, 0)], TypeSymbol.Integer);
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true, function)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4063", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithMismatchedFunctionId_ReturnsDiagnostic()
    {
        var function = new FunctionSymbol(
            "UCASE",
            [new ParameterSymbol("text", TypeSymbol.String, 0)],
            TypeSymbol.String,
            isBuiltIn: true,
            builtInId: BuiltinFunctionId.UCase);
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "UCASE", parameterCount: 1, returnsValue: true, function)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4064", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithMismatchedFunctionName_ReturnsDiagnostic()
    {
        var function = CreateLenFunction();
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "WRONGLEN", parameterCount: 1, returnsValue: true, function)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4065", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithMismatchedFunctionParameterCount_ReturnsDiagnostic()
    {
        var function = CreateLenFunction();
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 2, returnsValue: true, function)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4066", diagnostic.Code);
    }

    [Fact]
    public void Validate_BuiltinWithMismatchedReturnValueFlag_ReturnsDiagnostic()
    {
        var function = CreateLenFunction();
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: false, function)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4067", diagnostic.Code);
    }

    [Fact]
    public void Validate_InvalidPrintUsingSeparator_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PrintUsing, [1, 3]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4038", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidPrintSeparator_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PrintSeparator, [2]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4050", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativePrintSeparator_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PrintSeparator, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4050", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidRedimPreserveFlag_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.RedimArray, [2, 1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4051", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeRedimPreserveFlag_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.RedimArray, [-1, 1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4051", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidInputTargetKind_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [1, 0, 9, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4046", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InputWithNegativeLocalSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [1, 0, 1, -1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4084", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InputWithNegativeValueCount_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [-1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4087", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InputValueCountOverflow_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [int.MaxValue, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4052", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Theory]
    [MemberData(nameof(UnsupportedInputTargetTypes))]
    public void Validate_InputWithUnsupportedLocalTargetType_ReturnsDiagnostic(TypeSymbol targetType)
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [1, 0, 1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables: [new VariableSymbol("target", targetType, isReadOnly: false, isGlobal: false)]);
        var module = CreateValidModule(
            routines: [routine],
            debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4110", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InputWithUnsupportedGlobalTargetType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Input, [1, 0, 0, 0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            globals:
            [
                new BytecodeGlobal(0, "fileHandle", TypeSymbol.File, isReadOnly: false, GlobalInitializationStrategy.DefaultValue)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4110", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_OpenFileWithNonStringPathStackValue_ReturnsDiagnostic()
    {
        var routine = CreateEntryPointWithLocal(
            TypeSymbol.File,
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.OpenFile, [0, 1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(0, 1, "BC4111")]
    [InlineData(1, 0, "BC4023")]
    public void Validate_OpenExplicitEncodingRejectsWrongTypeOrFlag(int encodingConstant, int flag, string code)
    {
        var routine = CreateEntryPointWithLocal(TypeSymbol.File,
        [
            new BytecodeInstruction(Opcode.PushConst, [1]),
            new BytecodeInstruction(Opcode.PushConst, [encodingConstant]),
            new BytecodeInstruction(Opcode.LoadLocal, [0]),
            new BytecodeInstruction(Opcode.OpenFile, [0, 1, flag]),
            new BytecodeInstruction(Opcode.Pop),
            new BytecodeInstruction(Opcode.Return)
        ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));
        var result = new BytecodeModuleValidator().Validate(module);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_OpenLegacyAndExplicitEncodingHaveCorrectStackEffects(bool explicitEncoding)
    {
        var instructions = new List<BytecodeInstruction> { new(Opcode.PushConst, [1]) };
        if (explicitEncoding) instructions.Add(new(Opcode.PushConst, [1]));
        instructions.Add(new(Opcode.LoadLocal, [0]));
        instructions.Add(new(Opcode.OpenFile, explicitEncoding ? [0, 1, 1] : [0, 1]));
        instructions.Add(new(Opcode.Pop));
        instructions.Add(new(Opcode.Return));
        var routine = CreateEntryPointWithLocal(TypeSymbol.File, instructions);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));
        var result = new BytecodeModuleValidator().Validate(module);
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(explicitEncoding ? 3 : 2, routine.MaxEvaluationStackDepth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Validate_OpenExplicitEncodingRejectsMissingStackValues(int availableValues)
    {
        var instructions = Enumerable.Range(0, availableValues).Select(_ => new BytecodeInstruction(Opcode.PushConst, [1])).ToList();
        instructions.Add(new(Opcode.OpenFile, [0, 1, 1]));
        instructions.Add(new(Opcode.Pop));
        instructions.Add(new(Opcode.Return));
        var routine = CreateEntryPointWithLocal(TypeSymbol.File, instructions);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));
        var result = new BytecodeModuleValidator().Validate(module);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == "BC4053");
    }

    [Fact]
    public void Validate_RedimArrayWithNonArrayStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.RedimArray, [0, 1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4112", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadElementWithNonNumericIndexStackValue_ReturnsDiagnostic()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.LoadElement, [1, 1, 0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4113", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadFieldWithNonRecordStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.LoadField, [1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4114", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_PrintUsingWithNonStringFormatStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PrintUsing, [1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegateWithNonNumericStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Negate),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4113", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NotWithUnsupportedStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Not),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ValidateForStepWithNonNumericStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.ValidateForStep),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4113", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_AddWithIncompatibleStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Add),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_PowerWithNonDoubleStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Power),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ShiftLeftWithIncompatibleStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.ShiftLeft),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ConvertWithUnsupportedSourceStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Convert, [(int)BytecodeScalarTypeCode.Integer]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ComparisonResultFeedsConditionalJump_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.CompareEqual),
                new BytecodeInstruction(Opcode.JumpIfFalse, [5]),
                new BytecodeInstruction(Opcode.Jump, [5]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_TypeStructuralEquality_Succeeds(bool utcField)
    {
        var recordType = new UserDefinedTypeSymbol("Point");
        recordType.Complete(
        [
            new TypeFieldSymbol("X", TypeSymbol.Integer),
            new TypeFieldSymbol("Name", utcField ? TypeSymbol.UtcDateTime : TypeSymbol.String)
        ]);

        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 2,
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadLocal, [1]),
                new BytecodeInstruction(Opcode.CompareEqual),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables:
            [
                new VariableSymbol("left", recordType, isReadOnly: false, isGlobal: false),
                new VariableSymbol("right", recordType, isReadOnly: false, isGlobal: false)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_LoadFieldWithMissingKnownField_ReturnsDiagnostic()
    {
        var recordType = new UserDefinedTypeSymbol("Point");
        recordType.Complete([new TypeFieldSymbol("x", TypeSymbol.Integer)]);
        var routine = CreateEntryPointWithLocal(
            recordType,
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadField, [1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4115", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreFieldWithIncompatibleValue_ReturnsDiagnostic()
    {
        var recordType = new UserDefinedTypeSymbol("Point");
        recordType.Complete([new TypeFieldSymbol("value", TypeSymbol.Integer)]);
        var routine = CreateEntryPointWithLocal(
            recordType,
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.StoreField, [1]),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreElementWithIncompatibleValue_ReturnsDiagnostic()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.StoreElement, [1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreGlobalWithIncompatibleStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.StoreGlobal, [0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreLocalWithIncompatibleStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.StoreLocal, [0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreArgumentWithIncompatibleStackValue_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 1,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.StoreArgument, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            parameterVariables: [new ParameterSymbol("value", TypeSymbol.Integer, 0)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StoreCaptureWithIncompatibleStackValue_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.StoreCapture, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            capturedVariables: [new VariableSymbol("captured", TypeSymbol.Integer, isReadOnly: false, isGlobal: false)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallWithIncompatibleArgumentStackValue_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint(
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Call, [1, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 1,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            parameterVariables: [new ParameterSymbol("value", TypeSymbol.Integer, 0)]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallBuiltinWithIncompatibleArgumentStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true, CreateLenFunction())
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallBuiltinWithoutFunctionSymbolWithIncompatibleArgumentStackValue_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallBuiltinWithoutFunctionSymbolWithCompatibleArgument_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CallBuiltinWithoutFunctionSymbolResultFeedsConditionalJump_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.JumpIfFalse, [4]),
                new BytecodeInstruction(Opcode.Jump, [4]),
                new BytecodeInstruction(Opcode.Return)
            ],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Len, "LEN", parameterCount: 1, returnsValue: true)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ArrayBuiltinWithoutFunctionSymbolUsesGenericDescriptor_Succeeds()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.String),
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 2]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(
            routines: [routine],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.ArrayContains, "ARRAYCONTAINS", parameterCount: 2, returnsValue: true)
            ],
            debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ArrayPredicateBuiltinWithoutFunctionSymbolWithWrongCallbackReturn_ReturnsDiagnostic()
    {
        var predicate = new FunctionSymbol(
            "KeepNumber",
            [new ParameterSymbol("value", TypeSymbol.Integer, 0)],
            TypeSymbol.Integer);
        var entryPoint = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadFunction, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 2]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var predicateRoutine = new BytecodeRoutine(
            1,
            "KeepNumber",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 1,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadArgument, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            symbol: predicate,
            parameterVariables: predicate.Parameters);
        var module = CreateValidModule(
            routines: [entryPoint, predicateRoutine],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.ArrayFilter, "ARRAYFILTER", parameterCount: 2, returnsValue: true)
            ],
            debugInfo: CreateDebugInfo(entryPoint, predicateRoutine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ArrayMapWithoutFunctionSymbolInfersSelectorResultArray_Succeeds()
    {
        var selector = new FunctionSymbol(
            "TextOf",
            [new ParameterSymbol("value", TypeSymbol.Integer, 0)],
            TypeSymbol.String);
        var sourceArrayType = ArrayTypeSymbol.Create(TypeSymbol.Integer);
        var targetArrayType = ArrayTypeSymbol.Create(TypeSymbol.String);
        var entryPoint = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 2,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadFunction, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 2]),
                new BytecodeInstruction(Opcode.StoreLocal, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables:
            [
                new VariableSymbol("numbers", sourceArrayType, isReadOnly: false, isGlobal: false),
                new VariableSymbol("texts", targetArrayType, isReadOnly: false, isGlobal: false)
            ]);
        var selectorRoutine = new BytecodeRoutine(
            1,
            "TextOf",
            Location(20),
            TypeSymbol.String,
            parameterCount: 1,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            symbol: selector,
            parameterVariables: selector.Parameters);
        var module = CreateValidModule(
            routines: [entryPoint, selectorRoutine],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.ArrayMap, "ARRAYMAP", parameterCount: 2, returnsValue: true)
            ],
            debugInfo: CreateDebugInfo(entryPoint, selectorRoutine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CallBuiltinWithNumericCompatibleArgument_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.Str, "STR", parameterCount: 1, returnsValue: true, CreateStrFunction())
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ArrayCallbackBuiltinWithIncompatibleFunctionValue_ReturnsDiagnostic()
    {
        var selector = new FunctionSymbol(
            "SelectNumber",
            [new ParameterSymbol("value", TypeSymbol.Integer, 0)],
            TypeSymbol.Integer);
        var entryPoint = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.LoadFunction, [1]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 2]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var selectorRoutine = new BytecodeRoutine(
            1,
            "SelectNumber",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 1,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadArgument, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            symbol: selector,
            parameterVariables: selector.Parameters);
        var module = CreateValidModule(
            routines: [entryPoint, selectorRoutine],
            builtins:
            [
                new BuiltinBinding(0, BuiltinFunctionId.ArrayFilter, "ARRAYFILTER", parameterCount: 2, returnsValue: true, CreateArrayFilterIntegerFunction())
            ],
            debugInfo: CreateDebugInfo(entryPoint, selectorRoutine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ReturnWithIncompatibleStackValue_ReturnsDiagnostic()
    {
        var entryPoint = CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]);
        var routine = new BytecodeRoutine(
            1,
            "MakeNumber",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 0,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(21, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidConversionTypeCode_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Convert, [99]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4047", diagnostic.Code);
        Assert.Contains("неизвестный код типа 99", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_InvalidJumpTarget_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Jump, [9]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4017", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeJumpTarget_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Jump, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4082", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeGlobalSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadGlobal, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4083", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeLocalSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadLocal, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4084", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeArgumentSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadArgument, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4085", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_NegativeCaptureSlot_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadCapture, [-1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4086", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallWithMismatchedSignature_ReturnsDiagnostic()
    {
        var parameter = new ParameterSymbol("value", TypeSymbol.Integer, 0);
        var callee = new BytecodeRoutine(
            1,
            "AddOne",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 1,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadArgument, [0]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Add),
                new BytecodeInstruction(Opcode.Return)
            ],
            parameterVariables: [parameter]);

        var module = CreateValidModule(
            routines:
            [
                CreateEntryPoint(
                    [
                        new BytecodeInstruction(Opcode.Call, [1, 0]),
                        new BytecodeInstruction(Opcode.Pop),
                        new BytecodeInstruction(Opcode.Return)
                    ]),
                callee
            ],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string>(), []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string> { [0] = "value" }, new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4019", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_CallByRefWithByValStackArgumentsAndFunctionResult_Succeeds()
    {
        var byRefParameter = new ParameterSymbol("value", TypeSymbol.Integer, 0, ParameterPassingMode.ByRef);
        var byValParameter = new ParameterSymbol("delta", TypeSymbol.Integer, 1);
        var callee = new BytecodeRoutine(
            1,
            "Adjust",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 2,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadArgument, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            parameterVariables: [byRefParameter, byValParameter]);
        var entryPoint = CreateEntryPoint(
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.CallByRef, [1, 2, 1, 1, 0, 2, 0]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var module = CreateValidModule(
            routines: [entryPoint, callee],
            debugInfo: CreateDebugInfo(entryPoint, callee));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CallTargetWithMissingCapture_ReturnsDiagnostic()
    {
        var missingCapture = new VariableSymbol("outerValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var entryPoint = CreateEntryPoint(
            [
                new BytecodeInstruction(Opcode.Call, [1, 0]),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            capturedVariables: [missingCapture]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4117", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadFunctionTargetWithMissingCapture_ReturnsDiagnostic()
    {
        var missingCapture = new VariableSymbol("outerValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var function = new FunctionSymbol("Worker", [], TypeSymbol.Integer);
        var entryPoint = CreateEntryPoint(
            [
                new BytecodeInstruction(Opcode.LoadFunction, [1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 0,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            symbol: function,
            capturedVariables: [missingCapture]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4117", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LoadFunctionTargetWithVisibleLocalCapture_Succeeds()
    {
        var captured = new VariableSymbol("outerValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var function = new FunctionSymbol("Worker", [], TypeSymbol.Integer);
        var entryPoint = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions:
            [
                new BytecodeInstruction(Opcode.LoadFunction, [1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            isEntryPoint: true,
            localVariables: [captured]);
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Integer,
            parameterCount: 0,
            localSlotCount: 0,
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Return)
            ],
            symbol: function,
            capturedVariables: [captured]);
        var module = CreateValidModule(
            routines: [entryPoint, routine],
            debugInfo: CreateDebugInfo(entryPoint, routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_RoutineWithMismatchedParameterMetadata_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 1,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)]);

        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            routines: [CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]), routine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string> { [0] = "value" }, new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4056", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithMismatchedLocalMetadata_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true);

        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4057", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithInvalidParameterMetadata_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            1,
            "Worker",
            Location(20),
            TypeSymbol.Void,
            parameterCount: 1,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            parameterVariables: [new VariableSymbol("value", TypeSymbol.Integer, isReadOnly: false, isGlobal: false)]);

        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            routines: [CreateEntryPoint([new BytecodeInstruction(Opcode.Return)]), routine],
            debugInfo: new BytecodeDebugInfo(
                [
                    new SourceInstructionMap(0, 0, Location(10)),
                    new SourceInstructionMap(1, 0, Location(20))
                ],
                [
                    new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, []),
                    new BytecodeRoutineDebugInfo(1, new Dictionary<int, string> { [0] = "value" }, new Dictionary<int, string>(), [])
                ]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4058", diagnostic.Code);
        Assert.Equal(20, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithInvalidLocalMetadata_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true,
            localVariables: [new ParameterSymbol("temp", TypeSymbol.Integer, 0)]);

        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4059", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithInvalidCapturedMetadata_ReturnsDiagnostic()
    {
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 0,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true,
            capturedVariables: [new VariableSymbol("globalValue", TypeSymbol.Integer, isReadOnly: false, isGlobal: true)]);

        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string>(), [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4060", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineWithDuplicateVariableMetadata_ReturnsDiagnostic()
    {
        var variable = new VariableSymbol("temp", TypeSymbol.Integer, isReadOnly: false, isGlobal: false);
        var routine = new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 2,
            instructions: [new BytecodeInstruction(Opcode.Return)],
            isEntryPoint: true,
            localVariables: [variable, variable]);

        var module = CreateValidModule(
            routines: [routine],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp", [1] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4061", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_DebugMapWithNegativeRoutineId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(-1, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4088", diagnostic.Code);
    }

    [Fact]
    public void Validate_DebugMapWithNegativeInstructionIndex_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, -1, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4089", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_RoutineDebugInfoWithNegativeRoutineId_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(-1, new Dictionary<int, string>(), new Dictionary<int, string>(), [])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4090", diagnostic.Code);
    }

    [Fact]
    public void Validate_BlockDebugRangeWithNegativeStart_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [new BytecodeBlockDebugRange("body", -1, 1)])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4091", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_BlockDebugRangeWithEndBeforeStart_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions: [new BytecodeInstruction(Opcode.Return)],
            debugInfo: new BytecodeDebugInfo(
                [new SourceInstructionMap(0, 0, Location(10))],
                [new BytecodeRoutineDebugInfo(0, new Dictionary<int, string>(), new Dictionary<int, string> { [0] = "temp" }, [new BytecodeBlockDebugRange("body", 1, 0)])]));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4092", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StackUnderflow_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ReturnWithUnexpectedStackHeight_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4054", diagnostic.Code);
        Assert.Equal(11, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_StackHeightMismatchAtJoin_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.JumpIfFalse, [3]),
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4055", diagnostic.Code);
        Assert.Equal(13, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LineWithoutExplicitStartPoint_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Line, [0, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_LineWithoutExplicitStartPointWithColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Line, [0, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 4)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_LineWithExplicitStartPoint_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.Line, [4, 4]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Long, 30L),
                new BytecodeConstant(BytecodeConstantKind.Long, 40L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_LineWithExplicitStartPointAndColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.PushConst, [4]),
                new BytecodeInstruction(Opcode.Line, [4, 5]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Long, 30L),
                new BytecodeConstant(BytecodeConstantKind.Long, 40L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 4)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_LineWithoutExplicitStartPointWithMismatchedCoordinateType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Line, [0, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 20)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LineWithExplicitStartPointWithMismatchedStartCoordinateType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.Line, [4, 4]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 20),
                new BytecodeConstant(BytecodeConstantKind.Long, 30L),
                new BytecodeConstant(BytecodeConstantKind.Long, 40L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_LineWithoutExplicitStartPointInsufficientStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Line, [0, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_Screen_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.Screen),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                new BytecodeConstant(BytecodeConstantKind.Integer, 0),
                new BytecodeConstant(BytecodeConstantKind.Integer, 0),
                new BytecodeConstant(BytecodeConstantKind.Integer, 1)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ScreenWithInsufficientStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Screen),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                new BytecodeConstant(BytecodeConstantKind.Integer, 0),
                new BytecodeConstant(BytecodeConstantKind.Integer, 0)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
    }

    [Fact]
    public void Validate_PCopy_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PCopy),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 0),
                new BytecodeConstant(BytecodeConstantKind.Integer, 1)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_Locate_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Locate),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 5),
                new BytecodeConstant(BytecodeConstantKind.Integer, 10)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ColorWithForegroundAndBackground_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Color, [1, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 6),
                new BytecodeConstant(BytecodeConstantKind.Integer, 2)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ViewWithCoordinatesFillAndBorder_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.PushConst, [4]),
                new BytecodeInstruction(Opcode.PushConst, [5]),
                new BytecodeInstruction(Opcode.View, [14, 6]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 3),
                new BytecodeConstant(BytecodeConstantKind.Integer, 9)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_WindowWithCoordinates_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.Window, [2, 4]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Double, 100d),
                new BytecodeConstant(BytecodeConstantKind.Double, 200d),
                new BytecodeConstant(BytecodeConstantKind.Double, 110d),
                new BytecodeConstant(BytecodeConstantKind.Double, 210d)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PaletteWithIndexAndColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.Palette, [2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 4),
                new BytecodeConstant(BytecodeConstantKind.Integer, 12)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PaletteUsingWithArray_Succeeds()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.Palette, [1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PaletteUsingWithNonArray_ReturnsDiagnostic()
    {
        var routine = CreateEntryPointWithLocal(
            TypeSymbol.Integer,
            [
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.Palette, [1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var module = CreateValidModule(routines: [routine], debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4112", diagnostic.Code);
    }

    [Fact]
    public void Validate_PSetWithoutColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PSet, [0, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PSetWithColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PSet, [0, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 20L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 4)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PSetWithInsufficientStackValues_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PSet, [0, 2]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
    }

    [Fact]
    public void Validate_GetWithRectangleAndBuffer_Succeeds()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2),
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.Get, [0, 5]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var module = CreateValidModule(
            routines: [routine],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 11L),
                new BytecodeConstant(BytecodeConstantKind.Long, 11L)
            ],
            debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PutWithBuffer_Succeeds()
    {
        var routine = CreateEntryPointWithLocal(
            ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2),
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.Put, [0, 3]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        var module = CreateValidModule(
            routines: [routine],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L)
            ],
            debugInfo: CreateDebugInfo(routine));

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CircleWithoutOptionalArguments_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Circle, [0, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Double, 3d)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CircleWithColorAndAspect_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.PushConst, [4]),
                new BytecodeInstruction(Opcode.Circle, [24, 5]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Double, 3d),
                new BytecodeConstant(BytecodeConstantKind.Integer, 14),
                new BytecodeConstant(BytecodeConstantKind.Double, 1d)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_CircleWithMismatchedRadiusType_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Circle, [0, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 3)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
    }

    [Fact]
    public void Validate_PaintWithFillColor_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Paint, [2, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Integer, 9)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PaintWithStringFillAndBackground_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.PushConst, [3]),
                new BytecodeInstruction(Opcode.Paint, [10, 4]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.String, "*"),
                new BytecodeConstant(BytecodeConstantKind.String, "*")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_PaintWithNonNumericBorder_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.PushConst, [1]),
                new BytecodeInstruction(Opcode.PushConst, [2]),
                new BytecodeInstruction(Opcode.Paint, [4, 3]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.Long, 10L),
                new BytecodeConstant(BytecodeConstantKind.String, "border")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4113", diagnostic.Code);
    }

    [Fact]
    public void Validate_Draw_Succeeds()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Draw, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.String, "R10")
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_DrawWithNonStringCommand_ReturnsDiagnostic()
    {
        var module = CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.Draw, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants:
            [
                new BytecodeConstant(BytecodeConstantKind.Integer, 42)
            ]);

        var result = new BytecodeModuleValidator().Validate(module);

        Assert.False(result.Succeeded);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
    }

    private static BytecodeModule CreateValidModule(
        IReadOnlyList<BytecodeInstruction>? instructions = null,
        IReadOnlyList<BytecodeRoutine>? routines = null,
        IReadOnlyList<BytecodeConstant>? constants = null,
        IReadOnlyList<BuiltinBinding>? builtins = null,
        IReadOnlyList<BytecodeGlobal>? globals = null,
        BytecodeDebugInfo? debugInfo = null,
        BytecodeHeader? header = null,
        int entryPointRoutineId = 0)
    {
        var entryPoint = CreateEntryPoint(
            instructions
            ??
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.StoreLocal, [0]),
                new BytecodeInstruction(Opcode.LoadLocal, [0]),
                new BytecodeInstruction(Opcode.CallBuiltin, [0, 1]),
                new BytecodeInstruction(Opcode.Return)
            ]);

        return new BytecodeModule(
            header ?? CreateHeader(),
            new ConstantPool(
                constants
                ??
                [
                    new BytecodeConstant(BytecodeConstantKind.Integer, 1),
                    new BytecodeConstant(BytecodeConstantKind.String, "value")
                ]),
            routines ?? [entryPoint],
            globals
            ??
            [
                new BytecodeGlobal(0, "counter", TypeSymbol.Integer, isReadOnly: false, GlobalInitializationStrategy.DefaultValue)
            ],
            builtins
            ??
            [
                new BuiltinBinding(0, BuiltinFunctionId.Abs, "ABS", parameterCount: 1, returnsValue: false)
            ],
            debugInfo
            ??
            new BytecodeDebugInfo(
                CreateInstructionMap(entryPoint.Instructions.Count),
                [
                    new BytecodeRoutineDebugInfo(
                        0,
                        new Dictionary<int, string>(),
                        new Dictionary<int, string> { [0] = "temp" },
                        [new BytecodeBlockDebugRange("body", 0, entryPoint.Instructions.Count)])
                ]),
            entryPointRoutineId);
    }

    private static BytecodeHeader CreateHeader(
        BytecodeFormatVersion formatVersion = BytecodeFormatVersion.V1,
        BytecodeModuleFlags flags = BytecodeModuleFlags.HasDebugInfo | BytecodeModuleFlags.UsesBuiltinTable,
        string languageVersion = "Axiom language vNext",
        string compatibilityToken = "axiom-bytecode-v1")
    {
        return new BytecodeHeader(
            formatVersion,
            languageVersion,
            flags,
            compatibilityToken);
    }

    private static FunctionSymbol CreateLenFunction()
    {
        return new FunctionSymbol(
            "LEN",
            [new ParameterSymbol("text", TypeSymbol.String, 0)],
            TypeSymbol.Integer,
            isBuiltIn: true,
            builtInId: BuiltinFunctionId.Len);
    }

    private static FunctionSymbol CreateStrFunction()
    {
        return new FunctionSymbol(
            "STR",
            [new ParameterSymbol("value", TypeSymbol.Double, 0)],
            TypeSymbol.String,
            isBuiltIn: true,
            builtInId: BuiltinFunctionId.Str);
    }

    private static FunctionSymbol CreateArrayFilterIntegerFunction()
    {
        return new FunctionSymbol(
            "ARRAYFILTER",
            [
                new ParameterSymbol("array", ArrayTypeSymbol.Create(TypeSymbol.Integer), 0),
                new ParameterSymbol("predicate", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Boolean), 1)
            ],
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            isBuiltIn: true,
            builtInId: BuiltinFunctionId.ArrayFilter);
    }

    public static IEnumerable<object[]> UnsupportedInputTargetTypes()
    {
        yield return [ArrayTypeSymbol.Create(TypeSymbol.Integer)];
        yield return [TypeSymbol.File];
        yield return [new UserDefinedTypeSymbol("Point")];
        yield return [FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Integer)];
    }

    private static BytecodeRoutine CreateEntryPoint(IReadOnlyList<BytecodeInstruction> instructions)
    {
        return new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions,
            isEntryPoint: true,
            localVariables: [new VariableSymbol("temp", TypeSymbol.Integer, isReadOnly: false, isGlobal: false)]);
    }

    private static BytecodeRoutine CreateEntryPointWithLocal(TypeSymbol localType, IReadOnlyList<BytecodeInstruction> instructions)
    {
        return new BytecodeRoutine(
            0,
            "Main",
            Location(10),
            TypeSymbol.Void,
            parameterCount: 0,
            localSlotCount: 1,
            instructions,
            isEntryPoint: true,
            localVariables: [new VariableSymbol("target", localType, isReadOnly: false, isGlobal: false)]);
    }

    private static BytecodeDebugInfo CreateDebugInfo(params BytecodeRoutine[] routines)
    {
        var instructionMaps = new List<SourceInstructionMap>();
        var routineInfos = new List<BytecodeRoutineDebugInfo>();

        foreach (var routine in routines)
        {
            var parameterNames = new Dictionary<int, string>();
            for (var index = 0; index < routine.ParameterVariables.Count; index++)
            {
                parameterNames[index] = routine.ParameterVariables[index].Name;
            }

            var localNames = new Dictionary<int, string>();
            for (var index = 0; index < routine.LocalVariables.Count; index++)
            {
                localNames[index] = routine.LocalVariables[index].Name;
            }

            instructionMaps.AddRange(CreateInstructionMap(routine.Id, routine.Instructions.Count, routine.Location.Line));
            routineInfos.Add(
                new BytecodeRoutineDebugInfo(
                    routine.Id,
                    parameterNames,
                    localNames,
                    [new BytecodeBlockDebugRange("body", 0, routine.Instructions.Count)]));
        }

        return new BytecodeDebugInfo(instructionMaps, routineInfos);
    }

    private static IReadOnlyList<SourceInstructionMap> CreateInstructionMap(int instructionCount)
    {
        return CreateInstructionMap(0, instructionCount);
    }

    private static IReadOnlyList<SourceInstructionMap> CreateInstructionMap(int routineId, int instructionCount)
    {
        return CreateInstructionMap(routineId, instructionCount, 10);
    }

    private static IReadOnlyList<SourceInstructionMap> CreateInstructionMap(int routineId, int instructionCount, int startLine)
    {
        if (instructionCount <= 0)
        {
            return [];
        }

        if (instructionCount == 1)
        {
            return [new SourceInstructionMap(routineId, 0, Location(startLine))];
        }

        return
        [
            new SourceInstructionMap(routineId, 0, Location(startLine)),
            new SourceInstructionMap(routineId, instructionCount - 1, Location(startLine + instructionCount - 1))
        ];
    }

    private static TextLocation Location(int line)
    {
        return new TextLocation(new TextSpan((line - 1) * 10, 1), line, 1);
    }
}
