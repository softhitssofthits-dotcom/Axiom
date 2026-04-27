using System.Globalization;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static readonly TextLocation FallbackRuntimeLocation = new(new TextSpan(0, 0), 1, 1);

    private RuntimeValue GetVariableValue(VariableSymbol variable)
    {
        var storedValue = LookupStorage(variable);

        if (storedValue is RuntimeValue scalar)
        {
            return scalar;
        }

        if (storedValue is RuntimeArrayValue array && variable.Type is ArrayTypeSymbol arrayType)
        {
            return RuntimeValue.FromArray(arrayType, array);
        }

        return ReportInvalidRuntimeState<RuntimeValue>(
            $"Имя '{variable.Name}' ссылается на массив и не может использоваться как скалярное значение.");
    }

    private RuntimeArrayValue GetArrayValue(VariableSymbol variable, TextLocation? location = null)
    {
        var storedValue = LookupStorage(variable);

        if (storedValue is RuntimeArrayValue array)
        {
            if (!array.IsAllocated)
            {
                return ReportInvalidRuntimeState<RuntimeArrayValue>(
                    $"Массив '{variable.Name}' объявлен, но ещё не выделен.",
                    location);
            }

            return array;
        }

        return ReportInvalidRuntimeState<RuntimeArrayValue>(
            $"Имя '{variable.Name}' не содержит массива времени выполнения.");
    }

    private RuntimeArrayValue GetAllocatedArray(RuntimeValue value, string displayName, TextLocation? location = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (value.Type is not ArrayTypeSymbol)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Значение '{displayName}' не содержит массива времени выполнения.");
        }

        var array = value.AsArray();

        if (!array.IsAllocated)
        {
            return ReportInvalidRuntimeState<RuntimeArrayValue>(
                $"Массив '{displayName}' объявлен, но ещё не выделен.",
                location);
        }

        return array;
    }

    private IRuntimeStoredValue LookupStorage(VariableSymbol variable)
    {
        if (CurrentScope.TryLookup(variable, out var storedValue) && storedValue is not null)
        {
            return storedValue;
        }

        return ReportInvalidRuntimeState<IRuntimeStoredValue>(
            $"Для имени '{variable.Name}' отсутствует хранилище времени выполнения.");
    }

    private void AssignVariable(VariableSymbol variable, RuntimeValue value)
    {
        if (CurrentScope.TryLookup(variable, out var storedValue) && storedValue is RuntimeArrayValue)
        {
            if (value.Type is ArrayTypeSymbol)
            {
                var storedArray = variable.FixedStringLength is { } fixedStringLength
                    ? value.AsArray().Clone(fixedStringLength)
                    : value.AsArray();

                if (!CurrentScope.TryAssign(variable, storedArray))
                {
                    ReportInvalidRuntimeState(
                        $"Не удалось присвоить значение имени '{variable.Name}'.");
                    throw new RuntimeFailureException();
                }

                return;
            }

            ReportInvalidRuntimeState(
                $"Нельзя присвоить скалярное значение массиву '{variable.Name}' без индекса.");
            throw new RuntimeFailureException();
        }

        if (!CurrentScope.TryAssign(variable, value.CopyForStorage(variable.FixedStringLength)))
        {
            ReportInvalidRuntimeState(
                $"Не удалось присвоить значение имени '{variable.Name}'.");
            throw new RuntimeFailureException();
        }
    }

    private void AssignArrayElement(BoundArrayAccessExpression target, RuntimeValue value)
    {
        var array = GetAllocatedArray(EvaluateExpression(target.Target), target.DisplayName, target.Location);
        var indices = EvaluateArrayIndices(target.Indices, "Значение индекса массива");

        if (!array.ContainsIndices(indices))
        {
            ReportArrayIndicesOutOfBounds(target.DisplayName, indices, array, target.Location);
            throw new RuntimeFailureException();
        }

        array.SetValue(indices, value);
    }

    private int[] EvaluateArrayIndices(IReadOnlyList<BoundExpression> indexExpressions, string detailPrefix)
    {
        ArgumentNullException.ThrowIfNull(indexExpressions);
        ArgumentException.ThrowIfNullOrWhiteSpace(detailPrefix);

        var indices = new int[indexExpressions.Count];

        for (var index = 0; index < indexExpressions.Count; index++)
        {
            var expression = indexExpressions[index];
            var detail = indexExpressions.Count == 1
                ? detailPrefix
                : $"{detailPrefix} #{index + 1}";
            indices[index] = ToArrayBound(EvaluateExpression(expression), expression.Location, detail);
        }

        return indices;
    }

    private void AssignRecordField(BoundFieldAccessExpression target, RuntimeValue value)
    {
        var record = EvaluateExpression(target.Target).AsRecord();
        record.SetField(target.Field, value);
    }

    private RuntimeValue? TryParseInput(TypeSymbol type, string text)
    {
        if (type == TypeSymbol.Integer)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue)
                ? RuntimeValue.FromInteger(integerValue)
                : null;
        }

        if (type == TypeSymbol.Long)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue)
                ? RuntimeValue.FromLong(longValue)
                : null;
        }

        if (type == TypeSymbol.Double)
        {
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var doubleValue)
                   && double.IsFinite(doubleValue)
                ? RuntimeValue.FromDouble(doubleValue)
                : null;
        }

        if (type == TypeSymbol.Decimal)
        {
            return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalValue)
                ? RuntimeValue.FromDecimal(decimalValue)
                : null;
        }

        if (type == TypeSymbol.String)
        {
            return RuntimeValue.FromString(text);
        }

        if (type == TypeSymbol.Boolean)
        {
            if (string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(true);
            }

            if (string.Equals(text, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(false);
            }

            return null;
        }

        return ReportInvalidRuntimeState<RuntimeValue?>($"INPUT не поддерживает тип {type.Name}.");
    }

    private void AppendPendingOutput(string text)
    {
        _hasPendingOutput = true;
        _pendingOutput.Append(text);
    }

    private void AppendPrintSeparator()
    {
        _hasPendingOutput = true;
        _pendingOutput.Append(' ');
    }

    private void FlushPendingOutput(bool forceEmptyLine)
    {
        if (!_hasPendingOutput)
        {
            if (forceEmptyLine)
            {
                _host.WriteOutput(string.Empty);
            }

            return;
        }

        _host.WriteOutput(_pendingOutput.ToString());
        _pendingOutput.Clear();
        _hasPendingOutput = false;
    }

    private bool TryParseInputValues(
        IReadOnlyList<VariableSymbol> variables,
        string text,
        TextLocation location,
        out RuntimeValue[] values)
    {
        ArgumentNullException.ThrowIfNull(variables);

        values = Array.Empty<RuntimeValue>();

        if (variables.Count == 0)
        {
            return true;
        }

        var fields = variables.Count == 1
            ? [text]
            : text.Split(',');

        if (fields.Length != variables.Count)
        {
            ReportInputValueCountFailure(variables.Count, fields.Length, location);
            return false;
        }

        var parsedValues = new RuntimeValue[variables.Count];

        for (var index = 0; index < variables.Count; index++)
        {
            var variable = variables[index];
            var field = NormalizeInputField(fields[index], variables.Count > 1);
            var parsedValue = TryParseInput(variable.Type, field);

            if (parsedValue is null)
            {
                ReportInputConversionFailure(variable.Type, field, location);
                return false;
            }

            parsedValues[index] = parsedValue;
        }

        values = parsedValues;
        return true;
    }

    private static string NormalizeInputField(string text, bool trim)
    {
        return trim ? text.Trim() : text;
    }

    private int ToArrayBound(RuntimeValue value, TextLocation location, string detail)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => ToInt32Checked(value.AsLong(), location, detail),
            var type when type == TypeSymbol.Double => ToInt32Checked(Math.Truncate(value.AsDouble()), location, detail),
            var type when type == TypeSymbol.Decimal => ToInt32Checked(decimal.Truncate(value.AsDecimal()), location, detail),
            _ => throw new InvalidOperationException($"Value of type {value.Type.Name} cannot be used as array index.")
        };
    }

    private static bool IsZero(RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() == 0,
            var type when type == TypeSymbol.Long => value.AsLong() == 0L,
            var type when type == TypeSymbol.Double => value.AsDouble() == 0d,
            var type when type == TypeSymbol.Decimal => value.AsDecimal() == 0m,
            _ => false
        };
    }

    private int ToInt32Checked(double value, TextLocation location, string detail)
    {
        if (value < int.MinValue || value > int.MaxValue)
        {
            ReportNumericOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }

        return checked((int)value);
    }

    private int ToInt32Checked(long value, TextLocation location, string detail)
    {
        if (value < int.MinValue || value > int.MaxValue)
        {
            ReportNumericOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }

        return checked((int)value);
    }

    private int ToInt32Checked(decimal value, TextLocation location, string detail)
    {
        if (value < int.MinValue || value > int.MaxValue)
        {
            ReportNumericOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }

        return checked((int)value);
    }

    private RuntimeValue EvaluateCheckedIntegerOperation(Func<int> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromInteger(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCheckedLongOperation(Func<long> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromLong(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Long, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EnsureFiniteDoubleResult(double value, string detail, TextLocation location)
    {
        if (!double.IsFinite(value))
        {
            ReportNumericOverflow(detail, TypeSymbol.Double, location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromDouble(value);
    }

    private RuntimeValue EvaluateCheckedDecimalOperation(Func<decimal> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromDecimal(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Decimal, location);
            throw new RuntimeFailureException();
        }
    }

    private TextLocation GetBuiltInArgumentLocation(BoundCallExpression expression, int argumentIndex)
    {
        if (argumentIndex >= 0 && argumentIndex < expression.Arguments.Count)
        {
            return expression.Arguments[argumentIndex].Location;
        }

        return expression.Location;
    }

    private void ReportDivisionByZero(TextLocation location)
    {
        ReportRuntimeDiagnostic(DivisionByZeroCode, "Деление на ноль.", location);
    }

    private void ReportArrayIndicesOutOfBounds(string arrayName, IReadOnlyList<int> indices, RuntimeArrayValue array, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            ArrayBoundsCode,
            $"Индексы {FormatArrayIndices(indices)} выходят за границы массива '{arrayName}' ({FormatArrayBounds(array)}).",
            location);
    }

    private static string FormatArrayIndices(IReadOnlyList<int> indices)
    {
        return $"({string.Join(", ", indices)})";
    }

    private static string FormatArrayBounds(RuntimeArrayValue array)
    {
        return string.Join(
            ", ",
            Enumerable.Range(1, array.Rank)
                .Select(dimension => $"{array.GetLowerBound(dimension)}..{array.GetUpperBound(dimension)}"));
    }

    private void ReportInputConversionFailure(TypeSymbol type, string text, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            InputConversionCode,
            $"Невозможно преобразовать ввод '{text}' к типу {type.Name}.",
            location);
    }

    private void ReportInputValueCountFailure(int expectedCount, int actualCount, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            InputConversionCode,
            $"Ожидалось {expectedCount} значений INPUT, но получено {actualCount}.",
            location);
    }

    private void ReportStepZero(string variableName, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            StepZeroCode,
            $"В цикле FOR для переменной '{variableName}' недопустим STEP 0.",
            location);
    }

    private void ReportMissingReturnValue(string functionName, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            MissingReturnValueCode,
            $"Функция '{functionName}' завершилась без RETURN со значением.",
            location);
    }

    private void ReportEmptyFunctionValue(TextLocation location)
    {
        ReportRuntimeDiagnostic(
            EmptyFunctionValueCode,
            "Значение функционального типа не содержит функции.",
            location);
    }

    private void ReportBuiltInArgumentError(string functionName, string argumentName, string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Функция {functionName}: аргумент '{argumentName}' {detail}",
            location);
    }

    private void ReportNumericOverflow(string detail, TypeSymbol type, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            NumericOverflowCode,
            $"{detail} выходит за диапазон {type.Name}.",
            location);
    }

    private void ReportInvalidRuntimeState(string message, TextLocation? location = null)
    {
        ReportRuntimeDiagnostic(InvalidRuntimeStateCode, message, location ?? FallbackRuntimeLocation);
    }

    private T ReportInvalidRuntimeState<T>(string message, TextLocation? location = null)
    {
        ReportInvalidRuntimeState(message, location);
        throw new RuntimeFailureException();
    }

    private void ReportRuntimeDiagnostic(string code, string message, TextLocation location)
    {
        _diagnostics.Report(code, AppendCallChain(message), location);
    }

    private string AppendCallChain(string message)
    {
        if (_callStack.Count == 0)
        {
            return message;
        }

        var builder = new StringBuilder(message);
        builder.AppendLine();
        builder.AppendLine("Цепочка вызовов:");

        foreach (var frame in _callStack)
        {
            builder.Append("- ");
            builder.Append(FormatRoutineLabel(frame.Function));
            builder.Append(", вызов в ");
            builder.Append(frame.CallSiteLocation.Line);
            builder.Append(':');
            builder.Append(frame.CallSiteLocation.Column);

            if (frame.Function.ContainingRoutine is not null)
            {
                builder.Append(", объявлена внутри ");
                builder.Append(FormatContainingRoutineLabel(frame.Function.ContainingRoutine));
            }

            builder.AppendLine(".");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatRoutineLabel(FunctionSymbol function)
    {
        var builder = new StringBuilder();

        if (function.IsLocal)
        {
            builder.Append("локальная ");
        }

        builder.Append(function.IsSub ? "процедура" : "функция");
        builder.Append(" '");
        builder.Append(function.Name);
        builder.Append('\'');
        return builder.ToString();
    }

    private static string FormatContainingRoutineLabel(FunctionSymbol function)
    {
        var builder = new StringBuilder();

        if (function.IsLocal)
        {
            builder.Append("локальной ");
        }

        builder.Append(function.IsSub ? "процедуры" : "функции");
        builder.Append(" '");
        builder.Append(function.Name);
        builder.Append('\'');
        return builder.ToString();
    }

    private RuntimeScope CaptureScopeForFunction(FunctionSymbol function, TextLocation location)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (function.ContainingRoutine is null)
        {
            return _globalScope;
        }

        if (TryLookupRoutineScopeInCurrentChain(function.ContainingRoutine, out var scope) && scope is not null)
        {
            return scope;
        }

        if (TryLookupActiveRoutineScope(function.ContainingRoutine, out scope) && scope is not null)
        {
            return scope;
        }

        return ReportInvalidRuntimeState<RuntimeScope>(
            $"Для {FormatRoutineLabel(function)} не найдена область видимости объявляющей {FormatRoutineLabel(function.ContainingRoutine)}.",
            location);
    }

    private bool TryLookupActiveRoutineScope(FunctionSymbol function, out RuntimeScope? scope)
    {
        ArgumentNullException.ThrowIfNull(function);

        foreach (var frame in _callStack)
        {
            if (ReferenceEquals(frame.Function, function))
            {
                scope = frame.Scope;
                return true;
            }
        }

        scope = null;
        return false;
    }

    private bool TryLookupRoutineScopeInCurrentChain(FunctionSymbol function, out RuntimeScope? scope)
    {
        ArgumentNullException.ThrowIfNull(function);

        for (var current = CurrentScope; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current.OwningRoutine, function))
            {
                scope = current;
                return true;
            }
        }

        scope = null;
        return false;
    }

    private void EnterScope()
    {
        _scopeStack.Push(new RuntimeScope(CurrentScope));
    }

    private void EnterScope(RuntimeScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        _scopeStack.Push(scope);
    }

    private void LeaveScope()
    {
        _scopeStack.Pop();
    }

    private ExecutionSignal ExecuteStatementsInChildScope(IReadOnlyList<BoundStatement> statements)
    {
        EnterScope();

        try
        {
            return ExecuteStatements(statements);
        }
        finally
        {
            LeaveScope();
        }
    }

    private ExecutionSignal ExecuteStatementsInScope(RuntimeScope scope, IReadOnlyList<BoundStatement> statements)
    {
        EnterScope(scope);

        try
        {
            return ExecuteStatements(statements);
        }
        finally
        {
            LeaveScope();
        }
    }
}
