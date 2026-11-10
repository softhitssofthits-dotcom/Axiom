using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    /// <summary>
    /// Связывает операцию IN. Правый операнд — конструктор набора в квадратных
    /// скобках, множество HASHSET или массив; обычное выражение справа
    /// отвергается. Разбор связывается здесь целиком, поэтому общий путь
    /// BindBinaryExpression не вызывается.
    /// </summary>
    private BoundExpression BindInExpression(BinaryExpressionSyntax expression)
    {
        var value = BindExpression(expression.Left);

        if (expression.Right is SetConstructorExpressionSyntax constructor)
        {
            return BindInConstructorExpression(expression, value, constructor);
        }

        var right = BindExpression(expression.Right);

        if (value.Type == TypeSymbol.Error || right.Type == TypeSymbol.Error)
        {
            return new BoundErrorExpression(expression.OperatorToken.Location);
        }

        if (right.Type is SetTypeSymbol setType)
        {
            // Форма множества — то же связывание, что у метода Contains:
            // значение приводится к типу элемента множества.
            var element = BindExpression(expression.Left, setType.ElementType);

            if (element.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression(expression.OperatorToken.Location);
            }

            return new BoundSetMethodCallExpression(
                expression.OperatorToken.Location,
                BoundSetMethodKind.Contains,
                right,
                element,
                setType,
                TypeSymbol.Boolean,
                GetArrayTargetDisplayName(right));
        }

        if (right.Type is DictionaryTypeSymbol dictionaryType)
        {
            // Форма словаря — то же связывание, что у метода Exists: значение
            // приводится к объявленному типу ключа, проверяются ключи,
            // а не значения словаря.
            var key = BindExpression(expression.Left, dictionaryType.KeyType);

            if (key.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression(expression.OperatorToken.Location);
            }

            return new BoundDictionaryMethodCallExpression(
                expression.OperatorToken.Location,
                BoundDictionaryMethodKind.Exists,
                right,
                key,
                value: null,
                dictionaryType,
                TypeSymbol.Boolean,
                GetArrayTargetDisplayName(right));
        }

        if (right.Type is ArrayTypeSymbol)
        {
            return BindInArrayExpression(expression);
        }

        return ReportInvalidBinaryOperator(SyntaxKind.InKeyword, value.Type, right.Type, expression.OperatorToken.Location);
    }

    /// <summary>
    /// Форма массива — синтезированный вызов ARRAYCONTAINS(массив, значение):
    /// сохраняются и семантика встроенной функции, и её диагностики, включая
    /// приведение значения к типу элемента массива.
    /// </summary>
    private BoundExpression BindInArrayExpression(BinaryExpressionSyntax expression)
    {
        var arrayContains = BuiltinFunctions.All.FirstOrDefault(static function =>
            function.BuiltinId == BuiltinFunctionId.ArrayContains);

        if (arrayContains is null)
        {
            return ReportInvalidBinaryOperator(
                SyntaxKind.InKeyword,
                TypeSymbol.Error,
                TypeSymbol.Error,
                expression.OperatorToken.Location);
        }

        var location = expression.OperatorToken.Location;
        var synthesizedCall = new CallExpressionSyntax(
            expression.Right,
            new SyntaxToken(SyntaxKind.OpenParenToken, "(", location),
            [expression.Right, expression.Left],
            [new SyntaxToken(SyntaxKind.CommaToken, ",", location)],
            new SyntaxToken(SyntaxKind.CloseParenToken, ")", location));

        return BindInvocationExpression(
            new BoundFunctionExpression(location, arrayContains),
            synthesizedCall,
            location,
            arrayContains.Name);
    }

    private BoundExpression BindInConstructorExpression(
        BinaryExpressionSyntax expression,
        BoundExpression value,
        SetConstructorExpressionSyntax constructor)
    {
        var location = expression.OperatorToken.Location;

        if (value.Type == TypeSymbol.Error)
        {
            BindSetConstructorItemsForDiagnostics(constructor);
            return new BoundErrorExpression(location);
        }

        if (constructor.IsEmpty)
        {
            ReportEmptyInSet(constructor.OpenBracketToken.Location);
            return new BoundErrorExpression(location);
        }

        // Типизация и нормализация элементов повторяют селекторы SELECT CASE:
        // значения сравниваются по правилам равенства, границы диапазонов — по
        // правилам упорядочивания, и приводятся к типу проверяемого значения.
        var valueType = value.Type;
        var items = new List<BoundInItem>(constructor.Items.Count);
        var hasErrors = false;

        foreach (var item in constructor.Items)
        {
            if (item is RangeExpressionSyntax range)
            {
                var lowerBound = BindExpression(range.LowerBound);
                var upperBound = BindExpression(range.UpperBound);

                if (CanBindSelectCaseOrdering(valueType, lowerBound.Type)
                    && CanBindSelectCaseOrdering(valueType, upperBound.Type))
                {
                    items.Add(new BoundInRangeItem(
                        range.ToKeyword.Location,
                        NormalizeSelectCaseSelectorExpression(range.LowerBound, lowerBound, valueType, valueType),
                        NormalizeSelectCaseSelectorExpression(range.UpperBound, upperBound, valueType, valueType)));
                    continue;
                }

                if (lowerBound.Type != TypeSymbol.Error && upperBound.Type != TypeSymbol.Error)
                {
                    ReportInvalidInElement(range.ToKeyword.Location, valueType, isRange: true);
                }

                hasErrors = true;
                continue;
            }

            var boundItem = BindExpression(item);

            if (CanBindInValueItem(valueType, boundItem.Type))
            {
                items.Add(new BoundInValueItem(
                    GetLocation(item),
                    NormalizeSelectCaseSelectorExpression(item, boundItem, valueType, valueType)));
                continue;
            }

            if (boundItem.Type != TypeSymbol.Error)
            {
                ReportInvalidInElement(GetLocation(item), valueType, isRange: false);
            }

            hasErrors = true;
        }

        if (hasErrors)
        {
            return new BoundErrorExpression(location);
        }

        return new BoundInExpression(location, value, items);
    }

    // Записи допускаются как отдельные значения по правилам структурного
    // равенства; диапазоны для них недоступны, как и в сравнениях.
    private bool CanBindInValueItem(TypeSymbol valueType, TypeSymbol itemType)
    {
        if (CanBindSelectCaseEquality(valueType, itemType))
        {
            return true;
        }

        return valueType == itemType
            && valueType is UserDefinedTypeSymbol userDefinedType
            && SupportsStructuralEquality(userDefinedType);
    }

    /// <summary>
    /// Конструктор набора без IN недостижим от разбора и связывается только
    /// для защиты от синтаксических деревьев, построенных в обход парсера.
    /// </summary>
    private BoundExpression BindStandaloneSetConstructorExpression(SetConstructorExpressionSyntax expression)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidSetConstructorUse,
            "Конструктор набора в квадратных скобках допустим только справа от операции IN.",
            expression.OpenBracketToken.Location);

        BindSetConstructorItemsForDiagnostics(expression);
        return new BoundErrorExpression(expression.OpenBracketToken.Location);
    }

    private void BindSetConstructorItemsForDiagnostics(SetConstructorExpressionSyntax expression)
    {
        foreach (var item in expression.Items)
        {
            if (item is RangeExpressionSyntax range)
            {
                BindExpression(range.LowerBound);
                BindExpression(range.UpperBound);
            }
            else
            {
                BindExpression(item);
            }
        }
    }

    private void ReportEmptyInSet(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.EmptyInSet,
            "Конструктор набора IN не может быть пустым: укажите хотя бы одно значение или диапазон.",
            location);
    }

    private void ReportInvalidInElement(TextLocation location, TypeSymbol valueType, bool isRange)
    {
        var elementKind = isRange ? "Диапазон" : "Значение";
        _diagnostics.Report(
            DiagnosticCodes.InvalidInElement,
            $"{elementKind} в наборе IN несовместим по типу со значением {valueType.Name}: допустимы числа, строки и временные значения одного вида.",
            location);
    }
}
