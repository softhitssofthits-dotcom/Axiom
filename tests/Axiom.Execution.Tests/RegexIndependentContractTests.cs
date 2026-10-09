namespace Axiom.Execution.Tests;

public sealed class RegexIndependentContractTests
{
    public static IEnumerable<object[]> Forms()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var name in new[] { "REGEXISMATCH", "REGEXMATCH", "REGEXEXTRACT", "REGEXMATCHES", "REGEXEXTRACTALL", "REGEXSPLIT", "REGEXREPLACE", "REGEXCOUNT" })
        foreach (var modes in new[] { false, true })
        {
            yield return [mode, name, modes, false];
            if (name == "REGEXREPLACE") yield return [mode, name, modes, true];
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void AllFormsHaveCorrectOrdinaryResultsAndResultTypes(ExecutionMode mode, string name, bool modes, bool count)
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
            ? $"DIM v() AS STRING\nv = {call}\nPRINT ARRAYLENGTH(v)\nPRINT v(1)\nPRINT v(3)"
            : $"DIM v AS {(name == "REGEXISMATCH" ? "BOOLEAN" : name == "REGEXCOUNT" ? "INTEGER" : "STRING")} = {call}\nPRINT v";
        if (name == "REGEXMATCHES") source = $"DIM v() AS STRING\nv = {call}\nPRINT ARRAYLENGTH(v)\nPRINT v(1)\nPRINT v(2)";
        if (name == "REGEXEXTRACTALL") source = $"DIM v() AS STRING\nv = {call}\nPRINT ARRAYLENGTH(v)\nPRINT v(1)\nPRINT v(2)";
        AssertRun(source, mode, expected);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormRejectsInvalidPatternAndWrongTextType(ExecutionMode mode, string name, bool modes, bool count)
    {
        var call = Call(name, "\"текст\"", "\"[\"", modes ? "i" : null, count);
        AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", mode, ["MB3007"]);
        var wrong = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {Call(name, "TRUE", "\"а\"", modes ? "i" : null, count)}", mode: mode));
        Assert.False(wrong.Succeeded);
        Assert.NotEmpty(wrong.Diagnostics);
        if (modes)
            AssertRun($"TRY\nPRINT {Call(name, "\"а\"", "\"а\"", "q", count)}\nCATCH err\nPRINT err.Code\nEND TRY", mode, ["MB3007"]);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormHandlesEmptyInputAndMissingMatch(ExecutionMode mode, string name, bool modes, bool count)
    {
        foreach (var text in new[] { "", "ж" })
        {
            var call = Call(name, $"\"{text}\"", "\"(a)(b)\"", modes ? "msx" : null, count);
            if (name is "REGEXMATCHES" or "REGEXEXTRACTALL" or "REGEXSPLIT")
            {
                var expectedCount = name == "REGEXSPLIT" && text.Length > 0 ? "1" : "0";
                AssertRun($"DIM v() AS STRING\nv = {call}\nPRINT ARRAYLENGTH(v)", mode, [expectedCount]);
            }
            else
            {
                var expected = name switch { "REGEXISMATCH" => "FALSE", "REGEXCOUNT" => "0", "REGEXREPLACE" => text, _ => "" };
                AssertRun($"PRINT {call}", mode, [expected]);
            }
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ModesZeroWidthSplitAndMissingCapturesAreExplicit(ExecutionMode mode)
    {
        AssertRun("""
PRINT REGEXMATCH("А", "а", "i")
PRINT REGEXMATCH("a" + CHR(10) + "b", "^b", "m")
PRINT REGEXISMATCH("a" + CHR(10) + "b", "a.b", "s")
PRINT REGEXISMATCH("ab", "a b # пояснение", "x")
DIM v() AS STRING
v = REGEXMATCHES("😀", ".")
PRINT v(1) = LEFT("😀", 1)
PRINT v(2) = RIGHT("😀", 1)
v = REGEXSPLIT("ab", "")
PRINT ARRAYLENGTH(v)
PRINT v(1) = ""
PRINT v(2)
PRINT v(3)
PRINT v(4) = ""
v = REGEXEXTRACTALL("aba", "(a)?b|(a)", 1)
PRINT ARRAYLENGTH(v)
PRINT v(1)
PRINT v(2) = ""
PRINT REGEXEXTRACT("a", "(?<10>a)", 1) = ""
""", mode, ["А", "b", "TRUE", "TRUE", "TRUE", "TRUE", "4", "TRUE", "a", "b", "TRUE", "2", "a", "TRUE", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void EmptyMatchesUnicodeAndReplacementTokensFollowExplicitContracts(ExecutionMode mode)
    {
        AssertRun("""
PRINT REGEXCOUNT("😀", ".")
PRINT REGEXCOUNT("😀", "")
PRINT REGEXISMATCH("", "")
DIM matches() AS STRING
matches = REGEXMATCHES("", "")
PRINT ARRAYLENGTH(matches)
PRINT matches(1) = ""
DIM parts() AS STRING
parts = REGEXSPLIT("", ",")
PRINT ARRAYLENGTH(parts)
PRINT REGEXREPLACE("ab", "", "!", 2)
PRINT REGEXREPLACE("ab", "(?<letter>[a-z])", "${letter}$$$&", 1)
PRINT REGEXEXTRACT("bbb", "(a)?b", 1) = ""
PRINT REGEXEXTRACT("a", "(a)", 99) = ""
PRINT REGEXREPLACE("a", "[", "x", "q", 0)
""", mode, ["2", "3", "TRUE", "1", "TRUE", "0", "!a!b", "a$ab", "TRUE", "TRUE", "a"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExplicitNumericGroupNumbersAreNotArrayOrdinals(ExecutionMode mode)
    {
        AssertRun("""
PRINT REGEXEXTRACT("a", "(?<10>a)", 10)
PRINT REGEXEXTRACT("A", "(?<10>a)", 10, "i")
DIM v() AS STRING
v = REGEXEXTRACTALL("aa", "(?<10>a)", 10)
PRINT v(1)
PRINT v(2)
v = REGEXEXTRACTALL("AA", "(?<10>a)", 10, "i")
PRINT v(1)
PRINT v(2)
""", mode, ["a", "A", "a", "a", "A", "A"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void LimitedReplacementDoesNotSearchUnneededSuffix(ExecutionMode mode, bool modes)
    {
        var text = "x" + new string('a', 12000) + "!a";
        var suffix = modes ? ", \"i\", 1" : ", 1";
        AssertRun($"PRINT REGEXREPLACE(\"{text}\", \"x|(?:a|aa)+$\", \"y\"{suffix}) = \"y{text[1..]}\"", mode, ["TRUE"]);
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormConvertsRealMatchTimeoutToCatchableDiagnostic(ExecutionMode mode, string name, bool modes, bool count)
    {
        var text = new string('a', 12000) + "!a";
        var call = Call(name, $"\"{text}\"", "\"(?:a|aa)+$\"", modes ? "i" : null, count);
        AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", mode, ["MB3007"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void NegativeGroupAndCountAreCatchableArgumentErrors(ExecutionMode mode)
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
            AssertRun($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", mode, ["MB3007"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void IgnoreCaseAndCachedModesAreCultureInvariant(ExecutionMode mode)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "tr-TR", "ru-RU" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                AssertRun("PRINT REGEXISMATCH(\"I\", \"i\", \"i\")\nPRINT REGEXMATCH(\"I\", \"i\", \"i\")", mode, ["TRUE", "I"]);
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void PatternBackslashesAreWrittenOnceInAxiomStrings(ExecutionMode mode)
    {
        AssertRun("""
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT REGEXEXTRACT("A12", "([A-Z])(\\d+)", 2) = ""
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
""", mode, ["12", "TRUE", "A-12 B-34"]);
    }


    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void GroupZeroWholeMatchAndWrongArgumentTypesAreExplicit(ExecutionMode mode)
    {
        AssertRun("""
PRINT REGEXEXTRACT("код12", "код([0-9]+)", 0)
DIM v() AS STRING
v = REGEXEXTRACTALL("код12 код34", "код([0-9]+)", 0, "i")
PRINT v(1)
PRINT v(2)
""", mode, ["код12", "код12", "код34"]);
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
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {call}", mode: mode));
            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryFormHandlesEmptyPatternWithoutLosingZeroWidthMatches(ExecutionMode mode, string name, bool modes, bool count)
    {
        var call = Call(name, "\"ab\"", "\"\"", modes ? "i" : null, count);
        if (name == "REGEXREPLACE") call = call.Replace("\"$2:$1\"", "\"!\"", StringComparison.Ordinal);
        if (name is "REGEXMATCHES" or "REGEXEXTRACTALL" or "REGEXSPLIT")
            AssertRun($"DIM v() AS STRING\nv = {call}\nPRINT ARRAYLENGTH(v)\nPRINT v(1) = \"\"", mode, [name == "REGEXSPLIT" ? "4" : "3", "TRUE"]);
        else
        {
            var expected = name switch { "REGEXISMATCH" => "TRUE", "REGEXCOUNT" => "3", "REGEXREPLACE" => count ? "!ab" : "!a!b!", _ => "" };
            AssertRun($"PRINT {call}", mode, [expected]);
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

    private static void AssertRun(string source, ExecutionMode mode, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }
}
