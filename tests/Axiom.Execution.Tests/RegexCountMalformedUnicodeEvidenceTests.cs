namespace Axiom.Execution.Tests;

public sealed class RegexCountMalformedUnicodeEvidenceTests
{
    [Fact]
    public void CountUsesUtf16MatchesWithoutNormalizationOrRepair()
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            PRINT REGEXCOUNT("😀😀", ".")
            PRINT REGEXCOUNT("😀😀", ".", "s")
            PRINT REGEXCOUNT(high + nul + low, ".")
            PRINT REGEXCOUNT(high + nul + low, "", "s")
            PRINT REGEXCOUNT(high + nul + low, "😀")
            PRINT REGEXCOUNT("😀😀", high)
            PRINT REGEXCOUNT("😀😀", low, "s")
            PRINT REGEXCOUNT(nul + "А" + nul, nul)
            PRINT REGEXCOUNT("éé", "é", "i")
            PRINT REGEXCOUNT("éé", "é")
            PRINT REGEXCOUNT("😀😀😀", "😀😀", "s")
            PRINT REGEXCOUNT("", "")
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "4", "4", "3", "4", "0", "2", "2", "2", "0", "0", "1", "1" }, run.Output);
    }
}
