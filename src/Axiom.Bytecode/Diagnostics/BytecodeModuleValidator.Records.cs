using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private void ApplyValidateTypeInvariantStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var invariantType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, invariantType, TypeSymbol.Boolean, "TYPE invariant value");
    }

    private void ApplyCreateRecordStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        workingStack.Add(ResolveCreatedRecordType(module, routine, instructionIndex, instruction));
    }

    private void ApplyLoadFieldStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var recordType = PopStackType(workingStack);
        var fieldType = ValidateRecordFieldStackType(module, routine, instructionIndex, instruction, recordType);
        workingStack.Add(fieldType);
    }

    private void ApplyStoreFieldStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var recordType = PopStackType(workingStack);
        var valueType = PopStackType(workingStack);
        var fieldType = ValidateRecordFieldStackType(module, routine, instructionIndex, instruction, recordType);
        if (fieldType is not null)
        {
            ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, fieldType, "значение поля записи");
        }
    }
}
