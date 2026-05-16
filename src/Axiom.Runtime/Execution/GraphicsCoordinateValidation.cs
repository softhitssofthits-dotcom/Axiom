namespace Axiom.Runtime;

public static class GraphicsCoordinateValidation
{
    public const long MinCoordinate = -1_000_000_000L;
    public const long MaxCoordinate = 1_000_000_000L;

    public static bool IsSupportedCoordinate(long value) =>
        value >= MinCoordinate &&
        value <= MaxCoordinate;

    public static bool IsSupportedWindowCoordinate(double value) =>
        double.IsFinite(value) &&
        value >= MinCoordinate &&
        value <= MaxCoordinate;

    public static bool AreSupportedLineCoordinates(long x1, long y1, long x2, long y2) =>
        IsSupportedCoordinate(x1) &&
        IsSupportedCoordinate(y1) &&
        IsSupportedCoordinate(x2) &&
        IsSupportedCoordinate(y2);

    public static bool AreSupportedWindowCoordinates(double x1, double y1, double x2, double y2) =>
        IsSupportedWindowCoordinate(x1) &&
        IsSupportedWindowCoordinate(y1) &&
        IsSupportedWindowCoordinate(x2) &&
        IsSupportedWindowCoordinate(y2);

    public static bool IsSupportedCircleCenter(long x, long y) =>
        IsSupportedCoordinate(x) &&
        IsSupportedCoordinate(y);

    public static bool TryResolvePoint(long originX, long originY, long deltaX, long deltaY, out long resolvedX, out long resolvedY)
    {
        resolvedX = GraphicsLineMath.SaturatingAdd(originX, deltaX);
        resolvedY = GraphicsLineMath.SaturatingAdd(originY, deltaY);
        return IsSupportedCircleCenter(resolvedX, resolvedY);
    }
}
