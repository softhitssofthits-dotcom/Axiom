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
        if (_recordFieldCache is not null && _recordFieldCache.TryGetValue(cacheKey, out var cachedField))
        {
            return cachedField;
        }

        if (recordType.TryGetField(fieldName, out var field) && field is not null)
        {
            (_recordFieldCache ??= [])[cacheKey] = field;
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
        if (_prepared.RecordTypesByIdentity.TryGetValue(typeIdentity, out var cachedType))
        {
            return cachedType;
        }

        return ReportInvalidRuntimeState<UserDefinedTypeSymbol>(
            $"Не удалось разрешить TYPE '{typeIdentity}' для инструкции CreateRecord.",
            ResolveLocation(routineId, instructionIndex));
    }
}
