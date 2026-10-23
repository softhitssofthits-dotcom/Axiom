using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class StringSearchFormEvidenceTests
{
    private const string Setup = "DIM oldItems[1 TO 2] AS STRING\r\nDIM newItems[1 TO 2] AS STRING\r\noldItems[1] = \"a\"\r\noldItems[2] = \"b\"\r\nnewItems[1] = \"x\"\r\nnewItems[2] = \"y\"";

    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "Len", "LEN(\"А😀Б\")", "4", "INTEGER" };
        yield return new object[] { "Left", "LEFT(\"А😀Б\", 3)", "А😀", "STRING" };
        yield return new object[] { "Right", "RIGHT(\"А😀Б\", 3)", "😀Б", "STRING" };
        yield return new object[] { "Mid", "MID(\"А😀Б\", 2, 2)", "😀", "STRING" };
        yield return new object[] { "Contains", "CONTAINS(\"А😀Б\", \"😀\")", "TRUE", "BOOLEAN" };
        yield return new object[] { "StartsWith", "STARTSWITH(\"А😀Б\", \"А😀\")", "TRUE", "BOOLEAN" };
        yield return new object[] { "EndsWith", "ENDSWITH(\"А😀Б\", \"😀Б\")", "TRUE", "BOOLEAN" };
        yield return new object[] { "Replace", "REPLACE(\"А😀Б\", \"😀\", \"я\")", "АяБ", "STRING" };
        yield return new object[] { "ReplaceMany", "REPLACEMANY(\"ab\", oldItems[], newItems[])", "xy", "STRING" };
        yield return new object[] { "InStr", "INSTR(\"А😀Б😀\", \"😀\")", "2", "INTEGER" };
        yield return new object[] { "InStrFrom", "INSTR(3, \"А😀Б😀\", \"😀\")", "5", "INTEGER" };
        yield return new object[] { "InStrRev", "INSTRREV(\"А😀Б😀\", \"😀\")", "5", "INTEGER" };
        yield return new object[] { "InStrRevFrom", "INSTRREV(3, \"А😀Б😀\", \"😀\")", "2", "INTEGER" };
        yield return new object[] { "SubStrCount", "SUBSTRCOUNT(\"А😀Б😀\", \"😀\")", "2", "INTEGER" };
    }

    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "Len-arg1", "LEN(TRUE)" };
        yield return new object[] { "Left-arg1", "LEFT(TRUE, 3)" };
        yield return new object[] { "Left-arg2", "LEFT(\"А😀Б\", TRUE)" };
        yield return new object[] { "Right-arg1", "RIGHT(TRUE, 3)" };
        yield return new object[] { "Right-arg2", "RIGHT(\"А😀Б\", TRUE)" };
        yield return new object[] { "Mid-arg1", "MID(TRUE, 2, 2)" };
        yield return new object[] { "Mid-arg2", "MID(\"А😀Б\", TRUE, 2)" };
        yield return new object[] { "Mid-arg3", "MID(\"А😀Б\", 2, TRUE)" };
        yield return new object[] { "Contains-arg1", "CONTAINS(TRUE, \"😀\")" };
        yield return new object[] { "Contains-arg2", "CONTAINS(\"А😀Б\", TRUE)" };
        yield return new object[] { "StartsWith-arg1", "STARTSWITH(TRUE, \"А😀\")" };
        yield return new object[] { "StartsWith-arg2", "STARTSWITH(\"А😀Б\", TRUE)" };
        yield return new object[] { "EndsWith-arg1", "ENDSWITH(TRUE, \"😀Б\")" };
        yield return new object[] { "EndsWith-arg2", "ENDSWITH(\"А😀Б\", TRUE)" };
        yield return new object[] { "Replace-arg1", "REPLACE(TRUE, \"😀\", \"я\")" };
        yield return new object[] { "Replace-arg2", "REPLACE(\"А😀Б\", TRUE, \"я\")" };
        yield return new object[] { "Replace-arg3", "REPLACE(\"А😀Б\", \"😀\", TRUE)" };
        yield return new object[] { "ReplaceMany-arg1", "REPLACEMANY(TRUE, oldItems[], newItems[])" };
        yield return new object[] { "ReplaceMany-arg2", "REPLACEMANY(\"ab\", TRUE, newItems[])" };
        yield return new object[] { "ReplaceMany-arg3", "REPLACEMANY(\"ab\", oldItems[], TRUE)" };
        yield return new object[] { "InStr-arg1", "INSTR(TRUE, \"😀\")" };
        yield return new object[] { "InStr-arg2", "INSTR(\"А😀Б😀\", TRUE)" };
        yield return new object[] { "InStrFrom-arg1", "INSTR(TRUE, \"А😀Б😀\", \"😀\")" };
        yield return new object[] { "InStrFrom-arg2", "INSTR(3, TRUE, \"😀\")" };
        yield return new object[] { "InStrFrom-arg3", "INSTR(3, \"А😀Б😀\", TRUE)" };
        yield return new object[] { "InStrRev-arg1", "INSTRREV(TRUE, \"😀\")" };
        yield return new object[] { "InStrRev-arg2", "INSTRREV(\"А😀Б😀\", TRUE)" };
        yield return new object[] { "InStrRevFrom-arg1", "INSTRREV(TRUE, \"А😀Б😀\", \"😀\")" };
        yield return new object[] { "InStrRevFrom-arg2", "INSTRREV(3, TRUE, \"😀\")" };
        yield return new object[] { "InStrRevFrom-arg3", "INSTRREV(3, \"А😀Б😀\", TRUE)" };
        yield return new object[] { "SubStrCount-arg1", "SUBSTRCOUNT(TRUE, \"😀\")" };
        yield return new object[] { "SubStrCount-arg2", "SUBSTRCOUNT(\"А😀Б😀\", TRUE)" };
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
