using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    // Обращение по ключу: prices["яблоко"]. Ключ приводится к объявленному типу
    // ключа; числовые ключи разных типов в одном словаре не смешиваются.
    private BoundExpression BindDictionaryAccessExpression(
        ArrayAccessExpressionSyntax expression,
        BoundExpression target,
        DictionaryTypeSymbol dictionaryType,
        bool forAssignment)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dictionaryType);

        var location = GetLocation(expression.Target);
        var displayName = GetArrayTargetDisplayName(target);

        if (expression.IsWholeArray)
        {
            ReportDictionaryWholeValueAccess(displayName, location);
            return new BoundErrorExpression(location);
        }

        if (expression.Indices.Count != 1 || expression.Indices[0] is RangeExpressionSyntax)
        {
            ReportWrongArgumentCount(displayName, 1, expression.Indices.Count, location);
            foreach (var index in expression.Indices)
            {
                BindExpression(index);
            }

            return new BoundErrorExpression(location);
        }

        var key = BindDictionaryKeyExpression(expression.Indices[0], dictionaryType);
        return new BoundDictionaryAccessExpression(location, target, key, dictionaryType, displayName);
    }

    private BoundExpression BindDictionaryKeyExpression(ExpressionSyntax expression, DictionaryTypeSymbol dictionaryType)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(dictionaryType);

        return BindExpression(expression, dictionaryType.KeyType);
    }

    private void ReportDictionaryWholeValueAccess(string displayName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidDictionaryMember,
            $"Словарь '{displayName}' не обозначается пустыми скобками: укажите ключ в prices[ключ]. Для всех ключей используйте {displayName}.Keys().",
            location);
    }

    private void ReportDictionaryRequiresKey(string displayName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidDictionaryMember,
            $"Словарю '{displayName}' нужен ключ в квадратных скобках: {displayName}[ключ].",
            location);
    }

    // Методы словаря вызываются со скобками: Add, Exists, Remove, Keys,
    // TryGetValue. Count — свойство и скобок не имеет.
    private BoundExpression BindDictionaryMemberCall(
        BoundExpression receiver,
        FieldAccessExpressionSyntax methodAccess,
        CallExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(methodAccess);
        ArgumentNullException.ThrowIfNull(expression);

        var dictionaryType = (DictionaryTypeSymbol)receiver.Type;
        var displayName = GetArrayTargetDisplayName(receiver);
        var memberName = methodAccess.Identifier.Text;
        var location = methodAccess.Identifier.Location;

        if (TryReportUnsupportedNamedArguments(expression))
        {
            return new BoundErrorExpression(location);
        }

        switch (memberName.ToUpperInvariant())
        {
            case "ADD":
                return BindDictionaryAdd(receiver, dictionaryType, displayName, expression, location);

            case "EXISTS":
                return BindDictionaryExists(receiver, dictionaryType, displayName, expression, location);

            case "REMOVE":
                return BindDictionaryRemove(receiver, dictionaryType, displayName, expression, location);

            case "KEYS":
                if (expression.Arguments.Count != 0)
                {
                    ReportWrongArgumentCount(memberName, 0, expression.Arguments.Count, location);
                    foreach (var argument in expression.Arguments)
                    {
                        BindExpression(argument);
                    }

                    return new BoundErrorExpression(location);
                }

                return new BoundDictionaryMethodCallExpression(
                    location,
                    BoundDictionaryMethodKind.Keys,
                    receiver,
                    key: null,
                    value: null,
                    dictionaryType,
                    ArrayTypeSymbol.Create(dictionaryType.KeyType),
                    displayName);

            case "TRYGETVALUE":
                return BindDictionaryTryGetValue(receiver, dictionaryType, displayName, expression, location);

            case "COUNT":
                _diagnostics.Report(
                    DiagnosticCodes.InvalidDictionaryMember,
                    $"Count — свойство словаря '{displayName}' и записывается без скобок: {displayName}.Count.",
                    location);
                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(location);

            default:
                ReportInvalidDictionaryMember(memberName, dictionaryType, location);
                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(location);
        }
    }

    private BoundExpression BindDictionaryAdd(
        BoundExpression receiver,
        DictionaryTypeSymbol dictionaryType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        if (expression.Arguments.Count != 2)
        {
            ReportWrongArgumentCount("Add", 2, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var key = BindDictionaryKeyExpression(expression.Arguments[0], dictionaryType);
        var value = BindExpression(expression.Arguments[1], dictionaryType.ValueType);

        return new BoundDictionaryMethodCallExpression(
            location,
            BoundDictionaryMethodKind.Add,
            receiver,
            key,
            value,
            dictionaryType,
            TypeSymbol.Void,
            displayName);
    }

    private BoundExpression BindDictionaryExists(
        BoundExpression receiver,
        DictionaryTypeSymbol dictionaryType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        if (expression.Arguments.Count != 1)
        {
            ReportWrongArgumentCount("Exists", 1, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var key = BindDictionaryKeyExpression(expression.Arguments[0], dictionaryType);

        return new BoundDictionaryMethodCallExpression(
            location,
            BoundDictionaryMethodKind.Exists,
            receiver,
            key,
            value: null,
            dictionaryType,
            TypeSymbol.Boolean,
            displayName);
    }

    private BoundExpression BindDictionaryRemove(
        BoundExpression receiver,
        DictionaryTypeSymbol dictionaryType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        if (expression.Arguments.Count != 1)
        {
            ReportWrongArgumentCount("Remove", 1, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var key = BindDictionaryKeyExpression(expression.Arguments[0], dictionaryType);

        return new BoundDictionaryMethodCallExpression(
            location,
            BoundDictionaryMethodKind.Remove,
            receiver,
            key,
            value: null,
            dictionaryType,
            TypeSymbol.Boolean,
            displayName);
    }

    // TryGetValue принимает ключ и изменяемую переменную точного типа значения.
    // Наличие определяется возвращённым BOOLEAN, поэтому найденный ноль не
    // смешивается с отсутствием записи.
    private BoundExpression BindDictionaryTryGetValue(
        BoundExpression receiver,
        DictionaryTypeSymbol dictionaryType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        if (expression.Arguments.Count != 2)
        {
            ReportWrongArgumentCount("TryGetValue", 2, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var key = BindDictionaryKeyExpression(expression.Arguments[0], dictionaryType);
        var destinationSyntax = expression.Arguments[1];
        var destination = BindAssignableTarget(destinationSyntax);

        if (destination.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(location);
        }

        // Второй аргумент — изменяемая переменная точного типа значения:
        // результат записывается в неё, поэтому временное значение не подходит.
        if (destination is not BoundVariableExpression)
        {
            ReportInvalidByRefArgument(GetLocation(destinationSyntax));
            return new BoundErrorExpression(location);
        }

        var destinationVariable = TryGetAssignedVariable(destination);
        if (destinationVariable is not null && destinationVariable.IsReadOnly)
        {
            ReportAssignToConst(destinationVariable.Name, GetLocation(destinationSyntax));
            return new BoundErrorExpression(location);
        }

        if (destination.Type != dictionaryType.ValueType)
        {
            ReportTypeMismatch(destination.Type, dictionaryType.ValueType, GetLocation(destinationSyntax));
            return new BoundErrorExpression(location);
        }

        return new BoundDictionaryTryGetValueExpression(
            location,
            receiver,
            key,
            destination,
            dictionaryType,
            displayName);
    }

    private BoundExpression BindDictionaryCountProperty(
        BoundExpression target,
        DictionaryTypeSymbol dictionaryType,
        FieldAccessExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dictionaryType);
        ArgumentNullException.ThrowIfNull(expression);

        return new BoundDictionaryCountExpression(
            expression.Identifier.Location,
            target,
            dictionaryType,
            GetArrayTargetDisplayName(target));
    }

    private void ReportDictionaryMemberRequiresParentheses(
        string memberName,
        DictionaryTypeSymbol dictionaryType,
        TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        ArgumentNullException.ThrowIfNull(dictionaryType);

        _diagnostics.Report(
            DiagnosticCodes.InvalidDictionaryMember,
            $"'{memberName}' — метод словаря {dictionaryType.Name} и вызывается со скобками: {memberName}(...). Свойство Count записывается без скобок.",
            location);
    }

    private static bool IsDictionaryMethodName(string name)
    {
        return name.ToUpperInvariant() is "ADD" or "EXISTS" or "REMOVE" or "KEYS" or "TRYGETVALUE";
    }
}
