using Axiom.Core;

namespace Axiom.Execution.Tests;

public sealed class NextCounterExecutionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("counter")]
    [InlineData("COUNTER")]
    [InlineData("counter 'Комментарий после NEXT")]
    [InlineData("'Комментарий после NEXT")]
    public void Run_AcceptsOmittedOrMatchingNextCounter(string nextName)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            $"FOR counter = 1 TO 2\nPRINT counter\nNEXT {nextName}"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["1", "2"], result.Output);
    }

    [Fact]
    public void Run_AcceptsNamesOfBothNestedCounters()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            "FOR outer = 1 TO 2\nFOR inner = 1 TO 2\nPRINT outer, inner\nNEXT INNER\nNEXT OUTER"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["1 1", "1 2", "2 1", "2 2"], result.Output);
    }

    [Fact]
    public void Run_AcceptsLineAsBothForAndNextCounterName()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            "FOR line = 1 TO 2\nPRINT line\nNEXT LINE"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["1", "2"], result.Output);
    }

    [Theory]
    [InlineData("", "missing")]
    [InlineData("", "line")]
    [InlineData("DIM wrong AS INTEGER\n", "wrong")]
    [InlineData("FOR outer = 1 TO 2\n", "outer")]
    public void Run_RejectsMismatchedNextCounterBeforeAnyExecution(string prefix, string nextName)
    {
        var suffix = prefix.StartsWith("FOR", StringComparison.Ordinal) ? "\nNEXT outer" : "";
        var source = $"PRINT \"Must not execute\"\n{prefix}FOR counter = 1 TO 2\nPRINT counter\nNEXT {nextName}{suffix}";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Empty(result.Output);
        var error = Assert.Single(result.Diagnostics, d => d.Code == "MB2048");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains($"NEXT '{nextName}'", error.Message, StringComparison.Ordinal);
        Assert.Contains("FOR 'counter'", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownName);
    }

    [Theory]
    [InlineData("", "extra")]
    [InlineData("DIM extra AS INTEGER\n", "extra")]
    [InlineData("", "PRINT 1")]
    [InlineData("", "1")]
    [InlineData("", "+ 1")]
    public void Run_RejectsUnexpectedTokensAfterNextCounter(string prefix, string tail)
    {
        var source = $"PRINT \"Must not execute\"\n{prefix}FOR counter = 1 TO 2\nPRINT counter\nNEXT counter {tail}";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.Equal(ExecutionStatus.SyntaxFailure, result.Status);
        Assert.Empty(result.Output);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnexpectedToken);
    }
}
