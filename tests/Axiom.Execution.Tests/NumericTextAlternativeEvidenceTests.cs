namespace Axiom.Execution.Tests;

public sealed class NumericTextAlternativeEvidenceTests
{
    [Theory]
    [InlineData("0", "TRUE", "0")]
    [InlineData("abc", "FALSE", "0")]
    [InlineData("12abc", "FALSE", "12")]
    [InlineData("1e999", "TRUE", "0")]
    [InlineData("-1e999", "TRUE", "0")]
    [InlineData("1e-999", "TRUE", "0")]
    [InlineData("0e999", "TRUE", "0")]
    [InlineData(".5", "TRUE", "0.5")]
    [InlineData("-12.5e2", "TRUE", "-1250")]
    public void GrammarAndValDoNotProvideFullFiniteRepresentabilityEvidence(string text, string grammar, string value)
    {
        var source = $$"""
DIM text AS STRING = "{{text}}"
PRINT REGEXISMATCH(text, "\A[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?\z")
PRINT VAL(text)
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { grammar, value }, run.Output);
        }
    }
}
