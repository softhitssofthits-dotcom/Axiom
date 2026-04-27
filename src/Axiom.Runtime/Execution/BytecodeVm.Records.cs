using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteLoadField(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var recordValue = PopValue(frame, instructionIndex);
        var field = ResolveRecordField(recordValue, ReadDebugName(instruction.GetOperand(0)), location);

        frame.EvaluationStack.Push(recordValue.AsRecord().GetField(field));
    }

    private void ExecuteStoreField(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var recordValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var field = ResolveRecordField(recordValue, ReadDebugName(instruction.GetOperand(0)), location);

        recordValue.AsRecord().SetField(field, value);
    }

    private TypeFieldSymbol ResolveRecordField(RuntimeValue recordValue, string fieldName, TextLocation location)
    {
        if (recordValue.Type is not UserDefinedTypeSymbol recordType)
        {
            return ReportInvalidRuntimeState<TypeFieldSymbol>(
                $"Значение типа '{recordValue.Type.Name}' не содержит TYPE для доступа к полю '{fieldName}'.",
                location);
        }

        if (recordType.TryGetField(fieldName, out var field) && field is not null)
        {
            return field;
        }

        return ReportInvalidRuntimeState<TypeFieldSymbol>(
            $"TYPE '{recordType.Name}' не содержит поле '{fieldName}'.",
            location);
    }
}
