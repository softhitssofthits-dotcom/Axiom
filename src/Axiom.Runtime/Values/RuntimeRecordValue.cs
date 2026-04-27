using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeRecordValue : IRuntimeStoredValue
{
    private readonly Dictionary<string, RuntimeValue> _fields = new(StringComparer.OrdinalIgnoreCase);

    private RuntimeRecordValue(UserDefinedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
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
            record._fields[field.Name] = CreateDefaultFieldValue(field, activeTypes);
        }

        activeTypes.Remove(type);
        return record;
    }

    public RuntimeValue GetField(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return _fields[field.Name];
    }

    public void SetField(TypeFieldSymbol field, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);
        _fields[field.Name] = CloneFieldValue(field, value);
    }

    public RuntimeRecordValue Clone()
    {
        var clone = new RuntimeRecordValue(Type);

        foreach (var field in Type.Fields)
        {
            clone._fields[field.Name] = CloneFieldValue(field, _fields[field.Name]);
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
                FixedArrayLowerBound: { } lowerBound,
                FixedArrayUpperBound: { } upperBound
            })
        {
            return RuntimeValue.FromArray(arrayType, new RuntimeArrayValue(arrayType.ElementType, field.FixedStringLength, lowerBound, upperBound));
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
