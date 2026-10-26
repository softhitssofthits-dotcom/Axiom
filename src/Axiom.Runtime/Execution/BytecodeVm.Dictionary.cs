using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    // Словарь создаётся пустым: объявление задаёт типы ключа и значения,
    // а записи добавляются операциями словаря.
    private void ExecuteCreateDictionary(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var typeName = ReadDebugName(instruction.GetOperand(0));
        var dictionaryType = ResolveDictionaryType(typeName, frame.Routine.Id, instructionIndex);
        frame.EvaluationStack.PushUnchecked(
            RuntimeValue.FromDictionary(dictionaryType, new RuntimeDictionaryValue(dictionaryType)));
    }

    private void ExecuteLoadDictionaryValue(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        if (!dictionary.TryGetValue(keyValue, out var value))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.MissingDictionaryKey,
                $"Словарь '{displayName}' не содержит ключ {DescribeDictionaryKey(keyValue)}.",
                location);
            throw new RuntimeFailureException();
        }

        frame.EvaluationStack.Push(value);
    }

    private void ExecuteStoreDictionaryValue(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        dictionary.Set(keyValue, value);
    }

    private void ExecuteDictionaryAdd(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        if (!dictionary.TryAdd(keyValue, value))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.DuplicateDictionaryKey,
                $"Словарь '{displayName}' уже содержит ключ {DescribeDictionaryKey(keyValue)}. Для замены значения используйте присваивание {displayName}[ключ] = значение.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteDictionaryExists(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(dictionary.ContainsKey(keyValue)));
    }

    private void ExecuteDictionaryRemove(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(dictionary.Remove(keyValue)));
    }

    private void ExecuteDictionaryKeys(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(dictionary.CreateKeysArray());
    }

    private void ExecuteDictionaryCount(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);

        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromInteger(dictionary.Count));
    }

    // TryGetValue записывает найденное значение в переменную результата и
    // оставляет на стеке BOOLEAN. При отсутствии ключа переменная получает
    // значение по умолчанию своего типа; отсутствующая запись не добавляется.
    // Вид источника повторяет BYREF: локальная, параметр, глобальная, захват.
    private void ExecuteDictionaryTryGetValue(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var sourceKind = instruction.GetOperand(1);
        var sourceSlot = instruction.GetOperand(2);
        var keyValue = PopValue(frame, instructionIndex);
        var dictionaryValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var dictionary = GetDictionaryValue(dictionaryValue, displayName, location);
        var destination = CreateVariableSlot(frame, sourceKind, sourceSlot);

        if (dictionary.TryGetValue(keyValue, out var value))
        {
            destination.Store(value);
            frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(true));
            return;
        }

        destination.Store(RuntimeValue.Default(dictionary.Type.ValueType));
        frame.EvaluationStack.PushUnchecked(RuntimeValue.FromBoolean(false));
    }

    private RuntimeDictionaryValue GetDictionaryValue(RuntimeValue value, string displayName, TextLocation location)
    {
        if (value.Type is not DictionaryTypeSymbol)
        {
            return ReportInvalidRuntimeState<RuntimeDictionaryValue>(
                $"Значение '{displayName}' не является словарём, а имеет тип {value.Type.Name}.",
                location);
        }

        return value.AsDictionary();
    }

    /// <summary>
    /// Текст ключа для сообщения об ошибке. Строки показываются в кавычках,
    /// чтобы пустой ключ и ключ из пробелов читались однозначно.
    /// </summary>
    private static string DescribeDictionaryKey(RuntimeValue keyValue)
    {
        ArgumentNullException.ThrowIfNull(keyValue);

        return keyValue.Type == TypeSymbol.String
            ? $"\"{keyValue.AsString()}\""
            : keyValue.ToDisplayString();
    }

    private DictionaryTypeSymbol ResolveDictionaryType(string typeName, int routineId, int instructionIndex)
    {
        if (_dictionaryTypeCache.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        var visited = new HashSet<TypeSymbol>();

        foreach (var global in _module.Globals)
        {
            if (TryResolveDictionaryType(global.Type, typeName, visited) is { } globalType)
            {
                _dictionaryTypeCache[typeName] = globalType;
                return globalType;
            }
        }

        foreach (var routine in _module.Routines)
        {
            if (TryResolveDictionaryType(routine.ResultType, typeName, visited) is { } resultType)
            {
                _dictionaryTypeCache[typeName] = resultType;
                return resultType;
            }

            foreach (var variable in routine.ParameterVariables)
            {
                if (TryResolveDictionaryType(variable.Type, typeName, visited) is { } parameterType)
                {
                    _dictionaryTypeCache[typeName] = parameterType;
                    return parameterType;
                }
            }

            foreach (var variable in routine.LocalVariables)
            {
                if (TryResolveDictionaryType(variable.Type, typeName, visited) is { } localType)
                {
                    _dictionaryTypeCache[typeName] = localType;
                    return localType;
                }
            }

            foreach (var variable in routine.CapturedVariables)
            {
                if (TryResolveDictionaryType(variable.Type, typeName, visited) is { } capturedType)
                {
                    _dictionaryTypeCache[typeName] = capturedType;
                    return capturedType;
                }
            }
        }

        return ReportInvalidRuntimeState<DictionaryTypeSymbol>(
            $"Не удалось разрешить тип словаря '{typeName}' для инструкции CreateDictionary.",
            ResolveLocation(routineId, instructionIndex));
    }

    private static DictionaryTypeSymbol? TryResolveDictionaryType(TypeSymbol type, string typeName, HashSet<TypeSymbol> visited)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(visited);

        if (!visited.Add(type))
        {
            return null;
        }

        if (type is DictionaryTypeSymbol dictionaryType)
        {
            return string.Equals(dictionaryType.Name, typeName, StringComparison.OrdinalIgnoreCase)
                ? dictionaryType
                : null;
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            foreach (var field in userType.Fields)
            {
                if (TryResolveDictionaryType(field.Type, typeName, visited) is { } nestedType)
                {
                    return nestedType;
                }
            }

            return null;
        }

        return type is ArrayTypeSymbol arrayType
            ? TryResolveDictionaryType(arrayType.ElementType, typeName, visited)
            : null;
    }
}
