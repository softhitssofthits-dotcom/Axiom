using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Точный ключ коллекции в приведённом виде. Строки сравниваются точно, с
/// учётом регистра, без удаления пробелов и нормализации Unicode; числа
/// сравниваются по своему типу, поэтому INTEGER и LONG не смешиваются.
/// Один общий ключ используется словарём и множеством, чтобы правила
/// равенства и хеширования не расходились между коллекциями.
/// </summary>
internal readonly struct RuntimeCollectionKey : IEquatable<RuntimeCollectionKey>
{
    private RuntimeCollectionKey(string? text, int integer, long @long, TypeSymbol keyType)
    {
        Text = text;
        Integer = integer;
        Long = @long;
        KeyType = keyType;
    }

    public string? Text { get; }

    public int Integer { get; }

    public long Long { get; }

    public TypeSymbol KeyType { get; }

    public static RuntimeCollectionKey Create(RuntimeValue value, TypeSymbol keyType)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(keyType);

        if (keyType == TypeSymbol.String)
        {
            return new RuntimeCollectionKey(value.AsString(), 0, 0L, keyType);
        }

        if (keyType == TypeSymbol.Integer)
        {
            return new RuntimeCollectionKey(null, value.AsInteger(), 0L, keyType);
        }

        return new RuntimeCollectionKey(null, 0, value.AsLong(), keyType);
    }

    public RuntimeValue ToRuntimeValue(TypeSymbol keyType)
    {
        if (keyType == TypeSymbol.String)
        {
            return RuntimeValue.FromString(Text ?? string.Empty);
        }

        return keyType == TypeSymbol.Integer
            ? RuntimeValue.FromInteger(Integer)
            : RuntimeValue.FromLong(Long);
    }

    public bool Equals(RuntimeCollectionKey other)
    {
        if (!ReferenceEquals(KeyType, other.KeyType))
        {
            return false;
        }

        if (KeyType == TypeSymbol.String)
        {
            return string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        return KeyType == TypeSymbol.Integer
            ? Integer == other.Integer
            : Long == other.Long;
    }

    public override bool Equals(object? obj) => obj is RuntimeCollectionKey other && Equals(other);

    public override int GetHashCode()
    {
        if (KeyType == TypeSymbol.String)
        {
            return StringComparer.Ordinal.GetHashCode(Text ?? string.Empty);
        }

        return KeyType == TypeSymbol.Integer
            ? Integer
            : Long.GetHashCode();
    }
}
