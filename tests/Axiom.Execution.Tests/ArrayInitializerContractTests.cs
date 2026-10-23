using Axiom.Bytecode;

namespace Axiom.Execution.Tests;

public sealed class ArrayInitializerContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SplitInitializerCreatesStringArray(bool explicitType)
    {
        Check(Declaration("fruits[]", "STRING", "SPLIT(\"яблоко,банан,апельсин\", \",\")", explicitType)
            + "\nPRINT fruits[2]\nPRINT LBOUND(fruits[])\nPRINT UBOUND(fruits[])", ["банан", "1", "3"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GlobalAndLocalInitializersEvaluateOncePerVariable(bool explicitType)
    {
        var source = "DIM calls AS INTEGER\n"
            + Declaration("fruits[]", "STRING", "MakeFruits()", explicitType) + "\nPRINT calls\nReadLocal()\nPRINT calls\n"
            + "SUB ReadLocal()\n" + Declaration("localFruits[]", "STRING", "MakeFruits()", explicitType)
            + "\nPRINT localFruits[2]\nEND SUB\n"
            + "FUNCTION MakeFruits() AS STRING[]\ncalls = calls + 1\nRETURN SPLIT(\"яблоко,банан\", \",\")\nEND FUNCTION";
        Check(source, ["1", "банан", "2"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedInitializersEvaluateSeparatelyInNameOrder(bool explicitType)
    {
        var source = "DIM calls AS INTEGER\n"
            + Declaration("first[], second[]", "INTEGER", "MakeNumbers()", explicitType)
            + "\nPRINT calls\nPRINT first[1]\nPRINT second[1]\n"
            + "FUNCTION MakeNumbers() AS INTEGER[]\ncalls = calls + 1\nDIM values[1 TO 1] AS INTEGER\nvalues[1] = calls\nRETURN ARRAYCLONE(values[])\nEND FUNCTION";
        Check(source, ["2", "1", "2"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitializerKeepsArrayAliasAndUnallocatedState(bool explicitType)
    {
        var source = "DIM original[] AS INTEGER\n"
            + Declaration("alias[]", "INTEGER", "original[]", explicitType)
            + "\nPRINT ARRAYISALLOCATED(alias[])\nREDIM alias[-2 TO -1]\nalias[-1] = 42\n"
            + "PRINT ARRAYISALLOCATED(original[])\nPRINT LBOUND(original[])\nPRINT original[-1]\n";
        Check(source, ["FALSE", "TRUE", "-2", "42"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ViewInitializerKeepsBoundsSharedWritesAndExpiration(bool explicitType)
    {
        var source = "DIM grid[-2 TO -1, 7 TO 8] AS INTEGER\n"
            + Declaration("basketRow[]", "INTEGER", "grid[-1]", explicitType)
            + "\nbasketRow[7] = 99\nPRINT grid[-1, 7]\nPRINT LBOUND(basketRow[])\nPRINT UBOUND(basketRow[])\n"
            + "REDIM PRESERVE grid[-2 TO 0, 7 TO 8]\nTRY\nPRINT basketRow[7]\nCATCH err\nPRINT err.Code\nEND TRY";
        Check(source, ["99", "7", "8", "MB3014"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyInitializerStaysAllocated(bool explicitType)
    {
        var source = "DIM source[5 TO 5] AS INTEGER\n"
            + Declaration("empty[]", "INTEGER", "ARRAYTAKE(source[], 0)", explicitType)
            + "\nPRINT ARRAYISALLOCATED(empty[])\nPRINT ARRAYLENGTH(empty[])\nARRAYINSERT(empty[], 1, 7)\nPRINT empty[1]";
        Check(source, ["TRUE", "0", "7"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedStringArrayKeepsNormalizationAndCloneRules(bool explicitType)
    {
        var source = "DIM source[2 TO 2] AS STRING * 3\nsource[2] = \"яблоко\"\n"
            + Declaration("alias[]", "STRING", "source[]", explicitType)
            + "\nalias[2] = \"банан\"\nPRINT source[2]\n"
            + Declaration("copy[]", "STRING", "ARRAYCLONE(source[])", explicitType)
            + "\ncopy[2] = \"ёж\"\nPRINT source[2]\nPRINT copy[2] = \"ёж \"\nPRINT LEN(copy[2])";
        Check(source, ["бан", "бан", "TRUE", "3"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecordArraysKeepAliasAndExplicitClone(bool explicitType)
    {
        var source = "TYPE Fruit\nPrice AS INTEGER\nEND TYPE\nDIM source[1 TO 1] AS Fruit\nsource[1].Price = 10\n"
            + Declaration("alias[]", "Fruit", "source[]", explicitType)
            + "\nalias[1].Price = 20\nPRINT source[1].Price\n"
            + Declaration("copy[]", "Fruit", "ARRAYCLONE(source[])", explicitType)
            + "\ncopy[1].Price = 30\nPRINT source[1].Price\nPRINT copy[1].Price";
        Check(source, ["20", "20", "30"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultidimensionalInitializerSurvivesBytecodeRoundTrip(bool explicitType)
    {
        var source = "DIM original[-2 TO -1, 7 TO 8, 0 TO 0] AS INTEGER\noriginal[-1, 8, 0] = 42\n"
            + Declaration("copy[,,]", "INTEGER", "original[]", explicitType)
            + "\nPRINT ARRAYRANK(copy[])\nPRINT LBOUND(copy[], 1)\nPRINT UBOUND(copy[], 2)\nPRINT copy[-1, 8, 0]";
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source));
        Assert.True(compilation.Succeeded, Explain(compilation.Diagnostics.Concat(compilation.BackendDiagnostics)));
        var original = compilation.CompiledProgram!;
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(original.BytecodeModule!));
        Assert.Empty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
        var program = new CompiledProgram(source, original.Syntax, original.BoundProgram, original.LoweredProgram, restored, original.SourceDocuments);
        var result = pipeline.RunCompiled(program, new ExecutionRequest(source));
        Assert.True(result.Succeeded, Explain(result.Diagnostics));
        Assert.Equal(["3", "-2", "8", "42"], result.Output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ModuleGlobalInitializerIsSharedWithExportedFunction(bool explicitType)
    {
        const string main = "IMPORT shop\nPRINT fruits[2]\nfruits[1] = \"груша\"\nPRINT FirstFruit()";
        var shop = "PUBLIC " + Declaration("fruits[]", "STRING", "SPLIT(\"яблоко,банан\", \",\")", explicitType)
            + "\nPUBLIC FUNCTION FirstFruit() AS STRING\nRETURN fruits[1]\nEND FUNCTION";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(main, sourceName: "main.axm",
            additionalDocuments: [new SourceDocument("shop.axm", shop)]));
        Assert.True(result.Succeeded, Explain(result.Diagnostics));
        Assert.Equal(["банан", "груша"], result.Output);
    }

    [Theory]
    [InlineData("DIM original[1 TO 1] AS INTEGER\nDIM wrong[,] AS INTEGER = original[]")]
    [InlineData("DIM wrong[] AS INTEGER = SPLIT(\"a,b\", \",\")")]
    [InlineData("DIM wrong[] = 42")]
    [InlineData("DIM wrong[1 TO 2] AS STRING = SPLIT(\"a,b\", \",\")")]
    public void InvalidTypesRanksAndDeclaratorBoundsStayRejected(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2003");
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == "BC3001");
    }

    private static string Declaration(string name, string type, string initializer, bool explicitType) =>
        "DIM " + name + (explicitType ? " AS " + type : "") + " = " + initializer;

    private static string Explain(IEnumerable<Axiom.Core.Diagnostic> diagnostics) => string.Join(" | ", diagnostics.Select(d => d.Message));

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, Explain(result.Diagnostics));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
