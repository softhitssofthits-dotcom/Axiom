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

    bool IsCanvasActive => false;

    bool IsScreenOutputActive => IsScreenActive;

    int ScreenMode => 0;

    int ActiveScreenPage => 0;

    int VisualScreenPage => 0;

    int ScreenWidth => 0;

    int ScreenHeight => 0;

    int CurrentForegroundColor => 15;

    int CurrentBackgroundColor => 0;

    bool IsTextBackgroundTransparent => false;

    long CurrentGraphicsX => 0;

    long CurrentGraphicsY => 0;

    long CanvasCameraX => 0;

    long CanvasCameraY => 0;

    long CurrentViewportX =>
        GraphicsCoordinateSpaceMath.TryResolveCurrentViewportPosition(this, out var viewportX, out _)
            ? viewportX
            : CurrentGraphicsX;

    long CurrentViewportY =>
        GraphicsCoordinateSpaceMath.TryResolveCurrentViewportPosition(this, out _, out var viewportY)
            ? viewportY
            : CurrentGraphicsY;

    bool ViewportEnabled => false;

    bool ViewportRelativeCoordinates => false;

    long ViewportLeft => 0;

    long ViewportTop => 0;

    long ViewportRight => ScreenWidth > 0 ? ScreenWidth - 1L : -1L;

    long ViewportBottom => ScreenHeight > 0 ? ScreenHeight - 1L : -1L;

    bool WindowEnabled => false;

    bool WindowUsesScreenCoordinates => false;

    double WindowLeft => 0d;

    double WindowTop => 0d;

    double WindowRight => 0d;

    double WindowBottom => 0d;

    void SetGraphicsCurrentPosition(long x, long y)
    {
    }

    bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates) => false;

    void ResetViewport()
    {
    }

    bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates) => false;

    void ResetWindow()
    {
    }

    void ActivateScreen(int mode)
    {
    }

    void ActivateCanvas(long width, long height)
    {
    }

    void SetGraphicsSmoothing(bool enabled)
    {
    }

    void BeginFrame()
    {
    }

    void PresentFrame()
    {
    }

    void SetCanvasCamera(long x, long y)
    {
    }

    void ResetCanvasCamera()
    {
    }

    void ClearFrame(int color)
    {
    }

    void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
    {
    }

    void DrawCanvasCircle(long x, long y, long radius, int color)
    {
    }

    void FillCanvasCircle(long x, long y, long radius, int color)
    {
    }

    void DrawCanvasText(string text, long x, long y, int color)
    {
    }

    void DrawRect(long x, long y, long width, long height, int color)
    {
    }

    void FillRect(long x, long y, long width, long height, int color)
    {
    }

    void ActivateScreen(int mode, int activePage, int visualPage)
    {
        ActivateScreen(mode);
    }

    bool CopyScreenPage(int sourcePage, int destinationPage) => false;

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

    void ResetPalette()
    {
    }

    void SetPaletteColor(int index, int color)
    {
    }

    RuntimeImageValue? LoadImage(string path) => null;

    RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight)
    {
        var image = LoadImage(path);
        return image is null ? null : new RuntimeSpriteValue(image, frameWidth, frameHeight);
    }

    RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0)
    {
        var image = LoadImage(path);
        if (image is null)
        {
            return null;
        }

        try
        {
            return new RuntimeTilesetValue(image, tileWidth, tileHeight, margin, spacing);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    // null означает недоступный ресурс; InvalidDataException передаёт причину отказа загрузчика.
    RuntimeTilemapValue? LoadTilemap(string path) => null;

    void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
    {
    }

    void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
    {
    }

    void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
    }

    bool SetTextViewport(int topRow, int bottomRow) => false;

    void ResetTextViewport()
    {
    }

    void WriteGraphicsText(string text, bool newLine)
    {
    }

    string ReadGraphicsInput(InputRequest request) => string.Empty;

    void SetAbsolutePixel(long x, long y, int color)
    {
    }

    int GetAbsolutePixel(long x, long y) => 0;

    // Необязательное чтение области одним запросом. null сохраняет прежний путь хоста.
    int[]? CaptureAbsolutePixels(long x, long y, int width, int height) => null;

    void SetPixel(long x, long y, int color)
    {
    }

    int GetPixel(long x, long y) => 0;

    void Paint(GraphicsPaintRequest request)
    {
        GraphicsHostDrawing.Paint(this, request);
    }

    void Paint(long x, long y, int fillColor, int borderColor)
    {
        Paint(GraphicsPaintRequest.CreateSolid(x, y, fillColor, borderColor));
    }

    void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
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
        GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
    }
}
