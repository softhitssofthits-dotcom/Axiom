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

    /// <summary>Ссылка на значение для всех видов кроме числовых, BOOLEAN и упакованных дат.</summary>
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
    // Упакованные календарные виды: значения держатся в существующих полях
    // тех же 24 байтов (DATE — номер дня в Integer, TIME/DATETIME — тики в
    // Long, вид System.DateTime — в Integer), поэтому даты и время в стеке,
    // локальных и глобальных слотах не боксируются и не аллоцируют.
    public static VmValue FromDate(DateOnly value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Date;
        result.Integer = value.DayNumber;
        result.Long = 0;
        result.Object = null;
        return result;
    }

    public static VmValue FromTime(TimeOnly value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Time;
        result.Integer = 0;
        result.Long = value.Ticks;
        result.Object = null;
        return result;
    }

    public static VmValue FromDateTime(System.DateTime value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.DateTime;
        result.Integer = (int)value.Kind;
        result.Long = value.Ticks;
        result.Object = null;
        return result;
    }

    public static VmValue FromUtcDateTime(System.DateTime value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.UtcDateTime;
        result.Integer = (int)DateTimeKind.Utc;
        result.Long = value.Ticks;
        result.Object = null;
        return result;
    }

    /// <summary>Ссылочное значение: строки, DECIMAL, массивы, записи и прочее.</summary>
    public static VmValue FromObject(RuntimeValue value)
    {
        VmValue result = default;
        result.Kind = VmValueKind.Object;
        result.Integer = 0;
        result.Long = 0;
        result.Object = value;
        return result;
    }

    /// <summary>Помещает кэш материализации в упакованное значение.</summary>
    private static VmValue WithCachedMaterialization(VmValue packed, RuntimeValue source)
    {
        packed.Object = source;
        return packed;
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

        // Календарные типы упаковываются в существующие поля, а исходная
        // ссылка сохраняется как ленивый кэш материализации: чтения через
        // границы RuntimeValue не боксируют значение заново, поэтому число
        // аллокаций никогда не превышает прежнее.
        if (type == TypeSymbol.Date)
        {
            return WithCachedMaterialization(FromDate(value.AsDate()), value);
        }

        if (type == TypeSymbol.Time)
        {
            return WithCachedMaterialization(FromTime(value.AsTime()), value);
        }

        if (type == TypeSymbol.DateTime)
        {
            return WithCachedMaterialization(FromDateTime(value.AsDateTime()), value);
        }

        if (type == TypeSymbol.UtcDateTime)
        {
            return WithCachedMaterialization(FromUtcDateTime(value.AsUtcDateTime()), value);
        }

        return FromObject(value);
    }

    /// <summary>Создаёт обычное значение; числовые виды проходят через кэш малых чисел. Упакованные даты кэшируют материализацию в ссылочном слоте.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue Materialize()
    {
        switch (Kind)
        {
            case VmValueKind.Integer:
                return RuntimeValue.FromInteger(Integer);
            case VmValueKind.Long:
                return RuntimeValue.FromLong(Long);
            case VmValueKind.Double:
                return RuntimeValue.FromDouble(Double);
            case VmValueKind.Boolean:
                return Integer != 0 ? RuntimeValue.FromBoolean(true) : RuntimeValue.FromBoolean(false);
            case VmValueKind.Object:
                return Object!;
            case VmValueKind.Date:
            case VmValueKind.Time:
            case VmValueKind.DateTime:
            case VmValueKind.UtcDateTime:
                // Ленивый кэш в свободном ссылочном поле: повторные чтения
                // значения из копий и слота не боксируют дату заново. Даты
                // неизменяемы, поэтому кэш безопасен для любых копий.
                if (Object is { } cached)
                {
                    return cached;
                }

                var materialized = Kind switch
                {
                    VmValueKind.Date => RuntimeValue.FromDate(DateOnly.FromDayNumber(Integer)),
                    VmValueKind.Time => RuntimeValue.FromTime(new TimeOnly(Long)),
                    VmValueKind.DateTime => RuntimeValue.FromDateTime(new System.DateTime(Long, (DateTimeKind)Integer)),
                    _ => RuntimeValue.FromUtcDateTime(new System.DateTime(Long, DateTimeKind.Utc))
                };
                Object = materialized;
                return materialized;
            default:
                throw new InvalidOperationException("Пустой слот VM не может быть материализован.");
        }
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
    Object = 5,
    /// <summary>DATE: номер дня в <see cref="Integer"/>.</summary>
    Date = 6,
    /// <summary>TIME: тики в <see cref="Long"/>.</summary>
    Time = 7,
    /// <summary>DATETIME: тики в <see cref="Long"/>, вид System.DateTime в <see cref="Integer"/>.</summary>
    DateTime = 8,
    /// <summary>UTCDATETIME: тики в <see cref="Long"/>, вид всегда UTC.</summary>
    UtcDateTime = 9
}
