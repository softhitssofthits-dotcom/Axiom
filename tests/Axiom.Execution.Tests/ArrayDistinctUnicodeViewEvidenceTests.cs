namespace Axiom.Execution.Tests;

public sealed class ArrayDistinctUnicodeViewEvidenceTests
{
    [Fact]
    public void DistinctPreservesMalformedUnitsAndVisibleFirstOccurrenceOrder()
    {
        var source = "DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM result[] AS STRING\r\nDIM high AS STRING = LEFT(\"😀\", 1)\r\nDIM low AS STRING = RIGHT(\"😀\", 1)\r\ngrid[-1, 5] = \"Внешнее\"\r\ngrid[0, 5] = high\r\ngrid[0, 6] = high + CHRW(0)\r\ngrid[0, 7] = \"😀\"\r\ngrid[1, 5] = high\r\ngrid[1, 6] = low\r\ngrid[1, 7] = \"\"\r\nresult[] = ARRAYDISTINCT(grid[0 TO 1, 5 TO 7])\r\nPRINT ARRAYRANK(result[])\r\nPRINT LBOUND(result[])\r\nPRINT UBOUND(result[])\r\nPRINT result[1] = high\r\nPRINT result[2] = high + CHRW(0)\r\nPRINT result[3] = \"😀\"\r\nPRINT result[4] = low\r\nPRINT result[5] = \"\"\r\nresult[1] = \"Замена\"\r\nPRINT grid[0, 5] = high\r\nPRINT grid[1, 5] = high\r\nPRINT grid[-1, 5] = \"Внешнее\"\r\nDIM amounts[4 TO 6] AS DECIMAL\r\nDIM uniqueAmounts[] AS DECIMAL\r\namounts[4] = 79228162514264337593543950335.0\r\namounts[5] = 79228162514264337593543950334.0\r\namounts[6] = 79228162514264337593543950335.0\r\nuniqueAmounts[] = ARRAYDISTINCT(amounts[])\r\nPRINT ARRAYLENGTH(uniqueAmounts[])\r\nPRINT uniqueAmounts[1]\r\nPRINT uniqueAmounts[2]";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "1", "1", "5", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "2", "79228162514264337593543950335", "79228162514264337593543950334" }, run.Output);
    }
}
