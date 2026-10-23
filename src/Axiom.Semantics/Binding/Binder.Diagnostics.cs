using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private void ReportUnknownName(string name, TextLocation location)
    {
        if (_currentMethodOwner is not null
            && _currentMethodOwner.TryGetField(name, out _))
        {
            // NIT-003: внутри метода TYPE поле доступно только через SELF —
            // даём специализированную диагностику вместо общего «не объявлено».
            _diagnostics.Report(
                DiagnosticCodes.FieldRequiresSelf,
                $"Поле '{name}' типа '{_currentMethodOwner.Name}' доступно только через SELF: напишите SELF.{name}.",
                location);
            return;
        }

        var message = $"Имя '{name}' не объявлено.";

        if (string.Equals(name, "CALL", StringComparison.OrdinalIgnoreCase))
        {
            // NIT-005: у Axiom нет оператора CALL — подсказываем мигрантам из BASIC.
            message += " Оператор CALL в Axiom отсутствует: для вызова процедуры просто напишите Foo().";
        }

        if (TryReportPrivateModuleMember(name, location))
        {
            return;
        }

        _diagnostics.Report(DiagnosticCodes.UnknownName, message, location);
    }

    private void ReportDuplicateDeclaration(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.DuplicateDeclaration, $"Имя '{name}' уже объявлено в этой области видимости.", location);
    }

    private void ReportShadowedDeclaration(string name, TextLocation location, string? context)
    {
        var normalizedContext = string.IsNullOrWhiteSpace(context) ? "этом месте" : context;
        _diagnostics.Report(
            DiagnosticCodes.ShadowedDeclaration,
            $"Имя '{name}' уже объявлено во внешней области видимости. В {normalizedContext} нельзя объявить его повторно.",
            location);
    }

    private void ReportMissingVariableType(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.MissingVariableType,
            $"Для переменной '{name}' в DIM нужно указать тип через AS или инициализатор для вывода типа.",
            location);
    }

    private void ReportConstantOutOfRange(TypeSymbol expectedType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.ConstantOutOfRange,
            $"Константное числовое значение выходит за диапазон {expectedType.Name}.",
            location);
    }

    private void ReportTypeMismatch(TypeSymbol actualType, TypeSymbol expectedType, TextLocation location, string? note = null)
    {
        var message = $"Несовместимые типы: ожидался {expectedType.Name}, получен {actualType.Name}.";
        if (!string.IsNullOrWhiteSpace(note))
        {
            message += $" {note}";
        }

        _diagnostics.Report(DiagnosticCodes.TypeMismatch, message, location);
    }

    /// <summary>
    /// Сообщение одинаково для присваивания, аргумента, RETURN, сравнения, поля и
    /// элемента массива: читатель должен видеть один договор, а не набор частных
    /// запретов. Явные DATEVALUE и TIMEVALUE названы прямо в тексте.
    /// </summary>
    private void ReportLossyTemporalConversion(TypeSymbol targetType, TextLocation location)
    {
        var lostComponent = targetType == TypeSymbol.Date ? "time" : "date";
        var explicitFunction = targetType == TypeSymbol.Date ? "DATEVALUE" : "TIMEVALUE";
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Cannot convert a full date-time value to '{targetType.Name}': the {lostComponent} component would be truncated. Use {explicitFunction} to drop it explicitly.",
            location);
    }

    private void ReportUnsupportedScreenColorSwitch(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, "Параметр colorSwitch в SCREEN сейчас не поддерживается; используйте пустое место перед activePage.", location);
    }

    private void ReportScalarAssignedToArray(string targetName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Переменная '{targetName}' объявлена как массив. Нельзя присвоить ей одно скалярное значение целиком: укажите индекс элемента или присвойте справа другой массив.",
            location);
    }

    private void ReportArrayAssignedToScalar(string targetName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Переменная '{targetName}' не является массивом. Нельзя присвоить ей массив целиком.",
            location);
    }

    private void ReportWrongArgumentCount(string name, int expectedCount, int actualCount, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.WrongArgumentCount, $"Неверное число аргументов у '{name}': ожидалось {expectedCount}, получено {actualCount}.", location);
    }

    private void ReportWrongArgumentCount(string name, IReadOnlyList<int> expectedCounts, int actualCount, TextLocation location)
    {
        ArgumentNullException.ThrowIfNull(expectedCounts);

        var distinctCounts = expectedCounts
            .Distinct()
            .OrderBy(static count => count)
            .ToArray();

        if (distinctCounts.Length == 0)
        {
            ReportWrongArgumentCount(name, 0, actualCount, location);
            return;
        }

        if (distinctCounts.Length == 1)
        {
            ReportWrongArgumentCount(name, distinctCounts[0], actualCount, location);
            return;
        }

        var expectedText = string.Join(" или ", distinctCounts);
        _diagnostics.Report(
            DiagnosticCodes.WrongArgumentCount,
            $"Неверное число аргументов у '{name}': ожидалось {expectedText}, получено {actualCount}.",
            location);
    }

    private void ReportAmbiguousTypeConstructorCall(string typeName, IReadOnlyList<FunctionSymbol> constructors, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(constructors);

        var constructorList = string.Join(
            ", ",
            constructors.Select(static constructor =>
            {
                var parameters = constructor.Parameters
                    .Skip(1)
                    .Select(static parameter => parameter.IsByRef
                        ? $"BYREF {parameter.Type.Name}"
                        : parameter.Type.Name);
                return $"SUB New({string.Join(", ", parameters)})";
            }));

        _diagnostics.Report(
            DiagnosticCodes.AmbiguousTypeConstructor,
            $"Вызов '{typeName}(...)' неоднозначен: подходят несколько конструкторов: {constructorList}.",
            location);
    }

    private void ReportAmbiguousTypeMethodCall(string typeName, string methodName, IReadOnlyList<FunctionSymbol> methods, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(methods);

        var methodList = string.Join(
            ", ",
            methods.Select(static method =>
            {
                var parameters = method.Parameters
                    .Skip(1)
                    .Select(static parameter => parameter.IsByRef
                        ? $"BYREF {parameter.Type.Name}"
                        : parameter.Type.Name);
                var prefix = method.IsSub ? "SUB" : "FUNCTION";
                return $"{prefix} {method.ShortName}({string.Join(", ", parameters)})";
            }));

        _diagnostics.Report(
            DiagnosticCodes.AmbiguousTypeConstructor,
            $"Вызов '{typeName}.{methodName}(...)' неоднозначен: подходят несколько перегруженных методов: {methodList}.",
            location);
    }

    private void ReportExpectedArrayType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, $"Несовместимые типы: ожидался массив, получен {actualType.Name}.", location);
    }

    private void ReportOneDimensionalArrayRequired(string name, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Форма '{name}' пока поддерживает только одномерные массивы, а получен массив ранга {actualRank}.",
            location);
    }

    private void ReportExpectedArrayRank(string name, int expectedRank, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Форма '{name}' требует массив ранга {expectedRank}, а получен массив ранга {actualRank}.",
            location);
    }

    private void ReportNumericArrayElementRequired(string name, TypeSymbol actualElementType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Форма '{name}' требует числовой массив, а получен массив с элементами типа {actualElementType.Name}.",
            location);
    }

    private void ReportArrayDimensionArgumentRequired(string name, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Форма '{name}' для массива ранга {actualRank} требует явно указать аргумент dimension.",
            location);
    }

    private void ReportBinarySearchRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Функция 'ARRAYBINARYSEARCH' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportArrayToSortedRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Функция 'ARRAYTOSORTED' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportArraySortRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Функция 'ARRAYSORT' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportArrayAggregateRequiresSupportedArray(string functionName, TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Функция '{functionName}' не поддерживает элементы типа {actualType.Name}.",
            location);
    }

    private void ReportArrayConcatRankMismatch(int leftRank, int rightRank, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Ранг правого массива ARRAYCONCAT ({rightRank}) не совпадает с рангом левого ({leftRank}).",
            location);
    }

    private void ReportArrayInitializerNotSupported(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Массив '{name}' с заданными границами нельзя инициализировать значением при объявлении. Используйте пустые скобки или присваивание элементов.",
            location);
    }

    private void ReportArrayNameRequiresBrackets(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Массив '{name}' в квадратном синтаксисе нужно записывать с суффиксом [] или индексами в [].",
            location);
    }

    private void ReportArrayParenthesesForbidden(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Обращение к массиву '{name}' через круглые скобки запрещено; используйте [] или индексы в квадратных скобках.",
            location);
    }

    private void ReportArrayTypeSuffixForbidden(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            "В строгом квадратном синтаксисе суффикс массива после AS должен быть записан после имени: используйте имя[] AS тип.",
            location);
    }

    private void ReportAnonymousArrayTypeRequiresBrackets(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            "Массивный тип без имени записывается с квадратными скобками: тип[] или тип[,], а не с круглыми скобками.",
            location);
    }

    private void ReportArrayAssignmentRequiresFullIndices(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            "Присваивание элемента массива требует полных индексов без среза; для всего массива используйте [].",
            location);
    }

    private void ReportUnknownCallable(string name, TextLocation location)
    {
        if (TryReportPrivateModuleMember(name, location))
        {
            return;
        }

        _diagnostics.Report(DiagnosticCodes.UnknownCallable, $"Функция или процедура '{name}' не объявлена.", location);
    }

    private void ReportProcedureCannotBeUsedAsValue(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.ProcedureValue, $"Процедуру '{name}' нельзя использовать как значение.", location);
    }

    private void ReportExpressionStatementHasNoEffect(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.ExpressionStatementHasNoEffect,
            "Оператор-выражение не имеет эффекта: выражение не вызывает процедуру или функцию. Если это вызов, добавьте скобки, например FLUSH(f).",
            location);
    }

    private void ReportInvalidOnKeyHandler(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidOnKeyHandler,
            $"В ON KEY нужно указать имя процедуры SUB без параметров или с одним параметром STRING, а не '{name}'.",
            location);
    }

    private void ReportInvalidOnMouseHandler(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidOnMouseHandler,
            $"В ON MOUSE нужно указать имя процедуры SUB без параметров или с параметрами INTEGER, LONG, LONG, а не '{name}'.",
            location);
    }

    private void ReportInvalidOnMouseEvent(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidOnMouseEvent,
            "Цель ON MOUSE должна быть номером кнопки, номером кнопки с событием \"UP\"/\"DOWN\" либо событием \"MOVE\" или \"WHEEL\".",
            location);
    }

    private void ReportInvalidPaintFillType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Аргумент 'краска' у PAINT должен иметь числовой или строковый тип, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBorderType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Аргумент 'граница' у PAINT должен иметь числовой тип, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBackgroundType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Аргумент 'фон' у PAINT должен иметь тип STRING, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBackgroundWithoutPattern(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            "Аргумент 'фон' у PAINT допустим только вместе со строковой формой аргумента 'краска'.",
            location);
    }

    private void ReportValueIsNotCallable(TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.ValueNotCallable, $"Значение типа {type.Name} нельзя вызывать как функцию.", location);
    }

    private void ReportReturnValueInSub(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.ReturnValueInSub, $"В SUB '{name}' нельзя возвращать значение.", location);
    }

    private void ReportMissingReturnValue(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.MissingReturnValue, $"В FUNCTION '{name}' требуется RETURN со значением.", location);
    }

    private void ReportInvalidCondition(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidCondition, $"Условие должно иметь тип BOOLEAN, получен {actualType.Name}.", location);
    }

    private void ReportInvalidForControlVariable(string name, TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidForControl,
            $"Управляющая переменная FOR '{name}' должна иметь тип INTEGER, LONG или DOUBLE, получен {type.Name}. Счётчик FOR всегда локальный: используйте числовые границы и не объявляйте переменную с именем счётчика заранее.",
            location);
    }

    private void ReportAssignToConst(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.AssignToConst, $"Нельзя присваивать значение константе '{name}'.", location);
    }

    private void ReportInvalidFileAssignment(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, "Значения типа FILE нельзя присваивать напрямую. Для них используйте OPEN и CLOSE.", location);
    }

    private void ReportUnknownType(string name, TextLocation location)
    {
        if (TryReportPrivateModuleMember(name, location))
        {
            return;
        }

        _diagnostics.Report(DiagnosticCodes.UnknownType, $"Тип '{name}' не объявлен.", location);
    }

    private void ReportInvalidTypeMember(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeMember,
            $"Поле '{name}' в TYPE может иметь только скалярный встроенный тип или другой пользовательский TYPE.",
            location);
    }

    private void ReportInvalidFixedTypeArrayBounds(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeMember,
            $"Поле '{name}' в TYPE объявлено с недопустимыми границами массива. {details}",
            location);
    }

    private void ReportCyclicType(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeMember,
            $"TYPE '{name}' не может прямо или косвенно содержать поле своего же типа.",
            location);
    }

    private void ReportInvalidTypeConstructorDeclaration(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeMember,
            "Конструктор TYPE должен объявляться только в форме `SUB New(...) ... END SUB` без типа результата.",
            location);
    }

    private void ReportInvalidTypeInvariantDeclaration(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeMember,
            "Инвариант TYPE должен объявляться только в форме `FUNCTION IsValid() AS BOOLEAN ... END FUNCTION` без пользовательских параметров.",
            location);
    }

    private void ReportUnknownField(TypeSymbol type, string fieldName, TextLocation location)
    {
        var message = $"Тип {type.Name} не содержит поле '{fieldName}'.";

        if (type is UserDefinedTypeSymbol userDefinedType)
        {
            // NIT-003: подсказываем ближайшее похожее поле — типичная опечатка.
            var closest = userDefinedType.Fields
                .Select(static field => field.Name)
                .Where(candidate => !string.Equals(candidate, fieldName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(candidate => LevenshteinDistance(candidate, fieldName))
                .ThenBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (closest is not null)
            {
                message += $" Возможно, вы имели в виду поле '{closest}'.";
            }
        }

        _diagnostics.Report(DiagnosticCodes.UnknownField, message, location);
    }

    private void ReportNameClassCollision(string name, TextLocation location)
    {
        _diagnostics.Report(
            new Diagnostic(
                DiagnosticCodes.NameClassCollision,
                $"Имя '{name}' используется и как переменная, и как процедура (SUB/FUNCTION). Проверьте, не опечатка ли это.",
                location,
                DiagnosticSeverity.Warning));
    }

    private bool TryReportFunctionReferenceNote(BoundExpression expression, TypeSymbol expectedType, TextLocation location)
    {
        // NIT-001: имя одновременно обозначает переменную и функцию —
        // в выражении победила переменная; подсказываем про ссылку на функцию.
        if (expectedType is FunctionTypeSymbol
            && expression is BoundVariableExpression boundVariable
            && _scope.TryLookupFunction(boundVariable.Variable.Name, out var collidingFunction)
            && collidingFunction is not null
            && !collidingFunction.IsBuiltIn)
        {
            ReportTypeMismatch(
                expression.Type,
                expectedType,
                location,
                $"Возможно, вы хотели присвоить ссылку на функцию '{boundVariable.Variable.Name}'.");
            return true;
        }

        return false;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return string.IsNullOrEmpty(right) ? 0 : right.Length;
        }

        if (string.IsNullOrEmpty(right))
        {
            return left.Length;
        }

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= right.Length; column++)
            {
                var cost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private void ReportInvalidByRefArgument(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.InvalidByRefArgument,
            "Аргумент параметра BYREF должен быть простой переменной того же типа без промежуточного преобразования.",
            location);
    }

    private void ReportUnsupportedByRefCallableValue(string name, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.UnsupportedByRefCallableValue,
            $"Функцию '{name}' с параметрами BYREF пока нельзя использовать как значение, callback или переменную FUNCTION.",
            location);
    }

    private void ReportInvalidFieldAccess(TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidFieldAccess, $"Доступ к полям через точку возможен только для TYPE, получен {type.Name}.", location);
    }

    private void ReportNamedArgumentsOnlySupportedForTypeLiterals(TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.WrongArgumentCount,
            "Форма `name := value` сейчас поддержана только в именованных литералах TYPE.",
            location);
    }

    private void ReportTypeLiteralRequiresNamedFields(string typeName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.WrongArgumentCount,
            $"Литерал TYPE '{typeName}' принимает только именованные поля в форме `Name := value`.",
            location);
    }

    private void ReportDuplicateTypeLiteralField(string typeName, string fieldName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.DuplicateDeclaration,
            $"Литерал TYPE '{typeName}' повторно задаёт поле '{fieldName}'.",
            location);
    }

    private void ReportMixedTypeLiteralInitialization(string typeName, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.MixedTypeLiteralInitialization,
            $"Литерал TYPE '{typeName}' не допускает смешивания позиционных и именованных аргументов в одном вызове `TypeName(...)`.",
            location);
    }

    private void ReportTypeLiteralPositionalArity(string typeName, int expectedCount, int actualCount, TextLocation location)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeLiteralPositionalArity,
            $"Позиционная форма TYPE '{typeName}' требует {expectedCount} аргумент(а/ов) по числу полей, получено {actualCount}.",
            location);
    }

    private void ReportFixedTypeArrayFieldOperation(string keyword, string displayName, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        _diagnostics.Report(
            DiagnosticCodes.FixedTypeArrayFieldOperation,
            $"Оператор {keyword} нельзя применять к фиксированному полю-массиву '{displayName}' в TYPE.",
            location);
    }

    private void ReportInvalidFixedStringDeclaration(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            DiagnosticCodes.InvalidFixedStringDeclaration,
            $"Объявление '{name}' использует недопустимую форму фиксированной строки. {details}",
            location);
    }

    private void ReportInvalidTypeFieldDefaultValue(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            DiagnosticCodes.InvalidTypeFieldDefaultValue,
            $"Поле '{name}' в TYPE использует недопустимое значение по умолчанию. {details}",
            location);
    }
    private void ReportInvalidArrayAccess(string name, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidArrayAccess, $"Имя '{name}' не является массивом.", location);
    }

    private void ReportInvalidArrayBoundsForm(string name, TextLocation location, string details)
    {
        _diagnostics.Report(
            DiagnosticCodes.TypeMismatch,
            $"Объявление массива '{name}' использует недопустимую форму измерений. {details}",
            location);
    }

    private void ReportInvalidReturnContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidReturnContext, "RETURN допустим только внутри SUB или FUNCTION.", location);
    }

    private void ReportInvalidExitForContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidExitForContext, "EXIT FOR допустим только внутри FOR ... NEXT.", location);
    }

    private void ReportInvalidExitDoContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidExitDoContext, "EXIT DO допустим только внутри DO ... LOOP.", location);
    }

    private void ReportInvalidContinueForContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidContinueForContext, "CONTINUE FOR допустим только внутри FOR ... NEXT.", location);
    }

    private void ReportInvalidContinueWhileContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidContinueWhileContext, "CONTINUE WHILE допустим только внутри WHILE ... END WHILE.", location);
    }

    private void ReportInvalidContinueDoContext(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidContinueDoContext, "CONTINUE DO допустим только внутри DO ... LOOP.", location);
    }

    private void ReportInvalidSelectCaseRange(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.InvalidSelectCaseRange, "Диапазон CASE ... TO допустим только для числовых, строковых или временных значений одного вида.", location);
    }

    private void ReportInvalidRangeExpression(TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, "Диапазон TO допустим только внутри индексирования массива.", location);
    }

    private BoundErrorExpression ReportInvalidUnaryOperator(SyntaxKind operatorKind, TypeSymbol operandType, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, $"Оператор '{GetOperatorText(operatorKind)}' неприменим к типу {operandType.Name}.", location);
        return new BoundErrorExpression(location);
    }

    private BoundErrorExpression ReportInvalidBinaryOperator(SyntaxKind operatorKind, TypeSymbol leftType, TypeSymbol rightType, TextLocation location)
    {
        _diagnostics.Report(DiagnosticCodes.TypeMismatch, $"Оператор '{GetOperatorText(operatorKind)}' неприменим к типам {leftType.Name} и {rightType.Name}.", location);
        return new BoundErrorExpression(location);
    }
}
