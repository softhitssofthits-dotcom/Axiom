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
    long X,
    long Y,
    double Radius,
    int Color,
    bool RelativeCenter,
    double? StartAngle,
    double? EndAngle,
    double Aspect,
    bool DrawStartRadius,
    bool DrawEndRadius);

internal static class GraphicsInstructionArguments
{
    public static GraphicsLineArguments CreateLine(
        long currentGraphicsX,
        long currentGraphicsY,
        long x2,
        long y2,
        int color,
        GraphicsLineInstructionFlags flags,
        long? explicitX1 = null,
        long? explicitY1 = null)
    {
        var x1 = flags.HasExplicitStartPoint
            ? explicitX1 ?? throw new ArgumentNullException(nameof(explicitX1))
            : currentGraphicsX;
        var y1 = flags.HasExplicitStartPoint
            ? explicitY1 ?? throw new ArgumentNullException(nameof(explicitY1))
            : currentGraphicsY;

        if (flags.HasExplicitStartPoint && flags.StartRelative)
        {
            x1 += currentGraphicsX;
            y1 += currentGraphicsY;
        }

        if (flags.EndRelative)
        {
            x2 += x1;
            y2 += y1;
        }

        return new GraphicsLineArguments(x1, y1, x2, y2, color, flags.Kind);
    }

    public static GraphicsCircleArguments CreateCircle(
        long x,
        long y,
        double radius,
        int color,
        bool relativeCenter,
        double? startAngle,
        double? endAngle,
        double aspect)
    {
        return new GraphicsCircleArguments(
            x,
            y,
            radius,
            color,
            relativeCenter,
            NormalizeCircleAngle(startAngle),
            NormalizeCircleAngle(endAngle),
            aspect,
            startAngle is < 0,
            endAngle is < 0);
    }

    private static double? NormalizeCircleAngle(double? angle) =>
        angle switch
        {
            null => null,
            < 0 => -angle,
            _ => angle
        };
}
