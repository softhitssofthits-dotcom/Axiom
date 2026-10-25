using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private readonly List<BytecodeDiagnostic> _diagnostics = [];
    private readonly Stack<LoopExitContext> _doLoopExitLabels = [];
    private readonly Stack<LoopExitContext> _forExitLabels = [];

    // Продолжение привязано к виду цикла: CONTINUE FOR внутри WHILE должен
    // возвращаться к ближайшему FOR, а не к объемлющему WHILE.
    private readonly Stack<LoopExitContext> _forContinueLabels = [];
    private readonly Stack<LoopExitContext> _whileContinueLabels = [];
    private readonly Stack<LoopExitContext> _doContinueLabels = [];
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
        _forContinueLabels.Clear();
        _whileContinueLabels.Clear();
        _doContinueLabels.Clear();
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

                if (instructions.Count > 0 && IsUnconditionalControlTransfer(instructions[^1]))
                {
                    break;
                }
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
            case BoundRandomizeStatement randomize:
                if (randomize.SeedExpression is not null)
                {
                    LowerExpression(randomize.SeedExpression, instructions);
                }

                instructions.Add(LoweredInstruction.ReseedRandom(randomize.Location, randomize.SeedExpression is not null));
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
            case BoundSmoothStatement smooth:
                instructions.Add(LoweredInstruction.Smooth(smooth.Location, smooth.Enabled));
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
            case BoundViewportStatement viewport:
                if (!viewport.IsReset)
                {
                    LowerModernGraphicsIntegralExpression(viewport.LeftExpression!, instructions);
                    LowerModernGraphicsIntegralExpression(viewport.TopExpression!, instructions);
                    LowerModernGraphicsIntegralExpression(viewport.RightExpression!, instructions);
                    LowerModernGraphicsIntegralExpression(viewport.BottomExpression!, instructions);
                }
                instructions.Add(LoweredInstruction.Viewport(viewport.Location, !viewport.IsReset));
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

            case BoundDrawTileStatement drawTile:
                LowerExpression(drawTile.TilesetExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTile.TileIndexExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTile.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTile.YExpression, instructions);
                instructions.Add(LoweredInstruction.DrawTile(drawTile.Location));
                break;

            case BoundDrawTilemapStatement drawTilemap:
                LowerExpression(drawTilemap.TilemapExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTilemap.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTilemap.YExpression, instructions);
                instructions.Add(LoweredInstruction.DrawTilemap(drawTilemap.Location));
                break;

            case BoundDrawTileLayerStatement drawTileLayer:
                LowerExpression(drawTileLayer.TilemapExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTileLayer.LayerIndexExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTileLayer.XExpression, instructions);
                LowerModernGraphicsIntegralExpression(drawTileLayer.YExpression, instructions);
                instructions.Add(LoweredInstruction.DrawTileLayer(drawTileLayer.Location));
                break;

            case BoundSetMapTileStatement setMapTile:
                LowerExpression(setMapTile.TilemapExpression, instructions);
                LowerModernGraphicsIntegralExpression(setMapTile.LayerIndexExpression, instructions);
                LowerModernGraphicsIntegralExpression(setMapTile.ColumnExpression, instructions);
                LowerModernGraphicsIntegralExpression(setMapTile.RowExpression, instructions);
                LowerModernGraphicsIntegralExpression(setMapTile.TilesetIndexExpression, instructions);
                LowerModernGraphicsIntegralExpression(setMapTile.TileIndexExpression, instructions);
                if (setMapTile.FlagsExpression is null)
                {
                    instructions.Add(LoweredInstruction.LoadLiteral(setMapTile.Location, 0L, TypeSymbol.Long));
                }
                else
                {
                    LowerModernGraphicsIntegralExpression(setMapTile.FlagsExpression, instructions);
                }
                instructions.Add(LoweredInstruction.SetMapTile(setMapTile.Location));
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

            case BoundForEachStatement forEachStatement:
                LowerForEach(forEachStatement, instructions);
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

            case BoundContinueForStatement continueForStatement:
                LowerContinue(continueForStatement.Location, "CONTINUE FOR", _forContinueLabels, instructions);
                break;

            case BoundContinueWhileStatement continueWhileStatement:
                LowerContinue(continueWhileStatement.Location, "CONTINUE WHILE", _whileContinueLabels, instructions);
                break;

            case BoundContinueDoStatement continueDoStatement:
                LowerContinue(continueDoStatement.Location, "CONTINUE DO", _doContinueLabels, instructions);
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

}
