using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteRedimArray(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var dimensionCount = instruction.GetOperand(2);
        var upperBounds = new int[dimensionCount];
        var lowerBounds = new int[dimensionCount];

        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            var detailSuffix = dimensionCount == 1
                ? string.Empty
                : $" измерения {dimension + 1}";
            upperBounds[dimension] = ToArrayBound(PopValue(frame, instructionIndex), location, $"Верхняя граница массива{detailSuffix}");
            lowerBounds[dimension] = ToArrayBound(PopValue(frame, instructionIndex), location, $"Нижняя граница массива{detailSuffix}");
        }

        var arrayValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(1));
        var array = GetArrayValue(arrayValue, displayName, location);

        if (array.IsView)
        {
            ReportBuiltInArgumentError(
                instruction.GetOperand(0) != 0 ? "REDIM PRESERVE" : "REDIM",
                "array",
                "не поддерживает подмассивы-представления.",
                location);
            throw new RuntimeFailureException();
        }

        EnsureAllowedArrayLowerBounds(displayName, lowerBounds, location);

        if (!IsValidArrayBounds(lowerBounds, upperBounds))
        {
            ReportInvalidRuntimeState(
                $"Массив '{displayName}' имеет неверные границы. Для каждого измерения верхняя граница не может быть меньше нижней, кроме пустой формы 1 TO 0.",
                location);
            throw new RuntimeFailureException();
        }

        if (instruction.GetOperand(0) != 0)
        {
            array.AllocatePreserving(lowerBounds, upperBounds);
        }
        else
        {
            array.Allocate(lowerBounds, upperBounds);
        }
    }

    private void ExecuteEraseArray(BytecodeCallFrame frame, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var arrayValue = PopValue(frame, instructionIndex);
        var array = arrayValue.AsArray();

        if (array.IsView)
        {
            ReportBuiltInArgumentError("ERASE", "array", "не поддерживает подмассивы-представления.", location);
            throw new RuntimeFailureException();
        }

        array.Deallocate();
    }

    private void ExecuteLoadElement(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var slices = PopArrayIndexSlices(frame, instruction, instructionIndex, location);
        var arrayValue = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!TryValidateArraySlices(array, slices))
        {
            ReportArraySlicesOutOfBounds(displayName, slices, array, location);
            throw new RuntimeFailureException();
        }

        if (slices.Length == array.Rank && slices.All(static slice => !slice.IsRange))
        {
            var indices = slices.Select(static slice => slice.LowerBound).ToArray();
            frame.EvaluationStack.Push(array.GetValue(indices));
            return;
        }

        var fixedIndexCount = slices.Count(static slice => !slice.IsRange);
        var resultType = ArrayTypeSymbol.Create(((ArrayTypeSymbol)arrayValue.Type).ElementType, array.Rank - fixedIndexCount);
        frame.EvaluationStack.Push(RuntimeValue.FromArray((ArrayTypeSymbol)resultType, array.CreateView(slices)));
    }

    private void ExecuteStoreElement(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var indices = PopArrayIndices(frame, instruction, instructionIndex, location);
        var arrayValue = PopValue(frame, instructionIndex);
        var value = PopValue(frame, instructionIndex);
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.ContainsIndices(indices))
        {
            ReportArrayIndicesOutOfBounds(displayName, indices, array, location);
            throw new RuntimeFailureException();
        }

        array.SetValue(indices, value);
    }

    private int[] PopArrayIndices(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var dimensionCount = instruction.GetOperand(1);
        var indices = new int[dimensionCount];

        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            var detailSuffix = dimensionCount == 1
                ? string.Empty
                : $" измерения {dimension + 1}";
            indices[dimension] = ToArrayBound(PopValue(frame, instructionIndex), location, $"Значение индекса массива{detailSuffix}");
        }

        return indices;
    }

    private RuntimeArrayIndexSlice[] PopArrayIndexSlices(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var dimensionCount = instruction.GetOperand(1);
        var rangeMask = instruction.OperandCount >= 3 ? instruction.GetOperand(2) : 0;
        var slices = new RuntimeArrayIndexSlice[dimensionCount];

        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            var detailSuffix = dimensionCount == 1
                ? string.Empty
                : $" РёР·РјРµСЂРµРЅРёСЏ {dimension + 1}";

            if ((rangeMask & (1 << dimension)) != 0)
            {
                var upperBound = ToArrayBound(PopValue(frame, instructionIndex), location, $"Р’РµСЂС…РЅСЏСЏ РіСЂР°РЅРёС†Р° СЃСЂРµР·Р°{detailSuffix}");
                var lowerBound = ToArrayBound(PopValue(frame, instructionIndex), location, $"РќРёР¶РЅСЏСЏ РіСЂР°РЅРёС†Р° СЃСЂРµР·Р°{detailSuffix}");
                slices[dimension] = RuntimeArrayIndexSlice.Range(lowerBound, upperBound);
                continue;
            }

            slices[dimension] = RuntimeArrayIndexSlice.Single(ToArrayBound(PopValue(frame, instructionIndex), location, $"Р—РЅР°С‡РµРЅРёРµ РёРЅРґРµРєСЃР° РјР°СЃСЃРёРІР°{detailSuffix}"));
        }

        return slices;
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

    private static bool IsValidArrayBounds(IReadOnlyList<int> lowerBounds, IReadOnlyList<int> upperBounds)
    {
        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            var lowerBound = lowerBounds[dimension];
            var upperBound = upperBounds[dimension];

            if (upperBound < lowerBound && (lowerBound != 1 || upperBound != 0))
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureAllowedArrayLowerBounds(string displayName, IReadOnlyList<int> lowerBounds, TextLocation location)
    {
        for (var dimension = 0; dimension < lowerBounds.Count; dimension++)
        {
            if (lowerBounds[dimension] != int.MinValue)
            {
                continue;
            }

            var detailSuffix = lowerBounds.Count == 1
                ? string.Empty
                : $" в измерении {dimension + 1}";
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Массив '{displayName}' не может иметь нижнюю границу {int.MinValue}{detailSuffix}, потому что значение слева от диапазона используется поисковыми функциями массива как признак отсутствия совпадения.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private void ReportArrayIndicesOutOfBounds(string arrayName, IReadOnlyList<int> indices, RuntimeArrayValue array, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"Индексы {FormatArrayIndices(indices)} выходят за границы массива '{arrayName}' ({FormatArrayBounds(array)}).",
            location);
    }

    private void ReportArraySlicesOutOfBounds(string arrayName, IReadOnlyList<RuntimeArrayIndexSlice> slices, RuntimeArrayValue array, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"РРЅРґРµРєСЃС‹ {FormatArraySlices(slices)} РІС‹С…РѕРґСЏС‚ Р·Р° РіСЂР°РЅРёС†С‹ РјР°СЃСЃРёРІР° '{arrayName}' ({FormatArrayBounds(array)}).",
            location);
    }

    private static string FormatArrayIndices(IReadOnlyList<int> indices)
    {
        return string.Join(", ", indices);
    }

    private static string FormatArraySlices(IReadOnlyList<RuntimeArrayIndexSlice> slices)
    {
        return string.Join(", ", slices.Select(static slice => slice.IsRange ? $"{slice.LowerBound} TO {slice.UpperBound}" : slice.LowerBound.ToString()));
    }

    private static bool TryValidateArraySlices(RuntimeArrayValue array, IReadOnlyList<RuntimeArrayIndexSlice> slices)
    {
        if (!array.IsAllocated || slices.Count < 1 || slices.Count > array.Rank)
        {
            return false;
        }

        for (var index = 0; index < slices.Count; index++)
        {
            var lowerBound = array.GetLowerBound(index + 1);
            var upperBound = array.GetUpperBound(index + 1);
            var slice = slices[index];

            if (slice.LowerBound < lowerBound || slice.LowerBound > upperBound)
            {
                return false;
            }

            if (slice.UpperBound < lowerBound || slice.UpperBound > upperBound)
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatArrayBounds(RuntimeArrayValue array)
    {
        return string.Join(
            "; ",
            Enumerable.Range(1, array.Rank)
                .Select(dimension => $"{array.GetLowerBound(dimension)}..{array.GetUpperBound(dimension)}"));
    }
}
