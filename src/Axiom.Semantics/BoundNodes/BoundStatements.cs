using Axiom.Core;

namespace Axiom.Semantics;

public sealed class BoundArrayBound
{
    public BoundArrayBound(BoundExpression lowerBound, BoundExpression upperBound)
    {
        ArgumentNullException.ThrowIfNull(lowerBound);
        ArgumentNullException.ThrowIfNull(upperBound);

        LowerBound = lowerBound;
        UpperBound = upperBound;
    }

    public BoundExpression LowerBound { get; }

    public BoundExpression UpperBound { get; }
}

public sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public BoundVariableDeclarationStatement(
        TextLocation location,
        VariableSymbol variable,
        BoundExpression? initializer,
        IReadOnlyList<BoundArrayBound> arrayBounds,
        TextLocation? identifierLocation = null)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(arrayBounds);

        Variable = variable;
        Initializer = initializer;
        ArrayBounds = arrayBounds;
        IdentifierLocation = identifierLocation ?? location;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

    public VariableSymbol Variable { get; }

    public TextLocation IdentifierLocation { get; }

    public BoundExpression? Initializer { get; }

    public IReadOnlyList<BoundArrayBound> ArrayBounds { get; }

    public BoundExpression? LowerBound => ArrayBounds.Count == 0 ? null : ArrayBounds[0].LowerBound;

    public BoundExpression? UpperBound => ArrayBounds.Count == 0 ? null : ArrayBounds[0].UpperBound;
}

public sealed class BoundVariableDeclarationsStatement : BoundStatement
{
    public BoundVariableDeclarationsStatement(TextLocation location, IReadOnlyList<BoundVariableDeclarationStatement> declarations)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        Declarations = declarations;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationsStatement;

    public IReadOnlyList<BoundVariableDeclarationStatement> Declarations { get; }
}

public sealed class BoundAssignmentStatement : BoundStatement
{
    public BoundAssignmentStatement(TextLocation location, BoundExpression target, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(expression);

        Target = target;
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

    public BoundExpression Target { get; }

    public BoundExpression Expression { get; }
}

public sealed class BoundEraseStatement : BoundStatement
{
    public BoundEraseStatement(TextLocation location, BoundExpression target, string displayName)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Target = target;
        DisplayName = displayName;
    }

    public override BoundNodeKind Kind => BoundNodeKind.EraseStatement;

    public BoundExpression Target { get; }

    public string DisplayName { get; }

    public VariableSymbol? Variable => (Target as BoundVariableExpression)?.Variable;
}

public sealed class BoundRedimStatement : BoundStatement
{
    public BoundRedimStatement(
        TextLocation location,
        BoundExpression target,
        string displayName,
        bool preserve,
        IReadOnlyList<BoundArrayBound> arrayBounds)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(arrayBounds);

        Target = target;
        DisplayName = displayName;
        Preserve = preserve;
        ArrayBounds = arrayBounds;
    }

    public override BoundNodeKind Kind => BoundNodeKind.RedimStatement;

    public BoundExpression Target { get; }

    public string DisplayName { get; }

    public VariableSymbol? Variable => (Target as BoundVariableExpression)?.Variable;

    public bool Preserve { get; }

    public IReadOnlyList<BoundArrayBound> ArrayBounds { get; }

    public BoundExpression LowerBound => ArrayBounds[0].LowerBound;

    public BoundExpression UpperBound => ArrayBounds[0].UpperBound;
}

public enum PrintSeparatorKind
{
    Semicolon,
    Comma
}

public sealed class BoundPrintItem
{
    public BoundPrintItem(BoundExpression expression, PrintSeparatorKind? separatorKind)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression = expression;
        SeparatorKind = separatorKind;
    }

    public BoundExpression Expression { get; }

    public PrintSeparatorKind? SeparatorKind { get; }
}

public sealed class BoundPrintStatement : BoundStatement
{
    public BoundPrintStatement(TextLocation location, IReadOnlyList<BoundPrintItem> items)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PrintStatement;

    public IReadOnlyList<BoundPrintItem> Items { get; }
}

public sealed class BoundPrintUsingStatement : BoundStatement
{
    public BoundPrintUsingStatement(TextLocation location, BoundExpression formatExpression, IReadOnlyList<BoundPrintItem> items)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(formatExpression);
        ArgumentNullException.ThrowIfNull(items);

        FormatExpression = formatExpression;
        Items = items;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PrintUsingStatement;

    public BoundExpression FormatExpression { get; }

    public IReadOnlyList<BoundPrintItem> Items { get; }
}

public sealed class BoundInputStatement : BoundStatement
{
    public BoundInputStatement(TextLocation location, IReadOnlyList<VariableSymbol> variables, string? promptText, bool appendQuestionMark = true)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variables);
        Variables = variables;
        PromptText = promptText;
        AppendQuestionMark = appendQuestionMark;
    }

    public override BoundNodeKind Kind => BoundNodeKind.InputStatement;

    public IReadOnlyList<VariableSymbol> Variables { get; }

    public string? PromptText { get; }
    public bool AppendQuestionMark { get; }
    public string DisplayPrompt => PromptText is null ? "? " : PromptText + (AppendQuestionMark ? "? " : string.Empty);
}

public sealed class BoundSleepStatement : BoundStatement
{
    public BoundSleepStatement(TextLocation location, BoundExpression? durationExpression)
        : base(location)
    {
        DurationExpression = durationExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SleepStatement;

    public BoundExpression? DurationExpression { get; }
}

public sealed class BoundRandomizeStatement : BoundStatement
{
    public BoundRandomizeStatement(TextLocation location, BoundExpression? seedExpression)
        : base(location)
    {
        SeedExpression = seedExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.RandomizeStatement;

    public BoundExpression? SeedExpression { get; }
}

public sealed class BoundScreenStatement : BoundStatement
{
    public BoundScreenStatement(
        TextLocation location,
        BoundExpression modeExpression,
        BoundExpression? colorSwitchExpression,
        BoundExpression? activePageExpression,
        BoundExpression? visualPageExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(modeExpression);
        ModeExpression = modeExpression;
        ColorSwitchExpression = colorSwitchExpression;
        ActivePageExpression = activePageExpression;
        VisualPageExpression = visualPageExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ScreenStatement;

    public BoundExpression ModeExpression { get; }

    public BoundExpression? ColorSwitchExpression { get; }

    public BoundExpression? ActivePageExpression { get; }

    public BoundExpression? VisualPageExpression { get; }
}

public sealed class BoundCanvasStatement : BoundStatement
{
    public BoundCanvasStatement(TextLocation location, BoundExpression widthExpression, BoundExpression heightExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(heightExpression);
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CanvasStatement;

    public BoundExpression WidthExpression { get; }

    public BoundExpression HeightExpression { get; }
}

public sealed class BoundBeginFrameStatement : BoundStatement
{
    public BoundBeginFrameStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.BeginFrameStatement;
}

public sealed class BoundSmoothStatement : BoundStatement
{
    public BoundSmoothStatement(TextLocation location, bool enabled)
        : base(location)
    {
        Enabled = enabled;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SmoothStatement;

    public bool Enabled { get; }
}

public sealed class BoundPresentStatement : BoundStatement
{
    public BoundPresentStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.PresentStatement;
}

public sealed class BoundCameraStatement : BoundStatement
{
    public BoundCameraStatement(TextLocation location, BoundExpression xExpression, BoundExpression yExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        XExpression = xExpression;
        YExpression = yExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CameraStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }
}

public sealed class BoundViewportStatement : BoundStatement
{
    public BoundViewportStatement(
        TextLocation location,
        BoundExpression? leftExpression,
        BoundExpression? topExpression,
        BoundExpression? rightExpression,
        BoundExpression? bottomExpression)
        : base(location)
    {
        LeftExpression = leftExpression;
        TopExpression = topExpression;
        RightExpression = rightExpression;
        BottomExpression = bottomExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ViewportStatement;

    public BoundExpression? LeftExpression { get; }

    public BoundExpression? TopExpression { get; }

    public BoundExpression? RightExpression { get; }

    public BoundExpression? BottomExpression { get; }

    public bool IsReset => LeftExpression is null || TopExpression is null || RightExpression is null || BottomExpression is null;
}

public sealed class BoundResetCameraStatement : BoundStatement
{
    public BoundResetCameraStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ResetCameraStatement;
}

public sealed class BoundPCopyStatement : BoundStatement
{
    public BoundPCopyStatement(TextLocation location, BoundExpression sourcePageExpression, BoundExpression destinationPageExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(sourcePageExpression);
        ArgumentNullException.ThrowIfNull(destinationPageExpression);

        SourcePageExpression = sourcePageExpression;
        DestinationPageExpression = destinationPageExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PCopyStatement;

    public BoundExpression SourcePageExpression { get; }

    public BoundExpression DestinationPageExpression { get; }
}

public sealed class BoundViewStatement : BoundStatement
{
    public BoundViewStatement(
        TextLocation location,
        bool useScreenCoordinates,
        BoundExpression? leftExpression,
        BoundExpression? topExpression,
        BoundExpression? rightExpression,
        BoundExpression? bottomExpression,
        BoundExpression? fillColorExpression,
        BoundExpression? borderColorExpression)
        : base(location)
    {
        UseScreenCoordinates = useScreenCoordinates;
        LeftExpression = leftExpression;
        TopExpression = topExpression;
        RightExpression = rightExpression;
        BottomExpression = bottomExpression;
        FillColorExpression = fillColorExpression;
        BorderColorExpression = borderColorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ViewStatement;

    public bool UseScreenCoordinates { get; }

    public BoundExpression? LeftExpression { get; }

    public BoundExpression? TopExpression { get; }

    public BoundExpression? RightExpression { get; }

    public BoundExpression? BottomExpression { get; }

    public BoundExpression? FillColorExpression { get; }

    public BoundExpression? BorderColorExpression { get; }

    public bool IsReset => LeftExpression is null || TopExpression is null || RightExpression is null || BottomExpression is null;
}

public sealed class BoundViewPrintStatement : BoundStatement
{
    public BoundViewPrintStatement(TextLocation location, BoundExpression? topExpression, BoundExpression? bottomExpression)
        : base(location)
    {
        TopExpression = topExpression;
        BottomExpression = bottomExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ViewPrintStatement;

    public BoundExpression? TopExpression { get; }

    public BoundExpression? BottomExpression { get; }

    public bool IsReset => TopExpression is null || BottomExpression is null;
}

public sealed class BoundWindowStatement : BoundStatement
{
    public BoundWindowStatement(
        TextLocation location,
        bool useScreenCoordinates,
        BoundExpression? leftExpression,
        BoundExpression? topExpression,
        BoundExpression? rightExpression,
        BoundExpression? bottomExpression)
        : base(location)
    {
        UseScreenCoordinates = useScreenCoordinates;
        LeftExpression = leftExpression;
        TopExpression = topExpression;
        RightExpression = rightExpression;
        BottomExpression = bottomExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.WindowStatement;

    public bool UseScreenCoordinates { get; }

    public BoundExpression? LeftExpression { get; }

    public BoundExpression? TopExpression { get; }

    public BoundExpression? RightExpression { get; }

    public BoundExpression? BottomExpression { get; }

    public bool IsReset => LeftExpression is null || TopExpression is null || RightExpression is null || BottomExpression is null;
}

public sealed class BoundClsStatement : BoundStatement
{
    public BoundClsStatement(TextLocation location, BoundExpression? colorExpression)
        : base(location)
    {
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ClsStatement;

    public BoundExpression? ColorExpression { get; }
}

public enum BoundColorBackgroundMode
{
    Unchanged,
    Color,
    Transparent
}

public sealed class BoundColorStatement : BoundStatement
{
    public BoundColorStatement(
        TextLocation location,
        BoundExpression foregroundExpression,
        BoundColorBackgroundMode backgroundMode,
        BoundExpression? backgroundExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(foregroundExpression);
        ForegroundExpression = foregroundExpression;
        BackgroundMode = backgroundMode;
        BackgroundExpression = backgroundExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ColorStatement;

    public BoundExpression ForegroundExpression { get; }

    public BoundColorBackgroundMode BackgroundMode { get; }

    public BoundExpression? BackgroundExpression { get; }
}

public sealed class BoundLocateStatement : BoundStatement
{
    public BoundLocateStatement(TextLocation location, BoundExpression rowExpression, BoundExpression columnExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(rowExpression);
        ArgumentNullException.ThrowIfNull(columnExpression);
        RowExpression = rowExpression;
        ColumnExpression = columnExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LocateStatement;

    public BoundExpression RowExpression { get; }

    public BoundExpression ColumnExpression { get; }
}

public sealed class BoundPaletteStatement : BoundStatement
{
    public BoundPaletteStatement(TextLocation location, BoundExpression? indexExpression, BoundExpression? colorExpression, BoundExpression? valuesExpression)
        : base(location)
    {
        IndexExpression = indexExpression;
        ColorExpression = colorExpression;
        ValuesExpression = valuesExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PaletteStatement;

    public BoundExpression? IndexExpression { get; }

    public BoundExpression? ColorExpression { get; }

    public BoundExpression? ValuesExpression { get; }

    public bool IsReset => ValuesExpression is null && (IndexExpression is null || ColorExpression is null);

    public bool IsUsing => ValuesExpression is not null;
}

public enum BoundLineBoxKind
{
    None,
    Border,
    Filled
}

public enum BoundPutActionKind
{
    PSet,
    Preset,
    And,
    Or,
    Xor
}

public sealed class BoundPSetStatement : BoundStatement
{
    public BoundPSetStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression? colorExpression,
        bool reset,
        bool isRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        XExpression = xExpression;
        YExpression = yExpression;
        ColorExpression = colorExpression;
        Reset = reset;
        IsRelative = isRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PSetStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression? ColorExpression { get; }

    public bool Reset { get; }

    public bool IsRelative { get; }
}

public sealed class BoundGetStatement : BoundStatement
{
    public BoundGetStatement(
        TextLocation location,
        BoundExpression x1Expression,
        BoundExpression y1Expression,
        BoundExpression x2Expression,
        BoundExpression y2Expression,
        BoundExpression bufferExpression,
        bool isStartRelative,
        bool isEndRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(x1Expression);
        ArgumentNullException.ThrowIfNull(y1Expression);
        ArgumentNullException.ThrowIfNull(x2Expression);
        ArgumentNullException.ThrowIfNull(y2Expression);
        ArgumentNullException.ThrowIfNull(bufferExpression);
        X1Expression = x1Expression;
        Y1Expression = y1Expression;
        X2Expression = x2Expression;
        Y2Expression = y2Expression;
        BufferExpression = bufferExpression;
        IsStartRelative = isStartRelative;
        IsEndRelative = isEndRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.GetStatement;

    public BoundExpression X1Expression { get; }

    public BoundExpression Y1Expression { get; }

    public BoundExpression X2Expression { get; }

    public BoundExpression Y2Expression { get; }

    public BoundExpression BufferExpression { get; }

    public bool IsStartRelative { get; }

    public bool IsEndRelative { get; }
}

public sealed class BoundPutStatement : BoundStatement
{
    public BoundPutStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression bufferExpression,
        BoundPutActionKind action,
        bool isRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(bufferExpression);
        XExpression = xExpression;
        YExpression = yExpression;
        BufferExpression = bufferExpression;
        Action = action;
        IsRelative = isRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PutStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression BufferExpression { get; }

    public BoundPutActionKind Action { get; }

    public bool IsRelative { get; }
}

public sealed class BoundLineStatement : BoundStatement
{
    public BoundLineStatement(
        TextLocation location,
        BoundExpression? x1Expression,
        BoundExpression? y1Expression,
        BoundExpression x2Expression,
        BoundExpression y2Expression,
        BoundExpression? colorExpression,
        BoundLineBoxKind boxKind,
        bool hasExplicitStartPoint,
        bool isStartRelative,
        bool isEndRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(x2Expression);
        ArgumentNullException.ThrowIfNull(y2Expression);
        X1Expression = x1Expression;
        Y1Expression = y1Expression;
        X2Expression = x2Expression;
        Y2Expression = y2Expression;
        ColorExpression = colorExpression;
        BoxKind = boxKind;
        HasExplicitStartPoint = hasExplicitStartPoint;
        IsStartRelative = isStartRelative;
        IsEndRelative = isEndRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LineStatement;

    public BoundExpression? X1Expression { get; }

    public BoundExpression? Y1Expression { get; }

    public BoundExpression X2Expression { get; }

    public BoundExpression Y2Expression { get; }

    public BoundExpression? ColorExpression { get; }

    public BoundLineBoxKind BoxKind { get; }

    public bool HasExplicitStartPoint { get; }

    public bool IsStartRelative { get; }

    public bool IsEndRelative { get; }
}

public sealed class BoundCircleStatement : BoundStatement
{
    public BoundCircleStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression radiusExpression,
        BoundExpression? colorExpression,
        BoundExpression? startExpression,
        BoundExpression? endExpression,
        BoundExpression? aspectExpression,
        bool isRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(radiusExpression);
        XExpression = xExpression;
        YExpression = yExpression;
        RadiusExpression = radiusExpression;
        ColorExpression = colorExpression;
        StartExpression = startExpression;
        EndExpression = endExpression;
        AspectExpression = aspectExpression;
        IsRelative = isRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CircleStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression RadiusExpression { get; }

    public BoundExpression? ColorExpression { get; }

    public BoundExpression? StartExpression { get; }

    public BoundExpression? EndExpression { get; }

    public BoundExpression? AspectExpression { get; }

    public bool IsRelative { get; }
}

public sealed class BoundPaintStatement : BoundStatement
{
    public BoundPaintStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression? fillColorExpression,
        BoundExpression? borderColorExpression,
        BoundExpression? backgroundExpression,
        bool isRelative)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        XExpression = xExpression;
        YExpression = yExpression;
        FillColorExpression = fillColorExpression;
        BorderColorExpression = borderColorExpression;
        BackgroundExpression = backgroundExpression;
        IsRelative = isRelative;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PaintStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression? FillColorExpression { get; }

    public BoundExpression? BorderColorExpression { get; }

    public BoundExpression? BackgroundExpression { get; }

    public bool IsRelative { get; }
}

public sealed class BoundDrawStatement : BoundStatement
{
    public BoundDrawStatement(TextLocation location, BoundExpression commandExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(commandExpression);
        CommandExpression = commandExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawStatement;

    public BoundExpression CommandExpression { get; }
}

public sealed class BoundDrawLineStatement : BoundStatement
{
    public BoundDrawLineStatement(
        TextLocation location,
        BoundExpression x1Expression,
        BoundExpression y1Expression,
        BoundExpression x2Expression,
        BoundExpression y2Expression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(x1Expression);
        ArgumentNullException.ThrowIfNull(y1Expression);
        ArgumentNullException.ThrowIfNull(x2Expression);
        ArgumentNullException.ThrowIfNull(y2Expression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        X1Expression = x1Expression;
        Y1Expression = y1Expression;
        X2Expression = x2Expression;
        Y2Expression = y2Expression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawLineStatement;

    public BoundExpression X1Expression { get; }

    public BoundExpression Y1Expression { get; }

    public BoundExpression X2Expression { get; }

    public BoundExpression Y2Expression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundDrawCircleStatement : BoundStatement
{
    public BoundDrawCircleStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression radiusExpression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(radiusExpression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        XExpression = xExpression;
        YExpression = yExpression;
        RadiusExpression = radiusExpression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawCircleStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression RadiusExpression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundFillCircleStatement : BoundStatement
{
    public BoundFillCircleStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression radiusExpression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(radiusExpression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        XExpression = xExpression;
        YExpression = yExpression;
        RadiusExpression = radiusExpression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.FillCircleStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression RadiusExpression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundDrawTextStatement : BoundStatement
{
    public BoundDrawTextStatement(
        TextLocation location,
        BoundExpression textExpression,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(textExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        TextExpression = textExpression;
        XExpression = xExpression;
        YExpression = yExpression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawTextStatement;

    public BoundExpression TextExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundDrawImageStatement : BoundStatement
{
    public BoundDrawImageStatement(
        TextLocation location,
        BoundExpression imageExpression,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression? widthExpression,
        BoundExpression? heightExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(imageExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);

        ImageExpression = imageExpression;
        XExpression = xExpression;
        YExpression = yExpression;
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawImageStatement;

    public BoundExpression ImageExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression? WidthExpression { get; }

    public BoundExpression? HeightExpression { get; }

    public bool HasScale => WidthExpression is not null || HeightExpression is not null;
}

public sealed class BoundDrawRectStatement : BoundStatement
{
    public BoundDrawRectStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression widthExpression,
        BoundExpression heightExpression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(heightExpression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        XExpression = xExpression;
        YExpression = yExpression;
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawRectStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression WidthExpression { get; }

    public BoundExpression HeightExpression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundFillRectStatement : BoundStatement
{
    public BoundFillRectStatement(
        TextLocation location,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression widthExpression,
        BoundExpression heightExpression,
        BoundExpression colorExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);
        ArgumentNullException.ThrowIfNull(widthExpression);
        ArgumentNullException.ThrowIfNull(heightExpression);
        ArgumentNullException.ThrowIfNull(colorExpression);

        XExpression = xExpression;
        YExpression = yExpression;
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
        ColorExpression = colorExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.FillRectStatement;

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression WidthExpression { get; }

    public BoundExpression HeightExpression { get; }

    public BoundExpression ColorExpression { get; }
}

public sealed class BoundDrawSpriteStatement : BoundStatement
{
    public BoundDrawSpriteStatement(
        TextLocation location,
        BoundExpression spriteExpression,
        BoundExpression frameExpression,
        BoundExpression xExpression,
        BoundExpression yExpression,
        BoundExpression? widthExpression,
        BoundExpression? heightExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(spriteExpression);
        ArgumentNullException.ThrowIfNull(frameExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);

        SpriteExpression = spriteExpression;
        FrameExpression = frameExpression;
        XExpression = xExpression;
        YExpression = yExpression;
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawSpriteStatement;

    public BoundExpression SpriteExpression { get; }

    public BoundExpression FrameExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }

    public BoundExpression? WidthExpression { get; }

    public BoundExpression? HeightExpression { get; }

    public bool HasScale => WidthExpression is not null || HeightExpression is not null;
}

public sealed class BoundDrawTileStatement : BoundStatement
{
    public BoundDrawTileStatement(
        TextLocation location,
        BoundExpression tilesetExpression,
        BoundExpression tileIndexExpression,
        BoundExpression xExpression,
        BoundExpression yExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(tilesetExpression);
        ArgumentNullException.ThrowIfNull(tileIndexExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);

        TilesetExpression = tilesetExpression;
        TileIndexExpression = tileIndexExpression;
        XExpression = xExpression;
        YExpression = yExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawTileStatement;

    public BoundExpression TilesetExpression { get; }

    public BoundExpression TileIndexExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }
}

public sealed class BoundDrawTilemapStatement : BoundStatement
{
    public BoundDrawTilemapStatement(
        TextLocation location,
        BoundExpression tilemapExpression,
        BoundExpression xExpression,
        BoundExpression yExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(tilemapExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);

        TilemapExpression = tilemapExpression;
        XExpression = xExpression;
        YExpression = yExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawTilemapStatement;

    public BoundExpression TilemapExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }
}

public sealed class BoundDrawTileLayerStatement : BoundStatement
{
    public BoundDrawTileLayerStatement(
        TextLocation location,
        BoundExpression tilemapExpression,
        BoundExpression layerIndexExpression,
        BoundExpression xExpression,
        BoundExpression yExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(tilemapExpression);
        ArgumentNullException.ThrowIfNull(layerIndexExpression);
        ArgumentNullException.ThrowIfNull(xExpression);
        ArgumentNullException.ThrowIfNull(yExpression);

        TilemapExpression = tilemapExpression;
        LayerIndexExpression = layerIndexExpression;
        XExpression = xExpression;
        YExpression = yExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DrawTileLayerStatement;

    public BoundExpression TilemapExpression { get; }

    public BoundExpression LayerIndexExpression { get; }

    public BoundExpression XExpression { get; }

    public BoundExpression YExpression { get; }
}

public sealed class BoundSetMapTileStatement : BoundStatement
{
    public BoundSetMapTileStatement(
        TextLocation location,
        BoundExpression tilemapExpression,
        BoundExpression layerIndexExpression,
        BoundExpression columnExpression,
        BoundExpression rowExpression,
        BoundExpression tilesetIndexExpression,
        BoundExpression tileIndexExpression,
        BoundExpression? flagsExpression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(tilemapExpression);
        ArgumentNullException.ThrowIfNull(layerIndexExpression);
        ArgumentNullException.ThrowIfNull(columnExpression);
        ArgumentNullException.ThrowIfNull(rowExpression);
        ArgumentNullException.ThrowIfNull(tilesetIndexExpression);
        ArgumentNullException.ThrowIfNull(tileIndexExpression);

        TilemapExpression = tilemapExpression;
        LayerIndexExpression = layerIndexExpression;
        ColumnExpression = columnExpression;
        RowExpression = rowExpression;
        TilesetIndexExpression = tilesetIndexExpression;
        TileIndexExpression = tileIndexExpression;
        FlagsExpression = flagsExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SetMapTileStatement;

    public BoundExpression TilemapExpression { get; }

    public BoundExpression LayerIndexExpression { get; }

    public BoundExpression ColumnExpression { get; }

    public BoundExpression RowExpression { get; }

    public BoundExpression TilesetIndexExpression { get; }

    public BoundExpression TileIndexExpression { get; }

    public BoundExpression? FlagsExpression { get; }
}

public sealed class BoundOnKeyStatement : BoundStatement
{
    public BoundOnKeyStatement(
        TextLocation location,
        BoundExpression keyExpression,
        BoundFunctionExpression? handlerExpression,
        BoundOnKeyActionKind actionKind = BoundOnKeyActionKind.Register,
        bool handlerReceivesKeyText = false)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(keyExpression);

        KeyExpression = keyExpression;
        HandlerExpression = handlerExpression;
        ActionKind = actionKind;
        HandlerReceivesKeyText = handlerReceivesKeyText;
    }

    public override BoundNodeKind Kind => BoundNodeKind.OnKeyStatement;

    public BoundExpression KeyExpression { get; }

    public BoundFunctionExpression? HandlerExpression { get; }

    public BoundOnKeyActionKind ActionKind { get; }

    public bool HandlerReceivesKeyText { get; }

    public bool DisablesHandler => ActionKind == BoundOnKeyActionKind.Unregister;

    public bool PausesHandler => ActionKind == BoundOnKeyActionKind.Pause;

    public bool ResumesHandler => ActionKind == BoundOnKeyActionKind.Resume;
}

public enum BoundOnKeyActionKind
{
    Register,
    Unregister,
    Pause,
    Resume
}

public sealed class BoundOnMouseStatement : BoundStatement
{
    public BoundOnMouseStatement(
        TextLocation location,
        BoundExpression buttonExpression,
        BoundMouseEventKind eventKind,
        BoundFunctionExpression? handlerExpression,
        BoundOnMouseActionKind actionKind = BoundOnMouseActionKind.Register,
        bool handlerReceivesEvent = false)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(buttonExpression);

        ButtonExpression = buttonExpression;
        EventKind = eventKind;
        HandlerExpression = handlerExpression;
        ActionKind = actionKind;
        HandlerReceivesEvent = handlerReceivesEvent;
    }

    public override BoundNodeKind Kind => BoundNodeKind.OnMouseStatement;

    public BoundExpression ButtonExpression { get; }

    public BoundMouseEventKind EventKind { get; }

    public BoundFunctionExpression? HandlerExpression { get; }

    public BoundOnMouseActionKind ActionKind { get; }

    public bool HandlerReceivesEvent { get; }

    public bool DisablesHandler => ActionKind == BoundOnMouseActionKind.Unregister;

    public bool PausesHandler => ActionKind == BoundOnMouseActionKind.Pause;

    public bool ResumesHandler => ActionKind == BoundOnMouseActionKind.Resume;
}

public enum BoundOnMouseActionKind
{
    Register,
    Unregister,
    Pause,
    Resume
}

public enum BoundMouseEventKind
{
    Press,
    Release,
    Move,
    Wheel
}

public enum BoundFileOpenMode
{
    Input,
    Output,
    Append
}

public sealed class BoundOpenStatement : BoundStatement
{
    public BoundOpenStatement(TextLocation location, BoundExpression pathExpression, VariableSymbol fileVariable, BoundFileOpenMode mode, BoundExpression? encodingExpression = null)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(pathExpression);
        ArgumentNullException.ThrowIfNull(fileVariable);

        PathExpression = pathExpression;
        FileVariable = fileVariable;
        Mode = mode;
        EncodingExpression = encodingExpression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.OpenStatement;

    public BoundExpression PathExpression { get; }

    public VariableSymbol FileVariable { get; }

    public BoundFileOpenMode Mode { get; }

    public BoundExpression? EncodingExpression { get; }
}

public sealed class BoundCloseStatement : BoundStatement
{
    public BoundCloseStatement(TextLocation location, VariableSymbol fileVariable)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(fileVariable);
        FileVariable = fileVariable;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CloseStatement;

    public VariableSymbol FileVariable { get; }
}

public sealed class BoundErrorStatement : BoundStatement
{
    public BoundErrorStatement(TextLocation location, BoundExpression expression, BoundExpression? messageExpression, BoundErrorStatementMode mode)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression = expression;
        MessageExpression = messageExpression;
        Mode = mode;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ErrorStatement;

    public BoundExpression Expression { get; }

    public BoundExpression? MessageExpression { get; }

    public BoundErrorStatementMode Mode { get; }
}

public enum BoundErrorStatementMode
{
    UserCode,
    ExistingError
}

public sealed class BoundIfStatement : BoundStatement
{
    public BoundIfStatement(
        TextLocation location,
        BoundExpression condition,
        IReadOnlyList<BoundStatement> statements,
        IReadOnlyList<BoundStatement> elseStatements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(elseStatements);

        Condition = condition;
        Statements = statements;
        ElseStatements = elseStatements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

    public BoundExpression Condition { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }

    public IReadOnlyList<BoundStatement> ElseStatements { get; }
}

public sealed class BoundTryStatement : BoundStatement
{
    public BoundTryStatement(
        TextLocation location,
        IReadOnlyList<BoundStatement> statements,
        VariableSymbol? catchVariable,
        IReadOnlyList<BoundStatement> catchStatements,
        IReadOnlyList<BoundStatement> finallyStatements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(catchStatements);
        ArgumentNullException.ThrowIfNull(finallyStatements);

        Statements = statements;
        CatchVariable = catchVariable;
        CatchStatements = catchStatements;
        FinallyStatements = finallyStatements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.TryStatement;

    public IReadOnlyList<BoundStatement> Statements { get; }

    public VariableSymbol? CatchVariable { get; }

    public IReadOnlyList<BoundStatement> CatchStatements { get; }

    public IReadOnlyList<BoundStatement> FinallyStatements { get; }
}

public sealed class BoundWhileStatement : BoundStatement
{
    public BoundWhileStatement(TextLocation location, BoundExpression condition, IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(statements);

        Condition = condition;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

    public BoundExpression Condition { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public enum BoundDoLoopConditionKind
{
    None,
    While,
    Until
}

public sealed class BoundDoLoopStatement : BoundStatement
{
    public BoundDoLoopStatement(
        TextLocation location,
        BoundExpression? condition,
        BoundDoLoopConditionKind conditionKind,
        bool evaluateConditionAfterBody,
        IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(statements);

        Condition = condition;
        ConditionKind = conditionKind;
        EvaluateConditionAfterBody = evaluateConditionAfterBody;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DoLoopStatement;

    public BoundExpression? Condition { get; }

    public BoundDoLoopConditionKind ConditionKind { get; }

    public bool EvaluateConditionAfterBody { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public sealed class BoundForStatement : BoundStatement
{
    public BoundForStatement(
        TextLocation location,
        VariableSymbol variable,
        bool declaresVariable,
        BoundExpression startExpression,
        BoundExpression endExpression,
        BoundExpression stepExpression,
        IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(startExpression);
        ArgumentNullException.ThrowIfNull(endExpression);
        ArgumentNullException.ThrowIfNull(stepExpression);
        ArgumentNullException.ThrowIfNull(statements);

        Variable = variable;
        DeclaresVariable = declaresVariable;
        StartExpression = startExpression;
        EndExpression = endExpression;
        StepExpression = stepExpression;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

    public VariableSymbol Variable { get; }

    public bool DeclaresVariable { get; }

    public BoundExpression StartExpression { get; }

    public BoundExpression EndExpression { get; }

    public BoundExpression StepExpression { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public sealed class BoundExitForStatement : BoundStatement
{
    public BoundExitForStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExitForStatement;
}

public sealed class BoundExitDoStatement : BoundStatement
{
    public BoundExitDoStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExitDoStatement;
}

public sealed class BoundContinueForStatement : BoundStatement
{
    public BoundContinueForStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ContinueForStatement;
}

public sealed class BoundContinueWhileStatement : BoundStatement
{
    public BoundContinueWhileStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ContinueWhileStatement;
}

public sealed class BoundContinueDoStatement : BoundStatement
{
    public BoundContinueDoStatement(TextLocation location)
        : base(location)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ContinueDoStatement;
}

public sealed class BoundReturnStatement : BoundStatement
{
    public BoundReturnStatement(TextLocation location, BoundExpression? expression)
        : base(location)
    {
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

    public BoundExpression? Expression { get; }
}

public sealed class BoundExpressionStatement : BoundStatement
{
    public BoundExpressionStatement(TextLocation location, BoundExpression expression)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

    public BoundExpression Expression { get; }
}
