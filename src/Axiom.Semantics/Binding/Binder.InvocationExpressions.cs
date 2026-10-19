using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundExpression BindCallExpression(CallExpressionSyntax expression)
    {
        if (expression.Target is NameExpressionSyntax nameExpression)
        {
            if (_declaredTypes.TryGetValue(nameExpression.IdentifierToken.Text, out var recordType)
                || _importedTypes.TryGetValue(nameExpression.IdentifierToken.Text, out recordType))
            {
                return BindTypeCreationExpression(recordType, expression);
            }

            if (_scope.TryLookupVariable(nameExpression.IdentifierToken.Text, out var variable) && variable is not null)
            {
                if (TryReportUnsupportedNamedArguments(expression))
                {
                    return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
                }

                if (variable.IsArray)
                {
                    var arrayType = (ArrayTypeSymbol)variable.Type;

                    if (!IsValidReadableArrayIndexCount(arrayType, expression.Arguments.Count))
                    {
                        ReportReadableArrayIndexCount(variable.Name, arrayType.Rank, expression.Arguments.Count, nameExpression.IdentifierToken.Location);

                        foreach (var argument in expression.Arguments)
                        {
                            BindExpression(argument);
                        }

                        return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
                    }

                    return BindArrayAccess(
                        new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable),
                        expression.Arguments,
                        nameExpression.IdentifierToken.Location,
                        variable.Name);
                }

                return BindInvocationExpression(
                    new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable),
                    expression,
                    nameExpression.IdentifierToken.Location,
                    variable.Name);
            }

            if (_scope.TryLookupFunction(nameExpression.IdentifierToken.Text, out var function) && function is not null)
            {
                if (TryReportUnsupportedNamedArguments(expression))
                {
                    return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
                }

                return BindInvocationExpression(
                    new BoundFunctionExpression(nameExpression.IdentifierToken.Location, function),
                    expression,
                    nameExpression.IdentifierToken.Location,
                    function.Name);
            }

            ReportUnknownCallable(nameExpression.IdentifierToken.Text, nameExpression.IdentifierToken.Location);

            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
        }

        if (expression.Target is FieldAccessExpressionSyntax methodAccess
            && TryBindTypeMethodCall(methodAccess, expression, out var methodCall))
        {
            return methodCall;
        }

        var target = BindExpression(expression.Target);

        if (TryReportUnsupportedNamedArguments(expression))
        {
            return new BoundErrorExpression(GetLocation(expression.Target));
        }

        if (target.Type is ArrayTypeSymbol)
        {
            var displayName = GetArrayTargetDisplayName(target);
            var arrayType = (ArrayTypeSymbol)target.Type;

            if (!IsValidReadableArrayIndexCount(arrayType, expression.Arguments.Count))
            {
                ReportReadableArrayIndexCount(displayName, arrayType.Rank, expression.Arguments.Count, GetLocation(expression.Target));

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(GetLocation(expression.Target));
            }

            return BindArrayAccess(target, expression.Arguments, GetLocation(expression.Target), displayName);
        }

        return BindInvocationExpression(target, expression, GetLocation(expression.Target), "вызываемого значения");
    }

    private BoundExpression BindAssignableTarget(ExpressionSyntax target)
    {
        return target switch
        {
            NameExpressionSyntax nameExpression => BindAssignableNameTarget(nameExpression),
            CallExpressionSyntax callExpression => BindAssignableCallTarget(callExpression),
            FieldAccessExpressionSyntax fieldAccessExpression => BindFieldAccessExpression(fieldAccessExpression),
            _ => new BoundErrorExpression(GetLocation(target))
        };
    }

    private BoundExpression BindAssignableNameTarget(NameExpressionSyntax expression)
    {
        var variable = LookupVariable(expression.IdentifierToken.Text, expression.IdentifierToken.Location);
        return new BoundVariableExpression(expression.IdentifierToken.Location, variable);
    }

    private BoundExpression BindInvocationExpression(
        BoundExpression target,
        CallExpressionSyntax expression,
        TextLocation location,
        string callableName)
    {
        if (target.Type == TypeSymbol.Error)
        {
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        if (target.Type is not FunctionTypeSymbol functionType)
        {
            ReportValueIsNotCallable(target.Type, location);

            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(location);
        }

        if (target is BoundFunctionExpression functionExpression
            && functionExpression.Function.IsBuiltIn
            && BuiltinFunctions.TryGetDescriptors(functionExpression.Function.Name, out var descriptors))
        {
            return BindBuiltInInvocation(functionExpression, expression, descriptors, location, callableName);
        }

        if (target is not BoundFunctionExpression && functionType.ParameterPassingModes.Any(static mode => mode.IsByRef()))
        {
            ReportUnsupportedByRefCallableValue(callableName, location);

            var invalidArguments = expression.Arguments
                .Select((argument, index) => index < functionType.ParameterTypes.Count
                    ? BindExpression(argument, functionType.ParameterTypes[index])
                    : BindExpression(argument))
                .ToArray();

            return new BoundCallExpression(location, target, invalidArguments, functionType.ReturnType);
        }

        if (expression.Arguments.Count != functionType.ParameterTypes.Count)
        {
            ReportWrongArgumentCount(callableName, functionType.ParameterTypes.Count, expression.Arguments.Count, location);
        }

        return BindDeclaredInvocation(target, expression, functionType, location);
    }

    private bool TryReportUnsupportedNamedArguments(CallExpressionSyntax expression)
    {
        foreach (var argument in expression.Arguments)
        {
            if (argument is not NamedArgumentExpressionSyntax namedArgument)
            {
                continue;
            }

            ReportNamedArgumentsOnlySupportedForTypeLiterals(namedArgument.ColonEqualsToken.Location);

            foreach (var nestedArgument in expression.Arguments)
            {
                BindCallArgumentForDiagnostics(nestedArgument);
            }

            return true;
        }

        return false;
    }

    private void BindCallArgumentForDiagnostics(ExpressionSyntax argument)
    {
        if (argument is NamedArgumentExpressionSyntax namedArgument)
        {
            BindExpression(namedArgument.Expression);
            return;
        }

        BindExpression(argument);
    }

    private BoundExpression BindDeclaredInvocation(
        BoundExpression target,
        CallExpressionSyntax expression,
        FunctionTypeSymbol functionType,
        TextLocation location)
    {
        var arguments = new List<BoundExpression>(expression.Arguments.Count);

        for (var index = 0; index < expression.Arguments.Count; index++)
        {
            var argument = expression.Arguments[index];
            var expectsByRef = index < functionType.ParameterPassingModes.Count && functionType.ParameterPassingModes[index].IsByRef();

            if (index < functionType.ParameterTypes.Count)
            {
                arguments.Add(expectsByRef
                    ? BindByRefArgument(argument, functionType.ParameterTypes[index])
                    : BindExpression(argument, functionType.ParameterTypes[index]));
            }
            else
            {
                arguments.Add(BindExpression(argument));
            }
        }

        return new BoundCallExpression(location, target, arguments, functionType.ReturnType);
    }

    private BoundExpression BindByRefArgument(ExpressionSyntax argument, TypeSymbol expectedType)
    {
        var boundArgument = BindAssignableTarget(argument);

        if (boundArgument.Type == TypeSymbol.Error)
        {
            var fallback = BindExpression(argument, expectedType);

            if (fallback.Type != TypeSymbol.Error)
            {
                ReportInvalidByRefArgument(GetLocation(argument));
            }

            return new BoundErrorExpression(GetLocation(argument));
        }

        if (boundArgument is not BoundVariableExpression variableExpression)
        {
            ReportInvalidByRefArgument(GetLocation(argument));
            return new BoundErrorExpression(GetLocation(argument));
        }

        if (variableExpression.Variable.IsReadOnly)
        {
            ReportAssignToConst(variableExpression.Variable.Name, GetLocation(argument));
            return new BoundErrorExpression(GetLocation(argument));
        }

        if (variableExpression.Type != expectedType)
        {
            ReportTypeMismatch(variableExpression.Type, expectedType, GetLocation(argument));
            return new BoundErrorExpression(GetLocation(argument));
        }

        return variableExpression;
    }

}
