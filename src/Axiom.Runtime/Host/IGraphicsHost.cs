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

    void Paint(long x, long y, int fillColor, int borderColor)
    {
        GraphicsFloodFill.Paint(
            x,
            y,
            fillColor,
            borderColor,
            GetPixel,
            SetPixel);
    }

    void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
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
    }
}
