using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Сравнение значений массива для хеш-множества. Элементы одного массива
/// имеют объявленный тип элемента, поэтому хеш строится по фактическому типу
/// значения и согласован с <see cref="RuntimeValueHelpers.ValuesEqual"/>.
/// Записи TYPE допускаются, только когда весь граф полей поддерживает
/// структурное равенство; тогда тот же обход полей служит основой хеша.
/// </summary>
internal sealed class RuntimeValueEqualityComparer : IEqualityComparer<RuntimeValue>
{
    public static RuntimeValueEqualityComparer Instance { get; } = new();

    public static bool SupportsElementType(TypeSymbol elementType)
    {
        if (elementType is UserDefinedTypeSymbol userDefined)
        {
            return StructuralEqualitySupport.IsSupported(userDefined);
        }

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
        return RuntimeValueHashing.ComputeHash(value);
    }
}
