namespace Axiom.Execution.Tests;

public sealed class NumericConversionContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Cint_UnrepresentableValueIsCaughtAndFinallyRuns(ExecutionMode mode)
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
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3008", "Завершено", "Продолжение"], result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Cint_UncaughtOverflowReportsDiagnosticAtCall(ExecutionMode mode)
    {
        const string source = "PRINT CINT(2147483648.0)";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3008", diagnostic.Code);
        Assert.Equal(source.IndexOf("CINT", StringComparison.Ordinal), diagnostic.Location.Span.Start);
        Assert.False(result.UsedInterpreterFallback);
    }
}
