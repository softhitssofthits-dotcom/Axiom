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
                out var circle,
                drawStartRadius || startAngle is < 0,
                drawEndRadius || endAngle is < 0))
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
        var left = Math.Max(0, viewport.Left);
        var right = Math.Min(host.ScreenWidth - 1L, viewport.Right);
        var top = Math.Max(0, viewport.Top);
        var bottom = Math.Min(host.ScreenHeight - 1L, viewport.Bottom);
        if (left > right || top > bottom || Math.Max(x1, x2) < left || Math.Min(x1, x2) > right ||
            Math.Max(y1, y2) < top || Math.Min(y1, y2) > bottom)
            return;

        // Вычисляем точки исходной линии, а не строим новую линию между
        // отсечёнными концами: иначе меняется фаза округления Брезенхема.
        // UInt128 сохраняет точность и после преобразования WINDOW в LONG.
        var dx = (UInt128)Int128.Abs((Int128)x2 - x1);
        var dy = (UInt128)Int128.Abs((Int128)y2 - y1);
        var sx = x1 < x2 ? 1 : -1;
        var sy = y1 < y2 ? 1 : -1;
        if (dx == 0 && dy == 0)
        {
            PlotAbsolutePixel(host, viewport, x1, y1, color);
            return;
        }
        if (dx >= dy)
        {
            var first = Math.Max(left, Math.Min(x1, x2));
            var last = Math.Min(right, Math.Max(x1, x2));
            for (var x = first; x <= last; x++)
            {
                var step = (UInt128)Int128.Abs((Int128)x - x1);
                var y = (long)((Int128)y1 + sy * (Int128)((step * dy + dx / 2) / dx));
                PlotAbsolutePixel(host, viewport, x, y, color);
            }
        }
        else
        {
            var first = Math.Max(top, Math.Min(y1, y2));
            var last = Math.Min(bottom, Math.Max(y1, y2));
            for (var y = first; y <= last; y++)
            {
                var step = (UInt128)Int128.Abs((Int128)y - y1);
                var x = (long)((Int128)x1 + sx * (Int128)((step * dx + dy / 2) / dy));
                PlotAbsolutePixel(host, viewport, x, y, color);
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
            left = Math.Max(left, Math.Max(0, viewport.Left));
            right = Math.Min(right, Math.Min(host.ScreenWidth - 1L, viewport.Right));
            top = Math.Max(top, Math.Max(0, viewport.Top));
            bottom = Math.Min(bottom, Math.Min(host.ScreenHeight - 1L, viewport.Bottom));
            if (left > right || top > bottom) return;
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
