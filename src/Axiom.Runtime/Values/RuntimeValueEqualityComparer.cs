using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Сравнение значений массива для хеш-множества. Элементы одного массива
/// имеют объявленный тип элемента, поэтому хеш строится по фактическому типу
/// значения и согласован с <see cref="RuntimeValueHelpers.ValuesEqual"/>.
/// </summary>
internal sealed class RuntimeValueEqualityComparer : IEqualityComparer<RuntimeValue>
{
    public static RuntimeValueEqualityComparer Instance { get; } = new();

    public static bool SupportsElementType(TypeSymbol elementType)
    {
        return elementType == TypeSymbol.Integer
            || elementType == TypeSymbol.Long
            || elementType == TypeSymbol.Double
            || elementType == TypeSymbol.Decimal
            || elementType == TypeSymbol.String
            || elementType == TypeSymbol.Boolean
            || elementType == TypeSymbol.Date
            || elementType == TypeSymbol.Time
            || elementType == TypeSymbol.DateTime
            || elementType == TypeSymbol.UtcDateTime;
    }

    public bool Equals(RuntimeValue? left, RuntimeValue? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return RuntimeValueHelpers.ValuesEqual(left, right);
    }

    public int GetHashCode(RuntimeValue value)
    {
        if (value.Type == TypeSymbol.Integer)
        {
            return value.AsInteger();
        }

        if (value.Type == TypeSymbol.Long)
        {
            return value.AsLong().GetHashCode();
        }

        if (value.Type == TypeSymbol.Double)
        {
            var number = value.AsDouble();
            return number == 0 ? 0 : number.GetHashCode();
        }

        if (value.Type == TypeSymbol.Decimal)
        {
            return value.AsDecimal().GetHashCode();
        }

        if (value.Type == TypeSymbol.String)
        {
            return StringComparer.Ordinal.GetHashCode(value.AsString());
        }

        if (value.Type == TypeSymbol.Boolean)
        {
            return value.AsBoolean().GetHashCode();
        }

        if (value.Type == TypeSymbol.Date)
        {
            return value.AsDate().GetHashCode();
        }

        if (value.Type == TypeSymbol.Time)
        {
            return value.AsTime().GetHashCode();
        }

        if (value.Type == TypeSymbol.DateTime)
        {
            return value.AsDateTime().GetHashCode();
        }

        if (value.Type == TypeSymbol.UtcDateTime)
        {
            return value.AsUtcDateTime().GetHashCode();
        }

        return 0;
    }
}
