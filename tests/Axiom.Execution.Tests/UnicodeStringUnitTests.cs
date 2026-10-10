namespace Axiom.Execution.Tests;

public sealed class UnicodeStringUnitTests
{
    [Theory]
    [InlineData("", 0, 0, 0)]
    [InlineData("Я", 1, 1, 1)]
    [InlineData("😀", 2, 1, 1)]
    [InlineData("é", 2, 2, 1)]
    [InlineData("👨‍👩‍👧‍👦", 11, 7, 1)]
    [InlineData("🇷🇺", 4, 2, 1)]
    [InlineData("👍🏽", 4, 2, 1)]
    [InlineData("1️⃣", 3, 3, 1)]
    [InlineData("Я😀é", 5, 4, 3)]
    [InlineData("🇷🇺🇧", 6, 3, 2)]
    [InlineData("가", 2, 2, 1)]
    [InlineData("́A", 2, 2, 2)]
    public void LengthUsesExplicitUnit(string text, int utf16, int codepoints, int graphemes)
    {
        Run($$"""
PRINT LEN("{{text}}")
PRINT LEN("{{text}}", "UTF16")
PRINT LEN("{{text}}", "codepoint")
PRINT LEN("{{text}}", "GrApHeMe")
""", utf16.ToString(), utf16.ToString(), codepoints.ToString(), graphemes.ToString());
    }

    [Theory]
    [InlineData("UTF16", "А", "Б", "😀é", 6)]
    [InlineData("CODEPOINT", "А", "Б", "😀e", 5)]
    [InlineData("GRAPHEME", "А", "Б", "😀é", 4)]
    public void SlicesUseSelectedPositionsAndClampAtEnd(string unit, string left, string right, string middle, int length)
    {
        Run($$"""
DIM text AS STRING = "А😀éБ"
PRINT LEN(text, "{{unit}}")
PRINT LEFT(text, 1, "{{unit}}")
PRINT RIGHT(text, 1, "{{unit}}")
PRINT MID(text, 2, {{(unit == "UTF16" ? 4 : 2)}}, "{{unit}}")
PRINT LEFT(text, 2147483647, "{{unit}}") = text
PRINT RIGHT(text, 2147483647, "{{unit}}") = text
PRINT MID(text, 2, 2147483647, "{{unit}}") = RIGHT(text, {{length - 1}}, "{{unit}}")
PRINT LEN(MID(text, 2147483647, 1, "{{unit}}"))
PRINT LEN(LEFT("", 1, "{{unit}}"))
PRINT LEN(RIGHT("", 1, "{{unit}}"))
PRINT LEN(MID("", 1, 1, "{{unit}}"))
""", length.ToString(), left, right, middle, "TRUE", "TRUE", "TRUE", "0", "0", "0", "0");
    }

    [Fact]
    public void GraphemeSlicesPreserveWholeFamilyFlagAndCombiningMark()
    {
        Run("""
DIM text AS STRING = "👨‍👩‍👧‍👦🇷🇺é"
PRINT LEFT(text, 1, "GRAPHEME")
PRINT MID(text, 2, 1, "GRAPHEME")
PRINT RIGHT(text, 1, "GRAPHEME")
PRINT MID(text, 2, 2, "GRAPHEME")
PRINT LEFT("😀", 1, "CODEPOINT")
PRINT LEN(LEFT("😀", 1))
""", "👨‍👩‍👧‍👦", "🇷🇺", "é", "🇷🇺é", "😀", "1");
    }

    [Theory]
    [InlineData("LEN(\"\", \"bad\")")]
    [InlineData("LEFT(\"\", 1, \"\")")]
    [InlineData("RIGHT(\"\", 1, \" UTF16\")")]
    [InlineData("MID(\"\", 100, 1, \"GRAPHEME \")")]
    [InlineData("LEFT(\"a\", 0, \"GRAPHEME\")")]
    [InlineData("RIGHT(\"a\", -1, \"CODEPOINT\")")]
    [InlineData("MID(\"a\", 0, 1, \"GRAPHEME\")")]
    [InlineData("MID(\"a\", 1, 0, \"GRAPHEME\")")]
    public void InvalidArgumentsProduceRuntimeDiagnostic(string expression)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + expression));
            Assert.False(run.Succeeded);
            Assert.Contains(run.Diagnostics, d => d.Code == "MB3007");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void IsolatedSurrogatesArePreservedByCodepointSlices(int variant)
    {
        var text = new[] { "\ud800", "\udc00", "A\ud800B\udc00C" }[variant];
        Run($$"""
DIM text AS STRING = "{{text}}"
PRINT LEN(text, "CODEPOINT") = LEN(text)
PRINT LEFT(text, 2147483647, "CODEPOINT") = text
PRINT RIGHT(text, 2147483647, "CODEPOINT") = text
DIM rebuilt AS STRING = ""
FOR position = 1 TO LEN(text, "CODEPOINT")
    rebuilt = rebuilt + MID(text, position, 1, "CODEPOINT")
NEXT position
PRINT rebuilt = text
""", "TRUE", "TRUE", "TRUE", "TRUE");
    }

    [Theory]
    [InlineData("LEN(123, \"GRAPHEME\")")]
    [InlineData("LEN(\"a\", 1)")]
    [InlineData("LEFT(\"a\", \"1\", \"GRAPHEME\")")]
    [InlineData("RIGHT(\"a\", 1, 2)")]
    [InlineData("MID(\"a\", 1, 1, TRUE)")]
    public void WrongArgumentTypesAreRejectedBeforeExecution(string expression)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + expression));
            Assert.False(run.Succeeded);
            Assert.NotEmpty(run.Diagnostics);
            Assert.DoesNotContain(run.Diagnostics, d => d.Code.StartsWith("MB3", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("len")]
    [InlineData("left")]
    [InlineData("right")]
    [InlineData("mid")]
    public void RunnableSamplesHaveExpectedOutputInBytecodeVm(string name)
    {
        string[] expected = name switch
        {
            "len" => ["Единицы UTF-16: 11", "Unicode-коды: 7", "Целые составные символы: 1"],
            "left" => ["Первый составной символ: é", "Первый Unicode-код: e"],
            "right" => ["Флаг целиком: 🇷🇺", "Длина флага в Unicode-кодах: 2"],
            "mid" => ["Второй составной символ: 👨‍👩‍👧‍👦", "Два символа из середины: 👨‍👩‍👧‍👦é"],
            _ => throw new InvalidOperationException()
        };
        Run(TestProgramLoader.LoadText($"samples/functions/{name}/unicode_units.axm"), expected);
    }

    private static void Run(string source, params string[] expected)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
}
