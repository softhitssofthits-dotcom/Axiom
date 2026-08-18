using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Syntax.Tests;

public sealed class SyntaxTokenTests
{
    private static TextLocation MakeLocation(int start = 0, int length = 5)
    {
        return new TextLocation(new TextSpan(start, length), line: 1, column: start + 1);
    }

    [Fact]
    public void Constructor_SetsKind()
    {
        var token = new SyntaxToken(SyntaxKind.IdentifierToken, "x", MakeLocation());
        Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
    }

    [Fact]
    public void Constructor_SetsText()
    {
        var token = new SyntaxToken(SyntaxKind.IntegerLiteralToken, "42", MakeLocation());
        Assert.Equal("42", token.Text);
    }

    [Fact]
    public void Constructor_SetsLocation()
    {
        var location = MakeLocation(10, 3);
        var token = new SyntaxToken(SyntaxKind.PlusToken, "+", location);
        Assert.Equal(location, token.Location);
    }

    [Fact]
    public void Constructor_SetsValue()
    {
        var token = new SyntaxToken(SyntaxKind.IntegerLiteralToken, "42", MakeLocation(), 42);
        Assert.Equal(42, token.Value);
    }

    [Fact]
    public void Constructor_NullValue_IsAllowed()
    {
        var token = new SyntaxToken(SyntaxKind.IdentifierToken, "x", MakeLocation(), null);
        Assert.Null(token.Value);
    }

    [Fact]
    public void Span_ReturnsLocationSpan()
    {
        var location = MakeLocation(7, 4);
        var token = new SyntaxToken(SyntaxKind.StringLiteralToken, "test", location);
        Assert.Equal(location.Span, token.Span);
    }

    [Fact]
    public void ToString_ReturnsKindAndText()
    {
        var token = new SyntaxToken(SyntaxKind.PlusToken, "+", MakeLocation());
        Assert.Equal("PlusToken: '+'", token.ToString());
    }

    [Fact]
    public void Constructor_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SyntaxToken(SyntaxKind.IdentifierToken, null!, MakeLocation()));
    }
}
