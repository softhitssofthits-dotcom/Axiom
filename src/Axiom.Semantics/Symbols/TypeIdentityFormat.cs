namespace Axiom.Semantics;

/// <summary>
/// Устойчивая идентичность пользовательского TYPE внутри компиляции и снимка
/// байт-кода. Закрытые типы разных модулей могут называться одинаково, поэтому
/// короткого имени недостаточно: идентичность дополняет его именем модуля.
/// Идентичность используется словарями типов, инструкцией CreateRecord,
/// проверкой модуля, VM и сериализатором; <see cref="Symbol.Name"/> остаётся
/// только для сообщений и пользовательского текста.
/// </summary>
public static class TypeIdentityFormat
{
    /// <summary>
    /// Разделитель модуля и имени. Два двоеточия недопустимы в имени файла
    /// Windows, поэтому разделитель не встречается в имени модуля.
    /// </summary>
    public const string Separator = "::";

    public static string Format(string name, string? containingModule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return string.IsNullOrWhiteSpace(containingModule)
            ? name
            : $"{containingModule}{Separator}{name}";
    }

    public static bool TrySplit(string identity, out string name, out string? containingModule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);

        var separatorIndex = identity.LastIndexOf(Separator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex + Separator.Length >= identity.Length)
        {
            name = identity;
            containingModule = null;
            return false;
        }

        name = identity[(separatorIndex + Separator.Length)..];
        containingModule = identity[..separatorIndex];
        return true;
    }

    public static bool Equals(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
