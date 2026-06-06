using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed class BoundProgramLowerer
{
    private readonly List<BytecodeDiagnostic> _diagnostics = [];
    private readonly Stack<LoopExitContext> _doLoopExitLabels = [];
    private readonly Stack<LoopExitContext> _forExitLabels = [];
    private readonly List<FinallyContext> _activeFinallyContexts = [];
    private HashSet<VariableSymbol> _currentRoutineLocals = [];
    private bool _emitStatementBoundaries;
    private int _nextLabelId;

    private abstract class ByRefStoragePlan
    {
        public abstract void EmitLoadValue(List<LoweredInstruction> instructions);

        public abstract void EmitStoreValue(List<LoweredInstruction> instructions);
    }

    private sealed class VariableByRefStoragePlan : ByRefStoragePlan
    {
        private readonly VariableSymbol _variable;
        private readonly Axiom.Core.TextLocation _location;

        public VariableByRefStoragePlan(VariableSymbol variable, Axiom.Core.TextLocation location)
        {
            _variable = variable;
            _location = location;
        }

        public override void EmitLoadValue(List<LoweredInstruction> instructions)
        {
            instructions.Add(LoweredInstruction.LoadVariable(_location, _variable));
        }

        public override void EmitStoreValue(List<LoweredInstruction> instructions)
        {
            instructions.Add(LoweredInstruction.StoreVariable(_location, _variable));
        }
    }

    private sealed class FieldByRefStoragePlan : ByRefStoragePlan
    {
        private readonly ByRefStoragePlan _parent;
        private readonly string _fieldName;
        private readonly Axiom.Core.TextLocation _location;

        public FieldByRefStoragePlan(ByRefStoragePlan parent, string fieldName, Axiom.Core.TextLocation location)
        {
            _parent = parent;
            _fieldName = fieldName;
            _location = location;
        }

        public override void EmitLoadValue(List<LoweredInstruction> instructions)
        {
            _parent.EmitLoadValue(instructions);
            instructions.Add(LoweredInstruction.LoadField(_location, _fieldName));
        }

        public override void EmitStoreValue(List<LoweredInstruction> instructions)
        {
            _parent.EmitLoadValue(instructions);
            instructions.Add(LoweredInstruction.StoreField(_location, _fieldName));
        }
    }

    private sealed class ArrayElementByRefStoragePlan : ByRefStoragePlan
    {
        private readonly VariableSymbol _arrayTemporary;
        private readonly VariableSymbol[] _indexTemporaries;
        private readonly string _displayName;
        private readonly Axiom.Core.TextLocation _location;

        public ArrayElementByRefStoragePlan(
            VariableSymbol arrayTemporary,
            VariableSymbol[] indexTemporaries,
            string displayName,
            Axiom.Core.TextLocation location)
        {
            _arrayTemporary = arrayTemporary;
            _indexTemporaries = indexTemporaries;
            _displayName = displayName;
            _location = location;
        }

        public override void EmitLoadValue(List<LoweredInstruction> instructions)
        {
            instructions.Add(LoweredInstruction.LoadVariable(_location, _arrayTemporary));
            foreach (var indexTemporary in _indexTemporaries)
            {
                instructions.Add(LoweredInstruction.LoadVariable(_location, indexTemporary));
            }

            instructions.Add(LoweredInstruction.LoadElement(_location, _displayName, _indexTemporaries.Length));
        }

        public override void EmitStoreValue(List<LoweredInstruction> instructions)
        {
            instructions.Add(LoweredInstruction.LoadVariable(_location, _arrayTemporary));
            foreach (var indexTemporary in _indexTemporaries)
            {
                instructions.Add(LoweredInstruction.LoadVariable(_location, indexTemporary));
            }

            instructions.Add(LoweredInstruction.StoreElement(_location, _displayName, _indexTemporaries.Length));
        }
    }

    private sealed class ByRefCopyBackPlan
    {
        public ByRefCopyBackPlan(VariableSymbol tempVariable, Action<List<LoweredInstruction>> emitCopyBack)
        {
            TempVariable = tempVariable;
            EmitCopyBack = emitCopyBack;
        }

        public VariableSymbol TempVariable { get; }

        public Action<List<LoweredInstruction>> EmitCopyBack { get; }
    }

    private readonly record struct LoopExitContext(string Label, int FinallyDepth);

    private sealed class FinallyContext
    {
        public FinallyContext(Axiom.Core.TextLocation location, IReadOnlyList<BoundStatement> statements)
        {
            Location = location;
            Statements = statements;
        }

        public Axiom.Core.TextLocation Location { get; }

        public IReadOnlyList<BoundStatement> Statements { get; }
    }

    public LoweringResult Lower(BoundProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        _diagnostics.Clear();
        _doLoopExitLabels.Clear();
        _forExitLabels.Clear();
        _activeFinallyContexts.Clear();
        _emitStatementBoundaries = ContainsInputHandlerStatement(program);
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
                if (_emitStatementBoundaries)
                {
                    instructions.Add(LoweredInstruction.StatementBoundary(statement.Location));
                }

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
            case BoundSleepStatement sleep:
                if (sleep.DurationExpression is not null)
                {
                    LowerExpression(sleep.DurationExpression, instructions);
                }

                instructions.Add(LoweredInstruction.Sleep(sleep.Location, sleep.DurationExpression is not null));
                break;
            case BoundScreenStatement screen:
                LowerExpression(screen.ModeExpression, instructions);
                var screenValueCount = 1;
                if (screen.ActivePageExpression is not null)
                {
                    LowerExpression(screen.ActivePageExpression, instructions);
                    screenValueCount = 2;
                }

                if (screen.ActivePageExpression is null)
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(screen.Location, 0, TypeSymbol.Integer));
                }

                if (screen.VisualPageExpression is not null)
                {
                    if (screen.ActivePageExpression is null)
                    {
                        screenValueCount = 2;
                    }

                    LowerExpression(screen.VisualPageExpression, instructions);
                    screenValueCount = 3;
                }

                if (screen.VisualPageExpression is null)
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(screen.Location, 0, TypeSymbol.Integer));
                }

                instructions.Add(LoweredInstruction.LoadLiteral(screen.Location, screenValueCount, TypeSymbol.Integer));
                instructions.Add(LoweredInstruction.Screen(screen.Location, screenValueCount));
                break;
            case BoundCanvasStatement canvas:
                LowerModernGraphicsIntegralExpression(canvas.WidthExpression, instructions);
                LowerModernGraphicsIntegralExpression(canvas.HeightExpression, instructions);
                instructions.Add(LoweredInstruction.Canvas(canvas.Location));
                break;
            case BoundBeginFrameStatement beginFrame:
                instructions.Add(LoweredInstruction.BeginFrame(beginFrame.Location));
                break;
            case BoundPresentStatement present:
                instructions.Add(LoweredInstruction.Present(present.Location));
                break;
            case BoundCameraStatement camera:
                LowerModernGraphicsIntegralExpression(camera.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(camera.YExpression, instructions);
                instructions.Add(LoweredInstruction.Camera(camera.Location));
                break;
            case BoundResetCameraStatement resetCamera:
                instructions.Add(LoweredInstruction.ResetCamera(resetCamera.Location));
                break;
            case BoundPCopyStatement pcopy:
                LowerExpression(pcopy.SourcePageExpression, instructions);
                LowerExpression(pcopy.DestinationPageExpression, instructions);
                instructions.Add(LoweredInstruction.PCopy(pcopy.Location));
                break;
            case BoundViewStatement view:
                if (!view.IsReset)
                {
                    LowerExpression(view.LeftExpression!, instructions);
                    LowerExpression(view.TopExpression!, instructions);
                    LowerExpression(view.RightExpression!, instructions);
                    LowerExpression(view.BottomExpression!, instructions);
                    if (view.FillColorExpression is not null)
                    {
                        LowerExpression(view.FillColorExpression, instructions);
                    }

                    if (view.BorderColorExpression is not null)
                    {
                        LowerExpression(view.BorderColorExpression, instructions);
                    }
                }

                instructions.Add(LoweredInstruction.View(
                    view.Location,
                    view.UseScreenCoordinates,
                    hasCoordinates: !view.IsReset,
                    hasFillColor: view.FillColorExpression is not null,
                    hasBorderColor: view.BorderColorExpression is not null));
                break;
            case BoundViewPrintStatement viewPrint:
                if (!viewPrint.IsReset)
                {
                    LowerExpression(viewPrint.TopExpression!, instructions);
                    LowerExpression(viewPrint.BottomExpression!, instructions);
                }

                instructions.Add(LoweredInstruction.ViewPrint(viewPrint.Location, hasRows: !viewPrint.IsReset));
                break;
            case BoundWindowStatement window:
                if (!window.IsReset)
                {
                    LowerExpression(window.LeftExpression!, instructions);
                    LowerExpression(window.TopExpression!, instructions);
                    LowerExpression(window.RightExpression!, instructions);
                    LowerExpression(window.BottomExpression!, instructions);
                }

                instructions.Add(LoweredInstruction.Window(
                    window.Location,
                    window.UseScreenCoordinates,
                    hasCoordinates: !window.IsReset));
                break;
            case BoundClsStatement cls:
                if (cls.ColorExpression is not null)
                {
                    LowerExpression(cls.ColorExpression, instructions);
                }

                instructions.Add(LoweredInstruction.Cls(cls.Location, cls.ColorExpression is not null));
                break;
            case BoundColorStatement color:
                LowerExpression(color.ForegroundExpression, instructions);
                if (color.BackgroundMode == BoundColorBackgroundMode.Color)
                {
                    LowerExpression(color.BackgroundExpression!, instructions);
                }
                instructions.Add(LoweredInstruction.Color(color.Location, color.BackgroundMode));
                break;
            case BoundLocateStatement locate:
                LowerExpression(locate.RowExpression, instructions);
                LowerExpression(locate.ColumnExpression, instructions);
                instructions.Add(LoweredInstruction.Locate(locate.Location));
                break;
            case BoundPaletteStatement palette:
                if (palette.IsUsing)
                {
                    LowerExpression(palette.ValuesExpression!, instructions);
                }
                else if (!palette.IsReset)
                {
                    LowerExpression(palette.IndexExpression!, instructions);
                    LowerExpression(palette.ColorExpression!, instructions);
                }

                instructions.Add(LoweredInstruction.Palette(palette.Location, palette.IsUsing ? 1 : !palette.IsReset ? 2 : 0));
                break;
            case BoundPSetStatement pset:
                LowerExpression(pset.XExpression, instructions);
                LowerExpression(pset.YExpression, instructions);
                if (pset.ColorExpression is not null)
                {
                    LowerExpression(pset.ColorExpression, instructions);
                }
                instructions.Add(LoweredInstruction.PSet(pset.Location, pset.Reset, pset.ColorExpression is not null, pset.IsRelative));
                break;
            case BoundGetStatement get:
                LowerExpression(get.X1Expression, instructions);
                LowerExpression(get.Y1Expression, instructions);
                LowerExpression(get.X2Expression, instructions);
                LowerExpression(get.Y2Expression, instructions);
                LowerExpression(get.BufferExpression, instructions);
                instructions.Add(LoweredInstruction.Get(get.Location, get.IsStartRelative, get.IsEndRelative));
                break;
            case BoundPutStatement put:
                LowerExpression(put.XExpression, instructions);
                LowerExpression(put.YExpression, instructions);
                LowerExpression(put.BufferExpression, instructions);
                instructions.Add(LoweredInstruction.Put(put.Location, put.Action, put.IsRelative));
                break;
            case BoundLineStatement line:
                if (line.HasExplicitStartPoint)
                {
                    LowerExpression(line.X1Expression!, instructions);
                    LowerExpression(line.Y1Expression!, instructions);
                }

                LowerExpression(line.X2Expression, instructions);
                LowerExpression(line.Y2Expression, instructions);
                if (line.ColorExpression is not null)
                {
                    LowerExpression(line.ColorExpression, instructions);
                }
                instructions.Add(LoweredInstruction.Line(
                    line.Location,
                    line.BoxKind,
                    line.ColorExpression is not null,
                    line.HasExplicitStartPoint,
                    line.IsStartRelative,
                    line.IsEndRelative));
                break;
            case BoundCircleStatement circle:
                LowerExpression(circle.XExpression, instructions);
                LowerExpression(circle.YExpression, instructions);
                LowerExpression(circle.RadiusExpression, instructions);
                if (circle.ColorExpression is not null)
                {
                    LowerExpression(circle.ColorExpression, instructions);
                }
                if (circle.StartExpression is not null)
                {
                    LowerExpression(circle.StartExpression, instructions);
                }
                if (circle.EndExpression is not null)
                {
                    LowerExpression(circle.EndExpression, instructions);
                }
                if (circle.AspectExpression is not null)
                {
                    LowerExpression(circle.AspectExpression, instructions);
                }
                instructions.Add(LoweredInstruction.Circle(
                    circle.Location,
                    circle.IsRelative,
                    circle.ColorExpression is not null,
                    circle.StartExpression is not null,
                    circle.EndExpression is not null,
                    circle.AspectExpression is not null));
                break;
            case BoundPaintStatement paint:
                LowerExpression(paint.XExpression, instructions);
                LowerExpression(paint.YExpression, instructions);
                if (paint.FillColorExpression is not null)
                {
                    LowerExpression(paint.FillColorExpression, instructions);
                }
                if (paint.BorderColorExpression is not null)
                {
                    LowerExpression(paint.BorderColorExpression, instructions);
                }
                if (paint.BackgroundExpression is not null)
                {
                    LowerExpression(paint.BackgroundExpression, instructions);
                }
                instructions.Add(LoweredInstruction.Paint(
                    paint.Location,
                    paint.IsRelative,
                    paint.FillColorExpression is not null,
                    paint.BorderColorExpression is not null,
                    paint.BackgroundExpression is not null));
                break;
            case BoundDrawStatement draw:
                LowerExpression(draw.CommandExpression, instructions);
                instructions.Add(LoweredInstruction.Draw(draw.Location));
                break;
            case BoundDrawRectStatement drawRect:
                LowerModernGraphicsIntegralExpression(drawRect.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawRect.YExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawRect.WidthExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawRect.HeightExpression, instructions);
                LowerExpression(drawRect.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.DrawRect(drawRect.Location));
                break;
            case BoundFillRectStatement fillRect:
                LowerModernGraphicsIntegralExpression(fillRect.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(fillRect.YExpression, instructions);
                LowerModernGraphicsIntegralExpression(fillRect.WidthExpression, instructions);
                LowerModernGraphicsIntegralExpression(fillRect.HeightExpression, instructions);
                LowerExpression(fillRect.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.FillRect(fillRect.Location));
                break;
            case BoundDrawLineStatement drawLine:
                LowerModernGraphicsIntegralExpression(drawLine.X1Expression, instructions);
                LowerModernGraphicsIntegralExpression(drawLine.Y1Expression, instructions);
                LowerModernGraphicsIntegralExpression(drawLine.X2Expression, instructions);
                LowerModernGraphicsIntegralExpression(drawLine.Y2Expression, instructions);
                LowerExpression(drawLine.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.DrawLine(drawLine.Location));
                break;
            case BoundDrawCircleStatement drawCircle:
                LowerModernGraphicsIntegralExpression(drawCircle.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawCircle.YExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawCircle.RadiusExpression, instructions);
                LowerExpression(drawCircle.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.DrawCircle(drawCircle.Location));
                break;
            case BoundFillCircleStatement fillCircle:
                LowerModernGraphicsIntegralExpression(fillCircle.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(fillCircle.YExpression, instructions);
                LowerModernGraphicsIntegralExpression(fillCircle.RadiusExpression, instructions);
                LowerExpression(fillCircle.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.FillCircle(fillCircle.Location));
                break;
            case BoundDrawTextStatement drawText:
                LowerExpression(drawText.TextExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawText.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawText.YExpression, instructions);
                LowerExpression(drawText.ColorExpression, instructions);
                instructions.Add(LoweredInstruction.DrawText(drawText.Location));
                break;
            case BoundDrawImageStatement drawImage:
                LowerExpression(drawImage.ImageExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawImage.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawImage.YExpression, instructions);
                if (drawImage.HasScale)
                {
                    LowerModernGraphicsIntegralExpression(drawImage.WidthExpression!, instructions);
                    LowerModernGraphicsIntegralExpression(drawImage.HeightExpression!, instructions);
                }
                instructions.Add(LoweredInstruction.DrawImage(drawImage.Location, drawImage.HasScale));
                break;

            case BoundDrawSpriteStatement drawSprite:
                LowerExpression(drawSprite.SpriteExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawSprite.FrameExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawSprite.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawSprite.YExpression, instructions);
                if (drawSprite.HasScale)
                {
                    LowerModernGraphicsIntegralExpression(drawSprite.WidthExpression!, instructions);
                    LowerModernGraphicsIntegralExpression(drawSprite.HeightExpression!, instructions);
                }
                instructions.Add(LoweredInstruction.DrawSprite(drawSprite.Location, drawSprite.HasScale));
                break;

            case BoundOnKeyStatement onKey:
                LowerOnKey(onKey, instructions);
                break;

            case BoundOnMouseStatement onMouse:
                LowerOnMouse(onMouse, instructions);
                break;

            case BoundOpenStatement open:
                LowerOpen(open, instructions);
                break;

            case BoundCloseStatement close:
                LowerClose(close, instructions);
                break;

            case BoundErrorStatement error:
                LowerError(error, instructions);
                break;

            case BoundIfStatement ifStatement:
                LowerIf(ifStatement, instructions);
                break;

            case BoundTryStatement tryStatement:
                LowerTry(tryStatement, instructions);
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
                ReportUnsupported(statement, "оператор");
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
            ReportUnsupported(declaration, "объявление массива");
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

                ReportUnsupported(declaration, "объявление переменной без инициализатора этого типа");
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
            if (TryLowerStringAppendAssignment(assignment, directVariable.Variable, instructions))
            {
                return;
            }

            LowerExpression(assignment.Expression, instructions);
            instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, directVariable.Variable));
            return;
        }

        if (assignment.Target is BoundArrayAccessExpression arrayAccess)
        {
            LowerExpression(assignment.Expression, instructions);
            LowerExpression(arrayAccess.Target, instructions);
            foreach (var index in arrayAccess.Indices)
            {
                LowerExpression(index, instructions);
            }

            instructions.Add(LoweredInstruction.StoreElement(assignment.Location, arrayAccess.DisplayName, arrayAccess.Indices.Count));
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
            ReportUnsupported(assignment, "цель присваивания");
            return;
        }

        LowerExpression(assignment.Expression, instructions);
        instructions.Add(LoweredInstruction.StoreVariable(assignment.Location, variable.Variable));
    }

    private bool TryLowerStringAppendAssignment(
        BoundAssignmentStatement assignment,
        VariableSymbol targetVariable,
        List<LoweredInstruction> instructions)
    {
        if (targetVariable.Type != TypeSymbol.String
            || assignment.Expression is not BoundBinaryExpression binary
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

        LowerExpression(binary.Right, instructions);
        instructions.Add(LoweredInstruction.AppendStringVariable(assignment.Location, targetVariable, useDisplayString));
        return true;
    }

    private void LowerArrayDeclaration(BoundVariableDeclarationStatement declaration, List<LoweredInstruction> instructions)
    {
        if (declaration.Initializer is not null)
        {
            ReportUnsupported(declaration, "инициализатор массива");
            return;
        }

        if (declaration.ArrayBounds.Count == 0)
        {
            return;
        }

        instructions.Add(LoweredInstruction.LoadVariable(declaration.Location, declaration.Variable));
        foreach (var arrayBound in declaration.ArrayBounds)
        {
            LowerExpression(arrayBound.LowerBound, instructions);
            LowerExpression(arrayBound.UpperBound, instructions);
        }

        instructions.Add(LoweredInstruction.RedimArray(
            declaration.Location,
            declaration.Variable.Name,
            preserve: false,
            declaration.ArrayBounds.Count));
    }

    private void LowerErase(BoundEraseStatement erase, List<LoweredInstruction> instructions)
    {
        LowerExpression(erase.Target, instructions);
        instructions.Add(LoweredInstruction.EraseArray(erase.Location, erase.DisplayName));
    }

    private void LowerRedim(BoundRedimStatement redim, List<LoweredInstruction> instructions)
    {
        LowerExpression(redim.Target, instructions);
        foreach (var arrayBound in redim.ArrayBounds)
        {
            LowerExpression(arrayBound.LowerBound, instructions);
            LowerExpression(arrayBound.UpperBound, instructions);
        }

        instructions.Add(LoweredInstruction.RedimArray(
            redim.Location,
            redim.DisplayName,
            redim.Preserve,
            redim.ArrayBounds.Count));
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
        EmitLoopIterationBoundary(doLoopStatement.Location, instructions);

        _doLoopExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
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
            ReportUnsupported(doLoopStatement, "форма DO ... LOOP");
            return;
        }

        var startLabel = CreateLabel();
        var endLabel = CreateLabel();

        instructions.Add(LoweredInstruction.LabelMarker(doLoopStatement.Location, startLabel));
        EmitLoopIterationBoundary(doLoopStatement.Location, instructions);

        _doLoopExitLabels.Push(new LoopExitContext(endLabel, _activeFinallyContexts.Count));
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
                foreach (var index in arrayAccess.Indices)
                {
                    LowerArrayIndex(index, instructions);
                }

                instructions.Add(LoweredInstruction.LoadElement(
                    arrayAccess.Location,
                    arrayAccess.DisplayName,
                    arrayAccess.Indices.Count,
                    GetArrayIndexRangeMask(arrayAccess.Indices)));
                break;

            case BoundFieldAccessExpression fieldAccess:
                LowerExpression(fieldAccess.Target, instructions);
                instructions.Add(LoweredInstruction.LoadField(fieldAccess.Location, fieldAccess.Field.Name));
                break;

            case BoundRecordLiteralExpression recordLiteral:
                var recordTemporary = CreateRecordLiteralTemporary(recordLiteral);
                instructions.Add(LoweredInstruction.CreateRecord(recordLiteral.Location, recordLiteral.RecordType));
                instructions.Add(LoweredInstruction.StoreVariable(recordLiteral.Location, recordTemporary));
                foreach (var initializer in recordLiteral.Initializers)
                {
                    LowerExpression(initializer.Expression, instructions);
                    instructions.Add(LoweredInstruction.LoadVariable(recordLiteral.Location, recordTemporary));
                    instructions.Add(LoweredInstruction.StoreField(recordLiteral.Location, initializer.Field.Name));
                }
                instructions.Add(LoweredInstruction.LoadVariable(recordLiteral.Location, recordTemporary));
                break;

            case BoundTypeConstructorExpression typeConstructor:
                var constructorTemporary = CreateTypeConstructorTemporary(typeConstructor);
                instructions.Add(LoweredInstruction.CreateRecord(typeConstructor.Location, typeConstructor.RecordType));
                instructions.Add(LoweredInstruction.StoreVariable(typeConstructor.Location, constructorTemporary));

                var constructorArguments = new List<BoundExpression>(typeConstructor.Arguments.Count + 1)
                {
                    new BoundVariableExpression(typeConstructor.Location, constructorTemporary)
                };
                constructorArguments.AddRange(typeConstructor.Arguments);

                LowerCall(
                    new BoundCallExpression(
                        typeConstructor.Location,
                        new BoundFunctionExpression(typeConstructor.Location, typeConstructor.Constructor),
                        constructorArguments,
                        TypeSymbol.Void),
                    instructions);

                if (typeConstructor.RecordType.TryGetInvariant(out var invariant) && invariant is not null)
                {
                    LowerCall(
                        new BoundCallExpression(
                            typeConstructor.Location,
                            new BoundFunctionExpression(typeConstructor.Location, invariant),
                            [new BoundVariableExpression(typeConstructor.Location, constructorTemporary)],
                            TypeSymbol.Boolean),
                        instructions);
                    instructions.Add(LoweredInstruction.ValidateTypeInvariant(typeConstructor.Location, typeConstructor.RecordType.Name));
                }

                instructions.Add(LoweredInstruction.LoadVariable(typeConstructor.Location, constructorTemporary));
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
                ReportUnsupported(expression, "выражение");
                break;
        }
    }

    private void LowerCall(BoundCallExpression call, List<LoweredInstruction> instructions)
    {
        if (call.Target is BoundFunctionExpression function)
        {
            var byRefArgumentIndices = new List<int>();
            var byRefVariables = new List<VariableSymbol>();
            var byRefCopyBackPlans = new List<ByRefCopyBackPlan>();
            var byValArgumentCount = 0;

            for (var index = 0; index < call.Arguments.Count; index++)
            {
                var argument = call.Arguments[index];
                var parameter = index < function.Function.Parameters.Count
                    ? function.Function.Parameters[index]
                    : null;

                if (parameter?.IsByRef == true && argument is BoundVariableExpression variableArgument)
                {
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(variableArgument.Variable);
                    continue;
                }

                if (argument is not (BoundVariableExpression or BoundFieldAccessExpression or BoundArrayAccessExpression)
                    && IsTemporaryTypeMethodSelfArgument(function.Function, parameter, index))
                {
                    var receiverTemporary = CreateInternalTemporary(argument.Type, "__type_method_self");
                    LowerExpression(argument, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(argument.Location, receiverTemporary));
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(receiverTemporary);
                    continue;
                }

                if (parameter?.IsByRef == true
                    && argument is not BoundVariableExpression
                    && TryLowerByRefCopyBackArgument(argument, instructions, out var copyBackPlan))
                {
                    byRefArgumentIndices.Add(index);
                    byRefVariables.Add(copyBackPlan.TempVariable);
                    byRefCopyBackPlans.Add(copyBackPlan);
                    continue;
                }

                LowerExpression(argument, instructions);
                byValArgumentCount++;
            }

            instructions.Add(byRefVariables.Count == 0
                ? LoweredInstruction.CallFunction(call.Location, function.Function)
                : LoweredInstruction.CallFunctionByRef(
                    call.Location,
                    function.Function,
                    byValArgumentCount,
                    byRefArgumentIndices,
                    byRefVariables));

            foreach (var copyBackPlan in byRefCopyBackPlans)
            {
                copyBackPlan.EmitCopyBack(instructions);
            }
            return;
        }

        LowerExpression(call.Target, instructions);

        foreach (var argument in call.Arguments)
        {
            LowerExpression(argument, instructions);
        }

        instructions.Add(LoweredInstruction.CallValue(call.Location, call.Arguments.Count, call.Type));
    }

    private void LowerModernGraphicsIntegralExpression(BoundExpression expression, List<LoweredInstruction> instructions)
    {
        if (expression is BoundConversionExpression conversion
            && conversion.Type == TypeSymbol.Long
            && (conversion.Expression.Type == TypeSymbol.Integer || conversion.Expression.Type == TypeSymbol.Long))
        {
            LowerExpression(conversion.Expression, instructions);
            return;
        }

        LowerExpression(expression, instructions);
    }

    private static bool IsTemporaryTypeMethodSelfArgument(
        FunctionSymbol function,
        ParameterSymbol? parameter,
        int argumentIndex)
    {
        ArgumentNullException.ThrowIfNull(function);

        return argumentIndex == 0
            && parameter?.IsByRef == true
            && parameter.Type is UserDefinedTypeSymbol
            && string.Equals(parameter.Name, "SELF", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(function.Name, function.ShortName, StringComparison.Ordinal);
    }

    private bool TryLowerByRefCopyBackArgument(
        BoundExpression argument,
        List<LoweredInstruction> instructions,
        out ByRefCopyBackPlan plan)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(instructions);

        if (!TryCreateByRefStoragePlan(argument, instructions, out var storagePlan))
        {
            plan = null!;
            return false;
        }

        var receiverTemporary = CreateInternalTemporary(argument.Type, "__byref_value");
        storagePlan.EmitLoadValue(instructions);
        instructions.Add(LoweredInstruction.StoreVariable(argument.Location, receiverTemporary));

        plan = new ByRefCopyBackPlan(
            receiverTemporary,
            copyBackInstructions =>
            {
                copyBackInstructions.Add(LoweredInstruction.LoadVariable(argument.Location, receiverTemporary));
                storagePlan.EmitStoreValue(copyBackInstructions);
            });
        return true;
    }

    private bool TryCreateByRefStoragePlan(
        BoundExpression argument,
        List<LoweredInstruction> instructions,
        out ByRefStoragePlan plan)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(instructions);

        switch (argument)
        {
            case BoundVariableExpression variableExpression:
                plan = new VariableByRefStoragePlan(variableExpression.Variable, variableExpression.Location);
                return true;

            case BoundFieldAccessExpression fieldAccess when TryCreateByRefStoragePlan(fieldAccess.Target, instructions, out var parentPlan):
                plan = new FieldByRefStoragePlan(parentPlan, fieldAccess.Field.Name, fieldAccess.Location);
                return true;

            case BoundArrayAccessExpression arrayAccess:
            {
                var arrayTemporary = CreateInternalTemporary(arrayAccess.Target.Type, "__byref_array");
                var indexTemporaries = new VariableSymbol[arrayAccess.Indices.Count];

                LowerExpression(arrayAccess.Target, instructions);
                instructions.Add(LoweredInstruction.StoreVariable(arrayAccess.Location, arrayTemporary));

                for (var index = 0; index < arrayAccess.Indices.Count; index++)
                {
                    var indexExpression = arrayAccess.Indices[index];
                    var indexTemporary = CreateInternalTemporary(indexExpression.Type, $"__byref_index_{index}");
                    indexTemporaries[index] = indexTemporary;
                    LowerExpression(indexExpression, instructions);
                    instructions.Add(LoweredInstruction.StoreVariable(indexExpression.Location, indexTemporary));
                }

                plan = new ArrayElementByRefStoragePlan(
                    arrayTemporary,
                    indexTemporaries,
                    arrayAccess.DisplayName,
                    arrayAccess.Location);
                return true;
            }

            default:
                plan = null!;
                return false;
        }
    }

    private void LowerArrayIndex(BoundExpression index, List<LoweredInstruction> instructions)
    {
        if (index is BoundArrayRangeExpression range)
        {
            LowerExpression(range.LowerBound, instructions);
            LowerExpression(range.UpperBound, instructions);
            return;
        }

        LowerExpression(index, instructions);
    }

    private static int GetArrayIndexRangeMask(IReadOnlyList<BoundExpression> indices)
    {
        var mask = 0;

        for (var index = 0; index < indices.Count; index++)
        {
            if (indices[index] is BoundArrayRangeExpression)
            {
                mask |= 1 << index;
            }
        }

        return mask;
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

    private VariableSymbol CreateRecordLiteralTemporary(BoundRecordLiteralExpression recordLiteral)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__record_literal_{_currentRoutineLocals.Count}_{recordLiteral.RecordType.Name}"),
            recordLiteral.RecordType,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateTypeConstructorTemporary(BoundTypeConstructorExpression typeConstructor)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__type_constructor_{_currentRoutineLocals.Count}_{typeConstructor.RecordType.Name}"),
            typeConstructor.RecordType,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateInternalTemporary(TypeSymbol type, string stem)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);

        var variable = new VariableSymbol(
            FormattableString.Invariant($"{stem}_{_currentRoutineLocals.Count}"),
            type,
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
        return type is UserDefinedTypeSymbol or FunctionTypeSymbol || type == TypeSymbol.File || type == TypeSymbol.Image || type == TypeSymbol.Sprite;
    }

    private void ReportUnsupported(BoundNode node, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"Понижение пока не поддерживает {category}: {node.Kind}.",
            node.Location));
    }

    private void ReportUnsupportedSelector(BoundSelectCaseSelector selector, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"Понижение пока не поддерживает {category}: {selector.GetType().Name}.",
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

            case BoundTryStatement tryStatement:
                if (tryStatement.CatchVariable is not null)
                {
                    variables.Add(tryStatement.CatchVariable);
                }

                CollectDeclaredVariables(tryStatement.Statements, variables);
                CollectDeclaredVariables(tryStatement.CatchStatements, variables);
                CollectDeclaredVariables(tryStatement.FinallyStatements, variables);
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
