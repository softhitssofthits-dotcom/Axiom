using Axiom.Core;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    [Flags]
    private enum GraphicsModeKnowledge { Unknown = 1, Unavailable = 2, Text = 4, Screen = 8, Canvas = 16 }

    private sealed record GraphicsFlow(
        GraphicsModeKnowledge? Next,
        GraphicsModeKnowledge? Returned = null,
        GraphicsModeKnowledge? ExitedLoop = null,
        GraphicsModeKnowledge? ContinuedLoop = null);

    private void CheckGraphicsModes(BoundProgram program, InitialGraphicsContext initialContext)
    {
        var routines = program.Routines.ToDictionary(static routine => routine.Symbol);
        var activeCalls = new HashSet<FunctionSymbol>();
        var callResults = new Dictionary<(FunctionSymbol, GraphicsModeKnowledge), GraphicsModeKnowledge>();
        var reported = new HashSet<int>();

        var initialMode = initialContext switch
        {
            InitialGraphicsContext.None => GraphicsModeKnowledge.Unavailable,
            InitialGraphicsContext.TextScreen => GraphicsModeKnowledge.Text,
            InitialGraphicsContext.GraphicsScreen => GraphicsModeKnowledge.Screen,
            InitialGraphicsContext.Canvas => GraphicsModeKnowledge.Canvas,
            _ => GraphicsModeKnowledge.Unknown
        };
        AnalyzeBlock(program.GlobalStatements, initialMode);
        // Процедура может вызываться из неизвестного контекста, в том числе как
        // обработчик. Явный SCREEN 0 внутри её тела всё равно проверяется.
        foreach (var routine in program.Routines)
        {
            activeCalls.Add(routine.Symbol);
            AnalyzeBlock(routine.Statements, GraphicsModeKnowledge.Unknown);
            activeCalls.Remove(routine.Symbol);
        }

        GraphicsFlow AnalyzeBlock(IReadOnlyList<BoundStatement> statements, GraphicsModeKnowledge input)
        {
            var flow = new GraphicsFlow(input);
            foreach (var statement in statements)
            {
                if (flow.Next is not { } current) break;
                var next = AnalyzeStatement(statement, current);
                flow = new GraphicsFlow(next.Next, Join(flow.Returned, next.Returned),
                    Join(flow.ExitedLoop, next.ExitedLoop), Join(flow.ContinuedLoop, next.ContinuedLoop));
            }
            return flow;
        }

        GraphicsFlow AnalyzeStatement(BoundStatement statement, GraphicsModeKnowledge mode)
        {
            // Выражения вычисляются до выполнения самого оператора. Вызванная
            // функция может изменить SCREEN, поэтому учитывается её результат.
            foreach (var expression in BoundExpressionTraversal.Immediate(statement))
                mode = AnalyzeExpression(expression, mode);

            switch (statement)
            {
                case BoundScreenStatement screen:
                    return new(TryEvaluateBoundIntegralConstant(screen.ModeExpression, out var value)
                        ? value == 0 ? GraphicsModeKnowledge.Text
                            : value is >= 1 and <= 10 ? GraphicsModeKnowledge.Screen : GraphicsModeKnowledge.Unknown
                        : GraphicsModeKnowledge.Unknown);
                case BoundCanvasStatement:
                    return new(GraphicsModeKnowledge.Canvas);
                case BoundIfStatement conditional:
                    if (conditional.Condition is BoundLiteralExpression { Value: bool condition })
                        return AnalyzeBlock(condition ? conditional.Statements : conditional.ElseStatements, mode);
                    return Merge(AnalyzeBlock(conditional.Statements, mode), AnalyzeBlock(conditional.ElseStatements, mode));
                case BoundSelectCaseStatement selection:
                    var selectionFlow = AnalyzeBlock(selection.ElseStatements, mode);
                    foreach (var clause in selection.Clauses)
                    {
                        var branchMode = mode;
                        foreach (var selector in clause.Selectors)
                        {
                            var expressions = selector switch
                            {
                                BoundCaseValueSelector item => new[] { item.Expression },
                                BoundCaseRangeSelector item => new[] { item.LowerBound, item.UpperBound },
                                BoundCaseComparisonSelector item => new[] { item.Expression },
                                _ => Array.Empty<BoundExpression>()
                            };
                            foreach (var expression in expressions)
                                branchMode = AnalyzeExpression(expression, branchMode);
                        }
                        selectionFlow = Merge(selectionFlow, AnalyzeBlock(clause.Statements, branchMode));
                    }
                    return selectionFlow;
                case BoundWhileStatement loop:
                    if (loop.Condition is BoundLiteralExpression { Value: false }) return new(mode);
                    return AnalyzeLoop(loop.Statements, mode,
                        canSkip: loop.Condition is not BoundLiteralExpression { Value: true });
                case BoundForStatement loop:
                    if (TryEvaluateBoundIntegralConstant(loop.StartExpression, out var start) &&
                        TryEvaluateBoundIntegralConstant(loop.EndExpression, out var end) &&
                        TryEvaluateBoundIntegralConstant(loop.StepExpression, out var step) && step != 0)
                    {
                        if (step > 0 ? start > end : start < end) return new(mode);
                        return AnalyzeLoop(loop.Statements, mode, canSkip: false,
                            canRepeat: step > 0 ? (Int128)start + step <= end : (Int128)start + step >= end);
                    }
                    return AnalyzeLoop(loop.Statements, mode);
                case BoundDoLoopStatement loop:
                    return AnalyzeLoop(loop.Statements, mode,
                        canSkip: !loop.EvaluateConditionAfterBody && loop.Condition is not null);
                case BoundTryStatement guarded:
                    var attempt = AnalyzeBlock(guarded.Statements, mode);
                    // Исключение может возникнуть после любого изменения режима.
                    var caught = AnalyzeBlock(guarded.CatchStatements, GraphicsModeKnowledge.Unknown);
                    var result = Merge(attempt, caught);
                    if (guarded.FinallyStatements.Count == 0) return result;
                    var finalInput = Join(Join(result.Next, result.Returned),
                        Join(result.ExitedLoop, result.ContinuedLoop)) ?? mode;
                    var final = AnalyzeBlock(guarded.FinallyStatements, finalInput);
                    return new(result.Next is null ? null : final.Next,
                        Join(final.Returned, result.Returned is null ? null : final.Next),
                        result.ExitedLoop is null ? null : final.Next,
                        result.ContinuedLoop is null ? null : final.Next);
                case BoundReturnStatement:
                    return new(null, Returned: mode);
                case BoundExitForStatement or BoundExitDoStatement:
                    return new(null, ExitedLoop: mode);
                case BoundContinueForStatement or BoundContinueDoStatement or BoundContinueWhileStatement:
                    return new(null, ContinuedLoop: mode);
                case BoundErrorStatement:
                    return new(null);
            }

            var operation = GraphicsOperation(statement);
            if (operation is not null) ReportInvalidContext(mode, operation, statement.Location);
            return new(mode);
        }

        GraphicsFlow AnalyzeLoop(IReadOnlyList<BoundStatement> statements, GraphicsModeKnowledge mode,
            bool canSkip = true, bool canRepeat = true)
        {
            var body = AnalyzeBlock(statements, mode);
            var entry = mode;
            if (canRepeat)
            {
                // Повторный проход учитывает режим, оставленный предыдущей
                // итерацией. Конечное множество режимов гарантирует сходимость.
                while (Join(entry, Join(body.Next, body.ContinuedLoop)) is { } nextEntry && nextEntry != entry)
                {
                    entry = nextEntry;
                    body = Merge(body, AnalyzeBlock(statements, entry));
                }
            }
            return new(Join(canSkip ? mode : null, Join(body.Next, Join(body.ExitedLoop, body.ContinuedLoop))), body.Returned);
        }

        GraphicsModeKnowledge AnalyzeExpression(BoundExpression expression, GraphicsModeKnowledge mode)
        {
            foreach (var child in BoundExpressionTraversal.Immediate(expression))
                mode = AnalyzeExpression(child, mode);
            if (expression is BoundTypeConstructorExpression construction)
                return AnalyzeCall(construction.Constructor, mode);
            if (expression is not BoundCallExpression call) return mode;
            if (call.Target is not BoundFunctionExpression target)
                return GraphicsModeKnowledge.Unknown;
            if (!target.Function.IsBuiltIn) return AnalyzeCall(target.Function, mode);

            if (target.Function.Name.ToUpperInvariant() is
                "POINT" or "PMAP" or "TEXTWIDTH" or "TEXTHEIGHT" or
                "MOUSEX" or "MOUSEY" or "MOUSEBUTTON" or "MOUSEPRESS")
                ReportInvalidContext(mode, target.Function.Name.ToUpperInvariant(), call.Location,
                    target.Function.Name.Equals("POINT", StringComparison.OrdinalIgnoreCase) && call.Arguments.Count == 1);
            // Встроенная функция с пользовательским callback может менять экран.
            return call.Arguments.Any(static argument => argument.Type is FunctionTypeSymbol)
                ? GraphicsModeKnowledge.Unknown : mode;
        }

        GraphicsModeKnowledge AnalyzeCall(FunctionSymbol function, GraphicsModeKnowledge mode)
        {
            if (callResults.TryGetValue((function, mode), out var cached)) return cached;
            if (!routines.TryGetValue(function, out var routine) || !activeCalls.Add(function))
                return GraphicsModeKnowledge.Unknown;
            try
            {
                var result = AnalyzeBlock(routine.Statements, mode);
                var output = Join(result.Next, result.Returned) ?? GraphicsModeKnowledge.Unknown;
                callResults[(function, mode)] = output;
                return output;
            }
            finally { activeCalls.Remove(function); }
        }

        void ReportInvalidContext(GraphicsModeKnowledge mode, string operation, TextLocation location, bool pointSelector = false)
        {
            var requiresScreen = pointSelector || operation is "PCOPY" or "VIEW" or "VIEW PRINT" or "WINDOW" or
                "COLOR" or "LOCATE" or "PALETTE" or "LINE" or "CIRCLE" or "DRAW" or "PMAP";
            var requiresCanvas = operation is "BEGINFRAME" or "PRESENT" or "CAMERA" or "VIEWPORT" or "RESETCAMERA" or
                "DRAWRECT" or "FILLRECT" or "DRAWLINE" or "DRAWCIRCLE" or "FILLCIRCLE" or "DRAWTEXT" or
                "DRAWTILE" or "DRAWTILEMAP" or "DRAWTILELAYER" or "CLS" or "TEXTWIDTH" or "TEXTHEIGHT";
            var invalid = mode & (GraphicsModeKnowledge.Unavailable | GraphicsModeKnowledge.Text |
                (requiresCanvas ? GraphicsModeKnowledge.Screen : 0) |
                (requiresScreen ? GraphicsModeKnowledge.Canvas : 0));
            if (invalid == 0 || !reported.Add(location.Span.Start)) return;
            var current = invalid.HasFlag(GraphicsModeKnowledge.Text) ? "выбран текстовый режим SCREEN 0" :
                invalid.HasFlag(GraphicsModeKnowledge.Unavailable) ? "графический режим ещё не выбран" :
                invalid.HasFlag(GraphicsModeKnowledge.Screen) ? "выбран графический режим SCREEN" : "выбран графический режим CANVAS";
            if (mode != invalid || (invalid & (invalid - 1)) != 0)
                current = "на одном из путей программы " + current;
            var required = requiresScreen ? "SCREEN 1..10" : requiresCanvas ? "CANVAS" : "SCREEN 1..10 или CANVAS";
            _diagnostics.Report(DiagnosticCodes.InvalidGraphicsContext,
                $"Операция '{operation}' недопустима: {current}. Перед этой операцией включите графический режим {required}.", location);
        }
    }

    private static GraphicsModeKnowledge? Join(GraphicsModeKnowledge? left, GraphicsModeKnowledge? right) =>
        left is null ? right : right is null ? left : left | right;

    private static GraphicsFlow Merge(GraphicsFlow left, GraphicsFlow right) =>
        new(Join(left.Next, right.Next), Join(left.Returned, right.Returned),
            Join(left.ExitedLoop, right.ExitedLoop), Join(left.ContinuedLoop, right.ContinuedLoop));

    private static string? GraphicsOperation(BoundStatement statement) => statement switch
    {
        BoundBeginFrameStatement => "BEGINFRAME", BoundPresentStatement => "PRESENT",
        BoundCameraStatement => "CAMERA", BoundViewportStatement => "VIEWPORT", BoundResetCameraStatement => "RESETCAMERA",
        BoundPCopyStatement => "PCOPY", BoundViewStatement => "VIEW", BoundViewPrintStatement => "VIEW PRINT",
        BoundWindowStatement => "WINDOW", BoundColorStatement => "COLOR", BoundLocateStatement => "LOCATE",
        BoundPaletteStatement => "PALETTE", BoundPSetStatement node => node.Reset ? "PRESET" : "PSET",
        BoundGetStatement => "GET", BoundPutStatement => "PUT", BoundLineStatement => "LINE",
        BoundCircleStatement => "CIRCLE", BoundPaintStatement => "PAINT", BoundDrawStatement => "DRAW",
        BoundDrawRectStatement => "DRAWRECT", BoundFillRectStatement => "FILLRECT", BoundDrawLineStatement => "DRAWLINE",
        BoundDrawCircleStatement => "DRAWCIRCLE", BoundFillCircleStatement => "FILLCIRCLE", BoundDrawTextStatement => "DRAWTEXT",
        BoundDrawImageStatement => "DRAWIMAGE", BoundDrawSpriteStatement => "DRAWSPRITE", BoundDrawTileStatement => "DRAWTILE",
        BoundDrawTilemapStatement => "DRAWTILEMAP", BoundDrawTileLayerStatement => "DRAWTILELAYER",
        BoundClsStatement { ColorExpression: not null } => "CLS",
        _ => null
    };
}
