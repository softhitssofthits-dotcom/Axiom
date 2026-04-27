using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeValue : IRuntimeStoredValue
{
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

        if (type == TypeSymbol.Void)
        {
            return Void;
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

    public static RuntimeValue FromInteger(int value) => new(TypeSymbol.Integer, value);

    public static RuntimeValue FromLong(long value) => new(TypeSymbol.Long, value);

    public static RuntimeValue FromDouble(double value) => new(TypeSymbol.Double, value);

    public static RuntimeValue FromDecimal(decimal value) => new(TypeSymbol.Decimal, value);

    public static RuntimeValue FromString(string value) => new(TypeSymbol.String, value);

    public static RuntimeValue FromBoolean(bool value) => new(TypeSymbol.Boolean, value);

    public static RuntimeValue FromFile(RuntimeTextFile? value) => new(TypeSymbol.File, value);

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

    public static RuntimeValue FromFunction(FunctionSymbol function, RuntimeScope capturedScope)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedScope);
        return new RuntimeValue(FunctionTypeSymbol.FromFunction(function), new RuntimeFunctionValue(function, capturedScope));
    }

    internal static RuntimeValue FromBytecodeFunction(FunctionSymbol function, IReadOnlyList<BytecodeVariableSlot> capturedSlots)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(capturedSlots);
        return new RuntimeValue(FunctionTypeSymbol.FromFunction(function), new RuntimeFunctionValue(function, capturedSlots));
    }

    internal static RuntimeValue FromBytecodeBuiltinFunction(FunctionSymbol function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return new RuntimeValue(FunctionTypeSymbol.FromFunction(function), new RuntimeFunctionValue(function, []));
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

    public string AsString() => Type == TypeSymbol.String
        ? (string)Value!
        : throw new InvalidOperationException($"Value of type {Type.Name} is not STRING.");

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

    public RuntimeArrayValue AsArray()
    {
        if (Type is not ArrayTypeSymbol)
        {
            throw new InvalidOperationException($"Value of type {Type.Name} is not an array.");
        }

        return (RuntimeArrayValue)Value!;
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

            if (fixedStringLength is { } length)
            {
                text = NormalizeFixedString(text, length);
            }

            return FromString(text);
        }

        if (Type is ArrayTypeSymbol arrayType && fixedStringLength is not null)
        {
            return FromArray(arrayType, AsArray().Clone(fixedStringLength));
        }

        if (Type is UserDefinedTypeSymbol userType)
        {
            return FromRecord(userType, AsRecord().Clone());
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

        if (Type == TypeSymbol.File)
        {
            var file = AsFile();
            return file is not null && file.IsOpen ? "<FILE>" : "<ЗАКРЫТЫЙ FILE>";
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

        return string.Empty;
    }
}
