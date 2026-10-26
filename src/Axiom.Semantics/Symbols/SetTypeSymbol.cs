namespace Axiom.Semantics;

/// <summary>
/// Тип типизированного множества уникальных значений. Тип элемента входит в
/// идентичность типа, поэтому HASHSET(INTEGER) и HASHSET(LONG) несовместимы.
/// </summary>
public sealed class SetTypeSymbol : TypeSymbol
{
    private SetTypeSymbol(TypeSymbol elementType)
        : base(CreateName(elementType))
    {
        ElementType = elementType;
    }

    /// <summary>
    /// Тип элемента: STRING, INTEGER или LONG. Другие типы в первом выпуске
    /// элементами множества не являются.
    /// </summary>
    public TypeSymbol ElementType { get; }

    public static TypeSymbol Create(TypeSymbol elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);

        if (!CollectionKeyTypeSupport.IsSupportedKeyType(elementType))
        {
            return Error;
        }

        return new SetTypeSymbol(elementType);
    }

    public override bool Equals(object? obj)
    {
        return obj is SetTypeSymbol other
            && ElementType == other.ElementType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(SetTypeSymbol), ElementType);
    }

    private static string CreateName(TypeSymbol elementType)
    {
        return $"HASHSET({elementType.Name})";
    }
}
