using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
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
            // Массивные формы выводят тип результата из первого аргумента, поэтому
            // объявленный тип дескриптора здесь не подходит: нужен общий путь
            // BindArrayBuiltInCall, который знает про элементы массива.
            if (matchedDescriptor.BindingKind == BuiltinBindingKind.Array)
            {
                return BindResolvedBuiltInInvocation(expression, matchedDescriptor, location, callableName);
            }

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
        var selectedDescriptor = matchedDescriptor ?? arityMatches[0];

        // Массивные формы выводят типы из фактического массива: объявленный
        // параметр INTEGER[] — только образец. Поэтому они всегда идут через
        // BindArrayBuiltInCall, а не через путь объявленных типов.
        if (selectedDescriptor.BindingKind == BuiltinBindingKind.Array)
        {
            return BindResolvedBuiltInInvocation(expression, selectedDescriptor, location, callableName);
        }

        return BindResolvedBuiltInInvocationFromRawArguments(
            expression,
            selectedDescriptor,
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

                // Пустой литерал массива не выводит тип элементов из себя, поэтому
                // получает объявленный тип параметра сразу при связывании.
                var boundArgument = argument is ArrayLiteralExpressionSyntax { IsEmpty: true }
                    ? BindExpression(argument, parameterType)
                    : BindExpression(argument);

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

        // Сортировка по полю записи: второй аргумент — имя поля, третий —
        // необязательное направление STEP 1 / STEP -1. Форма проверяется
        // здесь, до общей проверки упорядочиваемости элементов, потому что
        // записи упорядочиваются не целиком, а по выбранному полю.
        if (IsSortByFieldInvocation(descriptor.Id, descriptor))
        {
            return BindArraySortByFieldCall(expression, descriptor, location, arrayType, firstArgument);
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

    /// <summary>
    /// Форма «сортировать по полю» отличается от формы с номером измерения
    /// типом второго параметра: имя поля — строка. Выбор между двумя формами
    /// уже выполнен по типам аргументов, поэтому достаточно проверить дескриптор.
    /// </summary>
    private static bool IsSortByFieldInvocation(BuiltinFunctionId id, BuiltinFunctionDescriptor descriptor)
    {
        return id is (BuiltinFunctionId.ArraySort or BuiltinFunctionId.ArrayToSorted)
            && descriptor.Parameters.Count >= 2
            && descriptor.Parameters[1].Type == TypeSymbol.String;
    }

    private BoundExpression BindArraySortByFieldCall(
        CallExpressionSyntax expression,
        BuiltinFunctionDescriptor descriptor,
        TextLocation location,
        ArrayTypeSymbol arrayType,
        BoundExpression firstArgument)
    {
        var arguments = new List<BoundExpression>(expression.Arguments.Count) { firstArgument };
        var parameterTypes = new List<TypeSymbol>(descriptor.Parameters.Count) { arrayType };
        var failed = false;

        var recordType = arrayType.ElementType as UserDefinedTypeSymbol;

        if (recordType is null)
        {
            ReportSortFieldRequiresRecordArray(descriptor.Name, arrayType.ElementType, GetLocation(expression.Arguments[0]));
            failed = true;
        }
        else if (arrayType.Rank != 1)
        {
            ReportSortFieldRequiresOneDimensional(descriptor.Name, arrayType.Rank, GetLocation(expression.Arguments[0]));
            failed = true;
        }

        // Имя поля должно быть известно при проверке программы: иначе опечатка
        // обнаружится только после запуска, когда массив уже заполнен.
        var fieldName = expression.Arguments[1] is LiteralExpressionSyntax { LiteralToken.Value: string text }
            ? text
            : null;
        arguments.Add(BindExpression(expression.Arguments[1]));
        parameterTypes.Add(TypeSymbol.String);

        if (fieldName is null)
        {
            ReportSortFieldRequiresConstantName(descriptor.Name, GetLocation(expression.Arguments[1]));
            failed = true;
        }
        else if (!failed && recordType is not null)
        {
            if (!recordType.TryGetField(fieldName, out var field) || field is null)
            {
                ReportUnknownSortField(descriptor.Name, recordType.Name, fieldName, GetLocation(expression.Arguments[1]));
                failed = true;
            }
            else if (field.IsArray || !SupportsOrderedComparison(field.Type))
            {
                ReportSortFieldNotOrderable(descriptor.Name, fieldName, field.Type, GetLocation(expression.Arguments[1]));
                failed = true;
            }
        }

        if (expression.Arguments.Count == 3)
        {
            if (expression.Arguments[2] is StepDirectionArgumentExpressionSyntax stepDirection
                && TryResolveSortDirection(stepDirection, out var direction))
            {
                arguments.Add(new BoundLiteralExpression(stepDirection.StepKeyword.Location, direction, TypeSymbol.Integer));
                parameterTypes.Add(TypeSymbol.Integer);
            }
            else
            {
                var directionLocation = expression.Arguments[2] is StepDirectionArgumentExpressionSyntax stepSyntax
                    ? stepSyntax.StepKeyword.Location
                    : GetLocation(expression.Arguments[2]);

                ReportInvalidSortDirection(directionLocation);
                arguments.Add(new BoundErrorExpression(directionLocation));
                parameterTypes.Add(TypeSymbol.Integer);
                failed = true;
            }
        }

        var resolvedReturnType = ResolveArrayBuiltInReturnType(descriptor, arrayType);
        var resolvedSymbol = descriptor.CreateResolvedSymbol(parameterTypes, resolvedReturnType);
        var resolvedTarget = new BoundFunctionExpression(location, resolvedSymbol);
        return new BoundCallExpression(location, resolvedTarget, arguments, resolvedReturnType);
    }

    /// <summary>
    /// Направление сортировки — только константа 1 или -1: шага у сортировки
    /// нет, есть знак. Всё остальное отклоняется при проверке программы.
    /// </summary>
    private static bool TryResolveSortDirection(
        StepDirectionArgumentExpressionSyntax stepDirection,
        out int direction)
    {
        direction = 1;

        var value = stepDirection.Value switch
        {
            LiteralExpressionSyntax { LiteralToken.Value: int integer } => (int?)integer,
            UnaryExpressionSyntax
            {
                OperatorToken.Kind: SyntaxKind.MinusToken,
                Operand: LiteralExpressionSyntax { LiteralToken.Value: int integer }
            } => -integer,
            _ => null
        };

        if (value is not (1 or -1))
        {
            return false;
        }

        direction = value.Value;
        return true;
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

}
