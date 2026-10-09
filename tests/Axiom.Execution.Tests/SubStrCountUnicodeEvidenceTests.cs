namespace Axiom.Execution.Tests;

public sealed class SubStrCountUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CountsOrdinalNonOverlappingUtf16Fragments(ExecutionMode mode)
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            PRINT SUBSTRCOUNT("😀😀😀", "😀😀")
            PRINT SUBSTRCOUNT("😀😀😀", high)
            PRINT SUBSTRCOUNT("😀😀😀", low)
            PRINT SUBSTRCOUNT(high + high + low + low, "😀")
            PRINT SUBSTRCOUNT(high + nul + low, "😀")
            PRINT SUBSTRCOUNT(nul + nul + nul, nul + nul)
            PRINT SUBSTRCOUNT("А" + nul + "Б" + nul, nul)
            PRINT SUBSTRCOUNT("éé", "é")
            PRINT SUBSTRCOUNT("ééé", "éé")
            PRINT SUBSTRCOUNT("éé", "́")
            PRINT SUBSTRCOUNT("", low)
            PRINT SUBSTRCOUNT(low, "😀")
            TRY
                PRINT SUBSTRCOUNT(high + nul + low, "")
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Продолжение"
            END TRY
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(new[] { "1", "3", "3", "1", "0", "1", "2", "0", "1", "2", "0", "0", "MB3007", "Продолжение" }, result.Output);
    }
}
