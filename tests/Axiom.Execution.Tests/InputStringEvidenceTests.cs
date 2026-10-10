using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class InputStringEvidenceTests
{
    public static IEnumerable<object[]> SingleValues()
    {

        foreach (var text in new[] { "", " ", "  текст  ", "а,б", ",", "а,,б,", "\"а,б\"", "\"\"", "\"незакрытая", "\tА😀\t", "\u00a0текст\u00a0", "А，Б" })
            yield return [text];
    }

    [Theory]
    [MemberData(nameof(SingleValues))]
    public void SingleStringPreservesEntireProviderLine(string text)
    {
        var provider = new OneInput(text);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("DIM value AS STRING = \"исходное\"\nINPUT value\nPRINT value", inputProvider: provider));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(new[] { text }, result.Output);
    }

    public static IEnumerable<object[]> MultipleValues()
    {
        var cases = new (string Text, string[] Expected)[]
        {
            ("  А  ,  Б  ", ["А", "Б"]),
            (",", ["", ""]),
            ("\"А\",\"Б\"", ["\"А\"", "\"Б\""]),
            ("\u00a0А\u00a0,\tБ\t", ["А", "Б"]),
            ("\"А,Б\",В", ["MB3003", "исходное А", "исходное Б"])
        };

        foreach (var item in cases) yield return [item.Text, item.Expected];
    }

    [Theory]
    [MemberData(nameof(MultipleValues))]
    public void MultipleStringsUsePlainCommaFieldsAndKeepStateOnCountFailure(string text, string[] expected)
    {
        var provider = new OneInput(text);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM first AS STRING = "исходное А"
            DIM second AS STRING = "исходное Б"
            TRY
                INPUT first, second
            CATCH err
                PRINT err.Code
            END TRY
            PRINT first
            PRINT second
            """, inputProvider: provider));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(expected, result.Output);
    }

    private sealed class OneInput(string text) : IInputProvider
    {
        public int Calls { get; private set; }
        public string ReadInput(InputRequest request) { Calls++; Assert.Equal(1, Calls); return text; }
    }
}
