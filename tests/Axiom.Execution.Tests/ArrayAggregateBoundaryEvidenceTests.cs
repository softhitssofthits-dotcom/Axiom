namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateBoundaryEvidenceTests
{
    [Fact]
    public void DecimalAcceptanceSampleRuns()
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Axiom.sln"))) repository = repository.Parent;
        Assert.NotNull(repository);
        Check(File.ReadAllText(Path.Combine(repository.FullName, "samples/functions/arrayaverage/decimal_mean.axm")), ["0.5", "TRUE"]);
    }

    public static IEnumerable<object[]> EmptyCases()
    {

        foreach (var operation in new[] { "ARRAYSUM", "ARRAYMIN", "ARRAYMAX", "ARRAYAVERAGE" })
        foreach (var allocated in new[] { false, true }) yield return [operation, allocated];
    }

    [Theory]
    [MemberData(nameof(EmptyCases))]
    public void EmptyAggregateCannotAllocateOrModifyArray(string operation, bool allocated)
    {
        var setup = allocated ? "DIM values[1 TO 1] AS INTEGER\nvalues[] = ARRAYTAKE(values[], 0)" : "DIM values[] AS INTEGER";
        Check(setup + $$"""

            TRY
                PRINT {{operation}}(values[])
            CATCH err
                PRINT err.Code
            END TRY
            PRINT ARRAYLENGTH(values[])
            PRINT ARRAYISALLOCATED(values[])
            """, [allocated ? "MB3007" : "MB3005", "0", allocated ? "TRUE" : "FALSE"]);
    }

    [Fact]
    public void LongExtremaKeepExactSumAndDocumentedDoubleAverage()
    {
        Check("DIM values[-2 TO -1] AS LONG\r\nvalues[-2] = -9223372036854775807 - CLNG(1)\r\nvalues[-1] = 9223372036854775807\r\nPRINT ARRAYSUM(values[])\r\nPRINT ARRAYMIN(values[])\r\nPRINT ARRAYMAX(values[])\r\nPRINT ARRAYAVERAGE(values[])\r\nPRINT values[-2]\r\nPRINT values[-1]", ["-1", "-9223372036854775808", "9223372036854775807", "0", "-9223372036854775808", "9223372036854775807"]);
    }

    [Fact]
    public void DecimalIntermediateOverflowCannotChangeSource()
    {
        Check("DIM high AS DECIMAL = CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591)\r\nDIM values[-2 TO 0] AS DECIMAL\r\nvalues[-2] = high\r\nvalues[-1] = CDEC(1)\r\nvalues[0] = -high\r\nTRY\r\n    PRINT ARRAYSUM(values[])\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT ARRAYMIN(values[])\r\nPRINT ARRAYMAX(values[])\r\nPRINT ARRAYAVERAGE(values[]) = VAL(\"0.3333333333333333\")\r\nPRINT values[-2] = high\r\nPRINT values[-1]\r\nPRINT values[0] = -high", ["MB3008", "-79228162514264337593543950335", "79228162514264337593543950335", "TRUE", "TRUE", "1", "TRUE"]);
    }

    [Fact]
    public void DoubleMatrixAverageCancelsExtremaAfterSumFailure()
    {
        Check("DIM high AS DOUBLE = VAL(\"1.7976931348623157e308\")\r\nDIM values[0 TO 1, -2 TO -1] AS DOUBLE\r\nvalues[0, -2] = high\r\nvalues[0, -1] = high\r\nvalues[1, -2] = -high\r\nvalues[1, -1] = -high\r\nTRY\r\n    PRINT ARRAYSUM(values[])\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT ARRAYAVERAGE(values[])\r\nPRINT ARRAYMIN(values[]) = -high\r\nPRINT ARRAYMAX(values[]) = high\r\nPRINT values[0, -2] = high\r\nPRINT values[0, -1] = high\r\nPRINT values[1, -2] = -high\r\nPRINT values[1, -1] = -high", ["MB3008", "0", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
