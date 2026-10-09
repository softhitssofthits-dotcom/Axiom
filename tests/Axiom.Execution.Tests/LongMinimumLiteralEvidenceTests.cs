namespace Axiom.Execution.Tests;

public sealed class LongMinimumLiteralEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MinimumValueIsAvailableThroughExactArithmetic(ExecutionMode mode)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            PRINT -9223372036854775807 - 1
            PRINT -&H7FFFFFFFFFFFFFFF - 1
            PRINT CLNG(-CDEC(9223372036854775807) - CDEC(1))
            PRINT 9223372036854775807
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { "-9223372036854775808", "-9223372036854775808", "-9223372036854775808", "9223372036854775807" }, result.Output);
    }

    [Theory]
    [InlineData("-9223372036854775808", "9223372036854775808")]
    [InlineData("-&H8000000000000000", "&H8000000000000000")]
    public void UnaryMinusDoesNotAdmitAnOutOfRangePositiveToken(string expression, string token)
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var source = "PRINT " + expression;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.SyntaxFailure, result.Status);
            Assert.Empty(result.Output);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB0004", diagnostic.Code);
            Assert.Equal(source.IndexOf(token, StringComparison.Ordinal), diagnostic.Location.Span.Start);
            Assert.Equal(token.Length, diagnostic.Location.Span.Length);
            Assert.False(result.UsedInterpreterFallback);
        }
    }
}
