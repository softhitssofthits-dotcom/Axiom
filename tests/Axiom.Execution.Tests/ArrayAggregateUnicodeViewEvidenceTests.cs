namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateUnicodeViewEvidenceTests
{
    [Fact]
    public void StringExtremaCompareUtf16WithoutNormalizingOrRepairing()
    {
        Check("DIM words[-1 TO 0] AS STRING\r\nDIM high AS STRING = LEFT(\"😀\", 1)\r\nDIM low AS STRING = RIGHT(\"😀\", 1)\r\nwords[-1] = \"é\"\r\nwords[0] = \"é\"\r\nPRINT ARRAYMIN(words[]) = \"é\"\r\nPRINT ARRAYMAX(words[]) = \"é\"\r\nwords[-1] = \"😀\"\r\nwords[0] = CHRW(57344)\r\nPRINT ARRAYMIN(words[]) = \"😀\"\r\nPRINT ARRAYMAX(words[]) = CHRW(57344)\r\nwords[-1] = high\r\nwords[0] = low\r\nPRINT ARRAYMIN(words[]) = high\r\nPRINT ARRAYMAX(words[]) = low\r\nwords[-1] = high\r\nwords[0] = \"😀\"\r\nPRINT ARRAYMIN(words[]) = high\r\nPRINT ARRAYMAX(words[]) = \"😀\"\r\nwords[-1] = \"\"\r\nwords[0] = CHRW(0)\r\nPRINT ARRAYMIN(words[]) = \"\"\r\nPRINT ARRAYMAX(words[]) = CHRW(0)\r\nwords[-1] = \"А\"\r\nwords[0] = \"А\" + CHRW(0)\r\nPRINT ARRAYMIN(words[]) = \"А\"\r\nPRINT ARRAYMAX(words[]) = \"А\" + CHRW(0)\r\nPRINT words[-1] = \"А\"\r\nPRINT words[0] = \"А\" + CHRW(0)", Enumerable.Repeat("TRUE", 14).ToArray());
    }

    [Fact]
    public void NumericAggregatesOnlyReadVisibleRegion()
    {
        Check("DIM grid[-1 TO 1, 5 TO 7] AS DECIMAL\r\nFOR r = -1 TO 1\r\n    FOR c = 5 TO 7\r\n        grid[r, c] = CDEC(1000)\r\n    NEXT c\r\nNEXT r\r\ngrid[0, 6] = CDEC(1) / CDEC(4)\r\ngrid[0, 7] = CDEC(3) / CDEC(4)\r\ngrid[1, 6] = CDEC(5) / CDEC(4)\r\ngrid[1, 7] = CDEC(7) / CDEC(4)\r\nDIM region[,] AS DECIMAL\r\nregion[] = grid[0 TO 1, 6 TO 7]\r\nPRINT ARRAYSUM(region[])\r\nPRINT ARRAYMIN(region[])\r\nPRINT ARRAYMAX(region[])\r\nPRINT ARRAYAVERAGE(region[])\r\nPRINT ARRAYAVERAGE(grid[0][6 TO 7])\r\nPRINT grid[-1, 5]\r\nPRINT grid[0, 6]\r\nPRINT grid[1, 7]", ["4.00", "0.25", "1.75", "1", "0.5", "1000", "0.25", "1.75"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Status == ExecutionStatus.Success, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
