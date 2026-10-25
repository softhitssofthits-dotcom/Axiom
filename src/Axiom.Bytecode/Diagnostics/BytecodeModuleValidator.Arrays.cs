using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private void ApplyEraseArrayStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var arrayType = PopStackType(workingStack);
        ValidateArrayStackType(module, routine, instructionIndex, instruction.Opcode, arrayType, "массив");
    }

    private void ApplyRedimArrayStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dimensionCount = instruction.GetOperand(2);
        var upperBoundTypes = new TypeSymbol?[dimensionCount];
        var lowerBoundTypes = new TypeSymbol?[dimensionCount];
        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            upperBoundTypes[dimension] = PopStackType(workingStack);
            lowerBoundTypes[dimension] = PopStackType(workingStack);
        }
        var arrayType = PopStackType(workingStack);
        ValidateArrayStackType(module, routine, instructionIndex, instruction.Opcode, arrayType, "массив");
        for (var dimension = 0; dimension < dimensionCount; dimension++)
        {
            ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, lowerBoundTypes[dimension], $"нижнюю границу измерения {dimension + 1}");
            ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, upperBoundTypes[dimension], $"верхнюю границу измерения {dimension + 1}");
        }
    }

    private void ApplyNewArrayStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dimensionCount = instruction.GetOperand(1);
        var templateType = PopStackType(workingStack);

        if (templateType is ArrayTypeSymbol templateArray && templateArray.Rank != dimensionCount)
        {
            Report(
                "BC4111",
                $"Инструкция NewArray в процедуре '{routine.Name}' создаёт массив ранга {dimensionCount}, а образец имеет ранг {templateArray.Rank}.",
                module,
                routine.Id,
                instructionIndex);
            workingStack.Add(null);
            return;
        }

        if (templateType is null || templateType is ArrayTypeSymbol)
        {
            workingStack.Add(templateType);
            return;
        }

        Report(
            "BC4111",
            $"Инструкция NewArray в процедуре '{routine.Name}' ожидает образец массива, но получила {templateType.Name}.",
            module,
            routine.Id,
            instructionIndex);
        workingStack.Add(null);
    }
    private void ApplyLoadElementStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dimensionCount = instruction.GetOperand(1);
        var rangeMask = instruction.GetOperand(2);
        var indexTypes = new TypeSymbol?[dimensionCount];
        var upperBoundTypes = new TypeSymbol?[dimensionCount];
        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            if (IsRangeDimension(rangeMask, dimension))
            {
                upperBoundTypes[dimension] = PopStackType(workingStack);
                indexTypes[dimension] = PopStackType(workingStack);
            }
            else
            {
                indexTypes[dimension] = PopStackType(workingStack);
            }
        }
        var arrayType = PopStackType(workingStack);
        for (var dimension = 0; dimension < dimensionCount; dimension++)
        {
            if (IsRangeDimension(rangeMask, dimension))
            {
                ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, indexTypes[dimension], $"нижнюю границу среза измерения {dimension + 1}");
                ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, upperBoundTypes[dimension], $"верхнюю границу среза измерения {dimension + 1}");
            }
            else
            {
                ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, indexTypes[dimension], $"индекс измерения {dimension + 1}");
            }
        }
        if (ValidateArrayStackType(module, routine, instructionIndex, instruction.Opcode, arrayType, "массив") is { } typedArray)
        {
            if (dimensionCount > typedArray.Rank)
            {
                Report(
                    "BC4111",
                    $"Инструкция LoadElement в процедуре '{routine.Name}' запрашивает {dimensionCount} индексов у массива ранга {typedArray.Rank}.",
                    module,
                    routine.Id,
                    instructionIndex);
                workingStack.Add(null);
            }
            else
            {
                var fixedDimensionCount = dimensionCount - BytecodeStackEffect.CountRangeBits(instruction);
                workingStack.Add(fixedDimensionCount == typedArray.Rank
                    ? typedArray.ElementType
                    : ArrayTypeSymbol.Create(typedArray.ElementType, typedArray.Rank - fixedDimensionCount));
            }
        }
        else
        {
            workingStack.Add(null);
        }
    }

    private void ApplyStoreElementStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var dimensionCount = instruction.GetOperand(1);
        var indexTypes = new TypeSymbol?[dimensionCount];
        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            indexTypes[dimension] = PopStackType(workingStack);
        }
        var arrayType = PopStackType(workingStack);
        var valueType = PopStackType(workingStack);
        for (var dimension = 0; dimension < dimensionCount; dimension++)
        {
            ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, indexTypes[dimension], $"индекс измерения {dimension + 1}");
        }
        if (ValidateArrayStackType(module, routine, instructionIndex, instruction.Opcode, arrayType, "массив") is { } typedArray)
        {
            if (dimensionCount != typedArray.Rank)
            {
                Report(
                    "BC4112",
                    $"Инструкция StoreElement в процедуре '{routine.Name}' запрашивает {dimensionCount} индексов у массива ранга {typedArray.Rank}; для записи элемента нужны все измерения.",
                    module,
                    routine.Id,
                    instructionIndex);
            }

            ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, typedArray.ElementType, "значение элемента массива");
        }
    }
}
