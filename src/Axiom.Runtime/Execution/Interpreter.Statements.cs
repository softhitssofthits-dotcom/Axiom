using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private ExecutionSignal ExecuteStatements(IReadOnlyList<BoundStatement> statements)
    {
        foreach (var statement in statements)
        {
            if (CheckCancellationRequested())
            {
                return ExecutionSignal.None;
            }

            DispatchInputHandlers(statement.Location);
            var signal = ExecuteStatement(statement);

            if (signal.HasReturn || signal.HasExitFor || signal.HasExitDo)
            {
                return signal;
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteStatement(BoundStatement statement)
    {
        return statement switch
        {
            BoundVariableDeclarationStatement variableDeclaration => ExecuteVariableDeclaration(variableDeclaration),
            BoundVariableDeclarationsStatement variableDeclarations => ExecuteVariableDeclarations(variableDeclarations),
            BoundAssignmentStatement assignment => ExecuteAssignment(assignment),
            BoundEraseStatement erase => ExecuteErase(erase),
            BoundRedimStatement redim => ExecuteRedim(redim),
            BoundPrintStatement print => ExecutePrint(print),
            BoundPrintUsingStatement printUsing => ExecutePrintUsing(printUsing),
            BoundInputStatement input => ExecuteInput(input),
            BoundSleepStatement sleep => ExecuteSleep(sleep),
            BoundScreenStatement screen => ExecuteScreen(screen),
            BoundClsStatement cls => ExecuteCls(cls),
            BoundColorStatement color => ExecuteColor(color),
            BoundLocateStatement locate => ExecuteLocate(locate),
            BoundPSetStatement pset => ExecutePSet(pset),
            BoundLineStatement line => ExecuteLine(line),
            BoundCircleStatement circle => ExecuteCircle(circle),
            BoundOnKeyStatement onKey => ExecuteOnKey(onKey),
            BoundOnMouseStatement onMouse => ExecuteOnMouse(onMouse),
            BoundOpenStatement open => ExecuteOpen(open),
            BoundCloseStatement close => ExecuteClose(close),
            BoundErrorStatement error => ExecuteError(error),
            BoundIfStatement ifStatement => ExecuteIf(ifStatement),
            BoundTryStatement tryStatement => ExecuteTry(tryStatement),
            BoundSelectCaseStatement selectCaseStatement => ExecuteSelectCase(selectCaseStatement),
            BoundDoLoopStatement doLoopStatement => ExecuteDoLoop(doLoopStatement),
            BoundWhileStatement whileStatement => ExecuteWhile(whileStatement),
            BoundForStatement forStatement => ExecuteFor(forStatement),
            BoundExitForStatement exitForStatement => ExecuteExitFor(exitForStatement),
            BoundExitDoStatement exitDoStatement => ExecuteExitDo(exitDoStatement),
            BoundReturnStatement returnStatement => ExecuteReturn(returnStatement),
            BoundExpressionStatement expressionStatement => ExecuteExpressionStatement(expressionStatement),
            _ => ReportInvalidRuntimeState<ExecutionSignal>($"Unsupported bound statement: {statement.Kind}.", statement.Location)
        };
    }

    private ExecutionSignal ExecuteVariableDeclaration(BoundVariableDeclarationStatement statement)
    {
        if (statement.Variable.IsArray)
        {
            var arrayType = (ArrayTypeSymbol)statement.Variable.Type;

            if (statement.ArrayBounds.Count == 0)
            {
                CurrentScope.Define(statement.Variable, new RuntimeArrayValue(arrayType.ElementType, statement.Variable.FixedStringLength, arrayType.Rank));
                return ExecutionSignal.None;
            }

            if (statement.Initializer is not null)
            {
                return ReportInvalidRuntimeState<ExecutionSignal>($"Array initializer for '{statement.Variable.Name}' is not supported in v0.1.", statement.Location);
            }

            var (lowerBounds, upperBounds) = EvaluateArrayBounds(statement.ArrayBounds);
            EnsureAllowedArrayLowerBounds(statement.Variable.Name, lowerBounds, statement.ArrayBounds);

            try
            {
                CurrentScope.Define(statement.Variable, new RuntimeArrayValue(arrayType.ElementType, statement.Variable.FixedStringLength, lowerBounds, upperBounds));
            }
            catch (ArgumentOutOfRangeException)
            {
                return ReportInvalidRuntimeState<ExecutionSignal>(
                    $"Массив '{statement.Variable.Name}' имеет недопустимые границы. В каждом измерении верхняя граница не может быть меньше нижней, кроме канонической пустой формы 1 TO 0.",
                    statement.Location);
            }

            return ExecutionSignal.None;
        }

        var value = statement.Initializer is null
            ? RuntimeValue.Default(statement.Variable.Type, statement.Variable.FixedStringLength)
            : EvaluateExpression(statement.Initializer);

        CurrentScope.Define(statement.Variable, value.CopyForStorage(statement.Variable.FixedStringLength));
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteVariableDeclarations(BoundVariableDeclarationsStatement statement)
    {
        foreach (var declaration in statement.Declarations)
        {
            var signal = ExecuteVariableDeclaration(declaration);

            if (signal.HasReturn || signal.HasExitFor || signal.HasExitDo)
            {
                return signal;
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteAssignment(BoundAssignmentStatement statement)
    {
        if (statement.Target is BoundVariableExpression directVariable
            && TryExecuteStringAppendAssignment(statement, directVariable.Variable))
        {
            return ExecutionSignal.None;
        }

        var value = EvaluateExpression(statement.Expression);

        switch (statement.Target)
        {
            case BoundVariableExpression variableExpression:
                AssignVariable(variableExpression.Variable, value);
                return ExecutionSignal.None;

            case BoundArrayAccessExpression arrayAccessExpression:
                AssignArrayElement(arrayAccessExpression, value);
                return ExecutionSignal.None;

            case BoundFieldAccessExpression fieldAccessExpression:
                AssignRecordField(fieldAccessExpression, value);
                return ExecutionSignal.None;

            default:
                return ReportInvalidRuntimeState<ExecutionSignal>($"Unsupported assignment target: {statement.Target.Kind}.", statement.Location);
        }
    }

    private ExecutionSignal ExecuteErase(BoundEraseStatement statement)
    {
        var array = EvaluateExpression(statement.Target).AsArray();

        if (array.IsView)
        {
            ReportBuiltInArgumentError("ERASE", "array", "не поддерживает подмассивы-представления.", statement.Location);
            throw new RuntimeFailureException();
        }

        array.Deallocate();
        return ExecutionSignal.None;
    }

    private void DispatchInputHandlers(TextLocation location)
    {
        DispatchKeyboardHandlers(location);
        DispatchMouseHandlers(location);
    }

    private ExecutionSignal ExecuteSleep(BoundSleepStatement statement)
    {
        if (statement.DurationExpression is null)
        {
            WaitForAnyKey();
            return ExecutionSignal.None;
        }

        var milliseconds = EvaluateExpression(statement.DurationExpression).AsLong();
        SleepForMilliseconds(milliseconds, statement.Location);
        return ExecutionSignal.None;
    }

    private void WaitForAnyKey()
    {
        while (!TryReadKeyboardKey(out _))
        {
            if (CheckCancellationRequested())
            {
                return;
            }

            System.Threading.Thread.Sleep(10);
        }
    }

    private bool TryExecuteStringAppendAssignment(BoundAssignmentStatement statement, VariableSymbol targetVariable)
    {
        if (targetVariable.Type != TypeSymbol.String
            || statement.Expression is not BoundBinaryExpression binary
            || binary.Left is not BoundVariableExpression leftVariable
            || !ReferenceEquals(leftVariable.Variable, targetVariable))
        {
            return false;
        }

        var useDisplayString = binary.OperatorKind == SyntaxKind.AmpersandToken;
        if (!useDisplayString
            && (binary.OperatorKind != SyntaxKind.PlusToken || binary.Type != TypeSymbol.String))
        {
            return false;
        }

        var current = GetVariableValue(targetVariable);
        var right = EvaluateExpression(binary.Right);
        var appendText = useDisplayString ? right.ToDisplayString() : right.AsString();

        if (targetVariable.FixedStringLength is { } fixedStringLength)
        {
            AssignVariable(
                targetVariable,
                RuntimeValue.FromString(RuntimeValue.NormalizeFixedString(current.AsString() + appendText, fixedStringLength)));
            return true;
        }

        var accumulator = current.AsStringAccumulator() ?? new RuntimeStringAccumulator(current.AsString());
        accumulator.Append(appendText);

        // Для обычного присваивания накопитель нельзя копировать как ссылку:
        // алиасы вроде t = s должны видеть прежнюю строку. Здесь мы обновляем
        // только ту же самую переменную в распознанном шаблоне s = s & expr.
        var accumulatedValue = RuntimeValue.FromStringAccumulator(accumulator);
        var storedValue = LookupStorage(targetVariable);
        if (storedValue is RuntimeReferenceStoredValue reference)
        {
            reference.Value = accumulatedValue;
            return true;
        }

        if (storedValue is RuntimeValue && CurrentScope.TryAssign(targetVariable, accumulatedValue))
        {
            return true;
        }

        AssignVariable(targetVariable, accumulatedValue);
        return true;
    }

    private void SleepForMilliseconds(long milliseconds, TextLocation location)
    {
        if (milliseconds < 0 || milliseconds > int.MaxValue)
        {
            ReportBuiltInArgumentError("SLEEP", "milliseconds", $"должен быть в диапазоне 0..{int.MaxValue}.", location);
            throw new RuntimeFailureException();
        }

        System.Threading.Thread.Sleep((int)milliseconds);
    }

    private ExecutionSignal ExecuteRedim(BoundRedimStatement statement)
    {
        var array = EvaluateExpression(statement.Target).AsArray();

        if (array.IsView)
        {
            ReportBuiltInArgumentError(statement.Preserve ? "REDIM PRESERVE" : "REDIM", "array", "не поддерживает подмассивы-представления.", statement.Location);
            throw new RuntimeFailureException();
        }

        var (lowerBounds, upperBounds) = EvaluateArrayBounds(statement.ArrayBounds);
        EnsureAllowedArrayLowerBounds(statement.DisplayName, lowerBounds, statement.ArrayBounds);

        try
        {
            if (statement.Preserve)
            {
                array.AllocatePreserving(lowerBounds, upperBounds);
            }
            else
            {
                array.Allocate(lowerBounds, upperBounds);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return ReportInvalidRuntimeState<ExecutionSignal>(
                $"Массив '{statement.DisplayName}' имеет недопустимые границы. В каждом измерении верхняя граница не может быть меньше нижней, кроме канонической пустой формы 1 TO 0.",
                statement.Location);
        }

        return ExecutionSignal.None;
    }

    private (int[] LowerBounds, int[] UpperBounds) EvaluateArrayBounds(IReadOnlyList<BoundArrayBound> arrayBounds)
    {
        var lowerBounds = new int[arrayBounds.Count];
        var upperBounds = new int[arrayBounds.Count];

        for (var index = 0; index < arrayBounds.Count; index++)
        {
            var bound = arrayBounds[index];
            var lowerDetail = arrayBounds.Count == 1
                ? "Нижняя граница массива"
                : $"Нижняя граница массива по измерению #{index + 1}";
            var upperDetail = arrayBounds.Count == 1
                ? "Верхняя граница массива"
                : $"Верхняя граница массива по измерению #{index + 1}";

            lowerBounds[index] = ToArrayBound(EvaluateExpression(bound.LowerBound), bound.LowerBound.Location, lowerDetail);
            upperBounds[index] = ToArrayBound(EvaluateExpression(bound.UpperBound), bound.UpperBound.Location, upperDetail);
        }

        return (lowerBounds, upperBounds);
    }

    private void EnsureAllowedArrayLowerBounds(string variableName, IReadOnlyList<int> lowerBounds, IReadOnlyList<BoundArrayBound> arrayBounds)
    {
        for (var index = 0; index < lowerBounds.Count; index++)
        {
            if (lowerBounds[index] != int.MinValue)
            {
                continue;
            }

            var dimensionText = lowerBounds.Count == 1
                ? string.Empty
                : $" по измерению #{index + 1}";
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Массив '{variableName}' не может иметь нижнюю границу {int.MinValue}{dimensionText}, потому что значение слева от диапазона используется поисковыми функциями массива как признак отсутствия совпадения.",
                arrayBounds[index].LowerBound.Location);
            throw new RuntimeFailureException();
        }
    }

    private ExecutionSignal ExecutePrint(BoundPrintStatement statement)
    {
        if (statement.Items.Count == 0)
        {
            FlushPendingOutput(forceEmptyLine: true);
            return ExecutionSignal.None;
        }

        foreach (var item in statement.Items)
        {
            var value = EvaluateExpression(item.Expression);
            AppendPendingOutput(value.ToDisplayString());

            switch (item.SeparatorKind)
            {
                case PrintSeparatorKind.Semicolon:
                    break;

                case PrintSeparatorKind.Comma:
                    AppendPrintSeparator();
                    break;

                default:
                    FlushPendingOutput(forceEmptyLine: false);
                    break;
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePrintUsing(BoundPrintUsingStatement statement)
    {
        var format = EvaluateExpression(statement.FormatExpression).AsString();
        var values = new List<(RuntimeValue Value, TextLocation Location)>(statement.Items.Count);

        foreach (var item in statement.Items)
        {
            values.Add((EvaluateExpression(item.Expression), item.Expression.Location));
        }

        AppendPendingOutput(FormatUsing(format, values, statement.Location));

        var trailingSeparator = statement.Items.Count == 0
            ? null
            : statement.Items[^1].SeparatorKind;

        switch (trailingSeparator)
        {
            case PrintSeparatorKind.Semicolon:
                break;

            case PrintSeparatorKind.Comma:
                AppendPrintSeparator();
                break;

            default:
                FlushPendingOutput(forceEmptyLine: false);
                break;
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteInput(BoundInputStatement statement)
    {
        if (statement.Variables.Any(static variable => variable.IsArray))
        {
            var arrayVariable = statement.Variables.First(static variable => variable.IsArray);
            return ReportInvalidRuntimeState<ExecutionSignal>($"INPUT does not support array '{arrayVariable.Name}'.", statement.Location);
        }

        var request = new InputRequest(statement.PromptText, statement.Variables.Count);
        var text = _host.IsScreenOutputActive ? _host.ReadGraphicsInput(request) : _host.ReadInput(request);

        if (!TryParseInputValues(statement.Variables, text, statement.Location, out var values))
        {
            throw new RuntimeFailureException();
        }

        for (var index = 0; index < statement.Variables.Count; index++)
        {
            AssignVariable(statement.Variables[index], values[index]);
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteOnKey(BoundOnKeyStatement statement)
    {
        var keyText = EvaluateExpression(statement.KeyExpression).AsString();

        if (statement.DisablesHandler)
        {
            UnregisterKeyboardHandler(keyText);
            return ExecutionSignal.None;
        }

        if (statement.PausesHandler)
        {
            PauseKeyboardHandler(keyText);
            return ExecutionSignal.None;
        }

        if (statement.ResumesHandler)
        {
            ResumeKeyboardHandler(keyText);
            return ExecutionSignal.None;
        }

        var handler = EvaluateExpression(statement.HandlerExpression!).AsFunction();
        if (handler is null)
        {
            return ReportInvalidRuntimeState<ExecutionSignal>(
                "ON KEY ожидает корректное значение процедуры-обработчика.",
                statement.Location);
        }

        RegisterKeyboardHandler(keyText, handler, statement.HandlerReceivesKeyText);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteOnMouse(BoundOnMouseStatement statement)
    {
        var buttonNumber = EvaluateExpression(statement.ButtonExpression).AsInteger();
        var eventKind = ToMouseInputEventKind(statement.EventKind);

        if (statement.DisablesHandler)
        {
            UnregisterMouseHandler(buttonNumber, eventKind);
            return ExecutionSignal.None;
        }

        if (statement.PausesHandler)
        {
            PauseMouseHandler(buttonNumber, eventKind);
            return ExecutionSignal.None;
        }

        if (statement.ResumesHandler)
        {
            ResumeMouseHandler(buttonNumber, eventKind);
            return ExecutionSignal.None;
        }

        var handler = EvaluateExpression(statement.HandlerExpression!).AsFunction();
        if (handler is null)
        {
            return ReportInvalidRuntimeState<ExecutionSignal>(
                "ON MOUSE ожидает корректное значение процедуры-обработчика.",
                statement.Location);
        }

        RegisterMouseHandler(buttonNumber, eventKind, handler, statement.HandlerReceivesEvent);
        return ExecutionSignal.None;
    }

    private static MouseInputEventKind ToMouseInputEventKind(BoundMouseEventKind eventKind) => eventKind switch
    {
        BoundMouseEventKind.Release => MouseInputEventKind.Release,
        BoundMouseEventKind.Move => MouseInputEventKind.Move,
        BoundMouseEventKind.Wheel => MouseInputEventKind.Wheel,
        _ => MouseInputEventKind.Press
    };

    private ExecutionSignal ExecuteError(BoundErrorStatement statement)
    {
        if (statement.Mode == BoundErrorStatementMode.ExistingError)
        {
            var error = EvaluateExpression(statement.Expression).AsRuntimeError();
            ReportRuntimeDiagnostic(
                error.Code,
                error.Message,
                new TextLocation(new TextSpan(0, 0), error.Line, error.Column));
            throw new RuntimeFailureException();
        }

        var code = EvaluateExpression(statement.Expression).AsString();

        if (string.IsNullOrWhiteSpace(code))
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                "ERROR: код пользовательской ошибки не должен быть пустым.",
                statement.Expression.Location);
            throw new RuntimeFailureException();
        }

        var message = statement.MessageExpression is null
            ? $"Пользовательская ошибка {code}."
            : EvaluateExpression(statement.MessageExpression).AsString();

        ReportRuntimeDiagnostic(code, message, statement.Location);
        throw new RuntimeFailureException();
    }

    private ExecutionSignal ExecuteIf(BoundIfStatement statement)
    {
        var condition = EvaluateExpression(statement.Condition).AsBoolean();
        return condition
            ? ExecuteStatementsInChildScope(statement.Statements)
            : ExecuteStatementsInChildScope(statement.ElseStatements);
    }

    private ExecutionSignal ExecuteTry(BoundTryStatement statement)
    {
        ExecutionSignal signal = ExecutionSignal.None;
        RuntimeFailureException? unhandledFailure = null;

        try
        {
            signal = ExecuteStatementsInChildScope(statement.Statements);
        }
        catch (RuntimeFailureException failure)
        {
            if (statement.CatchVariable is null)
            {
                unhandledFailure = failure;
            }
            else
            {
                var error = ConsumeLastRuntimeError();
                var catchScope = new RuntimeScope(CurrentScope);
                catchScope.Define(statement.CatchVariable, error);
                signal = ExecuteStatementsInScope(catchScope, statement.CatchStatements);
            }
        }

        var finallySignal = ExecuteFinally(statement);

        if (finallySignal.HasReturn || finallySignal.HasExitFor || finallySignal.HasExitDo)
        {
            return finallySignal;
        }

        if (unhandledFailure is not null)
        {
            throw unhandledFailure;
        }

        return signal;
    }

    private ExecutionSignal ExecuteFinally(BoundTryStatement statement)
    {
        return statement.FinallyStatements.Count == 0
            ? ExecutionSignal.None
            : ExecuteStatementsInChildScope(statement.FinallyStatements);
    }

    private RuntimeValue ConsumeLastRuntimeError()
    {
        var diagnostic = _lastRuntimeFailureDiagnostic;
        _lastRuntimeFailureDiagnostic = null;

        if (diagnostic is null)
        {
            return RuntimeValue.FromRuntimeError(new RuntimeErrorValue("MB3005", "Runtime failure.", 0, 0));
        }

        _diagnostics.Remove(diagnostic);
        return RuntimeValue.FromRuntimeError(new RuntimeErrorValue(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Location.Line,
            diagnostic.Location.Column));
    }

    private ExecutionSignal ExecuteWhile(BoundWhileStatement statement)
    {
        while (EvaluateExpression(statement.Condition).AsBoolean())
        {
            DispatchInputHandlers(statement.Location);
            var signal = ExecuteStatementsInChildScope(statement.Statements);

            if (signal.HasReturn || signal.HasExitFor || signal.HasExitDo)
            {
                return signal;
            }
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteSelectCase(BoundSelectCaseStatement statement)
    {
        var testValue = EvaluateExpression(statement.TestExpression);

        foreach (var clause in statement.Clauses)
        {
            if (MatchesSelectCaseClause(testValue, clause))
            {
                return ExecuteStatementsInChildScope(clause.Statements);
            }
        }

        return ExecuteStatementsInChildScope(statement.ElseStatements);
    }

    private bool MatchesSelectCaseClause(RuntimeValue testValue, BoundSelectCaseClause clause)
    {
        foreach (var selector in clause.Selectors)
        {
            if (MatchesSelectCaseSelector(testValue, selector))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesSelectCaseSelector(RuntimeValue testValue, BoundSelectCaseSelector selector)
    {
        return selector switch
        {
            BoundCaseValueSelector valueSelector => ValuesEqual(testValue, EvaluateExpression(valueSelector.Expression)),
            BoundCaseRangeSelector rangeSelector => MatchesCaseRange(testValue, rangeSelector),
            BoundCaseComparisonSelector comparisonSelector => MatchesCaseComparison(testValue, comparisonSelector),
            _ => ReportInvalidRuntimeState<bool>($"Unsupported SELECT CASE selector: {selector.GetType().Name}.", selector.Location)
        };
    }

    private bool MatchesCaseRange(RuntimeValue testValue, BoundCaseRangeSelector selector)
    {
        var lowerBound = EvaluateExpression(selector.LowerBound);
        var upperBound = EvaluateExpression(selector.UpperBound);
        return Compare(testValue, lowerBound) >= 0 && Compare(testValue, upperBound) <= 0;
    }

    private bool MatchesCaseComparison(RuntimeValue testValue, BoundCaseComparisonSelector selector)
    {
        var value = EvaluateExpression(selector.Expression);

        return selector.OperatorKind switch
        {
            SyntaxKind.EqualsToken => ValuesEqual(testValue, value),
            SyntaxKind.NotEqualsToken => !ValuesEqual(testValue, value),
            SyntaxKind.LessToken => Compare(testValue, value) < 0,
            SyntaxKind.LessOrEqualsToken => Compare(testValue, value) <= 0,
            SyntaxKind.GreaterToken => Compare(testValue, value) > 0,
            SyntaxKind.GreaterOrEqualsToken => Compare(testValue, value) >= 0,
            _ => ReportInvalidRuntimeState<bool>($"Unsupported CASE IS operator: {selector.OperatorKind}.", selector.Location)
        };
    }

    private ExecutionSignal ExecuteDoLoop(BoundDoLoopStatement statement)
    {
        if (!statement.EvaluateConditionAfterBody && !ShouldContinueDoLoop(statement))
        {
            return ExecutionSignal.None;
        }

        while (true)
        {
            if (CheckCancellationRequested())
            {
                return ExecutionSignal.None;
            }

            DispatchInputHandlers(statement.Location);
            var signal = ExecuteStatementsInChildScope(statement.Statements);

            if (signal.HasReturn)
            {
                return signal;
            }

            if (signal.HasExitDo)
            {
                break;
            }

            if (statement.EvaluateConditionAfterBody)
            {
                if (!ShouldContinueDoLoop(statement))
                {
                    break;
                }
            }
            else if (!ShouldContinueDoLoop(statement))
            {
                break;
            }
        }

        return ExecutionSignal.None;
    }

    private bool ShouldContinueDoLoop(BoundDoLoopStatement statement)
    {
        if (statement.Condition is null || statement.ConditionKind == BoundDoLoopConditionKind.None)
        {
            return true;
        }

        var condition = EvaluateExpression(statement.Condition).AsBoolean();
        return statement.ConditionKind == BoundDoLoopConditionKind.While
            ? condition
            : !condition;
    }

    private ExecutionSignal ExecuteFor(BoundForStatement statement)
    {
        var start = EvaluateExpression(statement.StartExpression);
        var end = EvaluateExpression(statement.EndExpression);
        var step = EvaluateExpression(statement.StepExpression);

        if (IsZero(step))
        {
            ReportStepZero(statement.Variable.Name, statement.StepExpression.Location);
            throw new RuntimeFailureException();
        }

        var loopScope = new RuntimeScope(CurrentScope);

        if (statement.Variable.Type == TypeSymbol.Integer)
        {
            loopScope.Define(statement.Variable, RuntimeValue.FromInteger(start.AsInteger()));
            EnterScope(loopScope);

            try
            {
                return RunIntegerForLoop(statement, end.AsInteger(), step.AsInteger());
            }
            finally
            {
                LeaveScope();
            }
        }

        if (statement.Variable.Type == TypeSymbol.Long)
        {
            loopScope.Define(statement.Variable, RuntimeValue.FromLong(start.AsLong()));
            EnterScope(loopScope);

            try
            {
                return RunLongForLoop(statement, end.AsLong(), step.AsLong());
            }
            finally
            {
                LeaveScope();
            }
        }

        if (statement.Variable.Type == TypeSymbol.Double)
        {
            loopScope.Define(statement.Variable, RuntimeValue.FromDouble(start.AsDouble()));
            EnterScope(loopScope);

            try
            {
                return RunDoubleForLoop(statement, end.AsDouble(), step.AsDouble());
            }
            finally
            {
                LeaveScope();
            }
        }

        return ReportInvalidRuntimeState<ExecutionSignal>($"Unsupported FOR control variable type: {statement.Variable.Type.Name}.", statement.Location);
    }

    private ExecutionSignal RunIntegerForLoop(BoundForStatement statement, int endValue, int stepValue)
    {
        while (true)
        {
            if (CheckCancellationRequested())
            {
                return ExecutionSignal.None;
            }

            DispatchInputHandlers(statement.Location);
            var current = GetVariableValue(statement.Variable).AsInteger();

            if (stepValue > 0)
            {
                if (current > endValue)
                {
                    break;
                }
            }
            else if (current < endValue)
            {
                break;
            }

            var signal = ExecuteStatementsInChildScope(statement.Statements);

            if (signal.HasReturn || signal.HasExitDo)
            {
                return signal;
            }

            if (signal.HasExitFor)
            {
                break;
            }

            AssignVariable(
                statement.Variable,
                EvaluateCheckedIntegerOperation(
                    () => checked(current + stepValue),
                    $"Следующее значение FOR для переменной '{statement.Variable.Name}'",
                    statement.StepExpression.Location));
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal RunLongForLoop(BoundForStatement statement, long endValue, long stepValue)
    {
        while (true)
        {
            if (CheckCancellationRequested())
            {
                return ExecutionSignal.None;
            }

            DispatchInputHandlers(statement.Location);
            var current = GetVariableValue(statement.Variable).AsLong();

            if (stepValue > 0L)
            {
                if (current > endValue)
                {
                    break;
                }
            }
            else if (current < endValue)
            {
                break;
            }

            var signal = ExecuteStatementsInChildScope(statement.Statements);

            if (signal.HasReturn || signal.HasExitDo)
            {
                return signal;
            }

            if (signal.HasExitFor)
            {
                break;
            }

            AssignVariable(
                statement.Variable,
                EvaluateCheckedLongOperation(
                    () => checked(current + stepValue),
                    $"Следующее значение FOR для переменной '{statement.Variable.Name}'",
                    statement.StepExpression.Location));
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal RunDoubleForLoop(BoundForStatement statement, double endValue, double stepValue)
    {
        while (true)
        {
            if (CheckCancellationRequested())
            {
                return ExecutionSignal.None;
            }

            DispatchInputHandlers(statement.Location);
            var current = GetVariableValue(statement.Variable).AsDouble();

            if (stepValue > 0d)
            {
                if (current > endValue)
                {
                    break;
                }
            }
            else if (current < endValue)
            {
                break;
            }

            var signal = ExecuteStatementsInChildScope(statement.Statements);

            if (signal.HasReturn || signal.HasExitDo)
            {
                return signal;
            }

            if (signal.HasExitFor)
            {
                break;
            }

            AssignVariable(
                statement.Variable,
                EnsureFiniteDoubleResult(
                    current + stepValue,
                    $"Следующее значение FOR для переменной '{statement.Variable.Name}'",
                    statement.StepExpression.Location));
        }

        return ExecutionSignal.None;
    }

    private static ExecutionSignal ExecuteExitFor(BoundExitForStatement statement)
    {
        return ExecutionSignal.ExitFor();
    }

    private static ExecutionSignal ExecuteExitDo(BoundExitDoStatement statement)
    {
        return ExecutionSignal.ExitDo();
    }

    private ExecutionSignal ExecuteReturn(BoundReturnStatement statement)
    {
        var value = statement.Expression is null
            ? RuntimeValue.Void
            : EvaluateExpression(statement.Expression);

        return ExecutionSignal.Return(value);
    }

    private ExecutionSignal ExecuteExpressionStatement(BoundExpressionStatement statement)
    {
        _ = EvaluateExpression(statement.Expression);
        return ExecutionSignal.None;
    }
}
