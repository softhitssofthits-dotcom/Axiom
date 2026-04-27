namespace Axiom.Syntax;

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax(SyntaxToken literalToken)
    {
        ArgumentNullException.ThrowIfNull(literalToken);
        LiteralToken = literalToken;
    }

    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

    public SyntaxToken LiteralToken { get; }
}

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public NameExpressionSyntax(SyntaxToken identifierToken)
    {
        ArgumentNullException.ThrowIfNull(identifierToken);
        IdentifierToken = identifierToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NameExpression;

    public SyntaxToken IdentifierToken { get; }
}

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        ArgumentNullException.ThrowIfNull(operatorToken);
        ArgumentNullException.ThrowIfNull(operand);

        OperatorToken = operatorToken;
        Operand = operand;
    }

    public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

    public SyntaxToken OperatorToken { get; }

    public ExpressionSyntax Operand { get; }
}

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(operatorToken);
        ArgumentNullException.ThrowIfNull(right);

        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public ExpressionSyntax Left { get; }

    public SyntaxToken OperatorToken { get; }

    public ExpressionSyntax Right { get; }
}

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public ParenthesizedExpressionSyntax(SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        OpenParenToken = openParenToken;
        Expression = expression;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

    public SyntaxToken OpenParenToken { get; }

    public ExpressionSyntax Expression { get; }

    public SyntaxToken CloseParenToken { get; }
}

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public CallExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken openParenToken,
        IReadOnlyList<ExpressionSyntax> arguments,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        Target = target;
        OpenParenToken = openParenToken;
        Arguments = arguments;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CallExpression;

    public ExpressionSyntax Target { get; }

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<ExpressionSyntax> Arguments { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseParenToken { get; }
}

public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
{
    public ArrayAccessExpressionSyntax(SyntaxToken identifier, SyntaxToken openParenToken, ExpressionSyntax index, SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        Identifier = identifier;
        OpenParenToken = openParenToken;
        Index = index;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayAccessExpression;

    public SyntaxToken Identifier { get; }

    public SyntaxToken OpenParenToken { get; }

    public ExpressionSyntax Index { get; }

    public SyntaxToken CloseParenToken { get; }
}

public sealed class FieldAccessExpressionSyntax : ExpressionSyntax
{
    public FieldAccessExpressionSyntax(ExpressionSyntax target, SyntaxToken dotToken, SyntaxToken identifier)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dotToken);
        ArgumentNullException.ThrowIfNull(identifier);

        Target = target;
        DotToken = dotToken;
        Identifier = identifier;
    }

    public override SyntaxKind Kind => SyntaxKind.FieldAccessExpression;

    public ExpressionSyntax Target { get; }

    public SyntaxToken DotToken { get; }

    public SyntaxToken Identifier { get; }
}

public sealed class ErrorExpressionSyntax : ExpressionSyntax
{
    public ErrorExpressionSyntax(SyntaxToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        Token = token;
    }

    public override SyntaxKind Kind => SyntaxKind.ErrorExpression;

    public SyntaxToken Token { get; }
}
