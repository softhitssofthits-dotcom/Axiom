namespace Axiom.Execution.Tests;

public sealed class ArrayConcatUnicodeViewEvidenceTests
{
    [Fact]
    public void ConcatenationKeepsWholeValuesAndIndependentVisibleCopies()
    {
        var source = "DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM flat[] AS STRING\r\nDIM merged[,] AS STRING\r\nDIM token AS STRING = LEFT(\"😀\", 1) + CHRW(0)\r\ngrid[-1, 5] = \"Внешний\"\r\ngrid[0, 6] = token\r\ngrid[0, 7] = \"é\"\r\ngrid[1, 6] = \"😀\"\r\ngrid[1, 7] = \"é\"\r\nflat[] = ARRAYCONCAT(grid[0][6 TO 7], grid[1][6 TO 7])\r\nPRINT LBOUND(flat[])\r\nPRINT UBOUND(flat[])\r\nPRINT flat[1] = token\r\nPRINT flat[2] = \"é\"\r\nPRINT flat[3] = \"😀\"\r\nPRINT flat[4] = \"é\"\r\nmerged[] = ARRAYCONCAT(grid[0 TO 1, 6 TO 7], grid[0 TO 1, 6 TO 7], 2)\r\nPRINT LBOUND(merged[], 1)\r\nPRINT UBOUND(merged[], 1)\r\nPRINT LBOUND(merged[], 2)\r\nPRINT UBOUND(merged[], 2)\r\nPRINT merged[0, 6] = token\r\nPRINT merged[0, 8] = token\r\nPRINT merged[1, 7] = \"é\"\r\nPRINT merged[1, 9] = \"é\"\r\nflat[1] = \"Замена\"\r\nmerged[0, 6] = \"Другая замена\"\r\nPRINT grid[0, 6] = token\r\nPRINT merged[0, 8] = token\r\nPRINT grid[-1, 5] = \"Внешний\"";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "1", "4", "TRUE", "TRUE", "TRUE", "TRUE", "0", "1", "6", "9", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE" }, run.Output);
    }
}
