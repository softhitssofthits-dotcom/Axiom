using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteCreateRecord(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var typeIdentity = ReadDebugName(instruction.GetOperand(0));
        var recordType = ResolveRecordType(typeIdentity, frame.Routine.Id, instructionIndex);
        frame.EvaluationStack.Push(RuntimeValue.FromRecord(recordType, RuntimeRecordValue.CreateDefault(recordType)));
    }

    private void ExecuteLoadField(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var recordValue = PopValue(frame, instructionIndex);
        if (recordValue.Type == TypeSymbol.RuntimeError)
        {
            var errorField = ResolveRuntimeErrorField(frame.Routine.Id, instructionIndex, ReadDebugName(instruction.GetOperand(0)));
            frame.EvaluationStack.Push(recordValue.AsRuntimeError().GetField(errorField));
            return;
        }

        var field = ResolveInstructionField(frame.Routine.Id, instructionIndex, recordValue, instruction);

        frame.EvaluationStack.Push(recordValue.AsRecord().GetField(field));
    }

    private void ExecuteStoreField(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var recordValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var field = ResolveInstructionField(frame.Routine.Id, instructionIndex, recordValue, instruction);

        recordValue.AsRecord().SetField(field, value);
    }

    private TypeFieldSymbol ResolveInstructionField(int routineId, int instructionIndex, RuntimeValue value, VmInstruction instruction)
    {
        if (instruction.Opcode is not (Opcode.LoadFieldOrdinal or Opcode.StoreFieldOrdinal))
        {
            return ResolveRecordField(routineId, instructionIndex, value, ReadDebugName(instruction.GetOperand(0)));
        }

        if (value.Type is not UserDefinedTypeSymbol recordType)
        {
            return ReportInvalidRuntimeState<TypeFieldSymbol>(
                $"Значение типа '{value.Type.Name}' не является TYPE для доступа к полю '{ReadDebugName(instruction.GetOperand(0))}'.",
                ResolveLocation(routineId, instructionIndex));
        }

        var ordinal = instruction.GetOperand(1);
        if ((uint)ordinal >= (uint)recordType.Fields.Count)
        {
            return ReportInvalidRuntimeState<TypeFieldSymbol>(
                $"TYPE '{recordType.Name}' не содержит поле с номером {ordinal}.",
                ResolveLocation(routineId, instructionIndex));
        }

        return recordType.Fields[ordinal];
    }

    private TypeFieldSymbol ResolveRecordField(int routineId, int instructionIndex, RuntimeValue recordValue, string fieldName)
    {
        if (recordValue.Type is not UserDefinedTypeSymbol recordType)
        {
            return ReportInvalidRuntimeState<TypeFieldSymbol>(
                $"Значение типа '{recordValue.Type.Name}' не является TYPE для доступа к полю '{fieldName}'.",
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

    private UserDefinedTypeSymbol ResolveRecordType(string typeIdentity, int routineId, int instructionIndex)
    {
        if (_recordTypeCache.TryGetValue(typeIdentity, out var cachedType))
        {
            return cachedType;
        }

        var visited = new HashSet<TypeSymbol>(ReferenceEqualityComparer.Instance);

        foreach (var global in _module.Globals)
        {
            if (TryResolveRecordType(global.Type, typeIdentity, visited) is { } globalType)
            {
                _recordTypeCache[typeIdentity] = globalType;
                return globalType;
            }
        }

        foreach (var routine in _module.Routines)
        {
            if (TryResolveRecordType(routine.ResultType, typeIdentity, visited) is { } resultType)
            {
                _recordTypeCache[typeIdentity] = resultType;
                return resultType;
            }

            foreach (var variable in routine.ParameterVariables)
            {
                if (TryResolveRecordType(variable.Type, typeIdentity, visited) is { } parameterType)
                {
                    _recordTypeCache[typeIdentity] = parameterType;
                    return parameterType;
                }
            }

            foreach (var variable in routine.LocalVariables)
            {
                if (TryResolveRecordType(variable.Type, typeIdentity, visited) is { } localType)
                {
                    _recordTypeCache[typeIdentity] = localType;
                    return localType;
                }
            }

            foreach (var variable in routine.CapturedVariables)
            {
                if (TryResolveRecordType(variable.Type, typeIdentity, visited) is { } capturedType)
                {
                    _recordTypeCache[typeIdentity] = capturedType;
                    return capturedType;
                }
            }
        }

        return ReportInvalidRuntimeState<UserDefinedTypeSymbol>(
            $"Не удалось разрешить TYPE '{typeIdentity}' для инструкции CreateRecord.",
            ResolveLocation(routineId, instructionIndex));
    }

    private static UserDefinedTypeSymbol? TryResolveRecordType(TypeSymbol type, string typeIdentity, HashSet<TypeSymbol> visited)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeIdentity);
        ArgumentNullException.ThrowIfNull(visited);

        if (!visited.Add(type))
        {
            return null;
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            if (MatchesTypeIdentity(userType, typeIdentity))
            {
                return userType;
            }

            foreach (var field in userType.Fields)
            {
                if (TryResolveRecordType(field.Type, typeIdentity, visited) is { } nestedType)
                {
                    return nestedType;
                }
            }

            return null;
        }

        return type is ArrayTypeSymbol arrayType
            ? TryResolveRecordType(arrayType.ElementType, typeIdentity, visited)
            : null;
    }

    /// <summary>
    /// Сравнивается полная идентичность. У типов из старых снимков без модуля
    /// она совпадает с коротким именем; квалифицированные типы им не подменяются.
    /// </summary>
    private static bool MatchesTypeIdentity(UserDefinedTypeSymbol type, string typeIdentity)
    {
        return TypeIdentityFormat.Equals(type.TypeIdentity, typeIdentity);
    }

    private void BuildRecordTypeCache(BytecodeModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var visited = new HashSet<TypeSymbol>(ReferenceEqualityComparer.Instance);

        foreach (var global in module.Globals)
        {
            CollectRecordTypes(global.Type, visited);
        }

        foreach (var routine in module.Routines)
        {
            CollectRecordTypes(routine.ResultType, visited);

            foreach (var variable in routine.ParameterVariables)
            {
                CollectRecordTypes(variable.Type, visited);
            }

            foreach (var variable in routine.LocalVariables)
            {
                CollectRecordTypes(variable.Type, visited);
            }

            foreach (var variable in routine.CapturedVariables)
            {
                CollectRecordTypes(variable.Type, visited);
            }
        }
    }

    private void CollectRecordTypes(TypeSymbol type, HashSet<TypeSymbol> visited)
    {
        if (type is null || !visited.Add(type))
        {
            return;
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            _recordTypeCache.TryAdd(userType.TypeIdentity, userType);

            foreach (var field in userType.Fields)
            {
                CollectRecordTypes(field.Type, visited);
            }

            return;
        }

        if (type is ArrayTypeSymbol arrayType)
        {
            CollectRecordTypes(arrayType.ElementType, visited);
        }
    }
}
