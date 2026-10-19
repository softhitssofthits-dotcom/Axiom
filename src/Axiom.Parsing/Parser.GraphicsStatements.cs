using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private ScreenStatementSyntax ParseScreenStatement()
    {
        var screenKeyword = Match(SyntaxKind.ScreenKeyword, "SCREEN");
        var modeExpression = ParseExpression();
        SyntaxToken? colorSwitchCommaToken = null;
        ExpressionSyntax? colorSwitchExpression = null;
        SyntaxToken? activePageCommaToken = null;
        ExpressionSyntax? activePageExpression = null;
        SyntaxToken? visualPageCommaToken = null;
        ExpressionSyntax? visualPageExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            colorSwitchCommaToken = NextToken();
            if (Current.Kind != SyntaxKind.CommaToken && !IsStatementTerminator(Current.Kind))
            {
                colorSwitchExpression = ParseExpression();
            }
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            activePageCommaToken = NextToken();
            if (Current.Kind != SyntaxKind.CommaToken && !IsStatementTerminator(Current.Kind))
            {
                activePageExpression = ParseExpression();
            }
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            visualPageCommaToken = NextToken();
            if (!IsStatementTerminator(Current.Kind))
            {
                visualPageExpression = ParseExpression();
            }
        }

        return new ScreenStatementSyntax(
            screenKeyword,
            modeExpression,
            colorSwitchCommaToken,
            colorSwitchExpression,
            activePageCommaToken,
            activePageExpression,
            visualPageCommaToken,
            visualPageExpression);
    }

    private CanvasStatementSyntax ParseCanvasStatement()
    {
        var canvasKeyword = Match(SyntaxKind.CanvasKeyword, "CANVAS");
        var widthExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после ширины CANVAS");
        var heightExpression = ParseExpression();
        return new CanvasStatementSyntax(canvasKeyword, widthExpression, commaToken, heightExpression);
    }

    private CameraStatementSyntax ParseCameraStatement()
    {
        var cameraKeyword = Match(SyntaxKind.CameraKeyword, "CAMERA");
        var xExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после X CAMERA");
        var yExpression = ParseExpression();
        return new CameraStatementSyntax(cameraKeyword, xExpression, commaToken, yExpression);
    }

    private ViewportStatementSyntax ParseViewportStatement()
    {
        var viewportKeyword = Match(SyntaxKind.ViewportKeyword, "VIEWPORT");
        if (IsStatementTerminator(Current.Kind))
        {
            return new ViewportStatementSyntax(viewportKeyword, null, null, null);
        }

        var topLeftPoint = ParseGraphicsPoint();
        var minusToken = Match(SyntaxKind.MinusToken, "'-' между углами VIEWPORT");
        var bottomRightPoint = ParseGraphicsPoint();
        return new ViewportStatementSyntax(viewportKeyword, topLeftPoint, minusToken, bottomRightPoint);
    }

    private SmoothStatementSyntax ParseSmoothStatement()
    {
        var smoothKeyword = Match(SyntaxKind.SmoothKeyword, "SMOOTH");
        var modeKeyword = Current.Kind is SyntaxKind.OnKeyword or SyntaxKind.OffKeyword
            ? NextToken()
            : Match(SyntaxKind.OnKeyword, "ON или OFF после SMOOTH");

        if (!IsStatementTerminator(Current.Kind))
        {
            ReportUnexpectedToken(Current, "После ON или OFF ожидается конец оператора SMOOTH");
            while (Current.Kind is not (SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
            {
                NextToken();
            }
        }

        return new SmoothStatementSyntax(smoothKeyword, modeKeyword, modeKeyword.Kind == SyntaxKind.OnKeyword);
    }

    private ResetCameraStatementSyntax ParseResetCameraStatement()
    {
        var resetCameraKeyword = Match(SyntaxKind.ResetCameraKeyword, "RESETCAMERA");
        return new ResetCameraStatementSyntax(resetCameraKeyword);
    }

    private PCopyStatementSyntax ParsePCopyStatement()
    {
        var pcopyKeyword = Match(SyntaxKind.PCopyKeyword, "PCOPY");
        var sourcePageExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после исходной страницы PCOPY");
        var destinationPageExpression = ParseExpression();
        return new PCopyStatementSyntax(pcopyKeyword, sourcePageExpression, commaToken, destinationPageExpression);
    }

    private StatementSyntax ParseViewStatement()
    {
        var viewKeyword = Match(SyntaxKind.ViewKeyword, "VIEW");
        if (Current.Kind == SyntaxKind.PrintKeyword)
        {
            var printKeyword = NextToken();
            if (IsStatementTerminator(Current.Kind))
            {
                return new ViewPrintStatementSyntax(viewKeyword, printKeyword, null, null, null);
            }

            var topExpression = ParseExpression();
            var toKeyword = Match(SyntaxKind.ToKeyword, "TO в VIEW PRINT");
            var bottomExpression = ParseExpression();
            return new ViewPrintStatementSyntax(viewKeyword, printKeyword, topExpression, toKeyword, bottomExpression);
        }

        if (IsStatementTerminator(Current.Kind))
        {
            return new ViewStatementSyntax(viewKeyword, null, null, null, null, null, null, null, null);
        }

        SyntaxToken? screenKeyword = null;
        if (Current.Kind == SyntaxKind.ScreenKeyword)
        {
            screenKeyword = NextToken();
        }

        var topLeftPoint = ParseGraphicsPoint();
        var minusToken = Match(SyntaxKind.MinusToken, "'-' между углами VIEW");
        var bottomRightPoint = ParseGraphicsPoint();
        SyntaxToken? fillCommaToken = null;
        ExpressionSyntax? fillColorExpression = null;
        SyntaxToken? borderCommaToken = null;
        ExpressionSyntax? borderColorExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            fillCommaToken = Match(SyntaxKind.CommaToken, "',' после прямоугольника VIEW");
            fillColorExpression = ParseOptionalGraphicsArgumentExpression();

            if (Current.Kind == SyntaxKind.CommaToken)
            {
                borderCommaToken = Match(SyntaxKind.CommaToken, "',' перед цветом рамки VIEW");
                borderColorExpression = ParseOptionalGraphicsArgumentExpression();
            }
        }

        return new ViewStatementSyntax(
            viewKeyword,
            screenKeyword,
            topLeftPoint,
            minusToken,
            bottomRightPoint,
            fillCommaToken,
            fillColorExpression,
            borderCommaToken,
            borderColorExpression);
    }

    private WindowStatementSyntax ParseWindowStatement()
    {
        var windowKeyword = Match(SyntaxKind.WindowKeyword, "WINDOW");
        if (IsStatementTerminator(Current.Kind))
        {
            return new WindowStatementSyntax(windowKeyword, null, null, null, null);
        }

        SyntaxToken? screenKeyword = null;
        if (Current.Kind == SyntaxKind.ScreenKeyword)
        {
            screenKeyword = NextToken();
        }

        var topLeftPoint = ParseGraphicsPoint();
        var minusToken = Match(SyntaxKind.MinusToken, "'-' между углами WINDOW");
        var bottomRightPoint = ParseGraphicsPoint();
        return new WindowStatementSyntax(windowKeyword, screenKeyword, topLeftPoint, minusToken, bottomRightPoint);
    }

    private ClsStatementSyntax ParseClsStatement()
    {
        var clsKeyword = Match(SyntaxKind.ClsKeyword, "CLS");
        var colorExpression = IsStatementTerminator(Current.Kind) ? null : ParseExpression();
        return new ClsStatementSyntax(clsKeyword, colorExpression);
    }

    private ColorStatementSyntax ParseColorStatement()
    {
        var colorKeyword = Match(SyntaxKind.ColorKeyword, "COLOR");
        var foregroundExpression = ParseExpression();
        SyntaxToken? commaToken = null;
        ExpressionSyntax? backgroundExpression = null;
        SyntaxToken? transparentKeyword = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            commaToken = Match(SyntaxKind.CommaToken, ",");
            if (Current.Kind == SyntaxKind.TransparentKeyword)
            {
                transparentKeyword = NextToken();
            }
            else
            {
                backgroundExpression = ParseExpression();
            }
        }

        return new ColorStatementSyntax(colorKeyword, foregroundExpression, commaToken, backgroundExpression, transparentKeyword);
    }

    private LocateStatementSyntax ParseLocateStatement()
    {
        var locateKeyword = Match(SyntaxKind.LocateKeyword, "LOCATE");
        var rowExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после строки LOCATE");
        var columnExpression = ParseExpression();
        return new LocateStatementSyntax(locateKeyword, rowExpression, commaToken, columnExpression);
    }

    private PaletteStatementSyntax ParsePaletteStatement()
    {
        var paletteKeyword = Match(SyntaxKind.PaletteKeyword, "PALETTE");
        if (IsStatementTerminator(Current.Kind))
        {
            return new PaletteStatementSyntax(paletteKeyword, null, null, null, null, null);
        }

        if (Current.Kind == SyntaxKind.UsingKeyword)
        {
            var usingKeyword = Match(SyntaxKind.UsingKeyword, "USING");
            var valuesExpression = ParseExpression();
            return new PaletteStatementSyntax(paletteKeyword, usingKeyword, valuesExpression, null, null, null);
        }

        var indexExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после индекса PALETTE");
        var colorExpression = ParseExpression();
        return new PaletteStatementSyntax(paletteKeyword, null, null, indexExpression, commaToken, colorExpression);
    }

    private PSetStatementSyntax ParsePSetStatement(bool reset)
    {
        var keyword = Match(reset ? SyntaxKind.PresetKeyword : SyntaxKind.PSetKeyword, reset ? "PRESET" : "PSET");
        var stepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var point = ParseGraphicsPoint();
        SyntaxToken? commaToken = null;
        ExpressionSyntax? colorExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            commaToken = Match(SyntaxKind.CommaToken, ",");
            colorExpression = ParseExpression();
        }

        return new PSetStatementSyntax(keyword, stepKeyword, point, commaToken, colorExpression, reset);
    }

    private LineStatementSyntax ParseLineStatement()
    {
        var lineKeyword = Match(SyntaxKind.LineKeyword, "LINE");
        var startStepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        GraphicsPointSyntax? startPoint = null;
        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            startPoint = ParseGraphicsPoint();
        }

        var minusToken = Match(SyntaxKind.MinusToken, "'-' между точками LINE");
        var endStepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var endPoint = ParseGraphicsPoint();
        SyntaxToken? colorCommaToken = null;
        ExpressionSyntax? colorExpression = null;
        SyntaxToken? boxCommaToken = null;
        SyntaxToken? boxToken = null;
        var boxKind = LineBoxKind.None;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            colorCommaToken = Match(SyntaxKind.CommaToken, ",");

            if (Current.Kind is not (SyntaxKind.IdentifierToken or SyntaxKind.NewLineToken or SyntaxKind.EndOfFileToken))
            {
                colorExpression = ParseExpression();
            }
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            boxCommaToken = Match(SyntaxKind.CommaToken, ",");
            boxToken = Match(SyntaxKind.IdentifierToken, "B или BF");
            var boxText = boxToken.Text;
            boxKind = string.Equals(boxText, "BF", StringComparison.OrdinalIgnoreCase)
                ? LineBoxKind.Filled
                : string.Equals(boxText, "B", StringComparison.OrdinalIgnoreCase)
                    ? LineBoxKind.Border
                    : LineBoxKind.None;
        }

        return new LineStatementSyntax(
            lineKeyword,
            startStepKeyword,
            startPoint,
            minusToken,
            endStepKeyword,
            endPoint,
            colorCommaToken,
            colorExpression,
            boxCommaToken,
            boxToken,
            boxKind);
    }

    private CircleStatementSyntax ParseCircleStatement()
    {
        var circleKeyword = Match(SyntaxKind.CircleKeyword, "CIRCLE");
        var stepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var centerPoint = ParseGraphicsPoint();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после центра CIRCLE");
        var radiusExpression = ParseExpression();
        SyntaxToken? colorCommaToken = null;
        ExpressionSyntax? colorExpression = null;
        SyntaxToken? startCommaToken = null;
        ExpressionSyntax? startExpression = null;
        SyntaxToken? endCommaToken = null;
        ExpressionSyntax? endExpression = null;
        SyntaxToken? aspectCommaToken = null;
        ExpressionSyntax? aspectExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            colorCommaToken = Match(SyntaxKind.CommaToken, ",");
            colorExpression = ParseOptionalGraphicsArgumentExpression();
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            startCommaToken = Match(SyntaxKind.CommaToken, ",");
            startExpression = ParseOptionalGraphicsArgumentExpression();
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            endCommaToken = Match(SyntaxKind.CommaToken, ",");
            endExpression = ParseOptionalGraphicsArgumentExpression();
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            aspectCommaToken = Match(SyntaxKind.CommaToken, ",");
            aspectExpression = ParseOptionalGraphicsArgumentExpression();
        }

        return new CircleStatementSyntax(
            circleKeyword,
            stepKeyword,
            centerPoint,
            commaToken,
            radiusExpression,
            colorCommaToken,
            colorExpression,
            startCommaToken,
            startExpression,
            endCommaToken,
            endExpression,
            aspectCommaToken,
            aspectExpression);
    }

    private PaintStatementSyntax ParsePaintStatement()
    {
        var paintKeyword = Match(SyntaxKind.PaintKeyword, "PAINT");
        var stepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var point = ParseGraphicsPoint();
        SyntaxToken? fillCommaToken = null;
        ExpressionSyntax? fillColorExpression = null;
        SyntaxToken? borderCommaToken = null;
        ExpressionSyntax? borderColorExpression = null;
        SyntaxToken? backgroundCommaToken = null;
        ExpressionSyntax? backgroundExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            fillCommaToken = Match(SyntaxKind.CommaToken, ",");
            fillColorExpression = ParseOptionalGraphicsArgumentExpression();
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            borderCommaToken = Match(SyntaxKind.CommaToken, ",");
            borderColorExpression = ParseOptionalGraphicsArgumentExpression();
        }

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            backgroundCommaToken = Match(SyntaxKind.CommaToken, ",");
            backgroundExpression = ParseOptionalGraphicsArgumentExpression();
        }

        return new PaintStatementSyntax(
            paintKeyword,
            stepKeyword,
            point,
            fillCommaToken,
            fillColorExpression,
            borderCommaToken,
            borderColorExpression,
            backgroundCommaToken,
            backgroundExpression);
    }

    private DrawStatementSyntax ParseDrawStatement()
    {
        var drawKeyword = Match(SyntaxKind.DrawKeyword, "DRAW");
        var commandExpression = ParseExpression();
        return new DrawStatementSyntax(drawKeyword, commandExpression);
    }

    private DrawLineStatementSyntax ParseDrawLineStatement()
    {
        var drawLineKeyword = Match(SyntaxKind.DrawLineKeyword, "DRAWLINE");
        var x1Expression = ParseExpression();
        var x1CommaToken = Match(SyntaxKind.CommaToken, "',' после X1 DRAWLINE");
        var y1Expression = ParseExpression();
        var y1CommaToken = Match(SyntaxKind.CommaToken, "',' после Y1 DRAWLINE");
        var x2Expression = ParseExpression();
        var x2CommaToken = Match(SyntaxKind.CommaToken, "',' после X2 DRAWLINE");
        var y2Expression = ParseExpression();
        var y2CommaToken = Match(SyntaxKind.CommaToken, "',' после Y2 DRAWLINE");
        var colorExpression = ParseExpression();
        return new DrawLineStatementSyntax(
            drawLineKeyword,
            x1Expression,
            x1CommaToken,
            y1Expression,
            y1CommaToken,
            x2Expression,
            x2CommaToken,
            y2Expression,
            y2CommaToken,
            colorExpression);
    }

    private DrawCircleStatementSyntax ParseDrawCircleStatement()
    {
        var drawCircleKeyword = Match(SyntaxKind.DrawCircleKeyword, "DRAWCIRCLE");
        var xExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после X DRAWCIRCLE");
        var yExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после Y DRAWCIRCLE");
        var radiusExpression = ParseExpression();
        var radiusCommaToken = Match(SyntaxKind.CommaToken, "',' после радиуса DRAWCIRCLE");
        var colorExpression = ParseExpression();
        return new DrawCircleStatementSyntax(
            drawCircleKeyword,
            xExpression,
            xCommaToken,
            yExpression,
            yCommaToken,
            radiusExpression,
            radiusCommaToken,
            colorExpression);
    }

    private FillCircleStatementSyntax ParseFillCircleStatement()
    {
        var fillCircleKeyword = Match(SyntaxKind.FillCircleKeyword, "FILLCIRCLE");
        var xExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после X FILLCIRCLE");
        var yExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после Y FILLCIRCLE");
        var radiusExpression = ParseExpression();
        var radiusCommaToken = Match(SyntaxKind.CommaToken, "',' после радиуса FILLCIRCLE");
        var colorExpression = ParseExpression();
        return new FillCircleStatementSyntax(
            fillCircleKeyword,
            xExpression,
            xCommaToken,
            yExpression,
            yCommaToken,
            radiusExpression,
            radiusCommaToken,
            colorExpression);
    }

    private DrawTextStatementSyntax ParseDrawTextStatement()
    {
        var drawTextKeyword = Match(SyntaxKind.DrawTextKeyword, "DRAWTEXT");
        var textExpression = ParseExpression();
        var textCommaToken = Match(SyntaxKind.CommaToken, "',' после текста DRAWTEXT");
        var xExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после X DRAWTEXT");
        var yExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после Y DRAWTEXT");
        var colorExpression = ParseExpression();
        return new DrawTextStatementSyntax(
            drawTextKeyword,
            textExpression,
            textCommaToken,
            xExpression,
            xCommaToken,
            yExpression,
            yCommaToken,
            colorExpression);
    }

    private DrawImageStatementSyntax ParseDrawImageStatement()
    {
        var drawImageKeyword = Match(SyntaxKind.DrawImageKeyword, "DRAWIMAGE");
        var imageExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после изображения DRAWIMAGE");
        var xExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после координаты X DRAWIMAGE");
        var yExpression = ParseExpression();
        SyntaxToken? widthCommaToken = null;
        ExpressionSyntax? widthExpression = null;
        SyntaxToken? heightCommaToken = null;
        ExpressionSyntax? heightExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            widthCommaToken = NextToken();
            widthExpression = ParseExpression();
            heightCommaToken = Match(SyntaxKind.CommaToken, "',' после ширины DRAWIMAGE");
            heightExpression = ParseExpression();
        }

        return new DrawImageStatementSyntax(
            drawImageKeyword,
            imageExpression,
            xCommaToken,
            xExpression,
            yCommaToken,
            yExpression,
            widthCommaToken,
            widthExpression,
            heightCommaToken,
            heightExpression);
    }

    private DrawRectStatementSyntax ParseDrawRectStatement()
    {
        var drawRectKeyword = Match(SyntaxKind.DrawRectKeyword, "DRAWRECT");
        var xExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после X DRAWRECT");
        var yExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после Y DRAWRECT");
        var widthExpression = ParseExpression();
        var widthCommaToken = Match(SyntaxKind.CommaToken, "',' после ширины DRAWRECT");
        var heightExpression = ParseExpression();
        var heightCommaToken = Match(SyntaxKind.CommaToken, "',' после высоты DRAWRECT");
        var colorExpression = ParseExpression();
        return new DrawRectStatementSyntax(
            drawRectKeyword,
            xExpression,
            xCommaToken,
            yExpression,
            yCommaToken,
            widthExpression,
            widthCommaToken,
            heightExpression,
            heightCommaToken,
            colorExpression);
    }

    private FillRectStatementSyntax ParseFillRectStatement()
    {
        var fillRectKeyword = Match(SyntaxKind.FillRectKeyword, "FILLRECT");
        var xExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после X FILLRECT");
        var yExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после Y FILLRECT");
        var widthExpression = ParseExpression();
        var widthCommaToken = Match(SyntaxKind.CommaToken, "',' после ширины FILLRECT");
        var heightExpression = ParseExpression();
        var heightCommaToken = Match(SyntaxKind.CommaToken, "',' после высоты FILLRECT");
        var colorExpression = ParseExpression();
        return new FillRectStatementSyntax(
            fillRectKeyword,
            xExpression,
            xCommaToken,
            yExpression,
            yCommaToken,
            widthExpression,
            widthCommaToken,
            heightExpression,
            heightCommaToken,
            colorExpression);
    }

    private DrawSpriteStatementSyntax ParseDrawSpriteStatement()
    {
        var drawSpriteKeyword = Match(SyntaxKind.DrawSpriteKeyword, "DRAWSPRITE");
        var spriteExpression = ParseExpression();
        var frameCommaToken = Match(SyntaxKind.CommaToken, "',' после спрайта DRAWSPRITE");
        var frameExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после номера кадра DRAWSPRITE");
        var xExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после координаты X DRAWSPRITE");
        var yExpression = ParseExpression();
        SyntaxToken? widthCommaToken = null;
        ExpressionSyntax? widthExpression = null;
        SyntaxToken? heightCommaToken = null;
        ExpressionSyntax? heightExpression = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            widthCommaToken = NextToken();
            widthExpression = ParseExpression();
            heightCommaToken = Match(SyntaxKind.CommaToken, "',' после ширины DRAWSPRITE");
            heightExpression = ParseExpression();
        }

        return new DrawSpriteStatementSyntax(
            drawSpriteKeyword,
            spriteExpression,
            frameCommaToken,
            frameExpression,
            xCommaToken,
            xExpression,
            yCommaToken,
            yExpression,
            widthCommaToken,
            widthExpression,
            heightCommaToken,
            heightExpression);
    }

    private DrawTileStatementSyntax ParseDrawTileStatement()
    {
        var drawTileKeyword = Match(SyntaxKind.DrawTileKeyword, "DRAWTILE");
        var tilesetExpression = ParseExpression();
        var tileIndexCommaToken = Match(SyntaxKind.CommaToken, "',' после тайлсета DRAWTILE");
        var tileIndexExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после номера тайла DRAWTILE");
        var xExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после координаты X DRAWTILE");
        var yExpression = ParseExpression();

        return new DrawTileStatementSyntax(
            drawTileKeyword,
            tilesetExpression,
            tileIndexCommaToken,
            tileIndexExpression,
            xCommaToken,
            xExpression,
            yCommaToken,
            yExpression);
    }

    private DrawTilemapStatementSyntax ParseDrawTilemapStatement()
    {
        var drawTilemapKeyword = Match(SyntaxKind.DrawTilemapKeyword, "DRAWTILEMAP");
        var tilemapExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после карты DRAWTILEMAP");
        var xExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после координаты X DRAWTILEMAP");
        var yExpression = ParseExpression();
        return new DrawTilemapStatementSyntax(
            drawTilemapKeyword,
            tilemapExpression,
            xCommaToken,
            xExpression,
            yCommaToken,
            yExpression);
    }

    private DrawTileLayerStatementSyntax ParseDrawTileLayerStatement()
    {
        var drawTileLayerKeyword = Match(SyntaxKind.DrawTileLayerKeyword, "DRAWTILELAYER");
        var tilemapExpression = ParseExpression();
        var layerIndexCommaToken = Match(SyntaxKind.CommaToken, "',' после карты DRAWTILELAYER");
        var layerIndexExpression = ParseExpression();
        var xCommaToken = Match(SyntaxKind.CommaToken, "',' после номера слоя DRAWTILELAYER");
        var xExpression = ParseExpression();
        var yCommaToken = Match(SyntaxKind.CommaToken, "',' после координаты X DRAWTILELAYER");
        var yExpression = ParseExpression();
        return new DrawTileLayerStatementSyntax(
            drawTileLayerKeyword,
            tilemapExpression,
            layerIndexCommaToken,
            layerIndexExpression,
            xCommaToken,
            xExpression,
            yCommaToken,
            yExpression);
    }

    private SetMapTileStatementSyntax ParseSetMapTileStatement()
    {
        var setMapTileKeyword = Match(SyntaxKind.SetMapTileKeyword, "SETMAPTILE");
        var tilemapExpression = ParseExpression();
        var layerIndexCommaToken = Match(SyntaxKind.CommaToken, "',' после карты SETMAPTILE");
        var layerIndexExpression = ParseExpression();
        var columnCommaToken = Match(SyntaxKind.CommaToken, "',' после номера слоя SETMAPTILE");
        var columnExpression = ParseExpression();
        var rowCommaToken = Match(SyntaxKind.CommaToken, "',' после столбца SETMAPTILE");
        var rowExpression = ParseExpression();
        var tilesetIndexCommaToken = Match(SyntaxKind.CommaToken, "',' после строки SETMAPTILE");
        var tilesetIndexExpression = ParseExpression();
        var tileIndexCommaToken = Match(SyntaxKind.CommaToken, "',' после номера тайлсета SETMAPTILE");
        var tileIndexExpression = ParseExpression();
        SyntaxToken? flagsCommaToken = null;
        ExpressionSyntax? flagsExpression = null;
        if (Current.Kind == SyntaxKind.CommaToken)
        {
            flagsCommaToken = NextToken();
            flagsExpression = ParseExpression();
        }

        return new SetMapTileStatementSyntax(
            setMapTileKeyword,
            tilemapExpression,
            layerIndexCommaToken,
            layerIndexExpression,
            columnCommaToken,
            columnExpression,
            rowCommaToken,
            rowExpression,
            tilesetIndexCommaToken,
            tilesetIndexExpression,
            tileIndexCommaToken,
            tileIndexExpression,
            flagsCommaToken,
            flagsExpression);
    }

    private GetStatementSyntax ParseGetStatement()
    {
        var getKeyword = Match(SyntaxKind.GetKeyword, "GET");
        var startStepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var startPoint = ParseGraphicsPoint();
        var minusToken = Match(SyntaxKind.MinusToken, "'-' между точками GET");
        var endStepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var endPoint = ParseGraphicsPoint();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после прямоугольника GET");
        var bufferExpression = ParseExpression();
        return new GetStatementSyntax(getKeyword, startStepKeyword, startPoint, minusToken, endStepKeyword, endPoint, commaToken, bufferExpression);
    }

    private PutStatementSyntax ParsePutStatement()
    {
        var putKeyword = Match(SyntaxKind.PutKeyword, "PUT");
        var stepKeyword = Current.Kind == SyntaxKind.StepKeyword ? NextToken() : null;
        var point = ParseGraphicsPoint();
        var commaToken = Match(SyntaxKind.CommaToken, "',' после точки PUT");
        var bufferExpression = ParseExpression();
        SyntaxToken? actionCommaToken = null;
        SyntaxToken? actionToken = null;

        if (Current.Kind == SyntaxKind.CommaToken)
        {
            actionCommaToken = Match(SyntaxKind.CommaToken, ",");
            actionToken = ParsePutActionToken();
        }

        return new PutStatementSyntax(putKeyword, stepKeyword, point, commaToken, bufferExpression, actionCommaToken, actionToken);
    }

    private ExpressionSyntax? ParseOptionalGraphicsArgumentExpression()
    {
        if (Current.Kind == SyntaxKind.CommaToken || IsStatementTerminator(Current.Kind))
        {
            return null;
        }

        return ParseExpression();
    }

    private GraphicsPointSyntax ParseGraphicsPoint()
    {
        var openParenToken = Match(SyntaxKind.OpenParenToken, "'('");
        var xExpression = ParseExpression();
        var commaToken = Match(SyntaxKind.CommaToken, "',' в графической точке");
        var yExpression = ParseExpression();
        var closeParenToken = Match(SyntaxKind.CloseParenToken, "')'");
        return new GraphicsPointSyntax(openParenToken, xExpression, commaToken, yExpression, closeParenToken);
    }

    private SyntaxToken ParsePutActionToken()
    {
        if (Current.Kind is SyntaxKind.PSetKeyword
            or SyntaxKind.PresetKeyword
            or SyntaxKind.AndKeyword
            or SyntaxKind.OrKeyword
            or SyntaxKind.XorKeyword)
        {
            return NextToken();
        }

        ReportExpectedToken("PSET, PRESET, AND, OR или XOR");
        return CreateMissingToken(SyntaxKind.PSetKeyword);
    }

}
