namespace Axiom.Execution.Tests;

public sealed class NestedInterpolationContractTests
{
    [Theory]
    [InlineData("PRINT $\"{$\"x\"}\"", "x")]
    [InlineData("PRINT $\"a{$\"b{1}c\"}d\"", "ab1cd")]
    [InlineData("PRINT $\"{LEN($\"x{2}\")}\"", "2")]
    [InlineData("PRINT $\"{$\"{1:F2}\",6}z\"", "  1.00z")]
    public void NestedTemplateRestoresOuterExpressionState(string source, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { expected }, result.Output);
    }
}
