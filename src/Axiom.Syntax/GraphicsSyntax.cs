namespace Axiom.Syntax;

public sealed class ScreenStatementSyntax : StatementSyntax
{
    public ScreenStatementSyntax(
        SyntaxToken screenKeyword,
        ExpressionSyntax modeExpression,
        SyntaxToken? colorSwitchCommaToken,
        ExpressionSyntax? colorSwitchExpression,
        SyntaxToken? activePageCommaToken,
        ExpressionSyntax? activePageExpression,
        SyntaxToken? visualPageCommaToken,
        ExpressionSyntax? visualPageExpression)
    {
        ArgumentNullException.ThrowIfNull(screenKeyword);
        ArgumentNullException.ThrowIfNull(modeExpression);

        ScreenKeyword = screenKeyword;
        ModeExpression = modeExpression;
        ColorSwitchCommaToken = colorSwitchCommaToken;
        ColorSwitchExpression = colorSwitchExpression;
        ActivePageCommaToken = activePageCommaToken;
        ActivePageExpression = activePageExpression;
        VisualPageCommaToken = visualPageCommaToken;
        VisualPageExpression = visualPageExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ScreenStatement;

    public SyntaxToken ScreenKeyword { get; }

    public ExpressionSyntax ModeExpression { get; }

    public SyntaxToken? ColorSwitchCommaToken { get; }

    public ExpressionSyntax? ColorSwitchExpression { get; }

    public SyntaxToken? ActivePageCommaToken { get; }

    public ExpressionSyntax? ActivePageExpression { get; }

    public SyntaxToken? VisualPageCommaToken { get; }

    public ExpressionSyntax? VisualPageExpression { get; }
}

public sealed class CanvasStatementSyntax : StatementSyntax
{
    public CanvasStatementSyntax(SyntaxToken canvasKeyword, ExpressionSyntax widthExpression, SyntaxToken commaToken, ExpressionSyntax heightExpression)
    {
        ArgumentNullException.ThrowIfNull(canvasKeyword);
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(heightExpression);

        CanvasKeyword = canvasKeyword;
        WidthExpression = widthExpression;
        CommaToken = commaToken;
        HeightExpression = heightExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.CanvasStatement;

    public SyntaxToken CanvasKeyword { get; }

    public ExpressionSyntax WidthExpression { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax HeightExpression { get; }
}

public sealed class BeginFrameStatementSyntax : StatementSyntax
{
    public BeginFrameStatementSyntax(SyntaxToken beginFrameKeyword)
    {
        ArgumentNullException.ThrowIfNull(beginFrameKeyword);
        BeginFrameKeyword = beginFrameKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.BeginFrameStatement;

    public SyntaxToken BeginFrameKeyword { get; }
}

public sealed class PresentStatementSyntax : StatementSyntax
{
    public PresentStatementSyntax(SyntaxToken presentKeyword)
    {
        ArgumentNullException.ThrowIfNull(presentKeyword);
        PresentKeyword = presentKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.PresentStatement;

    public SyntaxToken PresentKeyword { get; }
}

public sealed class CameraStatementSyntax : StatementSyntax
{
    public CameraStatementSyntax(
        SyntaxToken cameraKeyword,
        ExpressionSyntax xExpression,
        SyntaxToken commaToken,
        ExpressionSyntax yExpression)
    {
        ArgumentNullException.ThrowIfNull(cameraKeyword);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(yExpression);

        CameraKeyword = cameraKeyword;
        XExpression = xExpression;
        CommaToken = commaToken;
        YExpression = yExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.CameraStatement;

    public SyntaxToken CameraKeyword { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax YExpression { get; }
}

public sealed class ResetCameraStatementSyntax : StatementSyntax
{
    public ResetCameraStatementSyntax(SyntaxToken resetCameraKeyword)
    {
        ArgumentNullException.ThrowIfNull(resetCameraKeyword);
        ResetCameraKeyword = resetCameraKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ResetCameraStatement;

    public SyntaxToken ResetCameraKeyword { get; }
}

public sealed class PCopyStatementSyntax : StatementSyntax
{
    public PCopyStatementSyntax(SyntaxToken pcopyKeyword, ExpressionSyntax sourcePageExpression, SyntaxToken commaToken, ExpressionSyntax destinationPageExpression)
    {
        ArgumentNullException.ThrowIfNull(pcopyKeyword);
        ArgumentNullException.ThrowIfNull(sourcePageExpression);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(destinationPageExpression);

        PCopyKeyword = pcopyKeyword;
        SourcePageExpression = sourcePageExpression;
        CommaToken = commaToken;
        DestinationPageExpression = destinationPageExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.PCopyStatement;

    public SyntaxToken PCopyKeyword { get; }

    public ExpressionSyntax SourcePageExpression { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax DestinationPageExpression { get; }
}

public sealed class ViewStatementSyntax : StatementSyntax
{
    public ViewStatementSyntax(
        SyntaxToken viewKeyword,
        SyntaxToken? screenKeyword,
        GraphicsPointSyntax? topLeftPoint,
        SyntaxToken? minusToken,
        GraphicsPointSyntax? bottomRightPoint,
        SyntaxToken? fillCommaToken,
        ExpressionSyntax? fillColorExpression,
        SyntaxToken? borderCommaToken,
        ExpressionSyntax? borderColorExpression)
    {
        ArgumentNullException.ThrowIfNull(viewKeyword);

        ViewKeyword = viewKeyword;
        ScreenKeyword = screenKeyword;
        TopLeftPoint = topLeftPoint;
        MinusToken = minusToken;
        BottomRightPoint = bottomRightPoint;
        FillCommaToken = fillCommaToken;
        FillColorExpression = fillColorExpression;
        BorderCommaToken = borderCommaToken;
        BorderColorExpression = borderColorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ViewStatement;

    public SyntaxToken ViewKeyword { get; }

    public SyntaxToken? ScreenKeyword { get; }

    public GraphicsPointSyntax? TopLeftPoint { get; }

    public SyntaxToken? MinusToken { get; }

    public GraphicsPointSyntax? BottomRightPoint { get; }

    public SyntaxToken? FillCommaToken { get; }

    public ExpressionSyntax? FillColorExpression { get; }

    public SyntaxToken? BorderCommaToken { get; }

    public ExpressionSyntax? BorderColorExpression { get; }

    public bool IsReset => TopLeftPoint is null || BottomRightPoint is null;
}

public sealed class ViewPrintStatementSyntax : StatementSyntax
{
    public ViewPrintStatementSyntax(
        SyntaxToken viewKeyword,
        SyntaxToken printKeyword,
        ExpressionSyntax? topExpression,
        SyntaxToken? toKeyword,
        ExpressionSyntax? bottomExpression)
    {
        ArgumentNullException.ThrowIfNull(viewKeyword);
        ArgumentNullException.ThrowIfNull(printKeyword);

        ViewKeyword = viewKeyword;
        PrintKeyword = printKeyword;
        TopExpression = topExpression;
        ToKeyword = toKeyword;
        BottomExpression = bottomExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ViewPrintStatement;

    public SyntaxToken ViewKeyword { get; }

    public SyntaxToken PrintKeyword { get; }

    public ExpressionSyntax? TopExpression { get; }

    public SyntaxToken? ToKeyword { get; }

    public ExpressionSyntax? BottomExpression { get; }

    public bool IsReset => TopExpression is null || BottomExpression is null;
}

public sealed class WindowStatementSyntax : StatementSyntax
{
    public WindowStatementSyntax(
        SyntaxToken windowKeyword,
        SyntaxToken? screenKeyword,
        GraphicsPointSyntax? topLeftPoint,
        SyntaxToken? minusToken,
        GraphicsPointSyntax? bottomRightPoint)
    {
        ArgumentNullException.ThrowIfNull(windowKeyword);

        WindowKeyword = windowKeyword;
        ScreenKeyword = screenKeyword;
        TopLeftPoint = topLeftPoint;
        MinusToken = minusToken;
        BottomRightPoint = bottomRightPoint;
    }

    public override SyntaxKind Kind => SyntaxKind.WindowStatement;

    public SyntaxToken WindowKeyword { get; }

    public SyntaxToken? ScreenKeyword { get; }

    public GraphicsPointSyntax? TopLeftPoint { get; }

    public SyntaxToken? MinusToken { get; }

    public GraphicsPointSyntax? BottomRightPoint { get; }

    public bool IsReset => TopLeftPoint is null || BottomRightPoint is null;
}

public sealed class ClsStatementSyntax : StatementSyntax
{
    public ClsStatementSyntax(SyntaxToken clsKeyword, ExpressionSyntax? colorExpression)
    {
        ArgumentNullException.ThrowIfNull(clsKeyword);
        ClsKeyword = clsKeyword;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ClsStatement;

    public SyntaxToken ClsKeyword { get; }

    public ExpressionSyntax? ColorExpression { get; }
}

public sealed class ColorStatementSyntax : StatementSyntax
{
    public ColorStatementSyntax(
        SyntaxToken colorKeyword,
        ExpressionSyntax foregroundExpression,
        SyntaxToken? commaToken,
        ExpressionSyntax? backgroundExpression,
        SyntaxToken? transparentKeyword)
    {
        ArgumentNullException.ThrowIfNull(colorKeyword);
        ArgumentNullException.ThrowIfNull(foregroundExpression);

        ColorKeyword = colorKeyword;
        ForegroundExpression = foregroundExpression;
        CommaToken = commaToken;
        BackgroundExpression = backgroundExpression;
        TransparentKeyword = transparentKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ColorStatement;

    public SyntaxToken ColorKeyword { get; }

    public ExpressionSyntax ForegroundExpression { get; }

    public SyntaxToken? CommaToken { get; }

    public ExpressionSyntax? BackgroundExpression { get; }

    public SyntaxToken? TransparentKeyword { get; }
}

public sealed class LocateStatementSyntax : StatementSyntax
{
    public LocateStatementSyntax(SyntaxToken locateKeyword, ExpressionSyntax rowExpression, SyntaxToken commaToken, ExpressionSyntax columnExpression)
    {
        ArgumentNullException.ThrowIfNull(locateKeyword);
        ArgumentNullException.ThrowIfNull(rowExpression);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(columnExpression);

        LocateKeyword = locateKeyword;
        RowExpression = rowExpression;
        CommaToken = commaToken;
        ColumnExpression = columnExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.LocateStatement;

    public SyntaxToken LocateKeyword { get; }

    public ExpressionSyntax RowExpression { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax ColumnExpression { get; }
}

public sealed class PaletteStatementSyntax : StatementSyntax
{
    public PaletteStatementSyntax(
        SyntaxToken paletteKeyword,
        SyntaxToken? usingKeyword,
        ExpressionSyntax? valuesExpression,
        ExpressionSyntax? indexExpression,
        SyntaxToken? commaToken,
        ExpressionSyntax? colorExpression)
    {
        ArgumentNullException.ThrowIfNull(paletteKeyword);

        PaletteKeyword = paletteKeyword;
        UsingKeyword = usingKeyword;
        ValuesExpression = valuesExpression;
        IndexExpression = indexExpression;
        CommaToken = commaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.PaletteStatement;

    public SyntaxToken PaletteKeyword { get; }

    public SyntaxToken? UsingKeyword { get; }

    public ExpressionSyntax? ValuesExpression { get; }

    public ExpressionSyntax? IndexExpression { get; }

    public SyntaxToken? CommaToken { get; }

    public ExpressionSyntax? ColorExpression { get; }

    public bool IsReset => UsingKeyword is null && (IndexExpression is null || ColorExpression is null);

    public bool IsUsing => UsingKeyword is not null;
}

public sealed class GraphicsPointSyntax : SyntaxNode
{
    public GraphicsPointSyntax(SyntaxToken openParenToken, ExpressionSyntax xExpression, SyntaxToken commaToken, ExpressionSyntax yExpression, SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        OpenParenToken = openParenToken;
        XExpression = xExpression;
        CommaToken = commaToken;
        YExpression = yExpression;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.GraphicsPoint;

    public SyntaxToken OpenParenToken { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken CloseParenToken { get; }
}

public sealed class PSetStatementSyntax : StatementSyntax
{
    public PSetStatementSyntax(
        SyntaxToken keyword,
        SyntaxToken? stepKeyword,
        GraphicsPointSyntax point,
        SyntaxToken? commaToken,
        ExpressionSyntax? colorExpression,
        bool reset)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        ArgumentNullException.ThrowIfNull(point);

        Keyword = keyword;
        StepKeyword = stepKeyword;
        Point = point;
        CommaToken = commaToken;
        ColorExpression = colorExpression;
        Reset = reset;
    }

    public override SyntaxKind Kind => SyntaxKind.PSetStatement;

    public SyntaxToken Keyword { get; }

    public SyntaxToken? StepKeyword { get; }

    public bool IsRelative => StepKeyword is not null;

    public GraphicsPointSyntax Point { get; }

    public SyntaxToken? CommaToken { get; }

    public ExpressionSyntax? ColorExpression { get; }

    public bool Reset { get; }
}

public sealed class GetStatementSyntax : StatementSyntax
{
    public GetStatementSyntax(
        SyntaxToken getKeyword,
        SyntaxToken? startStepKeyword,
        GraphicsPointSyntax startPoint,
        SyntaxToken minusToken,
        SyntaxToken? endStepKeyword,
        GraphicsPointSyntax endPoint,
        SyntaxToken commaToken,
        ExpressionSyntax bufferExpression)
    {
        ArgumentNullException.ThrowIfNull(getKeyword);
        ArgumentNullException.ThrowIfNull(startPoint);
        ArgumentNullException.ThrowIfNull(minusToken);
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(bufferExpression);

        GetKeyword = getKeyword;
        StartStepKeyword = startStepKeyword;
        StartPoint = startPoint;
        MinusToken = minusToken;
        EndStepKeyword = endStepKeyword;
        EndPoint = endPoint;
        CommaToken = commaToken;
        BufferExpression = bufferExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.GetStatement;

    public SyntaxToken GetKeyword { get; }

    public SyntaxToken? StartStepKeyword { get; }

    public bool IsStartRelative => StartStepKeyword is not null;

    public GraphicsPointSyntax StartPoint { get; }

    public SyntaxToken MinusToken { get; }

    public SyntaxToken? EndStepKeyword { get; }

    public bool IsEndRelative => EndStepKeyword is not null;

    public GraphicsPointSyntax EndPoint { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax BufferExpression { get; }
}

public sealed class PutStatementSyntax : StatementSyntax
{
    public PutStatementSyntax(
        SyntaxToken putKeyword,
        SyntaxToken? stepKeyword,
        GraphicsPointSyntax point,
        SyntaxToken commaToken,
        ExpressionSyntax bufferExpression,
        SyntaxToken? actionCommaToken,
        SyntaxToken? actionToken)
    {
        ArgumentNullException.ThrowIfNull(putKeyword);
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(bufferExpression);

        PutKeyword = putKeyword;
        StepKeyword = stepKeyword;
        Point = point;
        CommaToken = commaToken;
        BufferExpression = bufferExpression;
        ActionCommaToken = actionCommaToken;
        ActionToken = actionToken;
    }

    public override SyntaxKind Kind => SyntaxKind.PutStatement;

    public SyntaxToken PutKeyword { get; }

    public SyntaxToken? StepKeyword { get; }

    public bool IsRelative => StepKeyword is not null;

    public GraphicsPointSyntax Point { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax BufferExpression { get; }

    public SyntaxToken? ActionCommaToken { get; }

    public SyntaxToken? ActionToken { get; }
}

public enum LineBoxKind
{
    None,
    Border,
    Filled
}

public sealed class LineStatementSyntax : StatementSyntax
{
    public LineStatementSyntax(
        SyntaxToken lineKeyword,
        SyntaxToken? startStepKeyword,
        GraphicsPointSyntax? startPoint,
        SyntaxToken minusToken,
        SyntaxToken? endStepKeyword,
        GraphicsPointSyntax endPoint,
        SyntaxToken? colorCommaToken,
        ExpressionSyntax? colorExpression,
        SyntaxToken? boxCommaToken,
        SyntaxToken? boxToken,
        LineBoxKind boxKind)
    {
        ArgumentNullException.ThrowIfNull(lineKeyword);
        ArgumentNullException.ThrowIfNull(minusToken);
        ArgumentNullException.ThrowIfNull(endPoint);

        LineKeyword = lineKeyword;
        StartStepKeyword = startStepKeyword;
        StartPoint = startPoint;
        MinusToken = minusToken;
        EndStepKeyword = endStepKeyword;
        EndPoint = endPoint;
        ColorCommaToken = colorCommaToken;
        ColorExpression = colorExpression;
        BoxCommaToken = boxCommaToken;
        BoxToken = boxToken;
        BoxKind = boxKind;
    }

    public override SyntaxKind Kind => SyntaxKind.LineStatement;

    public SyntaxToken LineKeyword { get; }

    public SyntaxToken? StartStepKeyword { get; }

    public bool IsStartRelative => StartStepKeyword is not null;

    public GraphicsPointSyntax? StartPoint { get; }

    public bool UsesCurrentGraphicsPositionAsStart => StartPoint is null;

    public SyntaxToken MinusToken { get; }

    public SyntaxToken? EndStepKeyword { get; }

    public bool IsEndRelative => EndStepKeyword is not null;

    public GraphicsPointSyntax EndPoint { get; }

    public SyntaxToken? ColorCommaToken { get; }

    public ExpressionSyntax? ColorExpression { get; }

    public SyntaxToken? BoxCommaToken { get; }

    public SyntaxToken? BoxToken { get; }

    public LineBoxKind BoxKind { get; }
}

public sealed class CircleStatementSyntax : StatementSyntax
{
    public CircleStatementSyntax(
        SyntaxToken circleKeyword,
        SyntaxToken? stepKeyword,
        GraphicsPointSyntax centerPoint,
        SyntaxToken commaToken,
        ExpressionSyntax radiusExpression,
        SyntaxToken? colorCommaToken,
        ExpressionSyntax? colorExpression,
        SyntaxToken? startCommaToken,
        ExpressionSyntax? startExpression,
        SyntaxToken? endCommaToken,
        ExpressionSyntax? endExpression,
        SyntaxToken? aspectCommaToken,
        ExpressionSyntax? aspectExpression)
    {
        ArgumentNullException.ThrowIfNull(circleKeyword);
        ArgumentNullException.ThrowIfNull(centerPoint);
        ArgumentNullException.ThrowIfNull(commaToken);
        ArgumentNullException.ThrowIfNull(radiusExpression);

        CircleKeyword = circleKeyword;
        StepKeyword = stepKeyword;
        CenterPoint = centerPoint;
        CommaToken = commaToken;
        RadiusExpression = radiusExpression;
        ColorCommaToken = colorCommaToken;
        ColorExpression = colorExpression;
        StartCommaToken = startCommaToken;
        StartExpression = startExpression;
        EndCommaToken = endCommaToken;
        EndExpression = endExpression;
        AspectCommaToken = aspectCommaToken;
        AspectExpression = aspectExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.CircleStatement;

    public SyntaxToken CircleKeyword { get; }

    public SyntaxToken? StepKeyword { get; }

    public bool IsRelative => StepKeyword is not null;

    public GraphicsPointSyntax CenterPoint { get; }

    public SyntaxToken CommaToken { get; }

    public ExpressionSyntax RadiusExpression { get; }

    public SyntaxToken? ColorCommaToken { get; }

    public ExpressionSyntax? ColorExpression { get; }

    public SyntaxToken? StartCommaToken { get; }

    public ExpressionSyntax? StartExpression { get; }

    public SyntaxToken? EndCommaToken { get; }

    public ExpressionSyntax? EndExpression { get; }

    public SyntaxToken? AspectCommaToken { get; }

    public ExpressionSyntax? AspectExpression { get; }
}

public sealed class PaintStatementSyntax : StatementSyntax
{
    public PaintStatementSyntax(
        SyntaxToken paintKeyword,
        SyntaxToken? stepKeyword,
        GraphicsPointSyntax point,
        SyntaxToken? fillCommaToken,
        ExpressionSyntax? fillColorExpression,
        SyntaxToken? borderCommaToken,
        ExpressionSyntax? borderColorExpression,
        SyntaxToken? backgroundCommaToken,
        ExpressionSyntax? backgroundExpression)
    {
        ArgumentNullException.ThrowIfNull(paintKeyword);
        ArgumentNullException.ThrowIfNull(point);

        PaintKeyword = paintKeyword;
        StepKeyword = stepKeyword;
        Point = point;
        FillCommaToken = fillCommaToken;
        FillColorExpression = fillColorExpression;
        BorderCommaToken = borderCommaToken;
        BorderColorExpression = borderColorExpression;
        BackgroundCommaToken = backgroundCommaToken;
        BackgroundExpression = backgroundExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.PaintStatement;

    public SyntaxToken PaintKeyword { get; }

    public SyntaxToken? StepKeyword { get; }

    public bool IsRelative => StepKeyword is not null;

    public GraphicsPointSyntax Point { get; }

    public SyntaxToken? FillCommaToken { get; }

    public ExpressionSyntax? FillColorExpression { get; }

    public SyntaxToken? BorderCommaToken { get; }

    public ExpressionSyntax? BorderColorExpression { get; }

    public SyntaxToken? BackgroundCommaToken { get; }

    public ExpressionSyntax? BackgroundExpression { get; }
}

public sealed class DrawStatementSyntax : StatementSyntax
{
    public DrawStatementSyntax(SyntaxToken drawKeyword, ExpressionSyntax commandExpression)
    {
        ArgumentNullException.ThrowIfNull(drawKeyword);
        ArgumentNullException.ThrowIfNull(commandExpression);
        DrawKeyword = drawKeyword;
        CommandExpression = commandExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawStatement;

    public SyntaxToken DrawKeyword { get; }

    public ExpressionSyntax CommandExpression { get; }
}

public sealed class DrawLineStatementSyntax : StatementSyntax
{
    public DrawLineStatementSyntax(
        SyntaxToken drawLineKeyword,
        ExpressionSyntax x1Expression,
        SyntaxToken x1CommaToken,
        ExpressionSyntax y1Expression,
        SyntaxToken y1CommaToken,
        ExpressionSyntax x2Expression,
        SyntaxToken x2CommaToken,
        ExpressionSyntax y2Expression,
        SyntaxToken y2CommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(drawLineKeyword);
        ArgumentNullException.ThrowIfNull(x1Expression);
        ArgumentNullException.ThrowIfNull(x1CommaToken);
        ArgumentNullException.ThrowIfNull(y1Expression);
        ArgumentNullException.ThrowIfNull(y1CommaToken);
        ArgumentNullException.ThrowIfNull(x2Expression);
        ArgumentNullException.ThrowIfNull(x2CommaToken);
        ArgumentNullException.ThrowIfNull(y2Expression);
        ArgumentNullException.ThrowIfNull(y2CommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        DrawLineKeyword = drawLineKeyword;
        X1Expression = x1Expression;
        X1CommaToken = x1CommaToken;
        Y1Expression = y1Expression;
        Y1CommaToken = y1CommaToken;
        X2Expression = x2Expression;
        X2CommaToken = x2CommaToken;
        Y2Expression = y2Expression;
        Y2CommaToken = y2CommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawLineStatement;

    public SyntaxToken DrawLineKeyword { get; }

    public ExpressionSyntax X1Expression { get; }

    public SyntaxToken X1CommaToken { get; }

    public ExpressionSyntax Y1Expression { get; }

    public SyntaxToken Y1CommaToken { get; }

    public ExpressionSyntax X2Expression { get; }

    public SyntaxToken X2CommaToken { get; }

    public ExpressionSyntax Y2Expression { get; }

    public SyntaxToken Y2CommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class DrawCircleStatementSyntax : StatementSyntax
{
    public DrawCircleStatementSyntax(
        SyntaxToken drawCircleKeyword,
        ExpressionSyntax xExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax radiusExpression,
        SyntaxToken radiusCommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(drawCircleKeyword);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(radiusExpression);
        ArgumentNullException.ThrowIfNull(radiusCommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        DrawCircleKeyword = drawCircleKeyword;
        XExpression = xExpression;
        XCommaToken = xCommaToken;
        YExpression = yExpression;
        YCommaToken = yCommaToken;
        RadiusExpression = radiusExpression;
        RadiusCommaToken = radiusCommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawCircleStatement;

    public SyntaxToken DrawCircleKeyword { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax RadiusExpression { get; }

    public SyntaxToken RadiusCommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class FillCircleStatementSyntax : StatementSyntax
{
    public FillCircleStatementSyntax(
        SyntaxToken fillCircleKeyword,
        ExpressionSyntax xExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax radiusExpression,
        SyntaxToken radiusCommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(fillCircleKeyword);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(radiusExpression);
        ArgumentNullException.ThrowIfNull(radiusCommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        FillCircleKeyword = fillCircleKeyword;
        XExpression = xExpression;
        XCommaToken = xCommaToken;
        YExpression = yExpression;
        YCommaToken = yCommaToken;
        RadiusExpression = radiusExpression;
        RadiusCommaToken = radiusCommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.FillCircleStatement;

    public SyntaxToken FillCircleKeyword { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax RadiusExpression { get; }

    public SyntaxToken RadiusCommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class DrawTextStatementSyntax : StatementSyntax
{
    public DrawTextStatementSyntax(
        SyntaxToken drawTextKeyword,
        ExpressionSyntax textExpression,
        SyntaxToken textCommaToken,
        ExpressionSyntax xExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(drawTextKeyword);
        ArgumentNullException.ThrowIfNull(textExpression);
        ArgumentNullException.ThrowIfNull(textCommaToken);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        DrawTextKeyword = drawTextKeyword;
        TextExpression = textExpression;
        TextCommaToken = textCommaToken;
        XExpression = xExpression;
        XCommaToken = xCommaToken;
        YExpression = yExpression;
        YCommaToken = yCommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawTextStatement;

    public SyntaxToken DrawTextKeyword { get; }

    public ExpressionSyntax TextExpression { get; }

    public SyntaxToken TextCommaToken { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class DrawImageStatementSyntax : StatementSyntax
{
    public DrawImageStatementSyntax(
        SyntaxToken drawImageKeyword,
        ExpressionSyntax imageExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax xExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken? widthCommaToken,
        ExpressionSyntax? widthExpression,
        SyntaxToken? heightCommaToken,
        ExpressionSyntax? heightExpression)
    {
        ArgumentNullException.ThrowIfNull(drawImageKeyword);
        ArgumentNullException.ThrowIfNull(imageExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);

        DrawImageKeyword = drawImageKeyword;
        ImageExpression = imageExpression;
        XCommaToken = xCommaToken;
        XExpression = xExpression;
        YCommaToken = yCommaToken;
        YExpression = yExpression;
        WidthCommaToken = widthCommaToken;
        WidthExpression = widthExpression;
        HeightCommaToken = heightCommaToken;
        HeightExpression = heightExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawImageStatement;

    public SyntaxToken DrawImageKeyword { get; }

    public ExpressionSyntax ImageExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken? WidthCommaToken { get; }

    public ExpressionSyntax? WidthExpression { get; }

    public SyntaxToken? HeightCommaToken { get; }

    public ExpressionSyntax? HeightExpression { get; }

    public bool HasScale => WidthExpression is not null || HeightExpression is not null;
}

public sealed class DrawRectStatementSyntax : StatementSyntax
{
    public DrawRectStatementSyntax(
        SyntaxToken drawRectKeyword,
        ExpressionSyntax xExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax widthExpression,
        SyntaxToken widthCommaToken,
        ExpressionSyntax heightExpression,
        SyntaxToken heightCommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(drawRectKeyword);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(widthCommaToken);
        ArgumentNullException.ThrowIfNull(heightExpression);
        ArgumentNullException.ThrowIfNull(heightCommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        DrawRectKeyword = drawRectKeyword;
        XExpression = xExpression;
        XCommaToken = xCommaToken;
        YExpression = yExpression;
        YCommaToken = yCommaToken;
        WidthExpression = widthExpression;
        WidthCommaToken = widthCommaToken;
        HeightExpression = heightExpression;
        HeightCommaToken = heightCommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawRectStatement;

    public SyntaxToken DrawRectKeyword { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax WidthExpression { get; }

    public SyntaxToken WidthCommaToken { get; }

    public ExpressionSyntax HeightExpression { get; }

    public SyntaxToken HeightCommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class FillRectStatementSyntax : StatementSyntax
{
    public FillRectStatementSyntax(
        SyntaxToken fillRectKeyword,
        ExpressionSyntax xExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax widthExpression,
        SyntaxToken widthCommaToken,
        ExpressionSyntax heightExpression,
        SyntaxToken heightCommaToken,
        ExpressionSyntax colorExpression)
    {
        ArgumentNullException.ThrowIfNull(fillRectKeyword);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(widthCommaToken);
        ArgumentNullException.ThrowIfNull(heightExpression);
        ArgumentNullException.ThrowIfNull(heightCommaToken);
        ArgumentNullException.ThrowIfNull(colorExpression);

        FillRectKeyword = fillRectKeyword;
        XExpression = xExpression;
        XCommaToken = xCommaToken;
        YExpression = yExpression;
        YCommaToken = yCommaToken;
        WidthExpression = widthExpression;
        WidthCommaToken = widthCommaToken;
        HeightExpression = heightExpression;
        HeightCommaToken = heightCommaToken;
        ColorExpression = colorExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.FillRectStatement;

    public SyntaxToken FillRectKeyword { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax WidthExpression { get; }

    public SyntaxToken WidthCommaToken { get; }

    public ExpressionSyntax HeightExpression { get; }

    public SyntaxToken HeightCommaToken { get; }

    public ExpressionSyntax ColorExpression { get; }
}

public sealed class DrawSpriteStatementSyntax : StatementSyntax
{
    public DrawSpriteStatementSyntax(
        SyntaxToken drawSpriteKeyword,
        ExpressionSyntax spriteExpression,
        SyntaxToken frameCommaToken,
        ExpressionSyntax frameExpression,
        SyntaxToken xCommaToken,
        ExpressionSyntax xExpression,
        SyntaxToken yCommaToken,
        ExpressionSyntax yExpression,
        SyntaxToken? widthCommaToken,
        ExpressionSyntax? widthExpression,
        SyntaxToken? heightCommaToken,
        ExpressionSyntax? heightExpression)
    {
        ArgumentNullException.ThrowIfNull(drawSpriteKeyword);
        ArgumentNullException.ThrowIfNull(spriteExpression);
        ArgumentNullException.ThrowIfNull(frameCommaToken);
        ArgumentNullException.ThrowIfNull(frameExpression);
        ArgumentNullException.ThrowIfNull(xCommaToken);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yCommaToken);
        ArgumentNullException.ThrowIfNull(yExpression);

        DrawSpriteKeyword = drawSpriteKeyword;
        SpriteExpression = spriteExpression;
        FrameCommaToken = frameCommaToken;
        FrameExpression = frameExpression;
        XCommaToken = xCommaToken;
        XExpression = xExpression;
        YCommaToken = yCommaToken;
        YExpression = yExpression;
        WidthCommaToken = widthCommaToken;
        WidthExpression = widthExpression;
        HeightCommaToken = heightCommaToken;
        HeightExpression = heightExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.DrawSpriteStatement;

    public SyntaxToken DrawSpriteKeyword { get; }

    public ExpressionSyntax SpriteExpression { get; }

    public SyntaxToken FrameCommaToken { get; }

    public ExpressionSyntax FrameExpression { get; }

    public SyntaxToken XCommaToken { get; }

    public ExpressionSyntax XExpression { get; }

    public SyntaxToken YCommaToken { get; }

    public ExpressionSyntax YExpression { get; }

    public SyntaxToken? WidthCommaToken { get; }

    public ExpressionSyntax? WidthExpression { get; }

    public SyntaxToken? HeightCommaToken { get; }

    public ExpressionSyntax? HeightExpression { get; }

    public bool HasScale => WidthExpression is not null || HeightExpression is not null;
}
