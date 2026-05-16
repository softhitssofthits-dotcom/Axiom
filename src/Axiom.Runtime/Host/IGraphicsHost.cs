namespace Axiom.Runtime;

public enum GraphicsLineKind
{
    Normal,
    Box,
    FilledBox
}

public enum TextBackgroundMode
{
    Unchanged,
    Color,
    Transparent
}

public interface IGraphicsHost
{
    bool IsScreenActive => false;

    bool IsScreenOutputActive => IsScreenActive;

    int ScreenMode => 0;

    int ScreenWidth => 0;

    int ScreenHeight => 0;

    int CurrentForegroundColor => 15;

    int CurrentBackgroundColor => 0;

    bool IsTextBackgroundTransparent => false;

    long CurrentGraphicsX => 0;

    long CurrentGraphicsY => 0;

    bool ViewportEnabled => false;

    bool ViewportRelativeCoordinates => false;

    long ViewportLeft => 0;

    long ViewportTop => 0;

    long ViewportRight => ScreenWidth > 0 ? ScreenWidth - 1L : -1L;

    long ViewportBottom => ScreenHeight > 0 ? ScreenHeight - 1L : -1L;

    void SetGraphicsCurrentPosition(long x, long y)
    {
    }

    bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates) => false;

    void ResetViewport()
    {
    }

    void ActivateScreen(int mode)
    {
    }

    void DeactivateScreen()
    {
    }

    void ClearGraphics()
    {
    }

    void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
    {
    }

    void LocateText(int row, int column)
    {
    }

    void WriteGraphicsText(string text, bool newLine)
    {
    }

    string ReadGraphicsInput(InputRequest request) => string.Empty;

    void SetPixel(long x, long y, int color)
    {
    }

    int GetPixel(long x, long y) => 0;

    void Paint(GraphicsPaintRequest request)
    {
        var viewport = GraphicsViewportMath.GetEffectiveViewport(this);
        if (viewport.IsEmpty)
        {
            return;
        }

        if (viewport.UsesRelativeCoordinates)
        {
            GraphicsFloodFill.Paint(
                request,
                GetPixel,
                SetPixel,
                static (x, y, width, height) => x >= 0 && y >= 0 && x < width && y < height,
                checked((int)viewport.Width),
                checked((int)viewport.Height));
            return;
        }

        GraphicsFloodFill.Paint(
            request,
            GetPixel,
            SetPixel,
            (x, y, width, height) =>
                x >= 0 &&
                y >= 0 &&
                x < width &&
                y < height &&
                viewport.IsInsideAbsolute(x, y),
            ScreenWidth,
            ScreenHeight);
    }

    void Paint(long x, long y, int fillColor, int borderColor)
    {
        Paint(GraphicsPaintRequest.CreateSolid(x, y, fillColor, borderColor));
    }

    void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            return;
        }

        switch (kind)
        {
            case GraphicsLineKind.Box:
                DrawBoxCore(x1, y1, x2, y2, color, filled: false);
                break;

            case GraphicsLineKind.FilledBox:
                DrawBoxCore(x1, y1, x2, y2, color, filled: true);
                break;

            default:
                DrawLineCore(x1, y1, x2, y2, color);
                break;
        }

        SetGraphicsCurrentPosition(x2, y2);
    }

    void DrawCircle(
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
        if (!GraphicsCircleValidation.HasValidArguments(radius, startAngle, endAngle, aspect))
        {
            return;
        }

        if (!GraphicsInstructionArguments.TryCreateCircle(
                CurrentGraphicsX,
                CurrentGraphicsY,
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

        var centerX = circle.CenterX;
        var centerY = circle.CenterY;
        SetGraphicsCurrentPosition(centerX, centerY);

        var radiusX = circle.Aspect <= 1d ? circle.Radius : circle.Radius / circle.Aspect;
        var radiusY = circle.Aspect <= 1d ? circle.Radius * circle.Aspect : circle.Radius;
        if (radiusX <= 0 || radiusY <= 0)
        {
            SetPixel(centerX, centerY, color);
            return;
        }

        var arcStart = circle.StartAngle ?? 0d;
        var startPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart);

        if (circle.DrawStartRadius)
        {
            DrawLineCore(centerX, centerY, startPoint.X, startPoint.Y, color);
        }

        GraphicsEllipseRasterizer.Rasterize(
            centerX,
            centerY,
            radiusX,
            radiusY,
            circle.FullCircle,
            arcStart,
            circle.Sweep,
            GetRasterWidth(),
            GetRasterHeight(),
            (x, y) => SetPixel(x, y, color));

        if (circle.DrawEndRadius)
        {
            var endPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart + circle.Sweep);
            DrawLineCore(centerX, centerY, endPoint.X, endPoint.Y, color);
        }

        SetGraphicsCurrentPosition(centerX, centerY);
    }

    private void DrawLineCore(long x1, long y1, long x2, long y2, int color)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            SetPixel(x1, y1, color);
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

    private void DrawBoxCore(long x1, long y1, long x2, long y2, int color, bool filled)
    {
        var left = Math.Min(x1, x2);
        var right = Math.Max(x1, x2);
        var top = Math.Min(y1, y2);
        var bottom = Math.Max(y1, y2);

        if (filled)
        {
            for (var y = top; y <= bottom; y++)
            {
                DrawLineCore(left, y, right, y, color);
            }

            return;
        }

        DrawLineCore(left, top, right, top, color);
        DrawLineCore(right, top, right, bottom, color);
        DrawLineCore(right, bottom, left, bottom, color);
        DrawLineCore(left, bottom, left, top, color);
    }

    private int GetRasterWidth()
    {
        var viewport = GraphicsViewportMath.GetEffectiveViewport(this);
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Width))
            : ScreenWidth;
    }

    private int GetRasterHeight()
    {
        var viewport = GraphicsViewportMath.GetEffectiveViewport(this);
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Height))
            : ScreenHeight;
    }
}
