namespace Axiom.Execution.Tests;

public sealed class StringFillMalformedUnicodeEvidenceTests
{
    [Fact]
    public void RepetitionAndPaddingKeepExactUtf16Units()
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            DIM original AS STRING = high + "А" + nul + low
            PRINT REPEAT(original, 2) = original + original
            PRINT REPEAT(original, 1) = original
            PRINT REPEAT(original, 0) = ""
            PRINT SPACE(3) = "   "
            PRINT STRING(3, low + "А") = low + low + low
            PRINT STRING(2, nul + "А") = nul + nul
            PRINT PADLEFT(original, 6) = "  " + original
            PRINT PADRIGHT(original, 6) = original + "  "
            PRINT CENTER(original, 7) = " " + original + "  "
            PRINT PADLEFT(original, 6, low + "А") = low + low + original
            PRINT PADRIGHT(original, 6, nul + "А") = original + nul + nul
            PRINT CENTER(original, 7, high + "А") = high + original + high + high
            PRINT PADLEFT(original, 0, low) = original
            PRINT PADRIGHT(original, 4, high) = original
            PRINT CENTER(original, 3, nul) = original
            PRINT PADLEFT("", 2, low) = low + low
            PRINT PADRIGHT("", 2, nul) = nul + nul
            PRINT CENTER("", 3, high) = high + high + high
            PRINT original = high + "А" + nul + low
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(Enumerable.Repeat("TRUE", 19), result.Output);
    }
}
