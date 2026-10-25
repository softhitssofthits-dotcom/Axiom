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

public sealed class RangeExpressionSyntax : ExpressionSyntax
{
    public RangeExpressionSyntax(ExpressionSyntax lowerBound, SyntaxToken toKeyword, ExpressionSyntax upperBound)
    {
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(toKeyword);
        ArgumentNullException.ThrowIfNull(upperBound);

        LowerBound = lowerBound;
        ToKeyword = toKeyword;
        UpperBound = upperBound;
    }

    public override SyntaxKind Kind => SyntaxKind.RangeExpression;

    public ExpressionSyntax LowerBound { get; }

    public SyntaxToken ToKeyword { get; }

    public ExpressionSyntax UpperBound { get; }
}

/// <summary>
/// Аргумент вида <c>STEP 1</c> или <c>STEP -1</c> в списке аргументов вызова.
/// Разбирается для любого вызова, а допустимость формы решает связыватель:
/// так же устроен разбор диапазона <c>TO</c> внутри аргументов.
/// </summary>
public sealed class StepDirectionArgumentExpressionSyntax : ExpressionSyntax
{
    public StepDirectionArgumentExpressionSyntax(SyntaxToken stepKeyword, ExpressionSyntax value)
    {
        ArgumentNullException.ThrowIfNull(stepKeyword);
        ArgumentNullException.ThrowIfNull(value);

        StepKeyword = stepKeyword;
        Value = value;
    }

    public override SyntaxKind Kind => SyntaxKind.StepDirectionArgumentExpression;

    public SyntaxToken StepKeyword { get; }

    public ExpressionSyntax Value { get; }
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

public sealed class NamedArgumentExpressionSyntax : ExpressionSyntax
{
    public NamedArgumentExpressionSyntax(SyntaxToken identifierToken, SyntaxToken colonEqualsToken, ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(identifierToken);
        ArgumentNullException.ThrowIfNull(colonEqualsToken);
        ArgumentNullException.ThrowIfNull(expression);

        IdentifierToken = identifierToken;
        ColonEqualsToken = colonEqualsToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.NamedArgumentExpression;

    public SyntaxToken IdentifierToken { get; }

    public SyntaxToken ColonEqualsToken { get; }

    public ExpressionSyntax Expression { get; }
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
    public ArrayAccessExpressionSyntax(
        ExpressionSyntax target,
        SyntaxToken openBracketToken,
        IReadOnlyList<ExpressionSyntax> indices,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeBracketToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(openBracketToken);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeBracketToken);

        Target = target;
        OpenBracketToken = openBracketToken;
        Indices = indices;
        CommaTokens = commaTokens;
        CloseBracketToken = closeBracketToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayAccessExpression;

    public ExpressionSyntax Target { get; }

    public SyntaxToken OpenBracketToken { get; }

    public IReadOnlyList<ExpressionSyntax> Indices { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public SyntaxToken CloseBracketToken { get; }

    public bool IsWholeArray => Indices.Count == 0;
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

public abstract class InterpolatedStringPartSyntax : SyntaxNode
{
}

public sealed class InterpolatedStringTextPartSyntax : InterpolatedStringPartSyntax
{
    public InterpolatedStringTextPartSyntax(SyntaxToken textToken)
    {
        ArgumentNullException.ThrowIfNull(textToken);
        TextToken = textToken;
    }

    public override SyntaxKind Kind => SyntaxKind.InterpolatedStringTextPart;

    public SyntaxToken TextToken { get; }
}

public sealed class InterpolatedStringExpressionPartSyntax : InterpolatedStringPartSyntax
{
    public InterpolatedStringExpressionPartSyntax(
        SyntaxToken openBraceToken,
        ExpressionSyntax expression,
        SyntaxToken? commaToken,
        SyntaxToken? alignmentToken,
        SyntaxToken? colonToken,
        SyntaxToken? formatSpecifierToken,
        SyntaxToken closeBraceToken)
    {
        ArgumentNullException.ThrowIfNull(openBraceToken);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(closeBraceToken);

        OpenBraceToken = openBraceToken;
        Expression = expression;
        CommaToken = commaToken;
        AlignmentToken = alignmentToken;
        ColonToken = colonToken;
        FormatSpecifierToken = formatSpecifierToken;
        CloseBraceToken = closeBraceToken;
    }

    public override SyntaxKind Kind => SyntaxKind.InterpolatedStringExpressionPart;

    public SyntaxToken OpenBraceToken { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken? CommaToken { get; }
    public SyntaxToken? AlignmentToken { get; }
    public SyntaxToken? ColonToken { get; }
    public SyntaxToken? FormatSpecifierToken { get; }
    public SyntaxToken CloseBraceToken { get; }
}

public sealed class InterpolatedStringExpressionSyntax : ExpressionSyntax
{
    public InterpolatedStringExpressionSyntax(
        SyntaxToken startToken,
        IReadOnlyList<InterpolatedStringPartSyntax> parts,
        SyntaxToken endToken)
    {
        ArgumentNullException.ThrowIfNull(startToken);
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(endToken);

        StartToken = startToken;
        Parts = parts;
        EndToken = endToken;
    }

    public override SyntaxKind Kind => SyntaxKind.InterpolatedStringExpression;

    public SyntaxToken StartToken { get; }
    public IReadOnlyList<InterpolatedStringPartSyntax> Parts { get; }
    public SyntaxToken EndToken { get; }
}
