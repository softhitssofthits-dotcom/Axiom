using Axiom.Bytecode;

namespace Axiom.Execution.Tests;

/// <summary>
/// Независимые закрытые TYPE с одинаковым именем в разных модулях должны
/// сохранять собственную идентичность на всех слоях: связывание, понижение,
/// выпуск байт-кода, проверка модуля, VM и снимок артефакта.
/// </summary>
public sealed class PrivateTypeIdentityTests
{
    private const string Main = "IMPORT first\nIMPORT second\nPRINT FirstRead(FirstMake())\nPRINT SecondRead(SecondMake())";

    private const string FirstIntegerBox =
        "TYPE Box\nItem AS INTEGER\nEND TYPE\n" +
        "PUBLIC FUNCTION FirstMake() AS Box\nRETURN Box(11)\nEND FUNCTION\n" +
        "PUBLIC FUNCTION FirstRead(x AS Box) AS INTEGER\nRETURN x.Item\nEND FUNCTION";

    private const string SecondStringBox =
        "TYPE Box\nItem AS STRING\nEND TYPE\n" +
        "PUBLIC FUNCTION SecondMake() AS Box\nRETURN Box(\"hello\")\nEND FUNCTION\n" +
        "PUBLIC FUNCTION SecondRead(x AS Box) AS STRING\nRETURN x.Item\nEND FUNCTION";

    [Fact]
    public void SameNamedPrivateTypesKeepTheirOwnFields()
    {
        var result = Run(Main, FirstIntegerBox, SecondStringBox);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "hello"], result.Output);
    }

    [Fact]
    public void SameNamedPrivateTypesWithFieldDefaultsKeepTheirOwnFields()
    {
        var result = Run(
            "IMPORT first\nIMPORT second\nPRINT FirstRead()\nPRINT SecondRead()",
            "TYPE Box\nItem AS INTEGER=11\nEND TYPE\nPUBLIC FUNCTION FirstRead() AS INTEGER\nDIM x AS Box\nRETURN x.Item\nEND FUNCTION",
            "TYPE Box\nItem AS STRING=\"hello\"\nEND TYPE\nPUBLIC FUNCTION SecondRead() AS STRING\nDIM x AS Box\nRETURN x.Item\nEND FUNCTION");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "hello"], result.Output);
    }

    [Fact]
    public void SameNamedPrivateTypesKeepTheirOwnConstructorsAndMethods()
    {
        var result = Run(
            "IMPORT first\nIMPORT second\nPRINT FirstMake().Read()\nPRINT SecondMake().Read()",
            "TYPE Box\nItem AS INTEGER\nSUB New(value AS INTEGER)\nSELF.Item=value\nEND SUB\nFUNCTION Read() AS INTEGER\nRETURN SELF.Item\nEND FUNCTION\nEND TYPE\nPUBLIC FUNCTION FirstMake() AS Box\nRETURN Box(11)\nEND FUNCTION",
            "TYPE Box\nItem AS STRING\nSUB New(value AS STRING)\nSELF.Item=value\nEND SUB\nFUNCTION Read() AS STRING\nRETURN SELF.Item\nEND FUNCTION\nEND TYPE\nPUBLIC FUNCTION SecondMake() AS Box\nRETURN Box(\"hello\")\nEND FUNCTION");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "hello"], result.Output);
    }

    [Fact]
    public void SameNamedPrivateTypesKeepTheirOwnArrays()
    {
        var result = Run(
            "IMPORT first\nIMPORT second\nPRINT FirstRead()\nPRINT SecondRead()",
            "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC FUNCTION FirstRead() AS INTEGER\nDIM boxes(0 TO 1) AS Box\nboxes(0).Item=11\nRETURN boxes(0).Item\nEND FUNCTION",
            "TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC FUNCTION SecondRead() AS STRING\nDIM boxes(0 TO 1) AS Box\nboxes(0).Item=\"hello\"\nRETURN boxes(0).Item\nEND FUNCTION");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "hello"], result.Output);
    }

    [Fact]
    public void SameNamedPrivateTypesKeepTheirOwnFunctionSignatures()
    {
        var result = Run(
            "IMPORT first\nIMPORT second\nPRINT FirstRead()\nPRINT SecondRead()",
            "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC FUNCTION FirstMake() AS Box\nRETURN Box(11)\nEND FUNCTION\nPUBLIC FUNCTION FirstRead() AS INTEGER\nDIM factory AS FUNCTION() AS Box=FirstMake\nRETURN factory().Item\nEND FUNCTION",
            "TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC FUNCTION SecondMake() AS Box\nRETURN Box(\"hello\")\nEND FUNCTION\nPUBLIC FUNCTION SecondRead() AS STRING\nDIM factory AS FUNCTION() AS Box=SecondMake\nRETURN factory().Item\nEND FUNCTION");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "hello"], result.Output);
    }

    [Fact]
    public void PassingOnePrivateTypeInsteadOfAnotherIsRejected()
    {
        var result = Run(
            "IMPORT first\nIMPORT second\nPRINT FirstRead(SecondMake())",
            "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC FUNCTION FirstRead(x AS Box) AS INTEGER\nRETURN x.Item\nEND FUNCTION",
            "TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC FUNCTION SecondMake() AS Box\nRETURN Box(\"hello\")\nEND FUNCTION");

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB2003");
    }

    [Fact]
    public void SerializationKeepsBothPrivateTypeIdentities()
    {
        var compilation = Compile(
            "IMPORT first\nIMPORT second\nPRINT 1",
            "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC SUB FirstRead(x AS Box)\nPRINT x.Item\nEND SUB",
            "TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC SUB SecondRead(x AS Box)\nPRINT x.Item\nEND SUB");
        Assert.Equal(CompilationStatus.Success, compilation.Status);

        var module = compilation.CompiledProgram!.BytecodeModule!;
        var before = ParameterRecordTypes(module);
        Assert.Equal(2, before.Count);
        Assert.NotSame(before[0], before[1]);

        var artifact = BytecodeModuleSerializer.ToArtifact(module);
        Assert.Equal(2, artifact.TypeDefinitions.Count);
        Assert.Equal(["INTEGER", "STRING"], artifact.TypeDefinitions.Select(definition => definition.Fields[0].Type));

        var restored = BytecodeModuleSerializer.FromArtifact(artifact);
        var after = ParameterRecordTypes(restored);
        Assert.Equal(2, after.Count);
        Assert.NotSame(after[0], after[1]);
        Assert.Equal(["INTEGER", "STRING"], after.Select(type => type.Fields[0].Type.Name));

        var validation = new BytecodeModuleValidator().Validate(restored);
        Assert.Empty(validation.Diagnostics);

        var reserializedJson = BytecodeModuleSerializer.SerializeToJson(restored);
        Assert.Equal(2, System.Text.Json.Nodes.JsonNode.Parse(reserializedJson)!["TypeDefinitions"]!.AsArray().Count);
    }

    [Fact]
    public void SerializationKeepsPrivateTypesInsideFunctionSignatures()
    {
        var compilation = Compile(
            "IMPORT first\nIMPORT second\nPRINT 1",
            "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC SUB FirstUse(factory AS FUNCTION() AS Box)\nPRINT factory().Item\nEND SUB",
            "TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC SUB SecondUse(factory AS FUNCTION() AS Box)\nPRINT factory().Item\nEND SUB");
        Assert.Equal(CompilationStatus.Success, compilation.Status);

        var artifact = BytecodeModuleSerializer.ToArtifact(compilation.CompiledProgram!.BytecodeModule!);
        Assert.Equal(2, artifact.TypeDefinitions.Count);

        var restored = BytecodeModuleSerializer.FromArtifact(artifact);
        var validation = new BytecodeModuleValidator().Validate(restored);
        Assert.Empty(validation.Diagnostics);

        var parameterTypes = restored.Routines
            .Where(routine => routine.ParameterVariables.Count == 1)
            .Select(routine => (Axiom.Semantics.FunctionTypeSymbol)routine.ParameterVariables[0].Type)
            .ToArray();
        Assert.Equal(2, parameterTypes.Length);
        Assert.NotSame(parameterTypes[0].ReturnType, parameterTypes[1].ReturnType);
        Assert.Equal(["INTEGER", "STRING"], parameterTypes.Select(type => ((Axiom.Semantics.UserDefinedTypeSymbol)type.ReturnType).Fields[0].Type.Name));
    }

    private static List<Axiom.Semantics.UserDefinedTypeSymbol> ParameterRecordTypes(BytecodeModule module)
    {
        return module.Routines
            .Where(routine => routine.ParameterVariables.Count == 1)
            .Select(routine => Assert.IsType<Axiom.Semantics.UserDefinedTypeSymbol>(routine.ParameterVariables[0].Type))
            .ToList();
    }

    private static CompilationResult Compile(string main, string first, string second)
    {
        return new AxiomExecutionPipeline().Compile(new CompilationRequest(main,
            sourceName: "main.axm",
            additionalDocuments: [new SourceDocument("first.axm", first), new SourceDocument("second.axm", second)]));
    }

    private static ExecutionResult Run(string main, string first, string second)
    {
        return new AxiomExecutionPipeline().Run(new ExecutionRequest(main,
            sourceName: "main.axm",
            additionalDocuments: [new SourceDocument("first.axm", first), new SourceDocument("second.axm", second)]));
    }
}
