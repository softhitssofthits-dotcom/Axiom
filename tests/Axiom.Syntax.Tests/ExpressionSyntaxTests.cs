using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Syntax.Tests;

public sealed class ExpressionSyntaxTests
{
    private static SyntaxToken Token(SyntaxKind kind, string text = "x")
    {
        return new SyntaxToken(kind, text, new TextLocation(new TextSpan(0, text.Length), line: 1, column: 1));
    }

    [Fact]
    public void LiteralExpression_HasCorrectKind()
    {
        var literal = Token(SyntaxKind.IntegerLiteralToken, "42");
        var expr = new LiteralExpressionSyntax(literal);
        Assert.Equal(SyntaxKind.LiteralExpression, expr.Kind);
        Assert.Same(literal, expr.LiteralToken);
    }

    [Fact]
    public void NameExpression_HasCorrectKind()
    {
        var identifier = Token(SyntaxKind.IdentifierToken, "myVar");
        var expr = new NameExpressionSyntax(identifier);
        Assert.Equal(SyntaxKind.NameExpression, expr.Kind);
        Assert.Same(identifier, expr.IdentifierToken);
    }

    [Fact]
    public void UnaryExpression_HasCorrectKind()
    {
        var op = Token(SyntaxKind.MinusToken, "-");
        var operand = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var expr = new UnaryExpressionSyntax(op, operand);
        Assert.Equal(SyntaxKind.UnaryExpression, expr.Kind);
        Assert.Same(op, expr.OperatorToken);
        Assert.Same(operand, expr.Operand);
    }

    [Fact]
    public void BinaryExpression_HasCorrectKind()
    {
        var left = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var op = Token(SyntaxKind.PlusToken, "+");
        var right = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "2"));
        var expr = new BinaryExpressionSyntax(left, op, right);
        Assert.Equal(SyntaxKind.BinaryExpression, expr.Kind);
        Assert.Same(left, expr.Left);
        Assert.Same(op, expr.OperatorToken);
        Assert.Same(right, expr.Right);
    }

    [Fact]
    public void ParenthesizedExpression_HasCorrectKind()
    {
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var inner = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var expr = new ParenthesizedExpressionSyntax(open, inner, close);
        Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind);
        Assert.Same(open, expr.OpenParenToken);
        Assert.Same(inner, expr.Expression);
        Assert.Same(close, expr.CloseParenToken);
    }

    [Fact]
    public void CallExpression_HasCorrectKind()
    {
        var target = new NameExpressionSyntax(Token(SyntaxKind.IdentifierToken, "PRINT"));
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var arg = new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"hello\""));
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var expr = new CallExpressionSyntax(target, open, [arg], [], close);
        Assert.Equal(SyntaxKind.CallExpression, expr.Kind);
        Assert.Same(target, expr.Target);
        Assert.Single(expr.Arguments);
    }

    [Fact]
    public void ArrayAccessExpression_HasCorrectKindAndTracksWholeArray()
    {
        var target = new NameExpressionSyntax(Token(SyntaxKind.IdentifierToken, "fruits"));
        var open = Token(SyntaxKind.OpenBracketToken, "[");
        var close = Token(SyntaxKind.CloseBracketToken, "]");
        var expr = new ArrayAccessExpressionSyntax(target, open, [], [], close);

        Assert.Equal(SyntaxKind.ArrayAccessExpression, expr.Kind);
        Assert.Same(target, expr.Target);
        Assert.Same(open, expr.OpenBracketToken);
        Assert.Empty(expr.Indices);
        Assert.Empty(expr.CommaTokens);
        Assert.Same(close, expr.CloseBracketToken);
        Assert.True(expr.IsWholeArray);
    }

    [Fact]
    public void ArrayAccessExpression_WithIndicesIsNotWholeArray()
    {
        var target = new NameExpressionSyntax(Token(SyntaxKind.IdentifierToken, "grid"));
        var open = Token(SyntaxKind.OpenBracketToken, "[");
        var first = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "2"));
        var comma = Token(SyntaxKind.CommaToken, ",");
        var second = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "3"));
        var close = Token(SyntaxKind.CloseBracketToken, "]");
        var expr = new ArrayAccessExpressionSyntax(target, open, [first, second], [comma], close);

        Assert.Equal(2, expr.Indices.Count);
        Assert.Single(expr.CommaTokens);
        Assert.False(expr.IsWholeArray);
    }

    [Fact]
    public void ArrayLiteralExpression_HasCorrectKindAndTracksEmptySet()
    {
        var open = Token(SyntaxKind.OpenBracketToken, "[");
        var close = Token(SyntaxKind.CloseBracketToken, "]");
        var expr = new ArrayLiteralExpressionSyntax(open, [], [], close);

        Assert.Equal(SyntaxKind.ArrayLiteralExpression, expr.Kind);
        Assert.Same(open, expr.OpenBracketToken);
        Assert.Same(close, expr.CloseBracketToken);
        Assert.Empty(expr.Items);
        Assert.Empty(expr.CommaTokens);
        Assert.True(expr.IsEmpty);
    }

    [Fact]
    public void ArrayLiteralExpression_KeepsItemsAndCommas()
    {
        var open = Token(SyntaxKind.OpenBracketToken, "[");
        var first = new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"яблоко\""));
        var comma = Token(SyntaxKind.CommaToken, ",");
        var second = new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"банан\""));
        var close = Token(SyntaxKind.CloseBracketToken, "]");
        var expr = new ArrayLiteralExpressionSyntax(open, [first, second], [comma], close);

        Assert.Equal(2, expr.Items.Count);
        Assert.Single(expr.CommaTokens);
        Assert.False(expr.IsEmpty);
    }

    [Fact]
    public void FieldAccessExpression_HasCorrectKind()
    {
        var target = new NameExpressionSyntax(Token(SyntaxKind.IdentifierToken, "obj"));
        var dot = Token(SyntaxKind.DotToken, ".");
        var field = Token(SyntaxKind.IdentifierToken, "Name");
        var expr = new FieldAccessExpressionSyntax(target, dot, field);
        Assert.Equal(SyntaxKind.FieldAccessExpression, expr.Kind);
    }

    [Fact]
    public void ErrorExpression_HasCorrectKind()
    {
        var token = Token(SyntaxKind.BadToken, "???");
        var expr = new ErrorExpressionSyntax(token);
        Assert.Equal(SyntaxKind.ErrorExpression, expr.Kind);
    }

    [Fact]
    public void RangeExpression_HasCorrectKind()
    {
        var lower = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var to = Token(SyntaxKind.ToKeyword, "TO");
        var upper = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "10"));
        var expr = new RangeExpressionSyntax(lower, to, upper);
        Assert.Equal(SyntaxKind.RangeExpression, expr.Kind);
    }

    [Fact]
    public void NamedArgumentExpression_HasCorrectKind()
    {
        var id = Token(SyntaxKind.IdentifierToken, "x");
        var ce = Token(SyntaxKind.ColonEqualsToken, ":=");
        var value = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "5"));
        var expr = new NamedArgumentExpressionSyntax(id, ce, value);
        Assert.Equal(SyntaxKind.NamedArgumentExpression, expr.Kind);
    }

    [Fact]
    public void LiteralExpression_NullToken_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LiteralExpressionSyntax(null!));
    }

    [Fact]
    public void BinaryExpression_NullLeft_Throws()
    {
        var op = Token(SyntaxKind.PlusToken, "+");
        var right = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        Assert.Throws<ArgumentNullException>(() => new BinaryExpressionSyntax(null!, op, right));
    }

    [Fact]
    public void BinaryExpression_NullOperator_Throws()
    {
        var left = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var right = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "2"));
        Assert.Throws<ArgumentNullException>(() => new BinaryExpressionSyntax(left, null!, right));
    }

    [Fact]
    public void BinaryExpression_NullRight_Throws()
    {
        var left = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var op = Token(SyntaxKind.PlusToken, "+");
        Assert.Throws<ArgumentNullException>(() => new BinaryExpressionSyntax(left, op, null!));
    }
}
