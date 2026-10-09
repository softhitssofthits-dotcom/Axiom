namespace Axiom.Execution.Tests;

public sealed class StringSearchIndependentContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Utf16PositionsAndOrdinalSearchAreExplicit(ExecutionMode mode)
    {
        Check(mode, """
PRINT LEN("А😀Б😀")
PRINT INSTR("А😀Б😀", "Б")
PRINT INSTR(3, "А😀Б😀", "😀")
PRINT INSTRREV("А😀Б😀", "😀")
PRINT INSTRREV(3, "А😀Б😀", "😀")
PRINT SUBSTRCOUNT("А😀Б😀", "😀")
PRINT CONTAINS("é", "é")
PRINT STARTSWITH("А😀Б", "А😀")
PRINT ENDSWITH("А😀Б", "😀Б")
PRINT REPLACE("А😀Б😀", "😀", "я")
PRINT LEFT("😀", 1) = MID("😀", 1, 1)
PRINT RIGHT("😀", 1) = MID("😀", 2, 1)
PRINT INSTR("А😀Б", RIGHT("😀", 1))
PRINT SUBSTRCOUNT("aaaa", "aa")
PRINT SUBSTRCOUNT("aaa", "aa")
PRINT REPLACE("aaa", "aa", "x")
PRINT INSTR("a" + CHR(0) + "b", CHR(0))
""", ["6", "4", "5", "5", "2", "2", "FALSE", "TRUE", "TRUE", "АяБя", "TRUE", "TRUE", "3", "2", "1", "xa", "2"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void EmptyValuesAndExtremeIndexesHaveDefinedResults(ExecutionMode mode)
    {
        Check(mode, """
PRINT INSTR("", "")
PRINT INSTR("", "a")
PRINT INSTR(4, "abc", "")
PRINT INSTR(2147483647, "abc", "")
PRINT INSTRREV("", "")
PRINT INSTRREV(1, "", "")
PRINT INSTRREV(2147483647, "abc", "")
PRINT INSTRREV(2147483647, "abc", "a")
PRINT CONTAINS("", "")
PRINT STARTSWITH("", "")
PRINT ENDSWITH("", "")
PRINT SUBSTRCOUNT("", "a")
PRINT LEN(LEFT("", 2147483647))
PRINT RIGHT("abc", 2147483647)
PRINT LEN(MID("abc", 2147483647, 2147483647))
PRINT MID("abc", 2, 2147483647)
PRINT REPLACE("", "a", "b") = ""
PRINT REPLACE("abc", "a", "")
""", ["1", "0", "4", "0", "0", "1", "4", "1", "TRUE", "TRUE", "TRUE", "0", "0", "abc", "0", "bc", "TRUE", "bc"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ReplaceManyUsesLongestFirstAndDoesNotRescanReplacement(ExecutionMode mode)
    {
        Check(mode, """
DIM oldItems(5 TO 8) AS STRING
DIM newItems(-2 TO 1) AS STRING
oldItems(5) = "a"
oldItems(6) = "ab"
oldItems(7) = "ab"
oldItems(8) = "😀"
newItems(-2) = "ab"
newItems(-1) = "Первое"
newItems(0) = "Второе"
newItems(1) = "я"
PRINT REPLACEMANY("aba😀", oldItems, newItems)
PRINT REPLACEMANY("", oldItems, newItems) = ""
DIM absentOld() AS STRING
DIM absentNew() AS STRING
PRINT REPLACEMANY("А😀Б", absentOld, absentNew)
""", ["Первоеabя", "TRUE", "А😀Б"]);
    }

    [Fact]
    public void InvalidArgumentsAreCatchableWithMatchingLocations()
    {
        foreach (var call in new[] { "LEFT(\"abc\", 0)", "RIGHT(\"abc\", -1)", "MID(\"abc\", 0, 1)", "MID(\"abc\", 1, 0)", "INSTR(0, \"abc\", \"a\")", "INSTRREV(0, \"abc\", \"a\")", "REPLACE(\"\", \"\", \"x\")", "SUBSTRCOUNT(\"\", \"\")" })
        {
            var first = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Interpreter));
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Bytecode));
            Assert.False(first.Succeeded);
            Assert.False(second.Succeeded);
            var a = Assert.Single(first.Diagnostics);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", a.Code);
            Assert.Equal((a.Code, a.Location.Line, a.Location.Column, a.Location.Span), (b.Code, b.Location.Line, b.Location.Column, b.Location.Span));
            foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
                Check(mode, $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ReplaceManyRejectsUnequalLengthsEmptyRulesAndWrongRank(ExecutionMode mode)
    {
        foreach (var setup in new[] {
            "DIM oldItems(1 TO 2) AS STRING\nDIM newItems(1 TO 1) AS STRING\noldItems(1) = \"a\"\noldItems(2) = \"b\"",
            "DIM oldItems(1 TO 1) AS STRING\nDIM newItems(1 TO 1) AS STRING",
            "DIM oldItems(1 TO 1, 1 TO 1) AS STRING\nDIM newItems(1 TO 1) AS STRING",
            "DIM oldItems(1 TO 1) AS STRING\noldItems(1) = \"a\"\nDIM newItems(1 TO 1, 1 TO 1) AS STRING" })
        {
            if (setup.Contains("1 TO 1, 1 TO 1", StringComparison.Ordinal))
            {
                var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + "\nPRINT REPLACEMANY(\"\", oldItems, newItems)", mode: mode));
                Assert.False(result.Succeeded);
                Assert.Contains(result.Diagnostics, d => d.Code == "MB2003");
                Assert.False(result.UsedInterpreterFallback);
            }
            else
                Check(mode, setup + "\nTRY\nPRINT REPLACEMANY(\"\", oldItems, newItems)\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void OrdinalSearchDoesNotUseTurkishOrRussianCulture(ExecutionMode mode)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR", "ru-RU" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                Check(mode, """
PRINT CONTAINS("Iıİi", "i")
PRINT INSTR("Iıİi", "i")
PRINT INSTRREV("Iıİi", "i")
PRINT REPLACE("Iıİi", "i", "я")
PRINT STARTSWITH("Iıİi", "i")
PRINT ENDSWITH("Iıİi", "I")
""", ["TRUE", "4", "4", "Iıİя", "FALSE", "FALSE"]);
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expected, result.Output);
    }
}
