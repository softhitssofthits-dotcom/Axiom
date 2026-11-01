using System.Runtime.CompilerServices;
using Axiom.Bytecode;

namespace Axiom.Runtime;

/// <summary>
/// Раскодированная инструкция цикла исполнения VM. Строится один раз из
/// <see cref="BytecodeInstruction"/> при создании исполнителя; первые пять
/// операндов хранятся полями, поэтому диспетчеризация читает непрерывную память
/// без обращения к массиву операндов исходной инструкции. Инструкции переменной
/// длины (CallByRef, BindArrayElement, Input) продолжают читать операнды с
/// индексом 5 и выше через отдельный массив. Формат байт-кода не меняется:
/// <see cref="BytecodeModule"/> остаётся источником истины.
/// </summary>
internal readonly struct VmInstruction
{
    /// <summary>Число операндов, хранящихся полями; совпадает с максимумом, читаемым исполнителем напрямую.</summary>
    private const int InlineOperandSlots = 5;

    public VmInstruction(BytecodeInstruction source)
    {
        var operands = source.Operands;
        var count = source.OperandCount;
        Opcode = source.Opcode;
        OperandCount = count;
        A = count > 0 ? operands[0] : 0;
        B = count > 1 ? operands[1] : 0;
        C = count > 2 ? operands[2] : 0;
        D = count > 3 ? operands[3] : 0;
        E = count > 4 ? operands[4] : 0;
        ExtraOperands = count > InlineOperandSlots ? ExtractExtraOperands(operands, count) : null;
    }

    /// <summary>
    /// Внутренний конструктор слитных инструкций декодированного потока
    /// (суперинструкции); в сериализуемый байт-код такие значения не попадают.
    /// </summary>
    internal VmInstruction(Opcode opcode, int a, int b, int c, int d, int e)
    {
        Opcode = opcode;
        OperandCount = InlineOperandSlots;
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        ExtraOperands = null;
    }

    public readonly Opcode Opcode;

    /// <summary>Первый операнд (у большинства инструкций — единственный).</summary>
    public readonly int A;

    /// <summary>Второй операнд (например, вид разделителя PRINT, признак newline).</summary>
    public readonly int B;

    /// <summary>Третий операнд (например, маска срезов массива).</summary>
    public readonly int C;

    /// <summary>Четвёртый операнд (например, ранг массива, целевой переход).</summary>
    public readonly int D;

    /// <summary>Пятый операнд (например, константный индекс RaiseError).</summary>
    public readonly int E;

    public readonly int OperandCount;

    /// <summary>Операнды с индексом 5 и выше; только для инструкций переменной длины.</summary>
    public readonly int[]? ExtraOperands;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOperand(int index)
    {
        if ((uint)index >= (uint)OperandCount)
        {
            ThrowOperandOutOfRange();
        }

        return index switch
        {
            0 => A,
            1 => B,
            2 => C,
            3 => D,
            4 => E,
            _ => ExtraOperands![index - InlineOperandSlots]
        };
    }

    private static int[] ExtractExtraOperands(System.Collections.Generic.IReadOnlyList<int> operands, int count)
    {
        var extra = new int[count - InlineOperandSlots];
        for (var index = 0; index < extra.Length; index++)
        {
            extra[index] = operands[InlineOperandSlots + index];
        }

        return extra;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperandOutOfRange()
    {
        throw new IndexOutOfRangeException("Операнд инструкции вне диапазона его количества.");
    }
}
