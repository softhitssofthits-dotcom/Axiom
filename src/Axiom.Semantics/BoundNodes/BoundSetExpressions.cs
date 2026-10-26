using Axiom.Core;

namespace Axiom.Semantics;

/// <summary>
/// Свойство количества элементов <c>found.Count</c>. Скобки у свойства не
/// пишутся: они отличают вызов метода от чтения свойства.
/// </summary>
public sealed class BoundSetCountExpression : BoundExpression
{
    public BoundSetCountExpression(
        TextLocation location,
        BoundExpression target,
        SetTypeSymbol setType,
        string displayName)
        : base(location, TypeSymbol.Integer)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(setType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Target = target;
        SetType = setType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SetCountExpression;

    public BoundExpression Target { get; }

    public SetTypeSymbol SetType { get; }

    public string DisplayName { get; }
}

public enum BoundSetMethodKind
{
    Add,
    Contains,
    Remove,
    ToArray,
    UnionWith,
    IntersectWith
}

/// <summary>
/// Вызов метода множества. Add и Contains возвращают BOOLEAN, Remove сообщает
/// признак удаления без ошибки на отсутствующем элементе, ToArray возвращает
/// независимый массив элементов, UnionWith и IntersectWith изменяют получателя
/// и значения не возвращают.
/// </summary>
public sealed class BoundSetMethodCallExpression : BoundExpression
{
    public BoundSetMethodCallExpression(
        TextLocation location,
        BoundSetMethodKind method,
        BoundExpression target,
        BoundExpression? value,
        SetTypeSymbol setType,
        TypeSymbol resultType,
        string displayName)
        : base(location, resultType)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(setType);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Method = method;
        Target = target;
        Value = value;
        SetType = setType;
        ResultType = resultType;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SetMethodCallExpression;

    public BoundSetMethodKind Method { get; }

    public BoundExpression Target { get; }

    /// <summary>
    /// Аргумент метода: элемент для Add/Contains/Remove, второе множество для
    /// UnionWith/IntersectWith. У ToArray аргумента нет.
    /// </summary>
    public BoundExpression? Value { get; }

    public SetTypeSymbol SetType { get; }

    /// <summary>
    /// Объявленный результат метода. Для UnionWith и IntersectWith это VOID:
    /// значение не возвращается, вызов допустим только оператором.
    /// </summary>
    public TypeSymbol ResultType { get; }

    public string DisplayName { get; }
}
