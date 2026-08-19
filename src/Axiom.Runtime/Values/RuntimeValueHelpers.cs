using Axiom.Semantics;

namespace Axiom.Runtime;

public static class RuntimeValueHelpers
{
    public static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        if (left.Type == TypeSymbol.Integer)
        {
            if (right.Type == TypeSymbol.Integer)
                return left.AsInteger() == right.AsInteger();
            if (right.Type == TypeSymbol.Long)
                return left.AsInteger() == right.AsLong();
            if (right.Type == TypeSymbol.Double)
                return left.AsInteger() == right.AsDouble();
            if (right.Type == TypeSymbol.Decimal)
                return left.AsInteger() == right.AsDecimal();
            return false;
        }

        if (left.Type == TypeSymbol.Long)
        {
            if (right.Type == TypeSymbol.Long)
                return left.AsLong() == right.AsLong();
            if (right.Type == TypeSymbol.Integer)
                return left.AsLong() == right.AsInteger();
            if (right.Type == TypeSymbol.Double)
                return left.AsLong() == right.AsDouble();
            if (right.Type == TypeSymbol.Decimal)
                return left.AsLong() == right.AsDecimal();
            return false;
        }

        if (left.Type == TypeSymbol.Double)
        {
            if (right.Type == TypeSymbol.Double)
                return left.AsDouble() == right.AsDouble();
            if (right.Type == TypeSymbol.Integer)
                return left.AsDouble() == right.AsInteger();
            if (right.Type == TypeSymbol.Long)
                return left.AsDouble() == right.AsLong();
            if (right.Type == TypeSymbol.Decimal)
                return (decimal)left.AsDouble() == right.AsDecimal();
            return false;
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            if (right.Type == TypeSymbol.Decimal)
                return left.AsDecimal() == right.AsDecimal();
            if (right.Type == TypeSymbol.Integer)
                return left.AsDecimal() == right.AsInteger();
            if (right.Type == TypeSymbol.Long)
                return left.AsDecimal() == right.AsLong();
            if (right.Type == TypeSymbol.Double)
                return left.AsDecimal() == (decimal)right.AsDouble();
            return false;
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
            if (right.Type == TypeSymbol.Integer)
                return left.AsInteger().CompareTo(right.AsInteger());
            if (right.Type == TypeSymbol.Long)
                return ((long)left.AsInteger()).CompareTo(right.AsLong());
            if (right.Type == TypeSymbol.Double)
                return ((double)left.AsInteger()).CompareTo(right.AsDouble());
            if (right.Type == TypeSymbol.Decimal)
                return ((decimal)left.AsInteger()).CompareTo(right.AsDecimal());
        }

        if (left.Type == TypeSymbol.Long)
        {
            if (right.Type == TypeSymbol.Long)
                return left.AsLong().CompareTo(right.AsLong());
            if (right.Type == TypeSymbol.Integer)
                return left.AsLong().CompareTo((long)right.AsInteger());
            if (right.Type == TypeSymbol.Double)
                return ((double)left.AsLong()).CompareTo(right.AsDouble());
            if (right.Type == TypeSymbol.Decimal)
                return ((decimal)left.AsLong()).CompareTo(right.AsDecimal());
        }

        if (left.Type == TypeSymbol.Double)
        {
            if (right.Type == TypeSymbol.Double)
                return left.AsDouble().CompareTo(right.AsDouble());
            if (right.Type == TypeSymbol.Integer)
                return left.AsDouble().CompareTo((double)right.AsInteger());
            if (right.Type == TypeSymbol.Long)
                return left.AsDouble().CompareTo((double)right.AsLong());
            if (right.Type == TypeSymbol.Decimal)
                return ((decimal)left.AsDouble()).CompareTo(right.AsDecimal());
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            if (right.Type == TypeSymbol.Decimal)
                return left.AsDecimal().CompareTo(right.AsDecimal());
            if (right.Type == TypeSymbol.Integer)
                return left.AsDecimal().CompareTo((decimal)right.AsInteger());
            if (right.Type == TypeSymbol.Long)
                return left.AsDecimal().CompareTo((decimal)right.AsLong());
            if (right.Type == TypeSymbol.Double)
                return left.AsDecimal().CompareTo((decimal)right.AsDouble());
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
