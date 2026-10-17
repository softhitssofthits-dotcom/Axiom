using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private BytecodeVariableSlot[] ResolveByRefArgumentSlots(BytecodeCallFrame callerFrame, BytecodeInstruction instruction)
    {
        var slots = new BytecodeVariableSlot[instruction.GetOperand(1)];
        var byRefCount = instruction.GetOperand(3);
        var operandIndex = 4;

        for (var index = 0; index < byRefCount; index++)
        {
            var argumentIndex = instruction.GetOperand(operandIndex++);
            var sourceKind = instruction.GetOperand(operandIndex++);

            slots[argumentIndex] = sourceKind == ByRefArgumentSourceKind.Address
                ? CreateAddressSlot(callerFrame, instruction, ref operandIndex)
                : CreateVariableSlot(callerFrame, sourceKind, instruction.GetOperand(operandIndex++));
        }

        return slots;
    }

    private BytecodeVariableSlot CreateVariableSlot(BytecodeCallFrame callerFrame, int sourceKind, int sourceSlot)
    {
        return sourceKind switch
        {
            ByRefArgumentSourceKind.Global => new BytecodeVariableSlot(
                () => LoadGlobalUnchecked(sourceSlot),
                value => StoreGlobalUnchecked(sourceSlot, value),
                GetGlobalFixedStringLength(sourceSlot)),
            ByRefArgumentSourceKind.Argument => callerFrame.Arguments[sourceSlot],
            ByRefArgumentSourceKind.Local => callerFrame.GetOrCreateLocalSlot(sourceSlot),
            ByRefArgumentSourceKind.Capture => callerFrame.Captures[sourceSlot],
            _ => throw new InvalidOperationException($"Unsupported BYREF source kind '{sourceKind}'.")
        };
    }

    /// <summary>
    /// Создаёт слот, который адресует поле или элемент массива получателя.
    /// Индексы уже вычислены вызывающей стороной и хранятся в её слотах,
    /// поэтому чтение и запись идут в фактическое хранилище без копии записи.
    /// </summary>
    private BytecodeVariableSlot CreateAddressSlot(
        BytecodeCallFrame callerFrame,
        BytecodeInstruction instruction,
        ref int operandIndex)
    {
        var baseKind = instruction.GetOperand(operandIndex++);
        var baseSlot = instruction.GetOperand(operandIndex++);
        var stepCount = instruction.GetOperand(operandIndex++);

        var steps = new AddressStep[stepCount];

        for (var stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            var stepKind = instruction.GetOperand(operandIndex++);

            if (stepKind == 0)
            {
                steps[stepIndex] = new AddressStep(instruction.GetOperand(operandIndex++), Array.Empty<BytecodeVariableSlot>());
                continue;
            }

            var indexCount = instruction.GetOperand(operandIndex++);
            var indexSlots = new BytecodeVariableSlot[indexCount];

            for (var indexIndex = 0; indexIndex < indexCount; indexIndex++)
            {
                var indexKind = instruction.GetOperand(operandIndex++);
                var indexSlot = instruction.GetOperand(operandIndex++);
                indexSlots[indexIndex] = CreateVariableSlot(callerFrame, indexKind, indexSlot);
            }

            steps[stepIndex] = new AddressStep(-1, indexSlots);
        }

        var baseVariableSlot = CreateVariableSlot(callerFrame, baseKind, baseSlot);

        return new BytecodeVariableSlot(
            () => ReadAddressedValue(baseVariableSlot, steps),
            value => WriteAddressedValue(baseVariableSlot, steps, value));
    }

    private RuntimeValue ReadAddressedValue(BytecodeVariableSlot baseSlot, AddressStep[] steps)
    {
        var current = baseSlot.Value;

        foreach (var step in steps)
        {
            current = ReadAddressStep(current, step);
        }

        return current;
    }

    private void WriteAddressedValue(BytecodeVariableSlot baseSlot, AddressStep[] steps, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (steps.Length == 1)
        {
            WriteAddressStep(baseSlot.Value, steps[0], value);
            return;
        }

        var current = baseSlot.Value;

        for (var index = 0; index < steps.Length - 1; index++)
        {
            current = ReadAddressStep(current, steps[index]);
        }

        WriteAddressStep(current, steps[^1], value);
    }

    private RuntimeValue ReadAddressStep(RuntimeValue container, AddressStep step)
    {
        if (step.FieldNameConstantIndex >= 0)
        {
            var fieldName = ReadDebugName(step.FieldNameConstantIndex);
            var record = container.AsRecord();
            return record.GetField(ResolveFieldByName(record.Type, fieldName, FallbackRuntimeLocation));
        }

        var array = container.AsArray();
        var indices = new int[step.IndexSlots.Length];

        for (var index = 0; index < indices.Length; index++)
        {
            indices[index] = ToArrayIndex(step.IndexSlots[index].Value, FallbackRuntimeLocation);
        }

        return array.GetValue(indices);
    }

    private void WriteAddressStep(RuntimeValue container, AddressStep step, RuntimeValue value)
    {
        if (step.FieldNameConstantIndex >= 0)
        {
            var fieldName = ReadDebugName(step.FieldNameConstantIndex);
            var record = container.AsRecord();
            record.SetField(ResolveFieldByName(record.Type, fieldName, FallbackRuntimeLocation), value);
            return;
        }

        var array = container.AsArray();
        var indices = new int[step.IndexSlots.Length];

        for (var index = 0; index < indices.Length; index++)
        {
            indices[index] = ToArrayIndex(step.IndexSlots[index].Value, FallbackRuntimeLocation);
        }

        array.SetValue(indices, value);
    }

    private TypeFieldSymbol ResolveFieldByName(UserDefinedTypeSymbol recordType, string fieldName, TextLocation location)
    {
        if (recordType.TryGetField(fieldName, out var field) && field is not null)
        {
            return field;
        }

        return ReportInvalidRuntimeState<TypeFieldSymbol>(
            $"TYPE '{recordType.Name}' не содержит поле '{fieldName}'.",
            location);
    }

    private int ToArrayIndex(RuntimeValue value, TextLocation location)
    {
        if (value.Type == TypeSymbol.Integer)
        {
            return value.AsInteger();
        }

        if (value.Type == TypeSymbol.Long)
        {
            return (int)value.AsLong();
        }

        return ReportInvalidRuntimeState<int>(
            $"Индекс адресуемого элемента массива должен быть целым числом, получен '{value.Type.Name}'.",
            location);
    }

    private readonly record struct AddressStep(int FieldNameConstantIndex, BytecodeVariableSlot[] IndexSlots);
}
