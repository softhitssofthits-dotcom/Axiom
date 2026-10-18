using System.Globalization;
using Axiom.Bytecode;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeValue : IRuntimeStoredValue
{
    private const int CachedIntegerMin = -1;
    private const int CachedIntegerMax = 1024;
    private const long CachedLongMin = -1;
    private const long CachedLongMax = 1024;
    private static readonly RuntimeValue[] CachedIntegers = CreateCachedIntegers();
    private static readonly RuntimeValue[] CachedLongs = CreateCachedLongs();
    private static readonly RuntimeValue[] CachedIntegerDisplayValues = CreateCachedIntegerDisplayValues();
    private static readonly RuntimeValue[] CachedLongDisplayValues = CreateCachedLongDisplayValues();
    private static readonly RuntimeValue EmptyStringValue = new(TypeSymbol.String, string.Empty);
    private static readonly RuntimeValue TrueStringValue = new(TypeSymbol.String, "TRUE");
    private static readonly RuntimeValue FalseStringValue = new(TypeSymbol.String, "FALSE");
    private static readonly RuntimeValue TrueValue = new(TypeSymbol.Boolean, true);
    private static readonly RuntimeValue FalseValue = new(TypeSymbol.Boolean, false);

    private RuntimeValue(TypeSymbol type, object? value)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        Value = value;
    }

    public static RuntimeValue Void { get; } = new(TypeSymbol.Void, value: null);

    public TypeSymbol Type { get; }

    public object? Value { get; }

    public static RuntimeValue Default(TypeSymbol type, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type == TypeSymbol.Integer)
        {
            return FromInteger(0);
        }

        if (type == TypeSymbol.Long)
        {
            return FromLong(0L);
        }

        if (type == TypeSymbol.Double)
        {
            return FromDouble(0d);
        }

        if (type == TypeSymbol.Decimal)
        {
            return FromDecimal(0m);
        }

        if (type == TypeSymbol.String)
        {
            return FromString(fixedStringLength is { } length
                ? new string(' ', length)
                : string.Empty);
        }

        if (type == TypeSymbol.Boolean)
        {
            return FromBoolean(false);
        }

        if (type == TypeSymbol.Date)
        {
            return FromDate(default);
        }

        if (type == TypeSymbol.Time)
        {
            return FromTime(default);
        }

        if (type == TypeSymbol.DateTime)
        {
            return FromDateTime(default);
        }

        if (type == TypeSymbol.UtcDateTime)
        {
            return FromUtcDateTime(default);
        }

        if (type == TypeSymbol.Void)
        {
            return Void;
        }

        if (type == TypeSymbol.RuntimeError)
        {
            return FromRuntimeError(new RuntimeErrorValue(string.Empty, string.Empty, 0, 0));
        }

        if (type is FunctionTypeSymbol functionType)
        {
            return FromFunction(functionType, value: null);
        }

        if (type is ArrayTypeSymbol arrayType)
        {
            return FromArray(arrayType, new RuntimeArrayValue(arrayType.ElementType, fixedStringLength, arrayType.Rank));
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            return FromRecord(userType, RuntimeRecordValue.CreateDefault(userType));
        }

        return new RuntimeValue(type, value: null);
    }

    public static RuntimeValue FromInteger(int value) =>
        value is >= CachedIntegerMin and <= CachedIntegerMax
            ? CachedIntegers[value - CachedIntegerMin]
            : new RuntimeValue(TypeSymbol.Integer, value);

    public static RuntimeValue FromLong(long value) =>
        value is >= CachedLongMin and <= CachedLongMax
            ? CachedLongs[(int)(value - CachedLongMin)]
            : new RuntimeValue(TypeSymbol.Long, value);

    public static RuntimeValue FromDouble(double value) => new(TypeSymbol.Double, value);

    public static RuntimeValue FromDecimal(decimal value) => new(TypeSymbol.Decimal, value);

    public static RuntimeValue FromString(string value) => value.Length == 0
        ? EmptyStringValue
        : new(TypeSymbol.String, value);

    internal static RuntimeValue FromStringAccumulator(RuntimeStringAccumulator value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new RuntimeValue(TypeSymbol.String, value);
    }

    public static RuntimeValue FromBoolean(bool value) => value ? TrueValue : FalseValue;

    public static RuntimeValue FromFile(RuntimeTextFile? value) => new(TypeSymbol.File, value);

    public static RuntimeValue FromImage(RuntimeImageValue? value) => new(TypeSymbol.Image, value);

    public static RuntimeValue FromSprite(RuntimeSpriteValue? value) => new(TypeSymbol.Sprite, value);

    public static RuntimeValue FromTilemap(RuntimeTilemapValue? value) => new(TypeSymbol.Tilemap, value);

    public static RuntimeValue FromTileset(RuntimeTilesetValue? value) => new(TypeSymbol.Tileset, value);

    public static RuntimeValue FromDate(DateOnly value) => new(TypeSymbol.Date, value);

    public static RuntimeValue FromTime(TimeOnly value) => new(TypeSymbol.Time, value);

    public static RuntimeValue FromDateTime(System.DateTime value) => new(TypeSymbol.DateTime, value);

    public static RuntimeValue FromUtcDateTime(System.DateTime value) => new(TypeSymbol.UtcDateTime, value);

    public static RuntimeValue FromRuntimeError(RuntimeErrorValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new RuntimeValue(TypeSymbol.RuntimeError, value);
    }

    public static RuntimeValue FromFunction(FunctionTypeSymbol type, RuntimeFunctionValue? value) => new(type, value);

    public static RuntimeValue FromArray(ArrayTypeSymbol type, RuntimeArrayValue value)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        return new RuntimeValue(type, value);
    }

    public static RuntimeValue FromRecord(UserDefinedTypeSymbol type, RuntimeRecordValue value)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(value);
        return new RuntimeValue(type, value);
    }

    private static RuntimeValue[] CreateCachedIntegers()
    {
        var values = new RuntimeValue[CachedIntegerMax - CachedIntegerMin + 1];
        for (var value = CachedIntegerMin; value <= CachedIntegerMax; value++)
        {
            values[value - CachedIntegerMin] = new RuntimeValue(TypeSymbol.Integer, value);
        }

        return values;
    }

    private static RuntimeValue[] CreateCachedLongs()
    {
        var values = new RuntimeValue[(int)(CachedLongMax - CachedLongMin + 1)];
        for (var value = CachedLongMin; value <= CachedLongMax; value++)
        {
            values[(int)(value - CachedLongMin)] = new RuntimeValue(TypeSymbol.Long, value);
        }

        return values;
    }

    private static RuntimeValue[] CreateCachedIntegerDisplayValues()
    {
        var values = new RuntimeValue[CachedIntegerMax - CachedIntegerMin + 1];
        for (var value = CachedIntegerMin; value <= CachedIntegerMax; value++)
        {
            values[value - CachedIntegerMin] = new RuntimeValue(
                TypeSymbol.String,
                value.ToString(CultureInfo.InvariantCulture));
        }

        return values;
    }

    private static RuntimeValue[] CreateCachedLongDisplayValues()
    {
        var values = new RuntimeValue[(int)(CachedLongMax - CachedLongMin + 1)];
        for (var value = CachedLongMin; value <= CachedLongMax; value++)
        {
            values[(int)(value - CachedLongMin)] = new RuntimeValue(
                TypeSymbol.String,
                value.ToString(CultureInfo.InvariantCulture));
        }

        return values;
    }

    internal static RuntimeValue FromBytecodeFunction(FunctionSymbol function, IReadOnlyList<BytecodeVariableSlot> capturedSlots)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedSlots);
        return new RuntimeValue(FunctionTypeSymbol.FromFunction(function), new RuntimeFunctionValue(function, capturedSlots));
    }

    internal static RuntimeValue FromBytecodeFunction(BytecodeRoutine routine, BytecodeVariableSlot[] capturedSlots)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(routine.Symbol);
        ArgumentNullException.ThrowIfNull(capturedSlots);
        return new RuntimeValue(FunctionTypeSymbol.FromFunction(routine.Symbol), new RuntimeFunctionValue(routine, capturedSlots));
    }

    internal static RuntimeValue FromBytecodeBuiltinFunction(FunctionSymbol function, int builtinBindingId)
    {
        ArgumentNullException.ThrowIfNull(function);
        return new RuntimeValue(
            FunctionTypeSymbol.FromFunction(function),
            new RuntimeFunctionValue(function, Array.Empty<BytecodeVariableSlot>(), builtinBindingId));
    }

    public int AsInteger() => Type == TypeSymbol.Integer
        ? (int)Value!
        : throw new InvalidOperationException($"Value of type {Type.Name} is not INTEGER.");

    public long AsLong() => Type == TypeSymbol.Long
        ? (long)Value!
        : throw new InvalidOperationException($"Value of type {Type.Name} is not LONG.");

    public double AsDouble()
    {
        if (Type == TypeSymbol.Double)
        {
            return (double)Value!;
        }

        if (Type == TypeSymbol.Integer)
        {
            return (int)Value!;
        }

        if (Type == TypeSymbol.Long)
        {
            return (long)Value!;
        }

        throw new InvalidOperationException($"Value of type {Type.Name} is not numeric.");
    }

    public decimal AsDecimal()
    {
        if (Type == TypeSymbol.Decimal)
        {
            return (decimal)Value!;
        }

        if (Type == TypeSymbol.Integer)
        {
            return (int)Value!;
        }

        if (Type == TypeSymbol.Long)
        {
            return (long)Value!;
        }

        throw new InvalidOperationException($"Value of type {Type.Name} is not DECIMAL.");
    }

    public string AsString()
    {
        if (Type != TypeSymbol.String)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not STRING.");
        }

        return Value switch
        {
            string text => text,
            RuntimeStringAccumulator accumulator => accumulator.ToString(),
            _ => string.Empty
        };
    }

    public bool AsBoolean() => Type == TypeSymbol.Boolean
        ? (bool)Value!
        : throw new InvalidOperationException($"Value of type {Type.Name} is not BOOLEAN.");

    public RuntimeFunctionValue? AsFunction()
    {
        if (Type is not FunctionTypeSymbol)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not FUNCTION.");
        }

        return (RuntimeFunctionValue?)Value;
    }

    public RuntimeTextFile? AsFile()
    {
        if (Type != TypeSymbol.File)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not FILE.");
        }

        return (RuntimeTextFile?)Value;
    }

    public RuntimeImageValue? AsImage()
    {
        if (Type != TypeSymbol.Image)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not IMAGE.");
        }

        return (RuntimeImageValue?)Value;
    }

    public RuntimeSpriteValue? AsSprite()
    {
        if (Type != TypeSymbol.Sprite)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not SPRITE.");
        }

        return (RuntimeSpriteValue?)Value;
    }

    public RuntimeTilesetValue? AsTileset()
    {
        if (Type != TypeSymbol.Tileset)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not TILESET.");
        }

        return (RuntimeTilesetValue?)Value;
    }

    public RuntimeTilemapValue? AsTilemap()
    {
        if (Type != TypeSymbol.Tilemap)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not TILEMAP.");
        }

        return (RuntimeTilemapValue?)Value;
    }

    public DateOnly AsDate()
    {
        if (Type != TypeSymbol.Date)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not DATE.");
        }

        return (DateOnly)Value!;
    }

    public TimeOnly AsTime()
    {
        if (Type != TypeSymbol.Time)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not TIME.");
        }

        return (TimeOnly)Value!;
    }

    public System.DateTime AsDateTime()
    {
        if (Type != TypeSymbol.DateTime)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not DATETIME.");
        }

        return (System.DateTime)Value!;
    }

    /// <summary>
    /// Значение момента на шкале UTC. Kind всегда Utc, поэтому повторные
    /// преобразования пояса не меняют само значение.
    /// </summary>
    public System.DateTime AsUtcDateTime()
    {
        if (Type != TypeSymbol.UtcDateTime)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not UTCDATETIME.");
        }

        return (System.DateTime)Value!;
    }

    public RuntimeArrayValue AsArray()
    {
        if (Type is not ArrayTypeSymbol)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not an array.");
        }

        return (RuntimeArrayValue)Value!;
    }

    public RuntimeErrorValue AsRuntimeError()
    {
        if (Type != TypeSymbol.RuntimeError)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not ERROR.");
        }

        return (RuntimeErrorValue)Value!;
    }

    public RuntimeRecordValue AsRecord()
    {
        if (Type is not UserDefinedTypeSymbol)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not TYPE.");
        }

        return (RuntimeRecordValue)Value!;
    }

    public RuntimeValue CopyForStorage(int? fixedStringLength = null)
    {
        if (Type == TypeSymbol.String)
        {
            var text = AsString();

            if (fixedStringLength is null && Value is not RuntimeStringAccumulator)
            {
                return this;
            }

            return FromString(fixedStringLength is { } length
                ? NormalizeFixedString(text, length)
                : text);
        }

        if (Type is ArrayTypeSymbol arrayType && fixedStringLength is not null)
        {
            return FromArray(arrayType, AsArray().Clone(fixedStringLength));
        }

        if (Type is UserDefinedTypeSymbol userType)
        {
            return FromRecord(userType, AsRecord().Clone());
        }

        if (Type == TypeSymbol.RuntimeError)
        {
            return this;
        }

        return this;
    }

    public static string NormalizeFixedString(string value, int fixedStringLength)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (fixedStringLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedStringLength), fixedStringLength, "Fixed string length must be positive.");
        }

        if (value.Length > fixedStringLength)
        {
            return value[..fixedStringLength];
        }

        if (value.Length < fixedStringLength)
        {
            return value.PadRight(fixedStringLength, ' ');
        }

        return value;
    }

    public string ToDisplayString()
    {
        if (Type == TypeSymbol.Integer)
        {
            return AsInteger().ToString(CultureInfo.InvariantCulture);
        }

        if (Type == TypeSymbol.Long)
        {
            return AsLong().ToString(CultureInfo.InvariantCulture);
        }

        if (Type == TypeSymbol.Double)
        {
            return AsDouble().ToString("G", CultureInfo.InvariantCulture);
        }

        if (Type == TypeSymbol.Decimal)
        {
            return AsDecimal().ToString(CultureInfo.InvariantCulture);
        }

        if (Type == TypeSymbol.String)
        {
            return AsString();
        }

        if (Type == TypeSymbol.Boolean)
        {
            return AsBoolean() ? "TRUE" : "FALSE";
        }

        if (Type == TypeSymbol.Date)
        {
            return AsDate().ToString(Builtins.BuiltinDateTimeHelpers.CanonicalDateFormat, CultureInfo.InvariantCulture);
        }

        if (Type == TypeSymbol.Time)
        {
            return Builtins.BuiltinDateTimeHelpers.FormatCanonicalTime(AsTime());
        }

        if (Type == TypeSymbol.DateTime)
        {
            return Builtins.BuiltinDateTimeHelpers.FormatCanonicalDateTime(AsDateTime());
        }

        if (Type == TypeSymbol.UtcDateTime)
        {
            return Builtins.BuiltinDateTimeHelpers.FormatCanonicalInstant(AsUtcDateTime());
        }

        if (Type == TypeSymbol.File)
        {
            var file = AsFile();
            return file is not null && file.IsOpen ? "<FILE>" : "<ЗАКРЫТЫЙ FILE>";
        }

        if (Type == TypeSymbol.Image)
        {
            var image = AsImage();
            return image is null ? "<ПУСТОЙ IMAGE>" : $"<IMAGE {image.Width}x{image.Height}>";
        }

        if (Type == TypeSymbol.Sprite)
        {
            var sprite = AsSprite();
            return sprite is null ? "<ПУСТОЙ SPRITE>" : $"<SPRITE {sprite.FrameWidth}x{sprite.FrameHeight}x{sprite.FrameCount}>";
        }

        if (Type == TypeSymbol.Tilemap)
        {
            var tilemap = AsTilemap();
            return tilemap is null ? "<ПУСТОЙ TILEMAP>" : $"<TILEMAP {tilemap.Width}x{tilemap.Height} layers={tilemap.LayerCount}>";
        }

        if (Type == TypeSymbol.Tileset)
        {
            var tileset = AsTileset();
            return tileset is null ? "<ПУСТОЙ TILESET>" : $"<TILESET {tileset.TileWidth}x{tileset.TileHeight}x{tileset.TileCount}>";
        }

        if (Type is FunctionTypeSymbol functionType)
        {
            return Value is RuntimeFunctionValue functionValue
                ? $"<ФУНКЦИЯ {functionValue.Function.Name}>"
                : $"<ПУСТАЯ {functionType.Name}>";
        }

        if (Type is ArrayTypeSymbol arrayType)
        {
            var array = AsArray();
            return array.IsAllocated
                ? $"<{arrayType.Name} {array.LowerBound} TO {array.UpperBound}>"
                : $"<UNALLOCATED {arrayType.Name}>";
        }

        if (Type is UserDefinedTypeSymbol userType)
        {
            return $"<TYPE {userType.Name}>";
        }

        if (Type == TypeSymbol.RuntimeError)
        {
            return $"<ERROR {AsRuntimeError().Code}>";
        }

        return string.Empty;
    }

    internal RuntimeValue ToDisplayRuntimeValue()
    {
        if (Type == TypeSymbol.String)
        {
            return this;
        }

        if (Type == TypeSymbol.Integer)
        {
            var value = AsInteger();
            return value is >= CachedIntegerMin and <= CachedIntegerMax
                ? CachedIntegerDisplayValues[value - CachedIntegerMin]
                : FromString(value.ToString(CultureInfo.InvariantCulture));
        }

        if (Type == TypeSymbol.Long)
        {
            var value = AsLong();
            return value is >= CachedLongMin and <= CachedLongMax
                ? CachedLongDisplayValues[(int)(value - CachedLongMin)]
                : FromString(value.ToString(CultureInfo.InvariantCulture));
        }

        if (Type == TypeSymbol.Boolean)
        {
            return AsBoolean() ? TrueStringValue : FalseStringValue;
        }

        if (Type == TypeSymbol.Date)
        {
            return FromString(AsDate().ToString(Builtins.BuiltinDateTimeHelpers.CanonicalDateFormat, CultureInfo.InvariantCulture));
        }

        if (Type == TypeSymbol.Time)
        {
            return FromString(Builtins.BuiltinDateTimeHelpers.FormatCanonicalTime(AsTime()));
        }

        if (Type == TypeSymbol.DateTime)
        {
            return FromString(Builtins.BuiltinDateTimeHelpers.FormatCanonicalDateTime(AsDateTime()));
        }

        if (Type == TypeSymbol.UtcDateTime)
        {
            return FromString(Builtins.BuiltinDateTimeHelpers.FormatCanonicalInstant(AsUtcDateTime()));
        }

        return FromString(ToDisplayString());
    }

    internal bool RequiresCopyForStorage => Value is RuntimeStringAccumulator;

    internal RuntimeStringAccumulator? AsStringAccumulator()
    {
        return Type == TypeSymbol.String
            ? Value as RuntimeStringAccumulator
            : null;
    }
}
