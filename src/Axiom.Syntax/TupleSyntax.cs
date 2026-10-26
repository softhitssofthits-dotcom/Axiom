namespace Axiom.Syntax;

public sealed class TupleTypeSyntax(SyntaxToken openParenToken,
    IReadOnlyList<SyntaxToken?> names, IReadOnlyList<TypeSyntax> elementTypes,
    SyntaxToken closeParenToken) : TypeSyntax
{
    public override SyntaxKind Kind => SyntaxKind.TupleType;
    public SyntaxToken OpenParenToken { get; } = openParenToken;
    public IReadOnlyList<SyntaxToken?> Names { get; } = names;
    public IReadOnlyList<TypeSyntax> ElementTypes { get; } = elementTypes;
    public SyntaxToken CloseParenToken { get; } = closeParenToken;
}

public sealed class TupleExpressionSyntax(SyntaxToken openParenToken,
    IReadOnlyList<ExpressionSyntax> elements, SyntaxToken closeParenToken) : ExpressionSyntax
{
    public override SyntaxKind Kind => SyntaxKind.TupleExpression;
    public SyntaxToken OpenParenToken { get; } = openParenToken;
    public IReadOnlyList<ExpressionSyntax> Elements { get; } = elements;
    public SyntaxToken CloseParenToken { get; } = closeParenToken;
}

public sealed class TupleUnpackingStatementSyntax(SyntaxToken? dimKeyword,
    TupleExpressionSyntax targets, SyntaxToken equalsToken, ExpressionSyntax source) : StatementSyntax
{
    public override SyntaxKind Kind => SyntaxKind.TupleUnpackingStatement;
    public SyntaxToken? DimKeyword { get; } = dimKeyword;
    public TupleExpressionSyntax Targets { get; } = targets;
    public SyntaxToken EqualsToken { get; } = equalsToken;
    public ExpressionSyntax Source { get; } = source;
}
