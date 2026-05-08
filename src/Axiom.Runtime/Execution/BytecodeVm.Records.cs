using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteCreateRecord(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var typeName = ReadDebugName(instruction.GetOperand(0));
        var recordType = ResolveRecordType(typeName, frame.Routine.Id, instructionIndex);
        frame.EvaluationStack.Push(RuntimeValue.FromRecord(recordType, RuntimeRecordValue.CreateDefault(recordType)));
    }

    private void ExecuteLoadField(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var recordValue = PopValue(frame, instructionIndex);
        if (recordValue.Type == TypeSymbol.RuntimeError)
        {
            var errorField = ResolveRuntimeErrorField(frame.Routine.Id, instructionIndex, ReadDebugName(instruction.GetOperand(0)));
            frame.EvaluationStack.Push(recordValue.AsRuntimeError().GetField(errorField));
            return;
        }

        var field = ResolveRecordField(frame.Routine.Id, instructionIndex, recordValue, ReadDebugName(instruction.GetOperand(0)));

        frame.EvaluationStack.Push(recordValue.AsRecord().GetField(field));
    }

    private void ExecuteStoreField(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var recordValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var field = ResolveRecordField(frame.Routine.Id, instructionIndex, recordValue, ReadDebugName(instruction.GetOperand(0)));

        recordValue.AsRecord().SetField(field, value);
    }

    private TypeFieldSymbol ResolveRecordField(int routineId, int instructionIndex, RuntimeValue recordValue, string fieldName)
    {
        if (recordValue.Type is not UserDefinedTypeSymbol recordType)
        {
            return ReportInvalidRuntimeState<TypeFieldSymbol>(
                $"Значение типа '{recordValue.Type.Name}' не содержит TYPE для доступа к полю '{fieldName}'.",
                ResolveLocation(routineId, instructionIndex));
        }

        var cacheKey = (routineId, instructionIndex);
        if (_recordFieldCache.TryGetValue(cacheKey, out var cachedField))
        {
            return cachedField;
        }

        if (recordType.TryGetField(fieldName, out var field) && field is not null)
        {
            _recordFieldCache[cacheKey] = field;
            return field;
        }

        return ReportInvalidRuntimeState<TypeFieldSymbol>(
            $"TYPE '{recordType.Name}' не содержит поле '{fieldName}'.",
            ResolveLocation(routineId, instructionIndex));
    }
    private TypeFieldSymbol ResolveRuntimeErrorField(int routineId, int instructionIndex, string fieldName)
    {
        if (TypeSymbol.RuntimeError.TryGetField(fieldName, out var field) && field is not null)
        {
            return field;
        }

        return ReportInvalidRuntimeState<TypeFieldSymbol>(
            $"ERROR не содержит поле '{fieldName}'.",
            ResolveLocation(routineId, instructionIndex));
    }

    private UserDefinedTypeSymbol ResolveRecordType(string typeName, int routineId, int instructionIndex)
    {
        if (_recordTypeCache.TryGetValue(typeName, out var cachedType))
        {
            return cachedType;
        }

        var visited = new HashSet<TypeSymbol>();

        foreach (var global in _module.Globals)
        {
            if (TryResolveRecordType(global.Type, typeName, visited) is { } globalType)
            {
                _recordTypeCache[typeName] = globalType;
                return globalType;
            }
        }

        foreach (var routine in _module.Routines)
        {
            if (TryResolveRecordType(routine.ResultType, typeName, visited) is { } resultType)
            {
                _recordTypeCache[typeName] = resultType;
                return resultType;
            }

            foreach (var variable in routine.ParameterVariables)
            {
                if (TryResolveRecordType(variable.Type, typeName, visited) is { } parameterType)
                {
                    _recordTypeCache[typeName] = parameterType;
                    return parameterType;
                }
            }

            foreach (var variable in routine.LocalVariables)
            {
                if (TryResolveRecordType(variable.Type, typeName, visited) is { } localType)
                {
                    _recordTypeCache[typeName] = localType;
                    return localType;
                }
            }

            foreach (var variable in routine.CapturedVariables)
            {
                if (TryResolveRecordType(variable.Type, typeName, visited) is { } capturedType)
                {
                    _recordTypeCache[typeName] = capturedType;
                    return capturedType;
                }
            }
        }

        return ReportInvalidRuntimeState<UserDefinedTypeSymbol>(
            $"Не удалось разрешить TYPE '{typeName}' для инструкции CreateRecord.",
            ResolveLocation(routineId, instructionIndex));
    }

    private static UserDefinedTypeSymbol? TryResolveRecordType(TypeSymbol type, string typeName, HashSet<TypeSymbol> visited)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(visited);

        if (!visited.Add(type))
        {
            return null;
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            if (string.Equals(userType.Name, typeName, StringComparison.OrdinalIgnoreCase))
            {
                return userType;
            }

            foreach (var field in userType.Fields)
            {
                if (TryResolveRecordType(field.Type, typeName, visited) is { } nestedType)
                {
                    return nestedType;
                }
            }

            return null;
        }

        return type is ArrayTypeSymbol arrayType
            ? TryResolveRecordType(arrayType.ElementType, typeName, visited)
            : null;
    }
}
