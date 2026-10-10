namespace Axiom.Execution.Tests;

public sealed class CharacterScalarBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (int Code, string Chr, string ChrW, int Asc, int Length)[]
        {
            (255, "\u00A0", "\u00FF", 255, 1),
            (256, "\u0100", "\u0100", 256, 1),
            (1040, "А", "А", 128, 1),
            (1105, "ё", "ё", 241, 1),
            (55295, "\uD7FF", "\uD7FF", 55295, 1),
            (57344, "\uE000", "\uE000", 57344, 1),
            (65535, "\uFFFF", "\uFFFF", 65535, 1),
            (65536, "\uD800\uDC00", "\uD800\uDC00", 65536, 2),
            (1114111, "\uDBFF\uDFFF", "\uDBFF\uDFFF", 1114111, 2)
        };

        foreach (var c in cases) yield return [c.Code, c.Chr, c.ChrW, c.Asc, c.Length];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ScalarBoundariesHaveLiteralCharactersAndCodes(int code, string chr, string chrW, int asc, int length)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            PRINT CHR({{code}}) = "{{chr}}"
            PRINT CHRW({{code}}) = "{{chrW}}"
            PRINT ASC("{{chr}}")
            PRINT ASCW("{{chrW}}")
            PRINT LEN(CHRW({{code}}))
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(new[] { "TRUE", "TRUE", asc.ToString(), code.ToString(), length.ToString() }, result.Output);
    }

    [Fact]
    public void FirstScalarIgnoresFollowingMalformedUnitsAndCombiningMarks()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            PRINT ASCW("А" + high)
            PRINT ASC("А" + low)
            PRINT ASCW("é")
            PRINT ASCW(high + "А")
            PRINT ASCW(low + high)
            PRINT ASC(high + low + "А")
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "1040", "128", "101", "65533", "65533", "128512" }, result.Output);
    }
}
