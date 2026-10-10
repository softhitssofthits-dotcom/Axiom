using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class StringBuildFormEvidenceTests
{
    private const string Setup = """
DIM items(-2 TO 0) AS STRING
items(-2) = "А"
items(-1) = "😀"
items(0) = "Б"
""";

    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "Insert", "INSERT(\"АБ\", 2, \"😀\")", "А😀Б", "STRING" };
        yield return new object[] { "Remove", "REMOVE(\"А😀Б\", 2, 2)", "АБ", "STRING" };
        yield return new object[] { "Repeat", "REPEAT(\"я😀\", 2)", "я😀я😀", "STRING" };
        yield return new object[] { "Space", "SPACE(2)", "  ", "STRING" };
        yield return new object[] { "StringFill", "STRING(3, \"яб\")", "яяя", "STRING" };
        yield return new object[] { "PadLeft", "PADLEFT(\"я\", 3)", "  я", "STRING" };
        yield return new object[] { "PadLeftFill", "PADLEFT(\"я\", 3, \".\")", "..я", "STRING" };
        yield return new object[] { "PadRight", "PADRIGHT(\"я\", 3)", "я  ", "STRING" };
        yield return new object[] { "PadRightFill", "PADRIGHT(\"я\", 3, \".\")", "я..", "STRING" };
        yield return new object[] { "Center", "CENTER(\"я\", 4)", " я  ", "STRING" };
        yield return new object[] { "CenterFill", "CENTER(\"я\", 4, \".\")", ".я..", "STRING" };
        yield return new object[] { "Split", "SPLIT(\"А,😀,Б\")", "А|😀|Б", "STRING()" };
        yield return new object[] { "SplitSeparator", "SPLIT(\"А😀Б😀\", \"😀\")", "А|Б|", "STRING()" };
        yield return new object[] { "Join", "JOIN(items)", "А,😀,Б", "STRING" };
        yield return new object[] { "JoinSeparator", "JOIN(items, \"|\")", "А|😀|Б", "STRING" };
    }
    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "Insert-arg1", "INSERT(TRUE, 2, \"😀\")" };
        yield return new object[] { "Insert-arg2", "INSERT(\"АБ\", TRUE, \"😀\")" };
        yield return new object[] { "Insert-arg3", "INSERT(\"АБ\", 2, TRUE)" };
        yield return new object[] { "Remove-arg1", "REMOVE(TRUE, 2, 2)" };
        yield return new object[] { "Remove-arg2", "REMOVE(\"А😀Б\", TRUE, 2)" };
        yield return new object[] { "Remove-arg3", "REMOVE(\"А😀Б\", 2, TRUE)" };
        yield return new object[] { "Repeat-arg1", "REPEAT(TRUE, 2)" };
        yield return new object[] { "Repeat-arg2", "REPEAT(\"я😀\", TRUE)" };
        yield return new object[] { "Space-arg1", "SPACE(TRUE)" };
        yield return new object[] { "StringFill-arg1", "STRING(TRUE, \"яб\")" };
        yield return new object[] { "StringFill-arg2", "STRING(3, TRUE)" };
        yield return new object[] { "PadLeft-arg1", "PADLEFT(TRUE, 3)" };
        yield return new object[] { "PadLeft-arg2", "PADLEFT(\"я\", TRUE)" };
        yield return new object[] { "PadLeftFill-arg1", "PADLEFT(TRUE, 3, \".\")" };
        yield return new object[] { "PadLeftFill-arg2", "PADLEFT(\"я\", TRUE, \".\")" };
        yield return new object[] { "PadLeftFill-arg3", "PADLEFT(\"я\", 3, TRUE)" };
        yield return new object[] { "PadRight-arg1", "PADRIGHT(TRUE, 3)" };
        yield return new object[] { "PadRight-arg2", "PADRIGHT(\"я\", TRUE)" };
        yield return new object[] { "PadRightFill-arg1", "PADRIGHT(TRUE, 3, \".\")" };
        yield return new object[] { "PadRightFill-arg2", "PADRIGHT(\"я\", TRUE, \".\")" };
        yield return new object[] { "PadRightFill-arg3", "PADRIGHT(\"я\", 3, TRUE)" };
        yield return new object[] { "Center-arg1", "CENTER(TRUE, 4)" };
        yield return new object[] { "Center-arg2", "CENTER(\"я\", TRUE)" };
        yield return new object[] { "CenterFill-arg1", "CENTER(TRUE, 4, \".\")" };
        yield return new object[] { "CenterFill-arg2", "CENTER(\"я\", TRUE, \".\")" };
        yield return new object[] { "CenterFill-arg3", "CENTER(\"я\", 4, TRUE)" };
        yield return new object[] { "Split-arg1", "SPLIT(TRUE)" };
        yield return new object[] { "SplitSeparator-arg1", "SPLIT(TRUE, \"😀\")" };
        yield return new object[] { "SplitSeparator-arg2", "SPLIT(\"А😀Б😀\", TRUE)" };
        yield return new object[] { "Join-arg1", "JOIN(TRUE)" };
        yield return new object[] { "JoinSeparator-arg1", "JOIN(TRUE, \"|\")" };
        yield return new object[] { "JoinSeparator-arg2", "JOIN(items, TRUE)" };
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
            var executionSource = expectedType == "STRING()" ? Setup + "\nPRINT JOIN(" + call + ", \"|\")" : source;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(executionSource));
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
