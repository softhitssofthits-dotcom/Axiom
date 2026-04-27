using Axiom.Syntax;

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
                declarations.Add(new BoundVariableDeclarationStatement(GetLocation(statement), variable, initializer, arrayBounds));
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
            var explicitInitializer = declaration.Initializer is null
                ? null
                : BindExpression(declaration.Initializer, explicitType);
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
        var type = ReportUnsupportedFixedStringType(declaration.TypeClause, declaration.Identifier.Text)
            ? TypeSymbol.Error
            : ResolveType(declaration.TypeClause);
        var constant = new VariableSymbol(
            declaration.Identifier.Text,
            type,
            isReadOnly: true,
            isGlobal: _currentFunction is null);

        TryDeclareScopedVariable(constant, declaration.Identifier.Location);

        var initializer = BindExpression(declaration.Initializer, type);

        if ((type == TypeSymbol.Integer || type == TypeSymbol.Long)
            && TryEvaluateBoundIntegralConstant(initializer, out var integralValue))
        {
            _boundIntegralConstants[constant] = integralValue;
        }

        return new BoundVariableDeclarationStatement(GetLocation(statement), constant, initializer, arrayBounds: []);
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
        var expression = BindExpression(statement.Expression, targetType);

        if (targetType == TypeSymbol.File || expression.Type == TypeSymbol.File)
        {
            ReportInvalidFileAssignment(GetLocation(statement));
        }

        return new BoundAssignmentStatement(GetLocation(statement), target, expression);
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
}
