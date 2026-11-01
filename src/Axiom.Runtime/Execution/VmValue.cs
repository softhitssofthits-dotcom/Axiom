using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Внутреннее тегированное значение цикла исполнения VM. Числовые типы и
/// BOOLEAN хранятся без упаковки; остальные виды значений держат ссылку на
/// обычный <see cref="RuntimeValue"/>. Публичным представлением значений языка
/// остаётся <see cref="RuntimeValue"/>: материализация происходит только на
/// границах (встроенные функции, снимки отладчика, хост, общие ячейки).
/// </summary>
/// <remarks>
/// Объединение LONG и DOUBLE безопасно: это числовые слоты без ссылок.
/// Ссылочный слот отдельный и для числовых видов всегда пуст, поэтому любая
/// пауза сборки мусора видит там null или корректную ссылку.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
internal struct VmValue
{
    [FieldOffset(0)] public VmValueKind Kind;

    /// <summary>INTEGER; для BOOLEAN — 0 или 1.</summary>
    [FieldOffset(4)] public int Integer;

    /// <summary>LONG.</summary>
    [FieldOffset(8)] public long Long;

    /// <summary>DOUBLE, разделяет слот с LONG.</summary>
    [FieldOffset(8)] public double Double;

    /// <summary>Ссылка на значение для всех видов кроме числовых и BOOLEAN.</summary>
    [FieldOffset(16)] public RuntimeValue? Object;

    public static VmValue FromInteger(int value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Integer;
        result.Integer = value;
        result.Long = 0;
        result.Object = null;
        return result;
    }

    public static VmValue FromLong(long value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Long;
        result.Integer = 0;
        result.Long = value;
        result.Object = null;
        return result;
    }

    public static VmValue FromDouble(double value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Double;
        result.Integer = 0;
        result.Double = value;
        result.Object = null;
        return result;
    }

    public static VmValue FromBoolean(bool value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Boolean;
        result.Integer = value ? 1 : 0;
        result.Long = 0;
        result.Object = null;
        return result;
    }

    /// <summary>Ссылочное значение: строки, DECIMAL, даты, массивы, записи и прочее.</summary>
    public static VmValue FromObject(RuntimeValue value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Object;
        result.Integer = 0;
        result.Long = 0;
        result.Object = value;
        return result;
    }

    /// <summary>
    /// Приводит обычное значение к внутреннему виду: числовые типы и BOOLEAN
    /// распаковываются, остальные остаются ссылками. Значения-аккумуляторы строк
    /// и записи типов остаются объектами.
    /// </summary>
    public static VmValue FromRuntime(RuntimeValue value)
    {
        var type = value.Type;
        if (type == TypeSymbol.Integer)
        {
            return FromInteger(value.AsInteger());
        }

        if (type == TypeSymbol.Long)
        {
            return FromLong(value.AsLong());
        }

        if (type == TypeSymbol.Double)
        {
            return FromDouble(value.AsDouble());
        }

        if (type == TypeSymbol.Boolean)
        {
            return FromBoolean(value.AsBoolean());
        }

        return FromObject(value);
    }

    /// <summary>Создаёт обычное значение; числовые виды проходят через кэш малых чисел.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue Materialize()
    {
        return Kind switch
        {
            VmValueKind.Integer => RuntimeValue.FromInteger(Integer),
            VmValueKind.Long => RuntimeValue.FromLong(Long),
            VmValueKind.Double => RuntimeValue.FromDouble(Double),
            VmValueKind.Boolean => Integer != 0 ? RuntimeValue.FromBoolean(true) : RuntimeValue.FromBoolean(false),
            VmValueKind.Object => Object!,
            _ => throw new InvalidOperationException("Пустой слот VM не может быть материализован.")
        };
    }

    public readonly bool IsInteger => Kind == VmValueKind.Integer;

    public readonly bool IsLong => Kind == VmValueKind.Long;

    public readonly bool IsDouble => Kind == VmValueKind.Double;

    public readonly bool IsObject => Kind == VmValueKind.Object;
}

internal enum VmValueKind : byte
{
    /// <summary>Нулевой слот очищенного массива; на стеке не встречается.</summary>
    Empty = 0,
    Integer = 1,
    Long = 2,
    Double = 3,
    Boolean = 4,
    Object = 5
}
