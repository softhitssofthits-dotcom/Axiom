using Axiom.Bytecode;
using Axiom.Semantics;
namespace Axiom.Execution.Tests;

public sealed class ModuleArtifactIdentityRegressionTests
{
    [Theory]
    [InlineData("FUNCTION(FUNCTION(Box, INTEGER) AS INTEGER) AS INTEGER")]
    [InlineData("FUNCTION() AS FUNCTION() AS Box")]
    [InlineData("FUNCTION() AS Box[,]")]
    [InlineData("FUNCTION(Box[,]) AS INTEGER")]
    [InlineData("FUNCTION(BYREF Box[,], FUNCTION(INTEGER, Box) AS Box) AS Box")]
    public void NestedSignatureSurvivesJsonRoundTrip(string signature)
    {
        var module = CompilePair(signature);
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(module));
        foreach (var name in new[] { "FirstUse", "SecondUse" })
        {
            var before = module.Routines.Single(r => r.Name == name).ParameterVariables[0].Type;
            var after = restored.Routines.Single(r => r.Name == name).ParameterVariables[0].Type;
            Assert.Equal(Shape(before), Shape(after));
        }
        Assert.Empty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
    }

    [Fact]
    public void DuplicateIdentityIsRejected()
    {
        var artifact = BytecodeModuleSerializer.ToArtifact(CompilePair("Box"));
        artifact = artifact with { TypeDefinitions = [.. artifact.TypeDefinitions, artifact.TypeDefinitions[0]] };
        Assert.Throws<InvalidOperationException>(() => BytecodeModuleSerializer.FromArtifact(artifact));
    }

    [Fact]
    public void AmbiguousShortTypeReferenceIsRejected()
    {
        var artifact = BytecodeModuleSerializer.ToArtifact(CompilePair("Box"));
        var routine = artifact.Routines.Single(r => r.Name == "FirstUse");
        var parameter = routine.ParameterVariables[0] with { Type = "Box" };
        artifact = artifact with { Routines = artifact.Routines.Select(r => r == routine ? r with { ParameterVariables = [parameter], Symbol = r.Symbol! with { Parameters = [parameter] } } : r).ToArray() };
        Assert.Throws<InvalidOperationException>(() => BytecodeModuleSerializer.FromArtifact(artifact));
    }

    [Fact]
    public void AmbiguousCreateRecordOperandIsRejected()
    {
        var compilation = new AxiomExecutionPipeline().Compile(new CompilationRequest("IMPORT first\nIMPORT second\nPRINT FirstUse()\nPRINT SecondUse()", sourceName: "main.axm", additionalDocuments: [
            new SourceDocument("first.axm", "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC FUNCTION FirstUse() AS INTEGER\nDIM item AS Box\nRETURN item.Item\nEND FUNCTION"),
            new SourceDocument("second.axm", "TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC FUNCTION SecondUse() AS INTEGER\nDIM item AS Box\nRETURN item.Item\nEND FUNCTION")]));
        Assert.Equal(CompilationStatus.Success, compilation.Status);
        var artifact = BytecodeModuleSerializer.ToArtifact(compilation.CompiledProgram!.BytecodeModule!);
        artifact = artifact with { ConstantPool = artifact.ConstantPool.Select(c => c.Kind == "String" && c.Value is string s && s == "first::Box" ? c with { Value = "Box" } : c).ToArray() };
        var restored = BytecodeModuleSerializer.FromArtifact(artifact);
        Assert.NotEmpty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
    }

    [Fact]
    public void ArrayOfFunctionsRetainsItsArrayBoundary()
    {
        const string source = "TYPE Box\nItem AS INTEGER\nEND TYPE\nDIM factories[0 TO 1] AS FUNCTION() AS Box";
        var c = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.True(c.Succeeded);
        var module = c.CompiledProgram!.BytecodeModule!;
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(module));
        var before = module.Routines.SelectMany(r => r.LocalVariables).Single(v => v.Name == "factories").Type;
        var after = restored.Routines.SelectMany(r => r.LocalVariables).Single(v => v.Name == "factories").Type;
        Assert.IsType<FunctionTypeSymbol>(Assert.IsType<ArrayTypeSymbol>(before).ElementType);
        Assert.Equal(Shape(before), Shape(after));
    }

    [Fact]
    public void RestoredPrivateRecordsExecuteWithTheirOwnDefaults()
    {
        const string source = "IMPORT first\nIMPORT second\nPRINT FirstUse()\nPRINT SecondUse()";
        var pipeline = new AxiomExecutionPipeline();
        var c = pipeline.Compile(new CompilationRequest(source, sourceName: "main.axm", additionalDocuments: [
            new SourceDocument("first.axm", "TYPE Box\nItem AS INTEGER=11\nEND TYPE\nPUBLIC FUNCTION FirstUse() AS INTEGER\nDIM item AS Box\nRETURN item.Item\nEND FUNCTION"),
            new SourceDocument("second.axm", "TYPE Box\nItem AS INTEGER=22\nEND TYPE\nPUBLIC FUNCTION SecondUse() AS INTEGER\nDIM item AS Box\nRETURN item.Item\nEND FUNCTION")]));
        Assert.True(c.Succeeded);
        var original = c.CompiledProgram!;
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(original.BytecodeModule!));
        var types = restored.Routines.SelectMany(r => r.LocalVariables).Select(v => v.Type).OfType<UserDefinedTypeSymbol>().ToArray();
        Assert.Contains(types, t => t.ContainingModule == "first" && t.TypeIdentity == "first::Box");
        Assert.Contains(types, t => t.ContainingModule == "second" && t.TypeIdentity == "second::Box");
        var result = pipeline.RunCompiled(new CompiledProgram(source, original.Syntax, original.BoundProgram, bytecodeModule: restored), new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "22"], result.Output);
    }

    [Fact]
    public void OriginalSchemaTwoFixtureStillValidatesAndExecutes()
    {
        using var stream = typeof(ModuleArtifactIdentityRegressionTests).Assembly.GetManifestResourceStream("Axiom.Execution.Tests.Fixtures.record-schema2.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();
        Assert.Contains("\"ArtifactSchemaVersion\": 2", json);
        Assert.DoesNotContain("\"Identity\"", json);
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(json);
        Assert.Empty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
        const string source = "TYPE Box\nItem AS INTEGER\nEND TYPE\nDIM value AS Box=Box(17)\nPRINT value.Item";
        var pipeline = new AxiomExecutionPipeline();
        var original = pipeline.Compile(new CompilationRequest(source)).CompiledProgram!;
        var result = pipeline.RunCompiled(new CompiledProgram(source, original.Syntax, original.BoundProgram, bytecodeModule: restored), new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["17"], result.Output);
    }

    private static BytecodeModule CompilePair(string signature)
    {
        var c = new AxiomExecutionPipeline().Compile(new CompilationRequest("IMPORT first\nIMPORT second\nPRINT 1", sourceName: "main.axm", additionalDocuments: [
            new SourceDocument("first.axm", $"TYPE Box\nItem AS INTEGER\nEND TYPE\nPUBLIC SUB FirstUse(arg AS {signature})\nEND SUB"),
            new SourceDocument("second.axm", $"TYPE Box\nItem AS STRING\nEND TYPE\nPUBLIC SUB SecondUse(arg AS {signature})\nEND SUB")]));
        Assert.True(c.Succeeded, string.Join(";", c.Diagnostics.Concat(c.BackendDiagnostics).Select(d => d.Message)));
        return c.CompiledProgram!.BytecodeModule!;
    }

    private static string Shape(TypeSymbol type) => type switch
    {
        UserDefinedTypeSymbol t => t.TypeIdentity,
        ArrayTypeSymbol t => $"ARRAY[{t.Rank}]({Shape(t.ElementType)})",
        FunctionTypeSymbol t => $"FUNCTION({string.Join(",", t.ParameterTypes.Select((p,i) => t.ParameterPassingModes[i]+":"+Shape(p)))}) AS {Shape(t.ReturnType)}",
        _ => type.Name
    };
}
