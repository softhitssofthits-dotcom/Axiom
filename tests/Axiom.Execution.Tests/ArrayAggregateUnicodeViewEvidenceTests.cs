namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateUnicodeViewEvidenceTests
{
    [Fact]
    public void StringExtremaCompareUtf16WithoutNormalizingOrRepairing()
    {
        Check("""
            DIM words(-1 TO 0) AS STRING
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            words(-1) = "é"
            words(0) = "é"
            PRINT ARRAYMIN(words) = "é"
            PRINT ARRAYMAX(words) = "é"
            words(-1) = "😀"
            words(0) = CHRW(57344)
            PRINT ARRAYMIN(words) = "😀"
            PRINT ARRAYMAX(words) = CHRW(57344)
            words(-1) = high
            words(0) = low
            PRINT ARRAYMIN(words) = high
            PRINT ARRAYMAX(words) = low
            words(-1) = high
            words(0) = "😀"
            PRINT ARRAYMIN(words) = high
            PRINT ARRAYMAX(words) = "😀"
            words(-1) = ""
            words(0) = CHRW(0)
            PRINT ARRAYMIN(words) = ""
            PRINT ARRAYMAX(words) = CHRW(0)
            words(-1) = "А"
            words(0) = "А" + CHRW(0)
            PRINT ARRAYMIN(words) = "А"
            PRINT ARRAYMAX(words) = "А" + CHRW(0)
            PRINT words(-1) = "А"
            PRINT words(0) = "А" + CHRW(0)
            """, Enumerable.Repeat("TRUE", 14).ToArray());
    }

    [Fact]
    public void NumericAggregatesOnlyReadVisibleRegion()
    {
        Check("""
            DIM grid(-1 TO 1, 5 TO 7) AS DECIMAL
            FOR r = -1 TO 1
                FOR c = 5 TO 7
                    grid(r, c) = CDEC(1000)
                NEXT c
            NEXT r
            grid(0, 6) = CDEC(1) / CDEC(4)
            grid(0, 7) = CDEC(3) / CDEC(4)
            grid(1, 6) = CDEC(5) / CDEC(4)
            grid(1, 7) = CDEC(7) / CDEC(4)
            DIM region(,) AS DECIMAL
            region = grid(0 TO 1, 6 TO 7)
            PRINT ARRAYSUM(region)
            PRINT ARRAYMIN(region)
            PRINT ARRAYMAX(region)
            PRINT ARRAYAVERAGE(region)
            PRINT ARRAYAVERAGE(grid(0)(6 TO 7))
            PRINT grid(-1, 5)
            PRINT grid(0, 6)
            PRINT grid(1, 7)
            """, ["4.00", "0.25", "1.75", "1", "0.5", "1000", "0.25", "1.75"]);
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
