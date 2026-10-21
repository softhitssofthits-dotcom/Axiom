using Axiom.Runtime;

sealed class ProfilingRuntimeHost : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IRuntimeClock _runtimeClock;
    private readonly IRuntimeRandom _runtimeRandom;

    public ProfilingRuntimeHost(IInputProvider inputProvider, IRuntimeClock runtimeClock, IRuntimeRandom runtimeRandom)
    {
        _inputProvider = inputProvider;
        _runtimeClock = runtimeClock;
        _runtimeRandom = runtimeRandom;
    }

    public IReadOnlyList<string> Output => _output;

    public bool IsCancellationRequested => false;

    public int OutputCheckpoint => _output.Count;

    public string ReadInput(InputRequest request) => _inputProvider.ReadInput(request);

    public string ReadKey() =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider
            ? keyboardInputProvider.ReadKey()
            : string.Empty;

    public bool IsKeyDown(string keyText) =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider
        && keyboardInputProvider.IsKeyDown(keyText);

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
        _inputProvider is IMouseInputProvider mouseInputProvider
        && mouseInputProvider.IsMouseButtonDown(buttonNumber);

    public int ReadMouseButtonPress() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseButtonPress()
            : 0;

    public MouseInputEvent ReadMouseEvent() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseEvent()
            : default;

    public void WriteOutput(string text)
    {
        _output.Add(text);
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > _output.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is out of range.");
        }

        _output.RemoveRange(checkpoint, _output.Count - checkpoint);
    }

    public DateTimeOffset GetLocalNow() => _runtimeClock.GetLocalNow();

    public double GetMonotonicSeconds() => _runtimeClock.GetMonotonicSeconds();

    public TimeZoneInfo GetLocalTimeZone() => _runtimeClock.GetLocalTimeZone();

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone) => _runtimeClock.TryResolveTimeZone(id, out timeZone);

    public double NextRandomDouble() => _runtimeRandom.NextRandomDouble();

    public void Reseed(int seed) => _runtimeRandom.Reseed(seed);

    public bool IsCanvasActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsCanvasActive;

    public bool IsScreenActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsScreenActive;

    public int ScreenWidth =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenWidth : 0;

    public int ScreenHeight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenHeight : 0;

    public long CurrentGraphicsX =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsX : 0;

    public long CurrentGraphicsY =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsY : 0;

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetGraphicsCurrentPosition(x, y);
        }
    }

    public void ActivateCanvas(long width, long height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateCanvas(width, height);
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

    public RuntimeImageValue? LoadImage(string path) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadImage(path) : null;

    public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadTileset(path, tileWidth, tileHeight, margin, spacing) : null;

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
}
