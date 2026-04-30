using System.Buffers;
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
        var dimensionCount = instruction.GetOperand(1);
        var rangeMask = instruction.OperandCount >= 3 ? instruction.GetOperand(2) : 0;

        if (rangeMask == 0)
        {
            if (dimensionCount == 1)
            {
                ExecuteDirectLoadElement1(frame, instruction, instructionIndex, location);
                return;
            }

            if (dimensionCount == 3)
            {
                ExecuteDirectLoadElement3(frame, instruction, instructionIndex, location);
                return;
            }

            if (dimensionCount <= 8)
            {
                Span<int> stackIndices = stackalloc int[dimensionCount];
                PopulateArrayIndices(frame, instructionIndex, location, stackIndices);
                ExecuteDirectLoadElement(frame, instruction, instructionIndex, location, stackIndices, rentedIndices: null);
                return;
            }

            using var indicesBuffer = RentArrayIndices(frame, instructionIndex, location, dimensionCount);
            ExecuteDirectLoadElement(frame, instruction, instructionIndex, location, indicesBuffer.Span, indicesBuffer.Buffer);
            return;
        }

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
        var dimensionCount = instruction.GetOperand(1);
        if (dimensionCount == 1)
        {
            ExecuteDirectStoreElement1(frame, instruction, instructionIndex, location);
            return;
        }

        if (dimensionCount == 3)
        {
            ExecuteDirectStoreElement3(frame, instruction, instructionIndex, location);
            return;
        }

        if (dimensionCount <= 8)
        {
            Span<int> stackIndices = stackalloc int[dimensionCount];
            PopulateArrayIndices(frame, instructionIndex, location, stackIndices);
            ExecuteDirectStoreElement(frame, instruction, instructionIndex, location, stackIndices, rentedIndices: null);
            return;
        }

        using var indicesBuffer = RentArrayIndices(frame, instructionIndex, location, dimensionCount);
        ExecuteDirectStoreElement(frame, instruction, instructionIndex, location, indicesBuffer.Span, indicesBuffer.Buffer);
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

    private PooledIntBuffer RentArrayIndices(
        BytecodeCallFrame frame,
        int instructionIndex,
        TextLocation location,
        int dimensionCount)
    {
        if (dimensionCount == 0)
        {
            return PooledIntBuffer.Empty;
        }

        var buffer = ArrayPool<int>.Shared.Rent(dimensionCount);
        var span = buffer.AsSpan(0, dimensionCount);

        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            var detailSuffix = dimensionCount == 1
                ? string.Empty
                : $" РёР·РјРµСЂРµРЅРёСЏ {dimension + 1}";
            span[dimension] = ToArrayBound(PopValue(frame, instructionIndex), location, $"Р—РЅР°С‡РµРЅРёРµ РёРЅРґРµРєСЃР° РјР°СЃСЃРёРІР°{detailSuffix}");
        }

        return new PooledIntBuffer(buffer, dimensionCount);
    }

    private void PopulateArrayIndices(
        BytecodeCallFrame frame,
        int instructionIndex,
        TextLocation location,
        Span<int> indices)
    {
        var dimensionCount = indices.Length;
        var stack = frame.EvaluationStack;

        for (var dimension = dimensionCount - 1; dimension >= 0; dimension--)
        {
            var detailSuffix = dimensionCount == 1
                ? string.Empty
                : $" РёР·РјРµСЂРµРЅРёСЏ {dimension + 1}";
            indices[dimension] = ToArrayBound(stack.Pop(), location, $"Р—РЅР°С‡РµРЅРёРµ РёРЅРґРµРєСЃР° РјР°СЃСЃРёРІР°{detailSuffix}");
        }
    }

    private void ExecuteDirectLoadElement(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location,
        ReadOnlySpan<int> indices,
        int[]? rentedIndices)
    {
        var directArrayValue = frame.EvaluationStack.Pop();
        var directDisplayName = ReadDebugName(instruction.GetOperand(0));
        var directArray = GetAllocatedArray(directArrayValue, directDisplayName, location);

        ExecuteDirectLoadElementAfterArray(frame, directArrayValue, directDisplayName, directArray, indices, rentedIndices, location);
    }

    private void ExecuteDirectLoadElement1(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var stack = frame.EvaluationStack;
        var index0 = ToArrayBound(stack.Pop(), location, "Значение индекса массива");
        var directArrayValue = stack.Pop();
        var directDisplayName = ReadDebugName(instruction.GetOperand(0));
        var directArray = GetAllocatedArray(directArrayValue, directDisplayName, location);

        if (directArray.Rank == 1)
        {
            if (directArray.TryGetValue1(index0, out var directValue))
            {
                stack.Push(directValue);
                return;
            }

            ReportArrayIndicesOutOfBounds(directDisplayName, stackalloc[] { index0 }, directArray, location);
            throw new RuntimeFailureException();
        }

        Span<int> indices = stackalloc[] { index0 };
        ExecuteDirectLoadElementAfterArray(frame, directArrayValue, directDisplayName, directArray, indices, rentedIndices: null, location);
    }

    private void ExecuteDirectLoadElement3(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var stack = frame.EvaluationStack;
        var index2 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 3");
        var index1 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 2");
        var index0 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 1");
        var directArrayValue = stack.Pop();
        var directDisplayName = ReadDebugName(instruction.GetOperand(0));
        var directArray = GetAllocatedArray(directArrayValue, directDisplayName, location);

        if (directArray.Rank == 3)
        {
            if (directArray.TryGetValue3(index0, index1, index2, out var directValue))
            {
                stack.Push(directValue);
                return;
            }

            ReportArrayIndicesOutOfBounds(directDisplayName, stackalloc[] { index0, index1, index2 }, directArray, location);
            throw new RuntimeFailureException();
        }

        Span<int> indices = stackalloc[] { index0, index1, index2 };
        ExecuteDirectLoadElementAfterArray(frame, directArrayValue, directDisplayName, directArray, indices, rentedIndices: null, location);
    }

    private void ExecuteDirectLoadElementAfterArray(
        BytecodeCallFrame frame,
        RuntimeValue directArrayValue,
        string directDisplayName,
        RuntimeArrayValue directArray,
        ReadOnlySpan<int> indices,
        int[]? rentedIndices,
        TextLocation location)
    {
        if (indices.Length == directArray.Rank)
        {
            if (directArray.TryGetValue(indices, out var directValue))
            {
                frame.EvaluationStack.Push(directValue);
                return;
            }

            if (rentedIndices is not null)
            {
                ReportArrayIndicesOutOfBounds(directDisplayName, rentedIndices, indices.Length, directArray, location);
            }
            else
            {
                ReportArrayIndicesOutOfBounds(directDisplayName, indices, directArray, location);
            }

            throw new RuntimeFailureException();
        }

        if (!directArray.ContainsLeadingIndices(indices))
        {
            ReportArraySlicesOutOfBounds(directDisplayName, CreateSingleIndexSlices(indices), directArray, location);
            throw new RuntimeFailureException();
        }

        var directResultType = ArrayTypeSymbol.Create(((ArrayTypeSymbol)directArrayValue.Type).ElementType, directArray.Rank - indices.Length);
        frame.EvaluationStack.Push(RuntimeValue.FromArray((ArrayTypeSymbol)directResultType, directArray.CreateView(indices)));
    }

    private void ExecuteDirectStoreElement(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location,
        ReadOnlySpan<int> indices,
        int[]? rentedIndices)
    {
        var stack = frame.EvaluationStack;
        var arrayValue = stack.Pop();
        var value = stack.Pop();
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.TrySetValue(indices, value))
        {
            if (rentedIndices is not null)
            {
                ReportArrayIndicesOutOfBounds(displayName, rentedIndices, indices.Length, array, location);
            }
            else
            {
                ReportArrayIndicesOutOfBounds(displayName, indices, array, location);
            }

            throw new RuntimeFailureException();
        }
    }

    private void ExecuteDirectStoreElement1(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var stack = frame.EvaluationStack;
        var index0 = ToArrayBound(stack.Pop(), location, "Значение индекса массива");
        var arrayValue = stack.Pop();
        var value = stack.Pop();
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.TrySetValue1(index0, value))
        {
            ReportArrayIndicesOutOfBounds(displayName, stackalloc[] { index0 }, array, location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteDirectStoreElement3(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        TextLocation location)
    {
        var stack = frame.EvaluationStack;
        var index2 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 3");
        var index1 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 2");
        var index0 = ToArrayBound(stack.Pop(), location, "Значение индекса массива измерения 1");
        var arrayValue = stack.Pop();
        var value = stack.Pop();
        var displayName = ReadDebugName(instruction.GetOperand(0));
        var array = GetAllocatedArray(arrayValue, displayName, location);

        if (!array.TrySetValue3(index0, index1, index2, value))
        {
            ReportArrayIndicesOutOfBounds(displayName, stackalloc[] { index0, index1, index2 }, array, location);
            throw new RuntimeFailureException();
        }
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
        var type = value.Type;

        if (type == TypeSymbol.Integer)
        {
            return value.AsInteger();
        }

        if (type == TypeSymbol.Long)
        {
            return ToInt32Checked(value.AsLong(), location, detail);
        }

        if (type == TypeSymbol.Double)
        {
            return ToInt32Checked(Math.Truncate(value.AsDouble()), location, detail);
        }

        if (type == TypeSymbol.Decimal)
        {
            return ToInt32Checked(decimal.Truncate(value.AsDecimal()), location, detail);
        }

        return ReportInvalidRuntimeState<int>(
            $"Значение типа '{value.Type.Name}' не может использоваться как индекс массива.",
            location);
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

    private static string FormatArrayIndices(ReadOnlySpan<int> indices)
    {
        if (indices.Length == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        builder.Append(indices[0]);

        for (var index = 1; index < indices.Length; index++)
        {
            builder.Append(", ");
            builder.Append(indices[index]);
        }

        return builder.ToString();
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

    private static RuntimeArrayIndexSlice[] CreateSingleIndexSlices(ReadOnlySpan<int> indices)
    {
        var slices = new RuntimeArrayIndexSlice[indices.Length];
        for (var index = 0; index < indices.Length; index++)
        {
            slices[index] = RuntimeArrayIndexSlice.Single(indices[index]);
        }

        return slices;
    }

    private void ReportArrayIndicesOutOfBounds(string arrayName, int[] indices, int count, RuntimeArrayValue array, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"РРЅРґРµРєСЃС‹ {FormatArrayIndices(indices.AsSpan(0, count))} РІС‹С…РѕРґСЏС‚ Р·Р° РіСЂР°РЅРёС†С‹ РјР°СЃСЃРёРІР° '{arrayName}' ({FormatArrayBounds(array)}).",
            location);
    }

    private void ReportArrayIndicesOutOfBounds(string arrayName, ReadOnlySpan<int> indices, RuntimeArrayValue array, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"РРЅРґРµРєСЃС‹ {FormatArrayIndices(indices)} РІС‹С…РѕРґСЏС‚ Р·Р° РіСЂР°РЅРёС†С‹ РјР°СЃСЃРёРІР° '{arrayName}' ({FormatArrayBounds(array)}).",
            location);
    }

    private readonly struct PooledIntBuffer : IDisposable
    {
        public static PooledIntBuffer Empty { get; } = new(Array.Empty<int>(), 0);

        private readonly int[] _buffer;
        private readonly int _count;

        public PooledIntBuffer(int[] buffer, int count)
        {
            _buffer = buffer;
            _count = count;
        }

        public int[] Buffer => _buffer;

        public ReadOnlySpan<int> Span => _buffer.AsSpan(0, _count);

        public void Dispose()
        {
            if (_buffer.Length > 0)
            {
                ArrayPool<int>.Shared.Return(_buffer);
            }
        }
    }
}
