namespace Axiom.Execution.Tests;

public sealed class StringEditMalformedUnicodeEvidenceTests
{
    [Fact]
    public void EditingUsesUtf16PositionsWithoutRepairingUnits()
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            PRINT INSERT("😀", 2, nul) = high + nul + low
            PRINT REMOVE("😀", 1, 1) = low
            PRINT REMOVE("😀", 2, 1) = high
            PRINT REMOVE(high + nul + low, 2, 1) = "😀"
            PRINT INSERT(high + low, 2, "") = "😀"
            PRINT INSERT("", 2147483647, low) = low
            PRINT INSERT(high, 2147483647, low) = "😀"
            PRINT REMOVE(high + nul + low, 2, 2147483647) = high
            PRINT REMOVE(high + nul + low, 4, 2147483647) = high + nul + low
            PRINT REMOVE(high + nul + low, 2147483647, 1) = high + nul + low
            PRINT INSERT("é", 2, nul) = "e" + nul + "́"
            PRINT REMOVE("é", 1, 1) = "́"
            PRINT REMOVE("é", 2, 1) = "e"
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(Enumerable.Repeat("TRUE", 13), result.Output);
    }
}
