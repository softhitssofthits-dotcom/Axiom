using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private static void LowerInput(BoundInputStatement input, List<LoweredInstruction> instructions)
    {
        instructions.Add(LoweredInstruction.Input(input.Location, input.Variables, input.DisplayPrompt));
    }

    private void LowerOnKey(BoundOnKeyStatement onKey, List<LoweredInstruction> instructions)
    {
        LowerExpression(onKey.KeyExpression, instructions);

        if (onKey.DisablesHandler)
        {
            instructions.Add(LoweredInstruction.UnregisterKeyHandler(onKey.Location));
            return;
        }

        if (onKey.PausesHandler)
        {
            instructions.Add(LoweredInstruction.PauseKeyHandler(onKey.Location));
            return;
        }

        if (onKey.ResumesHandler)
        {
            instructions.Add(LoweredInstruction.ResumeKeyHandler(onKey.Location));
            return;
        }

        LowerExpression(onKey.HandlerExpression!, instructions);
        instructions.Add(LoweredInstruction.RegisterKeyHandler(onKey.Location, onKey.HandlerReceivesKeyText));
    }

    private void LowerOnMouse(BoundOnMouseStatement onMouse, List<LoweredInstruction> instructions)
    {
        LowerExpression(onMouse.ButtonExpression, instructions);

        if (onMouse.DisablesHandler)
        {
            instructions.Add(LoweredInstruction.UnregisterMouseHandler(onMouse.Location, onMouse.EventKind));
            return;
        }

        if (onMouse.PausesHandler)
        {
            instructions.Add(LoweredInstruction.PauseMouseHandler(onMouse.Location, onMouse.EventKind));
            return;
        }

        if (onMouse.ResumesHandler)
        {
            instructions.Add(LoweredInstruction.ResumeMouseHandler(onMouse.Location, onMouse.EventKind));
            return;
        }

        LowerExpression(onMouse.HandlerExpression!, instructions);
        instructions.Add(LoweredInstruction.RegisterMouseHandler(onMouse.Location, onMouse.HandlerReceivesEvent, onMouse.EventKind));
    }

    private static bool ContainsInputHandlerStatement(BoundProgram program)
    {
        return program.GlobalStatements.Any(ContainsInputHandlerStatement)
            || program.Routines.Any(static routine => routine.Statements.Any(ContainsInputHandlerStatement));
    }

    private static bool ContainsInputHandlerStatement(BoundStatement statement)
    {
        return statement switch
        {
            BoundOnKeyStatement or BoundOnMouseStatement => true,
            BoundVariableDeclarationsStatement declarations => declarations.Declarations.Any(ContainsInputHandlerStatement),
            BoundIfStatement ifStatement => ifStatement.Statements.Any(ContainsInputHandlerStatement)
                || ifStatement.ElseStatements.Any(ContainsInputHandlerStatement),
            BoundTryStatement tryStatement => tryStatement.Statements.Any(ContainsInputHandlerStatement)
                || tryStatement.CatchStatements.Any(ContainsInputHandlerStatement)
                || tryStatement.FinallyStatements.Any(ContainsInputHandlerStatement),
            BoundSelectCaseStatement selectCase => selectCase.Clauses.Any(static clause => clause.Statements.Any(ContainsInputHandlerStatement))
                || selectCase.ElseStatements.Any(ContainsInputHandlerStatement),
            BoundWhileStatement whileStatement => whileStatement.Statements.Any(ContainsInputHandlerStatement),
            BoundDoLoopStatement doLoopStatement => doLoopStatement.Statements.Any(ContainsInputHandlerStatement),
            BoundForStatement forStatement => forStatement.Statements.Any(ContainsInputHandlerStatement),
            _ => false
        };
    }

    private void LowerOpen(BoundOpenStatement open, List<LoweredInstruction> instructions)
    {
        LowerExpression(open.PathExpression, instructions);
        if (open.EncodingExpression is not null)
        {
            LowerExpression(open.EncodingExpression, instructions);
        }
        instructions.Add(LoweredInstruction.LoadVariable(open.Location, open.FileVariable));
        instructions.Add(LoweredInstruction.OpenFile(open.Location, open.Mode, open.FileVariable.Name, open.EncodingExpression is not null));
        instructions.Add(LoweredInstruction.StoreVariable(open.Location, open.FileVariable));
    }

    private void LowerClose(BoundCloseStatement close, List<LoweredInstruction> instructions)
    {
        instructions.Add(LoweredInstruction.LoadVariable(close.Location, close.FileVariable));
        instructions.Add(LoweredInstruction.CloseFile(close.Location));
        instructions.Add(LoweredInstruction.StoreVariable(close.Location, close.FileVariable));
    }

    private void LowerError(BoundErrorStatement error, List<LoweredInstruction> instructions)
    {
        LowerExpression(error.Expression, instructions);
        if (error.MessageExpression is not null)
        {
            LowerExpression(error.MessageExpression, instructions);
        }

        instructions.Add(error.Mode == BoundErrorStatementMode.ExistingError
            ? LoweredInstruction.RaiseExistingError(error.Location)
            : LoweredInstruction.RaiseError(error.Location, error.MessageExpression is not null));
    }

    private void LowerTry(BoundTryStatement tryStatement, List<LoweredInstruction> instructions)
    {
        var catchLabel = tryStatement.CatchVariable is null ? null : CreateLabel();
        var finallyLabel = tryStatement.FinallyStatements.Count == 0 ? null : CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.PushErrorHandler(
            tryStatement.Location,
            catchLabel,
            finallyLabel,
            endLabel,
            tryStatement.CatchVariable));

        var finallyContext = tryStatement.FinallyStatements.Count == 0
            ? null
            : new FinallyContext(tryStatement.Location, tryStatement.FinallyStatements);

        if (finallyContext is not null)
        {
            _activeFinallyContexts.Add(finallyContext);
        }

        try
        {
            foreach (var statement in tryStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            if (finallyContext is not null)
            {
                _activeFinallyContexts.RemoveAt(_activeFinallyContexts.Count - 1);
            }
        }

        instructions.Add(LoweredInstruction.PopErrorHandler(tryStatement.Location));
        instructions.Add(LoweredInstruction.Jump(tryStatement.Location, finallyLabel ?? endLabel));

        if (catchLabel is not null)
        {
            instructions.Add(LoweredInstruction.LabelMarker(tryStatement.Location, catchLabel));

            if (finallyContext is not null)
            {
                _activeFinallyContexts.Add(finallyContext);
            }

            try
            {
                foreach (var statement in tryStatement.CatchStatements)
                {
                    LowerStatement(statement, instructions);
                }
            }
            finally
            {
                if (finallyContext is not null)
                {
                    _activeFinallyContexts.RemoveAt(_activeFinallyContexts.Count - 1);
                }
            }

            instructions.Add(LoweredInstruction.PopErrorHandler(tryStatement.Location));
            instructions.Add(LoweredInstruction.Jump(tryStatement.Location, finallyLabel ?? endLabel));
        }

        if (finallyLabel is not null)
        {
            instructions.Add(LoweredInstruction.LabelMarker(tryStatement.Location, finallyLabel));
            foreach (var statement in tryStatement.FinallyStatements)
            {
                LowerStatement(statement, instructions);
            }

            instructions.Add(LoweredInstruction.EndFinally(tryStatement.Location));
        }

        instructions.Add(LoweredInstruction.LabelMarker(tryStatement.Location, endLabel));
    }

    private void LowerIf(BoundIfStatement ifStatement, List<LoweredInstruction> instructions)
    {
        if (ifStatement.Condition is BoundLiteralExpression conditionLiteral
            && conditionLiteral.Value is bool boolValue)
        {
            if (boolValue)
            {
                foreach (var statement in ifStatement.Statements)
                {
                    LowerStatement(statement, instructions);
                }
            }
            else
            {
                foreach (var statement in ifStatement.ElseStatements)
                {
                    LowerStatement(statement, instructions);
                }
            }

            return;
        }

        var elseLabel = CreateLabel();
        var endLabel = CreateLabel();

        LowerExpression(ifStatement.Condition, instructions);
        instructions.Add(LoweredInstruction.JumpIfFalse(ifStatement.Condition.Location, elseLabel));

        foreach (var statement in ifStatement.Statements)
        {
            LowerStatement(statement, instructions);
        }

        if (ifStatement.ElseStatements.Count > 0)
        {
            instructions.Add(LoweredInstruction.Jump(ifStatement.Location, endLabel));
            instructions.Add(LoweredInstruction.LabelMarker(ifStatement.Location, elseLabel));

            foreach (var statement in ifStatement.ElseStatements)
            {
                LowerStatement(statement, instructions);
            }

            instructions.Add(LoweredInstruction.LabelMarker(ifStatement.Location, endLabel));
        }
        else
        {
            instructions.Add(LoweredInstruction.LabelMarker(ifStatement.Location, elseLabel));
        }
    }

    private void LowerSelectCase(BoundSelectCaseStatement selectCaseStatement, List<LoweredInstruction> instructions)
    {
        var endLabel = CreateLabel();
        var testVariable = CreateSelectCaseTemporary(selectCaseStatement, endLabel);

        LowerExpression(selectCaseStatement.TestExpression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(selectCaseStatement.TestExpression.Location, testVariable));

        foreach (var clause in selectCaseStatement.Clauses)
        {
            var caseLabel = CreateLabel();
            var nextClauseLabel = CreateLabel();

            foreach (var selector in clause.Selectors)
            {
                LowerSelectCaseSelector(selector, testVariable, instructions);
                instructions.Add(LoweredInstruction.JumpIfTrue(selector.Location, caseLabel));
            }

            instructions.Add(LoweredInstruction.Jump(selectCaseStatement.Location, nextClauseLabel));
            instructions.Add(LoweredInstruction.LabelMarker(selectCaseStatement.Location, caseLabel));

            foreach (var statement in clause.Statements)
            {
                LowerStatement(statement, instructions);
            }

            instructions.Add(LoweredInstruction.Jump(selectCaseStatement.Location, endLabel));
            instructions.Add(LoweredInstruction.LabelMarker(selectCaseStatement.Location, nextClauseLabel));
        }

        foreach (var statement in selectCaseStatement.ElseStatements)
        {
            LowerStatement(statement, instructions);
        }

        instructions.Add(LoweredInstruction.LabelMarker(selectCaseStatement.Location, endLabel));
    }

    private void LowerSelectCaseSelector(
        BoundSelectCaseSelector selector,
        VariableSymbol testVariable,
        List<LoweredInstruction> instructions)
    {
        switch (selector)
        {
            case BoundCaseValueSelector valueSelector:
                instructions.Add(LoweredInstruction.LoadVariable(valueSelector.Location, testVariable));
                LowerExpression(valueSelector.Expression, instructions);
                instructions.Add(LoweredInstruction.BinaryOperation(valueSelector.Location, SyntaxKind.EqualsToken));
                break;

            case BoundCaseRangeSelector rangeSelector:
                instructions.Add(LoweredInstruction.LoadVariable(rangeSelector.Location, testVariable));
                LowerExpression(rangeSelector.LowerBound, instructions);
                instructions.Add(LoweredInstruction.BinaryOperation(rangeSelector.LowerBound.Location, SyntaxKind.GreaterOrEqualsToken));
                instructions.Add(LoweredInstruction.LoadVariable(rangeSelector.Location, testVariable));
                LowerExpression(rangeSelector.UpperBound, instructions);
                instructions.Add(LoweredInstruction.BinaryOperation(rangeSelector.UpperBound.Location, SyntaxKind.LessOrEqualsToken));
                instructions.Add(LoweredInstruction.BinaryOperation(rangeSelector.Location, SyntaxKind.AndKeyword));
                break;

            case BoundCaseComparisonSelector comparisonSelector:
                instructions.Add(LoweredInstruction.LoadVariable(comparisonSelector.Location, testVariable));
                LowerExpression(comparisonSelector.Expression, instructions);
                instructions.Add(LoweredInstruction.BinaryOperation(comparisonSelector.Location, comparisonSelector.OperatorKind));
                break;

            default:
                ReportUnsupportedSelector(selector, "селектор SELECT CASE");
                break;
        }
    }

    private void LowerWhile(BoundWhileStatement whileStatement, List<LoweredInstruction> instructions)
    {
        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(whileStatement.Location, startLabel));
        EmitLoopIterationBoundary(whileStatement.Location, instructions);
        LowerExpression(whileStatement.Condition, instructions);
        instructions.Add(LoweredInstruction.JumpIfFalse(whileStatement.Condition.Location, endLabel));

        _whileContinueLabels.Push(new LoopExitContext(startLabel, _activeFinallyContexts.Count));
        try
        {
            foreach (var statement in whileStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _whileContinueLabels.Pop();
        }

        instructions.Add(LoweredInstruction.Jump(whileStatement.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(whileStatement.Location, endLabel));
    }

    private void LowerFor(BoundForStatement forStatement, List<LoweredInstruction> instructions)
    {
        var checkLabel = CreateLabel();
        var negativeStepLabel = CreateLabel();
        var bodyLabel = CreateLabel();
        var continueLabel = CreateLabel();
        var endLabel = CreateLabel();
        var endVariable = CreateForTemporary(forStatement, "end", checkLabel);
        var stepVariable = CreateForTemporary(forStatement, "step", checkLabel);

        LowerExpression(forStatement.StartExpression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(forStatement.StartExpression.Location, forStatement.Variable));

        LowerExpression(forStatement.EndExpression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(forStatement.EndExpression.Location, endVariable));

        LowerExpression(forStatement.StepExpression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(forStatement.StepExpression.Location, stepVariable));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.StepExpression.Location, stepVariable));
        instructions.Add(LoweredInstruction.ValidateForStep(forStatement.StepExpression.Location));

        instructions.Add(LoweredInstruction.LabelMarker(forStatement.Location, checkLabel));
        EmitLoopIterationBoundary(forStatement.Location, instructions);

        instructions.Add(LoweredInstruction.LoadVariable(forStatement.StepExpression.Location, stepVariable));
        instructions.Add(LoweredInstruction.LoadLiteral(forStatement.StepExpression.Location, CreateZeroLiteral(forStatement.Variable.Type), forStatement.Variable.Type));
        instructions.Add(LoweredInstruction.BinaryOperation(forStatement.StepExpression.Location, SyntaxKind.GreaterToken));
        instructions.Add(LoweredInstruction.JumpIfFalse(forStatement.StepExpression.Location, negativeStepLabel));

        instructions.Add(LoweredInstruction.LoadVariable(forStatement.Location, forStatement.Variable));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.EndExpression.Location, endVariable));
        instructions.Add(LoweredInstruction.BinaryOperation(forStatement.EndExpression.Location, SyntaxKind.GreaterToken));
        instructions.Add(LoweredInstruction.JumpIfTrue(forStatement.EndExpression.Location, endLabel));
        instructions.Add(LoweredInstruction.Jump(forStatement.Location, bodyLabel));

        instructions.Add(LoweredInstruction.LabelMarker(forStatement.StepExpression.Location, negativeStepLabel));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.Location, forStatement.Variable));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.EndExpression.Location, endVariable));
        instructions.Add(LoweredInstruction.BinaryOperation(forStatement.EndExpression.Location, SyntaxKind.LessToken));
        instructions.Add(LoweredInstruction.JumpIfTrue(forStatement.EndExpression.Location, endLabel));

        instructions.Add(LoweredInstruction.LabelMarker(forStatement.Location, bodyLabel));

        _forExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
        _forContinueLabels.Push(new LoopExitContext(continueLabel, _activeFinallyContexts.Count));
        try
        {
            foreach (var statement in forStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _forContinueLabels.Pop();
            _forExitLabels.Pop();
        }

        instructions.Add(LoweredInstruction.LabelMarker(forStatement.Location, continueLabel));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.Location, forStatement.Variable));
        instructions.Add(LoweredInstruction.LoadVariable(forStatement.StepExpression.Location, stepVariable));
        instructions.Add(LoweredInstruction.BinaryOperation(forStatement.StepExpression.Location, SyntaxKind.PlusToken));
        instructions.Add(LoweredInstruction.StoreVariable(forStatement.StepExpression.Location, forStatement.Variable));
        instructions.Add(LoweredInstruction.Jump(forStatement.Location, checkLabel));
        instructions.Add(LoweredInstruction.LabelMarker(forStatement.Location, endLabel));
    }

    private void LowerDoLoop(BoundDoLoopStatement doLoopStatement, List<LoweredInstruction> instructions)
    {
        if (doLoopStatement.ConditionKind == BoundDoLoopConditionKind.None)
        {
            if (doLoopStatement.Condition is not null)
            {
                ReportUnsupported(doLoopStatement, "форма DO ... LOOP");
                return;
            }

            LowerInfiniteDoLoop(doLoopStatement, instructions);
            return;
        }

        if (doLoopStatement.Condition is null
            || doLoopStatement.ConditionKind is not (BoundDoLoopConditionKind.While or BoundDoLoopConditionKind.Until))
        {
            ReportUnsupported(doLoopStatement, "форма DO ... LOOP");
            return;
        }

        if (doLoopStatement.EvaluateConditionAfterBody)
        {
            LowerPostCheckDoLoop(doLoopStatement, instructions);
        }
        else
        {
            LowerPreCheckDoLoop(doLoopStatement, instructions);
        }
    }

    private void LowerPreCheckDoLoop(BoundDoLoopStatement doLoopStatement, List<LoweredInstruction> instructions)
    {
        if (doLoopStatement.Condition is not { } condition)
        {
            ReportUnsupported(doLoopStatement, "форма DO ... LOOP");
            return;
        }

        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));
        EmitLoopIterationBoundary(doLoopStatement.Location, instructions);
        LowerExpression(condition, instructions);
        instructions.Add(doLoopStatement.ConditionKind == BoundDoLoopConditionKind.While
            ? LoweredInstruction.JumpIfFalse(condition.Location, endLabel)
            : LoweredInstruction.JumpIfTrue(condition.Location, endLabel));

        _doLoopExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
        _doContinueLabels.Push(new LoopExitContext(startLabel, _activeFinallyContexts.Count));
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _doContinueLabels.Pop();
            _doLoopExitLabels.Pop();
        }

        instructions.Add(LoweredInstruction.Jump(doLoopStatement.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, endLabel));
    }

    private void LowerInfiniteDoLoop(BoundDoLoopStatement doLoopStatement, List<LoweredInstruction> instructions)
    {
        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));
        EmitLoopIterationBoundary(doLoopStatement.Location, instructions);

        _doLoopExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
        _doContinueLabels.Push(new LoopExitContext(startLabel, _activeFinallyContexts.Count));
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _doContinueLabels.Pop();
            _doLoopExitLabels.Pop();
        }

        instructions.Add(LoweredInstruction.Jump(doLoopStatement.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, endLabel));
    }

    private void LowerPostCheckDoLoop(BoundDoLoopStatement doLoopStatement, List<LoweredInstruction> instructions)
    {
        if (doLoopStatement.Condition is not { } condition
            || doLoopStatement.ConditionKind is not (BoundDoLoopConditionKind.While or BoundDoLoopConditionKind.Until))
        {
            ReportUnsupported(doLoopStatement, "форма DO ... LOOP");
            return;
        }

        var startLabel = CreateLabel();
        var continueLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));
        EmitLoopIterationBoundary(doLoopStatement.Location, instructions);

        _doLoopExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
        _doContinueLabels.Push(new LoopExitContext(continueLabel, _activeFinallyContexts.Count));
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _doContinueLabels.Pop();
            _doLoopExitLabels.Pop();
        }

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, continueLabel));
        LowerExpression(condition, instructions);
        instructions.Add(doLoopStatement.ConditionKind == BoundDoLoopConditionKind.While
            ? LoweredInstruction.JumpIfTrue(condition.Location, startLabel)
            : LoweredInstruction.JumpIfFalse(condition.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, endLabel));
    }

    private void EmitLoopIterationBoundary(Axiom.Core.TextLocation location, List<LoweredInstruction> instructions)
    {
        if (_emitStatementBoundaries)
        {
            instructions.Add(LoweredInstruction.StatementBoundary(location));
        }
    }

    private void LowerExitDo(BoundExitDoStatement exitDoStatement, List<LoweredInstruction> instructions)
    {
        if (_doLoopExitLabels.Count == 0)
        {
            ReportUnsupported(exitDoStatement, "EXIT DO");
            return;
        }

        LowerExitJump(exitDoStatement.Location, _doLoopExitLabels.Peek(), instructions);
    }

    private void LowerExitFor(BoundExitForStatement exitForStatement, List<LoweredInstruction> instructions)
    {
        if (_forExitLabels.Count == 0)
        {
            ReportUnsupported(exitForStatement, "EXIT FOR");
            return;
        }

        LowerExitJump(exitForStatement.Location, _forExitLabels.Peek(), instructions);
    }

    private void LowerExitJump(Axiom.Core.TextLocation location, LoopExitContext exitContext, List<LoweredInstruction> instructions)
    {
        for (var index = _activeFinallyContexts.Count - 1; index >= exitContext.FinallyDepth; index--)
        {
            var context = _activeFinallyContexts[index];
            instructions.Add(LoweredInstruction.PopErrorHandler(location));
            foreach (var statement in context.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }

        instructions.Add(LoweredInstruction.Jump(location, exitContext.Label));
    }

    private void LowerContinue(
        Axiom.Core.TextLocation location,
        string constructName,
        Stack<LoopExitContext> continueLabels,
        List<LoweredInstruction> instructions)
    {
        // Каждая форма CONTINUE смотрит только на циклы своего вида: при
        // смешанном вложении CONTINUE FOR обязан вернуться к ближайшему FOR.
        if (continueLabels.Count == 0)
        {
            ReportUnsupported(location, constructName);
            return;
        }

        var continueContext = continueLabels.Peek();

        for (var index = _activeFinallyContexts.Count - 1; index >= continueContext.FinallyDepth; index--)
        {
            var context = _activeFinallyContexts[index];
            instructions.Add(LoweredInstruction.PopErrorHandler(location));
            foreach (var statement in context.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }

        instructions.Add(LoweredInstruction.Jump(location, continueContext.Label));
    }

}
