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
