namespace Axiom.Execution.Tests;

public sealed class StringSearchIndependentContractTests
{
    [Fact]
    public void Utf16PositionsAndOrdinalSearchAreExplicit()
    {
        Check("""
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

    [Fact]
    public void EmptyValuesAndExtremeIndexesHaveDefinedResults()
    {
        Check("""
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

    [Fact]
    public void ReplaceManyUsesLongestFirstAndDoesNotRescanReplacement()
    {
        Check("DIM oldItems[5 TO 8] AS STRING\r\nDIM newItems[-2 TO 1] AS STRING\r\noldItems[5] = \"a\"\r\noldItems[6] = \"ab\"\r\noldItems[7] = \"ab\"\r\noldItems[8] = \"😀\"\r\nnewItems[-2] = \"ab\"\r\nnewItems[-1] = \"Первое\"\r\nnewItems[0] = \"Второе\"\r\nnewItems[1] = \"я\"\r\nPRINT REPLACEMANY(\"aba😀\", oldItems[], newItems[])\r\nPRINT REPLACEMANY(\"\", oldItems[], newItems[]) = \"\"\r\nDIM absentOld[] AS STRING\r\nDIM absentNew[] AS STRING\r\nPRINT REPLACEMANY(\"А😀Б\", absentOld[], absentNew[])", ["Первоеabя", "TRUE", "А😀Б"]);
    }

    [Fact]
    public void InvalidArgumentsAreCatchableWithMatchingLocations()
    {
        foreach (var call in new[] { "LEFT(\"abc\", 0)", "RIGHT(\"abc\", -1)", "MID(\"abc\", 0, 1)", "MID(\"abc\", 1, 0)", "INSTR(0, \"abc\", \"a\")", "INSTRREV(0, \"abc\", \"a\")", "REPLACE(\"\", \"\", \"x\")", "SUBSTRCOUNT(\"\", \"\")" })
        {
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call));
            Assert.False(second.Succeeded);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", b.Code);

                Check($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Fact]
    public void ReplaceManyRejectsUnequalLengthsEmptyRulesAndWrongRank()
    {
        foreach (var setup in new[] {
            "DIM oldItems[1 TO 2] AS STRING\nDIM newItems[1 TO 1] AS STRING\noldItems[1] = \"a\"\noldItems[2] = \"b\"",
            "DIM oldItems[1 TO 1] AS STRING\nDIM newItems[1 TO 1] AS STRING",
            "DIM oldItems[1 TO 1, 1 TO 1] AS STRING\nDIM newItems[1 TO 1] AS STRING",
            "DIM oldItems[1 TO 1] AS STRING\noldItems[1] = \"a\"\nDIM newItems[1 TO 1, 1 TO 1] AS STRING" })
        {
            if (setup.Contains("1 TO 1, 1 TO 1", StringComparison.Ordinal))
            {
                var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + "\nPRINT REPLACEMANY(\"\", oldItems[], newItems[])"));
                Assert.False(result.Succeeded);
                Assert.Contains(result.Diagnostics, d => d.Code == "MB2003");
            }
            else
                Check(setup + "\nTRY\nPRINT REPLACEMANY(\"\", oldItems[], newItems[])\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        }
    }

    [Fact]
    public void OrdinalSearchDoesNotUseTurkishOrRussianCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR", "ru-RU" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                Check("""
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

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
