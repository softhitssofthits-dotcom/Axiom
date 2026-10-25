using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Структурное хеширование значений массива, согласованное с
/// <see cref="RuntimeValueHelpers.ValuesEqual"/>: равные значения получают
/// одинаковый хеш. Записи TYPE хешируются по полям в порядке объявления,
/// включая вложенные записи; хеш не кешируется, потому что запись изменяема.
/// </summary>
internal static class RuntimeValueHashing
{
    public static int ComputeHash(RuntimeValue value)
    {
        if (value.Type is UserDefinedTypeSymbol)
        {
            return ComputeRecordHash(value.AsRecord());
        }

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

    private static int ComputeRecordHash(RuntimeRecordValue record)
    {
        // Имя типа разделяет одинаковые по полям значения разных TYPE,
        // которые структурное равенство считает разными записями.
        var hash = record.Type.Name.GetHashCode(StringComparison.Ordinal);

        foreach (var field in record.Type.Fields)
        {
            hash = unchecked(hash * 31 + ComputeHash(record.GetField(field)));
        }

        return hash;
    }
}
