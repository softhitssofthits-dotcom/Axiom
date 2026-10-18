using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeRecordValue : IRuntimeStoredValue
{
    private readonly RuntimeValue[] _fields;

    private RuntimeRecordValue(UserDefinedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
        _fields = new RuntimeValue[type.Fields.Count];
    }

    public UserDefinedTypeSymbol Type { get; }

    public static RuntimeRecordValue CreateDefault(UserDefinedTypeSymbol type)
    {
        return CreateDefault(type, []);
    }

    private static RuntimeRecordValue CreateDefault(UserDefinedTypeSymbol type, HashSet<UserDefinedTypeSymbol> activeTypes)
    {
        if (!activeTypes.Add(type))
        {
            throw new InvalidOperationException($"Cannot create recursive default value for TYPE '{type.Name}'.");
        }

        var record = new RuntimeRecordValue(type);

        foreach (var field in type.Fields)
        {
            record._fields[field.Ordinal] = CreateDefaultFieldValue(field, activeTypes);
        }

        activeTypes.Remove(type);
        return record;
    }

    /// <summary>
    /// Создаёт запись для снимка состояния. Поля заполняются отдельно методом
    /// <see cref="SetSnapshotField"/> без правил присваивания: вызывающая сторона
    /// сама строит независимый граф значений.
    /// </summary>
    internal static RuntimeRecordValue CreateForSnapshot(UserDefinedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new RuntimeRecordValue(type);
    }

    internal void SetSnapshotField(TypeFieldSymbol field, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);
        _fields[field.Ordinal] = value;
    }

    public RuntimeValue GetField(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return _fields[field.Ordinal];
    }

    public void SetField(TypeFieldSymbol field, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);
        _fields[field.Ordinal] = CloneFieldValue(field, value);
    }

    public RuntimeRecordValue Clone()
    {
        var clone = new RuntimeRecordValue(Type);

        foreach (var field in Type.Fields)
        {
            clone._fields[field.Ordinal] = CloneFieldValue(field, _fields[field.Ordinal]);
        }

        return clone;
    }

    private static RuntimeValue CreateDefaultFieldValue(TypeFieldSymbol field, HashSet<UserDefinedTypeSymbol> activeTypes)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(activeTypes);

        if (field.HasDefaultValue)
        {
            return CreateConfiguredDefaultFieldValue(field);
        }

        if (field.Type is UserDefinedTypeSymbol nestedType)
        {
            return RuntimeValue.FromRecord(nestedType, CreateDefault(nestedType, activeTypes));
        }

        if (field is
            {
                IsFixedArray: true,
                Type: ArrayTypeSymbol arrayType,
                FixedArrayLowerBounds: { } lowerBounds,
                FixedArrayUpperBounds: { } upperBounds
            })
        {
            return RuntimeValue.FromArray(
                arrayType,
                new RuntimeArrayValue(arrayType.ElementType, field.FixedStringLength, lowerBounds, upperBounds));
        }

        return RuntimeValue.Default(field.Type, field.FixedStringLength);
    }

    private static RuntimeValue CreateConfiguredDefaultFieldValue(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (field.Type == TypeSymbol.Integer)
        {
            return RuntimeValue.FromInteger((int)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.Long)
        {
            return RuntimeValue.FromLong((long)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.Double)
        {
            return RuntimeValue.FromDouble((double)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.Decimal)
        {
            return RuntimeValue.FromDecimal((decimal)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.String)
        {
            var text = (string)field.DefaultValue!;
            return RuntimeValue.FromString(
                field.FixedStringLength is { } length
                    ? RuntimeValue.NormalizeFixedString(text, length)
                    : text);
        }

        if (field.Type == TypeSymbol.Boolean)
        {
            return RuntimeValue.FromBoolean((bool)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.Date)
        {
            return RuntimeValue.FromDate((DateOnly)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.Time)
        {
            return RuntimeValue.FromTime((TimeOnly)field.DefaultValue!);
        }

        if (field.Type == TypeSymbol.DateTime)
        {
            return RuntimeValue.FromDateTime((DateTime)field.DefaultValue!);
        }

        throw new InvalidOperationException($"TYPE field '{field.Name}' has unsupported default value type '{field.Type.Name}'.");
    }

    private static RuntimeValue CloneFieldValue(TypeFieldSymbol field, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

        if (field.Type is ArrayTypeSymbol stringArrayType && field.FixedStringLength is not null)
        {
            return RuntimeValue.FromArray(stringArrayType, value.AsArray().Clone(field.FixedStringLength));
        }

        if (field.IsFixedArray && field.Type is ArrayTypeSymbol arrayType)
        {
            return RuntimeValue.FromArray(arrayType, value.AsArray().Clone());
        }

        return value.CopyForStorage(field.FixedStringLength);
    }
}
