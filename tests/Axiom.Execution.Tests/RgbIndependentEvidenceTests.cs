namespace Axiom.Execution.Tests;

public sealed class RgbIndependentEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Arguments, string Expected)[]
        {
            ("0, 0, 0", "0"), ("255, 255, 255", "16777215"),
            ("255, 0, 0", "16711680"), ("0, 255, 0", "65280"),
            ("0, 0, 255", "255"), ("1, 2, 3", "66051"),
            ("-1, 0, 0", "MB3007"), ("0, -1, 0", "MB3007"),
            ("0, 0, -1", "MB3007"), ("256, 0, 0", "MB3007"),
            ("0, 256, 0", "MB3007"), ("0, 0, 256", "MB3007"),
            ("9223372036854775807, 0, 0", "MB3007"),
            ("0, 9223372036854775807, 0", "MB3007"),
            ("0, 0, 9223372036854775807", "MB3007")
        };

        foreach (var (arguments, expected) in cases) yield return [arguments, expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void PackedValueAndEachComponentBoundaryWorkWithoutScreen(string arguments, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT RGB({{arguments}})
            CATCH err
                PRINT err.Code
            END TRY
            PRINT "Продолжение"
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { expected, "Продолжение" }, result.Output);
    }

    [Theory]
    [InlineData("RGB(1, 2)")]
    [InlineData("RGB(1, 2, 3, 4)")]
    [InlineData("RGB(1.5, 2, 3)")]
    [InlineData("RGB(\"1\", 2, 3)")]
    public void InvalidFormsAreRejectedBeforeExecution(string call)
    {

        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call));
            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
            Assert.All(result.Diagnostics, diagnostic => Assert.StartsWith("MB2", diagnostic.Code));
            Assert.Empty(result.Output);
        }
    }
}
