using Axiom.Core;

namespace Axiom.Semantics;

/// <summary>
/// Единый договор временных преобразований, общий для связывания и валидатора
/// байткода. Неявная потеря календарной даты или времени суток запрещена везде,
/// кроме явного извлечения части: YEAR, HOUR и подобные функции существуют именно
/// для того, чтобы отбросить ненужный компонент.
/// </summary>
public static class TemporalConversionRules
{
    public static bool IsPartExtractionBuiltin(BuiltinFunctionId id)
    {
        return id is BuiltinFunctionId.Year
            or BuiltinFunctionId.Month
            or BuiltinFunctionId.Day
            or BuiltinFunctionId.Weekday
            or BuiltinFunctionId.Hour
            or BuiltinFunctionId.Minute
            or BuiltinFunctionId.Second
            or BuiltinFunctionId.StartOfMonth
            or BuiltinFunctionId.EndOfMonth
            or BuiltinFunctionId.StartOfYear
            or BuiltinFunctionId.EndOfYear;
    }

    /// <summary>
    /// Функция извлечения части принимает свой временной вид, DATETIME и канонический
    /// текст без промежуточного преобразования. Строка разбирается самой функцией,
    /// поэтому полный текст даты-времени не проходит через запрет усечения.
    /// </summary>
    public static bool AcceptsPartExtractionArgument(TypeSymbol actualType, TypeSymbol parameterType)
    {
        ArgumentNullException.ThrowIfNull(actualType);
        ArgumentNullException.ThrowIfNull(parameterType);

        if (actualType == parameterType || actualType == TypeSymbol.String)
        {
            return true;
        }

        return actualType == TypeSymbol.DateTime
            && (parameterType == TypeSymbol.Date || parameterType == TypeSymbol.Time);
    }

    /// <summary>
    /// Переход теряет компонент, если источник содержит и дату, и время суток,
    /// а цель хранит только одну часть. Строка проверяется отдельно: её содержимое
    /// известно только во время исполнения.
    /// </summary>
    public static bool LosesComponent(TypeSymbol sourceType, TypeSymbol targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(targetType);

        if (sourceType != TypeSymbol.DateTime)
        {
            return false;
        }

        return targetType == TypeSymbol.Date || targetType == TypeSymbol.Time;
    }
}
