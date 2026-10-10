namespace Axiom.Execution.Tests;

public sealed class TimeZoneUnicodeBoundaryEvidenceTests
{
    [Fact]
    public void InvalidZoneNamesAndOutOfRangeConversionsPreserveResult()
    {
        var source = """
            DIM firstMoment AS DATETIME = "0001-01-01 00:00:00"
            DIM lastMoment AS DATETIME = "9999-12-31 23:59:59"
            DIM result AS DATETIME = "2000-02-29 12:00:00"
            PRINT UTCOFFSET(firstMoment)
            PRINT UTCOFFSET(firstMoment, "UTC")
            PRINT TOUTC(firstMoment, "UTC")
            PRINT FROMUTC(lastMoment, "UTC")
            TRY
                result = TOUTC(firstMoment)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT result
            TRY
                result = FROMUTC(lastMoment)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT result
            TRY
                PRINT UTCOFFSET(result, "UTC" + CHRW(0))
            CATCH err
                PRINT err.Code
            END TRY
            TRY
                result = TOUTC(result, "UTC😀")
            CATCH err
                PRINT err.Code
            END TRY
            TRY
                result = FROMUTC(result, LEFT("😀", 1))
            CATCH err
                PRINT err.Code
            END TRY
            PRINT result
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, runtimeClock: new DateTimeIndependentContractTests.ControlledClock()));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "180", "0", "0001-01-01 00:00:00", "9999-12-31 23:59:59", "MB3007", "2000-02-29 12:00:00", "MB3007", "2000-02-29 12:00:00", "MB3007", "MB3007", "MB3007", "2000-02-29 12:00:00" }, run.Output);
    }
}
