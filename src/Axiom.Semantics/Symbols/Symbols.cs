using Axiom.Syntax;
using System.Runtime.CompilerServices;

namespace Axiom.Semantics;

public abstract class Symbol
{
    protected Symbol(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Symbol name must be provided.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name;
}

public class TypeSymbol : Symbol
{
    protected TypeSymbol(string name)
        : base(name)
    {
    }

    public static TypeSymbol Integer { get; } = new("INTEGER");

    public static TypeSymbol Long { get; } = new("LONG");

    public static TypeSymbol Double { get; } = new("DOUBLE");

    public static TypeSymbol Decimal { get; } = new("DECIMAL");

    public static TypeSymbol String { get; } = new("STRING");

    public static TypeSymbol Boolean { get; } = new("BOOLEAN");

    public static TypeSymbol File { get; } = new("FILE");

    public static TypeSymbol Void { get; } = new("VOID");

    public static TypeSymbol Error { get; } = new("<error>");

    public static TypeSymbol FromTypeClause(TypeClauseSyntax typeClause)
    {
        ArgumentNullException.ThrowIfNull(typeClause);
        return FromTypeSyntax(typeClause.Type);
    }

    public static TypeSymbol FromTypeSyntax(TypeSyntax typeSyntax)
    {
        ArgumentNullException.ThrowIfNull(typeSyntax);

        return typeSyntax switch
        {
            NamedTypeSyntax namedType => FromSyntaxKind(namedType.TypeToken.Kind),
            FixedLengthStringTypeSyntax => String,
            FunctionTypeSyntax functionType => FunctionTypeSymbol.Create(
                functionType.ParameterTypes.Select(FromTypeSyntax).ToArray(),
                FromTypeSyntax(functionType.ReturnType)),
            ArrayTypeSyntax arrayType => ArrayTypeSymbol.Create(FromTypeSyntax(arrayType.ElementType), arrayType.Rank),
            _ => Error
        };
    }

    public static TypeSymbol FromSyntaxKind(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.IntegerKeyword => Integer,
            SyntaxKind.LongKeyword => Long,
            SyntaxKind.DoubleKeyword => Double,
            SyntaxKind.DecimalKeyword => Decimal,
            SyntaxKind.StringKeyword => String,
            SyntaxKind.BooleanKeyword => Boolean,
            SyntaxKind.FileKeyword => File,
            _ => Error
        };
    }

    public static bool operator ==(TypeSymbol? left, TypeSymbol? right) => Equals(left, right);

    public static bool operator !=(TypeSymbol? left, TypeSymbol? right) => !Equals(left, right);

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }
}

public sealed class ArrayTypeSymbol : TypeSymbol
{
    private ArrayTypeSymbol(TypeSymbol elementType, int rank)
        : base(CreateName(elementType, rank))
    {
        ElementType = elementType;
        Rank = rank;
    }

    public TypeSymbol ElementType { get; }

    public int Rank { get; }

    public static TypeSymbol Create(TypeSymbol elementType, int rank = 1)
    {
        ArgumentNullException.ThrowIfNull(elementType);

        if (rank < 1 || elementType == TypeSymbol.Error || elementType is ArrayTypeSymbol)
        {
            return TypeSymbol.Error;
        }

        return new ArrayTypeSymbol(elementType, rank);
    }

    public override bool Equals(object? obj)
    {
        return obj is ArrayTypeSymbol other
            && ElementType == other.ElementType
            && Rank == other.Rank;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(ArrayTypeSymbol), ElementType, Rank);
    }

    private static string CreateName(TypeSymbol elementType, int rank)
    {
        return $"{elementType.Name}({new string(',', rank - 1)})";
    }
}

public sealed class TypeFieldSymbol : Symbol
{
    public TypeFieldSymbol(
        string name,
        TypeSymbol type,
        int? fixedStringLength = null,
        int? fixedArrayLowerBound = null,
        int? fixedArrayUpperBound = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(type);

        if ((fixedArrayLowerBound is null) != (fixedArrayUpperBound is null))
        {
            throw new ArgumentException("Fixed array bounds must be provided together.");
        }

        if (fixedArrayLowerBound is not null && type is not ArrayTypeSymbol)
        {
            throw new ArgumentException("Fixed array bounds are only valid for array-typed fields.", nameof(type));
        }

        if (fixedStringLength is not null && !SupportsFixedString(type))
        {
            throw new ArgumentException("Fixed string length is only valid for STRING values or arrays of STRING.", nameof(type));
        }

        Type = type;
        FixedStringLength = fixedStringLength;
        FixedArrayLowerBound = fixedArrayLowerBound;
        FixedArrayUpperBound = fixedArrayUpperBound;
    }

    public TypeSymbol Type { get; }

    public int? FixedStringLength { get; }

    public bool IsFixedString => FixedStringLength is not null;

    public bool IsArray => Type is ArrayTypeSymbol;

    public bool IsFixedArray => FixedArrayLowerBound is not null;

    public bool IsDynamicArray => IsArray && !IsFixedArray;

    public int? FixedArrayLowerBound { get; }

    public int? FixedArrayUpperBound { get; }

    private static bool SupportsFixedString(TypeSymbol type)
    {
        return type == TypeSymbol.String
            || type is ArrayTypeSymbol { ElementType: var elementType } && elementType == TypeSymbol.String;
    }
}

public sealed class UserDefinedTypeSymbol : TypeSymbol
{
    private readonly Dictionary<string, TypeFieldSymbol> _fieldsByName = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<TypeFieldSymbol> _fields = Array.Empty<TypeFieldSymbol>();

    public UserDefinedTypeSymbol(string name)
        : base(name)
    {
    }

    public IReadOnlyList<TypeFieldSymbol> Fields => _fields;

    public bool IsCompleted { get; private set; }

    public void Complete(IReadOnlyList<TypeFieldSymbol> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (IsCompleted)
        {
            throw new InvalidOperationException($"User-defined type '{Name}' is already completed.");
        }

        _fields = fields;

        foreach (var field in fields)
        {
            _fieldsByName[field.Name] = field;
        }

        IsCompleted = true;
    }

    public bool TryGetField(string name, out TypeFieldSymbol? field)
    {
        return _fieldsByName.TryGetValue(name, out field);
    }
}

public sealed class FunctionTypeSymbol : TypeSymbol
{
    private FunctionTypeSymbol(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
        : base(CreateName(parameterTypes, returnType))
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }

    public IReadOnlyList<TypeSymbol> ParameterTypes { get; }

    public TypeSymbol ReturnType { get; }

    public static FunctionTypeSymbol Create(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
    {
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(returnType);
        return new FunctionTypeSymbol(parameterTypes, returnType);
    }

    public static FunctionTypeSymbol FromFunction(FunctionSymbol function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return Create(function.Parameters.Select(static parameter => parameter.Type).ToArray(), function.ReturnType);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionTypeSymbol other)
        {
            return false;
        }

        if (ReturnType != other.ReturnType || ParameterTypes.Count != other.ParameterTypes.Count)
        {
            return false;
        }

        for (var index = 0; index < ParameterTypes.Count; index++)
        {
            if (ParameterTypes[index] != other.ParameterTypes[index])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ReturnType);

        foreach (var parameterType in ParameterTypes)
        {
            hash.Add(parameterType);
        }

        return hash.ToHashCode();
    }

    private static string CreateName(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
    {
        var parameterList = string.Join(", ", parameterTypes.Select(static parameterType => parameterType.Name));
        return $"FUNCTION({parameterList}) AS {returnType.Name}";
    }
}

public class VariableSymbol : Symbol
{
    public VariableSymbol(string name, TypeSymbol type, bool isReadOnly, bool isGlobal, int? fixedStringLength = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (fixedStringLength is not null && !SupportsFixedString(type))
        {
            throw new ArgumentException("Fixed string length is only valid for STRING values or arrays of STRING.", nameof(type));
        }

        Type = type;
        IsReadOnly = isReadOnly;
        IsGlobal = isGlobal;
        FixedStringLength = fixedStringLength;
    }

    public TypeSymbol Type { get; }

    public bool IsReadOnly { get; }

    public bool IsGlobal { get; }

    public int? FixedStringLength { get; }

    public bool IsFixedString => FixedStringLength is not null;

    public bool IsArray => Type is ArrayTypeSymbol;

    private static bool SupportsFixedString(TypeSymbol type)
    {
        return type == TypeSymbol.String
            || type is ArrayTypeSymbol { ElementType: var elementType } && elementType == TypeSymbol.String;
    }
}

public sealed class ParameterSymbol : VariableSymbol
{
    public ParameterSymbol(string name, TypeSymbol type, int ordinal)
        : base(name, type, isReadOnly: false, isGlobal: false)
    {
        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Parameter ordinal must be non-negative.");
        }

        Ordinal = ordinal;
    }

    public int Ordinal { get; }
}

public sealed class FunctionSymbol : Symbol
{
    public FunctionSymbol(
        string name,
        IReadOnlyList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        FunctionSymbol? containingRoutine = null,
        bool isBuiltIn = false,
        BuiltinFunctionId? builtInId = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(returnType);

        if (isBuiltIn && builtInId is null)
        {
            throw new ArgumentException("Built-in functions must provide a canonical built-in id.", nameof(builtInId));
        }

        if (!isBuiltIn && builtInId is not null)
        {
            throw new ArgumentException("Only built-in functions can provide a built-in id.", nameof(builtInId));
        }

        Parameters = parameters;
        ReturnType = returnType;
        ContainingRoutine = containingRoutine;
        IsBuiltIn = isBuiltIn;
        BuiltinId = builtInId;
    }

    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    public TypeSymbol ReturnType { get; }

    public FunctionSymbol? ContainingRoutine { get; }

    public bool IsBuiltIn { get; }

    public BuiltinFunctionId? BuiltinId { get; }

    public bool IsSub => ReturnType == TypeSymbol.Void;

    public bool IsLocal => ContainingRoutine is not null;
}
