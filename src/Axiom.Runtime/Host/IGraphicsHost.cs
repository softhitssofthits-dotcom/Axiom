namespace Axiom.Runtime;

public enum GraphicsLineKind
{
    Normal,
    Box,
    FilledBox
}

public interface IGraphicsHost
{
    bool IsScreenActive => false;

    bool IsScreenOutputActive => IsScreenActive;

    int ScreenMode => 0;

    int ScreenWidth => 0;

    int ScreenHeight => 0;

    void ActivateScreen(int mode)
    {
    }

    void DeactivateScreen()
    {
    }

    void ClearGraphics()
    {
    }

    void SetTextColor(int foreground, int? background)
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

    void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
    }

    void DrawCircle(long x, long y, long radius, int color)
    {
    }
}
