using Axiom.Semantics;

namespace Axiom.Runtime;

internal readonly record struct GraphicsLineInstructionFlags(
    BoundLineBoxKind BoxKind,
    bool HasExplicitStartPoint,
    bool StartRelative,
    bool EndRelative)
{
    public GraphicsLineKind Kind => BoxKind switch
    {
        BoundLineBoxKind.Border => GraphicsLineKind.Box,
        BoundLineBoxKind.Filled => GraphicsLineKind.FilledBox,
        _ => GraphicsLineKind.Normal
    };

    public static GraphicsLineInstructionFlags FromEncoded(int flags) => new(
        (BoundLineBoxKind)(flags & 3),
        (flags & 4) != 0,
        (flags & 8) != 0,
        (flags & 16) != 0);
}

internal readonly record struct GraphicsCircleInstructionFlags(
    bool RelativeCenter,
    bool HasStart,
    bool HasEnd,
    bool HasAspect,
    bool HasColor)
{
    public static GraphicsCircleInstructionFlags FromEncoded(int flags) => new(
        (flags & 1) != 0,
        (flags & 2) != 0,
        (flags & 4) != 0,
        (flags & 8) != 0,
        (flags & 16) != 0);
}

internal readonly record struct GraphicsLineArguments(
    long X1,
    long Y1,
    long X2,
    long Y2,
    int Color,
    GraphicsLineKind Kind);

internal readonly record struct GraphicsCircleArguments(
    long CenterX,
    long CenterY,
    double Radius,
    int Color,
    double? StartAngle,
    double? EndAngle,
    double Aspect,
    bool DrawStartRadius,
    bool DrawEndRadius,
    bool FullCircle,
    double Sweep);

internal static class GraphicsInstructionArguments
{
    public static bool TryCreateLine(
        long currentGraphicsX,
        long currentGraphicsY,
        long x2,
        long y2,
        int color,
        GraphicsLineInstructionFlags flags,
        out GraphicsLineArguments arguments,
        long? explicitX1 = null,
        long? explicitY1 = null)
    {
        arguments = default;

        var x1 = flags.HasExplicitStartPoint
            ? explicitX1 ?? throw new ArgumentNullException(nameof(explicitX1))
            : currentGraphicsX;
        var y1 = flags.HasExplicitStartPoint
            ? explicitY1 ?? throw new ArgumentNullException(nameof(explicitY1))
            : currentGraphicsY;

        if (flags.HasExplicitStartPoint && flags.StartRelative)
        {
            if (!GraphicsCoordinateValidation.TryResolvePoint(currentGraphicsX, currentGraphicsY, x1, y1, out x1, out y1))
            {
                return false;
            }
        }

        if (flags.EndRelative)
        {
            if (!GraphicsCoordinateValidation.TryResolvePoint(x1, y1, x2, y2, out x2, out y2))
            {
                return false;
            }
        }

        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            return false;
        }

        arguments = new GraphicsLineArguments(x1, y1, x2, y2, color, flags.Kind);
        return true;
    }

    public static bool TryCreateCircle(
        long currentGraphicsX,
        long currentGraphicsY,
        long x,
        long y,
        double radius,
        int color,
        bool relativeCenter,
        double? startAngle,
        double? endAngle,
        double aspect,
        out GraphicsCircleArguments arguments)
    {
        arguments = default;

        var centerX = x;
        var centerY = y;
        if (relativeCenter && !GraphicsCoordinateValidation.TryResolvePoint(currentGraphicsX, currentGraphicsY, x, y, out centerX, out centerY))
        {
            return false;
        }

        if (!relativeCenter && !GraphicsCoordinateValidation.IsSupportedCircleCenter(centerX, centerY))
        {
            return false;
        }

        var normalizedStart = NormalizeCircleAngle(startAngle);
        var normalizedEnd = NormalizeCircleAngle(endAngle);
        var fullCircle = !normalizedStart.HasValue && !normalizedEnd.HasValue;
        if (!fullCircle && normalizedStart.HasValue && normalizedEnd.HasValue)
        {
            fullCircle = Math.Abs(normalizedStart.Value - normalizedEnd.Value) <= 1e-9;
        }

        var arcStart = normalizedStart ?? 0d;
        var arcEnd = normalizedEnd ?? Math.Tau;
        var sweep = fullCircle ? Math.Tau : arcEnd - arcStart;
        if (!fullCircle && sweep <= 0d)
        {
            sweep += Math.Tau;
        }

        arguments = new GraphicsCircleArguments(
            centerX,
            centerY,
            radius,
            color,
            normalizedStart,
            normalizedEnd,
            aspect,
            startAngle is < 0,
            endAngle is < 0,
            fullCircle,
            sweep);
        return true;
    }

    private static double? NormalizeCircleAngle(double? angle) =>
        angle switch
        {
            null => null,
            < 0 => GraphicsEllipseRasterizer.NormalizeAngle(-angle.Value),
            _ => GraphicsEllipseRasterizer.NormalizeAngle(angle.Value)
        };
}
