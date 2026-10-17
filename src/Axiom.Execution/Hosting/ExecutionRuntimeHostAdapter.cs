using Axiom.Runtime;

namespace Axiom.Execution;

internal sealed class ExecutionRuntimeHostAdapter : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IOutputWriter _outputWriter;
    private readonly IRuntimeClock _runtimeClock;
    private readonly IRuntimeRandom _runtimeRandom;
    private readonly IRuntimeCancellationProvider? _inputCancellationProvider;
    private readonly IRuntimeCancellationProvider? _outputCancellationProvider;
    private readonly IRuntimeCancellationProvider? _externalCancellationProvider;
    private string _currentDirectory;

    public ExecutionRuntimeHostAdapter(IInputProvider inputProvider, IOutputWriter outputWriter, IRuntimeClock runtimeClock, IRuntimeRandom runtimeRandom, IRuntimeCancellationProvider? externalCancellationProvider = null, string? resourceBaseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(inputProvider);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(runtimeClock);
        ArgumentNullException.ThrowIfNull(runtimeRandom);

        _inputProvider = inputProvider;
        _outputWriter = outputWriter;
        _runtimeClock = runtimeClock;
        _runtimeRandom = runtimeRandom;
        _inputCancellationProvider = inputProvider as IRuntimeCancellationProvider;
        _outputCancellationProvider = ReferenceEquals(inputProvider, outputWriter)
            ? null
            : outputWriter as IRuntimeCancellationProvider;
        _externalCancellationProvider = externalCancellationProvider;
        ResourceBaseDirectory = resourceBaseDirectory ?? (inputProvider as IRuntimeHost)?.ResourceBaseDirectory;
        _currentDirectory = Path.GetFullPath(ResourceBaseDirectory ?? Environment.CurrentDirectory);
    }

    public string? ResourceBaseDirectory { get; }

    public string CurrentDirectory => _currentDirectory;

    public void ChangeCurrentDirectory(string path) => _currentDirectory = Path.GetFullPath(path);

    public IReadOnlyList<string> Output => _output;

    public int OutputCheckpoint => _output.Count;

    public bool IsCancellationRequested =>
        (_externalCancellationProvider is not null && _externalCancellationProvider.IsCancellationRequested)
        || (_inputCancellationProvider is not null && _inputCancellationProvider.IsCancellationRequested)
        || (_outputCancellationProvider is not null && _outputCancellationProvider.IsCancellationRequested);

    public string ReadInput(InputRequest request) => _inputProvider.ReadInput(request);

    public string ReadKey() =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider
            ? keyboardInputProvider.ReadKey()
            : string.Empty;

    public bool IsKeyDown(string keyText) =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider &&
        keyboardInputProvider.IsKeyDown(keyText);

    public long GetMouseX() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseX()
            : -1;

    public long GetMouseY() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseY()
            : -1;

    public void GetMousePosition(out long x, out long y)
    {
        if (_inputProvider is IMouseInputProvider mouseInputProvider)
        {
            mouseInputProvider.GetMousePosition(out x, out y);
            return;
        }

        x = -1;
        y = -1;
    }

    public long GetMouseClearGeneration() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseClearGeneration()
            : 0;

    public bool IsMouseButtonDown(int buttonNumber) =>
        _inputProvider is IMouseInputProvider mouseInputProvider &&
        mouseInputProvider.IsMouseButtonDown(buttonNumber);

    public int ReadMouseButtonPress() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseButtonPress()
            : 0;

    public MouseInputEvent ReadMouseEvent() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseEvent()
            : default;

    public bool IsScreenActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsScreenActive;

    public bool IsCanvasActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsCanvasActive;

    public bool IsScreenOutputActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsScreenOutputActive;

    public int ScreenMode =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenMode : 0;

    public int ScreenWidth =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenWidth : 0;

    public int ScreenHeight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenHeight : 0;

    public int CurrentForegroundColor =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentForegroundColor : 15;

    public int CurrentBackgroundColor =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentBackgroundColor : 0;

    public bool IsTextBackgroundTransparent =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsTextBackgroundTransparent;

    public long CurrentGraphicsX =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsX : 0;

    public long CurrentGraphicsY =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsY : 0;

    public long CanvasCameraX =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CanvasCameraX : 0;

    public long CanvasCameraY =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CanvasCameraY : 0;

    public bool ViewportEnabled =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.ViewportEnabled;

    public bool ViewportRelativeCoordinates =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.ViewportRelativeCoordinates;

    public long ViewportLeft =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ViewportLeft : 0;

    public long ViewportTop =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ViewportTop : 0;

    public long ViewportRight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ViewportRight : -1;

    public long ViewportBottom =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ViewportBottom : -1;

    public bool WindowEnabled =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.WindowEnabled;

    public bool WindowUsesScreenCoordinates =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.WindowUsesScreenCoordinates;

    public double WindowLeft =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.WindowLeft : 0d;

    public double WindowTop =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.WindowTop : 0d;

    public double WindowRight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.WindowRight : 0d;

    public double WindowBottom =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.WindowBottom : 0d;

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetGraphicsCurrentPosition(x, y);
        }
    }

    public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates) =>
        _inputProvider is IGraphicsHost graphicsHost &&
        graphicsHost.SetViewport(left, top, right, bottom, fillColor, borderColor, useScreenCoordinates);

    public void ResetViewport()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetViewport();
        }
    }

    public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates) =>
        _inputProvider is IGraphicsHost graphicsHost &&
        graphicsHost.SetWindow(left, top, right, bottom, useScreenCoordinates);

    public void ResetWindow()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetWindow();
        }
    }

    public void ActivateScreen(int mode)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateScreen(mode);
        }
    }

    public void ActivateScreen(int mode, int activePage, int visualPage)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateScreen(mode, activePage, visualPage);
        }
    }

    public void ActivateCanvas(long width, long height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateCanvas(width, height);
        }
    }

    public void SetGraphicsSmoothing(bool enabled)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetGraphicsSmoothing(enabled);
        }
    }

    public void BeginFrame()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.BeginFrame();
        }
    }

    public void PresentFrame()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.PresentFrame();
        }
    }

    public void SetCanvasCamera(long x, long y)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetCanvasCamera(x, y);
        }
    }

    public void ResetCanvasCamera()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetCanvasCamera();
        }
    }

    public void ClearFrame(int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ClearFrame(color);
        }
    }

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasLine(x1, y1, x2, y2, color);
        }
    }

    public void DrawCanvasCircle(long x, long y, long radius, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasCircle(x, y, radius, color);
        }
    }

    public void FillCanvasCircle(long x, long y, long radius, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.FillCanvasCircle(x, y, radius, color);
        }
    }

    public void DrawCanvasText(string text, long x, long y, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasText(text, x, y, color);
        }
    }

    public void DrawRect(long x, long y, long width, long height, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawRect(x, y, width, height, color);
        }
    }

    public void FillRect(long x, long y, long width, long height, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.FillRect(x, y, width, height, color);
        }
    }

    public bool CopyScreenPage(int sourcePage, int destinationPage) =>
        _inputProvider is IGraphicsHost graphicsHost &&
        graphicsHost.CopyScreenPage(sourcePage, destinationPage);

    public void DeactivateScreen()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DeactivateScreen();
        }
    }

    public void ClearGraphics()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ClearGraphics();
        }
    }

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetTextColor(foreground, backgroundMode, background);
        }
    }

    public void LocateText(int row, int column)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.LocateText(row, column);
        }
    }

    public void ResetPalette()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetPalette();
        }
    }

    public void SetPaletteColor(int index, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetPaletteColor(index, color);
        }
    }

    public RuntimeImageValue? LoadImage(string path) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadImage(path) : null;

    public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadSprite(path, frameWidth, frameHeight) : null;

    public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadTileset(path, tileWidth, tileHeight, margin, spacing) : null;

    public RuntimeTilemapValue? LoadTilemap(string path) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadTilemap(path) : null;

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawImage(image, x, y, width, height);
        }
    }

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawSprite(sprite, frame, x, y, width, height);
        }
    }

    public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawImageRegion(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height);
        }
    }

    public bool SetTextViewport(int topRow, int bottomRow) =>
        _inputProvider is IGraphicsHost graphicsHost &&
        graphicsHost.SetTextViewport(topRow, bottomRow);

    public void ResetTextViewport()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetTextViewport();
        }
    }

    public void WriteGraphicsText(string text, bool newLine)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.WriteGraphicsText(text, newLine);
        }
    }

    public string ReadGraphicsInput(InputRequest request) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ReadGraphicsInput(request) : string.Empty;

    public void SetPixel(long x, long y, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetPixel(x, y, color);
        }
    }

    public int GetPixel(long x, long y) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.GetPixel(x, y) : 0;

    public void SetAbsolutePixel(long x, long y, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetAbsolutePixel(x, y, color);
        }
    }

    public int GetAbsolutePixel(long x, long y) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.GetAbsolutePixel(x, y) : 0;

    public bool TryWriteAbsolutePixels(long x, long y, int width, int height, int[] pixels) => (_inputProvider as IGraphicsHost)?.TryWriteAbsolutePixels(x, y, width, height, pixels) ?? false;

    public int[]? CaptureAbsolutePixels(long x, long y, int width, int height) =>
        (_inputProvider as IGraphicsHost)?.CaptureAbsolutePixels(x, y, width, height);

    public void Paint(GraphicsPaintRequest request)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.Paint(request);
        }
    }

    public void Paint(long x, long y, int fillColor, int borderColor)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.Paint(x, y, fillColor, borderColor);
        }
    }

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
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
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCircle(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
        }
    }

    public void WriteOutput(string text)
    {
        _output.Add(text);
        _outputWriter.WriteOutput(text);
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > _output.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
        }

        if (checkpoint == _output.Count)
        {
            return;
        }

        _output.RemoveRange(checkpoint, _output.Count - checkpoint);

        if (_outputWriter is IReversibleOutputWriter reversibleOutputWriter)
        {
            reversibleOutputWriter.RestoreOutputCheckpoint(checkpoint);
        }
    }

    public DateTimeOffset GetLocalNow() => _runtimeClock.GetLocalNow();

    public TimeZoneInfo GetLocalTimeZone() => _runtimeClock.GetLocalTimeZone();

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone) => _runtimeClock.TryResolveTimeZone(id, out timeZone);

    public double NextRandomDouble() => _runtimeRandom.NextRandomDouble();

    public void Reseed(int seed) => _runtimeRandom.Reseed(seed);
}
