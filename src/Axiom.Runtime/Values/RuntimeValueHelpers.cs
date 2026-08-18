using Axiom.Semantics;

namespace Axiom.Runtime;

public static class RuntimeValueHelpers
{
    public static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        if (left.Type == TypeSymbol.Integer)
        {
            return left.AsInteger() == right.AsInteger();
        }

        if (left.Type == TypeSymbol.Long)
        {
            return left.AsLong() == right.AsLong();
        }

        if (left.Type == TypeSymbol.Double)
        {
            return left.AsDouble() == right.AsDouble();
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            return left.AsDecimal() == right.AsDecimal();
        }

        if (left.Type == TypeSymbol.String)
        {
            return string.Equals(left.AsString(), right.AsString(), StringComparison.Ordinal);
        }

        if (left.Type == TypeSymbol.Boolean)
        {
            return left.AsBoolean() == right.AsBoolean();
        }

        if (left.Type is UserDefinedTypeSymbol)
        {
            return RecordsEqual(left.AsRecord(), right.AsRecord());
        }

        return false;
    }

    public static int Compare(RuntimeValue left, RuntimeValue right)
    {
        if (left.Type == TypeSymbol.Integer)
        {
            return left.AsInteger().CompareTo(right.AsInteger());
        }

        if (left.Type == TypeSymbol.Long)
        {
            return left.AsLong().CompareTo(right.AsLong());
        }

        if (left.Type == TypeSymbol.Double)
        {
            return left.AsDouble().CompareTo(right.AsDouble());
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            return left.AsDecimal().CompareTo(right.AsDecimal());
        }

        if (left.Type == TypeSymbol.String)
        {
            return string.Compare(left.AsString(), right.AsString(), StringComparison.Ordinal);
        }

        throw new InvalidOperationException(
            $"Сравнение не поддерживается для типов {left.Type.Name} и {right.Type.Name}.");
    }

    public static bool RecordsEqual(RuntimeRecordValue left, RuntimeRecordValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!ReferenceEquals(left.Type, right.Type))
        {
            return false;
        }

        foreach (var field in left.Type.Fields)
        {
            if (!ValuesEqual(left.GetField(field), right.GetField(field)))
            {
                return false;
            }
        }

        return true;
    }
}
