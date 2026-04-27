using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteRedimArray(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var upperBound = ToArrayBound(PopValue(frame, instructionIndex), location, "Верхняя граница массива");
        var lowerBound = ToArrayBound(PopValue(frame, instructionIndex), location, "Нижняя граница массива");
        var arrayValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(1));
        var array = GetArrayValue(arrayValue, displayName, location);

        EnsureAllowedArrayLowerBound(displayName, lowerBound, location);

        if (!IsValidArrayBounds(lowerBound, upperBound))
        {
            ReportInvalidRuntimeState(
                $"Массив '{displayName}' имеет неверные границы. Верхняя граница не может быть меньше нижней, кроме пустой формы 1 TO 0.",
                location);
            throw new RuntimeFailureException();
        }

        if (instruction.GetOperand(0) != 0)
        {
            array.AllocatePreserving(lowerBound, upperBound);
        }
        else
        {
            array.Allocate(lowerBound, upperBound);
        }
    }

    private void ExecuteEraseArray(BytecodeCallFrame frame, int instructionIndex)
    {
        var arrayValue = PopValue(frame, instructionIndex);
        arrayValue.AsArray().Deallocate();
    }

    private void ExecuteLoadElement(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var index = ToArrayBound(PopValue(frame, instructionIndex), location, "Значение индекса массива");
        var arrayValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.ContainsIndex(index))
        {
            ReportArrayIndexOutOfBounds(displayName, index, array.LowerBound, array.UpperBound, location);
            throw new RuntimeFailureException();
        }

        frame.EvaluationStack.Push(array.GetValue(index));
    }

    private void ExecuteStoreElement(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var index = ToArrayBound(PopValue(frame, instructionIndex), location, "Значение индекса массива");
        var arrayValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.ContainsIndex(index))
        {
            ReportArrayIndexOutOfBounds(displayName, index, array.LowerBound, array.UpperBound, location);
            throw new RuntimeFailureException();
        }

        array.SetValue(index, value);
    }

    private RuntimeArrayValue GetArrayValue(RuntimeValue value, string displayName, TextLocation location)
    {
        if (value.Type is not ArrayTypeSymbol)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Значение '{displayName}' не содержит массива времени выполнения.",
                location);
        }

        return value.AsArray();
    }

    private RuntimeArrayValue GetAllocatedArray(RuntimeValue value, string displayName, TextLocation location)
    {
        var array = GetArrayValue(value, displayName, location);

        if (!array.IsAllocated)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Массив '{displayName}' объявлен, но ещё не выделен.",
                location);
        }

        return array;
    }

    private int ToArrayBound(RuntimeValue value, TextLocation location, string detail)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), location, detail),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Truncate(value.AsDouble()), location, detail),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Truncate(value.AsDecimal()), location, detail),
            _ => ReportInvalidRuntimeState<int>(
                $"Значение типа '{value.Type.Name}' не может использоваться как индекс массива.",
                location)
        };
    }

    private static bool IsValidArrayBounds(int lowerBound, int upperBound)
    {
        return upperBound >= lowerBound || (lowerBound == 1 && upperBound == 0);
    }

    private void EnsureAllowedArrayLowerBound(string displayName, int lowerBound, TextLocation location)
    {
        if (lowerBound != int.MinValue)
        {
            return;
        }

        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Массив '{displayName}' не может иметь нижнюю границу {int.MinValue}, потому что значение слева от диапазона используется поисковыми функциями массива как признак отсутствия совпадения.",
            location);
        throw new RuntimeFailureException();
    }

    private void ReportArrayIndexOutOfBounds(string arrayName, int index, int lowerBound, int upperBound, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"Индекс {index} выходит за границы массива '{arrayName}' ({lowerBound}..{upperBound}).",
            location);
    }
}
