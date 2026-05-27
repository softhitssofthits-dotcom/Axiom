using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private void ReportUnknownName(string name, TextLocation location)
    {
        _diagnostics.Report(UnknownNameCode, $"Имя '{name}' не объявлено.", location);
    }

    private void ReportDuplicateDeclaration(string name, TextLocation location)
    {
        _diagnostics.Report(DuplicateDeclarationCode, $"Имя '{name}' уже объявлено в этой области видимости.", location);
    }

    private void ReportShadowedDeclaration(string name, TextLocation location, string? context)
    {
        var normalizedContext = string.IsNullOrWhiteSpace(context) ? "этом месте" : context;
        _diagnostics.Report(
            ShadowedDeclarationCode,
            $"Имя '{name}' уже объявлено во внешней области видимости. В {normalizedContext} нельзя объявить его повторно.",
            location);
    }

    private void ReportMissingVariableType(string name, TextLocation location)
    {
        _diagnostics.Report(
            MissingVariableTypeCode,
            $"Для переменной '{name}' в DIM нужно указать тип через AS или инициализатор для вывода типа.",
            location);
    }

    private void ReportConstantOutOfRange(TypeSymbol expectedType, TextLocation location)
    {
        _diagnostics.Report(
            ConstantOutOfRangeCode,
            $"Константное числовое значение выходит за диапазон {expectedType.Name}.",
            location);
    }

    private void ReportTypeMismatch(TypeSymbol actualType, TypeSymbol expectedType, TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, $"Несовместимые типы: ожидался {expectedType.Name}, получен {actualType.Name}.", location);
    }

    private void ReportScalarAssignedToArray(string targetName, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Переменная '{targetName}' объявлена как массив. Нельзя присвоить ей одно скалярное значение целиком: укажите индекс элемента или присвойте справа другой массив.",
            location);
    }

    private void ReportArrayAssignedToScalar(string targetName, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Переменная '{targetName}' не является массивом. Нельзя присвоить ей массив целиком.",
            location);
    }

    private void ReportWrongArgumentCount(string name, int expectedCount, int actualCount, TextLocation location)
    {
        _diagnostics.Report(WrongArgumentCountCode, $"Неверное число аргументов у '{name}': ожидалось {expectedCount}, получено {actualCount}.", location);
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
            WrongArgumentCountCode,
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
            AmbiguousTypeConstructorCode,
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
            AmbiguousTypeConstructorCode,
            $"Вызов '{typeName}.{methodName}(...)' неоднозначен: подходят несколько перегруженных методов: {methodList}.",
            location);
    }

    private void ReportExpectedArrayType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, $"Несовместимые типы: ожидался массив, получен {actualType.Name}.", location);
    }

    private void ReportOneDimensionalArrayRequired(string name, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Форма '{name}' пока поддерживает только одномерные массивы, а получен массив ранга {actualRank}.",
            location);
    }

    private void ReportExpectedArrayRank(string name, int expectedRank, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Форма '{name}' требует массив ранга {expectedRank}, а получен массив ранга {actualRank}.",
            location);
    }

    private void ReportNumericArrayElementRequired(string name, TypeSymbol actualElementType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Форма '{name}' требует числовой массив, а получен массив с элементами типа {actualElementType.Name}.",
            location);
    }

    private void ReportArrayDimensionArgumentRequired(string name, int actualRank, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Форма '{name}' для массива ранга {actualRank} требует явно указать аргумент dimension.",
            location);
    }

    private void ReportBinarySearchRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Функция 'ARRAYBINARYSEARCH' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportArrayToSortedRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Функция 'ARRAYTOSORTED' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportArraySortRequiresOrderedArray(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Функция 'ARRAYSORT' поддерживает только массивы с числовыми или строковыми элементами, получен {actualType.Name}.",
            location);
    }

    private void ReportUnknownCallable(string name, TextLocation location)
    {
        _diagnostics.Report(UnknownCallableCode, $"Функция или процедура '{name}' не объявлена.", location);
    }

    private void ReportProcedureCannotBeUsedAsValue(string name, TextLocation location)
    {
        _diagnostics.Report(ProcedureValueCode, $"Процедуру '{name}' нельзя использовать как значение.", location);
    }

    private void ReportInvalidOnKeyHandler(string name, TextLocation location)
    {
        _diagnostics.Report(
            InvalidOnKeyHandlerCode,
            $"В ON KEY нужно указать имя процедуры SUB без параметров или с одним параметром STRING, а не '{name}'.",
            location);
    }

    private void ReportInvalidOnMouseHandler(string name, TextLocation location)
    {
        _diagnostics.Report(
            InvalidOnMouseHandlerCode,
            $"В ON MOUSE нужно указать имя процедуры SUB без параметров или с параметрами INTEGER, LONG, LONG, а не '{name}'.",
            location);
    }

    private void ReportInvalidOnMouseEvent(TextLocation location)
    {
        _diagnostics.Report(
            InvalidOnMouseEventCode,
            "Цель ON MOUSE должна быть номером кнопки, номером кнопки с событием \"UP\"/\"DOWN\" либо событием \"MOVE\" или \"WHEEL\".",
            location);
    }

    private void ReportInvalidPaintFillType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Аргумент 'краска' у PAINT должен иметь числовой или строковый тип, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBorderType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Аргумент 'граница' у PAINT должен иметь числовой тип, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBackgroundType(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Аргумент 'фон' у PAINT должен иметь тип STRING, получен {actualType.Name}.",
            location);
    }

    private void ReportInvalidPaintBackgroundWithoutPattern(TextLocation location)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            "Аргумент 'фон' у PAINT допустим только вместе со строковой формой аргумента 'краска'.",
            location);
    }

    private void ReportValueIsNotCallable(TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(ValueNotCallableCode, $"Значение типа {type.Name} нельзя вызывать как функцию.", location);
    }

    private void ReportReturnValueInSub(string name, TextLocation location)
    {
        _diagnostics.Report(ReturnValueInSubCode, $"В SUB '{name}' нельзя возвращать значение.", location);
    }

    private void ReportMissingReturnValue(string name, TextLocation location)
    {
        _diagnostics.Report(MissingReturnValueCode, $"В FUNCTION '{name}' требуется RETURN со значением.", location);
    }

    private void ReportInvalidCondition(TypeSymbol actualType, TextLocation location)
    {
        _diagnostics.Report(InvalidConditionCode, $"Условие должно иметь тип BOOLEAN, получен {actualType.Name}.", location);
    }

    private void ReportInvalidForControlVariable(string name, TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(InvalidForControlCode, $"Управляющая переменная FOR '{name}' должна иметь тип INTEGER, LONG или DOUBLE, получен {type.Name}.", location);
    }

    private void ReportAssignToConst(string name, TextLocation location)
    {
        _diagnostics.Report(AssignToConstCode, $"Нельзя присваивать значение константе '{name}'.", location);
    }

    private void ReportInvalidFileAssignment(TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, "Значения типа FILE нельзя присваивать напрямую. Для них используйте OPEN и CLOSE.", location);
    }

    private void ReportUnknownType(string name, TextLocation location)
    {
        _diagnostics.Report(UnknownTypeCode, $"Тип '{name}' не объявлен.", location);
    }

    private void ReportInvalidTypeMember(string name, TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMemberCode,
            $"Поле '{name}' в TYPE может иметь только скалярный встроенный тип или другой пользовательский TYPE.",
            location);
    }

    private void ReportTypeFieldMultidimensionalArrayNotSupported(string name, TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMemberCode,
            $"Поле '{name}' в TYPE пока поддерживает только одномерную массивную форму.",
            location);
    }

    private void ReportInvalidFixedTypeArrayBounds(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            InvalidTypeMemberCode,
            $"Поле '{name}' в TYPE объявлено с недопустимыми границами массива. {details}",
            location);
    }

    private void ReportCyclicType(string name, TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMemberCode,
            $"TYPE '{name}' не может прямо или косвенно содержать поле своего же типа.",
            location);
    }

    private void ReportInvalidTypeConstructorDeclaration(TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMemberCode,
            "Конструктор TYPE должен объявляться только в форме `SUB New(...) ... END SUB` без типа результата.",
            location);
    }

    private void ReportInvalidTypeInvariantDeclaration(TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMemberCode,
            "Инвариант TYPE должен объявляться только в форме `FUNCTION IsValid() AS BOOLEAN ... END FUNCTION` без пользовательских параметров.",
            location);
    }

    private void ReportUnknownField(TypeSymbol type, string fieldName, TextLocation location)
    {
        _diagnostics.Report(UnknownFieldCode, $"Тип {type.Name} не содержит поле '{fieldName}'.", location);
    }

    private void ReportInvalidByRefArgument(TextLocation location)
    {
        _diagnostics.Report(
            InvalidByRefArgumentCode,
            "Аргумент параметра BYREF должен быть простой переменной того же типа без промежуточного преобразования.",
            location);
    }

    private void ReportUnsupportedByRefCallableValue(string name, TextLocation location)
    {
        _diagnostics.Report(
            UnsupportedByRefCallableValueCode,
            $"Функцию '{name}' с параметрами BYREF пока нельзя использовать как значение, callback или переменную FUNCTION.",
            location);
    }

    private void ReportInvalidFieldAccess(TypeSymbol type, TextLocation location)
    {
        _diagnostics.Report(InvalidFieldAccessCode, $"Доступ к полям через точку возможен только для TYPE, получен {type.Name}.", location);
    }

    private void ReportNamedArgumentsOnlySupportedForTypeLiterals(TextLocation location)
    {
        _diagnostics.Report(
            WrongArgumentCountCode,
            "Форма `name := value` сейчас поддержана только в именованных литералах TYPE.",
            location);
    }

    private void ReportTypeLiteralRequiresNamedFields(string typeName, TextLocation location)
    {
        _diagnostics.Report(
            WrongArgumentCountCode,
            $"Литерал TYPE '{typeName}' принимает только именованные поля в форме `Name := value`.",
            location);
    }

    private void ReportDuplicateTypeLiteralField(string typeName, string fieldName, TextLocation location)
    {
        _diagnostics.Report(
            DuplicateDeclarationCode,
            $"Литерал TYPE '{typeName}' повторно задаёт поле '{fieldName}'.",
            location);
    }

    private void ReportMixedTypeLiteralInitialization(string typeName, TextLocation location)
    {
        _diagnostics.Report(
            MixedTypeLiteralInitializationCode,
            $"Литерал TYPE '{typeName}' не допускает смешивания позиционных и именованных аргументов в одном вызове `TypeName(...)`.",
            location);
    }

    private void ReportTypeLiteralPositionalArity(string typeName, int expectedCount, int actualCount, TextLocation location)
    {
        _diagnostics.Report(
            TypeLiteralPositionalArityCode,
            $"Позиционная форма TYPE '{typeName}' требует {expectedCount} аргумент(а/ов) по числу полей, получено {actualCount}.",
            location);
    }

    private void ReportFixedTypeArrayFieldOperation(string keyword, string displayName, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        _diagnostics.Report(
            FixedTypeArrayFieldOperationCode,
            $"Оператор {keyword} нельзя применять к фиксированному полю-массиву '{displayName}' в TYPE.",
            location);
    }

    private void ReportInvalidFixedStringDeclaration(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            InvalidFixedStringDeclarationCode,
            $"Объявление '{name}' использует недопустимую форму фиксированной строки. {details}",
            location);
    }

    private void ReportInvalidTypeFieldDefaultValue(string name, TextLocation location, string details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        _diagnostics.Report(
            InvalidTypeFieldDefaultValueCode,
            $"Поле '{name}' в TYPE использует недопустимое значение по умолчанию. {details}",
            location);
    }
    private void ReportInvalidArrayAccess(string name, TextLocation location)
    {
        _diagnostics.Report(InvalidArrayAccessCode, $"Имя '{name}' не является массивом.", location);
    }

    private void ReportInvalidArrayBoundsForm(string name, TextLocation location, string details)
    {
        _diagnostics.Report(
            TypeMismatchCode,
            $"Объявление массива '{name}' использует недопустимую форму измерений. {details}",
            location);
    }

    private void ReportInvalidReturnContext(TextLocation location)
    {
        _diagnostics.Report(InvalidReturnContextCode, "RETURN допустим только внутри SUB или FUNCTION.", location);
    }

    private void ReportInvalidExitForContext(TextLocation location)
    {
        _diagnostics.Report(InvalidExitForContextCode, "EXIT FOR допустим только внутри FOR ... NEXT.", location);
    }

    private void ReportInvalidExitDoContext(TextLocation location)
    {
        _diagnostics.Report(InvalidExitDoContextCode, "EXIT DO допустим только внутри DO ... LOOP.", location);
    }

    private void ReportInvalidSelectCaseRange(TextLocation location)
    {
        _diagnostics.Report(InvalidSelectCaseRangeCode, "Диапазон CASE ... TO допустим только для числовых или строковых значений.", location);
    }

    private void ReportInvalidRangeExpression(TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, "Диапазон TO допустим только внутри индексирования массива.", location);
    }

    private BoundErrorExpression ReportInvalidUnaryOperator(SyntaxKind operatorKind, TypeSymbol operandType, TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, $"Оператор '{GetOperatorText(operatorKind)}' неприменим к типу {operandType.Name}.", location);
        return new BoundErrorExpression(location);
    }

    private BoundErrorExpression ReportInvalidBinaryOperator(SyntaxKind operatorKind, TypeSymbol leftType, TypeSymbol rightType, TextLocation location)
    {
        _diagnostics.Report(TypeMismatchCode, $"Оператор '{GetOperatorText(operatorKind)}' неприменим к типам {leftType.Name} и {rightType.Name}.", location);
        return new BoundErrorExpression(location);
    }
    private void ReportInvalidTypeMethodReceiver(string methodName, TextLocation location)
    {
        _diagnostics.Report(
            InvalidTypeMethodReceiverCode,
            $"В текущем шаге метод '{methodName}' можно вызывать только у устойчивого получателя TYPE: переменной, поля записи или элемента массива. Временные значения и результаты выражений пока не поддерживаются.",
            location);
    }
}
