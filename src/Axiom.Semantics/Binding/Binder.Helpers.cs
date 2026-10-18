using Axiom.Core;
namespace Axiom.Semantics;

public sealed partial class Binder
{
    private static bool CanConvert(TypeSymbol fromType, TypeSymbol toType)
    {
        // Временные переходы, теряющие календарную дату или время суток, здесь
        // отсутствуют намеренно: их выполняет только явное извлечение части.
        return (fromType == TypeSymbol.Integer && toType == TypeSymbol.Long)
            || (fromType == TypeSymbol.Integer && toType == TypeSymbol.Decimal)
            || (fromType == TypeSymbol.Integer && toType == TypeSymbol.Double)
            || (fromType == TypeSymbol.Long && toType == TypeSymbol.Decimal)
            || (fromType == TypeSymbol.Long && toType == TypeSymbol.Double)
            || (fromType == TypeSymbol.Decimal && toType == TypeSymbol.Double)
            || (fromType == TypeSymbol.Date && toType == TypeSymbol.String)
            || (fromType == TypeSymbol.Time && toType == TypeSymbol.String)
            || (fromType == TypeSymbol.DateTime && toType == TypeSymbol.String)
            || (fromType == TypeSymbol.String && toType == TypeSymbol.Date)
            || (fromType == TypeSymbol.String && toType == TypeSymbol.Time)
            || (fromType == TypeSymbol.String && toType == TypeSymbol.DateTime)
            || (fromType == TypeSymbol.Date && toType == TypeSymbol.DateTime)
            || (fromType == TypeSymbol.Time && toType == TypeSymbol.DateTime);
    }

    private static bool IsNumeric(TypeSymbol type)
    {
        return type == TypeSymbol.Integer || type == TypeSymbol.Long || type == TypeSymbol.Double || type == TypeSymbol.Decimal;
    }

    private static bool IsIntegral(TypeSymbol type)
    {
        return type == TypeSymbol.Integer || type == TypeSymbol.Long;
    }

    private static bool CanConcatenateAsString(TypeSymbol type)
    {
        return type == TypeSymbol.String
            || type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal
            || type == TypeSymbol.Boolean
            || type == TypeSymbol.Date
            || type == TypeSymbol.Time
            || type == TypeSymbol.DateTime;
    }

    private static bool SupportsOrderedComparison(TypeSymbol type)
    {
        return IsNumeric(type) || type == TypeSymbol.String
            || type == TypeSymbol.Date || type == TypeSymbol.Time || type == TypeSymbol.DateTime;
    }

    private static bool RequiresNumericAggregateArray(BuiltinFunctionId id, TypeSymbol elementType)
    {
        return id is BuiltinFunctionId.ArraySum or BuiltinFunctionId.ArrayAverage
            && !IsNumeric(elementType);
    }

    private static bool RequiresOrderedAggregateArray(BuiltinFunctionId id, TypeSymbol elementType)
    {
        // ARRAYMIN/ARRAYMAX используют то же порядковое сравнение, что и обычные
        // операторы, поэтому временные элементы допустимы без перевода в строки.
        return id is BuiltinFunctionId.ArrayMin or BuiltinFunctionId.ArrayMax
            && !IsNumeric(elementType)
            && elementType != TypeSymbol.String
            && !IsTemporalType(elementType);
    }

    private bool SupportsStructuralEquality(TypeSymbol type)
    {
        return StructuralEqualitySupport.IsSupported(type, _structuralEqualityCache);
    }

    private static bool IsTemporalType(TypeSymbol type)
    {
        return type == TypeSymbol.Date || type == TypeSymbol.Time || type == TypeSymbol.DateTime;
    }

    private static bool IsTemporalComparisonOperand(TypeSymbol left, TypeSymbol right)
    {
        return (IsTemporalType(left) && (IsTemporalType(right) || right == TypeSymbol.String))
            || (IsTemporalType(right) && (IsTemporalType(left) || left == TypeSymbol.String));
    }

    private static TypeSymbol ResolveTemporalComparisonType(TypeSymbol left, TypeSymbol right)
    {
        if (left == right)
        {
            return left;
        }

        if (left == TypeSymbol.DateTime || right == TypeSymbol.DateTime)
        {
            return TypeSymbol.DateTime;
        }

        if (left == TypeSymbol.String)
        {
            return right;
        }

        if (right == TypeSymbol.String)
        {
            return left;
        }

        return TypeSymbol.Error;
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
            DiagnosticCodes.TypeMismatch,
            $"Функция 'ARRAYMAP' не может строить массив с элементами типа {actualType.Name}. Вложенные массивы не поддерживаются.",
            location);
    }
}
