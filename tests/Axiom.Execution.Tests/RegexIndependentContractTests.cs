namespace Axiom.Execution.Tests;

public sealed class RegexIndependentContractTests
{
    public static IEnumerable<object[]> Forms()
    {

        foreach (var name in new[] { "REGEXISMATCH", "REGEXMATCH", "REGEXEXTRACT", "REGEXMATCHES", "REGEXEXTRACTALL", "REGEXSPLIT", "REGEXREPLACE", "REGEXCOUNT" })
        foreach (var modes in new[] { false, true })
        {
            yield return [name, modes, false];
            if (name == "REGEXREPLACE") yield return [name, modes, true];
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void AllFormsHaveCorrectOrdinaryResultsAndResultTypes(string name, bool modes, bool count)
    {
        var call = Call(name, "\"А12 б34\"", "\"([АБаб])([0-9]+)\"", modes ? "Ii" : null, count);
        var expected = name switch
        {
            "REGEXISMATCH" => new[] { "TRUE" },
            "REGEXMATCH" => ["А12"],
            "REGEXEXTRACT" => ["12"],
            "REGEXCOUNT" => ["2"],
            "REGEXREPLACE" => [count ? "12:А б34" : "12:А 34:б"],
            "REGEXEXTRACTALL" => ["2", "12", "34"],
            "REGEXSPLIT" => ["7", "", "12"],
            _ => ["2", "А12", "б34"]
        };
        var source = name is "REGEXMATCHES" or "REGEXEXTRACTALL" or "REGEXSPLIT"
            ? $"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[])\nPRINT v[1]\nPRINT v[3]"
            : $"DIM v AS {(name == "REGEXISMATCH" ? "BOOLEAN" : name == "REGEXCOUNT" ? "INTEGER" : "STRING")} = {call}\nPRINT v";
        if (name == "REGEXMATCHES") source = $"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[])\nPRINT v[1]\nPRINT v[2]";
        if (name == "REGEXEXTRACTALL") source = $"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[])\nPRINT v[1]\nPRINT v[2]";
        AssertRun(source, expected);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormRejectsInvalidPatternAndWrongTextType(string name, bool modes, bool count)
    {
        var call = Call(name, "\"текст\"", "\"[\"", modes ? "i" : null, count);
        AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        var wrong = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {Call(name, "TRUE", "\"а\"", modes ? "i" : null, count)}"));
        Assert.False(wrong.Succeeded);
        Assert.NotEmpty(wrong.Diagnostics);
        if (modes)
            AssertRun($"TRY\nPRINT {Call(name, "\"а\"", "\"а\"", "q", count)}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormHandlesEmptyInputAndMissingMatch(string name, bool modes, bool count)
    {
        foreach (var text in new[] { "", "ж" })
        {
            var call = Call(name, $"\"{text}\"", "\"(a)(b)\"", modes ? "msx" : null, count);
            if (name is "REGEXMATCHES" or "REGEXEXTRACTALL" or "REGEXSPLIT")
            {
                var expectedCount = name == "REGEXSPLIT" && text.Length > 0 ? "1" : "0";
                AssertRun($"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[])", [expectedCount]);
            }
            else
            {
                var expected = name switch { "REGEXISMATCH" => "FALSE", "REGEXCOUNT" => "0", "REGEXREPLACE" => text, _ => "" };
                AssertRun($"PRINT {call}", [expected]);
            }
        }
    }

    [Fact]
    public void ModesZeroWidthSplitAndMissingCapturesAreExplicit()
    {
        AssertRun("PRINT REGEXMATCH(\"А\", \"а\", \"i\")\r\nPRINT REGEXMATCH(\"a\" + CHR(10) + \"b\", \"^b\", \"m\")\r\nPRINT REGEXISMATCH(\"a\" + CHR(10) + \"b\", \"a.b\", \"s\")\r\nPRINT REGEXISMATCH(\"ab\", \"a b # пояснение\", \"x\")\r\nDIM v[] AS STRING\r\nv[] = REGEXMATCHES(\"😀\", \".\")\r\nPRINT v[1] = LEFT(\"😀\", 1)\r\nPRINT v[2] = RIGHT(\"😀\", 1)\r\nv[] = REGEXSPLIT(\"ab\", \"\")\r\nPRINT ARRAYLENGTH(v[])\r\nPRINT v[1] = \"\"\r\nPRINT v[2]\r\nPRINT v[3]\r\nPRINT v[4] = \"\"\r\nv[] = REGEXEXTRACTALL(\"aba\", \"(a)?b|(a)\", 1)\r\nPRINT ARRAYLENGTH(v[])\r\nPRINT v[1]\r\nPRINT v[2] = \"\"\r\nPRINT REGEXEXTRACT(\"a\", \"(?<10>a)\", 1) = \"\"", ["А", "b", "TRUE", "TRUE", "TRUE", "TRUE", "4", "TRUE", "a", "b", "TRUE", "2", "a", "TRUE", "TRUE"]);
    }

    [Fact]
    public void EmptyMatchesUnicodeAndReplacementTokensFollowExplicitContracts()
    {
        AssertRun("PRINT REGEXCOUNT(\"😀\", \".\")\r\nPRINT REGEXCOUNT(\"😀\", \"\")\r\nPRINT REGEXISMATCH(\"\", \"\")\r\nDIM matches[] AS STRING\r\nmatches[] = REGEXMATCHES(\"\", \"\")\r\nPRINT ARRAYLENGTH(matches[])\r\nPRINT matches[1] = \"\"\r\nDIM parts[] AS STRING\r\nparts[] = REGEXSPLIT(\"\", \",\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT REGEXREPLACE(\"ab\", \"\", \"!\", 2)\r\nPRINT REGEXREPLACE(\"ab\", \"(?<letter>[a-z])\", \"${letter}$$$&\", 1)\r\nPRINT REGEXEXTRACT(\"bbb\", \"(a)?b\", 1) = \"\"\r\nPRINT REGEXEXTRACT(\"a\", \"(a)\", 99) = \"\"\r\nPRINT REGEXREPLACE(\"a\", \"[\", \"x\", \"q\", 0)", ["2", "3", "TRUE", "1", "TRUE", "0", "!a!b", "a$ab", "TRUE", "TRUE", "a"]);
    }

    [Fact]
    public void ExplicitNumericGroupNumbersAreNotArrayOrdinals()
    {
        AssertRun("PRINT REGEXEXTRACT(\"a\", \"(?<10>a)\", 10)\r\nPRINT REGEXEXTRACT(\"A\", \"(?<10>a)\", 10, \"i\")\r\nDIM v[] AS STRING\r\nv[] = REGEXEXTRACTALL(\"aa\", \"(?<10>a)\", 10)\r\nPRINT v[1]\r\nPRINT v[2]\r\nv[] = REGEXEXTRACTALL(\"AA\", \"(?<10>a)\", 10, \"i\")\r\nPRINT v[1]\r\nPRINT v[2]", ["a", "A", "a", "a", "A", "A"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LimitedReplacementDoesNotSearchUnneededSuffix(bool modes)
    {
        var text = "x" + new string('a', 12000) + "!a";
        var suffix = modes ? ", \"i\", 1" : ", 1";
        AssertRun($"PRINT REGEXREPLACE(\"{text}\", \"x|(?:a|aa)+$\", \"y\"{suffix}) = \"y{text[1..]}\"", ["TRUE"]);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormConvertsRealMatchTimeoutToCatchableDiagnostic(string name, bool modes, bool count)
    {
        var text = new string('a', 12000) + "!a";
        var call = Call(name, $"\"{text}\"", "\"(?:a|aa)+$\"", modes ? "i" : null, count);
        AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nPRINT err.Message\nEND TRY",
            ["MB3007", $"Function {name}: argument 'pattern' Pattern matching exceeded the 5 second timeout."]);
    }

    [Fact]
    public void NegativeGroupAndCountAreCatchableArgumentErrors()
    {
        foreach (var call in new[]
        {
            "REGEXEXTRACT(\"a\", \"(a)\", -1)",
            "REGEXEXTRACT(\"a\", \"(a)\", -1, \"i\")",
            "REGEXEXTRACTALL(\"a\", \"(a)\", -1)",
            "REGEXEXTRACTALL(\"a\", \"(a)\", -1, \"i\")",
            "REGEXREPLACE(\"a\", \"a\", \"x\", -1)",
            "REGEXREPLACE(\"a\", \"a\", \"x\", \"i\", -1)"
        })
            AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
    }

    [Fact]
    public void IgnoreCaseAndCachedModesAreCultureInvariant()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR", "ru-RU" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                AssertRun("PRINT REGEXISMATCH(\"I\", \"i\", \"i\")\nPRINT REGEXMATCH(\"I\", \"i\", \"i\")", ["TRUE", "I"]);
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void PatternBackslashesAreWrittenOnceInAxiomStrings()
    {
        AssertRun("""
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT REGEXEXTRACT("A12", "([A-Z])(\\d+)", 2) = ""
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
""", ["12", "TRUE", "A-12 B-34"]);
    }


    [Fact]
    public void GroupZeroWholeMatchAndWrongArgumentTypesAreExplicit()
    {
        AssertRun("PRINT REGEXEXTRACT(\"код12\", \"код([0-9]+)\", 0)\r\nDIM v[] AS STRING\r\nv[] = REGEXEXTRACTALL(\"код12 код34\", \"код([0-9]+)\", 0, \"i\")\r\nPRINT v[1]\r\nPRINT v[2]", ["код12", "код12", "код34"]);
        foreach (var call in new[]
        {
            "REGEXISMATCH(\"a\", TRUE)",
            "REGEXISMATCH(\"a\", \"a\", FALSE)",
            "REGEXEXTRACT(\"a\", \"a\", TRUE)",
            "REGEXEXTRACTALL(\"a\", \"a\", \"0\")",
            "REGEXREPLACE(\"a\", \"a\", FALSE)",
            "REGEXREPLACE(\"a\", \"a\", \"x\", \"i\", FALSE)"
        })
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {call}"));
            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormHandlesEmptyPatternWithoutLosingZeroWidthMatches(string name, bool modes, bool count)
    {
        var call = Call(name, "\"ab\"", "\"\"", modes ? "i" : null, count);
        if (name == "REGEXREPLACE") call = call.Replace("\"$2:$1\"", "\"!\"", StringComparison.Ordinal);
        if (name is "REGEXMATCHES" or "REGEXEXTRACTALL" or "REGEXSPLIT")
            AssertRun($"DIM v[] AS STRING\nv[] = {call}\nPRINT ARRAYLENGTH(v[])\nPRINT v[1] = \"\"", [name == "REGEXSPLIT" ? "4" : "3", "TRUE"]);
        else
        {
            var expected = name switch { "REGEXISMATCH" => "TRUE", "REGEXCOUNT" => "3", "REGEXREPLACE" => count ? "!ab" : "!a!b!", _ => "" };
            AssertRun($"PRINT {call}", [expected]);
        }
    }

    private static string Call(string name, string text, string pattern, string? modes, bool count)
    {
        var args = new List<string> { text, pattern };
        if (name is "REGEXEXTRACT" or "REGEXEXTRACTALL") args.Add("2");
        if (name == "REGEXREPLACE") args.Add("\"$2:$1\"");
        if (modes is not null) args.Add($"\"{modes}\"");
        if (count) args.Add("1");
        return $"{name}({string.Join(", ", args)})";
    }

    private static void AssertRun(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
