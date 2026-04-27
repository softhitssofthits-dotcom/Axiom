using Axiom.Core;

namespace Axiom.Lexing;

public sealed class Token
{
    public Token(TokenKind kind, string text, TextLocation location, object? value = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        Kind = kind;
        Text = text;
        Location = location;
        Value = value;
    }

    public TokenKind Kind { get; }

    public string Text { get; }

    public TextSpan Span => Location.Span;

    public TextLocation Location { get; }

    public object? Value { get; }

    public override string ToString() => $"{Kind}: '{Text}'";
}
