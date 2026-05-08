namespace Axiom.Syntax;

public sealed class ArrayDimensionSyntax : SyntaxNode
{
    public ArrayDimensionSyntax(
        ExpressionSyntax? startExpression,
        SyntaxToken? toKeyword,
        ExpressionSyntax? endExpression)
    {
        StartExpression = startExpression;
        ToKeyword = toKeyword;
        EndExpression = endExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayBounds;

    public ExpressionSyntax? StartExpression { get; }

    public SyntaxToken? ToKeyword { get; }

    public ExpressionSyntax? EndExpression { get; }

    public bool IsDynamic => StartExpression is null && ToKeyword is null && EndExpression is null;
}

public sealed class ArrayBoundsSyntax : SyntaxNode
{
    public ArrayBoundsSyntax(
        SyntaxToken openParenToken,
        IReadOnlyList<ArrayDimensionSyntax> dimensions,
        IReadOnlyList<SyntaxToken> commaTokens,
        SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        OpenParenToken = openParenToken;
        Dimensions = dimensions;
        CommaTokens = commaTokens;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayBounds;

    public SyntaxToken OpenParenToken { get; }

    public IReadOnlyList<ArrayDimensionSyntax> Dimensions { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public ExpressionSyntax? StartExpression => Dimensions.Count == 0 ? null : Dimensions[0].StartExpression;

    public SyntaxToken? ToKeyword => Dimensions.Count == 0 ? null : Dimensions[0].ToKeyword;

    public ExpressionSyntax? EndExpression => Dimensions.Count == 0 ? null : Dimensions[0].EndExpression;

    public SyntaxToken CloseParenToken { get; }

    public int Rank => Dimensions.Count;

    public bool IsDynamic => Dimensions.Count > 0 && Dimensions.All(static dimension => dimension.IsDynamic);

    public bool HasMixedDimensions => Dimensions.Any(static dimension => dimension.IsDynamic)
        && Dimensions.Any(static dimension => !dimension.IsDynamic);
}

public sealed class VariableDeclarationSyntax : SyntaxNode
{
    public VariableDeclarationSyntax(
        IReadOnlyList<VariableDeclaratorSyntax> declarators,
        IReadOnlyList<SyntaxToken> commaTokens,
        TypeClauseSyntax? typeClause,
        SyntaxToken? equalsToken,
        ExpressionSyntax? initializer)
    {
        ArgumentNullException.ThrowIfNull(declarators);
        ArgumentNullException.ThrowIfNull(commaTokens);

        Declarators = declarators;
        CommaTokens = commaTokens;
        TypeClause = typeClause;
        EqualsToken = equalsToken;
        Initializer = initializer;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;

    public IReadOnlyList<VariableDeclaratorSyntax> Declarators { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public TypeClauseSyntax? TypeClause { get; }

    public SyntaxToken? EqualsToken { get; }

    public ExpressionSyntax? Initializer { get; }
}

public sealed class VariableDeclaratorSyntax : SyntaxNode
{
    public VariableDeclaratorSyntax(SyntaxToken identifier, ArrayBoundsSyntax? arrayBounds)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        Identifier = identifier;
        ArrayBounds = arrayBounds;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclarator;

    public SyntaxToken Identifier { get; }

    public ArrayBoundsSyntax? ArrayBounds { get; }
}

public sealed class ConstantDeclarationSyntax : SyntaxNode
{
    public ConstantDeclarationSyntax(
        SyntaxToken identifier,
        TypeClauseSyntax typeClause,
        SyntaxToken equalsToken,
        ExpressionSyntax initializer)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(typeClause);
        ArgumentNullException.ThrowIfNull(equalsToken);
        ArgumentNullException.ThrowIfNull(initializer);

        Identifier = identifier;
        TypeClause = typeClause;
        EqualsToken = equalsToken;
        Initializer = initializer;
    }

    public override SyntaxKind Kind => SyntaxKind.ConstantDeclaration;

    public SyntaxToken Identifier { get; }

    public TypeClauseSyntax TypeClause { get; }

    public SyntaxToken EqualsToken { get; }

    public ExpressionSyntax Initializer { get; }
}

public sealed class ElseClauseSyntax : SyntaxNode
{
    public ElseClauseSyntax(SyntaxToken elseKeyword, IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(elseKeyword);
        ArgumentNullException.ThrowIfNull(statements);

        ElseKeyword = elseKeyword;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.ElseClause;

    public SyntaxToken ElseKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public sealed class ElseIfClauseSyntax : SyntaxNode
{
    public ElseIfClauseSyntax(
        SyntaxToken elseIfKeyword,
        ExpressionSyntax condition,
        SyntaxToken thenKeyword,
        IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(elseIfKeyword);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenKeyword);
        ArgumentNullException.ThrowIfNull(statements);

        ElseIfKeyword = elseIfKeyword;
        Condition = condition;
        ThenKeyword = thenKeyword;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.ElseIfClause;

    public SyntaxToken ElseIfKeyword { get; }

    public ExpressionSyntax Condition { get; }

    public SyntaxToken ThenKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public sealed class CatchClauseSyntax : SyntaxNode
{
    public CatchClauseSyntax(SyntaxToken catchKeyword, SyntaxToken identifier, IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(catchKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(statements);

        CatchKeyword = catchKeyword;
        Identifier = identifier;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.CatchClause;

    public SyntaxToken CatchKeyword { get; }

    public SyntaxToken Identifier { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public sealed class FinallyClauseSyntax : SyntaxNode
{
    public FinallyClauseSyntax(SyntaxToken finallyKeyword, IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(finallyKeyword);
        ArgumentNullException.ThrowIfNull(statements);

        FinallyKeyword = finallyKeyword;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.FinallyClause;

    public SyntaxToken FinallyKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public sealed class StepClauseSyntax : SyntaxNode
{
    public StepClauseSyntax(SyntaxToken stepKeyword, ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(stepKeyword);
        ArgumentNullException.ThrowIfNull(expression);

        StepKeyword = stepKeyword;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.StepClause;

    public SyntaxToken StepKeyword { get; }

    public ExpressionSyntax Expression { get; }
}

public sealed class DoLoopConditionClauseSyntax : SyntaxNode
{
    public DoLoopConditionClauseSyntax(SyntaxToken keyword, ExpressionSyntax condition)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        ArgumentNullException.ThrowIfNull(condition);

        Keyword = keyword;
        Condition = condition;
    }

    public override SyntaxKind Kind => SyntaxKind.DoLoopConditionClause;

    public SyntaxToken Keyword { get; }

    public ExpressionSyntax Condition { get; }
}

public sealed class LocalSubDeclarationStatementSyntax : StatementSyntax
{
    public LocalSubDeclarationStatementSyntax(SubDeclarationSyntax declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        Declaration = declaration;
    }

    public override SyntaxKind Kind => SyntaxKind.LocalSubDeclarationStatement;

    public SubDeclarationSyntax Declaration { get; }
}

public sealed class LocalFunctionDeclarationStatementSyntax : StatementSyntax
{
    public LocalFunctionDeclarationStatementSyntax(FunctionDeclarationSyntax declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        Declaration = declaration;
    }

    public override SyntaxKind Kind => SyntaxKind.LocalFunctionDeclarationStatement;

    public FunctionDeclarationSyntax Declaration { get; }
}

public sealed class AssignmentStatementSyntax : StatementSyntax
{
    public AssignmentStatementSyntax(ExpressionSyntax target, SyntaxToken equalsToken, ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(equalsToken);
        ArgumentNullException.ThrowIfNull(expression);

        Target = target;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;

    public ExpressionSyntax Target { get; }

    public SyntaxToken EqualsToken { get; }

    public ExpressionSyntax Expression { get; }
}

public sealed class EraseStatementSyntax : StatementSyntax
{
    public EraseStatementSyntax(SyntaxToken eraseKeyword, ExpressionSyntax target)
    {
        ArgumentNullException.ThrowIfNull(eraseKeyword);
        ArgumentNullException.ThrowIfNull(target);

        EraseKeyword = eraseKeyword;
        Target = target;
    }

    public override SyntaxKind Kind => SyntaxKind.EraseStatement;

    public SyntaxToken EraseKeyword { get; }

    public ExpressionSyntax Target { get; }

    public SyntaxToken? Identifier => (Target as NameExpressionSyntax)?.IdentifierToken;
}

public sealed class RedimStatementSyntax : StatementSyntax
{
    public RedimStatementSyntax(
        SyntaxToken redimKeyword,
        SyntaxToken? preserveKeyword,
        ExpressionSyntax target,
        ArrayBoundsSyntax arrayBounds)
    {
        ArgumentNullException.ThrowIfNull(redimKeyword);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(arrayBounds);

        RedimKeyword = redimKeyword;
        PreserveKeyword = preserveKeyword;
        Target = target;
        ArrayBounds = arrayBounds;
    }

    public override SyntaxKind Kind => SyntaxKind.RedimStatement;

    public SyntaxToken RedimKeyword { get; }

    public SyntaxToken? PreserveKeyword { get; }

    public ExpressionSyntax Target { get; }

    public SyntaxToken? Identifier => (Target as NameExpressionSyntax)?.IdentifierToken;

    public ArrayBoundsSyntax ArrayBounds { get; }

    public bool IsPreserve => PreserveKeyword is not null;
}

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(
        SyntaxToken ifKeyword,
        ExpressionSyntax condition,
        SyntaxToken thenKeyword,
        IReadOnlyList<StatementSyntax> statements,
        IReadOnlyList<ElseIfClauseSyntax> elseIfClauses,
        ElseClauseSyntax? elseClause,
        SyntaxToken endKeyword,
        SyntaxToken endIfKeyword)
    {
        ArgumentNullException.ThrowIfNull(ifKeyword);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(thenKeyword);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(elseIfClauses);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endIfKeyword);

        IfKeyword = ifKeyword;
        Condition = condition;
        ThenKeyword = thenKeyword;
        Statements = statements;
        ElseIfClauses = elseIfClauses;
        ElseClause = elseClause;
        EndKeyword = endKeyword;
        EndIfKeyword = endIfKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.IfStatement;

    public SyntaxToken IfKeyword { get; }

    public ExpressionSyntax Condition { get; }

    public SyntaxToken ThenKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public IReadOnlyList<ElseIfClauseSyntax> ElseIfClauses { get; }

    public ElseClauseSyntax? ElseClause { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndIfKeyword { get; }
}

public sealed class ForStatementSyntax : StatementSyntax
{
    public ForStatementSyntax(
        SyntaxToken forKeyword,
        SyntaxToken identifier,
        SyntaxToken equalsToken,
        ExpressionSyntax startExpression,
        SyntaxToken toKeyword,
        ExpressionSyntax endExpression,
        StepClauseSyntax? stepClause,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken nextKeyword)
    {
        ArgumentNullException.ThrowIfNull(forKeyword);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(equalsToken);
        ArgumentNullException.ThrowIfNull(startExpression);
        ArgumentNullException.ThrowIfNull(toKeyword);
        ArgumentNullException.ThrowIfNull(endExpression);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(nextKeyword);

        ForKeyword = forKeyword;
        Identifier = identifier;
        EqualsToken = equalsToken;
        StartExpression = startExpression;
        ToKeyword = toKeyword;
        EndExpression = endExpression;
        StepClause = stepClause;
        Statements = statements;
        NextKeyword = nextKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ForStatement;

    public SyntaxToken ForKeyword { get; }

    public SyntaxToken Identifier { get; }

    public SyntaxToken EqualsToken { get; }

    public ExpressionSyntax StartExpression { get; }

    public SyntaxToken ToKeyword { get; }

    public ExpressionSyntax EndExpression { get; }

    public StepClauseSyntax? StepClause { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public SyntaxToken NextKeyword { get; }
}

public sealed class WhileStatementSyntax : StatementSyntax
{
    public WhileStatementSyntax(
        SyntaxToken whileKeyword,
        ExpressionSyntax condition,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken endKeyword,
        SyntaxToken endWhileKeyword)
    {
        ArgumentNullException.ThrowIfNull(whileKeyword);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endWhileKeyword);

        WhileKeyword = whileKeyword;
        Condition = condition;
        Statements = statements;
        EndKeyword = endKeyword;
        EndWhileKeyword = endWhileKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.WhileStatement;

    public SyntaxToken WhileKeyword { get; }

    public ExpressionSyntax Condition { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndWhileKeyword { get; }
}

public sealed class DoLoopStatementSyntax : StatementSyntax
{
    public DoLoopStatementSyntax(
        SyntaxToken doKeyword,
        DoLoopConditionClauseSyntax? leadingCondition,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken loopKeyword,
        DoLoopConditionClauseSyntax? trailingCondition)
    {
        ArgumentNullException.ThrowIfNull(doKeyword);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(loopKeyword);

        DoKeyword = doKeyword;
        LeadingCondition = leadingCondition;
        Statements = statements;
        LoopKeyword = loopKeyword;
        TrailingCondition = trailingCondition;
    }

    public override SyntaxKind Kind => SyntaxKind.DoLoopStatement;

    public SyntaxToken DoKeyword { get; }

    public DoLoopConditionClauseSyntax? LeadingCondition { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public SyntaxToken LoopKeyword { get; }

    public DoLoopConditionClauseSyntax? TrailingCondition { get; }
}

public sealed class TryStatementSyntax : StatementSyntax
{
    public TryStatementSyntax(
        SyntaxToken tryKeyword,
        IReadOnlyList<StatementSyntax> statements,
        CatchClauseSyntax? catchClause,
        FinallyClauseSyntax? finallyClause,
        SyntaxToken endKeyword,
        SyntaxToken endTryKeyword)
    {
        ArgumentNullException.ThrowIfNull(tryKeyword);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endTryKeyword);

        TryKeyword = tryKeyword;
        Statements = statements;
        CatchClause = catchClause;
        FinallyClause = finallyClause;
        EndKeyword = endKeyword;
        EndTryKeyword = endTryKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.TryStatement;

    public SyntaxToken TryKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }

    public CatchClauseSyntax? CatchClause { get; }

    public FinallyClauseSyntax? FinallyClause { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndTryKeyword { get; }
}

public sealed class ExitForStatementSyntax : StatementSyntax
{
    public ExitForStatementSyntax(SyntaxToken exitKeyword, SyntaxToken forKeyword)
    {
        ArgumentNullException.ThrowIfNull(exitKeyword);
        ArgumentNullException.ThrowIfNull(forKeyword);

        ExitKeyword = exitKeyword;
        ForKeyword = forKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ExitForStatement;

    public SyntaxToken ExitKeyword { get; }

    public SyntaxToken ForKeyword { get; }
}

public sealed class ExitDoStatementSyntax : StatementSyntax
{
    public ExitDoStatementSyntax(SyntaxToken exitKeyword, SyntaxToken doKeyword)
    {
        ArgumentNullException.ThrowIfNull(exitKeyword);
        ArgumentNullException.ThrowIfNull(doKeyword);

        ExitKeyword = exitKeyword;
        DoKeyword = doKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ExitDoStatement;

    public SyntaxToken ExitKeyword { get; }

    public SyntaxToken DoKeyword { get; }
}

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax? expression)
    {
        ArgumentNullException.ThrowIfNull(returnKeyword);

        ReturnKeyword = returnKeyword;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStatement;

    public SyntaxToken ReturnKeyword { get; }

    public ExpressionSyntax? Expression { get; }
}

public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public ExpressionStatementSyntax(ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

    public ExpressionSyntax Expression { get; }
}

public sealed class DimStatementSyntax : StatementSyntax
{
    public DimStatementSyntax(
        SyntaxToken dimKeyword,
        IReadOnlyList<VariableDeclarationSyntax> declarations,
        IReadOnlyList<SyntaxToken> commaTokens)
    {
        ArgumentNullException.ThrowIfNull(dimKeyword);
        ArgumentNullException.ThrowIfNull(declarations);
        ArgumentNullException.ThrowIfNull(commaTokens);

        DimKeyword = dimKeyword;
        Declarations = declarations;
        CommaTokens = commaTokens;
    }

    public override SyntaxKind Kind => SyntaxKind.DimStatement;

    public SyntaxToken DimKeyword { get; }

    public IReadOnlyList<VariableDeclarationSyntax> Declarations { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }
}

public sealed class ConstStatementSyntax : StatementSyntax
{
    public ConstStatementSyntax(SyntaxToken constKeyword, ConstantDeclarationSyntax declaration)
    {
        ArgumentNullException.ThrowIfNull(constKeyword);
        ArgumentNullException.ThrowIfNull(declaration);

        ConstKeyword = constKeyword;
        Declaration = declaration;
    }

    public override SyntaxKind Kind => SyntaxKind.ConstStatement;

    public SyntaxToken ConstKeyword { get; }

    public ConstantDeclarationSyntax Declaration { get; }
}
