using Axiom.Core;

namespace Axiom.Lexing.Tests;

public sealed class BinaryLiteralTests
{
    [Theory]
    [InlineData("&B0", 0)]
    [InlineData("&b1010", 10)]
    [InlineData("&B00001111", 15)]
    public void BinaryLiteralHasIntegerValue(string source, int expected)
    {
        var tokens = Lexer.Lex(source, out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.IntegerLiteralToken, tokens[0].Kind);
        Assert.Equal(expected, Assert.IsType<int>(tokens[0].Value));
        Assert.Equal(source, tokens[0].Text);
    }

    [Fact]
    public void BinaryLiteralSelectsWidthByValueRatherThanDigitCount()
    {
        var tokens = Lexer.Lex("&B" + new string('0', 64) + "1 &B1" + new string('0', 31) + " &B" + new string('1', 63), out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(1, Assert.IsType<int>(tokens[0].Value));
        Assert.Equal(2147483648L, Assert.IsType<long>(tokens[1].Value));
        Assert.Equal(long.MaxValue, Assert.IsType<long>(tokens[2].Value));
    }

    [Theory]
    [InlineData("&B")]
    [InlineData("&B2")]
    [InlineData("&B102")]
    [InlineData("&B1_0")]
    [InlineData("&B101foo")]
    [InlineData("&HFFG")]
    public void InvalidRadixLiteralIsOneRejectedToken(string source)
    {
        var tokens = Lexer.Lex(source, out var diagnostics);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidNumericLiteral, diagnostic.Code);
        Assert.Equal(source.Length, diagnostic.Location.Span.Length);
        Assert.Equal(source, tokens[0].Text);
        Assert.Equal(TokenKind.EndOfFileToken, tokens[1].Kind);
    }

    [Fact]
    public void BinaryMagnitudeAboveSignedLongIsRejected()
    {
        Lexer.Lex("&B1" + new string('0', 63), out var diagnostics);
        Assert.Equal(DiagnosticCodes.NumericLiteralOutOfRange, Assert.Single(diagnostics).Code);
    }
}
