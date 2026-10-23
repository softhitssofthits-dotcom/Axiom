namespace Axiom.Execution.Tests;

public sealed class WholeArraySyntaxTests
{
    [Theory]
    [InlineData("DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")")]
    [InlineData("DIM fruits[] = SPLIT(\"яблоко,банан\", \",\")")]
    public void DeclaratorInitializerAndWholeArrayKeepAlias(string declaration)
    {
        Check(declaration + "\nDIM basket[] = fruits[]\nbasket[2] = \"груша\"\nPRINT fruits[2]\nPRINT ARRAYLENGTH(basket[])", "груша", "2");
    }

    [Fact]
    public void WholeArraySupportsFieldsReturnsAndCalls()
    {
        Check("TYPE Bag\nfruits[] AS STRING\nEND TYPE\nDIM bagValue AS Bag\nbagValue.fruits[] = SPLIT(\"яблоко,банан\", \",\")\nDIM basket[] AS STRING = bagValue.fruits[]\nShow(basket[])\nPRINT ARRAYLENGTH(MakeFruits()[])\nSUB Show(values[] AS STRING)\nPRINT values[2]\nEND SUB\nFUNCTION MakeFruits() AS STRING[]\nRETURN bagValue.fruits[]\nEND FUNCTION", "банан", "2");
    }

    [Fact]
    public void WholeArrayPreservesRankBoundsViewsAndUnallocatedState()
    {
        Check("DIM grid[-2 TO -1, 7 TO 8] AS INTEGER\nDIM alias[,] AS INTEGER = grid[]\nalias[-1, 7] = 42\nDIM basketRow[] = grid[-1]\nPRINT basketRow[7]\nPRINT LBOUND(alias[], 1)\nPRINT ARRAYRANK(alias[])\nDIM empty[] AS INTEGER\nDIM other[] = empty[]\nPRINT ARRAYISALLOCATED(other[])", "42", "-2", "2", "FALSE");
    }

    [Fact]
    public void WholeArrayAssignmentAndGroupedInitializersKeepOrdinaryRules()
    {
        Check("DIM source[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nDIM first[], second[] AS STRING = source[]\nDIM basket[] AS STRING\nbasket[] = first[]\nbasket[2] = \"груша\"\nPRINT second[2]\nDIM grade AS INTEGER = 3\nPRINT grade", "груша", "3");
    }

    [Fact]
    public void ModuleInitializerUsesDeclaratorArrayType()
    {
        const string source = "IMPORT shop\nPRINT fruits[2]\nPRINT ARRAYLENGTH(fruits[])";
        const string shop = "PUBLIC DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, sourceName: "main.axm",
            additionalDocuments: [new SourceDocument("shop.axm", shop)]));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["банан", "2"], result.Output);
    }

    [Fact]
    public void WholeArrayArgumentsSupportMutationAndByRefReplacement()
    {
        Check("DIM fruits[] AS STRING = SPLIT(\"яблоко,банан\", \",\")\nARRAYPUSH(fruits[], \"апельсин\")\nPRINT fruits[3]\nRefillBasket(fruits[])\nPRINT fruits[1]\nSUB RefillBasket(BYREF values[] AS STRING)\nvalues[] = SPLIT(\"груша\", \",\")\nEND SUB", "апельсин", "груша");
    }

    [Theory]
    [InlineData("DIM wrong[] AS INTEGER = SPLIT(\"a,b\", \",\")")]
    [InlineData("DIM wrong[,] = SPLIT(\"a,b\", \",\")")]
    [InlineData("DIM wrong[] = 42")]
    [InlineData("DIM wrong[1 TO 2] AS STRING = SPLIT(\"a,b\", \",\")")]
    public void IncompatibleDeclarationsStayRejected(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == "MB2003");
    }

    [Theory]
    [InlineData("DIM fruits() AS STRING")]
    [InlineData("DIM fruits[1 TO 2] AS STRING\nPRINT fruits(2)")]
    [InlineData("SUB Show(values AS STRING ())\nEND SUB")]
    [InlineData("FUNCTION Make() AS STRING ()\nRETURN SPLIT(\"яблоко\", \",\")\nEND FUNCTION")]
    public void LegacyRoundArrayFormsStayRejected(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB2003");
        Assert.Empty(result.Output);
    }

    private static void Check(string source, params string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
