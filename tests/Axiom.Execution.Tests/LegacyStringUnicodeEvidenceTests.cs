namespace Axiom.Execution.Tests;

public sealed class LegacyStringUnicodeEvidenceTests
{
    [Fact]
    public void DefaultSlicesAndPredicatesKeepLiteralUtf16AndNullUnits()
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM text AS STRING = high + CHRW(0) + low + "é"
            PRINT LEN(text)
            PRINT LEFT(text, 2147483647) = text
            PRINT RIGHT(text, 2147483647) = text
            PRINT MID(text, 2, 2147483647) = CHRW(0) + low + "é"
            PRINT MID(text, 2147483647, 2147483647) = ""
            PRINT CONTAINS(text, "😀")
            PRINT CONTAINS(text, CHRW(0))
            PRINT CONTAINS(text, high + CHRW(0))
            PRINT STARTSWITH(text, high)
            PRINT STARTSWITH(text, "😀")
            PRINT ENDSWITH(text, "é")
            PRINT ENDSWITH(text, "é")
            PRINT STARTSWITH(text, "")
            PRINT ENDSWITH(text, "")
            """));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "5", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE", "TRUE" }, run.Output);
    }
}
