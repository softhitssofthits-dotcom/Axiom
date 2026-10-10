namespace Axiom.Execution.Tests;

public sealed class ArrayMutationUnicodeEvidenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneDimensionalMutationPreservesWholeUnicodeValues(bool explicitAxis)
    {
        var axis = explicitAxis ? ", 1" : "";
        var source = """
            DIM words() AS STRING
            REDIM words(-2 TO -1)
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0)
            words(-2) = "é"
            words(-1) = "😀"
            """ + $"\nARRAYPUSH(words, token{axis})\nARRAYINSERT(words, -1, \"é\"{axis})\n" + """
            PRINT LBOUND(words)
            PRINT UBOUND(words)
            PRINT words(-2) = "é"
            PRINT words(-1) = "é"
            PRINT words(0) = "😀"
            PRINT words(1) = token
            """ + $"\nPRINT ARRAYREMOVE(words, -1{axis}) = \"é\"\nPRINT ARRAYPOP(words{axis}) = token\n" + """
            PRINT LBOUND(words)
            PRINT UBOUND(words)
            PRINT words(-2) = "é"
            PRINT words(-1) = "😀"
            TRY
            """ + $"\nPRINT ARRAYREMOVE(words, 2147483647{axis})\n" + """
            CATCH err
                PRINT err.Code
            END TRY
            PRINT words(-1) = "😀"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "-2", "1", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "-2", "-1", "TRUE", "TRUE", "MB3007", "TRUE" }, run.Output);
    }
}
