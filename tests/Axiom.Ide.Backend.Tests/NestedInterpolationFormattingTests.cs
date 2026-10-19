using Axiom.Execution;
using Axiom.Formatting;

namespace Axiom.Ide.Backend.Tests;

public sealed class NestedInterpolationFormattingTests
{
    [Theory]
    [InlineData("PRINT $\"a{$\"b{1}c\"}d\"", "ab1cd")]
    [InlineData("PRINT $\"{LEN($\"x{2}\")}\"", "2")]
    [InlineData("PRINT $\"{$\"{1:F2}\",6}z\"", "  1.00z")]
    [InlineData("PRINT $\"a{$\"b{$\"c{1}d\"}e\"}f{$\"g{2}h\"}i\"", "abc1defg2hi")]
    public void FormattingPreservesNestedTemplateResult(string source, string expected)
    {
        var formatter = new AxiomCodeFormattingService();
        foreach (var style in Enum.GetValues<CodeFormattingStyle>())
        {
            var formatted = formatter.FormatDocument(source, style);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(formatted));
            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { expected }, result.Output);
            Assert.Equal(formatted, formatter.FormatDocument(formatted, style));
        }
    }
}
