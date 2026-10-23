using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_BuiltinRegexIsMatchSupportsModes()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXISMATCH("one" + CHR(10) + "two", "^two", "m")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchReturnsFirstMatchOrEmptyString()
    {
        const string text = """
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXMATCH("A12", "a\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "[]", "A12"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReturnsRequestedGroup()
    {
        const string text = """
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXEXTRACT("a12", "([A-Z])(\d+)", 1, "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "[]", "a"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceSupportsGroupReferencesAndCount()
    {
        const string text = """
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#")
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
PRINT REGEXREPLACE("payload 42", "payload\s+", "payload ", "i")
PRINT REGEXREPLACE("abc", "x", "y", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one # two #", "one # two 34", "A-12 B-34", "# b34", "payload 42", "abc"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexCountReturnsMatchCount()
    {
        const string text = """
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
PRINT REGEXCOUNT("text", "\d+")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchesReturnsAllMatches()
    {
        const string text = "DIM values[] AS STRING\r\nvalues[] = REGEXMATCHES(\"Order 42 and 100\", \"\\d+\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]\r\nPRINT values[2]\r\n\r\nvalues[] = REGEXMATCHES(\"A12 b34\", \"[a-z]\\d+\", \"i\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]\r\nPRINT values[2]\r\n\r\nvalues[] = REGEXMATCHES(\"Order\", \"\\d+\")\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "42", "100", "2", "A12", "b34", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReturnsRequestedGroupForEveryMatch()
    {
        const string text = "DIM values[] AS STRING\r\nvalues[] = REGEXEXTRACTALL(\"A12 B34\", \"([A-Z])(\\d+)\", 2)\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]\r\nPRINT values[2]\r\n\r\nvalues[] = REGEXEXTRACTALL(\"a12 b34\", \"([A-Z])(\\d+)\", 1, \"i\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]\r\nPRINT values[2]\r\n\r\nvalues[] = REGEXEXTRACTALL(\"A12 B34\", \"([A-Z])(\\d+)\", 3)\r\nPRINT \"[\" + values[1] + \"]\"\r\nPRINT \"[\" + values[2] + \"]\"";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "12", "34", "2", "a", "b", "[]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexSplitReturnsStringArray()
    {
        const string text = "DIM parts[] AS STRING\r\nparts[] = REGEXSPLIT(\"one, two;three\", \"[,;]\\s*\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1]\r\nPRINT parts[2]\r\nPRINT parts[3]\r\n\r\nparts[] = REGEXSPLIT(\"A12 b34\", \"[a-z]\\d+\", \"i\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT \"[\" + parts[1] + \"]\"\r\nPRINT \"[\" + parts[2] + \"]\"\r\nPRINT \"[\" + parts[3] + \"]\"";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "one", "two", "three", "3", "[]", "[ ]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsInvalidPattern()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"[\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsUnknownMode()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"a\", \"z\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXREPLACE(\"abc\", \"a\", \"x\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACT(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACTALL(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACTALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
