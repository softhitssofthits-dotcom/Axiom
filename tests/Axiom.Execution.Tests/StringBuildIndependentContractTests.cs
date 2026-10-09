namespace Axiom.Execution.Tests;

public sealed class StringBuildIndependentContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void EmptyCountsAndUtf16FillHaveExplicitContracts(ExecutionMode mode)
    {
        Check(mode, """
PRINT REPEAT("", 2147483647) = ""
PRINT REPEAT("я", 0) = ""
PRINT SPACE(0) = ""
PRINT STRING(0, "") = ""
PRINT STRING(2, "😀") = LEFT("😀", 1) + LEFT("😀", 1)
PRINT PADLEFT("я", 3, "аб")
PRINT PADRIGHT("я", 3, "аб")
PRINT CENTER("😀", 5, ".")
PRINT CENTER("😀", 1)
PRINT PADLEFT("😀", 3, "😀") = LEFT("😀", 1) + "😀"
PRINT INSERT("АБ", 2147483647, "😀")
PRINT INSERT("АБ", 1, "😀")
PRINT INSERT("😀", 2, "я") = LEFT("😀", 1) + "я" + RIGHT("😀", 1)
PRINT REMOVE("А😀Б", 2, 2147483647)
PRINT REMOVE("А😀Б", 2147483647, 2147483647)
PRINT REMOVE("А😀Б", 2, 0)
""", ["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "аая", "яаа", ".😀..", "😀", "TRUE", "АБ😀", "😀АБ", "TRUE", "А", "А😀Б", "А😀Б"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void SplitKeepsEdgeAndRepeatedSeparatorsAndJoinUsesVisibleOrder(ExecutionMode mode)
    {
        Check(mode, """
DIM parts() AS STRING
parts = SPLIT("", "😀")
PRINT ARRAYLENGTH(parts)
PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT JOIN(parts) = ""
parts = SPLIT("😀А😀😀Б😀", "😀")
PRINT ARRAYLENGTH(parts)
PRINT JOIN(parts, "|")
parts = SPLIT("ааа", "аа")
PRINT ARRAYLENGTH(parts)
PRINT JOIN(parts, "|")
DIM items(-5 TO -3) AS STRING
items(-5) = "А"
items(-4) = ""
items(-3) = "😀"
PRINT JOIN(items, "|")
PRINT JOIN(items, "")
DIM absent() AS STRING
PRINT JOIN(absent, "|") = ""
""", ["0", "TRUE", "1", "0", "TRUE", "5", "|А||Б|", "2", "|а", "А||😀", "А😀", "TRUE"]);
    }

    [Fact]
    public void InvalidCountsFillAndSeparatorMatchInBothEngines()
    {
        foreach (var call in new[] { "REPEAT(\"\", -1)", "SPACE(-1)", "STRING(-1, \"\")", "STRING(1, \"\")", "PADLEFT(\"abc\", 0, \"\")", "PADRIGHT(\"abc\", 0, \"\")", "CENTER(\"abc\", 0, \"\")", "PADLEFT(\"abc\", -1)", "PADRIGHT(\"abc\", -1)", "CENTER(\"abc\", -1)", "INSERT(\"abc\", 0, \"x\")", "REMOVE(\"abc\", 0, 0)", "REMOVE(\"abc\", 1, -1)", "JOIN(SPLIT(\"\", \"\"))" })
        {
            var first = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Interpreter));
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Bytecode));
            Assert.False(first.Succeeded);
            Assert.False(second.Succeeded);
            Assert.False(second.UsedInterpreterFallback);
            var a = Assert.Single(first.Diagnostics);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", a.Code);
            Assert.Equal((a.Code, a.Location.Line, a.Location.Column, a.Location.Span), (b.Code, b.Location.Line, b.Location.Column, b.Location.Span));
            foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
                Check(mode, $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Fact]
    public void JoinRejectsWrongArrayRankBeforeExecution()
    {
        var source = "DIM items(1 TO 1, 1 TO 1) AS STRING\nPRINT JOIN(items)";
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Contains(first.Diagnostics, d => d.Code == "MB2003");
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Span)));
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expected, result.Output);
    }
}
