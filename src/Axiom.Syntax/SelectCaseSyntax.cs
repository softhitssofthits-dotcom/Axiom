namespace Axiom.Syntax;

public sealed class SelectCaseStatementSyntax : StatementSyntax
{
    public SelectCaseStatementSyntax(
        SyntaxToken selectKeyword,
        SyntaxToken caseKeyword,
        ExpressionSyntax testExpression,
        IReadOnlyList<SelectCaseClauseSyntax> clauses,
        CaseElseClauseSyntax? caseElseClause,
        SyntaxToken endKeyword,
        SyntaxToken endSelectKeyword)
    {
        ArgumentNullException.ThrowIfNull(selectKeyword);
        ArgumentNullException.ThrowIfNull(caseKeyword);
        ArgumentNullException.ThrowIfNull(testExpression);
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentNullException.ThrowIfNull(endKeyword);
        ArgumentNullException.ThrowIfNull(endSelectKeyword);

        SelectKeyword = selectKeyword;
        CaseKeyword = caseKeyword;
        TestExpression = testExpression;
        Clauses = clauses;
        CaseElseClause = caseElseClause;
        EndKeyword = endKeyword;
        EndSelectKeyword = endSelectKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.SelectCaseStatement;

    public SyntaxToken SelectKeyword { get; }

    public SyntaxToken CaseKeyword { get; }

    public ExpressionSyntax TestExpression { get; }

    public IReadOnlyList<SelectCaseClauseSyntax> Clauses { get; }

    public CaseElseClauseSyntax? CaseElseClause { get; }

    public SyntaxToken EndKeyword { get; }

    public SyntaxToken EndSelectKeyword { get; }
}

public sealed class SelectCaseClauseSyntax : SyntaxNode
{
    public SelectCaseClauseSyntax(
        SyntaxToken caseKeyword,
        IReadOnlyList<SelectCaseSelectorSyntax> selectors,
        IReadOnlyList<SyntaxToken> commaTokens,
        IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(caseKeyword);
        ArgumentNullException.ThrowIfNull(selectors);
        ArgumentNullException.ThrowIfNull(commaTokens);
        ArgumentNullException.ThrowIfNull(statements);

        CaseKeyword = caseKeyword;
        Selectors = selectors;
        CommaTokens = commaTokens;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.SelectCaseClause;

    public SyntaxToken CaseKeyword { get; }

    public IReadOnlyList<SelectCaseSelectorSyntax> Selectors { get; }

    public IReadOnlyList<SyntaxToken> CommaTokens { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public sealed class CaseElseClauseSyntax : SyntaxNode
{
    public CaseElseClauseSyntax(
        SyntaxToken caseKeyword,
        SyntaxToken elseKeyword,
        IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(caseKeyword);
        ArgumentNullException.ThrowIfNull(elseKeyword);
        ArgumentNullException.ThrowIfNull(statements);

        CaseKeyword = caseKeyword;
        ElseKeyword = elseKeyword;
        Statements = statements;
    }

    public override SyntaxKind Kind => SyntaxKind.CaseElseClause;

    public SyntaxToken CaseKeyword { get; }

    public SyntaxToken ElseKeyword { get; }

    public IReadOnlyList<StatementSyntax> Statements { get; }
}

public abstract class SelectCaseSelectorSyntax : SyntaxNode;

public sealed class CaseValueSelectorSyntax : SelectCaseSelectorSyntax
{
    public CaseValueSelectorSyntax(ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.CaseValueSelector;

    public ExpressionSyntax Expression { get; }
}

public sealed class CaseRangeSelectorSyntax : SelectCaseSelectorSyntax
{
    public CaseRangeSelectorSyntax(ExpressionSyntax startExpression, SyntaxToken toKeyword, ExpressionSyntax endExpression)
    {
        ArgumentNullException.ThrowIfNull(startExpression);
        ArgumentNullException.ThrowIfNull(toKeyword);
        ArgumentNullException.ThrowIfNull(endExpression);

        StartExpression = startExpression;
        ToKeyword = toKeyword;
        EndExpression = endExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.CaseRangeSelector;

    public ExpressionSyntax StartExpression { get; }

    public SyntaxToken ToKeyword { get; }

    public ExpressionSyntax EndExpression { get; }
}

public sealed class CaseComparisonSelectorSyntax : SelectCaseSelectorSyntax
{
    public CaseComparisonSelectorSyntax(SyntaxToken isKeyword, SyntaxToken operatorToken, ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(isKeyword);
        ArgumentNullException.ThrowIfNull(operatorToken);
        ArgumentNullException.ThrowIfNull(expression);

        IsKeyword = isKeyword;
        OperatorToken = operatorToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.CaseComparisonSelector;

    public SyntaxToken IsKeyword { get; }

    public SyntaxToken OperatorToken { get; }

    public ExpressionSyntax Expression { get; }
}
