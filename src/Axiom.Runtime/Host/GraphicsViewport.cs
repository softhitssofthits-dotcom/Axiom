namespace Axiom.Runtime;

public readonly record struct GraphicsViewport(
    long Left,
    long Top,
    long Right,
    long Bottom,
    bool UsesRelativeCoordinates)
{
    public long Width => Right >= Left ? Right - Left + 1 : 0;

    public long Height => Bottom >= Top ? Bottom - Top + 1 : 0;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool IsInsideAbsolute(long x, long y) =>
        !IsEmpty &&
        x >= Left &&
        y >= Top &&
        x <= Right &&
        y <= Bottom;

    public bool IsInsideLogical(long x, long y)
    {
        if (IsEmpty)
        {
            return false;
        }

        if (!UsesRelativeCoordinates)
        {
            return IsInsideAbsolute(x, y);
        }

        return x >= 0 &&
               y >= 0 &&
               x < Width &&
               y < Height;
    }

    public bool TryTranslateLogicalToAbsolute(long x, long y, out long absoluteX, out long absoluteY)
    {
        if (UsesRelativeCoordinates)
        {
            absoluteX = GraphicsLineMath.SaturatingAdd(Left, x);
            absoluteY = GraphicsLineMath.SaturatingAdd(Top, y);
        }
        else
        {
            absoluteX = x;
            absoluteY = y;
        }

        return IsInsideAbsolute(absoluteX, absoluteY);
    }
}

public static class GraphicsViewportMath
{
    public static GraphicsViewport GetEffectiveViewport(IGraphicsHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (host.ViewportEnabled)
        {
            return new GraphicsViewport(
                host.ViewportLeft,
                host.ViewportTop,
                host.ViewportRight,
                host.ViewportBottom,
                host.ViewportRelativeCoordinates);
        }

        var right = host.ScreenWidth > 0 ? host.ScreenWidth - 1L : -1L;
        var bottom = host.ScreenHeight > 0 ? host.ScreenHeight - 1L : -1L;
        return new GraphicsViewport(0, 0, right, bottom, UsesRelativeCoordinates: false);
    }
}
