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
        const string source = "DIM values[-2 TO 1] AS UTCDATETIME\r\nvalues[-2] = \"2024-02-29T12:34:56.0000002Z\"\r\nvalues[-1] = \"2024-02-29T12:34:56.0000001Z\"\r\nvalues[0] = values[-2]\r\nvalues[1] = \"2024-02-29T12:34:56.0000003Z\"\r\nDIM result[] AS UTCDATETIME\r\nresult[] = ARRAYDISTINCT(values[])\r\nPRINT LBOUND(result[])\r\nPRINT UBOUND(result[])\r\nPRINT result[1]\r\nPRINT result[2]\r\nPRINT result[3]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT values[0]";
        Check(source, ["1", "3", "2024-02-29T12:34:56.0000002Z", "2024-02-29T12:34:56.0000001Z", "2024-02-29T12:34:56.0000003Z", "-2", "1", "2024-02-29T12:34:56.0000002Z"]);
    }

    [Fact]
    public void UtcDistinctHandlesEmptyAndAllEqualArrays()
    {
        const string source = "DIM values[5 TO 7] AS UTCDATETIME\r\nDIM empty[] AS UTCDATETIME\r\nDIM result[] AS UTCDATETIME\r\nempty[] = ARRAYTAKE(values[], 0)\r\nresult[] = ARRAYDISTINCT(empty[])\r\nPRINT ARRAYISALLOCATED(result[])\r\nPRINT LBOUND(result[])\r\nPRINT UBOUND(result[])\r\nPRINT ARRAYLENGTH(result[])\r\nresult[] = ARRAYDISTINCT(values[])\r\nPRINT ARRAYLENGTH(result[])\r\nPRINT result[1]";
        Check(source, ["TRUE", "1", "0", "0", "1", "0001-01-01T00:00:00Z"]);
    }

    [Fact]
    public void LargeUniqueUtcArrayRemainsComplete()
    {
        const string source = "DIM values[1 TO 8000] AS UTCDATETIME\r\nDIM origin AS DATETIME = \"2024-01-01 00:00:00\"\r\nFOR i = 1 TO 8000\r\nvalues[i] = TOINSTANT(DATEADD(\"s\", i, origin), \"UTC\")\r\nNEXT\r\nDIM result[] AS UTCDATETIME\r\nresult[] = ARRAYDISTINCT(values[])\r\nPRINT ARRAYLENGTH(result[])\r\nPRINT result[1]\r\nPRINT result[8000]";
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
