namespace Axiom.Execution.Tests;

public sealed class ArrayWindowUnicodeCountEvidenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtremeCountsDoNotAllocateBeyondSourceOrChangeIt(bool explicitAxis)
    {
        var axis = explicitAxis ? ", 1" : "";
        var source = """
            DIM words(2147483645 TO 2147483647) AS STRING
            DIM result() AS STRING
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0)
            words(2147483645) = token
            words(2147483646) = "é"
            words(2147483647) = "😀"
            """ + $"\nresult = ARRAYTAKE(words, 2147483647{axis})\n" + """
            PRINT ARRAYLENGTH(result)
            PRINT LBOUND(result)
            PRINT UBOUND(result)
            PRINT result(1) = token
            PRINT result(2) = "é"
            PRINT result(3) = "😀"
            result(1) = "Замена"
            PRINT words(2147483645) = token
            """ + $"\nresult = ARRAYSKIP(words, 2147483647{axis})\n" + """
            PRINT ARRAYISALLOCATED(result)
            PRINT ARRAYLENGTH(result)
            PRINT LBOUND(result)
            PRINT UBOUND(result)
            """ + $"\nresult = ARRAYSLICE(words, 2147483647, 1{axis})\n" + """
            PRINT result(1) = "😀"
            TRY
            """ + $"\nresult = ARRAYSLICE(words, 2147483647, 2147483647{axis})\n" + """
            CATCH err
                PRINT err.Code
            END TRY
            PRINT result(1) = "😀"
            PRINT words(2147483647) = "😀"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "3", "1", "3", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "0", "1", "0", "TRUE", "MB3007", "TRUE", "TRUE" }, run.Output);
    }
}
