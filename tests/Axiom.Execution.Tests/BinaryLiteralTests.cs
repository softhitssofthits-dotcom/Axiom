namespace Axiom.Execution.Tests;

public sealed class BinaryLiteralTests
{
    [Theory]
    [InlineData("PRINT &B1010", "10")]
    [InlineData("PRINT -&b1010", "-10")]
    [InlineData("DIM n AS LONG = &B1010\nPRINT n", "10")]
    [InlineData("DIM n AS DECIMAL = &B1010\nPRINT n", "10")]
    [InlineData("CONST n AS LONG = &B1010\nPRINT n", "10")]
    [InlineData("PRINT &B1010 AND &B1100", "8")]
    [InlineData("DIM n AS LONG = &H100000000\nPRINT n OR &B1010", "4294967306")]
    [InlineData("PRINT $\"{&B1010:B8}\"", "00001010")]
    [InlineData("PRINT $\"{&B11111111:X4}\"", "00FF")]
    public void BinaryLiteralCompilesAndRuns(string source, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal([expected], result.Output);
    }

    [Fact]
    public void InvalidBinaryDigitCannotBePrintedAsSeparateDecimalValue()
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest("PRINT &B102"));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, item => item.Code == "MB0003");
    }
}
