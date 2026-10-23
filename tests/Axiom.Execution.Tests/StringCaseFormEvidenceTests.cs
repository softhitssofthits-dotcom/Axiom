using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class StringCaseFormEvidenceTests
{
    private const string Setup = "DIM oldItems[1 TO 2] AS STRING\r\nDIM newItems[1 TO 2] AS STRING\r\noldItems[1] = \"a\"\r\noldItems[2] = \"b\"\r\nnewItems[1] = \"x\"\r\nnewItems[2] = \"y\"";

    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "TrimWhitespace", "TRIM(\"  Я  \")", "Я", "STRING" };
        yield return new object[] { "TrimCharacters", "TRIM(\"абЯба\", \"аб\")", "Я", "STRING" };
        yield return new object[] { "LTrimWhitespace", "LTRIM(\"  Я  \")", "Я  ", "STRING" };
        yield return new object[] { "LTrimCharacters", "LTRIM(\"абЯба\", \"аб\")", "Яба", "STRING" };
        yield return new object[] { "RTrimWhitespace", "RTRIM(\"  Я  \")", "  Я", "STRING" };
        yield return new object[] { "RTrimCharacters", "RTRIM(\"абЯба\", \"аб\")", "абЯ", "STRING" };
        yield return new object[] { "LCase", "LCASE(\"ЯABC😀\")", "яabc😀", "STRING" };
        yield return new object[] { "UCase", "UCASE(\"яabc😀\")", "ЯABC😀", "STRING" };
        yield return new object[] { "Reverse", "REVERSE(\"А😀Б\")", "Б😀А", "STRING" };
        yield return new object[] { "Asc", "ASC(\"Я\")", "159", "INTEGER" };
        yield return new object[] { "AscW", "ASCW(\"Я\")", "1071", "INTEGER" };
        yield return new object[] { "Chr", "CHR(159)", "Я", "STRING" };
        yield return new object[] { "ChrW", "CHRW(128512)", "😀", "STRING" };
    }
    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "TrimWhitespace-arg1", "TRIM(TRUE)" };
        yield return new object[] { "TrimCharacters-arg1", "TRIM(TRUE, \"аб\")" };
        yield return new object[] { "TrimCharacters-arg2", "TRIM(\"абЯба\", TRUE)" };
        yield return new object[] { "LTrimWhitespace-arg1", "LTRIM(TRUE)" };
        yield return new object[] { "LTrimCharacters-arg1", "LTRIM(TRUE, \"аб\")" };
        yield return new object[] { "LTrimCharacters-arg2", "LTRIM(\"абЯба\", TRUE)" };
        yield return new object[] { "RTrimWhitespace-arg1", "RTRIM(TRUE)" };
        yield return new object[] { "RTrimCharacters-arg1", "RTRIM(TRUE, \"аб\")" };
        yield return new object[] { "RTrimCharacters-arg2", "RTRIM(\"абЯба\", TRUE)" };
        yield return new object[] { "LCase-arg1", "LCASE(TRUE)" };
        yield return new object[] { "UCase-arg1", "UCASE(TRUE)" };
        yield return new object[] { "Reverse-arg1", "REVERSE(TRUE)" };
        yield return new object[] { "Asc-arg1", "ASC(TRUE)" };
        yield return new object[] { "AscW-arg1", "ASCW(TRUE)" };
        yield return new object[] { "Chr-arg1", "CHR(TRUE)" };
        yield return new object[] { "ChrW-arg1", "CHRW(TRUE)" };
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void ExactRegisteredFormAndTypeProduceExpectedValue(string id, string call, string expected, string expectedType)
    {
        var source = Setup + "\nPRINT " + call;
        var syntax = Parser.Parse(source, out var parsingDiagnostics);
        Assert.Empty(parsingDiagnostics);
        var program = Binder.Bind(syntax, out var bindingDiagnostics);
        Assert.Empty(bindingDiagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last());
        var boundCall = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(boundCall.Target).Function.BuiltinId?.ToString());
        Assert.Equal(expectedType, boundCall.Type.Name.ToUpperInvariant());

        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { expected }, result.Output);
        }
    }

    [Theory]
    [MemberData(nameof(WrongArguments))]
    public void WrongTypesHaveMatchingDiagnostics(string id, string call)
    {
        var source = Setup + "\nPRINT " + call;
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded, id);
        Assert.NotEmpty(second.Diagnostics);
    }
}
