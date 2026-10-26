namespace Axiom.Semantics;

/// <summary>
/// Общие правила типов элементов коллекций: какие типы могут быть ключом
/// словаря или элементом множества и какие ресурсы в коллекции не хранятся.
/// Правила вынесены из <see cref="DictionaryTypeSymbol"/>, чтобы словарь и
/// множество не расходились в допустимом наборе типов.
/// </summary>
public static class CollectionKeyTypeSupport
{
    /// <summary>
    /// Допустимый ключ: STRING, INTEGER или LONG. Числовые значения разных
    /// типов не смешиваются в одной коллекции.
    /// </summary>
    public static bool IsSupportedKeyType(TypeSymbol keyType)
    {
        ArgumentNullException.ThrowIfNull(keyType);
        return keyType == TypeSymbol.String || keyType == TypeSymbol.Integer || keyType == TypeSymbol.Long;
    }

    /// <summary>
    /// Графические и файловые ресурсы в коллекции не хранятся: их копирование
    /// и владение не описаны договором коллекции.
    /// </summary>
    public static bool IsResourceType(TypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type == TypeSymbol.File
            || type == TypeSymbol.Image
            || type == TypeSymbol.Sprite
            || type == TypeSymbol.Tilemap
            || type == TypeSymbol.Tileset;
    }
}
