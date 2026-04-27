using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundExpression BindCallExpression(CallExpressionSyntax expression)
    {
        if (expression.Target is NameExpressionSyntax nameExpression)
        {
            if (_scope.TryLookupVariable(nameExpression.IdentifierToken.Text, out var variable) && variable is not null)
            {
                if (variable.IsArray)
                {
                    if (expression.Arguments.Count != 1)
                    {
                        ReportWrongArgumentCount(variable.Name, 1, expression.Arguments.Count, nameExpression.IdentifierToken.Location);

                        foreach (var argument in expression.Arguments)
                        {
                            BindExpression(argument);
                        }

                        return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
                    }

                    return BindArrayAccess(
                        new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable),
                        expression.Arguments[0],
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

        var target = BindExpression(expression.Target);

        if (target.Type is ArrayTypeSymbol)
        {
            var displayName = GetArrayTargetDisplayName(target);

            if (expression.Arguments.Count != 1)
            {
                ReportWrongArgumentCount(displayName, 1, expression.Arguments.Count, GetLocation(expression.Target));

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(GetLocation(expression.Target));
            }

            return BindArrayAccess(target, expression.Arguments[0], GetLocation(expression.Target), displayName);
        }

        return BindInvocationExpression(target, expression, GetLocation(expression.Target), "вызываемого значения");
    }

    private BoundExpression BindArrayAccessExpression(ArrayAccessExpressionSyntax expression)
    {
        var variable = LookupVariable(expression.Identifier.Text, expression.Identifier.Location);
        return BindArrayAccess(
            new BoundVariableExpression(expression.Identifier.Location, variable),
            expression.Index,
            expression.Identifier.Location,
            variable.Name);
    }

    private BoundExpression BindFieldAccessExpression(FieldAccessExpressionSyntax expression)
    {
        var target = BindExpression(expression.Target);

        if (target.Type == TypeSymbol.Error)
        {
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

        return new BoundFieldAccessExpression(expression.Identifier.Location, target, field);
    }

    private BoundExpression BindAssignableTarget(ExpressionSyntax target)
    {
        return target switch
        {
            NameExpressionSyntax nameExpression => BindAssignableNameTarget(nameExpression),
            CallExpressionSyntax callExpression => BindAssignableCallTarget(callExpression),
            ArrayAccessExpressionSyntax arrayAccessExpression => BindArrayAccessExpression(arrayAccessExpression),
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

        if (expression.Arguments.Count != functionType.ParameterTypes.Count)
        {
            ReportWrongArgumentCount(callableName, functionType.ParameterTypes.Count, expression.Arguments.Count, location);
        }

        return BindDeclaredInvocation(target, expression, functionType, location);
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

            if (index < functionType.ParameterTypes.Count)
            {
                arguments.Add(BindExpression(argument, functionType.ParameterTypes[index]));
            }
            else
            {
                arguments.Add(BindExpression(argument));
            }
        }

        return new BoundCallExpression(location, target, arguments, functionType.ReturnType);
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

        var matchedDescriptor = arityMatches.FirstOrDefault(descriptor => CanBindBuiltInArguments(rawArguments, descriptor));

        return matchedDescriptor is not null
            ? BindResolvedBuiltInInvocation(expression, matchedDescriptor, location, callableName)
            : BindDeclaredBuiltInInvocation(expression, arityMatches[0], location);
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
                arguments.Add(BindExpression(argument, descriptor.Parameters[index].Type));
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

            var invalidReturnType = descriptor.ResultTypePolicy switch
            {
                BuiltinResultTypePolicy.SameAsFirstArgument => arrayType,
                BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument => arrayType.ElementType,
                _ => descriptor.DeclaredReturnType
            };
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

        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count)
        {
            arrayType
        };

        for (var index = 1; index < expression.Arguments.Count; index++)
        {
            var parameterType = ResolveArrayBuiltInParameterType(descriptor, arrayType, index);
            var boundArgument = BindExpression(expression.Arguments[index], parameterType);
            arguments.Add(boundArgument);
            parameterTypes.Add(parameterType);
        }

        var resolvedReturnType = descriptor.ResultTypePolicy switch
        {
            BuiltinResultTypePolicy.SameAsFirstArgument => arrayType,
            BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument => arrayType.ElementType,
            _ => descriptor.DeclaredReturnType
        };
        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, resolvedReturnType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedSymbol.ReturnType);
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
        var resultArrayType = ArrayTypeSymbol.Create(selectorReturnType) as ArrayTypeSymbol;

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

        return UsesArrayElementTypeForValueParameter(descriptor.Id, parameterIndex)
            ? arrayType.ElementType
            : descriptor.Parameters[parameterIndex].Type;
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

    private BoundExpression BindArrayAccess(BoundExpression target, ExpressionSyntax indexExpression, TextLocation location, string displayName)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(indexExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (target.Type is not ArrayTypeSymbol && target.Type != TypeSymbol.Error)
        {
            ReportInvalidArrayAccess(displayName, location);
            BindExpression(indexExpression);
            return new BoundErrorExpression(location);
        }

        var index = BindNumericExpression(indexExpression);
        return new BoundArrayAccessExpression(location, target, index, displayName);
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
        if (expression.Target is NameExpressionSyntax nameExpression
            && _scope.TryLookupVariable(nameExpression.IdentifierToken.Text, out var variable)
            && variable is not null
            && variable.IsArray)
        {
            if (expression.Arguments.Count != 1)
            {
                ReportWrongArgumentCount(variable.Name, 1, expression.Arguments.Count, nameExpression.IdentifierToken.Location);

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(nameExpression.IdentifierToken.Location);
            }

            return BindArrayAccess(
                new BoundVariableExpression(nameExpression.IdentifierToken.Location, variable),
                expression.Arguments[0],
                nameExpression.IdentifierToken.Location,
                variable.Name);
        }

        var target = BindExpression(expression.Target);

        if (target.Type is ArrayTypeSymbol)
        {
            var displayName = GetArrayTargetDisplayName(target);

            if (expression.Arguments.Count != 1)
            {
                ReportWrongArgumentCount(displayName, 1, expression.Arguments.Count, GetLocation(expression.Target));

                foreach (var argument in expression.Arguments)
                {
                    BindExpression(argument);
                }

                return new BoundErrorExpression(GetLocation(expression.Target));
            }

            return BindArrayAccess(target, expression.Arguments[0], GetLocation(expression.Target), displayName);
        }

        return new BoundErrorExpression(GetLocation(expression));
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
}
