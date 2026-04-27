using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed class BoundSelectCaseStatement : BoundStatement
{
    public BoundSelectCaseStatement(
        TextLocation location,
        BoundExpression testExpression,
        IReadOnlyList<BoundSelectCaseClause> clauses,
        IReadOnlyList<BoundStatement> elseStatements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(testExpression);
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentNullException.ThrowIfNull(elseStatements);

        TestExpression = testExpression;
        Clauses = clauses;
        ElseStatements = elseStatements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SelectCaseStatement;

    public BoundExpression TestExpression { get; }

    public IReadOnlyList<BoundSelectCaseClause> Clauses { get; }

    public IReadOnlyList<BoundStatement> ElseStatements { get; }
}

public sealed class BoundSelectCaseClause
{
    public BoundSelectCaseClause(IReadOnlyList<BoundSelectCaseSelector> selectors, IReadOnlyList<BoundStatement> statements)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        ArgumentNullException.ThrowIfNull(statements);

        Selectors = selectors;
        Statements = statements;
    }

    public IReadOnlyList<BoundSelectCaseSelector> Selectors { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public abstract class BoundSelectCaseSelector
{
    protected BoundSelectCaseSelector(TextLocation location)
    {
        Location = location;
    }

    public TextLocation Location { get; }
}

public sealed class BoundCaseValueSelector : BoundSelectCaseSelector
{
    public BoundCaseValueSelector(TextLocation location, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public BoundExpression Expression { get; }
}

public sealed class BoundCaseRangeSelector : BoundSelectCaseSelector
{
    public BoundCaseRangeSelector(TextLocation location, BoundExpression lowerBound, BoundExpression upperBound)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(upperBound);

        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public BoundExpression LowerBound { get; }

    public BoundExpression UpperBound { get; }
}

public sealed class BoundCaseComparisonSelector : BoundSelectCaseSelector
{
    public BoundCaseComparisonSelector(TextLocation location, SyntaxKind operatorKind, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(expression);

        OperatorKind = operatorKind;
        Expression = expression;
    }

    public SyntaxKind OperatorKind { get; }

    public BoundExpression Expression { get; }
}
