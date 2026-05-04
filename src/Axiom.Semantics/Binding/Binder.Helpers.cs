using Axiom.Core;
namespace Axiom.Semantics;

public sealed partial class Binder
{
    private static bool CanConvert(TypeSymbol fromType, TypeSymbol toType)
    {
        return (fromType == TypeSymbol.Integer && toType == TypeSymbol.Long)
            || (fromType == TypeSymbol.Integer && toType == TypeSymbol.Decimal)
            || (fromType == TypeSymbol.Integer && toType == TypeSymbol.Double)
            || (fromType == TypeSymbol.Long && toType == TypeSymbol.Decimal)
            || (fromType == TypeSymbol.Long && toType == TypeSymbol.Double);
    }

    private static bool IsNumeric(TypeSymbol type)
    {
        return type == TypeSymbol.Integer || type == TypeSymbol.Long || type == TypeSymbol.Double || type == TypeSymbol.Decimal;
    }

    private static bool IsIntegral(TypeSymbol type)
    {
        return type == TypeSymbol.Integer || type == TypeSymbol.Long;
    }

    private static bool SupportsOrderedComparison(TypeSymbol type)
    {
        return IsNumeric(type) || type == TypeSymbol.String;
    }

    private static bool SupportsStructuralEquality(TypeSymbol type)
    {
        return type switch
        {
            var scalar when scalar == TypeSymbol.Integer
                || scalar == TypeSymbol.Long
                || scalar == TypeSymbol.Double
                || scalar == TypeSymbol.Decimal
                || scalar == TypeSymbol.String
                || scalar == TypeSymbol.Boolean => true,
            UserDefinedTypeSymbol userDefinedType => userDefinedType.Fields.All(static field => SupportsStructuralEquality(field.Type)),
            _ => false
        };
    }

    private static TypeSymbol PromoteNumericType(TypeSymbol leftType, TypeSymbol rightType, bool forceDouble = false)
    {
        if (forceDouble || leftType == TypeSymbol.Double || rightType == TypeSymbol.Double)
        {
            return TypeSymbol.Double;
        }

        if (leftType == TypeSymbol.Decimal || rightType == TypeSymbol.Decimal)
        {
            return TypeSymbol.Decimal;
        }

        if (leftType == TypeSymbol.Long || rightType == TypeSymbol.Long)
        {
            return TypeSymbol.Long;
        }

        return TypeSymbol.Integer;
    }

    private void ReportArrayMapRejectsNestedArrayResult(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Функция 'ARRAYMAP' не может строить массив с элементами типа {actualType.Name}. Вложенные массивы не поддерживаются.",
            location);
    }
}
