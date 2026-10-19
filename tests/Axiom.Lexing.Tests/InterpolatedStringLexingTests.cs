using Axiom.Core;
using Axiom.Lexing;

namespace Axiom.Lexing.Tests;

public sealed class InterpolatedStringLexingTests
{
    [Theory]
    [InlineData("$\"{{x}}\"", "{x}")]
    [InlineData("$\"a}}b\"", "a}b")]
    [InlineData("$\"a\"\"b\"", "a\"b")]
    [InlineData("$\"c\\\\d\"", "c\\\\d")]
    [InlineData("$\"e\\{f\"", null)]
    public void Lex_InterpolatedStringTextKeepsItsOwnEscapingRules(string source, string? expectedValue)
    {
        var tokens = Lex(source, out var diagnostics);

        if (expectedValue is null)
        {
            // Обратная косая черта не является экраном: \{ открывает вставку выражения.
            Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB0002");
            Assert.Contains(tokens, static token => token.Kind == TokenKind.InterpolatedStringExpressionStartToken);
            return;
        }

        Assert.Empty(diagnostics);
        var textToken = Assert.Single(tokens, static token => token.Kind == TokenKind.InterpolatedStringTextToken);
        Assert.Equal(expectedValue, Assert.IsType<string>(textToken.Value));
    }

    [Fact]
    public void Lex_InterpolatedStringFormatSpecifierTrimsSurroundingSpaces()
    {
        var tokens = Lex("$\"{n: F2 }\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var formatToken = Assert.Single(tokens, static token => token.Kind == TokenKind.InterpolatedStringFormatSpecifierToken);
        Assert.Equal(" F2 ", formatToken.Text);
        Assert.Equal("F2", Assert.IsType<string>(formatToken.Value));
    }

    [Fact]
    public void Lex_InterpolatedStringTextStopsAtNewLine()
    {
        const string source = "PRINT $\"abc\ndef\"";
        var tokens = Lex(source, out var diagnostics);

        var firstDiagnostic = Assert.Single(
            diagnostics,
            static diagnostic => diagnostic.Code == "MB0002" && diagnostic.Location.Span.Start == 6);
        Assert.Equal(6, firstDiagnostic.Location.Span.Start);

        var endToken = Assert.Single(tokens, static token => token.Kind == TokenKind.InterpolatedStringEndToken);
        Assert.Equal(0, endToken.Location.Span.Length);
        Assert.Equal(11, endToken.Location.Span.Start);

        var textToken = Assert.Single(tokens, static token => token.Kind == TokenKind.InterpolatedStringTextToken);
        Assert.Equal("abc", Assert.IsType<string>(textToken.Value));
        Assert.Contains(tokens, static token => token.Kind == TokenKind.NewLineToken);
    }

    [Fact]
    public void Lex_InterpolatedStringReportsUnterminatedAtEndOfFile()
    {
        const string source = "PRINT $\"abc";
        var tokens = Lex(source, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0002", diagnostic.Code);
        Assert.Equal(6, diagnostic.Location.Span.Start);
        Assert.Equal(TokenKind.EndOfFileToken, tokens[^1].Kind);
    }

    [Fact]
    public void Lex_InterpolatedStringSingleBraceStartsExpression()
    {
        var tokens = Lex("$\"a{b}c\"", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(
            new[]
            {
                TokenKind.InterpolatedStringStartToken,
                TokenKind.InterpolatedStringTextToken,
                TokenKind.InterpolatedStringExpressionStartToken,
                TokenKind.IdentifierToken,
                TokenKind.InterpolatedStringExpressionEndToken,
                TokenKind.InterpolatedStringTextToken,
                TokenKind.InterpolatedStringEndToken,
                TokenKind.EndOfFileToken
            },
            tokens.Select(static token => token.Kind));
    }

    [Fact]
    public void Lex_NestedInterpolatedStringRestoresOuterTextState()
    {
        var tokens = Lex("$\"a{$\"b\"}c\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var textValues = tokens
            .Where(static token => token.Kind == TokenKind.InterpolatedStringTextToken)
            .Select(static token => Assert.IsType<string>(token.Value))
            .ToArray();
        Assert.Equal(new[] { "a", "b", "c" }, textValues);
        Assert.Equal(TokenKind.EndOfFileToken, tokens[^1].Kind);
    }

    private static IReadOnlyList<Token> Lex(string text, out DiagnosticBag diagnostics)
    {
        return Lexer.Lex(text, out diagnostics);
    }
}
