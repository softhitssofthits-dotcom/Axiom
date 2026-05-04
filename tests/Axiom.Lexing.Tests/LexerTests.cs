using Axiom.Core;
using Axiom.Lexing;

namespace Axiom.Lexing.Tests;

public sealed class LexerTests
{
    [Fact]
    public void Lex_RecognizesKeywordsCaseInsensitively()
    {
        var tokens = Lex("dIm value aS integer\r\nDiM bigger As LoNg\r\nDIM amount AS DeCiMaL\r\nDo UnTiL TRUE\r\nLoOp WhIlE FALSE\r\nSeLeCt CaSe value\r\nCaSe Is >= 10\r\nPrInT TRUE\r\nEnD SeLeCt\r\nIf FALSE tHeN\r\nElSeIf TRUE tHeN\r\nExIt FoR\r\nByReF ByVaL\r\nMoD\r\nEnD If", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertTokenKinds(
            tokens,
            TokenKind.DimKeyword,
            TokenKind.IdentifierToken,
            TokenKind.AsKeyword,
            TokenKind.IntegerKeyword,
            TokenKind.NewLineToken,
            TokenKind.DimKeyword,
            TokenKind.IdentifierToken,
            TokenKind.AsKeyword,
            TokenKind.LongKeyword,
            TokenKind.NewLineToken,
            TokenKind.DimKeyword,
            TokenKind.IdentifierToken,
            TokenKind.AsKeyword,
            TokenKind.DecimalKeyword,
            TokenKind.NewLineToken,
            TokenKind.DoKeyword,
            TokenKind.UntilKeyword,
            TokenKind.BooleanLiteralToken,
            TokenKind.NewLineToken,
            TokenKind.LoopKeyword,
            TokenKind.WhileKeyword,
            TokenKind.BooleanLiteralToken,
            TokenKind.NewLineToken,
            TokenKind.SelectKeyword,
            TokenKind.CaseKeyword,
            TokenKind.IdentifierToken,
            TokenKind.NewLineToken,
            TokenKind.CaseKeyword,
            TokenKind.IsKeyword,
            TokenKind.GreaterOrEqualsToken,
            TokenKind.IntegerLiteralToken,
            TokenKind.NewLineToken,
            TokenKind.PrintKeyword,
            TokenKind.BooleanLiteralToken,
            TokenKind.NewLineToken,
            TokenKind.EndKeyword,
            TokenKind.SelectKeyword,
            TokenKind.NewLineToken,
            TokenKind.IfKeyword,
            TokenKind.BooleanLiteralToken,
            TokenKind.ThenKeyword,
            TokenKind.NewLineToken,
            TokenKind.ElseIfKeyword,
            TokenKind.BooleanLiteralToken,
            TokenKind.ThenKeyword,
            TokenKind.NewLineToken,
            TokenKind.ExitKeyword,
            TokenKind.ForKeyword,
            TokenKind.NewLineToken,
            TokenKind.ByRefKeyword,
            TokenKind.ByValKeyword,
            TokenKind.NewLineToken,
            TokenKind.ModKeyword,
            TokenKind.NewLineToken,
            TokenKind.EndKeyword,
            TokenKind.IfKeyword,
            TokenKind.EndOfFileToken);
    }

    [Fact]
    public void Lex_RecognizesIdentifiersAndPreservesSourceText()
    {
        var tokens = Lex("alpha _Beta Beta123 remark REMark REMx", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Collection(
            TokensWithoutEndOfFile(tokens),
            token => Assert.Equal("alpha", token.Text),
            token => Assert.Equal("_Beta", token.Text),
            token => Assert.Equal("Beta123", token.Text),
            token => Assert.Equal("remark", token.Text),
            token => Assert.Equal("REMark", token.Text),
            token => Assert.Equal("REMx", token.Text));
    }

    [Fact]
    public void Lex_RecognizesIntegerAndDoubleLiterals()
    {
        var tokens = Lex("0 123 0.5 45.67", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.IntegerLiteralToken, tokens[0].Kind);
        Assert.Equal(0, Assert.IsType<int>(tokens[0].Value));
        Assert.Equal(TokenKind.IntegerLiteralToken, tokens[1].Kind);
        Assert.Equal(123, Assert.IsType<int>(tokens[1].Value));
        Assert.Equal(TokenKind.DoubleLiteralToken, tokens[2].Kind);
        Assert.Equal(0.5, Assert.IsType<double>(tokens[2].Value), 6);
        Assert.Equal(TokenKind.DoubleLiteralToken, tokens[3].Kind);
        Assert.Equal(45.67, Assert.IsType<double>(tokens[3].Value), 6);
    }

    [Fact]
    public void Lex_RecognizesHexLiterals()
    {
        var tokens = Lex("&HFF &h80000000", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.IntegerLiteralToken, tokens[0].Kind);
        Assert.Equal(255, Assert.IsType<int>(tokens[0].Value));
        Assert.Equal(TokenKind.LongLiteralToken, tokens[1].Kind);
        Assert.Equal(2147483648L, Assert.IsType<long>(tokens[1].Value));
    }

    [Fact]
    public void Lex_RecognizesLongLiteralWhenIntegerRangeExceeded()
    {
        var tokens = Lex("21474836479", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.LongLiteralToken, tokens[0].Kind);
        Assert.Equal(21474836479L, Assert.IsType<long>(tokens[0].Value));
    }

    [Theory]
    [InlineData("00")]
    [InlineData("001")]
    [InlineData("00.5")]
    [InlineData("01.25")]
    public void Lex_ReportsLeadingZeroNumericLiteral(string text)
    {
        var tokens = Lex(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0003", diagnostic.Code);
        Assert.Contains("ведущие нули", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(tokens[0].Kind is TokenKind.IntegerLiteralToken or TokenKind.DoubleLiteralToken);
    }

    [Fact]
    public void Lex_ReportsOutOfRangeIntegerLiteral()
    {
        var tokens = Lex("9223372036854775808", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0004", diagnostic.Code);
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TokenKind.LongLiteralToken, tokens[0].Kind);
    }

    [Fact]
    public void Lex_ReportsOutOfRangeDoubleLiteral()
    {
        var text = new string('9', 400) + ".0";
        var tokens = Lex(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0004", diagnostic.Code);
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TokenKind.DoubleLiteralToken, tokens[0].Kind);
    }

    [Fact]
    public void Lex_RecognizesStringLiteralAndEscapedQuotes()
    {
        var tokens = Lex("\"He said \"\"Hi\"\"\"", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.StringLiteralToken, tokens[0].Kind);
        Assert.Equal("\"He said \"\"Hi\"\"\"", tokens[0].Text);
        Assert.Equal("He said \"Hi\"", Assert.IsType<string>(tokens[0].Value));
    }

    [Fact]
    public void Lex_RecognizesQuoteAndRemComments()
    {
        var tokens = Lex("' first\r\nREM second\r\nReM third\r\nvalue", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertTokenKinds(
            tokens,
            TokenKind.CommentToken,
            TokenKind.NewLineToken,
            TokenKind.CommentToken,
            TokenKind.NewLineToken,
            TokenKind.CommentToken,
            TokenKind.NewLineToken,
            TokenKind.IdentifierToken,
            TokenKind.EndOfFileToken);
    }

    [Fact]
    public void Lex_RecognizesComparisonOperatorsAndPunctuation()
    {
        var tokens = Lex("< <= <> > >= = + - * / \\ ( ) , ;", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertTokenKinds(
            tokens,
            TokenKind.LessToken,
            TokenKind.LessOrEqualsToken,
            TokenKind.NotEqualsToken,
            TokenKind.GreaterToken,
            TokenKind.GreaterOrEqualsToken,
            TokenKind.EqualsToken,
            TokenKind.PlusToken,
            TokenKind.MinusToken,
            TokenKind.StarToken,
            TokenKind.SlashToken,
            TokenKind.BackslashToken,
            TokenKind.OpenParenToken,
            TokenKind.CloseParenToken,
            TokenKind.CommaToken,
            TokenKind.SemicolonToken,
            TokenKind.EndOfFileToken);
    }

    [Fact]
    public void Lex_RecognizesExtendedArithmeticOperators()
    {
        var tokens = Lex("^ << >> XOR EQV IMP", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertTokenKinds(
            tokens,
            TokenKind.CaretToken,
            TokenKind.LessLessToken,
            TokenKind.GreaterGreaterToken,
            TokenKind.XorKeyword,
            TokenKind.EqvKeyword,
            TokenKind.ImpKeyword,
            TokenKind.EndOfFileToken);
    }

    [Fact]
    public void Lex_ReportsHexLiteralWithoutDigits()
    {
        var tokens = Lex("&H", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0003", diagnostic.Code);
        Assert.Equal(TokenKind.IntegerLiteralToken, tokens[0].Kind);
    }

    [Fact]
    public void Lex_ReportsUnterminatedString()
    {
        var tokens = Lex("\"unterminated", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0002", diagnostic.Code);
        Assert.Equal(TokenKind.StringLiteralToken, tokens[0].Kind);
        Assert.Equal("unterminated", Assert.IsType<string>(tokens[0].Value));
    }

    [Fact]
    public void Lex_ReportsUnknownCharacter()
    {
        var tokens = Lex("@", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MB0001", diagnostic.Code);
        Assert.Equal(TokenKind.BadToken, tokens[0].Kind);
        Assert.Equal("@", tokens[0].Text);
    }

    private static IReadOnlyList<Token> Lex(string text, out DiagnosticBag diagnostics)
    {
        return Lexer.Lex(text, out diagnostics);
    }

    private static IReadOnlyList<Token> TokensWithoutEndOfFile(IReadOnlyList<Token> tokens)
    {
        return tokens.Where(static token => token.Kind != TokenKind.EndOfFileToken).ToArray();
    }

    private static void AssertTokenKinds(IReadOnlyList<Token> tokens, params TokenKind[] expectedKinds)
    {
        Assert.Equal(expectedKinds, tokens.Select(static token => token.Kind));
    }
}
