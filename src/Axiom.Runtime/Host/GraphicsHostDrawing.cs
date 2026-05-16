namespace Axiom.Runtime;

public static class GraphicsHostDrawing
{
    public static void Paint(IGraphicsHost host, GraphicsPaintRequest request)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(request);

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        if (viewport.IsEmpty || host.ScreenWidth <= 0 || host.ScreenHeight <= 0)
        {
            return;
        }

        if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(host, request.X, request.Y, out var absoluteX, out var absoluteY))
        {
            return;
        }

        var absoluteRequest = request.UsesPattern
            ? GraphicsPaintRequest.CreatePattern(
                absoluteX,
                absoluteY,
                request.FillPatternRows!,
                request.BorderColor,
                request.PatternForegroundColor,
                request.PatternBackgroundColor,
                request.BackgroundPatternRows)
            : GraphicsPaintRequest.CreateSolid(
                absoluteX,
                absoluteY,
                request.SolidFillColor ?? request.PatternForegroundColor,
                request.BorderColor);

        GraphicsFloodFill.Paint(
            absoluteRequest,
            host.GetAbsolutePixel,
            host.SetAbsolutePixel,
            (x, y, width, height) =>
                x >= 0 &&
                y >= 0 &&
                x < width &&
                y < height &&
                viewport.IsInsideAbsolute(x, y),
            host.ScreenWidth,
            host.ScreenHeight);
    }

    public static void DrawLine(IGraphicsHost host, long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            return;
        }

        if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, x1, y1, out var absoluteX1, out var absoluteY1) ||
            !GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, x2, y2, out var absoluteX2, out var absoluteY2))
        {
            return;
        }

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        switch (kind)
        {
            case GraphicsLineKind.Box:
                DrawAbsoluteBox(host, viewport, absoluteX1, absoluteY1, absoluteX2, absoluteY2, color, filled: false);
                break;

            case GraphicsLineKind.FilledBox:
                DrawAbsoluteBox(host, viewport, absoluteX1, absoluteY1, absoluteX2, absoluteY2, color, filled: true);
                break;

            default:
                DrawAbsoluteLine(host, viewport, absoluteX1, absoluteY1, absoluteX2, absoluteY2, color);
                break;
        }

        host.SetGraphicsCurrentPosition(x2, y2);
    }

    public static void DrawCircle(
        IGraphicsHost host,
        long x,
        long y,
        double radius,
        int color,
        bool relativeCenter,
        double? startAngle,
        double? endAngle,
        double aspect,
        bool drawStartRadius,
        bool drawEndRadius)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (!GraphicsCircleValidation.HasValidArguments(radius, startAngle, endAngle, aspect))
        {
            return;
        }

        if (!GraphicsInstructionArguments.TryCreateCircle(
                host.CurrentGraphicsX,
                host.CurrentGraphicsY,
                x,
                y,
                radius,
                color,
                relativeCenter,
                startAngle,
                endAngle,
                aspect,
                out var circle))
        {
            return;
        }

        host.SetGraphicsCurrentPosition(circle.CenterX, circle.CenterY);
        if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, circle.CenterX, circle.CenterY, out var centerX, out var centerY))
        {
            return;
        }

        var viewport = GraphicsViewportMath.GetEffectiveViewport(host);
        var radiusX = circle.Aspect <= 1d ? circle.Radius : circle.Radius / circle.Aspect;
        var radiusY = circle.Aspect <= 1d ? circle.Radius * circle.Aspect : circle.Radius;
        if (host.WindowEnabled)
        {
            if (!GraphicsCoordinateSpaceMath.TryGetWindowScale(host, out var scaleX, out var scaleY))
            {
                return;
            }

            radiusX *= scaleX;
            radiusY *= scaleY;
        }

        if (radiusX <= 0d || radiusY <= 0d)
        {
            PlotAbsolutePixel(host, viewport, centerX, centerY, color);
            return;
        }

        var arcStart = circle.StartAngle ?? 0d;
        var startPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart);

        if (circle.DrawStartRadius)
        {
            DrawAbsoluteLine(host, viewport, centerX, centerY, startPoint.X, startPoint.Y, color);
        }

        GraphicsEllipseRasterizer.Rasterize(
            centerX,
            centerY,
            radiusX,
            radiusY,
            circle.FullCircle,
            arcStart,
            circle.Sweep,
            host.ScreenWidth,
            host.ScreenHeight,
            (plotX, plotY) => PlotAbsolutePixel(host, viewport, plotX, plotY, color));

        if (circle.DrawEndRadius)
        {
            var endPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart + circle.Sweep);
            DrawAbsoluteLine(host, viewport, centerX, centerY, endPoint.X, endPoint.Y, color);
        }
    }

    private static void DrawAbsoluteLine(IGraphicsHost host, GraphicsViewport viewport, long x1, long y1, long x2, long y2, int color)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            PlotAbsolutePixel(host, viewport, x1, y1, color);
            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var doubledError = GraphicsLineMath.SaturatingDouble(error);
            if (doubledError >= dy)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dy);
                x1 += sx;
            }

            if (doubledError <= dx)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dx);
                y1 += sy;
            }
        }
    }

    private static void DrawAbsoluteBox(IGraphicsHost host, GraphicsViewport viewport, long x1, long y1, long x2, long y2, int color, bool filled)
    {
        var left = Math.Min(x1, x2);
        var right = Math.Max(x1, x2);
        var top = Math.Min(y1, y2);
        var bottom = Math.Max(y1, y2);

        if (filled)
        {
            for (var y = top; y <= bottom; y++)
            {
                DrawAbsoluteLine(host, viewport, left, y, right, y, color);
            }

            return;
        }

        DrawAbsoluteLine(host, viewport, left, top, right, top, color);
        DrawAbsoluteLine(host, viewport, right, top, right, bottom, color);
        DrawAbsoluteLine(host, viewport, right, bottom, left, bottom, color);
        DrawAbsoluteLine(host, viewport, left, bottom, left, top, color);
    }

    private static void PlotAbsolutePixel(IGraphicsHost host, GraphicsViewport viewport, long x, long y, int color)
    {
        if (x < 0 || y < 0 || x >= host.ScreenWidth || y >= host.ScreenHeight || !viewport.IsInsideAbsolute(x, y))
        {
            return;
        }

        host.SetAbsolutePixel(x, y, color);
    }
}
