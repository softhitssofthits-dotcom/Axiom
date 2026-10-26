using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private void ApplyCreateDictionaryStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        workingStack.Add(ResolveDictionaryStackType(module, routine, instructionIndex, instruction));
    }

    private void ApplyLoadDictionaryValueStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var keyType = PopStackType(workingStack);
        var dictionaryType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");

        if (resolved is null)
        {
            workingStack.Add(null);
            return;
        }

        ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, keyType, resolved.KeyType, "ключ словаря");
        workingStack.Add(resolved.ValueType);
    }

    private void ApplyStoreDictionaryValueStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var keyType = PopStackType(workingStack);
        var dictionaryType = PopStackType(workingStack);
        var valueType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");

        if (resolved is null)
        {
            return;
        }

        ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, keyType, resolved.KeyType, "ключ словаря");
        ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, resolved.ValueType, "значение записи словаря");
    }

    private void ApplyDictionaryAddStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var valueType = PopStackType(workingStack);
        var keyType = PopStackType(workingStack);
        var dictionaryType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");

        if (resolved is null)
        {
            return;
        }

        ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, keyType, resolved.KeyType, "ключ словаря");
        ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, resolved.ValueType, "значение новой записи");
    }

    private void ApplyDictionaryKeyLookupStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var keyType = PopStackType(workingStack);
        var dictionaryType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");

        if (resolved is not null)
        {
            ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, keyType, resolved.KeyType, "ключ словаря");
        }

        workingStack.Add(TypeSymbol.Boolean);
    }

    private void ApplyDictionaryKeysStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dictionaryType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");
        workingStack.Add(resolved is null ? null : ArrayTypeSymbol.Create(resolved.KeyType));
    }

    private void ApplyDictionaryCountStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dictionaryType = PopStackType(workingStack);
        _ = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");
        workingStack.Add(TypeSymbol.Integer);
    }

    private void ApplyDictionaryTryGetValueStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var keyType = PopStackType(workingStack);
        var dictionaryType = PopStackType(workingStack);
        var resolved = ValidateDictionaryStackType(module, routine, instructionIndex, instruction, dictionaryType, "словарь");

        if (resolved is not null)
        {
            ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, keyType, resolved.KeyType, "ключ словаря");

            var sourceKind = instruction.GetOperand(1);
            var sourceSlot = instruction.GetOperand(2);
            var destinationType = sourceKind switch
            {
                ByRefArgumentSourceKind.Argument when sourceSlot >= 0 && sourceSlot < routine.ParameterVariables.Count
                    => routine.ParameterVariables[sourceSlot].Type,
                ByRefArgumentSourceKind.Local when sourceSlot >= 0 && sourceSlot < routine.LocalVariables.Count
                    => routine.LocalVariables[sourceSlot].Type,
                ByRefArgumentSourceKind.Capture when sourceSlot >= 0 && sourceSlot < routine.CapturedVariables.Count
                    => routine.CapturedVariables[sourceSlot].Type,
                ByRefArgumentSourceKind.Global => module.Globals.FirstOrDefault(global => global.Slot == sourceSlot)?.Type,
                _ => null
            };

            if (destinationType is not null && destinationType != resolved.ValueType)
            {
                Report(
                    "BC4113",
                    $"Инструкция DictionaryTryGetValue в процедуре '{routine.Name}' записывает результат в переменную типа {destinationType.Name}, а значение словаря имеет тип {resolved.ValueType.Name}.",
                    module,
                    routine.Id,
                    instructionIndex);
            }
        }

        workingStack.Add(TypeSymbol.Boolean);
    }

    /// <summary>
    /// Тип словаря восстанавливается по метаданным модуля: эмиттер записывает
    /// каноническое имя типа, которое уже присутствует в типах глобалов,
    /// параметров, локальных и захваченных переменных. Неразрешимая ссылка
    /// отклоняется до запуска, а не превращается в неизвестный тип стека.
    /// </summary>
    private DictionaryTypeSymbol? ResolveDictionaryStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        var typeName = ReadStringConstant(module, instruction.GetOperand(0));

        if (typeName is null)
        {
            Report(
                "BC4134",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующую константу типа словаря.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (!TryGetModuleTypeIndex(module).TryGetValue(typeName.Trim(), out var resolved))
        {
            Report(
                "BC4135",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на неизвестный тип словаря '{typeName}'.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (resolved is DictionaryTypeSymbol dictionaryType)
        {
            return dictionaryType;
        }

        Report(
            "BC4135",
            $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает тип словаря, а ссылка '{typeName}' обозначает {resolved.Name}.",
            module,
            routine.Id,
            instructionIndex);
        return null;
    }

    /// <summary>
    /// Индекс канонических имён типов, встречающихся в модуле, включая
    /// вложенные типы элементов массивов, ключей и значений словарей, полей
    /// записей и сигнатур функций. Строится один раз на модуль.
    /// </summary>
    private IReadOnlyDictionary<string, TypeSymbol> TryGetModuleTypeIndex(BytecodeModule module)
    {
        if (ReferenceEquals(_moduleTypeIndexSource, module) && _moduleTypeIndex is not null)
        {
            return _moduleTypeIndex;
        }

        var index = new Dictionary<string, TypeSymbol>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<TypeSymbol>();

        void Add(TypeSymbol type)
        {
            if (!visited.Add(type))
            {
                return;
            }

            index[type.Name] = type;

            switch (type)
            {
                case ArrayTypeSymbol arrayType:
                    Add(arrayType.ElementType);
                    break;

                case DictionaryTypeSymbol dictionaryType:
                    Add(dictionaryType.KeyType);
                    Add(dictionaryType.ValueType);
                    break;

                case SetTypeSymbol setType:
                    Add(setType.ElementType);
                    break;

                case FunctionTypeSymbol functionType:
                    Add(functionType.ReturnType);
                    foreach (var parameterType in functionType.ParameterTypes)
                    {
                        Add(parameterType);
                    }

                    break;

                case UserDefinedTypeSymbol userType:
                    foreach (var field in userType.Fields)
                    {
                        Add(field.Type);
                    }

                    break;
            }
        }

        foreach (var global in module.Globals)
        {
            Add(global.Type);
        }

        foreach (var routine in module.Routines)
        {
            Add(routine.ResultType);

            foreach (var variable in routine.ParameterVariables)
            {
                Add(variable.Type);
            }

            foreach (var variable in routine.LocalVariables)
            {
                Add(variable.Type);
            }

            foreach (var variable in routine.CapturedVariables)
            {
                Add(variable.Type);
            }
        }

        foreach (var builtin in module.Builtins)
        {
            if (builtin.Function is { } function)
            {
                Add(FunctionTypeSymbol.FromFunction(function));
            }
        }

        _moduleTypeIndexSource = module;
        _moduleTypeIndex = index;
        return index;
    }

    private BytecodeModule? _moduleTypeIndexSource;
    private Dictionary<string, TypeSymbol>? _moduleTypeIndex;

    private DictionaryTypeSymbol? ValidateDictionaryStackType(
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

        if (stackType is DictionaryTypeSymbol dictionaryType)
        {
            return dictionaryType;
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

    private static string? ReadStringConstant(BytecodeModule module, int index)
    {
        if (index < 0 || index >= module.ConstantPool.Count)
        {
            return null;
        }

        var constant = module.ConstantPool[index];
        return constant.Kind == BytecodeConstantKind.String ? (string?)constant.Value : null;
    }
}
