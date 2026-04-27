using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundStatement BindSelectCaseStatement(SelectCaseStatementSyntax statement)
    {
        var testExpression = BindExpression(statement.TestExpression);
        var comparisonType = GetSelectCaseComparisonType(testExpression.Type);
        var normalizedTestExpression = NormalizeSelectCaseExpression(testExpression, comparisonType);
        var clauses = new List<BoundSelectCaseClause>(statement.Clauses.Count);

        foreach (var clause in statement.Clauses)
        {
            var selectors = new List<BoundSelectCaseSelector>(clause.Selectors.Count);

            foreach (var selector in clause.Selectors)
            {
                selectors.Add(BindSelectCaseSelector(selector, testExpression.Type, comparisonType));
            }

            clauses.Add(new BoundSelectCaseClause(selectors, BindStatementsInChildScope(clause.Statements, DeclarationContexts.CaseBranch)));
        }

        var elseStatements = statement.CaseElseClause is null
            ? Array.Empty<BoundStatement>()
            : BindStatementsInChildScope(statement.CaseElseClause.Statements, DeclarationContexts.CaseElseBranch);

        return new BoundSelectCaseStatement(GetLocation(statement), normalizedTestExpression, clauses, elseStatements);
    }

    private BoundSelectCaseSelector BindSelectCaseSelector(
        SelectCaseSelectorSyntax selector,
        TypeSymbol testType,
        TypeSymbol comparisonType)
    {
        return selector switch
        {
            CaseValueSelectorSyntax valueSelector => BindCaseValueSelector(valueSelector, testType, comparisonType),
            CaseRangeSelectorSyntax rangeSelector => BindCaseRangeSelector(rangeSelector, testType, comparisonType),
            CaseComparisonSelectorSyntax comparisonSelector => BindCaseComparisonSelector(comparisonSelector, testType, comparisonType),
            _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, "Unsupported SELECT CASE selector.")
        };
    }

    private BoundCaseValueSelector BindCaseValueSelector(
        CaseValueSelectorSyntax selector,
        TypeSymbol testType,
        TypeSymbol comparisonType)
    {
        var expression = BindExpression(selector.Expression);

        if (CanBindSelectCaseEquality(testType, expression.Type))
        {
            expression = NormalizeSelectCaseExpression(expression, comparisonType);
        }
        else if (testType != TypeSymbol.Error && expression.Type != TypeSymbol.Error)
        {
            ReportInvalidBinaryOperator(SyntaxKind.EqualsToken, testType, expression.Type, GetLocation(selector.Expression));
        }

        return new BoundCaseValueSelector(GetLocation(selector.Expression), expression);
    }

    private BoundCaseRangeSelector BindCaseRangeSelector(
        CaseRangeSelectorSyntax selector,
        TypeSymbol testType,
        TypeSymbol comparisonType)
    {
        var lowerBound = BindExpression(selector.StartExpression);
        var upperBound = BindExpression(selector.EndExpression);

        if (CanBindSelectCaseOrdering(testType, lowerBound.Type) && CanBindSelectCaseOrdering(testType, upperBound.Type))
        {
            lowerBound = NormalizeSelectCaseExpression(lowerBound, comparisonType);
            upperBound = NormalizeSelectCaseExpression(upperBound, comparisonType);
        }
        else if (testType != TypeSymbol.Error && lowerBound.Type != TypeSymbol.Error && upperBound.Type != TypeSymbol.Error)
        {
            ReportInvalidSelectCaseRange(selector.ToKeyword.Location);
        }

        return new BoundCaseRangeSelector(selector.ToKeyword.Location, lowerBound, upperBound);
    }

    private BoundCaseComparisonSelector BindCaseComparisonSelector(
        CaseComparisonSelectorSyntax selector,
        TypeSymbol testType,
        TypeSymbol comparisonType)
    {
        var expression = BindExpression(selector.Expression);
        var isEqualityOperator = selector.OperatorToken.Kind is SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken;

        if (isEqualityOperator)
        {
            if (CanBindSelectCaseEquality(testType, expression.Type))
            {
                expression = NormalizeSelectCaseExpression(expression, comparisonType);
            }
            else if (testType != TypeSymbol.Error && expression.Type != TypeSymbol.Error)
            {
                ReportInvalidBinaryOperator(selector.OperatorToken.Kind, testType, expression.Type, selector.OperatorToken.Location);
            }
        }
        else if (CanBindSelectCaseOrdering(testType, expression.Type))
        {
            expression = NormalizeSelectCaseExpression(expression, comparisonType);
        }
        else if (testType != TypeSymbol.Error && expression.Type != TypeSymbol.Error)
        {
            ReportInvalidBinaryOperator(selector.OperatorToken.Kind, testType, expression.Type, selector.OperatorToken.Location);
        }

        return new BoundCaseComparisonSelector(selector.OperatorToken.Location, selector.OperatorToken.Kind, expression);
    }

    private static TypeSymbol GetSelectCaseComparisonType(TypeSymbol testType)
    {
        if (testType == TypeSymbol.Integer || testType == TypeSymbol.Double)
        {
            return TypeSymbol.Double;
        }

        return testType;
    }

    private static BoundExpression NormalizeSelectCaseExpression(BoundExpression expression, TypeSymbol targetType)
    {
        if (expression.Type == TypeSymbol.Error || targetType == TypeSymbol.Error || expression.Type == targetType)
        {
            return expression;
        }

        return new BoundConversionExpression(expression.Location, targetType, expression);
    }

    private static bool CanBindSelectCaseEquality(TypeSymbol testType, TypeSymbol selectorType)
    {
        return (IsNumeric(testType) && IsNumeric(selectorType))
            || testType == selectorType && (testType == TypeSymbol.String || testType == TypeSymbol.Boolean);
    }

    private static bool CanBindSelectCaseOrdering(TypeSymbol testType, TypeSymbol selectorType)
    {
        return (IsNumeric(testType) && IsNumeric(selectorType))
            || testType == TypeSymbol.String && selectorType == TypeSymbol.String;
    }
}
