using Axiom.Core;

namespace Axiom.Syntax;

public abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }
}

public abstract class MemberSyntax : SyntaxNode;

public abstract class StatementSyntax : SyntaxNode;

public abstract class ExpressionSyntax : SyntaxNode;

public abstract class TypeSyntax : SyntaxNode;

public sealed class SyntaxToken : SyntaxNode
{
    public SyntaxToken(SyntaxKind kind, string text, TextLocation location, object? value = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        Kind = kind;
        Text = text;
        Location = location;
        Value = value;
    }

    public override SyntaxKind Kind { get; }

    public string Text { get; }

    public TextLocation Location { get; }

    public TextSpan Span => Location.Span;

    public object? Value { get; }

    public override string ToString() => $"{Kind}: '{Text}'";
}
