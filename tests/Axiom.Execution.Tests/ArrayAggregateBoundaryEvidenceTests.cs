namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateBoundaryEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void DecimalAcceptanceSampleRuns(ExecutionMode mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Axiom.sln"))) repository = repository.Parent;
        Assert.NotNull(repository);
        Check(mode, File.ReadAllText(Path.Combine(repository.FullName, "samples/functions/arrayaverage/decimal_mean.axm")), ["0.5", "TRUE"]);
    }

    public static IEnumerable<object[]> EmptyCases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "ARRAYSUM", "ARRAYMIN", "ARRAYMAX", "ARRAYAVERAGE" })
        foreach (var allocated in new[] { false, true }) yield return [mode, operation, allocated];
    }

    [Theory]
    [MemberData(nameof(EmptyCases))]
    public void EmptyAggregateCannotAllocateOrModifyArray(ExecutionMode mode, string operation, bool allocated)
    {
        var setup = allocated ? "DIM values(1 TO 1) AS INTEGER\nvalues = ARRAYTAKE(values, 0)" : "DIM values() AS INTEGER";
        Check(mode, setup + $$"""

            TRY
                PRINT {{operation}}(values)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT ARRAYLENGTH(values)
            PRINT ARRAYISALLOCATED(values)
            """, [allocated ? "MB3007" : "MB3005", "0", allocated ? "TRUE" : "FALSE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void LongExtremaKeepExactSumAndDocumentedDoubleAverage(ExecutionMode mode)
    {
        Check(mode, """
            DIM values(-2 TO -1) AS LONG
            values(-2) = -9223372036854775807 - CLNG(1)
            values(-1) = 9223372036854775807
            PRINT ARRAYSUM(values)
            PRINT ARRAYMIN(values)
            PRINT ARRAYMAX(values)
            PRINT ARRAYAVERAGE(values)
            PRINT values(-2)
            PRINT values(-1)
            """, ["-1", "-9223372036854775808", "9223372036854775807", "0", "-9223372036854775808", "9223372036854775807"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void DecimalIntermediateOverflowCannotChangeSource(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS DECIMAL = CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591)
            DIM values(-2 TO 0) AS DECIMAL
            values(-2) = high
            values(-1) = CDEC(1)
            values(0) = -high
            TRY
                PRINT ARRAYSUM(values)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT ARRAYMIN(values)
            PRINT ARRAYMAX(values)
            PRINT ARRAYAVERAGE(values) = VAL("0.3333333333333333")
            PRINT values(-2) = high
            PRINT values(-1)
            PRINT values(0) = -high
            """, ["MB3008", "-79228162514264337593543950335", "79228162514264337593543950335", "TRUE", "TRUE", "1", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void DoubleMatrixAverageCancelsExtremaAfterSumFailure(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS DOUBLE = VAL("1.7976931348623157e308")
            DIM values(0 TO 1, -2 TO -1) AS DOUBLE
            values(0, -2) = high
            values(0, -1) = high
            values(1, -2) = -high
            values(1, -1) = -high
            TRY
                PRINT ARRAYSUM(values)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT ARRAYAVERAGE(values)
            PRINT ARRAYMIN(values) = -high
            PRINT ARRAYMAX(values) = high
            PRINT values(0, -2) = high
            PRINT values(0, -1) = high
            PRINT values(1, -2) = -high
            PRINT values(1, -1) = -high
            """, ["MB3008", "0", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
