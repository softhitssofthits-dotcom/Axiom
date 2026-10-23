namespace Axiom.Execution.Tests;

public sealed class StringBuildIndependentContractTests
{
    [Fact]
    public void EmptyCountsAndUtf16FillHaveExplicitContracts()
    {
        Check("""
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

    [Fact]
    public void SplitKeepsEdgeAndRepeatedSeparatorsAndJoinUsesVisibleOrder()
    {
        Check("DIM parts[] AS STRING\r\nparts[] = SPLIT(\"\", \"😀\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT ARRAYISALLOCATED(parts[])\r\nPRINT LBOUND(parts[])\r\nPRINT UBOUND(parts[])\r\nPRINT JOIN(parts[]) = \"\"\r\nparts[] = SPLIT(\"😀А😀😀Б😀\", \"😀\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT JOIN(parts[], \"|\")\r\nparts[] = SPLIT(\"ааа\", \"аа\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT JOIN(parts[], \"|\")\r\nDIM items[-5 TO -3] AS STRING\r\nitems[-5] = \"А\"\r\nitems[-4] = \"\"\r\nitems[-3] = \"😀\"\r\nPRINT JOIN(items[], \"|\")\r\nPRINT JOIN(items[], \"\")\r\nDIM absent[] AS STRING\r\nPRINT JOIN(absent[], \"|\") = \"\"", ["0", "TRUE", "1", "0", "TRUE", "5", "|А||Б|", "2", "|а", "А||😀", "А😀", "TRUE"]);
    }

    [Fact]
    public void InvalidCountsFillAndSeparatorMatchInBytecodeVm()
    {
        foreach (var call in new[] { "REPEAT(\"\", -1)", "SPACE(-1)", "STRING(-1, \"\")", "STRING(1, \"\")", "PADLEFT(\"abc\", 0, \"\")", "PADRIGHT(\"abc\", 0, \"\")", "CENTER(\"abc\", 0, \"\")", "PADLEFT(\"abc\", -1)", "PADRIGHT(\"abc\", -1)", "CENTER(\"abc\", -1)", "INSERT(\"abc\", 0, \"x\")", "REMOVE(\"abc\", 0, 0)", "REMOVE(\"abc\", 1, -1)", "JOIN(SPLIT(\"\", \"\"))" })
        {
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call));
            Assert.False(second.Succeeded);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", b.Code);

                Check($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Fact]
    public void JoinRejectsWrongArrayRankBeforeExecution()
    {
        var source = "DIM items[1 TO 1, 1 TO 1] AS STRING\nPRINT JOIN(items[])";
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.Contains(second.Diagnostics, d => d.Code == "MB2003");
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
