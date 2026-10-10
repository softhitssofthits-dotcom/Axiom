namespace Axiom.Execution.Tests;

public sealed class DateArithmeticRangeEvidenceTests
{
    [Fact]
    public void FullDateTimeRangeReturnsExactLongAndExtremeAddsAreCatchable()
    {
        var source = """
            DIM firstMoment AS DATETIME = "0001-01-01 00:00:00"
            DIM lastMoment AS DATETIME = "9999-12-31 23:59:59"
            DIM unchanged AS DATE = DATESERIAL(2000, 2, 29)
            PRINT DATEDIFF("s", firstMoment, lastMoment)
            PRINT DATEDIFF("s", lastMoment, firstMoment)
            PRINT DATEDIFF("d", DATESERIAL(1, 1, 1), DATESERIAL(9999, 12, 31))
            PRINT DATEDIFF("yyyy", firstMoment, lastMoment)
            PRINT DATEPART("y", lastMoment)
            PRINT DATEPART("q", DATESERIAL(9999, 12, 31))
            PRINT DATEPART("h", TIMESERIAL(23, 59, 59))
            TRY
                unchanged = DATEADD("d", 2147483647, unchanged)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT unchanged
            TRY
                unchanged = DATEADD("yyyy", (-2147483647 - 1), unchanged)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT unchanged
            PRINT DATEADD("s", 2147483647, TIMESERIAL(0, 0, 0))
            PRINT DATEADD("s", (-2147483647 - 1), TIMESERIAL(0, 0, 0))
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "315537897599", "-315537897599", "3652058", "9998", "365", "4", "23", "MB3007", "2000-02-29", "MB3007", "2000-02-29", "03:14:07", "20:45:52" }, run.Output);
    }
}
