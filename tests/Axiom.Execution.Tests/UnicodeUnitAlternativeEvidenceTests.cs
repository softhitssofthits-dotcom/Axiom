using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class UnicodeUnitAlternativeEvidenceTests
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
    public void ExistingFunctionsCanCountScalarsButUtf16LengthIsDifferent(string text, int utf16, int scalars, int graphemes)
    {
        Assert.Equal(graphemes, StringInfo.ParseCombiningCharacters(text).Length);
        var source = $$"""
DIM text AS STRING = "{{text}}"
DIM position AS INTEGER = 1
DIM count AS INTEGER = 0
DIM reconstructed AS STRING = ""
DO WHILE position <= LEN(text)
    DIM code AS INTEGER = ASCW(MID(text, position, 2))
    DIM fragment AS STRING = CHRW(code)
    reconstructed = reconstructed + fragment
    count = count + 1
    position = position + LEN(fragment)
LOOP
PRINT LEN(text)
PRINT count
PRINT reconstructed = text
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { utf16.ToString(), scalars.ToString(), "TRUE" }, run.Output);
        }
    }

    [Fact]
    public void ScalarSafeFirstFragmentDoesNotPreserveACombinedVisibleSymbol()
    {
        const string source = """
DIM combining AS STRING = "é"
DIM family AS STRING = "👨‍👩‍👧‍👦"
PRINT CHRW(ASCW(combining)) = combining
PRINT CHRW(ASCW(family)) = family
PRINT LEN(LEFT("😀", 1))
PRINT ASCW(LEFT("😀", 1))
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded);
            Assert.Equal(new[] { "FALSE", "FALSE", "1", "65533" }, run.Output);
        }
    }
}
