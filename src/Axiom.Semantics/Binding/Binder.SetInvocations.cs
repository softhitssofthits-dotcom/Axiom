using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    // Методы множества вызываются со скобками: Add, Contains, Remove, ToArray,
    // UnionWith, IntersectWith. Count — свойство и скобок не имеет.
    private BoundExpression BindSetMemberCall(
        BoundExpression receiver,
        FieldAccessExpressionSyntax methodAccess,
        CallExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(methodAccess);
        ArgumentNullException.ThrowIfNull(expression);

        var setType = (SetTypeSymbol)receiver.Type;
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
                return BindSetElementMethod(receiver, setType, displayName, expression, location, BoundSetMethodKind.Add);

            case "CONTAINS":
                return BindSetElementMethod(receiver, setType, displayName, expression, location, BoundSetMethodKind.Contains);

            case "REMOVE":
                return BindSetElementMethod(receiver, setType, displayName, expression, location, BoundSetMethodKind.Remove);

            case "TOARRAY":
                return BindSetToArray(receiver, setType, displayName, expression, location);

            case "UNIONWITH":
                return BindSetSetMethod(receiver, setType, displayName, expression, location, BoundSetMethodKind.UnionWith);

            case "INTERSECTWITH":
                return BindSetSetMethod(receiver, setType, displayName, expression, location, BoundSetMethodKind.IntersectWith);

            case "COUNT":
                _diagnostics.Report(
                    DiagnosticCodes.InvalidSetMember,
                    $"Count — свойство множества '{displayName}' и записывается без скобок: {displayName}.Count.",
                    location);
                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(location);

            default:
                ReportInvalidSetMember(memberName, setType, location);
                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(location);
        }
    }

    // Add, Contains и Remove принимают ровно один элемент объявленного типа.
    private BoundExpression BindSetElementMethod(
        BoundExpression receiver,
        SetTypeSymbol setType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location,
        BoundSetMethodKind method)
    {
        var methodName = SetMethodName(method);

        if (expression.Arguments.Count != 1)
        {
            ReportWrongArgumentCount(methodName, 1, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var value = BindExpression(expression.Arguments[0], setType.ElementType);

        // Add сообщает, добавлен ли элемент; Contains и Remove — признак
        // наличия или удаления. Ошибки повтора и отсутствия нет.
        return new BoundSetMethodCallExpression(
            location,
            method,
            receiver,
            value,
            setType,
            TypeSymbol.Boolean,
            displayName);
    }

    private BoundExpression BindSetToArray(
        BoundExpression receiver,
        SetTypeSymbol setType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        if (expression.Arguments.Count != 0)
        {
            ReportWrongArgumentCount("ToArray", 0, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        return new BoundSetMethodCallExpression(
            location,
            BoundSetMethodKind.ToArray,
            receiver,
            value: null,
            setType,
            ArrayTypeSymbol.Create(setType.ElementType),
            displayName);
    }

    // UnionWith и IntersectWith принимают множество точно того же типа и
    // значения не возвращают: коллекции разных типов элементов несовместимы.
    private BoundExpression BindSetSetMethod(
        BoundExpression receiver,
        SetTypeSymbol setType,
        string displayName,
        CallExpressionSyntax expression,
        TextLocation location,
        BoundSetMethodKind method)
    {
        var methodName = SetMethodName(method);

        if (expression.Arguments.Count != 1)
        {
            ReportWrongArgumentCount(methodName, 1, expression.Arguments.Count, location);
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        var argumentSyntax = expression.Arguments[0];
        var setArgument = BindExpression(argumentSyntax);

        if (setArgument.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(location);
        }

        if (setArgument.Type != setType)
        {
            ReportSetTypeMismatch(setArgument.Type, setType, GetLocation(argumentSyntax));
            return new BoundErrorExpression(location);
        }

        return new BoundSetMethodCallExpression(
            location,
            method,
            receiver,
            setArgument,
            setType,
            TypeSymbol.Void,
            displayName);
    }

    private BoundExpression BindSetCountProperty(
        BoundExpression target,
        SetTypeSymbol setType,
        FieldAccessExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(setType);
        ArgumentNullException.ThrowIfNull(expression);

        return new BoundSetCountExpression(
            expression.Identifier.Location,
            target,
            setType,
            GetArrayTargetDisplayName(target));
    }

    private void ReportSetMemberRequiresParentheses(string memberName, SetTypeSymbol setType, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        ArgumentNullException.ThrowIfNull(setType);

        _diagnostics.Report(
            DiagnosticCodes.InvalidSetMember,
            $"'{memberName}' — метод множества {setType.Name} и вызывается со скобками: {memberName}(...). Свойство Count записывается без скобок.",
            location);
    }

    // Имя множества без ключа или индекса: found[] и found[1] не являются
    // операциями множества.
    private void ReportSetRequiresMethodCall(string displayName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidSetMember,
            $"Множество '{displayName}' не индексируется: элементами управляют Add, Contains, Remove, ToArray, UnionWith и IntersectWith, а Count читается без скобок.",
            location);
    }

    private static string SetMethodName(BoundSetMethodKind method)
    {
        return method switch
        {
            BoundSetMethodKind.Add => "Add",
            BoundSetMethodKind.Contains => "Contains",
            BoundSetMethodKind.Remove => "Remove",
            BoundSetMethodKind.ToArray => "ToArray",
            BoundSetMethodKind.UnionWith => "UnionWith",
            BoundSetMethodKind.IntersectWith => "IntersectWith",
            _ => method.ToString()
        };
    }

    private static bool IsSetMethodName(string name)
    {
        return name.ToUpperInvariant() is "ADD" or "CONTAINS" or "REMOVE" or "TOARRAY" or "UNIONWITH" or "INTERSECTWITH";
    }
}
