using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed class BoundProgramLowerer
{
    private readonly List<BytecodeDiagnostic> _diagnostics = [];
    private readonly Stack<string> _doLoopExitLabels = [];
    private readonly Stack<string> _forExitLabels = [];
    private HashSet<VariableSymbol> _currentRoutineLocals = [];
    private int _nextLabelId;

    public LoweringResult Lower(BoundProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        _diagnostics.Clear();
        _doLoopExitLabels.Clear();
        _forExitLabels.Clear();
        _nextLabelId = 0;

        var entryPoint = LowerRoutine(
            id: 0,
            name: LoweredProgram.EntryPointName,
            symbol: null,
            resultType: TypeSymbol.Void,
            isEntryPoint: true,
            statements: program.GlobalStatements,
            location: program.Location);

        var routines = new List<LoweredRoutine>(program.Routines.Count);
        for (var index = 0; index < program.Routines.Count; index++)
        {
            var routine = program.Routines[index];
            routines.Add(LowerRoutine(
                id: index + 1,
                name: routine.Symbol.Name,
                symbol: routine.Symbol,
                resultType: routine.Symbol.ReturnType,
                isEntryPoint: false,
                statements: routine.Statements,
                location: routine.Location));
        }

        if (_diagnostics.Count > 0)
        {
            return new LoweringResult(null, _diagnostics.ToArray());
        }

        return new LoweringResult(new LoweredProgram(program.Location, entryPoint, routines), Array.Empty<BytecodeDiagnostic>());
    }

    private LoweredRoutine LowerRoutine(
        int id,
        string name,
        FunctionSymbol? symbol,
        TypeSymbol resultType,
        bool isEntryPoint,
        IReadOnlyList<BoundStatement> statements,
        Axiom.Core.TextLocation location)
    {
        var instructions = new List<LoweredInstruction>();
        var previousRoutineLocals = _currentRoutineLocals;
        _currentRoutineLocals = CollectDeclaredVariables(statements);

        try
        {
            foreach (var statement in statements)
            {
                LowerStatement(statement, instructions);
            }

            if (instructions.Count == 0 || instructions[^1].Kind != LoweredInstructionKind.Return)
            {
                instructions.Add(LoweredInstruction.Return(location));
            }

            return new LoweredRoutine(id, name, location, resultType, instructions, symbol, isEntryPoint, _currentRoutineLocals.ToArray());
        }
        finally
        {
            _currentRoutineLocals = previousRoutineLocals;
        }
    }

    private void LowerStatement(BoundStatement statement, List<LoweredInstruction> instructions)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    LowerStatement(declaration, instructions);
                }

                break;

            case BoundVariableDeclarationStatement declaration:
                LowerVariableDeclaration(declaration, instructions);
                break;

            case BoundAssignmentStatement assignment:
                LowerAssignment(assignment, instructions);
                break;

            case BoundEraseStatement erase:
                LowerErase(erase, instructions);
                break;

            case BoundRedimStatement redim:
                LowerRedim(redim, instructions);
                break;

            case BoundExpressionStatement expressionStatement:
                LowerExpression(expressionStatement.Expression, instructions);
                if (expressionStatement.Expression.Type != TypeSymbol.Void)
                {
                    instructions.Add(LoweredInstruction.Pop(expressionStatement.Location));
                }
                break;

            case BoundPrintStatement print:
                LowerPrint(print, instructions);
                break;

            case BoundPrintUsingStatement printUsing:
                LowerPrintUsing(printUsing, instructions);
                break;

            case BoundInputStatement input:
                LowerInput(input, instructions);
                break;

            case BoundOpenStatement open:
                LowerOpen(open, instructions);
                break;

            case BoundCloseStatement close:
                LowerClose(close, instructions);
                break;

            case BoundIfStatement ifStatement:
                LowerIf(ifStatement, instructions);
                break;

            case BoundSelectCaseStatement selectCaseStatement:
                LowerSelectCase(selectCaseStatement, instructions);
                break;

            case BoundWhileStatement whileStatement:
                LowerWhile(whileStatement, instructions);
                break;

            case BoundForStatement forStatement:
                LowerFor(forStatement, instructions);
                break;

            case BoundDoLoopStatement doLoopStatement:
                LowerDoLoop(doLoopStatement, instructions);
                break;

            case BoundExitDoStatement exitDoStatement:
                LowerExitDo(exitDoStatement, instructions);
                break;

            case BoundExitForStatement exitForStatement:
                LowerExitFor(exitForStatement, instructions);
                break;

            case BoundReturnStatement returnStatement:
                if (returnStatement.Expression is not null)
                {
                    LowerExpression(returnStatement.Expression, instructions);
                }

                instructions.Add(LoweredInstruction.Return(returnStatement.Location));
                break;

            default:
                ReportUnsupported(statement, "РѕРїРµСЂР°С‚РѕСЂ");
                break;
        }
    }

    private void LowerVariableDeclaration(BoundVariableDeclarationStatement declaration, List<LoweredInstruction> instructions)
    {
        if (declaration.Variable.IsArray)
        {
            LowerArrayDeclaration(declaration, instructions);
            return;
        }

        if (declaration.LowerBound is not null || declaration.UpperBound is not null)
        {
            ReportUnsupported(declaration, "РѕР±СЉСЏРІР»РµРЅРёРµ РјР°СЃСЃРёРІР°");
            return;
        }

        if (declaration.Initializer is null)
        {
            if (!TryCreateDefaultLiteral(declaration.Variable.Type, out var defaultValue))
            {
                if (CanUseImplicitDefaultSlotValue(declaration.Variable.Type))
                {
                    return;
                }

                ReportUnsupported(declaration, "РѕР±СЉСЏРІР»РµРЅРёРµ РїРµСЂРµРјРµРЅРЅРѕР№ Р±РµР· РёРЅРёС†РёР°Р»РёР·Р°С‚РѕСЂР° СЌС‚РѕРіРѕ С‚РёРїР°");
                return;
            }

            instructions.Add(LoweredInstruction.LoadLiteral(declaration.Location, defaultValue, declaration.Variable.Type));
            instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
            return;
        }

        LowerExpression(declaration.Initializer, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(declaration.Location, declaration.Variable));
    }

    private void LowerAssignment(BoundAssignmentStatement assignment, List<LoweredInstruction> instructions)
    {
        if (assignment.Target is BoundVariableExpression directVariable)
        {
            LowerExpression(assignment.Expression, instructions);
            instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, directVariable.Variable));
            return;
        }

        if (assignment.Target is BoundArrayAccessExpression arrayAccess)
        {
            LowerExpression(assignment.Expression, instructions);
            LowerExpression(arrayAccess.Target, instructions);
            LowerExpression(arrayAccess.Index, instructions);
            instructions.Add(LoweredInstruction.StoreElement(assignment.Location, arrayAccess.DisplayName));
            return;
        }

        if (assignment.Target is BoundFieldAccessExpression fieldAccess)
        {
            LowerExpression(assignment.Expression, instructions);
            LowerExpression(fieldAccess.Target, instructions);
            instructions.Add(LoweredInstruction.StoreField(assignment.Location, fieldAccess.Field.Name));
            return;
        }

        if (assignment.Target is not BoundVariableExpression variable)
        {
            ReportUnsupported(assignment, "С†РµР»СЊ РїСЂРёСЃРІР°РёРІР°РЅРёСЏ");
            return;
        }

        LowerExpression(assignment.Expression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, variable.Variable));
    }

    private void LowerArrayDeclaration(BoundVariableDeclarationStatement declaration, List<LoweredInstruction> instructions)
    {
        if (declaration.Initializer is not null)
        {
            ReportUnsupported(declaration, "РёРЅРёС†РёР°Р»РёР·Р°С‚РѕСЂ РјР°СЃСЃРёРІР°");
            return;
        }

        if (declaration.LowerBound is null && declaration.UpperBound is null)
        {
            return;
        }

        if (declaration.LowerBound is null || declaration.UpperBound is null)
        {
            ReportUnsupported(declaration, "РЅРµРїРѕР»РЅС‹Рµ РіСЂР°РЅРёС†С‹ РјР°СЃСЃРёРІР°");
            return;
        }

        instructions.Add(LoweredInstruction.LoadVariable(declaration.Location, declaration.Variable));
        LowerExpression(declaration.LowerBound, instructions);
        LowerExpression(declaration.UpperBound, instructions);
        instructions.Add(LoweredInstruction.RedimArray(declaration.Location, declaration.Variable.Name, preserve: false));
    }

    private void LowerErase(BoundEraseStatement erase, List<LoweredInstruction> instructions)
    {
        LowerExpression(erase.Target, instructions);
        instructions.Add(LoweredInstruction.EraseArray(erase.Location, erase.DisplayName));
    }

    private void LowerRedim(BoundRedimStatement redim, List<LoweredInstruction> instructions)
    {
        LowerExpression(redim.Target, instructions);
        LowerExpression(redim.LowerBound, instructions);
        LowerExpression(redim.UpperBound, instructions);
        instructions.Add(LoweredInstruction.RedimArray(redim.Location, redim.DisplayName, redim.Preserve));
    }

    private void LowerPrint(BoundPrintStatement print, List<LoweredInstruction> instructions)
    {
        foreach (var item in print.Items)
        {
            LowerExpression(item.Expression, instructions);
            instructions.Add(LoweredInstruction.PrintValue(item.Expression.Location));

            if (item.SeparatorKind is { } separatorKind)
            {
                instructions.Add(LoweredInstruction.PrintSeparator(item.Expression.Location, separatorKind));
            }
        }

        var trailingSeparator = print.Items.Count == 0
            ? null
            : print.Items[^1].SeparatorKind;

        if (trailingSeparator is null)
        {
            instructions.Add(LoweredInstruction.PrintLine(print.Location));
        }
    }

    private void LowerPrintUsing(BoundPrintUsingStatement printUsing, List<LoweredInstruction> instructions)
    {
        LowerExpression(printUsing.FormatExpression, instructions);

        foreach (var item in printUsing.Items)
        {
            LowerExpression(item.Expression, instructions);
        }

        var trailingSeparator = printUsing.Items.Count == 0
            ? null
            : printUsing.Items[^1].SeparatorKind;

        instructions.Add(LoweredInstruction.PrintUsing(printUsing.Location, printUsing.Items.Count, trailingSeparator));
    }

    private static void LowerInput(BoundInputStatement input, List<LoweredInstruction> instructions)
    {
        instructions.Add(LoweredInstruction.Input(input.Location, input.Variables, input.PromptText));
    }

    private void LowerOpen(BoundOpenStatement open, List<LoweredInstruction> instructions)
    {
        LowerExpression(open.PathExpression, instructions);
        instructions.Add(LoweredInstruction.LoadVariable(open.Location, open.FileVariable));
        instructions.Add(LoweredInstruction.OpenFile(open.Location, open.Mode, open.FileVariable.Name));
        instructions.Add(LoweredInstruction.StoreVariable(open.Location, open.FileVariable));
    }

    private void LowerClose(BoundCloseStatement close, List<LoweredInstruction> instructions)
    {
        instructions.Add(LoweredInstruction.LoadVariable(close.Location, close.FileVariable));
        instructions.Add(LoweredInstruction.CloseFile(close.Location));
        instructions.Add(LoweredInstruction.StoreVariable(close.Location, close.FileVariable));
    }

    private void LowerIf(BoundIfStatement ifStatement, List<LoweredInstruction> instructions)
    {
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
                ReportUnsupportedSelector(selector, "СЃРµР»РµРєС‚РѕСЂ SELECT CASE");
                break;
        }
    }

    private void LowerWhile(BoundWhileStatement whileStatement, List<LoweredInstruction> instructions)
    {
        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(whileStatement.Location, startLabel));
        LowerExpression(whileStatement.Condition, instructions);
        instructions.Add(LoweredInstruction.JumpIfFalse(whileStatement.Condition.Location, endLabel));

        foreach (var statement in whileStatement.Statements)
        {
            LowerStatement(statement, instructions);
        }

        instructions.Add(LoweredInstruction.Jump(whileStatement.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(whileStatement.Location, endLabel));
    }

    private void LowerFor(BoundForStatement forStatement, List<LoweredInstruction> instructions)
    {
        var checkLabel = CreateLabel();
        var negativeStepLabel = CreateLabel();
        var bodyLabel = CreateLabel();
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

        _forExitLabels.Push(endLabel);
        try
        {
            foreach (var statement in forStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _forExitLabels.Pop();
        }

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
                ReportUnsupported(doLoopStatement, "РЎвЂћР С•РЎР‚Р СР В° DO ... LOOP");
                return;
            }

            LowerInfiniteDoLoop(doLoopStatement, instructions);
            return;
        }

        if (doLoopStatement.Condition is null
            || doLoopStatement.ConditionKind is not (BoundDoLoopConditionKind.While or BoundDoLoopConditionKind.Until))
        {
            ReportUnsupported(doLoopStatement, "С„РѕСЂРјР° DO ... LOOP");
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
            ReportUnsupported(doLoopStatement, "С„РѕСЂРјР° DO ... LOOP");
            return;
        }

        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));
        LowerExpression(condition, instructions);
        instructions.Add(doLoopStatement.ConditionKind == BoundDoLoopConditionKind.While
            ? LoweredInstruction.JumpIfFalse(condition.Location, endLabel)
            : LoweredInstruction.JumpIfTrue(condition.Location, endLabel));

        _doLoopExitLabels.Push(endLabel);
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
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

        _doLoopExitLabels.Push(endLabel);
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
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
            ReportUnsupported(doLoopStatement, "С„РѕСЂРјР° DO ... LOOP");
            return;
        }

        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));

        _doLoopExitLabels.Push(endLabel);
        try
        {
            foreach (var statement in doLoopStatement.Statements)
            {
                LowerStatement(statement, instructions);
            }
        }
        finally
        {
            _doLoopExitLabels.Pop();
        }

        LowerExpression(condition, instructions);
        instructions.Add(doLoopStatement.ConditionKind == BoundDoLoopConditionKind.While
            ? LoweredInstruction.JumpIfTrue(condition.Location, startLabel)
            : LoweredInstruction.JumpIfFalse(condition.Location, startLabel));
        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, endLabel));
    }

    private void LowerExitDo(BoundExitDoStatement exitDoStatement, List<LoweredInstruction> instructions)
    {
        if (_doLoopExitLabels.Count == 0)
        {
            ReportUnsupported(exitDoStatement, "EXIT DO");
            return;
        }

        instructions.Add(LoweredInstruction.Jump(exitDoStatement.Location, _doLoopExitLabels.Peek()));
    }

    private void LowerExitFor(BoundExitForStatement exitForStatement, List<LoweredInstruction> instructions)
    {
        if (_forExitLabels.Count == 0)
        {
            ReportUnsupported(exitForStatement, "EXIT FOR");
            return;
        }

        instructions.Add(LoweredInstruction.Jump(exitForStatement.Location, _forExitLabels.Peek()));
    }

    private void LowerExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        switch (expression)
        {
            case BoundLiteralExpression literal:
                instructions.Add(LoweredInstruction.LoadLiteral(literal.Location, literal.Value, literal.Type));
                break;

            case BoundFunctionExpression function:
                instructions.Add(LoweredInstruction.LoadFunction(function.Location, function.Function));
                break;

            case BoundVariableExpression variable:
                instructions.Add(LoweredInstruction.LoadVariable(variable.Location, variable.Variable));
                break;

            case BoundArrayAccessExpression arrayAccess:
                LowerExpression(arrayAccess.Target, instructions);
                LowerExpression(arrayAccess.Index, instructions);
                instructions.Add(LoweredInstruction.LoadElement(arrayAccess.Location, arrayAccess.DisplayName));
                break;

            case BoundFieldAccessExpression fieldAccess:
                LowerExpression(fieldAccess.Target, instructions);
                instructions.Add(LoweredInstruction.LoadField(fieldAccess.Location, fieldAccess.Field.Name));
                break;

            case BoundUnaryExpression unary:
                LowerExpression(unary.Operand, instructions);
                instructions.Add(LoweredInstruction.UnaryOperation(unary.Location, unary.OperatorKind));
                break;

            case BoundBinaryExpression binary:
                LowerExpression(binary.Left, instructions);
                LowerExpression(binary.Right, instructions);
                instructions.Add(LoweredInstruction.BinaryOperation(binary.Location, binary.OperatorKind));
                break;

            case BoundConversionExpression conversion:
                LowerExpression(conversion.Expression, instructions);
                instructions.Add(LoweredInstruction.Convert(conversion.Location, conversion.Type));
                break;

            case BoundCallExpression call:
                LowerCall(call, instructions);
                break;

            default:
                ReportUnsupported(expression, "РІС‹СЂР°Р¶РµРЅРёРµ");
                break;
        }
    }

    private void LowerCall(BoundCallExpression call, List<LoweredInstruction> instructions)
    {
        if (call.Target is BoundFunctionExpression function)
        {
            foreach (var argument in call.Arguments)
            {
                LowerExpression(argument, instructions);
            }

            instructions.Add(LoweredInstruction.CallFunction(call.Location, function.Function));
            return;
        }

        LowerExpression(call.Target, instructions);

        foreach (var argument in call.Arguments)
        {
            LowerExpression(argument, instructions);
        }

        instructions.Add(LoweredInstruction.CallValue(call.Location, call.Arguments.Count, call.Type));
    }

    private string CreateLabel()
    {
        _nextLabelId++;
        return FormattableString.Invariant($"L{_nextLabelId:0000}");
    }

    private VariableSymbol CreateForTemporary(BoundForStatement forStatement, string role, string checkLabel)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__for_{forStatement.Variable.Name}_{role}_{checkLabel}"),
            forStatement.Variable.Type,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateSelectCaseTemporary(BoundSelectCaseStatement selectCaseStatement, string endLabel)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__select_case_test_{endLabel}"),
            selectCaseStatement.TestExpression.Type,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private static object CreateZeroLiteral(TypeSymbol type)
    {
        if (type == TypeSymbol.Double)
        {
            return 0d;
        }

        if (type == TypeSymbol.Long)
        {
            return 0L;
        }

        return 0;
    }

    private static bool TryCreateDefaultLiteral(TypeSymbol type, out object value)
    {
        if (type == TypeSymbol.Integer)
        {
            value = 0;
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            value = 0L;
            return true;
        }

        if (type == TypeSymbol.Double)
        {
            value = 0d;
            return true;
        }

        if (type == TypeSymbol.Decimal)
        {
            value = 0m;
            return true;
        }

        if (type == TypeSymbol.String)
        {
            value = string.Empty;
            return true;
        }

        if (type == TypeSymbol.Boolean)
        {
            value = false;
            return true;
        }

        value = null!;
        return false;
    }

    private static bool CanUseImplicitDefaultSlotValue(TypeSymbol type)
    {
        return type is UserDefinedTypeSymbol or FunctionTypeSymbol || type == TypeSymbol.File;
    }

    private void ReportUnsupported(BoundNode node, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"РџРѕРЅРёР¶РµРЅРёРµ РїРѕРєР° РЅРµ РїРѕРґРґРµСЂР¶РёРІР°РµС‚ {category}: {node.Kind}.",
            node.Location));
    }

    private void ReportUnsupportedSelector(BoundSelectCaseSelector selector, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"РџРѕРЅРёР¶РµРЅРёРµ РїРѕРєР° РЅРµ РїРѕРґРґРµСЂР¶РёРІР°РµС‚ {category}: {selector.GetType().Name}.",
            selector.Location));
    }

    private static HashSet<VariableSymbol> CollectDeclaredVariables(IReadOnlyList<BoundStatement> statements)
    {
        var variables = new HashSet<VariableSymbol>();
        CollectDeclaredVariables(statements, variables);
        return variables;
    }

    private static void CollectDeclaredVariables(IReadOnlyList<BoundStatement> statements, HashSet<VariableSymbol> variables)
    {
        foreach (var statement in statements)
        {
            CollectDeclaredVariables(statement, variables);
        }
    }

    private static void CollectDeclaredVariables(BoundStatement statement, HashSet<VariableSymbol> variables)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    variables.Add(declaration.Variable);
                }
                break;

            case BoundVariableDeclarationStatement declaration:
                variables.Add(declaration.Variable);
                break;

            case BoundIfStatement ifStatement:
                CollectDeclaredVariables(ifStatement.Statements, variables);
                CollectDeclaredVariables(ifStatement.ElseStatements, variables);
                break;

            case BoundWhileStatement whileStatement:
                CollectDeclaredVariables(whileStatement.Statements, variables);
                break;

            case BoundDoLoopStatement doLoopStatement:
                CollectDeclaredVariables(doLoopStatement.Statements, variables);
                break;

            case BoundForStatement forStatement:
                variables.Add(forStatement.Variable);
                CollectDeclaredVariables(forStatement.Statements, variables);
                break;

            case BoundSelectCaseStatement selectCaseStatement:
                foreach (var clause in selectCaseStatement.Clauses)
                {
                    CollectDeclaredVariables(clause.Statements, variables);
                }

                CollectDeclaredVariables(selectCaseStatement.ElseStatements, variables);
                break;
        }
    }
}
