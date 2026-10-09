namespace Axiom.Execution.Tests;

public sealed class NumericBoundaryContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Cint_TruncatesBeforeCheckingRange(ExecutionMode mode)
    {
        const string source = """
            PRINT CINT(2147483647.9)
            PRINT CINT(-2147483648.9)
            PRINT CINT(CDEC(2147483647) + CDEC(0.9))
            PRINT CINT(CDEC(-2147483648) - CDEC(0.9))
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["2147483647", "-2147483648", "2147483647", "-2147483648"], result.Output);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Clng_RejectsExclusiveUpperBoundAndAcceptsNeighbors(ExecutionMode mode)
    {
        const string source = """
            PRINT CLNG(9223372036854774784.0)
            PRINT CLNG(-9223372036854775808.0)
            TRY
                PRINT CLNG(9223372036854775808.0)
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["9223372036854774784", "-9223372036854775808", "MB3008", "Завершено"], result.Output);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Val_RejectsNonFiniteAndPreservesOrdinaryParsing(ExecutionMode mode)
    {
        const string source = """
            PRINT VAL("NaN")
            PRINT VAL("Infinity")
            PRINT VAL("-Infinity")
            PRINT VAL("1e309")
            PRINT VAL("12abc")
            PRINT VAL("  -2.5  ")
            PRINT VAL("1e2")
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["0", "0", "0", "0", "12", "-2.5", "100"], result.Output);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
    }
}
