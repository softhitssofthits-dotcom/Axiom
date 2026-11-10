using Axiom.Core;

namespace Axiom.Semantics;

/// <summary>
/// Элемент конструктора набора операции IN: отдельное значение или диапазон
/// TO с включёнными границами. Строится по образцу селекторов SELECT CASE.
/// </summary>
public abstract class BoundInItem
{
    protected BoundInItem(TextLocation location)
    {
        Location = location;
    }

    public TextLocation Location { get; }
}

public sealed class BoundInValueItem : BoundInItem
{
    public BoundInValueItem(TextLocation location, BoundExpression value)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    public BoundExpression Value { get; }
}

public sealed class BoundInRangeItem : BoundInItem
{
    public BoundInRangeItem(TextLocation location, BoundExpression lowerBound, BoundExpression upperBound)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(upperBound);

        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public BoundExpression LowerBound { get; }

    public BoundExpression UpperBound { get; }
}

/// <summary>
/// Проверка вхождения <c>a In [1 To 4, 6 To 9]</c>. Значение вычисляется
/// один раз, элементы проверяются слева направо с ранним выходом. Формы
/// <c>a In множество</c> и <c>a In массив</c> связываются в готовые узлы
/// вызова и сюда не попадают.
/// </summary>
public sealed class BoundInExpression : BoundExpression
{
    public BoundInExpression(
        TextLocation location,
        BoundExpression value,
        IReadOnlyList<BoundInItem> items)
        : base(location, TypeSymbol.Boolean)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(items);

        Value = value;
        Items = items;
    }

    public override BoundNodeKind Kind => BoundNodeKind.InExpression;

    public BoundExpression Value { get; }

    public IReadOnlyList<BoundInItem> Items { get; }
}
