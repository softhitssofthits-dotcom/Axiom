namespace Axiom.Execution.Tests;

public sealed class TrimUnicodeBoundaryEvidenceTests
{
    public static IEnumerable<object[]> WhitespaceCases()
    {
        int[] whitespace = [9, 10, 11, 12, 13, 32, 133, 160, 5760, 8192, 8193, 8194, 8195, 8196, 8197, 8198, 8199, 8200, 8201, 8202, 8232, 8233, 8239, 8287, 12288];
        int[] retained = [0, 6158, 8203, 8288, 65279];

        {
            foreach (var code in whitespace) yield return [code, true];
            foreach (var code in retained) yield return [code, false];
        }
    }

    [Theory]
    [MemberData(nameof(WhitespaceCases))]
    public void UnicodeWhitespaceHasLiteralMembershipEvidence(int code, bool removed)
    {
        var expectedTrim = removed ? "\"А\"" : "text";
        var expectedLeft = removed ? "\"А\" + edge" : "text";
        var expectedRight = removed ? "edge + \"А\"" : "text";
        Check($$"""
            DIM edge AS STRING = CHRW({{code}})
            DIM text AS STRING = edge + "А" + edge
            PRINT TRIM(text) = {{expectedTrim}}
            PRINT LTRIM(text) = {{expectedLeft}}
            PRINT RTRIM(text) = {{expectedRight}}
            PRINT TRIM("А" + edge + "Б") = "А" + edge + "Б"
            """, ["TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void ExplicitSetsTrimUtf16UnitsWithoutReplacingMalformedResult()
    {
        Check("""
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            PRINT TRIM("😀А😀", high) = low + "А😀"
            PRINT LTRIM("😀А😀", high) = low + "А😀"
            PRINT RTRIM("😀А😀", low) = "😀А" + high
            PRINT TRIM("😀А😀", low) = "😀А" + high
            PRINT TRIM("😀А😀", high + low) = "А"
            PRINT TRIM(high + low, high + low) = ""
            PRINT TRIM("😀А😀", "") = "😀А😀"
            PRINT LTRIM("", high) = ""
            PRINT RTRIM("", low) = ""
            PRINT TRIM("А" + CHRW(0), CHRW(0)) = "А"
            PRINT TRIM("е́", "́") = "е"
            PRINT TRIM("е́", "е") = "́"
            """, Enumerable.Repeat("TRUE", 12).ToArray());
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
