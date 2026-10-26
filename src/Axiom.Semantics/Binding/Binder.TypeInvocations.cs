using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundExpression BindFieldAccessExpression(FieldAccessExpressionSyntax expression, bool allowArrayTarget = false)
    {
        var target = BindExpression(expression.Target);

        if (target.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        if (target.Type is RuntimeErrorTypeSymbol runtimeErrorType)
        {
            if (!runtimeErrorType.TryGetField(expression.Identifier.Text, out var runtimeErrorField) || runtimeErrorField is null)
            {
                ReportUnknownField(runtimeErrorType, expression.Identifier.Text, expression.Identifier.Location);
                return new BoundErrorExpression(expression.Identifier.Location);
            }

            return new BoundFieldAccessExpression(expression.Identifier.Location, target, runtimeErrorField);
        }

        if (target.Type is DictionaryTypeSymbol dictionaryType)
        {
            if (string.Equals(expression.Identifier.Text, "Count", StringComparison.OrdinalIgnoreCase))
            {
                return BindDictionaryCountProperty(target, dictionaryType, expression);
            }

            if (IsDictionaryMethodName(expression.Identifier.Text))
            {
                ReportDictionaryMemberRequiresParentheses(expression.Identifier.Text, dictionaryType, expression.Identifier.Location);
                return new BoundErrorExpression(expression.Identifier.Location);
            }

            ReportInvalidDictionaryMember(expression.Identifier.Text, dictionaryType, expression.Identifier.Location);
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        if (target.Type is SetTypeSymbol setType)
        {
            if (string.Equals(expression.Identifier.Text, "Count", StringComparison.OrdinalIgnoreCase))
            {
                return BindSetCountProperty(target, setType, expression);
            }

            if (IsSetMethodName(expression.Identifier.Text))
            {
                ReportSetMemberRequiresParentheses(expression.Identifier.Text, setType, expression.Identifier.Location);
                return new BoundErrorExpression(expression.Identifier.Location);
            }

            ReportInvalidSetMember(expression.Identifier.Text, setType, expression.Identifier.Location);
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        if (target.Type is not UserDefinedTypeSymbol userType)
        {
            ReportInvalidFieldAccess(target.Type, expression.DotToken.Location);
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        if (!userType.TryGetField(expression.Identifier.Text, out var field) || field is null)
        {
            ReportUnknownField(userType, expression.Identifier.Text, expression.Identifier.Location);
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        _editorReferences?.Add(new(field, expression.Identifier.Location, false));
        if (_usesSquareArraySyntax && field.IsArray && !allowArrayTarget)
        {
            ReportArrayNameRequiresBrackets(field.Name, expression.Identifier.Location);
            return new BoundErrorExpression(expression.Identifier.Location);
        }

        return new BoundFieldAccessExpression(expression.Identifier.Location, target, field);
    }

    private bool TryBindTypeMethodCall(
        FieldAccessExpressionSyntax methodAccess,
        CallExpressionSyntax expression,
        out BoundExpression result)
    {
        result = null!;

        var receiver = BindExpression(methodAccess.Target);
        if (receiver.Type == TypeSymbol.Error)
        {
            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            result = new BoundErrorExpression(methodAccess.Identifier.Location);
            return true;
        }

        if (receiver.Type is DictionaryTypeSymbol)
        {
            result = BindDictionaryMemberCall(receiver, methodAccess, expression);
            return true;
        }

        if (receiver.Type is SetTypeSymbol)
        {
            result = BindSetMemberCall(receiver, methodAccess, expression);
            return true;
        }

        if (receiver.Type is not UserDefinedTypeSymbol userType
            || !userType.TryGetMethods(methodAccess.Identifier.Text, out var allMethods)
            || allMethods is null)
        {
            return false;
        }

        if (TryReportUnsupportedNamedArguments(expression))
        {
            result = new BoundErrorExpression(methodAccess.Identifier.Location);
            return true;
        }

        var methods = userType.GetMethods(methodAccess.Identifier.Text, expression.Arguments.Count);
        if (methods.Count == 0)
        {
            ReportWrongArgumentCount(
                methodAccess.Identifier.Text,
                userType.GetMethodArities(methodAccess.Identifier.Text),
                expression.Arguments.Count,
                methodAccess.Identifier.Location);

            var invalidArguments = new List<BoundExpression>(expression.Arguments.Count + 1)
            {
                receiver
            };

            invalidArguments.AddRange(expression.Arguments.Select(argument => BindExpression(argument)));
            result = new BoundCallExpression(
                methodAccess.Identifier.Location,
                new BoundFunctionExpression(methodAccess.Identifier.Location, allMethods[0]),
                invalidArguments,
                allMethods[0].ReturnType);
            return true;
        }

        if (methods.Count == 1)
        {
            result = BindTypeMethodCallExpression(receiver, methods[0], expression, methodAccess.Identifier.Location);
            return true;
        }

        var rawArguments = expression.Arguments
            .Select(argument => BindExpression(argument))
            .ToArray();

        var candidateMethods = methods
            .Where(method => CanBindVisibleCallableArguments(expression.Arguments, rawArguments, method, visibleParameterOffset: 1))
            .ToArray();

        if (candidateMethods.Length == 0)
        {
            result = BindTypeMethodCallExpression(receiver, methods[0], expression, methodAccess.Identifier.Location);
            return true;
        }

        if (candidateMethods.Length == 1)
        {
            result = BindTypeMethodCallExpressionFromRawArguments(receiver, candidateMethods[0], expression, rawArguments, methodAccess.Identifier.Location);
            return true;
        }

        var bestScore = candidateMethods
            .Max(method => CountExactVisibleArgumentMatches(rawArguments, method, visibleParameterOffset: 1));
        var bestMethods = candidateMethods
            .Where(method => CountExactVisibleArgumentMatches(rawArguments, method, visibleParameterOffset: 1) == bestScore)
            .ToArray();

        if (bestMethods.Length == 1)
        {
            result = BindTypeMethodCallExpressionFromRawArguments(receiver, bestMethods[0], expression, rawArguments, methodAccess.Identifier.Location);
            return true;
        }

        ReportAmbiguousTypeMethodCall(userType.Name, methodAccess.Identifier.Text, bestMethods, methodAccess.Identifier.Location);
        result = new BoundErrorExpression(methodAccess.Identifier.Location);
        return true;
    }

    private BoundExpression BindRecordLiteralExpression(UserDefinedTypeSymbol recordType, CallExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(recordType);
        ArgumentNullException.ThrowIfNull(expression);

        if (expression.Arguments.Count == 0)
        {
            return new BoundRecordLiteralExpression(GetLocation(expression), recordType, Array.Empty<BoundRecordLiteralFieldInitializer>());
        }

        var hasNamedArguments = expression.Arguments.Any(static argument => argument is NamedArgumentExpressionSyntax);
        var hasPositionalArguments = expression.Arguments.Any(static argument => argument is not NamedArgumentExpressionSyntax);

        if (hasNamedArguments && hasPositionalArguments)
        {
            var firstNamedArgument = (NamedArgumentExpressionSyntax)expression.Arguments.First(static argument => argument is NamedArgumentExpressionSyntax);
            ReportMixedTypeLiteralInitialization(
                recordType.Name,
                firstNamedArgument.ColonEqualsToken.Location);

            foreach (var argument in expression.Arguments)
            {
                if (argument is NamedArgumentExpressionSyntax namedArgument)
                {
                    BindExpression(namedArgument.Expression);
                }
                else
                {
                    BindExpression(argument);
                }
            }

            return new BoundErrorExpression(GetLocation(expression));
        }

        if (!hasNamedArguments)
        {
            return BindPositionalRecordLiteralExpression(recordType, expression);
        }

        var initializers = new List<BoundRecordLiteralFieldInitializer>(expression.Arguments.Count);
        var seenFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasErrors = false;

        foreach (var argument in expression.Arguments)
        {
            if (argument is not NamedArgumentExpressionSyntax namedArgument)
            {
                ReportTypeLiteralRequiresNamedFields(recordType.Name, GetLocation(argument));
                BindExpression(argument);
                hasErrors = true;
                continue;
            }

            if (!recordType.TryGetField(namedArgument.IdentifierToken.Text, out var field) || field is null)
            {
                ReportUnknownField(recordType, namedArgument.IdentifierToken.Text, namedArgument.IdentifierToken.Location);
                BindExpression(namedArgument.Expression);
                hasErrors = true;
                continue;
            }

            if (!seenFields.Add(field.Name))
            {
                ReportDuplicateTypeLiteralField(recordType.Name, field.Name, namedArgument.IdentifierToken.Location);
                BindExpression(namedArgument.Expression, field.Type);
                hasErrors = true;
                continue;
            }

            _editorReferences?.Add(new(field, namedArgument.IdentifierToken.Location, false));

            var value = BindExpression(namedArgument.Expression, field.Type);
            initializers.Add(new BoundRecordLiteralFieldInitializer(field, value));
        }

        return hasErrors
            ? new BoundErrorExpression(GetLocation(expression))
            : new BoundRecordLiteralExpression(GetLocation(expression), recordType, initializers);
    }

    private BoundExpression BindTypeCreationExpression(UserDefinedTypeSymbol recordType, CallExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(recordType);
        ArgumentNullException.ThrowIfNull(expression);

        var hasNamedArguments = expression.Arguments.Any(static argument => argument is NamedArgumentExpressionSyntax);
        if (hasNamedArguments)
        {
            return BindRecordLiteralExpression(recordType, expression);
        }

        if (recordType.Constructors.Count == 0)
        {
            return BindRecordLiteralExpression(recordType, expression);
        }

        var constructors = recordType.GetConstructors(expression.Arguments.Count);
        if (constructors.Count == 0)
        {
            ReportWrongArgumentCount(recordType.Name, recordType.GetConstructorArities(), expression.Arguments.Count, GetLocation(expression.Target));

            foreach (var argument in expression.Arguments)
            {
                BindExpression(argument);
            }

            return new BoundErrorExpression(GetLocation(expression));
        }

        if (constructors.Count == 1)
        {
            return BindTypeConstructorExpression(recordType, constructors[0], expression);
        }

        var rawArguments = expression.Arguments
            .Select(argument => BindExpression(argument))
            .ToArray();

        var candidateConstructors = constructors
            .Where(constructor => CanBindVisibleCallableArguments(expression.Arguments, rawArguments, constructor, visibleParameterOffset: 1))
            .ToArray();

        if (candidateConstructors.Length == 0)
        {
            return BindTypeConstructorExpression(recordType, constructors[0], expression);
        }

        if (candidateConstructors.Length == 1)
        {
            return BindTypeConstructorExpressionFromRawArguments(recordType, candidateConstructors[0], expression, rawArguments);
        }

        var bestScore = candidateConstructors
            .Max(constructor => CountExactVisibleArgumentMatches(rawArguments, constructor, visibleParameterOffset: 1));
        var bestConstructors = candidateConstructors
            .Where(constructor => CountExactVisibleArgumentMatches(rawArguments, constructor, visibleParameterOffset: 1) == bestScore)
            .ToArray();

        if (bestConstructors.Length == 1)
        {
            return BindTypeConstructorExpressionFromRawArguments(recordType, bestConstructors[0], expression, rawArguments);
        }

        ReportAmbiguousTypeConstructorCall(recordType.Name, bestConstructors, GetLocation(expression.Target));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private bool CanBindVisibleCallableArguments(
        IReadOnlyList<ExpressionSyntax> argumentSyntaxes,
        IReadOnlyList<BoundExpression> rawArguments,
        FunctionSymbol callable,
        int visibleParameterOffset)
    {
        if (rawArguments.Count != callable.Parameters.Count - visibleParameterOffset)
        {
            return false;
        }

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var argument = rawArguments[index];
            var parameter = callable.Parameters[index + visibleParameterOffset];

            if (argument.Type == TypeSymbol.Error)
            {
                return false;
            }

            if (parameter.IsByRef)
            {
                if (BindAssignableTarget(argumentSyntaxes[index]) is not BoundVariableExpression variableExpression
                    || variableExpression.Type != parameter.Type)
                {
                    return false;
                }

                continue;
            }

            if (argument.Type == parameter.Type || CanConvert(argument.Type, parameter.Type))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static int CountExactVisibleArgumentMatches(
        IReadOnlyList<BoundExpression> rawArguments,
        FunctionSymbol callable,
        int visibleParameterOffset)
    {
        var matches = 0;

        for (var index = 0; index < rawArguments.Count; index++)
        {
            if (rawArguments[index].Type == callable.Parameters[index + visibleParameterOffset].Type)
            {
                matches++;
            }
        }

        return matches;
    }

    private BoundExpression BindTypeMethodCallExpressionFromRawArguments(
        BoundExpression receiver,
        FunctionSymbol method,
        CallExpressionSyntax expression,
        IReadOnlyList<BoundExpression> rawArguments,
        TextLocation location)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(rawArguments);

        var arguments = new List<BoundExpression>(rawArguments.Count + 1)
        {
            receiver
        };

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var parameter = method.Parameters[index + 1];
            arguments.Add(parameter.IsByRef
                ? BindByRefArgument(expression.Arguments[index], parameter.Type)
                : BindExpression(expression.Arguments[index], parameter.Type));
        }

        return new BoundCallExpression(
            location,
            new BoundFunctionExpression(location, method),
            arguments,
            method.ReturnType);
    }

    private BoundExpression BindTypeMethodCallExpression(
        BoundExpression receiver,
        FunctionSymbol method,
        CallExpressionSyntax expression,
        TextLocation location)
    {
        var rawArguments = expression.Arguments
            .Select(argument => BindExpression(argument))
            .ToArray();

        return BindTypeMethodCallExpressionFromRawArguments(receiver, method, expression, rawArguments, location);
    }

    private BoundExpression BindTypeConstructorExpressionFromRawArguments(
        UserDefinedTypeSymbol recordType,
        FunctionSymbol constructor,
        CallExpressionSyntax expression,
        IReadOnlyList<BoundExpression> rawArguments)
    {
        ArgumentNullException.ThrowIfNull(recordType);
        ArgumentNullException.ThrowIfNull(constructor);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(rawArguments);

        var arguments = new List<BoundExpression>(rawArguments.Count);

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var parameter = constructor.Parameters[index + 1];
            arguments.Add(parameter.IsByRef
                ? BindByRefArgument(expression.Arguments[index], parameter.Type)
                : BindExpression(expression.Arguments[index], parameter.Type));
        }

        return new BoundTypeConstructorExpression(GetLocation(expression), recordType, constructor, arguments);
    }

    private BoundExpression BindTypeConstructorExpression(
        UserDefinedTypeSymbol recordType,
        FunctionSymbol constructor,
        CallExpressionSyntax expression)
    {
        var rawArguments = expression.Arguments
            .Select(argument => BindExpression(argument))
            .ToArray();

        return BindTypeConstructorExpressionFromRawArguments(recordType, constructor, expression, rawArguments);
    }

    private BoundExpression BindPositionalRecordLiteralExpression(UserDefinedTypeSymbol recordType, CallExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(recordType);
        ArgumentNullException.ThrowIfNull(expression);

        if (expression.Arguments.Count != recordType.Fields.Count)
        {
            ReportTypeLiteralPositionalArity(recordType.Name, recordType.Fields.Count, expression.Arguments.Count, GetLocation(expression));

            for (var index = 0; index < expression.Arguments.Count; index++)
            {
                var expectedType = index < recordType.Fields.Count
                    ? recordType.Fields[index].Type
                    : null;

                if (expectedType is null)
                {
                    BindExpression(expression.Arguments[index]);
                }
                else
                {
                    BindExpression(expression.Arguments[index], expectedType);
                }
            }

            return new BoundErrorExpression(GetLocation(expression));
        }

        var initializers = new List<BoundRecordLiteralFieldInitializer>(recordType.Fields.Count);

        for (var index = 0; index < recordType.Fields.Count; index++)
        {
            var field = recordType.Fields[index];
            var value = BindExpression(expression.Arguments[index], field.Type);
            initializers.Add(new BoundRecordLiteralFieldInitializer(field, value));
        }

        return new BoundRecordLiteralExpression(GetLocation(expression), recordType, initializers);
    }

}
