using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

internal sealed class IdeInputProvider : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IGraphicsHost
{
    private readonly IProgramInputPromptService _promptService;
    private int _requestCount;

    public IdeInputProvider(IProgramInputPromptService promptService)
    {
        ArgumentNullException.ThrowIfNull(promptService);
        _promptService = promptService;
    }

    public string ReadInput(InputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _requestCount++;
        return _promptService.RequestInput(new ProgramInputRequest(_requestCount, request.PromptText, request.ValueCount));
    }

    public string ReadKey() =>
        _promptService is IKeyboardInputProvider keyboardInputProvider
            ? keyboardInputProvider.ReadKey()
            : string.Empty;

    public bool IsKeyDown(string keyText) =>
        _promptService is IKeyboardInputProvider keyboardInputProvider &&
        keyboardInputProvider.IsKeyDown(keyText);

    public long GetMouseX() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseX()
            : -1;

    public long GetMouseY() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseY()
            : -1;

    public bool IsMouseButtonDown(int buttonNumber) =>
        _promptService is IMouseInputProvider mouseInputProvider &&
        mouseInputProvider.IsMouseButtonDown(buttonNumber);

    public int ReadMouseButtonPress() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseButtonPress()
            : 0;

    public MouseInputEvent ReadMouseEvent() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseEvent()
            : default;

    public bool IsScreenActive =>
        _promptService is IGraphicsHost graphicsHost && graphicsHost.IsScreenActive;

    public bool IsScreenOutputActive =>
        _promptService is IGraphicsHost graphicsHost && graphicsHost.IsScreenOutputActive;

    public int ScreenMode =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.ScreenMode : 0;

    public int ScreenWidth =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.ScreenWidth : 0;

    public int ScreenHeight =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.ScreenHeight : 0;

    public int CurrentForegroundColor =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.CurrentForegroundColor : 15;

    public int CurrentBackgroundColor =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.CurrentBackgroundColor : 0;

    public bool IsTextBackgroundTransparent =>
        _promptService is IGraphicsHost graphicsHost && graphicsHost.IsTextBackgroundTransparent;

    public long CurrentGraphicsX =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsX : 0;

    public long CurrentGraphicsY =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsY : 0;

    public void ActivateScreen(int mode)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateScreen(mode);
        }
    }

    public void DeactivateScreen()
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.DeactivateScreen();
        }
    }

    public void ClearGraphics()
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.ClearGraphics();
        }
    }

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetTextColor(foreground, backgroundMode, background);
        }
    }

    public void LocateText(int row, int column)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.LocateText(row, column);
        }
    }

    public void WriteGraphicsText(string text, bool newLine)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.WriteGraphicsText(text, newLine);
        }
    }

    public string ReadGraphicsInput(InputRequest request) =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.ReadGraphicsInput(request) : string.Empty;

    public void SetPixel(long x, long y, int color)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetPixel(x, y, color);
        }
    }

    public int GetPixel(long x, long y) =>
        _promptService is IGraphicsHost graphicsHost ? graphicsHost.GetPixel(x, y) : 0;

    public void Paint(long x, long y, int fillColor, int borderColor)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.Paint(x, y, fillColor, borderColor);
        }
    }

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawLine(x1, y1, x2, y2, color, kind);
        }
    }

    public void DrawCircle(
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
        if (_promptService is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCircle(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
        }
    }
}
