using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    // Множество создаётся пустым: объявление задаёт тип элемента, а элементы
    // добавляются операциями множества.
    private void ExecuteCreateSet(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var typeName = ReadDebugName(instruction.GetOperand(0));
        var setType = ResolveSetType(typeName, frame.Routine.Id, instructionIndex);
        frame.EvaluationStack.PushUnchecked(
            RuntimeValue.FromSet(setType, new RuntimeSetValue(setType)));
    }

    private void ExecuteSetAdd(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var element = PopValue(frame, instructionIndex);
        var setValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var set = GetSetValue(setValue, displayName, location);

        // Повторный элемент не создаёт второй записи и не является ошибкой.
        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(set.TryAdd(element)));
    }

    private void ExecuteSetContains(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var element = PopValue(frame, instructionIndex);
        var setValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var set = GetSetValue(setValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(set.Contains(element)));
    }

    private void ExecuteSetRemove(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var element = PopValue(frame, instructionIndex);
        var setValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var set = GetSetValue(setValue, displayName, location);

        // Отсутствующий элемент не является ошибкой: результат FALSE.
        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(set.Remove(element)));
    }

    private void ExecuteSetCount(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var setValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var set = GetSetValue(setValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromInteger(set.Count));
    }

    private void ExecuteSetToArray(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var setValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var set = GetSetValue(setValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(set.CreateElementsArray(_host));
    }

    // Объединение изменяет получателя и значения не возвращает. Проверка
    // вместимости выполняется до изменения, поэтому логическое превышение
    // предела не оставляет получателя частично изменённым.
    private void ExecuteSetUnionWith(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var argument = PopValue(frame, instructionIndex);
        var receiverValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var receiver = GetSetValue(receiverValue, displayName, location);
        var argumentSet = GetSetValue(argument, displayName, location);

        receiver.UnionWith(argumentSet, _host);
    }

    private void ExecuteSetIntersectWith(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var argument = PopValue(frame, instructionIndex);
        var receiverValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var receiver = GetSetValue(receiverValue, displayName, location);
        var argumentSet = GetSetValue(argument, displayName, location);

        receiver.IntersectWith(argumentSet, _host);
    }

    private RuntimeSetValue GetSetValue(RuntimeValue value, string displayName, TextLocation location)
    {
        if (value.Type is not SetTypeSymbol)
        {
            return ReportInvalidRuntimeState<RuntimeSetValue>(
                $"Значение '{displayName}' не является множеством, а имеет тип {value.Type.Name}.",
                location);
        }

        return value.AsSet();
    }

    /// <summary>
    /// Тип множества разрешается по метаданным модуля тем же путём, что и тип
    /// словаря: глобальные, результаты, параметры, локальные, захваченные
    /// переменные, поля TYPE и элементы массивов.
    /// </summary>
    private SetTypeSymbol ResolveSetType(string typeName, int routineId, int instructionIndex)
    {
        if (_prepared.SetTypesByName.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        return ReportInvalidRuntimeState<SetTypeSymbol>(
            $"Не удалось разрешить тип множества '{typeName}' для инструкции CreateSet.",
            ResolveLocation(routineId, instructionIndex));
    }
}
