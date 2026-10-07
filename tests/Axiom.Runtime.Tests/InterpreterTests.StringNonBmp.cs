using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    // Контракт строковых единиц (аудит 2026-10-06, AXD-011): LEN/LEFT/MID/INSERT/
    // REMOVE/PAD работают в UTF-16 code units и могут разрезать суррогатную пару;
    // ASC/ASCW/CHR/CHRW работают в Unicode scalar; REVERSE сохраняет пары.
    // Тесты фиксируют текущий договор, чтобы любое его изменение стало осознанным.

    [Fact]
    public void Execute_StringLengthCountsUtf16CodeUnits()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEN(\"😀\"), LEN(\"a😀b\"), LEN(\"ё\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["2 4 1"], host.Output);
    }

    [Fact]
    public void Execute_StringSlicingOperatesOnUtf16CodeUnits()
    {
        // xUnit искажает одиночные суррогаты при сериализации theory-данных,
        // поэтому кейсы проверяются внутри одного факта.
        var high = (char)0xD83D;
        var low = (char)0xDE00;
        var cases = new (string Expression, string Expected)[]
        {
            ("LEFT(\"a😀b\", 2)", "a" + high),
            ("RIGHT(\"a😀b\", 2)", low + "b"),
            ("MID(\"a😀b\", 2, 1)", high.ToString()),
            ("REMOVE(\"a😀b\", 2, 1)", "a" + low + "b"),
        };

        foreach (var (expression, expected) in cases)
        {
            var (_, host, result) = ExecuteProgram("PRINT " + expression);

            Assert.True(result.Succeeded, expression);
            Assert.Equal([expected], host.Output);
        }
    }

    [Fact]
    public void Execute_ReversePreservesSurrogatePairs()
    {
        var (_, host, result) = ExecuteProgram("PRINT REVERSE(\"😀😎\"), REVERSE(\"a😀\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["😎😀 😀a"], host.Output);
    }

    [Fact]
    public void Execute_AscAndAscWReadScalarValuesIncludingNonBmp()
    {
        var (_, host, result) = ExecuteProgram("PRINT ASC(\"😀\"), ASCW(\"😀\"), ASC(\"ё\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["128512 128512 241"], host.Output);
    }

    [Theory]
    [InlineData("CHR(128512)", "😀")]
    [InlineData("CHR(1114111)", "\U0010FFFF")]
    public void Execute_ChrBuildsScalarIncludingNonBmp(string expression, string expected)
    {
        var (_, host, result) = ExecuteProgram("PRINT " + expression);

        Assert.True(result.Succeeded);
        Assert.Equal([expected], host.Output);
    }

    [Theory]
    [InlineData("CHR(55296)")]
    [InlineData("CHR(57343)")]
    [InlineData("CHR(1114112)")]
    public void Execute_ChrRejectsSurrogateHalvesAndOutOfRange(string expression)
    {
        var (_, _, result) = ExecuteProgram("PRINT " + expression);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHR", diagnostic.Message, StringComparison.Ordinal);
    }

    private const string HighSurrogate = "\uD83D";
    private const string LowSurrogate = "\uDE00";
}
