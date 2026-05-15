namespace Axiom.Runtime;

internal enum GraphicsDrawCommandKind
{
    Move,
    SetColor,
    SetScale,
    SetAngleQuadrant,
    SetAngleDegrees,
    MoveTo,
    Paint,
    UnsupportedHistoricalX
}

internal enum GraphicsDrawDirection
{
    Up,
    Down,
    Left,
    Right,
    UpRight,
    DownRight,
    DownLeft,
    UpLeft
}

internal readonly record struct GraphicsDrawCommand(
    GraphicsDrawCommandKind Kind,
    int Position,
    GraphicsDrawDirection Direction = GraphicsDrawDirection.Right,
    double NumericValue = 0,
    bool Blind = false,
    bool ReturnToOrigin = false,
    long X = 0,
    long Y = 0,
    bool IsRelativeMove = false,
    int FillColor = 0,
    int? BorderColor = null);
