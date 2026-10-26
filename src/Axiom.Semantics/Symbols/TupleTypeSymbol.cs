namespace Axiom.Semantics;

/// <summary>Структурный кортеж; имена компонентов не влияют на совместимость.</summary>
public sealed class TupleTypeSymbol : UserDefinedTypeSymbol
{
    public TupleTypeSymbol(IReadOnlyList<TypeSymbol> types, IReadOnlyList<string?> names)
        : this(FormatName(types, names), names)
    {
        Complete(types.Select((type, i) => new TypeFieldSymbol("Item" + (i + 1), type)).ToArray());
    }

    // Двухфазное восстановление графа типов из байт-кода.
    public TupleTypeSymbol(string name, IReadOnlyList<string?> names) : base(name)
    {
        ElementNames = names.ToArray();
    }

    public IReadOnlyList<string?> ElementNames { get; }

    public override bool TryGetField(string name, out TypeFieldSymbol? field)
    {
        for (var i = 0; i < ElementNames.Count; i++)
            if (string.Equals(name, ElementNames[i], StringComparison.OrdinalIgnoreCase))
            { field = Fields[i]; return true; }
        return base.TryGetField(name, out field);
    }

    public override bool Equals(object? obj) => obj is TupleTypeSymbol other &&
        Fields.Count == other.Fields.Count && Fields.Zip(other.Fields).All(p => p.First.Type == p.Second.Type);

    public override int GetHashCode()
    {
        var hash = new HashCode(); hash.Add(typeof(TupleTypeSymbol));
        foreach (var field in Fields) hash.Add(field.Type);
        return hash.ToHashCode();
    }

    private static string FormatName(IReadOnlyList<TypeSymbol> types, IReadOnlyList<string?> names)
        => "(" + string.Join(", ", types.Select((type, i) =>
            (names[i] is { } name ? name + " AS " : "") +
            (type is UserDefinedTypeSymbol record ? record.TypeIdentity : type.Name))) + ")";
}
