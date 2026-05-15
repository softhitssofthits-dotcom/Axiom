namespace Axiom.Syntax;

public sealed class ScreenStatementSyntax : StatementSyntax
{
    public ScreenStatementSyntax(SyntaxToken screenKeyword, ExpressionSyntax modeExpression)
    {
        ArgumentNullException.ThrowIfNull(screenKeyword);
        ArgumentNullException.ThrowIfNull(modeExpression);

        ScreenKeyword = screenKeyword;
        ModeExpression = modeExpression;
    }

    public override SyntaxKind Kind => SyntaxKind.ScreenStatement;

    public SyntaxToken ScreenKeyword { get; }

    public ExpressionSyntax ModeExpression { get; }
}

public sealed class ClsStatementSyntax : StatementSyntax
{
    public ClsStatementSyntax(SyntaxToken clsKeyword)
    {
        ArgumentNullException.ThrowIfNull(clsKeyword);
        ClsKeyword = clsKeyword;
    }

    public override SyntaxKind Kind => SyntaxKind.ClsStatement;

    public SyntaxToken ClsKeyword { get; }
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
