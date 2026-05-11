using Axiom.Runtime;

namespace Axiom.Execution;

internal sealed class ExecutionRuntimeHostAdapter : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IOutputWriter _outputWriter;
    private readonly IRuntimeClock _runtimeClock;
    private readonly IRuntimeRandom _runtimeRandom;

    public ExecutionRuntimeHostAdapter(IInputProvider inputProvider, IOutputWriter outputWriter, IRuntimeClock runtimeClock, IRuntimeRandom runtimeRandom)
    {
        ArgumentNullException.ThrowIfNull(inputProvider);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(runtimeClock);
        ArgumentNullException.ThrowIfNull(runtimeRandom);

        _inputProvider = inputProvider;
        _outputWriter = outputWriter;
        _runtimeClock = runtimeClock;
        _runtimeRandom = runtimeRandom;
    }

    public IReadOnlyList<string> Output => _output;

    public int OutputCheckpoint => _output.Count;

    public bool IsCancellationRequested =>
        _inputProvider is IRuntimeCancellationProvider inputHost && inputHost.IsCancellationRequested
        || _outputWriter is IRuntimeCancellationProvider outputHost && outputHost.IsCancellationRequested;

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

    public bool IsScreenOutputActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsScreenOutputActive;

    public int ScreenMode =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenMode : 0;

    public int ScreenWidth =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenWidth : 0;

    public int ScreenHeight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenHeight : 0;

    public void ActivateScreen(int mode)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateScreen(mode);
        }
    }

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

    public void SetTextColor(int foreground, int? background)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetTextColor(foreground, background);
        }
    }

    public void LocateText(int row, int column)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.LocateText(row, column);
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

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawLine(x1, y1, x2, y2, color, kind);
        }
    }

    public void DrawCircle(long x, long y, long radius, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCircle(x, y, radius, color);
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
}
