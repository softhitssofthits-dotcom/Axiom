using Axiom.Core;

namespace Axiom.Semantics;

/// <summary>
/// Чтение значения по ключу: <c>prices["яблоко"]</c>. Отсутствующий ключ
/// сообщает ошибку времени выполнения и не подменяется значением по умолчанию.
/// </summary>
public sealed class BoundDictionaryAccessExpression : BoundExpression
{
    public BoundDictionaryAccessExpression(
        TextLocation location,
        BoundExpression target,
        BoundExpression key,
        DictionaryTypeSymbol dictionaryType,
        string displayName)
        : base(location, dictionaryType.ValueType)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(dictionaryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Target = target;
        Key = key;
        DictionaryType = dictionaryType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DictionaryAccessExpression;

    public BoundExpression Target { get; }

    public BoundExpression Key { get; }

    public DictionaryTypeSymbol DictionaryType { get; }

    public string DisplayName { get; }
}

/// <summary>
/// Свойство количества записей <c>prices.Count</c>. Скобки у свойства не
/// пишутся: они отличают вызов метода от чтения свойства.
/// </summary>
public sealed class BoundDictionaryCountExpression : BoundExpression
{
    public BoundDictionaryCountExpression(
        TextLocation location,
        BoundExpression target,
        DictionaryTypeSymbol dictionaryType,
        string displayName)
        : base(location, TypeSymbol.Integer)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dictionaryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Target = target;
        DictionaryType = dictionaryType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DictionaryCountExpression;

    public BoundExpression Target { get; }

    public DictionaryTypeSymbol DictionaryType { get; }

    public string DisplayName { get; }
}

public enum BoundDictionaryMethodKind
{
    Add,
    Exists,
    Remove,
    Keys
}

/// <summary>
/// Вызов метода словаря. Add сообщает ошибку на повторном ключе, Exists
/// возвращает BOOLEAN, Remove возвращает признак удаления без ошибки на
/// отсутствующем ключе, Keys возвращает самостоятельный массив ключей.
/// </summary>
public sealed class BoundDictionaryMethodCallExpression : BoundExpression
{
    public BoundDictionaryMethodCallExpression(
        TextLocation location,
        BoundDictionaryMethodKind method,
        BoundExpression target,
        BoundExpression? key,
        BoundExpression? value,
        DictionaryTypeSymbol dictionaryType,
        TypeSymbol resultType,
        string displayName)
        : base(location, resultType)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dictionaryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Method = method;
        Target = target;
        Key = key;
        Value = value;
        DictionaryType = dictionaryType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DictionaryMethodCallExpression;

    public BoundDictionaryMethodKind Method { get; }

    public BoundExpression Target { get; }

    public BoundExpression? Key { get; }

    public BoundExpression? Value { get; }

    public DictionaryTypeSymbol DictionaryType { get; }

    public string DisplayName { get; }
}

/// <summary>
/// Поиск без исключения <c>TryGetValue(key, variable)</c>. Результат BOOLEAN
/// отличает найденную нулевую цену от отсутствующей записи; при отсутствии
/// ключа переменная получает значение по умолчанию своего типа.
/// </summary>
public sealed class BoundDictionaryTryGetValueExpression : BoundExpression
{
    public BoundDictionaryTryGetValueExpression(
        TextLocation location,
        BoundExpression target,
        BoundExpression key,
        BoundExpression destination,
        DictionaryTypeSymbol dictionaryType,
        string displayName)
        : base(location, TypeSymbol.Boolean)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(dictionaryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Target = target;
        Key = key;
        Destination = destination;
        DictionaryType = dictionaryType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DictionaryTryGetValueExpression;

    public BoundExpression Target { get; }

    public BoundExpression Key { get; }

    public BoundExpression Destination { get; }

    public DictionaryTypeSymbol DictionaryType { get; }

    public string DisplayName { get; }
}
