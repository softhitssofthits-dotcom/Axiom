using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private void LowerExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        switch (expression)
        {
            case BoundLiteralExpression literal:
                instructions.Add(LoweredInstruction.LoadLiteral(literal.Location, literal.Value, literal.Type));
                break;

            case BoundFunctionExpression function:
                instructions.Add(LoweredInstruction.LoadFunction(function.Location, function.Function));
                break;

            case BoundVariableExpression variable:
                instructions.Add(LoweredInstruction.LoadVariable(variable.Location, variable.Variable));
                break;

            case BoundArrayAccessExpression arrayAccess:
                LowerExpression(arrayAccess.Target, instructions);
                foreach (var index in arrayAccess.Indices)
                {
                    LowerArrayIndex(index, instructions);
                }

                instructions.Add(LoweredInstruction.LoadElement(
                    arrayAccess.Location,
                    arrayAccess.DisplayName,
                    arrayAccess.Indices.Count,
                    GetArrayIndexRangeMask(arrayAccess.Indices)));
                break;

            case BoundArrayLiteralExpression arrayLiteral:
                var arrayLiteralTemporary = CreateArrayLiteralTemporary(arrayLiteral);
                var arrayLiteralName = arrayLiteralTemporary.Name;

                // Массив создаётся с нижней границей 1, как и результат функций,
                // возвращающих массив. Пустой набор получает каноническую форму 1 TO 0.
                // Образец-переменная задаёт только тип и длину фиксированной строки:
                // новый объект создаётся на каждое вычисление, иначе повторное
                // выполнение того же места изменило бы прежний результат.
                instructions.Add(LoweredInstruction.LoadVariable(arrayLiteral.Location, arrayLiteralTemporary));
                instructions.Add(LoweredInstruction.NewArray(arrayLiteral.Location, arrayLiteralName, dimensionCount: 1));
                instructions.Add(LoweredInstruction.StoreVariable(arrayLiteral.Location, arrayLiteralTemporary));
                instructions.Add(LoweredInstruction.LoadVariable(arrayLiteral.Location, arrayLiteralTemporary));
                instructions.Add(LoweredInstruction.LoadLiteral(arrayLiteral.Location, 1, TypeSymbol.Integer));
                instructions.Add(LoweredInstruction.LoadLiteral(arrayLiteral.Location, arrayLiteral.Items.Count, TypeSymbol.Integer));
                instructions.Add(LoweredInstruction.RedimArray(arrayLiteral.Location, arrayLiteralName, preserve: false, dimensionCount: 1));

                for (var index = 0; index < arrayLiteral.Items.Count; index++)
                {
                    // Каждый элемент вычисляется ровно один раз и слева направо.
                    LowerExpression(arrayLiteral.Items[index], instructions);
                    instructions.Add(LoweredInstruction.LoadVariable(arrayLiteral.Location, arrayLiteralTemporary));
                    instructions.Add(LoweredInstruction.LoadLiteral(arrayLiteral.Location, index + 1, TypeSymbol.Integer));
                    instructions.Add(LoweredInstruction.StoreElement(arrayLiteral.Location, arrayLiteralName, dimensionCount: 1));
                }

                instructions.Add(LoweredInstruction.LoadVariable(arrayLiteral.Location, arrayLiteralTemporary));
                break;

            case BoundFieldAccessExpression fieldAccess:
                LowerExpression(fieldAccess.Target, instructions);
                instructions.Add(LoweredInstruction.LoadField(fieldAccess.Location, fieldAccess.Field.Name));
                break;

            case BoundRecordLiteralExpression recordLiteral:
                var recordTemporary = CreateRecordLiteralTemporary(recordLiteral);
                instructions.Add(LoweredInstruction.CreateRecord(recordLiteral.Location, recordLiteral.RecordType));
                instructions.Add(LoweredInstruction.StoreVariable(recordLiteral.Location, recordTemporary));
                foreach (var initializer in recordLiteral.Initializers)
                {
                    LowerExpression(initializer.Expression, instructions);
                    instructions.Add(LoweredInstruction.LoadVariable(recordLiteral.Location, recordTemporary));
                    instructions.Add(LoweredInstruction.StoreField(recordLiteral.Location, initializer.Field.Name));
                }
                instructions.Add(LoweredInstruction.LoadVariable(recordLiteral.Location, recordTemporary));
                break;

            case BoundTypeConstructorExpression typeConstructor:
                var constructorTemporary = CreateTypeConstructorTemporary(typeConstructor);
                instructions.Add(LoweredInstruction.CreateRecord(typeConstructor.Location, typeConstructor.RecordType));
                instructions.Add(LoweredInstruction.StoreVariable(typeConstructor.Location, constructorTemporary));

                var constructorArguments = new List<BoundExpression>(typeConstructor.Arguments.Count + 1)
                {
                    new BoundVariableExpression(typeConstructor.Location, constructorTemporary)
                };
                constructorArguments.AddRange(typeConstructor.Arguments);

                LowerCall(
                    new BoundCallExpression(
                        typeConstructor.Location,
                        new BoundFunctionExpression(typeConstructor.Location, typeConstructor.Constructor),
                        constructorArguments,
                        TypeSymbol.Void),
                    instructions);

                if (typeConstructor.RecordType.TryGetInvariant(out var invariant) && invariant is not null)
                {
                    LowerCall(
                        new BoundCallExpression(
                            typeConstructor.Location,
                            new BoundFunctionExpression(typeConstructor.Location, invariant),
                            [new BoundVariableExpression(typeConstructor.Location, constructorTemporary)],
                            TypeSymbol.Boolean),
                        instructions);
                    instructions.Add(LoweredInstruction.ValidateTypeInvariant(typeConstructor.Location, typeConstructor.RecordType.Name, typeConstructor.Constructor));
                }

                instructions.Add(LoweredInstruction.LoadVariable(typeConstructor.Location, constructorTemporary));
                break;

            case BoundUnaryExpression unary:
                if (unary.Operand is BoundLiteralExpression unaryLiteral
                    && TryFoldUnary(unary.OperatorKind, unaryLiteral.Value, unaryLiteral.Type, out var foldedUnaryValue))
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(unary.Location, foldedUnaryValue, unary.Type));
                }
                else
                {
                    LowerExpression(unary.Operand, instructions);
                    instructions.Add(LoweredInstruction.UnaryOperation(unary.Location, unary.OperatorKind));
                }

                break;

            case BoundBinaryExpression binary:
                if (binary.OperatorKind is SyntaxKind.AndAlsoKeyword or SyntaxKind.OrElseKeyword)
                {
                    // Короткое замыкание: левый операнд решает исход, правый
                    // вычисляется только когда он ещё может повлиять на результат.
                    var rightLabel = CreateLabel();
                    var endLabel = CreateLabel();
                    LowerExpression(binary.Left, instructions);
                    instructions.Add(binary.OperatorKind == SyntaxKind.AndAlsoKeyword
                        ? LoweredInstruction.JumpIfTrue(binary.Location, rightLabel)
                        : LoweredInstruction.JumpIfFalse(binary.Location, rightLabel));
                    instructions.Add(LoweredInstruction.LoadLiteral(
                        binary.Location,
                        binary.OperatorKind == SyntaxKind.AndAlsoKeyword ? (object)false : true,
                        TypeSymbol.Boolean));
                    instructions.Add(LoweredInstruction.Jump(binary.Location, endLabel));
                    instructions.Add(LoweredInstruction.LabelMarker(binary.Location, rightLabel));
                    LowerExpression(binary.Right, instructions);
                    instructions.Add(LoweredInstruction.LabelMarker(binary.Location, endLabel));
                    break;
                }

                if (binary.Left is BoundLiteralExpression leftLiteral
                    && binary.Right is BoundLiteralExpression rightLiteral
                    && TryFoldBinary(binary.OperatorKind, leftLiteral.Value, leftLiteral.Type, rightLiteral.Value, rightLiteral.Type, binary.Type, out var foldedBinaryValue))
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(binary.Location, foldedBinaryValue, binary.Type));
                }
                else
                {
                    LowerExpression(binary.Left, instructions);
                    LowerExpression(binary.Right, instructions);
                    instructions.Add(LoweredInstruction.BinaryOperation(binary.Location, binary.OperatorKind));
                }

                break;

            case BoundConversionExpression conversion:
                if (conversion.Expression.Type is TupleTypeSymbol sourceTuple && conversion.Type is TupleTypeSymbol targetTuple)
                {
                    LowerExpression(conversion.Expression, instructions);
                    LowerTupleConversion(sourceTuple, targetTuple, conversion.Location, instructions);
                }
                else if (conversion.Expression is BoundLiteralExpression convLiteral
                    && TryFoldConversion(convLiteral.Value, convLiteral.Type, conversion.Type, out var foldedConvValue))
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(conversion.Location, foldedConvValue, conversion.Type));
                }
                else
                {
                    LowerExpression(conversion.Expression, instructions);
                    instructions.Add(LoweredInstruction.Convert(conversion.Location, conversion.Type));
                }

                break;

            case BoundCallExpression call:
                LowerCall(call, instructions);
                break;

            case BoundInterpolatedStringExpression interpolated:
                LowerInterpolatedString(interpolated, instructions);
                break;

            case BoundDictionaryAccessExpression:
            case BoundDictionaryCountExpression:
            case BoundDictionaryMethodCallExpression:
            case BoundDictionaryTryGetValueExpression:
                LowerDictionaryExpression(expression, instructions);
                break;

            case BoundSetCountExpression:
            case BoundSetMethodCallExpression:
                LowerSetExpression(expression, instructions);
                break;

            default:
                ReportUnsupported(expression, "выражение");
                break;
        }
    }

    private void LowerInterpolatedString(BoundInterpolatedStringExpression interpolated, List<LoweredInstruction> instructions)
    {
        if (interpolated.Parts.Count == 0)
        {
            instructions.Add(LoweredInstruction.LoadLiteral(interpolated.Location, string.Empty, TypeSymbol.String));
            return;
        }

        LowerInterpolatedStringPart(interpolated.Parts[0], instructions);

        for (var index = 1; index < interpolated.Parts.Count; index++)
        {
            LowerInterpolatedStringPart(interpolated.Parts[index], instructions);
            instructions.Add(LoweredInstruction.BinaryOperation(interpolated.Location, SyntaxKind.AmpersandToken));
        }
    }

    private void LowerInterpolatedStringPart(BoundInterpolatedStringPart part, List<LoweredInstruction> instructions)
    {
        if (part.FormatSpecifier is not null || part.Alignment is not null)
        {
            LowerExpression(part.Expression, instructions);
            instructions.Add(LoweredInstruction.LoadLiteral(part.Location, part.Alignment ?? 0, TypeSymbol.Integer));
            instructions.Add(LoweredInstruction.LoadLiteral(part.Location, part.FormatSpecifier ?? string.Empty, TypeSymbol.String));
            instructions.Add(LoweredInstruction.FormatInterpolatedPart(part.Location));
        }
        else
        {
            LowerExpression(part.Expression, instructions);
        }
    }

    private void LowerCall(BoundCallExpression call, List<LoweredInstruction> instructions)
    {
        if (call.Target is BoundFunctionExpression function)
        {
            var byRefArgumentIndices = new List<int>();
            var byRefVariables = new List<VariableSymbol>();
            var byRefAddresses = new List<LoweredByRefAddress?>();
            var byRefCopyBackPlans = new List<ByRefCopyBackPlan>();
            var byValArgumentCount = 0;

            for (var index = 0; index < call.Arguments.Count; index++)
            {
                var argument = call.Arguments[index];
                var parameter = index < function.Function.Parameters.Count
                    ? function.Function.Parameters[index]
                    : null;

                if (parameter?.IsByRef == true && argument is BoundVariableExpression variableArgument)
                {
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(variableArgument.Variable);
                    byRefAddresses.Add(null);
                    continue;
                }

                // Значение словаря тоже адресуемо: временная копия потеряла бы
                // изменения SELF, поэтому оно идёт путём адреса ниже.
                if (argument is not (BoundVariableExpression or BoundFieldAccessExpression
                        or BoundArrayAccessExpression or BoundDictionaryAccessExpression)
                    && IsTemporaryTypeMethodSelfArgument(function.Function, parameter, index))
                {
                    var receiverTemporary = CreateInternalTemporary(argument.Type, "__type_method_self");
                    LowerExpression(argument, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(argument.Location, receiverTemporary));
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(receiverTemporary);
                    byRefAddresses.Add(null);
                    continue;
                }

                // Поле и элемент массива передаются адресом: метод видит и меняет
                // фактическое хранилище получателя, а не временную копию записи.
                if (parameter?.IsByRef == true
                    && argument is not BoundVariableExpression
                    && TryCreateByRefAddress(argument, instructions, out var byRefAddress))
                {
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(byRefAddress.BaseVariable);
                    byRefAddresses.Add(byRefAddress);
                    continue;
                }

                if (parameter?.IsByRef == true
                    && argument is not BoundVariableExpression
                    && TryLowerByRefCopyBackArgument(argument, instructions, out var copyBackPlan))
                {
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(copyBackPlan.TempVariable);
                    byRefAddresses.Add(null);
                    byRefCopyBackPlans.Add(copyBackPlan);
                    continue;
                }

                LowerExpression(argument, instructions);
                LowerByValSnapshot(argument, instructions, copyCollections: !function.Function.IsBuiltIn);
                byValArgumentCount++;
            }

            instructions.Add(byRefVariables.Count == 0
                ? LoweredInstruction.CallFunction(call.Location, function.Function)
                : LoweredInstruction.CallFunctionByRef(
                    call.Location,
                    function.Function,
                    byValArgumentCount,
                    byRefArgumentIndices,
                    byRefVariables,
                    byRefAddresses));

            foreach (var copyBackPlan in byRefCopyBackPlans)
            {
                copyBackPlan.EmitCopyBack(instructions);
            }
            return;
        }

        LowerExpression(call.Target, instructions);

        foreach (var argument in call.Arguments)
        {
            LowerExpression(argument, instructions);
            LowerByValSnapshot(argument, instructions);
        }

        instructions.Add(LoweredInstruction.CallValue(call.Location, call.Arguments.Count, call.Type));
    }

    /// <summary>
    /// Запись BYVAL фиксируется сразу после вычисления аргумента, до вычисления
    /// следующих аргументов. Иначе побочный эффект соседнего аргумента попал бы
    /// в уже вычисленное значение.
    /// </summary>
    private static void LowerByValSnapshot(BoundExpression argument, List<LoweredInstruction> instructions, bool copyCollections = true)
    {
        if (argument.Type is UserDefinedTypeSymbol)
        {
            instructions.Add(LoweredInstruction.SnapshotValue(argument.Location));
        }
        else if (copyCollections)
        {
            LowerCollectionCopy(argument, instructions);
        }
    }

    private static void LowerCollectionCopy(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        if (expression.Type is ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol)
            instructions.Add(LoweredInstruction.SnapshotValue(expression.Location));
    }

    private void LowerModernGraphicsIntegralExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        if (expression is BoundConversionExpression conversion
            && conversion.Type == TypeSymbol.Long
            && (conversion.Expression.Type == TypeSymbol.Integer || conversion.Expression.Type == TypeSymbol.Long))
        {
            LowerExpression(conversion.Expression, instructions);
            return;
        }

        LowerExpression(expression, instructions);
    }

    private static bool IsTemporaryTypeMethodSelfArgument(
        FunctionSymbol function,
        ParameterSymbol? parameter,
        int argumentIndex)
    {
        ArgumentNullException.ThrowIfNull(function);

        return argumentIndex == 0
            && parameter?.IsByRef == true
            && parameter.Type is UserDefinedTypeSymbol
            && string.Equals(parameter.Name, "SELF", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(function.Name, function.ShortName, StringComparison.Ordinal);
    }

    private bool TryCreateByRefAddress(
        BoundExpression argument,
        List<LoweredInstruction> instructions,
        out LoweredByRefAddress address)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(instructions);

        var steps = new List<LoweredByRefAddressStep>();
        if (!TryAppendByRefAddressSteps(argument, instructions, steps, out var baseVariable))
        {
            address = null!;
            return false;
        }

        address = new LoweredByRefAddress(baseVariable, steps);
        return true;
    }

    private bool TryAppendByRefAddressSteps(
        BoundExpression expression,
        List<LoweredInstruction> instructions,
        List<LoweredByRefAddressStep> steps,
        out VariableSymbol baseVariable)
    {
        switch (expression)
        {
            case BoundVariableExpression variableExpression:
                baseVariable = variableExpression.Variable;
                return true;

            case BoundFieldAccessExpression fieldAccess:
                if (!TryAppendByRefAddressSteps(fieldAccess.Target, instructions, steps, out baseVariable))
                {
                    return false;
                }

                steps.Add(LoweredByRefAddressStep.Field(fieldAccess.Field.Name));
                return true;

            case BoundArrayAccessExpression arrayAccess:
            {
                var resolvedBaseVariable = default(VariableSymbol);
                var stepsBeforeTarget = steps.Count;

                if (arrayAccess.Target is BoundVariableExpression or BoundFieldAccessExpression
                    && TryAppendByRefAddressSteps(arrayAccess.Target, instructions, steps, out resolvedBaseVariable))
                {
                }
                else
                {
                    // Получатель-выражение вычисляется ровно один раз. Массив хранит
                    // общее хранилище, поэтому временная переменная с его значением
                    // остаётся живым адресом фактического элемента.
                    steps.RemoveRange(stepsBeforeTarget, steps.Count - stepsBeforeTarget);
                    resolvedBaseVariable = CreateInternalTemporary(arrayAccess.Target.Type, "__byref_receiver");
                    LowerExpression(arrayAccess.Target, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(arrayAccess.Target.Location, resolvedBaseVariable));
                }

                baseVariable = resolvedBaseVariable;

                // Индексы вычисляются один раз при вызове и сохраняются в локальных
                // временных переменных: адрес не повторяет вычисление при каждом чтении.
                var indexTemporaries = new VariableSymbol[arrayAccess.Indices.Count];

                for (var index = 0; index < arrayAccess.Indices.Count; index++)
                {
                    var indexExpression = arrayAccess.Indices[index];
                    var indexTemporary = CreateInternalTemporary(indexExpression.Type, $"__byref_index_{index}");
                    indexTemporaries[index] = indexTemporary;
                    LowerExpression(indexExpression, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(indexExpression.Location, indexTemporary));
                }

                steps.Add(LoweredByRefAddressStep.Element(indexTemporaries));
                return true;
            }

            case BoundDictionaryAccessExpression dictionaryAccess:
            {
                var stepsBeforeDictionary = steps.Count;
                var resolvedDictionaryVariable = default(VariableSymbol);

                if (dictionaryAccess.Target is BoundVariableExpression or BoundFieldAccessExpression
                    && TryAppendByRefAddressSteps(dictionaryAccess.Target, instructions, steps, out resolvedDictionaryVariable))
                {
                }
                else
                {
                    // Словарь, полученный выражением, вычисляется один раз:
                    // временная переменная хранит ссылку на тот же экземпляр,
                    // поэтому адрес остаётся живым.
                    steps.RemoveRange(stepsBeforeDictionary, steps.Count - stepsBeforeDictionary);
                    resolvedDictionaryVariable = CreateInternalTemporary(dictionaryAccess.Target.Type, "__byref_dictionary");
                    LowerExpression(dictionaryAccess.Target, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(dictionaryAccess.Target.Location, resolvedDictionaryVariable));
                }

                baseVariable = resolvedDictionaryVariable;

                // Ключ вычисляется один раз при вызове и сохраняется локально.
                var keyTemporary = CreateInternalTemporary(dictionaryAccess.Key.Type, "__byref_key");
                LowerExpression(dictionaryAccess.Key, instructions);
                instructions.Add(LoweredInstruction.StoreVariable(dictionaryAccess.Key.Location, keyTemporary));

                steps.Add(LoweredByRefAddressStep.DictionaryValue(keyTemporary));
                return true;
            }

            default:
                baseVariable = null!;
                return false;
        }
    }

    private bool TryLowerByRefCopyBackArgument(
        BoundExpression argument,
        List<LoweredInstruction> instructions,
        out ByRefCopyBackPlan plan)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(instructions);

        if (!TryCreateByRefStoragePlan(argument, instructions, out var storagePlan))
        {
            plan = null!;
            return false;
        }

        var receiverTemporary = CreateInternalTemporary(argument.Type, "__byref_value");
        storagePlan.EmitLoadValue(instructions);
        instructions.Add(LoweredInstruction.StoreVariable(argument.Location, receiverTemporary));

        plan = new ByRefCopyBackPlan(
            receiverTemporary,
            copyBackInstructions =>
            {
                copyBackInstructions.Add(LoweredInstruction.LoadVariable(argument.Location, receiverTemporary));
                storagePlan.EmitStoreValue(copyBackInstructions);
            });
        return true;
    }

    private bool TryCreateByRefStoragePlan(
        BoundExpression argument,
        List<LoweredInstruction> instructions,
        out ByRefStoragePlan plan)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(instructions);

        switch (argument)
        {
            case BoundVariableExpression variableExpression:
                plan = new VariableByRefStoragePlan(variableExpression.Variable, variableExpression.Location);
                return true;

            case BoundFieldAccessExpression fieldAccess when TryCreateByRefStoragePlan(fieldAccess.Target, instructions, out var parentPlan):
                plan = new FieldByRefStoragePlan(parentPlan, fieldAccess.Field.Name, fieldAccess.Location);
                return true;

            case BoundArrayAccessExpression arrayAccess:
            {
                var arrayTemporary = CreateInternalTemporary(arrayAccess.Target.Type, "__byref_array");
                var indexTemporaries = new VariableSymbol[arrayAccess.Indices.Count];

                LowerExpression(arrayAccess.Target, instructions);
                instructions.Add(LoweredInstruction.StoreVariable(arrayAccess.Location, arrayTemporary));

                for (var index = 0; index < arrayAccess.Indices.Count; index++)
                {
                    var indexExpression = arrayAccess.Indices[index];
                    var indexTemporary = CreateInternalTemporary(indexExpression.Type, $"__byref_index_{index}");
                    indexTemporaries[index] = indexTemporary;
                    LowerExpression(indexExpression, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(indexExpression.Location, indexTemporary));
                }

                plan = new ArrayElementByRefStoragePlan(
                    arrayTemporary,
                    indexTemporaries,
                    arrayAccess.DisplayName,
                    arrayAccess.Location);
                return true;
            }

            default:
                plan = null!;
                return false;
        }
    }

    private void LowerArrayIndex(BoundExpression index, List<LoweredInstruction> instructions)
    {
        if (index is BoundArrayRangeExpression range)
        {
            LowerExpression(range.LowerBound, instructions);
            LowerExpression(range.UpperBound, instructions);
            return;
        }

        LowerExpression(index, instructions);
    }

    private static int GetArrayIndexRangeMask(IReadOnlyList<BoundExpression> indices)
    {
        var mask = 0;

        for (var index = 0; index < indices.Count; index++)
        {
            if (indices[index] is BoundArrayRangeExpression)
            {
                mask |= 1 << index;
            }
        }

        return mask;
    }

}
