using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class StringCancellationContractTests
{
    [Theory]
    [InlineData("REPLACEMANY(text, oldItems, newItems)")]
    [InlineData("STR(SUBSTRCOUNT(text, \"a\"))")]
    [InlineData("STR(LEN(text, \"CODEPOINT\"))")]
    public void LongInternalScanObservesCancellation(string expression)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
DIM text AS STRING = REPEAT("a", 200000)
DIM oldItems(1 TO 1) AS STRING
DIM newItems(1 TO 1) AS STRING
oldItems(1) = "z"
newItems(1) = "x"
PRINT {{expression}}
""", cancellationProvider: new CancelAfterChecks(64)));
        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
    }

    [Fact]
    public void ExpandingReplaceObservesCancellationDuringMatchCounting()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
DIM text AS STRING = REPEAT("a", 200000)
PRINT REPLACE(text, "a", "xx")
""", cancellationProvider: new CancelAfterChecks(64)));
        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
    }

    private sealed class CancelAfterChecks(int limit) : IRuntimeCancellationProvider
    {
        private int _checks;
        public bool IsCancellationRequested => ++_checks >= limit;
    }
}
