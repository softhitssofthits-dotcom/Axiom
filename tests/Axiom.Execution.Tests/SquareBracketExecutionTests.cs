using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Execution.Tests;

public sealed class SquareBracketExecutionTests
{
    [Theory]
    [InlineData("DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")")]
    [InlineData("DIM fruits[] = SPLIT(\"яблоко,банан\", \",\")")]
    public void WholeArrayAliasAndExplicitClonePreserveOwnership(string declaration)
    {
        Check(declaration + "\nDIM basket[] = fruits[]\nbasket[2] = \"груша\"\nDIM copy[] = ARRAYCLONE(fruits[])\ncopy[2] = \"апельсин\"\nPRINT fruits[2]\nPRINT copy[2]\nPRINT ARRAYLENGTH(basket[])", "груша", "апельсин", "2");
    }

    [Fact]
    public void WholeArrayByRefReplacesOriginalVariable()
    {
        Check("DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nARRAYPUSH(fruits[], \"апельсин\")\nPRINT fruits[3]\nRefill(fruits[])\nPRINT fruits[1]\nPRINT ARRAYLENGTH(fruits[])\nSUB Refill(BYREF values[] AS STRING)\nvalues[] = SPLIT(\"груша\", \",\")\nEND SUB", "апельсин", "груша", "1");
    }

    [Fact]
    public void FunctionResultIndexCallsAndInterpolationUseSquareAst()
    {
        Check("DIM calls AS INTEGER = 0\nDIM fruits[] = Make()\nPRINT fruits[Choose() + (1 - 1)]\nPRINT Make()[2]\nPRINT $\"Фрукт: {fruits[1]}\"\nPRINT calls\nFUNCTION Make() AS STRING[]\ncalls = calls + 1\nRETURN SPLIT(\"яблоко,банан\", \",\")\nEND FUNCTION\nFUNCTION Choose() AS INTEGER\nRETURN 2\nEND FUNCTION", "банан", "банан", "Фрукт: яблоко", "2");
    }

    [Fact]
    public void MultidimensionalViewsShareStorageAndBounds()
    {
        Check("DIM grid[-2 TO -1, 7 TO 8] AS INTEGER\ngrid[-1, 7] = 42\nDIM alias[,] = grid[]\nDIM rowValues[] = grid[-1]\nDIM windowValues[,] = grid[-2 TO -1, 7 TO 8]\nrowValues[8] = 17\nPRINT alias[-1, 7]\nPRINT windowValues[-1, 8]\nPRINT LBOUND(alias[], 1)\nPRINT UBOUND(rowValues[])\nPRINT ARRAYRANK(windowValues[])\nDIM empty[] AS INTEGER\nDIM other[] = empty[]\nPRINT ARRAYISALLOCATED(other[])", "42", "17", "-2", "8", "2", "FALSE");
    }

    [Fact]
    public void FieldsRecordsAndFixedStringsRetainExistingExecution()
    {
        Check("TYPE Bag\nfruits[] AS STRING\nlabels[1 TO 1] AS STRING * 4\nEND TYPE\nDIM bags[1 TO 1] AS Bag\nbags[1].fruits[] = SPLIT(\"яблоко,банан\", \",\")\nDIM basket[] = bags[1].fruits[]\nbasket[2] = \"груша\"\nPRINT bags[1].fruits[2]\nbags[1].labels[1] = \"яблоко\"\nPRINT bags[1].labels[1]\nREDIM PRESERVE bags[1].fruits[1 TO 3]\nPRINT ARRAYLENGTH(bags[1].fruits[])\nERASE bags[1].fruits[]\nPRINT ARRAYISALLOCATED(bags[1].fruits[])", "груша", "ябло", "3", "FALSE");
    }

    [Fact]
    public void RecordArrayCloneKeepsIndependentRecords()
    {
        Check("TYPE Fruit\nprice AS INTEGER\nEND TYPE\nDIM fruits[1 TO 1] AS Fruit\nfruits[1].price = 10\nDIM alias[] = fruits[]\nalias[1].price = 20\nDIM copy[] = ARRAYCLONE(fruits[])\ncopy[1].price = 30\nPRINT fruits[1].price\nPRINT copy[1].price", "20", "30");
    }

    [Fact]
    public void RedimAndEraseKeepExistingViewInvalidation()
    {
        Check("DIM fruits[1 TO 2] AS INTEGER\nfruits[1] = 5\nDIM arrayView[] = fruits[1 TO 2]\nREDIM PRESERVE fruits[1 TO 3]\nPRINT fruits[1]\nTRY\nPRINT arrayView[1]\nCATCH problem\nPRINT problem.Code\nEND TRY\nERASE fruits[]\nPRINT ARRAYISALLOCATED(fruits[])", "5", "MB3014", "FALSE");
    }

    [Fact]
    public void MultidimensionalBytecodeRoundTripPreservesOutput()
    {
        const string source = "DIM grid[-2 TO -1, 7 TO 8, 0 TO 0] AS INTEGER\ngrid[-1, 8, 0] = 42\nDIM copy[,,] = grid[]\nPRINT ARRAYRANK(copy[])\nPRINT LBOUND(copy[], 1)\nPRINT copy[-1, 8, 0]";
        var program = Compile(source);
        var restored = BytecodeModuleSerializer.DeserializeModuleFromJson(BytecodeModuleSerializer.SerializeToJson(program.BytecodeModule!));
        Assert.Empty(new BytecodeModuleValidator().Validate(restored).Diagnostics);
        var replay = new CompiledProgram(source, program.Syntax, program.BoundProgram, program.LoweredProgram, restored, program.SourceDocuments);
        var pipeline = new AxiomExecutionPipeline();
        var first = pipeline.RunCompiled(replay, new ExecutionRequest(source));
        var second = pipeline.RunCompiled(replay, new ExecutionRequest(source));
        Assert.True(first.Succeeded, Explain(first.Diagnostics));
        Assert.True(second.Succeeded, Explain(second.Diagnostics));
        Assert.Equal(["3", "-2", "42"], first.Output);
        Assert.Equal(first.Output, second.Output);
    }

    [Fact]
    public void PublicImportsKeepArrayTypesAndByRef()
    {
        const string main = "IMPORT shop\nPRINT fruits[2]\nRefill(fruits[])\nPRINT fruits[1]";
        const string shop = "PUBLIC DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nPUBLIC SUB Refill(BYREF values[] AS STRING)\nvalues[] = SPLIT(\"груша\", \",\")\nEND SUB";
        var syntax = Parse(main, "main.axm");
        var shopSyntax = Parse(shop, "shop.axm");
        var bound = Binder.BindModules([new("main", syntax), new("shop", shopSyntax)], "main", out var binding, checkGraphicsModes: false);
        Assert.Empty(binding);
        var compiled = CompileBound(main, syntax, bound, [new("main.axm", main), new("shop.axm", shop)]);
        var result = new AxiomExecutionPipeline().RunCompiled(compiled, new ExecutionRequest(main, sourceName: "main.axm"));
        Assert.True(result.Succeeded, Explain(result.Diagnostics));
        Assert.Equal(["банан", "груша"], result.Output);
    }

    [Fact]
    public void NamedArrayConstantRetainsExistingSharedStorageSemantics()
    {
        Check("CONST values[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nDIM alias[] = values[]\nalias[2] = \"груша\"\nPRINT values[2]\nPRINT ARRAYLENGTH(values[])", "груша", "2");
    }

    [Fact]
    public void NamedArrayConstantPreservesMultidimensionalRank()
    {
        Check("DIM source[1 TO 2, 7 TO 8] AS INTEGER\nsource[2, 8] = 42\nCONST values[,] AS INTEGER = source[]\nPRINT values[2, 8]\nPRINT ARRAYRANK(values[])\nPRINT LBOUND(values[], 2)", "42", "2", "7");
    }

    private static ProgramSyntax Parse(string source, string? document = null)
    {
        var syntax = Parser.ParseSquareBracketSyntax(source, out var parsing, document);
        Assert.Empty(parsing);
        Assert.True(syntax.UsesSquareArraySyntax);
        return syntax;
    }

    private static CompiledProgram Compile(string source)
    {
        var syntax = Parse(source);
        var bound = Binder.Bind(syntax, out var binding, checkGraphicsModes: false);
        Assert.Empty(binding);
        return CompileBound(source, syntax, bound);
    }

    private static CompiledProgram CompileBound(string source, ProgramSyntax syntax, BoundProgram bound,
        IEnumerable<SourceDocument>? documents = null)
    {
        var lowering = new BoundProgramLowerer().Lower(bound);
        Assert.True(lowering.Succeeded, string.Join(" | ", lowering.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var compilation = new BytecodeCompiler().Compile(lowering.Program!);
        Assert.True(compilation.Succeeded, string.Join(" | ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(new BytecodeModuleValidator().Validate(compilation.Module!).Diagnostics);
        return new CompiledProgram(source, syntax, bound, lowering.Program, compilation.Module, documents);
    }

    private static string Explain(IEnumerable<Diagnostic> diagnostics) =>
        string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Message));

    private static void Check(string source, params string[] expected)
    {
        var result = new AxiomExecutionPipeline().RunCompiled(Compile(source), new ExecutionRequest(source));
        Assert.True(result.Succeeded, Explain(result.Diagnostics));
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
