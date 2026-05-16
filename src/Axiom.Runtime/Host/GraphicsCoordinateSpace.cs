namespace Axiom.Runtime;

public readonly record struct GraphicsWindow(
    double Left,
    double Top,
    double Right,
    double Bottom,
    bool UsesScreenCoordinates)
{
    public double HorizontalSpan => Right - Left;

    public double VerticalSpan => Top - Bottom;

    public bool HasValidSpan =>
        double.IsFinite(Left) &&
        double.IsFinite(Top) &&
        double.IsFinite(Right) &&
        double.IsFinite(Bottom) &&
        HorizontalSpan != 0d &&
        VerticalSpan != 0d;
}

public static class GraphicsCoordinateSpaceMath
{
    public static bool TryCreateWindow(
        double x1,
        double y1,
        double x2,
        double y2,
        bool useScreenCoordinates,
        out GraphicsWindow window)
    {
        window = default;
        if (!GraphicsCoordinateValidation.AreSupportedWindowCoordinates(x1, y1, x2, y2))
        {
            return false;
        }

        var left = Math.Min(x1, x2);
        var right = Math.Max(x1, x2);
        if (left == right)
        {
            return false;
        }

        var maxY = Math.Max(y1, y2);
        var minY = Math.Min(y1, y2);
        window = useScreenCoordinates
            ? new GraphicsWindow(left, minY, right, maxY, UsesScreenCoordinates: true)
            : new GraphicsWindow(left, maxY, right, minY, UsesScreenCoordinates: false);
        return window.HasValidSpan;
    }

    public static bool TryTranslateLogicalToViewportUnchecked(
        IGraphicsHost host,
        long logicalX,
        long logicalY,
        out long viewportX,
        out long viewportY)
    {
        ArgumentNullException.ThrowIfNull(host);

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        if (viewport.IsEmpty)
        {
            viewportX = 0;
            viewportY = 0;
            return false;
        }

        if (!TryGetWindow(host, out var window))
        {
            viewportX = logicalX;
            viewportY = logicalY;
            return true;
        }

        return TryTranslateLogicalToViewport(window, viewport, logicalX, logicalY, out viewportX, out viewportY);
    }

    public static bool TryTranslateLogicalToAbsoluteUnchecked(
        IGraphicsHost host,
        long logicalX,
        long logicalY,
        out long absoluteX,
        out long absoluteY)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (!TryTranslateLogicalToViewportUnchecked(host, logicalX, logicalY, out var viewportX, out var viewportY))
        {
            absoluteX = 0;
            absoluteY = 0;
            return false;
        }

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        if (viewport.UsesRelativeCoordinates)
        {
            absoluteX = GraphicsLineMath.SaturatingAdd(viewport.Left, viewportX);
            absoluteY = GraphicsLineMath.SaturatingAdd(viewport.Top, viewportY);
        }
        else
        {
            absoluteX = viewportX;
            absoluteY = viewportY;
        }

        return true;
    }

    public static bool TryTranslateLogicalToAbsolute(
        IGraphicsHost host,
        long logicalX,
        long logicalY,
        out long absoluteX,
        out long absoluteY)
    {
        if (!TryTranslateLogicalToAbsoluteUnchecked(host, logicalX, logicalY, out absoluteX, out absoluteY))
        {
            return false;
        }

        return IsInsideClip(host, absoluteX, absoluteY);
    }

    public static bool TryTranslateAbsoluteToLogical(
        IGraphicsHost host,
        long absoluteX,
        long absoluteY,
        out long logicalX,
        out long logicalY)
    {
        ArgumentNullException.ThrowIfNull(host);

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        if (viewport.IsEmpty)
        {
            logicalX = 0;
            logicalY = 0;
            return false;
        }

        var viewportX = viewport.UsesRelativeCoordinates ? absoluteX - viewport.Left : absoluteX;
        var viewportY = viewport.UsesRelativeCoordinates ? absoluteY - viewport.Top : absoluteY;
        if (!TryGetWindow(host, out var window))
        {
            logicalX = viewportX;
            logicalY = viewportY;
            return true;
        }

        return TryTranslateViewportToLogical(window, viewport, viewportX, viewportY, out logicalX, out logicalY);
    }

    public static bool TryResolveCurrentAbsolutePosition(IGraphicsHost host, out long absoluteX, out long absoluteY)
    {
        ArgumentNullException.ThrowIfNull(host);
        return TryTranslateLogicalToAbsoluteUnchecked(host, host.CurrentGraphicsX, host.CurrentGraphicsY, out absoluteX, out absoluteY);
    }

    public static bool TryResolveCurrentViewportPosition(IGraphicsHost host, out long viewportX, out long viewportY)
    {
        ArgumentNullException.ThrowIfNull(host);
        return TryTranslateLogicalToViewportUnchecked(host, host.CurrentGraphicsX, host.CurrentGraphicsY, out viewportX, out viewportY);
    }

    public static bool TryGetWindowScale(IGraphicsHost host, out double scaleX, out double scaleY)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (!TryGetWindow(host, out var window))
        {
            scaleX = 1d;
            scaleY = 1d;
            return true;
        }

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        if (viewport.Width < 2 || viewport.Height < 2)
        {
            scaleX = 0d;
            scaleY = 0d;
            return false;
        }

        scaleX = (viewport.Width - 1d) / Math.Abs(window.HorizontalSpan);
        scaleY = (viewport.Height - 1d) / Math.Abs(window.VerticalSpan);
        return double.IsFinite(scaleX) && scaleX > 0d && double.IsFinite(scaleY) && scaleY > 0d;
    }

    public static bool IsInsideClip(IGraphicsHost host, long absoluteX, long absoluteY)
    {
        ArgumentNullException.ThrowIfNull(host);

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        return absoluteX >= 0 &&
               absoluteY >= 0 &&
               absoluteX < host.ScreenWidth &&
               absoluteY < host.ScreenHeight &&
               viewport.IsInsideAbsolute(absoluteX, absoluteY);
    }

    private static bool TryGetWindow(IGraphicsHost host, out GraphicsWindow window)
    {
        if (host.WindowEnabled)
        {
            window = new GraphicsWindow(host.WindowLeft, host.WindowTop, host.WindowRight, host.WindowBottom, host.WindowUsesScreenCoordinates);
            return window.HasValidSpan;
        }

        window = default;
        return false;
    }

    private static bool TryTranslateLogicalToViewport(
        GraphicsWindow window,
        GraphicsViewport viewport,
        long logicalX,
        long logicalY,
        out long viewportX,
        out long viewportY)
    {
        if (!TryGetViewportRange(viewport, out var viewportLeft, out var viewportTop, out var viewportRight, out var viewportBottom))
        {
            viewportX = 0;
            viewportY = 0;
            return false;
        }

        if (!TryMapRange(logicalX, window.Left, window.Right, viewportLeft, viewportRight, out viewportX))
        {
            viewportY = 0;
            return false;
        }

        return TryMapRange(logicalY, window.Top, window.Bottom, viewportTop, viewportBottom, out viewportY);
    }

    private static bool TryTranslateViewportToLogical(
        GraphicsWindow window,
        GraphicsViewport viewport,
        long viewportX,
        long viewportY,
        out long logicalX,
        out long logicalY)
    {
        if (!TryGetViewportRange(viewport, out var viewportLeft, out var viewportTop, out var viewportRight, out var viewportBottom))
        {
            logicalX = 0;
            logicalY = 0;
            return false;
        }

        if (!TryMapRange(viewportX, viewportLeft, viewportRight, window.Left, window.Right, out logicalX))
        {
            logicalY = 0;
            return false;
        }

        return TryMapRange(viewportY, viewportTop, viewportBottom, window.Top, window.Bottom, out logicalY);
    }

    private static bool TryGetViewportRange(
        GraphicsViewport viewport,
        out double left,
        out double top,
        out double right,
        out double bottom)
    {
        if (viewport.IsEmpty)
        {
            left = 0d;
            top = 0d;
            right = 0d;
            bottom = 0d;
            return false;
        }

        if (viewport.UsesRelativeCoordinates)
        {
            if (viewport.Width < 2 || viewport.Height < 2)
            {
                left = 0d;
                top = 0d;
                right = 0d;
                bottom = 0d;
                return false;
            }

            left = 0d;
            top = 0d;
            right = viewport.Width - 1d;
            bottom = viewport.Height - 1d;
            return true;
        }

        left = viewport.Left;
        top = viewport.Top;
        right = viewport.Right;
        bottom = viewport.Bottom;
        return true;
    }

    private static bool TryMapRange(
        double input,
        double sourceStart,
        double sourceEnd,
        double targetStart,
        double targetEnd,
        out long mapped)
    {
        mapped = 0;
        var sourceSpan = sourceEnd - sourceStart;
        if (!double.IsFinite(input) ||
            !double.IsFinite(sourceStart) ||
            !double.IsFinite(sourceEnd) ||
            !double.IsFinite(targetStart) ||
            !double.IsFinite(targetEnd) ||
            sourceSpan == 0d)
        {
            return false;
        }

        var normalized = (input - sourceStart) / sourceSpan;
        var value = targetStart + ((targetEnd - targetStart) * normalized);
        return TryRoundToLong(value, out mapped);
    }

    private static bool TryRoundToLong(double value, out long rounded)
    {
        rounded = 0;
        if (!double.IsFinite(value))
        {
            return false;
        }

        try
        {
            rounded = checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
