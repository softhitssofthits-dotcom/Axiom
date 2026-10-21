namespace Axiom.Execution.Tests;

public sealed class UtcStructuralEqualityTests
{
    [Fact]
    public void NestedRecordsCompareUtcFieldsAtFullPrecision()
    {
        const string source = """
TYPE Stamp
    Instant AS UTCDATETIME
END TYPE
TYPE Entry
    Label AS STRING
    Value AS Stamp
END TYPE
DIM first AS Entry
DIM second AS Entry
first.Label = "same"
second.Label = "same"
first.Value.Instant = "2024-02-29T12:34:56.0000001Z"
second.Value.Instant = "2024-02-29T12:34:56.0000001Z"
PRINT first = second
PRINT first <> second
second.Value.Instant = "2024-02-29T12:34:56.0000002Z"
PRINT first = second
PRINT first <> second
second = first
PRINT first = second
second.Label = "different"
PRINT first <> second
""";
        Check(source, ["TRUE", "FALSE", "FALSE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void UtcDistinctKeepsFirstOccurrencesAndDoesNotChangeSource()
    {
        const string source = """
DIM values(-2 TO 1) AS UTCDATETIME
values(-2) = "2024-02-29T12:34:56.0000002Z"
values(-1) = "2024-02-29T12:34:56.0000001Z"
values(0) = values(-2)
values(1) = "2024-02-29T12:34:56.0000003Z"
DIM result() AS UTCDATETIME
result = ARRAYDISTINCT(values)
PRINT LBOUND(result)
PRINT UBOUND(result)
PRINT result(1)
PRINT result(2)
PRINT result(3)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(0)
""";
        Check(source, ["1", "3", "2024-02-29T12:34:56.0000002Z", "2024-02-29T12:34:56.0000001Z", "2024-02-29T12:34:56.0000003Z", "-2", "1", "2024-02-29T12:34:56.0000002Z"]);
    }

    [Fact]
    public void UtcDistinctHandlesEmptyAndAllEqualArrays()
    {
        const string source = """
DIM values(5 TO 7) AS UTCDATETIME
DIM empty() AS UTCDATETIME
DIM result() AS UTCDATETIME
empty = ARRAYTAKE(values, 0)
result = ARRAYDISTINCT(empty)
PRINT ARRAYISALLOCATED(result)
PRINT LBOUND(result)
PRINT UBOUND(result)
PRINT ARRAYLENGTH(result)
result = ARRAYDISTINCT(values)
PRINT ARRAYLENGTH(result)
PRINT result(1)
""";
        Check(source, ["TRUE", "1", "0", "0", "1", "0001-01-01T00:00:00Z"]);
    }

    [Fact]
    public void LargeUniqueUtcArrayRemainsComplete()
    {
        const string source = """
DIM values(1 TO 8000) AS UTCDATETIME
DIM origin AS DATETIME = "2024-01-01 00:00:00"
FOR i = 1 TO 8000
values(i) = TOINSTANT(DATEADD("s", i, origin), "UTC")
NEXT
DIM result() AS UTCDATETIME
result = ARRAYDISTINCT(values)
PRINT ARRAYLENGTH(result)
PRINT result(1)
PRINT result(8000)
""";
        Check(source, ["8000", "2024-01-01T00:00:01Z", "2024-01-01T02:13:20Z"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
