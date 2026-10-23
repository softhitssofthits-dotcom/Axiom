using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    // Разрешение цели не отключает строгую проверку вложенных выражений:
    // только непосредственные имя/поле получают массивный контекст.
    private BoundExpression BindArrayTarget(ExpressionSyntax target)
    {
        return target switch
        {
            NameExpressionSyntax name => BindNameExpression(name, allowArrayTarget: true),
            FieldAccessExpressionSyntax field => BindFieldAccessExpression(field, allowArrayTarget: true),
            ParenthesizedExpressionSyntax parenthesized => BindArrayTarget(parenthesized.Expression),
            _ => BindExpression(target)
        };
    }

    private BoundExpression BindSquareArrayAccessExpression(ArrayAccessExpressionSyntax expression, bool forAssignment = false)
    {
        var target = BindArrayTarget(expression.Target);
        var location = GetLocation(expression.Target);
        var displayName = GetArrayTargetDisplayName(target);

        if (target.Type is not ArrayTypeSymbol arrayType)
        {
            if (target.Type != TypeSymbol.Error)
            {
                ReportInvalidArrayAccess(displayName, location);
            }

            foreach (var index in expression.Indices)
            {
                BindArrayIndexExpression(index);
            }

            return new BoundErrorExpression(location);
        }

        // Никакой обёртки или копии: BYREF должен видеть исходную переменную,
        // а поле — сохранять свой прежний адресуемый bound-узел.
        if (expression.IsWholeArray)
        {
            if (forAssignment && target is not (BoundVariableExpression or BoundFieldAccessExpression))
            {
                ReportArrayAssignmentRequiresFullIndices(location);
                return new BoundErrorExpression(location);
            }

            return target;
        }

        if (!IsValidReadableArrayIndexCount(arrayType, expression.Indices.Count))
        {
            ReportReadableArrayIndexCount(displayName, arrayType.Rank, expression.Indices.Count, location);
            foreach (var index in expression.Indices)
            {
                BindArrayIndexExpression(index);
            }

            return new BoundErrorExpression(location);
        }

        if (forAssignment && (expression.Indices.Count != arrayType.Rank
            || expression.Indices.Any(static index => index is RangeExpressionSyntax)))
        {
            ReportArrayAssignmentRequiresFullIndices(location);
            foreach (var index in expression.Indices)
            {
                BindArrayIndexExpression(index);
            }

            return new BoundErrorExpression(location);
        }

        return BindArrayAccess(target, expression.Indices, location, displayName);
    }

    private BoundExpression RejectParenthesizedArrayAccess(
        BoundExpression target,
        CallExpressionSyntax expression)
    {
        var location = GetLocation(expression.Target);
        ReportArrayParenthesesForbidden(GetArrayTargetDisplayName(target), location);
        foreach (var argument in expression.Arguments)
        {
            BindArrayIndexExpression(argument);
        }

        return new BoundErrorExpression(location);
    }

    private BoundExpression BindArrayAccess(
        BoundExpression target,
        IReadOnlyList<ExpressionSyntax> indexExpressions,
        TextLocation location,
        string displayName)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(indexExpressions);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (target.Type is not ArrayTypeSymbol && target.Type != TypeSymbol.Error)
        {
            ReportInvalidArrayAccess(displayName, location);

            foreach (var indexExpression in indexExpressions)
            {
                BindExpression(indexExpression);
            }

            return new BoundErrorExpression(location);
        }

        var indices = indexExpressions
            .Select(BindArrayIndexExpression)
            .ToArray();
        return new BoundArrayAccessExpression(location, target, indices, ResolveArrayAccessResultType(target.Type, indices), displayName);
    }

    private BoundExpression BindArrayIndexExpression(ExpressionSyntax expression)
    {
        return expression is RangeExpressionSyntax rangeExpression
            ? BindRangeExpression(rangeExpression, reportInvalidStandaloneUse: false)
            : BindNumericExpression(expression);
    }

    private static VariableSymbol? TryGetAssignedVariable(BoundExpression target)
    {
        return target switch
        {
            BoundVariableExpression variableExpression => variableExpression.Variable,
            BoundArrayAccessExpression arrayAccessExpression => TryGetAssignedVariable(arrayAccessExpression.Target),
            BoundFieldAccessExpression fieldAccessExpression => TryGetAssignedVariable(fieldAccessExpression.Target),
            _ => null
        };
    }

    private BoundExpression BindAssignableCallTarget(CallExpressionSyntax expression)
    {
        if (expression.Target is NameExpressionSyntax typeNameExpression
            && _declaredTypes.TryGetValue(typeNameExpression.IdentifierToken.Text, out var recordType))
        {
            return BindRecordLiteralExpression(recordType, expression);
        }

        if (expression.Target is NameExpressionSyntax nameExpression
            && _scope.TryLookupVariable(nameExpression.IdentifierToken.Text, out var variable)
            && variable is not null
            && variable.IsArray)
        {
            _editorReferences?.Add(new(variable, nameExpression.IdentifierToken.Location, false));
            if (_usesSquareArraySyntax)
            {
                return RejectParenthesizedArrayAccess(
                    new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable), expression);
            }

            var arrayType = (ArrayTypeSymbol)variable.Type;

            if (expression.Arguments.Count == 0)
                return new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable);

            if (expression.Arguments.Count != arrayType.Rank)
            {
                ReportWrongArgumentCount(variable.Name, arrayType.Rank, expression.Arguments.Count, nameExpression.IdentifierToken.Location);

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
            }

            if (HasRangeArgument(expression.Arguments))
            {
                foreach (var argument in expression.Arguments)
                {
                    BindArrayIndexExpression(argument);
                }

                return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
            }

            return BindArrayAccess(
                new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable),
                expression.Arguments,
                nameExpression.IdentifierToken.Location,
                variable.Name);
        }

        var target = BindArrayTarget(expression.Target);

        if (target.Type is ArrayTypeSymbol)
        {
            if (_usesSquareArraySyntax)
            {
                return RejectParenthesizedArrayAccess(target, expression);
            }

            if (expression.Arguments.Count == 0) return target;
            var displayName = GetArrayTargetDisplayName(target);
            var arrayType = (ArrayTypeSymbol)target.Type;

            if (expression.Arguments.Count != arrayType.Rank)
            {
                ReportWrongArgumentCount(displayName, arrayType.Rank, expression.Arguments.Count, GetLocation(expression.Target));

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(GetLocation(expression.Target));
            }

            if (HasRangeArgument(expression.Arguments))
            {
                foreach (var argument in expression.Arguments)
                {
                    BindArrayIndexExpression(argument);
                }

                return new BoundErrorExpression(GetLocation(expression.Target));
            }

            return BindArrayAccess(target, expression.Arguments, GetLocation(expression.Target), displayName);
        }

        return new BoundErrorExpression(GetLocation(expression));
    }

    private bool HasRangeArgument(IReadOnlyList<ExpressionSyntax> arguments)
    {
        var hasRange = false;

        foreach (var argument in arguments)
        {
            if (argument is not RangeExpressionSyntax range)
            {
                continue;
            }

            ReportInvalidRangeExpression(range.ToKeyword.Location);
            hasRange = true;
        }

        return hasRange;
    }

    private static string GetArrayTargetDisplayName(BoundExpression target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target switch
        {
            BoundVariableExpression variableExpression => variableExpression.Variable.Name,
            BoundFieldAccessExpression fieldAccessExpression => $"{GetArrayTargetDisplayName(fieldAccessExpression.Target)}.{fieldAccessExpression.Field.Name}",
            BoundArrayAccessExpression arrayAccessExpression => $"{GetArrayTargetDisplayName(arrayAccessExpression.Target)}(...)",
            _ => "массива"
        };
    }

    private static bool RequiresExplicitDimensionArgument(BuiltinFunctionId id, int arrayRank, int argumentCount)
    {
        if (arrayRank <= 1)
        {
            return false;
        }

        return id switch
        {
            BuiltinFunctionId.ArraySlice => argumentCount == 3,
            BuiltinFunctionId.ArrayTake => argumentCount == 2,
            BuiltinFunctionId.ArraySkip => argumentCount == 2,
            BuiltinFunctionId.ArrayConcat => argumentCount == 2,
            BuiltinFunctionId.ArrayToSorted => argumentCount == 1,
              BuiltinFunctionId.ArrayToReversed => argumentCount == 1,
              BuiltinFunctionId.ArrayPush => argumentCount == 2,
              BuiltinFunctionId.ArrayPop => argumentCount == 1,
              BuiltinFunctionId.ArrayInsert => argumentCount == 3,
              BuiltinFunctionId.ArrayRemove => argumentCount == 2,
              BuiltinFunctionId.ArrayReverse => argumentCount == 1,
              BuiltinFunctionId.ArraySort => argumentCount == 1,
              BuiltinFunctionId.ArrayBinarySearch => argumentCount == 2,
            _ => false
        };
    }

    private static bool IsValidReadableArrayIndexCount(ArrayTypeSymbol arrayType, int actualCount)
    {
        return actualCount >= 1 && actualCount <= arrayType.Rank;
    }

    private void ReportReadableArrayIndexCount(string displayName, int rank, int actualCount, TextLocation location)
    {
        if (rank == 1)
        {
            ReportWrongArgumentCount(displayName, 1, actualCount, location);
            return;
        }

        ReportWrongArgumentCount(displayName, Enumerable.Range(1, rank).ToArray(), actualCount, location);
    }

    private static TypeSymbol ResolveArrayAccessResultType(TypeSymbol targetType, IReadOnlyList<BoundExpression> indices)
    {
        if (targetType is not ArrayTypeSymbol arrayType)
        {
            return TypeSymbol.Error;
        }

        var indexCount = indices.Count;
        if (indexCount < 1 || indexCount > arrayType.Rank)
        {
            return TypeSymbol.Error;
        }

        var fixedIndexCount = indices.Count(static index => index is not BoundArrayRangeExpression);
        var hasRange = fixedIndexCount != indexCount;

        if (!hasRange && indexCount == arrayType.Rank)
        {
            return arrayType.ElementType;
        }

        return ArrayTypeSymbol.Create(arrayType.ElementType, arrayType.Rank - fixedIndexCount);
    }
}
