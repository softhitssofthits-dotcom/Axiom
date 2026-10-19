namespace Axiom.Execution.Tests;

public sealed class ValPrefixContractTests
{
    [Theory]
    [InlineData("12eX", "12")]
    [InlineData("12e+X", "12")]
    [InlineData("12e", "12")]
    [InlineData("12١x", "12")]
    [InlineData("12e-2x", "0.12")]
    [InlineData("١2", "0")]
    [InlineData("", "0")]
    [InlineData("1e999", "0")]
    public void ReadsLongestCompleteInvariantNumericPrefix(string text, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT VAL(\"{text}\")"));
        Assert.True(result.Succeeded);
        Assert.Equal(new[] { expected }, result.Output);
    }
}
