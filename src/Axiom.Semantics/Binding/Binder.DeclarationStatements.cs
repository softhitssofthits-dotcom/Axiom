using Axiom.Syntax;
using System.Globalization;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundStatement BindDimStatement(DimStatementSyntax statement)
    {
        var declarations = new List<BoundVariableDeclarationStatement>();

        foreach (var declaration in statement.Declarations)
        {
            var (type, initializer, fixedStringLength) = BindDimDeclaration(declaration);

            foreach (var declarator in declaration.Declarators)
            {
                var arrayRank = declarator.ArrayBounds?.Rank ?? 0;
                var variableType = declarator.ArrayBounds is null
                    ? type
                    : BindArrayVariableType(type, arrayRank);
                var variable = new VariableSymbol(
                    declarator.Identifier.Text,
                    variableType,
                    isReadOnly: false,
                    isGlobal: _currentFunction is null,
                    fixedStringLength: fixedStringLength);

                TryDeclareScopedVariable(variable, declarator.Identifier.Location);

                var arrayBounds = BindDeclaratorArrayBounds(declarator);
                var declaratorInitializer = initializer;

                if (declarator.ArrayBounds is not null && declaratorInitializer is not null)
                {
                    ReportArrayInitializerNotSupported(declarator.Identifier.Text, declarator.Identifier.Location);
                    declaratorInitializer = null;
                }

                declarations.Add(new BoundVariableDeclarationStatement(GetLocation(statement), variable, declaratorInitializer, arrayBounds, declarator.Identifier.Location));
            }
        }

        return declarations.Count == 1
            ? declarations[0]
            : new BoundVariableDeclarationsStatement(GetLocation(statement), declarations);
    }

    private (TypeSymbol type, BoundExpression? initializer, int? fixedStringLength) BindDimDeclaration(VariableDeclarationSyntax declaration)
    {
        if (declaration.TypeClause is not null)
        {
            if (!TryResolveFixedStringLength(
                    declaration.Declarators[0].Identifier.Text,
                    declaration.TypeClause,
                    allowGlobalForwardConstants: _currentFunction is null,
                    out var fixedStringLength))
            {
                return (TypeSymbol.Error, null, null);
            }

            var explicitType = ResolveType(declaration.TypeClause);
            BoundExpression? explicitInitializer = null;
            if (declaration.Initializer is not null)
            {
                explicitInitializer = BindExpression(declaration.Initializer);
                if (explicitType != TypeSymbol.Error)
                {
                    if (TryReportTruncatingTemporalAssignment(declaration.Initializer, explicitInitializer, explicitType))
                    {
                        explicitInitializer = new BoundErrorExpression(GetLocation(declaration.Initializer));
                    }
                    else
                    {
                        explicitInitializer = ApplyExpectedType(declaration.Initializer, explicitInitializer, explicitType);
                    }
                }
            }

            return (explicitType, explicitInitializer, fixedStringLength);
        }

        if (declaration.Initializer is null)
        {
            var missingType = declaration.Declarators[0].Identifier;
            ReportMissingVariableType(missingType.Text, missingType.Location);
            return (TypeSymbol.Error, null, null);
        }

        var inferredInitializer = BindExpression(declaration.Initializer);
        return (inferredInitializer.Type, inferredInitializer, null);
    }

    private BoundStatement BindConstStatement(ConstStatementSyntax statement)
    {
        var declaration = statement.Declaration;
        var declarationName = string.IsNullOrWhiteSpace(declaration.Identifier.Text)
            ? statement.ConstKeyword.Text
            : declaration.Identifier.Text;
        var type = ReportUnsupportedFixedStringType(declaration.TypeClause, declarationName)
            ? TypeSymbol.Error
            : ResolveType(declaration.TypeClause);
        var initializer = BindExpression(declaration.Initializer);
        if (type != TypeSymbol.Error)
        {
            if (TryReportTruncatingTemporalAssignment(declaration.Initializer, initializer, type))
            {
                initializer = new BoundErrorExpression(GetLocation(declaration.Initializer));
            }
            else
            {
                initializer = ApplyExpectedType(declaration.Initializer, initializer, type);
            }
        }

        if (string.IsNullOrWhiteSpace(declaration.Identifier.Text))
        {
            return new BoundExpressionStatement(GetLocation(statement), initializer);
        }

        var constant = new VariableSymbol(
            declaration.Identifier.Text,
            type,
            isReadOnly: true,
            isGlobal: _currentFunction is null);

        TryDeclareScopedVariable(constant, declaration.Identifier.Location);

        if ((type == TypeSymbol.Integer || type == TypeSymbol.Long)
            && TryEvaluateBoundIntegralConstant(initializer, out var integralValue))
        {
            _boundIntegralConstants[constant] = integralValue;
        }

        return new BoundVariableDeclarationStatement(GetLocation(statement), constant, initializer, arrayBounds: [], identifierLocation: declaration.Identifier.Location);
    }

    private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax statement)
    {
        var target = BindAssignableTarget(statement.Target);
        var variable = TryGetAssignedVariable(target);

        if (variable is not null && variable.IsReadOnly)
        {
            ReportAssignToConst(variable.Name, GetLocation(statement.Target));
        }

        var targetType = target.Type;
        var expression = BindAssignmentExpression(statement.Expression, target, targetType);

        if (targetType == TypeSymbol.File || expression.Type == TypeSymbol.File)
        {
            ReportInvalidFileAssignment(GetLocation(statement));
        }

        return new BoundAssignmentStatement(GetLocation(statement), target, expression);
    }

    private BoundExpression BindAssignmentExpression(ExpressionSyntax expressionSyntax, BoundExpression target, TypeSymbol targetType)
    {
        var expression = BindExpression(expressionSyntax);
        var targetName = GetArrayTargetDisplayName(target);

        if (targetType == TypeSymbol.Error || expression.Type == TypeSymbol.Error)
        {
            return expression;
        }

        if (TryReportTruncatingTemporalAssignment(expressionSyntax, expression, targetType))
        {
            return new BoundErrorExpression(GetLocation(expressionSyntax));
        }

        if (expression.Type == targetType)
        {
            return expression;
        }

        if (TryConvertConstant(expressionSyntax, expression, targetType, out var convertedConstant))
        {
            return convertedConstant;
        }

        if (CanConvert(expression.Type, targetType))
        {
            return new BoundConversionExpression(expression.Location, targetType, expression);
        }

        if (targetType is ArrayTypeSymbol && expression.Type is not ArrayTypeSymbol)
        {
            ReportScalarAssignedToArray(targetName, GetLocation(expressionSyntax));
        }
        else if (targetType is not ArrayTypeSymbol && expression.Type is ArrayTypeSymbol)
        {
            ReportArrayAssignedToScalar(targetName, GetLocation(expressionSyntax));
        }
        else
        {
            if (TryReportFunctionReferenceNote(expression, targetType, GetLocation(expressionSyntax)))
            {
                return new BoundErrorExpression(GetLocation(expressionSyntax));
            }

            ReportTypeMismatch(expression.Type, targetType, GetLocation(expressionSyntax));
        }

        return new BoundErrorExpression(GetLocation(expressionSyntax));
    }

    private BoundStatement BindEraseStatement(EraseStatementSyntax statement)
    {
        var target = BindExpression(statement.Target);
        var variable = TryGetAssignedVariable(target);
        var displayName = GetArrayTargetDisplayName(target);
        var field = target as BoundFieldAccessExpression;

        if (variable is not null && variable.IsReadOnly)
        {
            ReportAssignToConst(variable.Name, GetLocation(statement.Target));
        }

        if (target.Type is not ArrayTypeSymbol && target.Type != TypeSymbol.Error)
        {
            ReportInvalidArrayAccess(displayName, GetLocation(statement.Target));
        }
        else if (field?.Field.IsFixedArray == true)
        {
            ReportFixedTypeArrayFieldOperation("ERASE", displayName, GetLocation(statement.Target));
        }

        return new BoundEraseStatement(GetLocation(statement), target, displayName);
    }

    private BoundStatement BindRedimStatement(RedimStatementSyntax statement)
    {
        var target = BindExpression(statement.Target);
        var variable = TryGetAssignedVariable(target);
        var displayName = GetArrayTargetDisplayName(target);
        var field = target as BoundFieldAccessExpression;

        if (variable is not null && variable.IsReadOnly)
        {
            ReportAssignToConst(variable.Name, GetLocation(statement.Target));
        }

        if (target.Type is not ArrayTypeSymbol && target.Type != TypeSymbol.Error)
        {
            ReportInvalidArrayAccess(displayName, GetLocation(statement.Target));
        }
        else if (field?.Field.IsFixedArray == true)
        {
            ReportFixedTypeArrayFieldOperation("REDIM", displayName, GetLocation(statement.Target));
        }

        if (target.Type is ArrayTypeSymbol arrayType
            && statement.ArrayBounds.Rank != arrayType.Rank)
        {
            ReportWrongArgumentCount(displayName, arrayType.Rank, statement.ArrayBounds.Rank, statement.ArrayBounds.OpenParenToken.Location);
        }

        var arrayBounds = statement.ArrayBounds.Dimensions
            .Select(BindExplicitArrayBound)
            .ToArray();
        return new BoundRedimStatement(GetLocation(statement), target, displayName, statement.IsPreserve, arrayBounds);
    }

    private IReadOnlyList<BoundArrayBound> BindDeclaratorArrayBounds(VariableDeclaratorSyntax declarator)
    {
        if (declarator.ArrayBounds is null)
        {
            return [];
        }

        if (declarator.ArrayBounds.HasMixedDimensions)
        {
            ReportInvalidArrayBoundsForm(
                declarator.Identifier.Text,
                declarator.ArrayBounds.OpenParenToken.Location,
                "Либо укажите границы для каждого измерения, либо оставьте все измерения пустыми.");
            return [];
        }

        if (declarator.ArrayBounds.IsDynamic)
        {
            return [];
        }

        return declarator.ArrayBounds.Dimensions
            .Select(BindExplicitArrayBound)
            .ToArray();
    }

    private BoundArrayBound BindExplicitArrayBound(ArrayDimensionSyntax dimension)
    {
        return new BoundArrayBound(
            BindNumericExpression(dimension.StartExpression!),
            BindNumericExpression(dimension.EndExpression!));
    }

    private static TypeSymbol BindArrayVariableType(TypeSymbol elementType, int rank)
    {
        return ArrayTypeSymbol.Create(elementType, rank);
    }

    private bool TryReportTruncatingTemporalAssignment(
        ExpressionSyntax initializerSyntax,
        BoundExpression boundExpression,
        TypeSymbol targetType)
    {
        if (targetType != TypeSymbol.Date && targetType != TypeSymbol.Time)
        {
            return false;
        }

        if (boundExpression.Type == TypeSymbol.DateTime
            || (boundExpression.Type == TypeSymbol.String
                && TryGetStringLiteralText(initializerSyntax, out var literalText)
                && IsCanonicalDateTimeText(literalText)))
        {
            ReportTruncatingTemporalAssignment(targetType, GetLocation(initializerSyntax));
            return true;
        }

        return false;
    }

    private static bool TryGetStringLiteralText(ExpressionSyntax expression, out string text)
    {
        if (expression is LiteralExpressionSyntax literal
            && literal.LiteralToken.Kind == SyntaxKind.StringLiteralToken
            && literal.LiteralToken.Value is string value)
        {
            text = value;
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool IsCanonicalDateTimeText(string text)
    {
        return text.Length == 19
            && text[10] == ' '
            && DateOnly.TryParseExact(text.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            && TimeOnly.TryParseExact(text.AsSpan(11, 8), "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
