namespace Axiom.Execution.Tests;

public sealed class PrintUsingScientificContractTests
{
    [Theory]
    [InlineData("0.001", "  1.00E-03")]
    [InlineData("0.1", "  1.00E-01")]
    [InlineData("1", "  1.00E+00")]
    [InlineData("1000", "  1.00E+03")]
    [InlineData("-0.001", " -1.00E-03")]
    public void ScientificMaskPreservesExponentSign(string value, string expected) =>
        Check($"PRINT USING \"###.##^^^^\"; {value}", expected);

    [Theory]
    [InlineData("9007199254740993", "                   9.0071992547409930E+15")]
    [InlineData("CDEC(9007199254740993)", "                   9.0071992547409930E+15")]
    public void ScientificMaskPreservesExactIntegralDigits(string value, string expected) =>
        Check($"PRINT USING \"####################.################^^^^\"; {value}", expected);

    [Theory]
    [InlineData("1", 29)]
    [InlineData("1.0", 16)]
    public void LongFractionMaskProducesTextInsteadOfClrException(string value, int digits)
    {
        Check($"PRINT USING \"#.{new string('#', digits)}\"; {value}", "1." + new string('0', digits));
    }

    private static void Check(string source, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { expected }, result.Output);
    }
}
