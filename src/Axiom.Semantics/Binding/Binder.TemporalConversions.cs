using Axiom.Core;
using Axiom.Syntax;
using System.Globalization;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private bool TryReportLossyTemporalConversion(
        ExpressionSyntax syntax,
        BoundExpression boundExpression,
        TypeSymbol targetType)
    {
        if (targetType != TypeSymbol.Date && targetType != TypeSymbol.Time)
        {
            return false;
        }

        var losesComponent = TemporalConversionRules.LosesComponent(boundExpression.Type, targetType)
            || (boundExpression.Type == TypeSymbol.String
                && TryGetConstantStringText(syntax, out var text)
                && IsCanonicalDateTimeText(text));

        if (!losesComponent)
        {
            return false;
        }

        ReportLossyTemporalConversion(targetType, GetLocation(syntax));
        return true;
    }

    /// <summary>
    /// Скобки, именованный аргумент и вложенность не меняют значения литерала,
    /// поэтому проверка договора видит тот же текст, что и исполнение. Переменная,
    /// CONST и результат вызова проверяются во время исполнения.
    /// </summary>
    private static bool TryGetConstantStringText(ExpressionSyntax expression, out string text)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;

                case NamedArgumentExpressionSyntax namedArgument:
                    expression = namedArgument.Expression;
                    continue;

                case LiteralExpressionSyntax literal
                    when literal.LiteralToken.Kind == SyntaxKind.StringLiteralToken
                        && literal.LiteralToken.Value is string value:
                    text = value;
                    return true;

                default:
                    text = string.Empty;
                    return false;
            }
        }
    }

    /// <summary>
    /// Полный текст даты-времени, включая необязательную дробную часть секунд.
    /// Проверка повторяет канонический разбор исполнения: иначе литерал с долями
    /// секунды проходил бы связывание, а затем отвергался во время выполнения.
    /// </summary>
    private static bool IsCanonicalDateTimeText(string text)
    {
        return text.Length >= 19
            && text[10] == ' '
            && DateOnly.TryParseExact(text.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            && TimeOnly.TryParseExact(text.AsSpan(11), "HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Аргумент функции извлечения части: значение передаётся без преобразования,
    /// а совместимость вида проверяется здесь. Строка разбирается самой функцией.
    /// </summary>
    private BoundExpression ApplyPartExtractionArgument(
        ExpressionSyntax argument,
        BoundExpression boundArgument,
        TypeSymbol parameterType)
    {
        if (boundArgument.Type == TypeSymbol.Error
            || TemporalConversionRules.AcceptsPartExtractionArgument(boundArgument.Type, parameterType))
        {
            return boundArgument;
        }

        ReportTypeMismatch(boundArgument.Type, parameterType, GetLocation(argument));
        return new BoundErrorExpression(GetLocation(argument));
    }
}
