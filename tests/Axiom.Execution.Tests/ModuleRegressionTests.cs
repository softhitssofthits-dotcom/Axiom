using Axiom.Bytecode;

namespace Axiom.Execution.Tests;

public sealed class ModuleRegressionTests
{
    [Fact]
    public void CompileAndRunUseTheSameDocuments()
    {
        const string source = "IMPORT math\nPRINT Square(5)";
        SourceDocument[] documents = [new("math.axm", "PUBLIC FUNCTION Square(x AS INTEGER) AS INTEGER\nRETURN x*x\nEND FUNCTION")];
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source, sourceName: "main.axm", additionalDocuments: documents));
        Assert.Equal(CompilationStatus.Success, compilation.Status);
        Assert.Equal(2, compilation.CompiledProgram!.SourceDocuments.Count);
        Assert.Equal("math", Assert.Single(compilation.CompiledProgram.BoundProgram.Routines).Symbol.ContainingModule);
        var result = pipeline.Run(new ExecutionRequest(source, sourceName: "main.axm", additionalDocuments: documents));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["25"], result.Output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleModuleVisibilityDeclarationsWork(bool extraDocument)
    {
        const string source = "PUBLIC DIM x AS INTEGER=7\nPRIVATE CONST y AS INTEGER=2\nPRINT x+y";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source,
            additionalDocuments: extraDocument ? [new SourceDocument("unused.axm", "PRINT 0")] : []));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["9"], result.Output);
    }

    [Fact]
    public void SingleModuleUnknownImportIsDiagnosed()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT missing\nPRINT 1"));
        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2049");
    }

    [Fact]
    public void ImportedTypeConstructorIsAvailable()
    {
        var result = Run("IMPORT shapes\nDIM x AS Box=Box(9)\nPRINT x.Item",
            "PUBLIC TYPE Box\nItem AS INTEGER\nEND TYPE");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["9"], result.Output);
    }

    [Theory]
    [InlineData("PUBLIC")]
    [InlineData("PRIVATE")]
    public void VisibilityDoesNotChangeTypeFieldConstantResolution(string visibility)
    {
        var result = Run("IMPORT shapes\nDIM x AS Box\nPRINT LEN(x.Item)",
            $"{visibility} CONST Width AS INTEGER=4\nPUBLIC TYPE Box\nItem AS STRING * Width\nEND TYPE");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["4"], result.Output);
    }

    [Fact]
    public void SyntaxErrorsInAdditionalDocumentsFailCompilationAndKeepDocument()
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest("IMPORT shapes\nPRINT 1",
            sourceName: "main.axm", additionalDocuments: [new SourceDocument("shapes.axm", "PUBLIC FUNCTION Broken( AS INTEGER\nEND FUNCTION")]));
        Assert.Equal(CompilationStatus.SyntaxFailure, result.Status);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, d => Assert.Equal("shapes.axm", d.Location.Document));
    }

    [Fact]
    public void RethrownErrorKeepsOriginDocument()
    {
        var result = Run("IMPORT shapes\nPRINT Failing()",
            "PUBLIC FUNCTION Failing() AS INTEGER\nTRY\nERROR \"CUSTOM\", \"bad\"\nCATCH e\nERROR e\nEND TRY\nRETURN 0\nEND FUNCTION");
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("shapes.axm", diagnostic.Location.Document);
        Assert.Equal(3, diagnostic.Location.Line);
    }

    [Fact]
    public void BytecodeRoundTripKeepsSourceDocuments()
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest("IMPORT shapes\nPRINT Square(3)",
            sourceName: "main.axm", additionalDocuments: [new SourceDocument("shapes.axm", "PUBLIC FUNCTION Square(x AS INTEGER) AS INTEGER\nRETURN x*x\nEND FUNCTION")]));
        Assert.Equal(CompilationStatus.Success, result.Status);
        var module = result.CompiledProgram!.BytecodeModule!;
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(module));
        Assert.Equal(module.DebugInfo!.InstructionMap.Select(m => m.Location), restored.DebugInfo!.InstructionMap.Select(m => m.Location));
        Assert.Contains(restored.DebugInfo.InstructionMap, m => m.Location.Document == "shapes.axm");
    }

    private static ExecutionResult Run(string main, string shapes) => new AxiomExecutionPipeline().Run(new ExecutionRequest(main,
        sourceName: "main.axm", additionalDocuments: [new SourceDocument("shapes.axm", shapes)]));

    [Fact]
    public void SelfImportOfNamedSingleFileIsReportedAsCyclicImport()
    {
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest("IMPORT main\nPRINT 1", sourceName: "main.axm"));
        Assert.Equal(CompilationStatus.SemanticFailure, compilation.Status);
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal("MB2052", diagnostic.Code);
        Assert.Contains("main", diagnostic.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelfImportKeepsTheSameDiagnosticWithOrWithoutAdditionalDocuments(bool withUnusedDocument)
    {
        var pipeline = new AxiomExecutionPipeline();
        var documents = withUnusedDocument ? new[] { new SourceDocument("unused.axm", "PRINT 0") } : [];
        var result = pipeline.Run(new ExecutionRequest("IMPORT main\nPRINT 1",
            sourceName: "main.axm",
            additionalDocuments: documents));

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2052", diagnostic.Code);
    }

    [Fact]
    public void UnnamedSingleFileApiStillBindsWithoutEntryName()
    {
        // Безымянный вызов API сохраняет прежнее поведение: модуль остаётся
        // синтетическим, поэтому самоимпорт сообщается как неизвестный модуль.
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT main\nPRINT 1"));

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB2049");
    }

    [Fact]
    public void StepIntoRecognizesTheSameLineInAnotherDocument()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT shapes\nPRINT Answer()",
            sourceName: "main.axm", additionalDocuments: [new SourceDocument("shapes.axm", "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION")],
            sourceBreakpointLines: [2]));
        Assert.True(result.Stopped);
        Assert.Equal("main.axm", result.StoppedAt!.SourceLocation.Document);
        var stepped = result.StopState!.StepInto();
        Assert.True(stepped.Stopped);
        Assert.Equal("shapes.axm", stepped.StoppedAt!.SourceLocation.Document);
        Assert.Equal(2, stepped.StoppedAt.SourceLocation.Line);
    }

    [Fact]
    public void DocumentBreakpointDoesNotStopInAnotherFileOnTheSameLine()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT shapes\nPRINT Answer()",
            sourceName: "main.axm", additionalDocuments: [new SourceDocument("shapes.axm", "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION")],
            sourceBreakpoints: [new Axiom.Runtime.BytecodeSourceBreakpoint(2, document: "shapes.axm")]));
        Assert.True(result.Stopped);
        Assert.Equal("shapes.axm", result.StoppedAt!.SourceLocation.Document);
        Assert.False(result.StopState!.Resume().Stopped);
    }

    [Fact]
    public void LocalTypeCannotSilentlyReplaceAnImportedType()
    {
        var result = Run("IMPORT shapes\nTYPE Box\nOther AS INTEGER\nEND TYPE\nPRINT 1", "PUBLIC TYPE Box\nItem AS INTEGER\nEND TYPE");
        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2055");
    }

    [Fact]
    public void UnusedDocumentsAreStillCheckedButNotInitialized()
    {
        var pipeline = new AxiomExecutionPipeline();
        var invalid = pipeline.Compile(new CompilationRequest("PRINT 1", additionalDocuments: [new SourceDocument("unused.axm", "PRINT missingName")]));
        Assert.Equal(CompilationStatus.SemanticFailure, invalid.Status);
        var valid = pipeline.Run(new ExecutionRequest("PRINT 1", additionalDocuments: [new SourceDocument("unused.axm", "PRINT 2")]));
        Assert.Equal(ExecutionStatus.Success, valid.Status);
        Assert.Equal(["1"], valid.Output);
    }

    [Fact]
    public void DiamondImportInitializesOnceAndByRefCrossesModules()
    {
        var pipeline = new AxiomExecutionPipeline();
        const string source = "IMPORT left\nIMPORT right\nDIM value AS INTEGER=1\nChange(value)\nPRINT value";
        SourceDocument[] documents = [
            new("left.axm", "IMPORT state\nPUBLIC SUB Change(BYREF value AS INTEGER)\nvalue=value+counter\nEND SUB"),
            new("right.axm", "IMPORT state\nPUBLIC SUB Unused()\nEND SUB"),
            new("state.axm", "PUBLIC DIM counter AS INTEGER=10\nPRINT \"init\"")];
        for (var iteration = 0; iteration < 2; iteration++)
        {
            var result = pipeline.Run(new ExecutionRequest(source, sourceName:"main.axm", additionalDocuments:documents));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(["init", "11"], result.Output);
        }
    }

    [Fact]
    public void InitializationFailurePreventsEntryExecution()
    {
        var result = Run("IMPORT shapes\nPRINT \"entry\"", "ERROR \"INIT\", \"failed\"");
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Empty(result.Output);
        Assert.Equal("shapes.axm", Assert.Single(result.Diagnostics).Location.Document);
    }

    [Fact]
    public void PublicConstantCanSizeATypeInTheImportingModule()
    {
        var result = Run("IMPORT shapes\nTYPE Box\nItem AS STRING * Width\nEND TYPE\nDIM x AS Box\nPRINT LEN(x.Item)", "PUBLIC CONST Width AS INTEGER=4");
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["4"], result.Output);
    }
}
