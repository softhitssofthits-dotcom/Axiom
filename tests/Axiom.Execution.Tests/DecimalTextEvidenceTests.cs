using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class DecimalTextEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var text in new[] { "9007199254740993", "1.0000000000000000000000000001", "0.1234567890123456789012345678", "79228162514264337593543950335" }) yield return [text];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TypedInputAvoidsDoubleButValThenCdecDoesNot(string text)
    {
        var provider = new OneInput(text);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            DIM value AS DECIMAL
            INPUT value
            PRINT value
            TRY
                PRINT value = CDEC(VAL("{{text}}"))
            CATCH err
                PRINT err.Code
            END TRY
            """, inputProvider: provider));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(new[] { text, text == "79228162514264337593543950335" ? "MB3008" : "FALSE" }, result.Output);
    }

    [Fact]
    public void CdecDoesNotCurrentlyAcceptStoredString()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM text AS STRING = "9007199254740993"
            PRINT CDEC(text)
            """));
        Assert.False(result.Succeeded);
        Assert.Empty(result.Output);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.StartsWith("MB2", diagnostic.Code));
    }

    private sealed class OneInput(string text) : IInputProvider
    {
        public int Calls { get; private set; }
        public string ReadInput(InputRequest request) { Calls++; Assert.Equal(1, Calls); return text; }
    }
}
