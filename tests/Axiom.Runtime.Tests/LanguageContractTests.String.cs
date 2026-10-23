using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_SplitReturnsStringArrayWithOneBasedBounds()
    {
        const string text = "DIM parts[] AS STRING\r\nparts[] = SPLIT(\"red,green,,blue\", \",\")\r\nPRINT ARRAYISALLOCATED(parts[])\r\nPRINT LBOUND(parts[])\r\nPRINT UBOUND(parts[])\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1]\r\nPRINT parts[3]\r\nPRINT parts[4]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue"], host.Output);
    }

    [Fact]
    public void Execute_SplitEmptyStringReturnsCanonicalEmptyArray()
    {
        const string text = "DIM parts[] AS STRING\r\nparts[] = SPLIT(\"\", \",\")\r\nPRINT ARRAYISALLOCATED(parts[])\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT LBOUND(parts[])\r\nPRINT UBOUND(parts[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SplitReportsEmptySeparator()
    {
        const string text = """
PRINT SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_JoinUsesStringArrayBounds()
    {
        const string text = "DIM parts[] AS STRING\r\nparts[] = SPLIT(\"red|green|blue\", \"|\")\r\nPRINT JOIN(parts[], \"/\")";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["red/green/blue"], host.Output);
    }

    [Fact]
    public void Execute_JoinUnallocatedAndEmptyArraysReturnEmptyString()
    {
        const string text = "DIM first[] AS STRING\r\nDIM second[1 TO 0] AS STRING\r\nPRINT JOIN(first[])\r\nPRINT JOIN(second[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", ""], host.Output);
    }

    [Fact]
    public void Execute_JoinSingleElementReturnsThatElement()
    {
        const string text = "DIM parts[5 TO 5] AS STRING\r\nparts[5] = \"solo\"\r\nPRINT JOIN(parts[])\r\nPRINT JOIN(parts[], \"/\")";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["solo", "solo"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinLen()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEN(\"hello\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStrAndVal()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"12.5\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["12.5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinTrimVariants()
    {
        const string text = """
PRINT TRIM("  hello  ")
PRINT TRIM("--hello--", "-")
PRINT TRIM("hello")
PRINT "[" + TRIM("hello   ") + "]"
PRINT "[" + TRIM("   hello") + "]"
PRINT "[" + TRIM("   ") + "]"
PRINT LTRIM("  hello  ")
PRINT LTRIM("hello")
PRINT RTRIM("  hello  ")
PRINT RTRIM("hello")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hello", "hello", "hello", "[hello]", "[hello]", "[]", "hello  ", "hello", "  hello", "hello", "hello", "hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReverse()
    {
        var (_, host, result) = ExecuteProgram("PRINT REVERSE(\"abcd\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["dcba"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatAndSpace()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT REPEAT("x", 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", ""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStringUsesFirstFillCharacter()
    {
        var (_, host, result) = ExecuteProgram("PRINT STRING(5, \"ab\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["aaaaa"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinPadLeftAndPadRight()
    {
        const string text = """
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADLEFT("7", 3, "0")
PRINT PADLEFT("word", 2, ".")
PRINT PADLEFT("word", 4)
PRINT "[" + PADRIGHT("7", 3) + "]"
PRINT PADRIGHT("7", 3, ".")
PRINT PADRIGHT("word", 2, ".")
PRINT PADRIGHT("word", 4, ".")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[  7]", "007", "word", "word", "[7  ]", "7..", "word", "word"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInsertAndRemove()
    {
        const string text = """
PRINT INSERT("abcd", 1, "<")
PRINT INSERT("abcd", 3, "-")
PRINT INSERT("abcd", 99, "!")
PRINT INSERT("", 5, "seed")
PRINT INSERT("abcd", 2, "")
PRINT REMOVE("abcdef", 2, 3)
PRINT REMOVE("abcdef", 6, 10)
PRINT REMOVE("abcdef", 99, 2)
PRINT REMOVE("abcdef", 3, 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["<abcd", "ab-cd", "abcd!", "seed", "abcd", "aef", "abcde", "abcdef", "abcdef"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPEAT(\"ha\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPEAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadLeftReportsNegativeWidth()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADLEFT(\"7\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADLEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadRightReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADRIGHT(\"7\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADRIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInsertReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSERT(\"abc\", 0, \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRemoveReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REMOVE(\"abc\", 1, -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinStringReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT STRING(3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSearchFunctionsUseOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT CONTAINS("Hello", "ELL")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT STARTSWITH("Hello", "")
PRINT ENDSWITH("Hello", "")
PRINT STARTSWITH("Hello", "Hello")
PRINT ENDSWITH("Hello", "Hello")
PRINT STARTSWITH("Hello", "Hello!")
PRINT ENDSWITH("Hello", "Hello!")
PRINT CONTAINS("Hello", "")
PRINT CONTAINS("", "x")
PRINT CONTAINS("Hello", "Hello")
PRINT CONTAINS("Hello", "Hello!")
PRINT CONTAINS("Hello", "o")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceUsesOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT REPLACE("one two two", "two", "three")
PRINT REPLACE("One one", "one", "two")
PRINT REPLACE("abc", "x", "y")
PRINT REPLACE("hello", "he", "he")
PRINT REPLACE("hello", "hello", "done")
PRINT REPLACE("abba", "b", "c")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one three three", "One two", "abc", "hello", "done", "acca"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceManyUsesOnePassLongestMatchRules()
    {
        const string text = "DIM oldItems[1 TO 4] AS STRING\r\nDIM newItems[1 TO 4] AS STRING\r\noldItems[1] = \"ab\"\r\nnewItems[1] = \"X\"\r\noldItems[2] = \"abc\"\r\nnewItems[2] = \"Y\"\r\noldItems[3] = \"x\"\r\nnewItems[3] = \"z\"\r\noldItems[4] = \"one\"\r\nnewItems[4] = \"two\"\r\nPRINT REPLACEMANY(\"abc ab x One\", oldItems[], newItems[])\r\nPRINT REPLACEMANY(\"one\", oldItems[], newItems[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Y X z One", "two"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReturnsOneBasedPositions()
    {
        const string text = """
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTR("banana", "NA")
PRINT INSTR(7, "banana", "")
PRINT INSTR(8, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "5", "0", "7", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTR(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReturnsLastOneBasedPosition()
    {
        const string text = """
PRINT INSTRREV("banana", "na")
PRINT INSTRREV("banana", "NA")
PRINT INSTRREV("abc", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "0", "4"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevCanSearchBackFromStartPosition()
    {
        const string text = """
PRINT INSTRREV(4, "banana", "na")
PRINT INSTRREV(2, "banana", "na")
PRINT INSTRREV(100, "banana", "na")
PRINT INSTRREV(4, "banana", "")
PRINT INSTRREV(100, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "0", "5", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTRREV(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTRREV", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscAndChrUseCp866ForByteCodesAndUnicodeAbove255()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
PRINT ASC("Я")
PRINT CHR(159)
PRINT ASC("█")
PRINT CHR(219)
PRINT ASC(CHR(1071))
PRINT ASC(CHR(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["65", "A", "159", "Я", "219", "█", "159", "128512"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinAscWAndChrWUseUnicodeCodePoints()
    {
        const string text = """
PRINT ASC("Я")
PRINT ASCW("Я")
PRINT ASC("█")
PRINT ASCW("█")
PRINT ASCW(CHR(159))
PRINT ASCW(CHRW(1071))
PRINT CHRW(1071)
PRINT ASCW(CHRW(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["159", "1071", "219", "9608", "1071", "1071", "Я", "128512"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterCentersText()
    {
        const string text = """
PRINT "[" + CENTER("A", 3) + "]"
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT "[" + CENTER("AB", 5, ".") + "]"
PRINT "[" + CENTER("Hello", 3, ".") + "]"
PRINT "[" + CENTER("Hello", 5, ".") + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[ A ]", "[.A..]", "[.AB..]", "[Hello]", "[Hello]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT CENTER(\"A\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    public void Execute_BuiltinCenterReportsNegativeWidth(string widthText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CENTER(\"A\", {widthText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASC(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscWReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASCW(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASCW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHR({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrWReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHRW({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHRW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReturnsNonOverlappingCount()
    {
        const string text = """
PRINT SUBSTRCOUNT("banana", "na")
PRINT SUBSTRCOUNT("aaaa", "aa")
PRINT SUBSTRCOUNT("banana", "NA")
PRINT SUBSTRCOUNT("", "a")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReportsEmptyFragment()
    {
        var (_, _, result) = ExecuteProgram("PRINT SUBSTRCOUNT(\"abc\", \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SUBSTRCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fragment", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinReplaceReportsEmptyOldText()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPLACE(\"abc\", \"\", \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не может быть пустой строкой", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_BuiltinReplaceManyReportsInvalidRules()
    {
        const string text = "DIM oldItems[1 TO 1] AS STRING\r\nDIM newItems[1 TO 2] AS STRING\r\noldItems[1] = \"\"\r\nnewItems[1] = \"x\"\r\nnewItems[2] = \"y\"\r\nPRINT REPLACEMANY(\"abc\", oldItems[], newItems[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACEMANY", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
