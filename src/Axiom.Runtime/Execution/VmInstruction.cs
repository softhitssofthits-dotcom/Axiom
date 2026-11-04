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
    private const ushort Extended = ushort.MaxValue;
    // Общий маркер только для внутренней специализации; исходные инструкции
    // создаются без этой ссылки, включая повреждённый внешний байт-код.
    private static readonly int[] FreshRecordLocalMarker = new int[0];
    private readonly ushort _opcode;
    private readonly ushort _operandCount;

    public VmInstruction(BytecodeInstruction source)
    {
        var operands = source.Operands;
        var count = source.OperandCount;
        var opcode = (int)source.Opcode;
        var prefixLength = (uint)opcode >= Extended ? 1 : 0;
        _opcode = prefixLength == 0 ? (ushort)opcode : Extended;
        _operandCount = count < Extended ? (ushort)count : Extended;
        A = count > 0 ? operands[0] : 0;
        B = count > 1 ? operands[1] : 0;
        C = count > 2 ? operands[2] : 0;
        D = count > 3 ? operands[3] : 0;
        E = count > 4 ? operands[4] : 0;
        var extraCount = Math.Max(0, count - InlineOperandSlots);
        ExtraOperands = extraCount + prefixLength == 0 ? null : new int[extraCount + prefixLength];
        if (prefixLength != 0)
        {
            ExtraOperands![0] = opcode;
        }

        for (var index = 0; index < extraCount; index++)
        {
            ExtraOperands![prefixLength + index] = operands[InlineOperandSlots + index];
        }
    }

    /// <summary>
    /// Внутренний конструктор слитных инструкций декодированного потока
    /// (суперинструкции); в сериализуемый байт-код такие значения не попадают.
    /// </summary>
    internal VmInstruction(Opcode opcode, int a, int b, int c, int d, int e)
    {
        var opcodeValue = (int)opcode;
        _opcode = (uint)opcodeValue < Extended ? (ushort)opcodeValue : Extended;
        _operandCount = InlineOperandSlots;
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        ExtraOperands = _opcode == Extended ? [opcodeValue] : null;
    }

    private VmInstruction(int typeIdentity, int localSlot)
    {
        _opcode = (ushort)Opcode.CreateRecord;
        _operandCount = 1;
        A = typeIdentity;
        B = localSlot;
        C = D = E = 0;
        ExtraOperands = FreshRecordLocalMarker;
    }

    internal static VmInstruction CreateFreshRecordLocal(int typeIdentity, int localSlot)
        => new(typeIdentity, localSlot);

    internal bool IsFreshRecordLocal => ReferenceEquals(ExtraOperands, FreshRecordLocalMarker);

    /// <summary>Опкод за пределами обычного диапазона хранится без потери в дополнительном массиве.</summary>
    public Opcode Opcode => _opcode == Extended ? (Opcode)ExtraOperands![0] : (Opcode)_opcode;

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

    /// <summary>Длинные инструкции восстанавливают количество из дополнительного массива.</summary>
    public int OperandCount => _operandCount == Extended
        ? ExtraOperands!.Length - (_opcode == Extended ? 1 : 0) + InlineOperandSlots
        : _operandCount;

    /// <summary>Операнды с индекса 5 и, при необходимости, полный неизвестный опкод.</summary>
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
            _ => ExtraOperands![index - InlineOperandSlots + (_opcode == Extended ? 1 : 0)]
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperandOutOfRange()
    {
        throw new IndexOutOfRangeException("Операнд инструкции вне диапазона его количества.");
    }
}
