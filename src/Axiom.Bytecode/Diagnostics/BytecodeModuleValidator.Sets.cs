using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private void ApplyCreateSetStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        workingStack.Add(ResolveSetStackType(module, routine, instructionIndex, instruction));
    }

    private void ApplySetElementOperationStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var elementType = PopStackType(workingStack);
        var setType = PopStackType(workingStack);
        var resolved = ValidateSetStackType(module, routine, instructionIndex, instruction, setType, "множество");

        if (resolved is not null)
        {
            ValidateAssignableStackType(
                module,
                routine,
                instructionIndex,
                instruction.Opcode,
                elementType,
                resolved.ElementType,
                "элемент множества");
        }

        // Add сообщает, добавлен ли элемент; Contains и Remove — признак
        // наличия или удаления.
        workingStack.Add(TypeSymbol.Boolean);
    }

    private void ApplySetCountStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var setType = PopStackType(workingStack);
        _ = ValidateSetStackType(module, routine, instructionIndex, instruction, setType, "множество");
        workingStack.Add(TypeSymbol.Integer);
    }

    private void ApplySetToArrayStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var setType = PopStackType(workingStack);
        var resolved = ValidateSetStackType(module, routine, instructionIndex, instruction, setType, "множество");
        workingStack.Add(resolved is null ? null : ArrayTypeSymbol.Create(resolved.ElementType));
    }

    private void ApplySetSetOperationStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var argumentType = PopStackType(workingStack);
        var receiverType = PopStackType(workingStack);
        var resolved = ValidateSetStackType(module, routine, instructionIndex, instruction, receiverType, "множество-получатель");

        if (resolved is null || argumentType is null)
        {
            return;
        }

        // Обе операции изменяют получателя и требуют множество точно того же
        // типа: HASHSET(INTEGER) и HASHSET(LONG) несовместимы.
        if (argumentType != resolved)
        {
            Report(
                "BC4139",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает второе множество типа {resolved.Name}, но получила {argumentType.Name}. Тип коллекции должен совпадать точно.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    /// <summary>
    /// Тип множества восстанавливается по метаданным модуля: эмиттер записывает
    /// каноническое имя HASHSET(элемент), которое присутствует в типах глобалов,
    /// параметров, локальных и захваченных переменных. Неразрешимая ссылка
    /// отклоняется до запуска, а не превращается в неизвестный тип стека.
    /// </summary>
    private SetTypeSymbol? ResolveSetStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        var typeName = ReadStringConstant(module, instruction.GetOperand(0));

        if (typeName is null)
        {
            Report(
                "BC4137",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующую константу типа множества.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (!TryGetModuleTypeIndex(module).TryGetValue(typeName.Trim(), out var resolved))
        {
            Report(
                "BC4138",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на неизвестный тип множества '{typeName}'.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (resolved is SetTypeSymbol setType)
        {
            return setType;
        }

        Report(
            "BC4138",
            $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает тип множества, а ссылка '{typeName}' обозначает {resolved.Name}.",
            module,
            routine.Id,
            instructionIndex);
        return null;
    }

    private SetTypeSymbol? ValidateSetStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        TypeSymbol? stackType,
        string role)
    {
        if (stackType is null)
        {
            return null;
        }

        if (stackType is SetTypeSymbol setType)
        {
            return setType;
        }

        ReportIncompatibleOperationStackType(
            module,
            routine,
            instructionIndex,
            instruction.Opcode,
            stackType,
            role);
        return null;
    }
}
