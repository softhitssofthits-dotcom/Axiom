using Axiom.Execution;
using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed partial class BytecodeModuleValidatorTests
{
    [Theory]
    [InlineData("DATE", "2024-02-29")]
    [InlineData("TIME", "12:34:56.125")]
    [InlineData("DATETIME", "2024-02-29 12:34:56.125")]
    [InlineData("UTCDATETIME", "2024-02-29T12:34:56.125Z")]
    public void Compile_ConcatAcceptsTemporalOperands(string type, string value)
    {
        var source = $"DIM value AS {type} = \"{value}\"\nPRINT \"value=\" & value\nPRINT value & \"!\"";

        var compilation = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));

        Assert.True(compilation.Succeeded, string.Join(" | ", compilation.Diagnostics.Concat(compilation.BackendDiagnostics)));
        Assert.Contains(compilation.CompiledProgram!.BytecodeModule!.Routines.SelectMany(routine => routine.Instructions),
            instruction => instruction.Opcode == Opcode.Concat);
    }

    [Theory]
    [InlineData("DIM value AS FILE", "value")]
    [InlineData("DIM value AS IMAGE", "value")]
    [InlineData("DIM value AS SPRITE", "value")]
    [InlineData("DIM value AS TILEMAP", "value")]
    [InlineData("DIM value AS TILESET", "value")]
    [InlineData("DIM value[1 TO 2] AS INTEGER", "value[]")]
    [InlineData("TYPE Item\n    Number AS INTEGER\nEND TYPE\nDIM value AS Item", "value")]
    public void Compile_InterpolationAcceptsDisplayableValues(string declaration, string expression)
    {
        var source = declaration + "\nPRINT $\"value={" + expression + "}!\"";

        var compilation = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));

        Assert.True(compilation.Succeeded, string.Join(" | ", compilation.Diagnostics.Concat(compilation.BackendDiagnostics)));
        Assert.Contains(compilation.CompiledProgram!.BytecodeModule!.Routines.SelectMany(routine => routine.Instructions),
            instruction => instruction.Opcode == Opcode.Concat);
    }

    [Fact]
    public void Compile_StringConcatenationStillRejectsFileOperands()
    {
        var compilation = new AxiomExecutionPipeline().Compile(
            new CompilationRequest("DIM value AS FILE\nPRINT \"value=\" & value"));

        Assert.False(compilation.FrontendSucceeded);
        Assert.NotEmpty(compilation.Diagnostics);
        Assert.Empty(compilation.BackendDiagnostics);
    }

    private static BytecodeValidationResult ValidateWithExactLocations(BytecodeModule module)
    {
        var instructionMap = module.Routines.SelectMany(routine =>
            routine.Instructions.Select((_, index) => new SourceInstructionMap(
                routine.Id, index, Location(routine.Location.Line + index)))).ToArray();
        var mappedModule = new BytecodeModule(
            module.Header, module.ConstantPool, module.Routines, module.Globals, module.Builtins,
            new BytecodeDebugInfo(instructionMap, module.DebugInfo!.Routines), module.EntryPointRoutineId);

        return new BytecodeModuleValidator().Validate(mappedModule);
    }

    [Theory]
    [InlineData(Opcode.Concat, 0)]
    [InlineData(Opcode.Concat, 1)]
    [InlineData(Opcode.FormatInterpolatedPart, 0)]
    [InlineData(Opcode.FormatInterpolatedPart, 1)]
    [InlineData(Opcode.FormatInterpolatedPart, 2)]
    [InlineData(Opcode.ReseedRandom, 0)]
    public void Validate_PreviouslyMissingStackEffect_RejectsInsufficientInputs(Opcode opcode, int available)
    {
        var instructions = Enumerable.Range(0, available)
            .Select(_ => new BytecodeInstruction(Opcode.PushConst, [0]))
            .Concat([
                new BytecodeInstruction(opcode, opcode == Opcode.ReseedRandom ? [1] : []),
                new BytecodeInstruction(Opcode.Return)
            ])
            .ToArray();

        var result = ValidateWithExactLocations(CreateValidModule(instructions));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(10 + available, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(Opcode.Smooth, 0)]
    [InlineData(Opcode.Smooth, 1)]
    [InlineData(Opcode.ReseedRandom, 0)]
    public void Validate_NoStackEffectCommand_VisitsFollowingInstruction(Opcode opcode, int flag)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(opcode, [flag]),
            new BytecodeInstruction(Opcode.Pop),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(11, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(Opcode.Smooth, 0)]
    [InlineData(Opcode.Smooth, 1)]
    [InlineData(Opcode.ReseedRandom, 0)]
    public void Validate_NoStackEffectCommand_ChecksReturnHeight(Opcode opcode, int flag)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(opcode, [flag]),
            new BytecodeInstruction(Opcode.PushConst, [0]),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4054", diagnostic.Code);
        Assert.Equal(12, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(Opcode.Smooth, 0)]
    [InlineData(Opcode.Smooth, 1)]
    [InlineData(Opcode.ReseedRandom, 0)]
    public void Validate_NoStackEffectCommand_ChecksFollowingStackTypes(Opcode opcode, int flag)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(opcode, [flag]),
            new BytecodeInstruction(Opcode.PushConst, [1]),
            new BytecodeInstruction(Opcode.StoreLocal, [0]),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(12, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(Opcode.Concat)]
    [InlineData(Opcode.FormatInterpolatedPart)]
    public void Validate_StringProducingCommand_PreservesResultType(Opcode opcode)
    {
        var inputs = opcode == Opcode.Concat ? new[] { 1, 1 } : new[] { 0, 0, 1 };
        var instructions = inputs.Select(index => new BytecodeInstruction(Opcode.PushConst, [index]))
            .Concat([
                new BytecodeInstruction(opcode),
                new BytecodeInstruction(Opcode.StoreLocal, [0]),
                new BytecodeInstruction(Opcode.Return)
            ])
            .ToArray();

        var result = ValidateWithExactLocations(CreateValidModule(instructions));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4116", diagnostic.Code);
        Assert.Equal(11 + inputs.Length, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(0, 0, "STRING")]
    [InlineData(1, 1, "INTEGER")]
    public void Validate_FormatInterpolatedPart_RejectsInvalidMetadataTypes(int alignment, int format, string expectedType)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.PushConst, [0]),
            new BytecodeInstruction(Opcode.PushConst, [alignment]),
            new BytecodeInstruction(Opcode.PushConst, [format]),
            new BytecodeInstruction(Opcode.FormatInterpolatedPart),
            new BytecodeInstruction(Opcode.Pop),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(13, diagnostic.Location.Line);
        Assert.Contains(expectedType, diagnostic.Message);
    }

    [Theory]
    [InlineData(Opcode.Concat)]
    [InlineData(Opcode.FormatInterpolatedPart)]
    public void Validate_StringProducingCommand_AcceptsStringDestination(Opcode opcode)
    {
        var inputs = opcode == Opcode.Concat ? new[] { 1, 1 } : new[] { 0, 0, 1 };
        var instructions = inputs.Select(index => new BytecodeInstruction(Opcode.PushConst, [index]))
            .Concat([
                new BytecodeInstruction(opcode),
                new BytecodeInstruction(Opcode.StoreLocal, [0]),
                new BytecodeInstruction(Opcode.Return)
            ])
            .ToArray();
        var routine = CreateEntryPointWithLocal(TypeSymbol.String, instructions);

        var result = ValidateWithExactLocations(CreateValidModule(
            routines: [routine], debugInfo: CreateDebugInfo(routine)));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ReseedRandomWithSeed_VisitsFollowingInstruction()
    {
        var result = ValidateWithExactLocations(CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.ReseedRandom, [1]),
                new BytecodeInstruction(Opcode.Pop),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants: [new BytecodeConstant(BytecodeConstantKind.Long, 42L)]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(12, diagnostic.Location.Line);
    }

    [Fact]
    public void Validate_ReseedRandomWithSeed_RejectsInvalidSeedType()
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.PushConst, [1]),
            new BytecodeInstruction(Opcode.ReseedRandom, [1]),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(11, diagnostic.Location.Line);
        Assert.Contains("LONG", diagnostic.Message);
    }

    [Fact]
    public void Validate_ReseedRandomWithSeed_AcceptsLongSeed()
    {
        var result = ValidateWithExactLocations(CreateValidModule(
            instructions:
            [
                new BytecodeInstruction(Opcode.PushConst, [0]),
                new BytecodeInstruction(Opcode.ReseedRandom, [1]),
                new BytecodeInstruction(Opcode.Return)
            ],
            constants: [new BytecodeConstant(BytecodeConstantKind.Long, 42L)]));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Validate_ReseedRandom_RequiresOneFlagOperand(int operandCount)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.ReseedRandom, new int[operandCount]),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4012", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void Validate_ReseedRandom_RejectsUnknownSeedFlag(int flag)
    {
        var result = ValidateWithExactLocations(CreateValidModule(instructions:
        [
            new BytecodeInstruction(Opcode.ReseedRandom, [flag]),
            new BytecodeInstruction(Opcode.Return)
        ]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4133", diagnostic.Code);
        Assert.Equal(10, diagnostic.Location.Line);
    }
}
