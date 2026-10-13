namespace Axiom.Execution.Tests;

public sealed class BooleanContractAuditTests
{
    [Fact]
    public void BooleanConstInitializerUsesValueAtRuntimeDeclaration()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM flag AS BOOLEAN = FALSE
            CONST saved AS BOOLEAN = flag
            flag = TRUE
            PRINT saved
            PRINT flag
            """));
        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE"], result.Output);
    }

    [Theory]
    [InlineData("boolean_values.axm", "FALSE\nTRUE\nTRUE\nready=FALSE\nTRUE\n-1\nFALSE\nTRUE")]
    [InlineData("truth_table.axm", "FALSE FALSE | FALSE FALSE FALSE TRUE TRUE\nFALSE TRUE | FALSE TRUE TRUE FALSE TRUE\nTRUE FALSE | FALSE TRUE TRUE FALSE FALSE\nTRUE TRUE | TRUE TRUE FALSE TRUE TRUE")]
    [InlineData("evaluation_order.axm", "right\nFALSE\nright\nTRUE\nFALSE\nTRUE\nright\nTRUE\nright\nTRUE")]
    public void SamplesHaveIndependentExpectedOutput(string file, string expected)
    {
        var source = TestProgramLoader.LoadText("samples/expressions/logical/" + file);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected.Split('\n'), result.Output);
    }

    [Theory]
    [InlineData("DIM b AS BOOLEAN = -1", "MB2003")]
    [InlineData("DIM b AS BOOLEAN = 0", "MB2003")]
    [InlineData("DIM b AS BOOLEAN = \"TRUE\"", "MB2003")]
    [InlineData("DIM n AS INTEGER = TRUE", "MB2003")]
    [InlineData("PRINT TRUE = -1", "MB2003")]
    [InlineData("PRINT FALSE = 0", "MB2003")]
    [InlineData("PRINT TRUE AND 1", "MB2003")]
    [InlineData("PRINT FALSE < TRUE", "MB2003")]
    [InlineData("PRINT CINT(TRUE)", "MB2003")]
    [InlineData("PRINT CBOOL(1)", "MB2005")]
    [InlineData("PRINT FALSE ANDALSO missing", "MB2001")]
    [InlineData("PRINT FALSE ANDALSO 1", "MB2003")]
    [InlineData("IF -1 THEN\nPRINT 1\nEND IF", "MB2008")]
    public void NumericAndTextTruthinessIsRejected(string source, string code)
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }
}
