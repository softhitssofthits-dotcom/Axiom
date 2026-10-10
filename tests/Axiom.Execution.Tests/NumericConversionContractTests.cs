namespace Axiom.Execution.Tests;

public sealed class NumericConversionContractTests
{
    [Fact]
    public void Cint_UnrepresentableValueIsCaughtAndFinallyRuns()
    {
        const string source = """
            TRY
                PRINT CINT(2147483648.0)
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3008", "Завершено", "Продолжение"], result.Output);
    }

    [Fact]
    public void Cint_UncaughtOverflowReportsDiagnosticAtCall()
    {
        const string source = "PRINT CINT(2147483648.0)";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3008", diagnostic.Code);
        Assert.Equal(source.IndexOf("CINT", StringComparison.Ordinal), diagnostic.Location.Span.Start);
    }
}
