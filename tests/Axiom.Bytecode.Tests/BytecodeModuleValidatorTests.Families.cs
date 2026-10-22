using Axiom.Semantics;

namespace Axiom.Bytecode.Tests;

public sealed partial class BytecodeModuleValidatorTests
{
    public static TheoryData<Opcode, int[], TypeSymbol[], string[]> ModernGraphicsTypeContracts => new()
    {
        { Opcode.Canvas, [], [TypeSymbol.Integer, TypeSymbol.Long], ["ширина CANVAS", "высота CANVAS"] },
        { Opcode.Camera, [], [TypeSymbol.Long, TypeSymbol.Integer], ["координата X CAMERA", "координата Y CAMERA"] },
        { Opcode.Viewport, [0], [], [] },
        { Opcode.Viewport, [4], [TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long], ["левая граница VIEWPORT", "верхняя граница VIEWPORT", "правая граница VIEWPORT", "нижняя граница VIEWPORT"] },
        { Opcode.DrawRect, [], [TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["координата X", "координата Y", "ширина", "высота", "цвет"] },
        { Opcode.FillRect, [], [TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Integer], ["координата X", "координата Y", "ширина", "высота", "цвет"] },
        { Opcode.DrawLine, [], [TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["координата X1", "координата Y1", "координата X2", "координата Y2", "цвет"] },
        { Opcode.DrawCircle, [], [TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Integer], ["координата X", "координата Y", "радиус", "цвет"] },
        { Opcode.FillCircle, [], [TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["координата X", "координата Y", "радиус", "цвет"] },
        { Opcode.DrawText, [], [TypeSymbol.String, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["текст", "координата X", "координата Y", "цвет"] },
        { Opcode.DrawImage, [0], [TypeSymbol.Image, TypeSymbol.Integer, TypeSymbol.Long], ["изображение DRAWIMAGE", "координата X DRAWIMAGE", "координата Y DRAWIMAGE"] },
        { Opcode.DrawImage, [1], [TypeSymbol.Image, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["ширина DRAWIMAGE", "высота DRAWIMAGE", "изображение DRAWIMAGE", "координата X DRAWIMAGE", "координата Y DRAWIMAGE"] },
        { Opcode.DrawSprite, [0], [TypeSymbol.Sprite, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long], ["спрайт DRAWSPRITE", "номер кадра DRAWSPRITE", "координата X DRAWSPRITE", "координата Y DRAWSPRITE"] },
        { Opcode.DrawSprite, [1], [TypeSymbol.Sprite, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["ширина DRAWSPRITE", "высота DRAWSPRITE", "спрайт DRAWSPRITE", "номер кадра DRAWSPRITE", "координата X DRAWSPRITE", "координата Y DRAWSPRITE"] },
        { Opcode.DrawTile, [], [TypeSymbol.Tileset, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["тайлсет DRAWTILE", "номер тайла DRAWTILE", "координата X DRAWTILE", "координата Y DRAWTILE"] },
        { Opcode.DrawTilemap, [], [TypeSymbol.Tilemap, TypeSymbol.Long, TypeSymbol.Integer], ["тайловая карта DRAWTILEMAP", "координата X DRAWTILEMAP", "координата Y DRAWTILEMAP"] },
        { Opcode.DrawTileLayer, [], [TypeSymbol.Tilemap, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer], ["тайловая карта DRAWTILELAYER", "номер слоя DRAWTILELAYER", "координата X DRAWTILELAYER", "координата Y DRAWTILELAYER"] },
        { Opcode.SetMapTile, [], [TypeSymbol.Tilemap, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long, TypeSymbol.Integer, TypeSymbol.Long], ["тайловая карта SETMAPTILE", "номер слоя SETMAPTILE", "столбец SETMAPTILE", "строка SETMAPTILE", "номер тайлсета SETMAPTILE", "номер тайла SETMAPTILE", "флаги SETMAPTILE"] }
    };

    [Theory]
    [MemberData(nameof(ModernGraphicsTypeContracts))]
    public void Validate_ModernGraphicsFamily_AcceptsItsExactInputTypes(
        Opcode opcode, int[] operands, TypeSymbol[] inputs, string[] expectedRoles)
    {
        Assert.Equal(expectedRoles.Length, inputs.Length);
        var result = ValidateFamilyInstruction(inputs, new BytecodeInstruction(opcode, operands));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [MemberData(nameof(ModernGraphicsTypeContracts))]
    public void Validate_ModernGraphicsFamily_PreservesDiagnosticOrderAndLocation(
        Opcode opcode, int[] operands, TypeSymbol[] inputs, string[] expectedRoles)
    {
        var invalidInputs = inputs.Select(type => type == TypeSymbol.String ? TypeSymbol.Integer : TypeSymbol.String).ToArray();

        var result = ValidateFamilyInstruction(invalidInputs, new BytecodeInstruction(opcode, operands));

        Assert.Equal(expectedRoles.Length, result.Diagnostics.Count);
        for (var index = 0; index < expectedRoles.Length; index++)
        {
            var diagnostic = result.Diagnostics[index];
            Assert.Equal("BC4111", diagnostic.Code);
            Assert.Equal(Location(10 + inputs.Length), diagnostic.Location);
            Assert.Contains($"ожидает {expectedRoles[index]} типа", diagnostic.Message);
        }
    }

    [Theory]
    [MemberData(nameof(ModernGraphicsTypeContracts))]
    public void Validate_ModernGraphicsFamily_InsufficientInputsStopBeforeTypeValidation(
        Opcode opcode, int[] operands, TypeSymbol[] inputs, string[] expectedRoles)
    {
        Assert.Equal(expectedRoles.Length, inputs.Length);
        if (inputs.Length == 0)
        {
            return;
        }

        var availableInputs = inputs.Take(inputs.Length - 1).ToArray();
        var result = ValidateFamilyInstruction(availableInputs, new BytecodeInstruction(opcode, operands));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(Location(10 + availableInputs.Length), diagnostic.Location);
    }

    [Theory]
    [InlineData(0, 2, 0)]
    [InlineData(1, 2, 1)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 2)]
    [InlineData(0, 1, 1)]
    public void Validate_ArrayFamily_LoadElementPreservesElementAndViewTypes(int rangeMask, int dimensions, int resultRank)
    {
        var inputs = new List<TypeSymbol> { ArrayTypeSymbol.Create(TypeSymbol.Integer, 2) };
        for (var dimension = 0; dimension < dimensions; dimension++)
        {
            inputs.Add(TypeSymbol.Long);
            if ((rangeMask & (1 << dimension)) != 0)
            {
                inputs.Add(TypeSymbol.Double);
            }
        }
        var outputType = resultRank == 0 ? TypeSymbol.Integer : ArrayTypeSymbol.Create(TypeSymbol.Integer, resultRank);

        var result = ValidateFamilyInstruction(inputs.ToArray(), new BytecodeInstruction(Opcode.LoadElement, [1, dimensions, rangeMask]), outputType);

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Theory]
    [InlineData(Opcode.RedimArray)]
    [InlineData(Opcode.LoadElement)]
    [InlineData(Opcode.StoreElement)]
    public void Validate_ArrayFamily_PreservesInvalidArgumentDiagnosticOrder(Opcode opcode)
    {
        var instruction = opcode switch
        {
            Opcode.RedimArray => new BytecodeInstruction(opcode, [0, 1, 2]),
            Opcode.LoadElement => new BytecodeInstruction(opcode, [1, 2, 1]),
            _ => new BytecodeInstruction(opcode, [1, 2])
        };
        TypeSymbol[] inputs = opcode switch
        {
            Opcode.RedimArray => [TypeSymbol.Integer, TypeSymbol.String, TypeSymbol.String, TypeSymbol.String, TypeSymbol.String],
            Opcode.LoadElement => [TypeSymbol.Integer, TypeSymbol.String, TypeSymbol.String, TypeSymbol.String],
            _ => [TypeSymbol.String, ArrayTypeSymbol.Create(TypeSymbol.Integer), TypeSymbol.String, TypeSymbol.String]
        };
        string[] codes = opcode switch
        {
            Opcode.RedimArray => ["BC4112", "BC4113", "BC4113", "BC4113", "BC4113"],
            Opcode.LoadElement => ["BC4113", "BC4113", "BC4113", "BC4112"],
            _ => ["BC4113", "BC4113", "BC4112", "BC4116"]
        };
        string[] roles = opcode switch
        {
            Opcode.RedimArray => ["массив", "нижнюю границу измерения 1", "верхнюю границу измерения 1", "нижнюю границу измерения 2", "верхнюю границу измерения 2"],
            Opcode.LoadElement => ["нижнюю границу среза измерения 1", "верхнюю границу среза измерения 1", "индекс измерения 2", "массив"],
            _ => ["индекс измерения 1", "индекс измерения 2", "для записи элемента нужны все измерения", "значение элемента массива"]
        };

        var result = ValidateFamilyInstruction(inputs, instruction, opcode == Opcode.LoadElement ? TypeSymbol.Integer : null);

        Assert.Equal(codes, result.Diagnostics.Select(diagnostic => diagnostic.Code));
        for (var index = 0; index < codes.Length; index++)
        {
            Assert.Equal(Location(10 + inputs.Length), result.Diagnostics[index].Location);
            Assert.Contains(roles[index], result.Diagnostics[index].Message);
        }
    }

    [Theory]
    [InlineData(Opcode.EraseArray, 0)]
    [InlineData(Opcode.RedimArray, 4)]
    [InlineData(Opcode.LoadElement, 3)]
    [InlineData(Opcode.StoreElement, 3)]
    [InlineData(Opcode.LoadField, 0)]
    [InlineData(Opcode.StoreField, 1)]
    [InlineData(Opcode.ValidateTypeInvariant, 0)]
    public void Validate_ArrayAndRecordFamilies_RejectInsufficientInputs(Opcode opcode, int inputCount)
    {
        int[] operands = opcode switch
        {
            Opcode.EraseArray => [],
            Opcode.RedimArray => [0, 1, 2],
            Opcode.LoadElement => [1, 2, 1],
            Opcode.StoreElement => [1, 2],
            _ => [1]
        };

        var result = ValidateFamilyInstruction(Enumerable.Repeat(TypeSymbol.String, inputCount).ToArray(), new BytecodeInstruction(opcode, operands));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4053", diagnostic.Code);
        Assert.Equal(Location(10 + inputCount), diagnostic.Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RecordFamily_CreationAndFieldAccessPreserveKnownTypes(bool hasIdentityOperand)
    {
        var recordType = new UserDefinedTypeSymbol("Point");
        recordType.Complete([new TypeFieldSymbol("value", TypeSymbol.Integer)]);
        var creation = ValidateFamilyInstruction([], new BytecodeInstruction(Opcode.CreateRecord, hasIdentityOperand ? [1, 0] : [1]), recordType,
            [new BytecodeConstant(BytecodeConstantKind.Integer, 1), new BytecodeConstant(BytecodeConstantKind.String, "Point")]);
        var load = ValidateFamilyInstruction([recordType], new BytecodeInstruction(Opcode.LoadField, [1]), TypeSymbol.Integer);
        var store = ValidateFamilyInstruction([TypeSymbol.Integer, recordType], new BytecodeInstruction(Opcode.StoreField, [1]));

        Assert.Empty(creation.Diagnostics);
        Assert.Empty(load.Diagnostics);
        Assert.Empty(store.Diagnostics);
    }

    [Theory]
    [InlineData(Opcode.LoadField, "BC4114")]
    [InlineData(Opcode.StoreField, "BC4114")]
    [InlineData(Opcode.CreateRecord, "BC4117")]
    [InlineData(Opcode.ValidateTypeInvariant, "BC4111")]
    public void Validate_RecordFamily_PreservesInvalidInputCodeAndLocation(Opcode opcode, string expectedCode)
    {
        TypeSymbol[] inputs = opcode switch
        {
            Opcode.CreateRecord => [],
            Opcode.StoreField => [TypeSymbol.String, TypeSymbol.Integer],
            _ => [TypeSymbol.Integer]
        };

        var result = ValidateFamilyInstruction(inputs, new BytecodeInstruction(opcode, [1]),
            opcode is Opcode.CreateRecord or Opcode.LoadField ? TypeSymbol.Integer : null);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(Location(10 + inputs.Length), diagnostic.Location);
    }

    [Theory]
    [InlineData(Opcode.DrawImage, "BC4130")]
    [InlineData(Opcode.DrawSprite, "BC4131")]
    [InlineData(Opcode.RedimArray, "BC4051")]
    public void Validate_ExtractedFamilies_KeepStructuralDiagnosticsBeforeStackAnalysis(Opcode opcode, string expectedCode)
    {
        int[] operands = opcode == Opcode.RedimArray ? [2, 1, 1] : [2];

        var result = ValidateFamilyInstruction([], new BytecodeInstruction(opcode, operands));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(Location(10), diagnostic.Location);
    }

    [Fact]
    public void Validate_ModernGraphicsFamily_ViewportRejectsUnsupportedValueCount()
    {
        var result = ValidateFamilyInstruction([TypeSymbol.Integer, TypeSymbol.Long], new BytecodeInstruction(Opcode.Viewport, [2]));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4124", diagnostic.Code);
        Assert.Equal(Location(12), diagnostic.Location);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Validate_ArrayFamily_AcceptsBothRedimFormsAndCompleteElementStore(int preserve)
    {
        var arrayType = ArrayTypeSymbol.Create(TypeSymbol.Integer, 2);

        var redim = ValidateFamilyInstruction([arrayType, TypeSymbol.Long, TypeSymbol.Double, TypeSymbol.Integer, TypeSymbol.Double],
            new BytecodeInstruction(Opcode.RedimArray, [preserve, 1, 2]));
        var store = ValidateFamilyInstruction([TypeSymbol.Integer, arrayType, TypeSymbol.Long, TypeSymbol.Double],
            new BytecodeInstruction(Opcode.StoreElement, [1, 2]));
        var erase = ValidateFamilyInstruction([arrayType], new BytecodeInstruction(Opcode.EraseArray));

        Assert.Empty(redim.Diagnostics);
        Assert.Empty(store.Diagnostics);
        Assert.Empty(erase.Diagnostics);
    }

    [Fact]
    public void Validate_ArrayFamily_ExcessLoadDimensionsDoNotInventAResultType()
    {
        var result = ValidateFamilyInstruction([ArrayTypeSymbol.Create(TypeSymbol.Integer), TypeSymbol.Integer, TypeSymbol.Long],
            new BytecodeInstruction(Opcode.LoadElement, [1, 2, 0]), TypeSymbol.String);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4111", diagnostic.Code);
        Assert.Equal(Location(13), diagnostic.Location);
        Assert.Contains("ранга 1", diagnostic.Message);
    }

    [Theory]
    [InlineData(Opcode.LoadField)]
    [InlineData(Opcode.StoreField)]
    public void Validate_RecordFamily_MissingFieldStopsAssignmentValidation(Opcode opcode)
    {
        var recordType = new UserDefinedTypeSymbol("Point");
        recordType.Complete([new TypeFieldSymbol("other", TypeSymbol.Integer)]);
        TypeSymbol[] inputs = opcode == Opcode.LoadField ? [recordType] : [TypeSymbol.String, recordType];

        var result = ValidateFamilyInstruction(inputs, new BytecodeInstruction(opcode, [1]), opcode == Opcode.LoadField ? TypeSymbol.String : null);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BC4115", diagnostic.Code);
        Assert.Equal(Location(10 + inputs.Length), diagnostic.Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RecordFamily_AcceptsBooleanInvariants(bool hasIdentityOperand)
    {
        var result = ValidateFamilyInstruction([TypeSymbol.Boolean],
            new BytecodeInstruction(Opcode.ValidateTypeInvariant, hasIdentityOperand ? [1, 0] : [1]));

        Assert.Empty(result.Diagnostics);
    }

    private static BytecodeValidationResult ValidateFamilyInstruction(
        TypeSymbol[] inputTypes,
        BytecodeInstruction instruction,
        TypeSymbol? outputType = null,
        IReadOnlyList<BytecodeConstant>? constants = null)
    {
        var locals = inputTypes.Select((type, index) => new VariableSymbol($"input{index}", type, isReadOnly: false, isGlobal: false)).ToList();
        var instructions = Enumerable.Range(0, inputTypes.Length).Select(index => new BytecodeInstruction(Opcode.LoadLocal, [index])).ToList();
        instructions.Add(instruction);
        if (outputType is not null)
        {
            instructions.Add(new BytecodeInstruction(Opcode.StoreLocal, [locals.Count]));
            locals.Add(new VariableSymbol("output", outputType, isReadOnly: false, isGlobal: false));
        }
        instructions.Add(new BytecodeInstruction(Opcode.Return));
        var routine = new BytecodeRoutine(0, "Main", Location(10), TypeSymbol.Void, parameterCount: 0,
            localSlotCount: locals.Count, instructions, isEntryPoint: true, localVariables: locals);
        var module = CreateValidModule(routines: [routine], constants: constants, debugInfo: CreateDebugInfo(routine));

        return ValidateWithExactLocations(module);
    }
}
