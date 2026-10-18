using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundExpression BindCallExpression(CallExpressionSyntax expression)
    {
        if (expression.Target is NameExpressionSyntax nameExpression)
        {
            if (_declaredTypes.TryGetValue(nameExpression.IdentifierToken.Text, out var recordType))
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

    private BoundExpression BindFieldAccessExpression(FieldAccessExpressionSyntax expression)
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

        if (variableExpression.Type != expectedType)
        {
            ReportTypeMismatch(variableExpression.Type, expectedType, GetLocation(argument));
            return new BoundErrorExpression(GetLocation(argument));
        }

        return variableExpression;
    }

    private BoundExpression BindBuiltInInvocation(
        BoundFunctionExpression target,
        CallExpressionSyntax expression,
        IReadOnlyList<BuiltinFunctionDescriptor> descriptors,
        TextLocation location,
        string callableName)
    {
        var arityMatches = descriptors
            .Where(descriptor => descriptor.Parameters.Count == expression.Arguments.Count)
            .ToArray();

        if (arityMatches.Length == 0)
        {
            ReportWrongArgumentCount(
                callableName,
                descriptors.Select(static descriptor => descriptor.Parameters.Count).ToArray(),
                expression.Arguments.Count,
                location);

            var invalidArguments = expression.Arguments
                .Select(argument => BindExpression(argument))
                .ToArray();

            return new BoundCallExpression(location, target, invalidArguments, target.Function.ReturnType);
        }

        if (arityMatches.Length == 1)
        {
            return BindResolvedBuiltInInvocation(expression, arityMatches[0], location, callableName);
        }

        var rawArguments = expression.Arguments
            .Select(argument => BindExpression(argument))
            .ToArray();

        var matchedDescriptor = arityMatches.FirstOrDefault(descriptor => ArgumentsMatchExactly(rawArguments, descriptor));
        if (matchedDescriptor is not null)
        {
            return BindResolvedBuiltInInvocationFromRawArguments(
                expression,
                matchedDescriptor,
                location,
                rawArguments);
        }

        if (arityMatches.Length > 0 && IsTemporalArithmeticBuiltinId(arityMatches[0].Id))
        {
            return BindStrictTemporalArithmeticInvocation(expression, arityMatches, location, rawArguments);
        }

        matchedDescriptor = arityMatches.FirstOrDefault(descriptor => CanBindBuiltInArguments(rawArguments, descriptor));

        return BindResolvedBuiltInInvocationFromRawArguments(
            expression,
            matchedDescriptor ?? arityMatches[0],
            location,
            rawArguments);
    }

    private static bool IsTemporalArithmeticBuiltinId(BuiltinFunctionId id)
    {
        return id is BuiltinFunctionId.DateAddDateValue
            or BuiltinFunctionId.DateAddTimeValue
            or BuiltinFunctionId.DateAddDateTimeValue
            or BuiltinFunctionId.DateDiffDateValues
            or BuiltinFunctionId.DateDiffTimeValues
            or BuiltinFunctionId.DateDiffDateTimeValues
            or BuiltinFunctionId.DatePartDateValue
            or BuiltinFunctionId.DatePartTimeValue
            or BuiltinFunctionId.DatePartDateTimeValue;
    }

    private static int CountArgumentsMatchingExactly(
        IReadOnlyList<BoundExpression> rawArguments,
        BuiltinFunctionDescriptor descriptor)
    {
        var matches = 0;

        for (var index = 0; index < rawArguments.Count; index++)
        {
            if (rawArguments[index].Type == descriptor.Parameters[index].Type)
            {
                matches++;
            }
        }

        return matches;
    }

    private BoundExpression BindStrictTemporalArithmeticInvocation(
        CallExpressionSyntax expression,
        IReadOnlyList<BuiltinFunctionDescriptor> arityMatches,
        TextLocation location,
        IReadOnlyList<BoundExpression> rawArguments)
    {
        // Значения DATEADD/DATEDIFF/DATEPART должны быть нативными значениями одного вида.
        // Неявные преобразования между DATE/TIME/DATETIME и из STRING здесь запрещены:
        // смешение видов не должно молча усекаться (дефект DT-001), а строковая форма
        // арифметики удалена — строки нужно явно преобразовывать через DATEVALUE/TIMEVALUE.
        var reportDescriptor = arityMatches
            .OrderByDescending(descriptor => CountArgumentsMatchingExactly(rawArguments, descriptor))
            .First();

        var arguments = new List<BoundExpression>(rawArguments.Count);

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var argument = rawArguments[index];
            var parameterType = reportDescriptor.Parameters[index].Type;

            if (argument.Type == TypeSymbol.Error || argument.Type == parameterType)
            {
                arguments.Add(argument);
                continue;
            }

            ReportTypeMismatch(argument.Type, parameterType, GetLocation(expression.Arguments[index]));
            arguments.Add(new BoundErrorExpression(GetLocation(expression.Arguments[index])));
        }

        var resolvedTarget = new BoundFunctionExpression(location, reportDescriptor.CreateSymbol());
        return new BoundCallExpression(location, resolvedTarget, arguments, reportDescriptor.DeclaredReturnType);
    }

    private static bool ArgumentsMatchExactly(
        IReadOnlyList<BoundExpression> rawArguments,
        BuiltinFunctionDescriptor descriptor)
    {
        if (rawArguments.Count != descriptor.Parameters.Count)
        {
            return false;
        }

        for (var index = 0; index < rawArguments.Count; index++)
        {
            if (rawArguments[index].Type != descriptor.Parameters[index].Type)
            {
                return false;
            }
        }

        return true;
    }

    private BoundExpression BindResolvedBuiltInInvocationFromRawArguments(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        IReadOnlyList<BoundExpression> rawArguments)
    {
        var resolvedSymbol = descriptor.CreateSymbol();
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        var arguments = new List<BoundExpression>(expression.Arguments.Count);

        for (var index = 0; index < expression.Arguments.Count; index++)
        {
            arguments.Add(index < descriptor.Parameters.Count
                ? ApplyBuiltInArgumentType(descriptor, expression.Arguments[index], rawArguments[index], index)
                : rawArguments[index]);
        }

        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedSymbol.ReturnType);
    }

    /// <summary>
    /// Аргумент встроенной функции подчиняется общему договору временных
    /// преобразований. Исключение одно: функции извлечения части принимают
    /// DATETIME и канонический текст напрямую, потому что отбрасывание
    /// ненужного компонента и есть их назначение.
    /// </summary>
    private BoundExpression ApplyBuiltInArgumentType(
        BuiltinFunctionDescriptor descriptor,
        ExpressionSyntax argumentSyntax,
        BoundExpression boundArgument,
        int parameterIndex)
    {
        var parameterType = descriptor.Parameters[parameterIndex].Type;

        if (TemporalConversionRules.IsPartExtractionBuiltin(descriptor.Id)
            && (parameterType == TypeSymbol.Date || parameterType == TypeSymbol.Time))
        {
            return ApplyPartExtractionArgument(argumentSyntax, boundArgument, parameterType);
        }

        return ApplyExpectedType(argumentSyntax, boundArgument, parameterType);
    }

    private BoundExpression BindResolvedBuiltInInvocation(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        string callableName)
    {
        var resolvedSymbol = descriptor.CreateSymbol();
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        var functionType = FunctionTypeSymbol.FromFunction(resolvedSymbol);

        if (descriptor.BindingKind == BuiltinBindingKind.Numeric)
        {
            return BindNumericBuiltInCall(resolvedTarget, expression, descriptor, functionType, location, callableName);
        }

        if (descriptor.BindingKind == BuiltinBindingKind.Array)
        {
            return BindArrayBuiltInCall(expression, descriptor, location);
        }

        return BindDeclaredBuiltInInvocation(expression, descriptor, location);
    }

    private BoundExpression BindDeclaredBuiltInInvocation(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location)
    {
        var resolvedSymbol = descriptor.CreateSymbol();
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        var arguments = new List<BoundExpression>(expression.Arguments.Count);

        for (var index = 0; index < expression.Arguments.Count; index++)
        {
            var argument = expression.Arguments[index];

            if (index < descriptor.Parameters.Count)
            {
                var parameterType = descriptor.Parameters[index].Type;
                var boundArgument = BindExpression(argument);

                arguments.Add(TemporalConversionRules.IsPartExtractionBuiltin(descriptor.Id)
                        && (parameterType == TypeSymbol.Date || parameterType == TypeSymbol.Time)
                    ? ApplyPartExtractionArgument(argument, boundArgument, parameterType)
                    : ApplyExpectedType(argument, boundArgument, parameterType));
            }
            else
            {
                arguments.Add(BindExpression(argument));
            }
        }

        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedSymbol.ReturnType);
    }

    private bool CanBindBuiltInArguments(
        IReadOnlyList<BoundExpression> rawArguments,
        BuiltinFunctionDescriptor descriptor)
    {
        if (rawArguments.Count != descriptor.Parameters.Count)
        {
            return false;
        }

        ArrayTypeSymbol? resolvedArrayType = null;
        TypeSymbol? reshapeElementType = null;

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var argument = rawArguments[index];
            var parameterType = descriptor.Parameters[index].Type;

            if (argument.Type == TypeSymbol.Error)
            {
                return false;
            }

            if (descriptor.BindingKind == BuiltinBindingKind.Array
                && parameterType is ArrayTypeSymbol)
            {
                if (argument.Type is not ArrayTypeSymbol actualArrayType)
                {
                    return false;
                }

                if (descriptor.Id == BuiltinFunctionId.ArrayReshape)
                {
                    reshapeElementType ??= actualArrayType.ElementType;

                    if (actualArrayType.ElementType != reshapeElementType)
                    {
                        return false;
                    }

                    continue;
                }

                resolvedArrayType ??= actualArrayType;

                if (actualArrayType != resolvedArrayType)
                {
                    return false;
                }

                continue;
            }

            if (argument.Type == parameterType)
            {
                continue;
            }

            if (descriptor.BindingKind == BuiltinBindingKind.Numeric && IsNumeric(argument.Type))
            {
                continue;
            }

            if (CanConvert(argument.Type, parameterType))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private BoundExpression BindArrayBuiltInCall(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location)
    {
        var arguments = new List<BoundExpression>(expression.Arguments.Count);
        var firstArgument = BindExpression(expression.Arguments[0]);
        arguments.Add(firstArgument);

        if (firstArgument.Type == TypeSymbol.Error)
        {
            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, descriptor.DeclaredReturnType);
        }

        if (firstArgument.Type is not ArrayTypeSymbol arrayType)
        {
            ReportExpectedArrayType(firstArgument.Type, GetLocation(expression.Arguments[0]));

            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, descriptor.DeclaredReturnType);
        }

        if (RequiresExplicitDimensionArgument(descriptor.Id, arrayType.Rank, expression.Arguments.Count))
        {
            ReportArrayDimensionArgumentRequired(descriptor.Name, arrayType.Rank, location);

            for (var index = 1; index < expression.Arguments.Count; index++)
            {
                arguments.Add(BindExpression(expression.Arguments[index]));
            }

            var invalidReturnType = ResolveArrayBuiltInReturnType(descriptor, arrayType);
            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, invalidReturnType);
        }

        if ((descriptor.Id == BuiltinFunctionId.ArrayBinarySearch
            || descriptor.Id == BuiltinFunctionId.ArrayToSorted
            || descriptor.Id == BuiltinFunctionId.ArraySort)
            && !SupportsOrderedComparison(arrayType.ElementType))
        {
            if (descriptor.Id == BuiltinFunctionId.ArrayToSorted)
            {
                ReportArrayToSortedRequiresOrderedArray(arrayType.ElementType, GetLocation(expression.Arguments[0]));
            }
            else if (descriptor.Id == BuiltinFunctionId.ArraySort)
            {
                ReportArraySortRequiresOrderedArray(arrayType.ElementType, GetLocation(expression.Arguments[0]));
            }
            else
            {
                ReportBinarySearchRequiresOrderedArray(arrayType.ElementType, GetLocation(expression.Arguments[0]));
            }

            for (var index = 1; index < expression.Arguments.Count; index++)
            {
                arguments.Add(BindExpression(expression.Arguments[index]));
            }

            var invalidReturnType = ResolveArrayBuiltInReturnType(descriptor, arrayType);
            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, invalidReturnType);
        }

        if (RequiresNumericAggregateArray(descriptor.Id, arrayType.ElementType)
            || RequiresOrderedAggregateArray(descriptor.Id, arrayType.ElementType))
        {
            ReportArrayAggregateRequiresSupportedArray(
                descriptor.Name,
                arrayType.ElementType,
                GetLocation(expression.Arguments[0]));

            for (var index = 1; index < expression.Arguments.Count; index++)
            {
                arguments.Add(BindExpression(expression.Arguments[index]));
            }

            var invalidReturnType = ResolveArrayBuiltInReturnType(descriptor, arrayType);
            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, invalidReturnType);
        }

        if (descriptor.Id == BuiltinFunctionId.ArrayMap)
        {
            return BindPolymorphicArrayMapBuiltInCall(expression, descriptor, location, arrayType, arguments);
        }

        if (descriptor.Id == BuiltinFunctionId.ArrayReduce)
        {
            return BindPolymorphicArrayReduceBuiltInCall(expression, descriptor, location, arrayType, arguments);
        }

        if (descriptor.Id == BuiltinFunctionId.ArrayReshape)
        {
            return BindArrayReshapeBuiltInCall(expression, descriptor, location, arrayType, arguments);
        }

        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count)
        {
            arrayType
        };

        for (var index = 1; index < expression.Arguments.Count; index++)
        {
            var parameterType = ResolveArrayBuiltInParameterType(descriptor, arrayType, index);

            if (descriptor.Id == BuiltinFunctionId.ArrayConcat && index == 1)
            {
                var rightArgument = BindExpression(expression.Arguments[index]);

                if (rightArgument.Type is ArrayTypeSymbol rightArrayType
                    && rightArrayType.Rank != arrayType.Rank)
                {
                    ReportArrayConcatRankMismatch(
                        arrayType.Rank,
                        rightArrayType.Rank,
                        GetLocation(expression.Arguments[index]));
                    arguments.Add(rightArgument);
                    parameterTypes.Add(parameterType);
                    continue;
                }

                arguments.Add(ApplyExpectedType(expression.Arguments[index], rightArgument, parameterType));
                parameterTypes.Add(parameterType);
                continue;
            }

            var boundArgument = BindExpression(expression.Arguments[index], parameterType);
            arguments.Add(boundArgument);
            parameterTypes.Add(parameterType);
        }

        var resolvedReturnType = ResolveArrayBuiltInReturnType(descriptor, arrayType);
        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, resolvedReturnType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedSymbol.ReturnType);
    }

    private static TypeSymbol ResolveArrayBuiltInReturnType(BuiltinFunctionDescriptor descriptor, ArrayTypeSymbol arrayType)
    {
        return descriptor.ResultTypePolicy switch
        {
            BuiltinResultTypePolicy.SameAsFirstArgument => arrayType,
            BuiltinResultTypePolicy.SameAsSecondArgument => descriptor.DeclaredReturnType,
            BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument => arrayType.ElementType,
            BuiltinResultTypePolicy.ArrayOfFirstArrayElementType => ArrayTypeSymbol.Create(arrayType.ElementType),
            BuiltinResultTypePolicy.ArrayDynamicSliceResult => arrayType.Rank == 1
                ? arrayType.ElementType
                : ArrayTypeSymbol.Create(arrayType.ElementType, arrayType.Rank - 1),
            BuiltinResultTypePolicy.ArrayIndexCoordinateResult => arrayType.Rank == 1
                ? TypeSymbol.Integer
                : ArrayTypeSymbol.Create(TypeSymbol.Integer),
            BuiltinResultTypePolicy.ArrayFindAllCoordinateResult => arrayType.Rank == 1
                ? ArrayTypeSymbol.Create(TypeSymbol.Integer)
                : ArrayTypeSymbol.Create(TypeSymbol.Integer, rank: 2),
            _ => descriptor.DeclaredReturnType
        };
    }

    private BoundExpression BindPolymorphicArrayMapBuiltInCall(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        ArrayTypeSymbol arrayType,
        List<BoundExpression> arguments)
    {
        var selectorLocation = GetLocation(expression.Arguments[1]);
        var rawSelector = BindExpression(expression.Arguments[1]);
        var selectorReturnType = rawSelector.Type is FunctionTypeSymbol selectorType
            ? selectorType.ReturnType
            : arrayType.ElementType;
        var expectedSelectorType = FunctionTypeSymbol.Create([arrayType.ElementType], selectorReturnType);
        var selectorArgument = rawSelector;
        var resultArrayType = ArrayTypeSymbol.Create(selectorReturnType, arrayType.Rank) as ArrayTypeSymbol;

        if (rawSelector.Type != TypeSymbol.Error)
        {
            if (rawSelector.Type is not FunctionTypeSymbol actualSelectorType)
            {
                ReportTypeMismatch(rawSelector.Type, expectedSelectorType, selectorLocation);
                selectorArgument = new BoundErrorExpression(selectorLocation);
            }
            else if (actualSelectorType != expectedSelectorType)
            {
                ReportTypeMismatch(actualSelectorType, expectedSelectorType, selectorLocation);
                selectorArgument = new BoundErrorExpression(selectorLocation);
            }
            else if (resultArrayType is null)
            {
                ReportArrayMapRejectsNestedArrayResult(selectorReturnType, selectorLocation);
                selectorArgument = new BoundErrorExpression(selectorLocation);
            }
        }

        arguments.Add(selectorArgument);

        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count)
        {
            arrayType,
            expectedSelectorType
        };
        var resolvedReturnType = selectorArgument.Type == TypeSymbol.Error || resultArrayType is null
            ? TypeSymbol.Error
            : resultArrayType;
        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, resolvedReturnType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedReturnType);
    }

    private BoundExpression BindPolymorphicArrayReduceBuiltInCall(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        ArrayTypeSymbol arrayType,
        List<BoundExpression> arguments)
    {
        var rawSeed = BindExpression(expression.Arguments[1]);
        var rawReducer = BindExpression(expression.Arguments[2]);
        var accumulatorType = ResolvePolymorphicArrayReduceAccumulatorType(rawSeed.Type, rawReducer.Type);

        var seedArgument = rawSeed;

        if (rawSeed.Type != TypeSymbol.Error && rawSeed.Type != accumulatorType)
        {
            seedArgument = BindExpression(expression.Arguments[1], accumulatorType);
        }

        var reducerLocation = GetLocation(expression.Arguments[2]);
        var expectedReducerType = FunctionTypeSymbol.Create([accumulatorType, arrayType.ElementType], accumulatorType);
        var reducerArgument = rawReducer;

        if (rawReducer.Type != TypeSymbol.Error)
        {
            if (rawReducer.Type is not FunctionTypeSymbol actualReducerType)
            {
                ReportTypeMismatch(rawReducer.Type, expectedReducerType, reducerLocation);
                reducerArgument = new BoundErrorExpression(reducerLocation);
            }
            else if (actualReducerType != expectedReducerType)
            {
                ReportTypeMismatch(actualReducerType, expectedReducerType, reducerLocation);
                reducerArgument = new BoundErrorExpression(reducerLocation);
            }
        }

        arguments.Add(seedArgument);
        arguments.Add(reducerArgument);

        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count)
        {
            arrayType,
            accumulatorType,
            expectedReducerType
        };
        var resolvedReturnType = seedArgument.Type == TypeSymbol.Error || reducerArgument.Type == TypeSymbol.Error
            ? TypeSymbol.Error
            : accumulatorType;
        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, resolvedReturnType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedReturnType);
    }

    private BoundExpression BindArrayReshapeBuiltInCall(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        ArrayTypeSymbol sourceArrayType,
        List<BoundExpression> arguments)
    {
        var rawTemplate = BindExpression(expression.Arguments[1]);
        arguments.Add(rawTemplate);

        if (rawTemplate.Type == TypeSymbol.Error)
        {
            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, descriptor.DeclaredReturnType);
        }

        if (rawTemplate.Type is not ArrayTypeSymbol templateArrayType)
        {
            ReportExpectedArrayType(rawTemplate.Type, GetLocation(expression.Arguments[1]));

            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, descriptor.DeclaredReturnType);
        }

        if (sourceArrayType.ElementType != templateArrayType.ElementType)
        {
            ReportTypeMismatch(sourceArrayType.ElementType, templateArrayType.ElementType, GetLocation(expression.Arguments[1]));

            var invalidTarget = new BoundFunctionExpression(location, descriptor.CreateSymbol());
            return new BoundCallExpression(location, invalidTarget, arguments, templateArrayType);
        }

        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count)
        {
            sourceArrayType,
            templateArrayType
        };

        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, templateArrayType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedSymbol.ReturnType);
    }

    private static TypeSymbol ResolvePolymorphicArrayReduceAccumulatorType(TypeSymbol seedType, TypeSymbol reducerType)
    {
        if (reducerType is FunctionTypeSymbol reducerFunctionType
            && reducerFunctionType.ParameterTypes.Count >= 1)
        {
            return reducerFunctionType.ParameterTypes[0];
        }

        return seedType;
    }

    private static TypeSymbol ResolveArrayBuiltInParameterType(
        BuiltinFunctionDescriptor descriptor,
        ArrayTypeSymbol arrayType,
        int parameterIndex)
    {
        if (TryResolveArrayCallbackParameterType(descriptor.Id, arrayType, parameterIndex, out var callbackType))
        {
            return callbackType;
        }

        if (descriptor.Parameters[parameterIndex].Type is ArrayTypeSymbol)
        {
            return arrayType;
        }

        if (TryResolveDynamicArrayValueParameterType(descriptor, arrayType, parameterIndex, out var dynamicValueType))
        {
            return dynamicValueType;
        }

        return UsesArrayElementTypeForValueParameter(descriptor.Id, parameterIndex)
            ? arrayType.ElementType
            : descriptor.Parameters[parameterIndex].Type;
    }

    private static bool TryResolveDynamicArrayValueParameterType(
        BuiltinFunctionDescriptor descriptor,
        ArrayTypeSymbol arrayType,
        int parameterIndex,
        out TypeSymbol parameterType)
    {
        if (descriptor.Id == BuiltinFunctionId.ArrayPush && parameterIndex == 1 && descriptor.Parameters.Count >= 3)
        {
            parameterType = arrayType.Rank == 1
                ? arrayType.ElementType
                : ArrayTypeSymbol.Create(arrayType.ElementType, arrayType.Rank - 1);
            return true;
        }

        if (descriptor.Id == BuiltinFunctionId.ArrayInsert && parameterIndex == 2 && descriptor.Parameters.Count >= 4)
        {
            parameterType = arrayType.Rank == 1
                ? arrayType.ElementType
                : ArrayTypeSymbol.Create(arrayType.ElementType, arrayType.Rank - 1);
            return true;
        }

        parameterType = TypeSymbol.Error;
        return false;
    }

    private static bool UsesArrayElementTypeForValueParameter(BuiltinFunctionId id, int parameterIndex)
    {
        if (parameterIndex == 1)
        {
            return id is BuiltinFunctionId.ArrayContains
                or BuiltinFunctionId.ArrayIndexOf
                or BuiltinFunctionId.ArrayLastIndexOf
                or BuiltinFunctionId.ArrayCount
                or BuiltinFunctionId.ArrayFindAll
                or BuiltinFunctionId.ArrayBinarySearch
                or BuiltinFunctionId.ArrayFill
                or BuiltinFunctionId.ArrayPush;
        }

        return id == BuiltinFunctionId.ArrayInsert && parameterIndex == 2;
    }

    private static bool TryResolveArrayCallbackParameterType(
        BuiltinFunctionId id,
        ArrayTypeSymbol arrayType,
        int parameterIndex,
        out TypeSymbol callbackType)
    {
        if (parameterIndex == 1
            && (id == BuiltinFunctionId.ArrayEvery
                || id == BuiltinFunctionId.ArraySome
                || id == BuiltinFunctionId.ArrayFilter))
        {
            callbackType = FunctionTypeSymbol.Create([arrayType.ElementType], TypeSymbol.Boolean);
            return true;
        }

        callbackType = TypeSymbol.Error;
        return false;
    }

    private BoundExpression BindNumericBuiltInCall(
        BoundExpression target,
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        FunctionTypeSymbol functionType,
        TextLocation location,
        string callableName)
    {
        if (expression.Arguments.Count != functionType.ParameterTypes.Count)
        {
            ReportWrongArgumentCount(callableName, functionType.ParameterTypes.Count, expression.Arguments.Count, location);
            var invalidArguments = expression.Arguments
                .Select(argument => BindExpression(argument))
                .ToArray();
            return new BoundCallExpression(location, target, invalidArguments, functionType.ReturnType);
        }

        var rawArguments = new BoundExpression[expression.Arguments.Count];

        for (var index = 0; index < expression.Arguments.Count; index++)
        {
            var argument = BindExpression(expression.Arguments[index]);

            if (argument.Type != TypeSymbol.Error && !IsNumeric(argument.Type))
            {
                ReportTypeMismatch(argument.Type, TypeSymbol.Double, GetLocation(expression.Arguments[index]));
                argument = new BoundErrorExpression(GetLocation(expression.Arguments[index]));
            }

            rawArguments[index] = argument;
        }

        if (rawArguments.Any(static argument => argument.Type == TypeSymbol.Error))
        {
            return new BoundCallExpression(location, target, rawArguments, functionType.ReturnType);
        }

        if (!TryResolveNumericBuiltinArgumentTypes(expression, rawArguments, descriptor, out var resolvedArgumentTypes))
        {
            return new BoundCallExpression(location, target, rawArguments, functionType.ReturnType);
        }

        var boundArguments = RebindNumericBuiltinArguments(expression.Arguments, rawArguments, resolvedArgumentTypes, descriptor);
        var resolvedReturnType = ResolveNumericBuiltinReturnType(boundArguments, functionType, descriptor);

        return new BoundCallExpression(location, target, boundArguments, resolvedReturnType);
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
            var arrayType = (ArrayTypeSymbol)variable.Type;

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

        var target = BindExpression(expression.Target);

        if (target.Type is ArrayTypeSymbol)
        {
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
