using System.Windows;
using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public sealed class ProgramConsoleService : ViewModelBase, IProgramConsoleService, IRuntimeCancellationProvider
{
    private static readonly TimeSpan GraphicsFrameCoalescingInterval = TimeSpan.FromMilliseconds(16);
    private const long GraphicsFrameSummaryInterval = 120;

    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly RuntimeInputSnapshot _inputSnapshot = new();
    private DispatcherFrame? _pendingFrame;
    private string _currentInputText = string.Empty;
    private string _currentPromptText = string.Empty;
    private bool _isWaitingForInput;
    private string _outputText = string.Empty;
    private string _submittedText = string.Empty;
    private bool _isScreenActive;
    private bool _isScreenOutputActive;
    private int _screenMode;
    private int _screenWidth;
    private int _screenHeight;
    private string _graphicsText = string.Empty;
    private readonly ScreenTextBuffer _graphicsTextBuffer = new();
    private int _graphicsForeground = 15;
    private int? _graphicsBackground;
    private int _graphicsRow = 1;
    private int _graphicsColumn = 1;
    private int _graphicsInputRequestCount;
    private ScreenFramebuffer? _framebuffer;
    private ScreenFramebufferSnapshot? _graphicsFrame;
    private bool _graphicsVisualRefreshPending;
    private DispatcherTimer? _graphicsVisualRefreshTimer;
    private NativeGraphicsScreenWindow? _graphicsWindow;
    private DateTimeOffset? _graphicsFrameMetricsStartedAt;
    private DateTimeOffset? _lastGraphicsFramePublishedAt;
    private long _graphicsFramePublishCount;
    private long _graphicsImmediateFramePublishCount;
    private long _graphicsCoalescedFramePublishCount;
    private long _graphicsQueuedCommandCount;
    private long _graphicsQueuedCommandsSinceLastFrame;
    private long _graphicsMaxCommandsPerFrame;
    private double _graphicsTotalFrameIntervalMs;
    private double _graphicsMaxFrameIntervalMs;
    private long _graphicsKeysEnqueued;
    private long _graphicsKeysRead;
    private long _graphicsMouseEventsEnqueued;
    private long _graphicsMouseEventsRead;
    private long _graphicsMouseMoveEnqueued;
    private long _graphicsMousePressEnqueued;
    private long _graphicsMouseReleaseEnqueued;
    private long _graphicsMouseWheelEnqueued;
    private long _graphicsMouseMoveRead;
    private long _graphicsMousePressRead;
    private long _graphicsMouseReleaseRead;
    private long _graphicsMouseWheelRead;
    private long _graphicsMaxPendingKeys;
    private long _graphicsMaxPendingMouseEvents;
    private long _graphicsInputEventsAtLastSummary;
    private volatile bool _isCancellationRequested;

    public ProgramConsoleService(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null)
    {
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
    }

    public string CurrentInputText
    {
        get => _currentInputText;
        set
        {
            if (SetProperty(ref _currentInputText, value) && _inputSnapshot.IsScreenOutputActive && _isWaitingForInput)
            {
                PublishGraphicsFrame();
            }
        }
    }

    public string CurrentPromptText
    {
        get => _currentPromptText;
        private set
        {
            if (SetProperty(ref _currentPromptText, value) && _inputSnapshot.IsScreenOutputActive)
            {
                PublishGraphicsFrame();
            }
        }
    }

    public bool IsWaitingForInput
    {
        get => _isWaitingForInput;
        private set
        {
            if (SetProperty(ref _isWaitingForInput, value) && _inputSnapshot.IsScreenOutputActive)
            {
                PublishGraphicsFrame();
            }
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set => SetProperty(ref _outputText, value);
    }

    public string GraphicsText
    {
        get => _graphicsText;
        private set => SetProperty(ref _graphicsText, value);
    }

    public ScreenFramebufferSnapshot? GraphicsFrame
    {
        get => _graphicsFrame;
        private set => SetProperty(ref _graphicsFrame, value);
    }

    public bool IsScreenActive => _inputSnapshot.IsScreenActive;

    public bool IsScreenOutputActive => _inputSnapshot.IsScreenOutputActive;

    public int ScreenMode => _inputSnapshot.ScreenMode;

    public int ScreenWidth => _inputSnapshot.ScreenWidth;

    public int ScreenHeight => _inputSnapshot.ScreenHeight;

    public int OutputCheckpoint =>
        string.IsNullOrEmpty(OutputText)
            ? 0
            : OutputText.Split(Environment.NewLine).Length;

    public bool IsCancellationRequested
    {
        get => _isCancellationRequested;
    }

    public void BeginSession()
    {
        InvokeOnDispatcher(ResetSessionState);
        _traceService.Write("console.begin", "Console session started.");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "output",
            EventName = "console.session-begin",
            Source = nameof(ProgramConsoleService),
            OutputLength = 0
        });
    }

    public void Clear()
    {
        InvokeOnDispatcher(ResetSessionState);
    }

    public void ClearOutput()
    {
        InvokeOnDispatcher(ClearOutputCore);
    }

    public string RequestInput(ProgramInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (!dispatcher.CheckAccess())
        {
            return dispatcher.Invoke(() => RequestInput(request));
        }

        if (_pendingFrame is not null)
        {
            throw new InvalidOperationException("Concurrent INPUT requests are not supported.");
        }

        _submittedText = string.Empty;
        CurrentPromptText = CreateConsolePrompt(request);
        CurrentInputText = string.Empty;
        IsWaitingForInput = true;
        _traceService.Write(
            "console.input.request",
            $"request={request.RequestNumber} | values={request.ValueCount} | prompt=\"{SanitizeForLog(CurrentPromptText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "input",
            EventName = "input.requested",
            Source = nameof(ProgramConsoleService),
            TextLength = CurrentPromptText.Length,
            TextPreview = CurrentPromptText,
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["requestNumber"] = request.RequestNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["valueCount"] = request.ValueCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        });

        var frame = new DispatcherFrame();
        _pendingFrame = frame;
        Dispatcher.PushFrame(frame);

        var submittedText = _submittedText;
        ResetInputState();
        return submittedText;
    }

    public void SubmitInput()
    {
        InvokeOnDispatcher(SubmitInputCore);
    }

    public void AppendInputCharacter(char character)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isWaitingForInput || char.IsControl(character))
            {
                return;
            }

            CurrentInputText += character;
        });
    }

    public void BackspaceInputCharacter()
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isWaitingForInput || CurrentInputText.Length == 0)
            {
                return;
            }

            CurrentInputText = CurrentInputText[..^1];
        });
    }

    public void WriteOutput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() =>
        {
            if (_inputSnapshot.IsScreenOutputActive)
            {
                WriteGraphicsTextCore(text, newLine: true);
                PublishGraphicsFrame();
            }
            else
            {
                AppendTranscriptLine(text);
            }
        });
    }

    public void ActivateScreen(int mode)
    {
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsFrameSummary(final: true);
            WriteGraphicsInputSummary(final: true);

            var (width, height) = ResolveScreenSize(mode);
            _isScreenActive = mode != 0;
            _isScreenOutputActive = true;
            _screenMode = mode;
            _screenWidth = width;
            _screenHeight = height;
            _inputSnapshot.SetScreenState(_isScreenActive, _isScreenOutputActive, _screenMode, _screenWidth, _screenHeight);
            _framebuffer = new ScreenFramebuffer(width, height);
            _graphicsTextBuffer.Clear();
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            _graphicsForeground = 15;
            _graphicsBackground = null;
            _graphicsRow = 1;
            _graphicsColumn = 1;
            ResetGraphicsFrameMetrics();
            PublishGraphicsFrame();
            OnPropertyChanged(nameof(IsScreenActive));
            OnPropertyChanged(nameof(IsScreenOutputActive));
            OnPropertyChanged(nameof(ScreenMode));
            OnPropertyChanged(nameof(ScreenWidth));
            OnPropertyChanged(nameof(ScreenHeight));
            EnsureGraphicsWindow();
            WriteGraphicsEvent(
                "screen.activated",
                $"mode={mode} | size={width}x{height}");
        });
    }

    public void DeactivateScreen()
    {
        InvokeOnDispatcher(() =>
        {
            var previousMode = _screenMode;
            var previousWidth = _screenWidth;
            var previousHeight = _screenHeight;
            _isScreenActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _inputSnapshot.SetScreenState(_isScreenActive, _isScreenOutputActive, _screenMode, _screenWidth, _screenHeight);
            _framebuffer = null;
            GraphicsFrame = null;
            _graphicsTextBuffer.Clear();
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            WriteGraphicsFrameSummary(final: true);
            WriteGraphicsInputSummary(final: true);
            OnPropertyChanged(nameof(IsScreenActive));
            OnPropertyChanged(nameof(IsScreenOutputActive));
            OnPropertyChanged(nameof(ScreenMode));
            OnPropertyChanged(nameof(ScreenWidth));
            OnPropertyChanged(nameof(ScreenHeight));
            CloseGraphicsWindow();
            WriteGraphicsEvent(
                "screen.deactivated",
                $"mode={previousMode} | size={previousWidth}x{previousHeight}");
        });
    }

    public void ClearGraphics()
    {
        InvokeOnDispatcher(() =>
        {
            GraphicsText = string.Empty;
            _graphicsTextBuffer.Clear();
            _framebuffer?.Clear();
            PublishGraphicsFrame();
            CancelScheduledGraphicsVisualRefresh();
            _graphicsRow = 1;
            _graphicsColumn = 1;
        });
    }

    public void SetTextColor(int foreground, int? background)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsForeground = foreground;
            if (background.HasValue)
            {
                _graphicsBackground = background.Value;
            }

            PublishGraphicsFrame();
        });
    }

    public void LocateText(int row, int column)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsRow = Math.Max(1, row);
            _graphicsColumn = Math.Max(1, column);
        });
    }

    public void WriteGraphicsText(string text, bool newLine)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsTextCore(text, newLine);
            PublishGraphicsFrame();
        });
    }

    public string ReadGraphicsInput(InputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _graphicsInputRequestCount++;
        return RequestInput(new ProgramInputRequest(_graphicsInputRequestCount, request.PromptText, request.ValueCount));
    }

    public void SetPixel(long x, long y, int color)
    {
        InvokeOnDispatcher(() =>
        {
            SetPixelCore(x, y, color);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public int GetPixel(long x, long y)
    {
        var color = 0;
        InvokeOnDispatcher(() => color = GetPixelCore(x, y), DispatcherPriority.Background);
        return color;
    }

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        InvokeOnDispatcher(() =>
        {
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

            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawCircle(long x, long y, long radius, int color)
    {
        InvokeOnDispatcher(() =>
        {
            DrawCircleCore(x, y, radius, color);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void EnqueueKey(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.EnqueueKey(keyText);
        if (_inputSnapshot.IsScreenActive)
        {
            _graphicsKeysEnqueued++;
            _graphicsMaxPendingKeys = Math.Max(_graphicsMaxPendingKeys, _inputSnapshot.PendingKeyCount);
        }
    }

    public void SetKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.SetKeyDown(keyText);
    }

    public void SetKeyUp(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.SetKeyUp(keyText);
    }

    public string ReadKey()
    {
        if (_inputSnapshot.TryReadKey(out var keyText))
        {
            if (_inputSnapshot.IsScreenActive)
            {
                _graphicsKeysRead++;
            }

            return keyText;
        }

        return string.Empty;
    }

    public bool IsKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return false;
        }

        return _inputSnapshot.IsKeyDown(keyText);
    }

    public void ClearKeyState()
    {
        _inputSnapshot.ClearKeyState();
    }

    public void SetMousePosition(long x, long y)
    {
        _inputSnapshot.SetMousePosition(x, y);
    }

    public void SetMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.SetMouseButtonDown(buttonNumber);
    }

    public void EnqueueMouseButtonPress(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber));
    }

    public void EnqueueMouseButtonRelease(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
    }

    public void EnqueueMouseMove()
    {
        EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Move, 0));
    }

    public void EnqueueMouseWheel(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Wheel, delta));
    }

    public void SetMouseButtonUp(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.SetMouseButtonUp(buttonNumber);
    }

    public long GetMouseX()
    {
        return _inputSnapshot.GetMouseX();
    }

    public long GetMouseY()
    {
        return _inputSnapshot.GetMouseY();
    }

    public bool IsMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return false;
        }

        return _inputSnapshot.IsMouseButtonDown(buttonNumber);
    }

    public int ReadMouseButtonPress()
    {
        return _inputSnapshot.ReadMouseButtonPress();
    }

    public MouseInputEvent ReadMouseEvent()
    {
        if (_inputSnapshot.TryReadMouseEvent(out var mouseEvent))
        {
            if (_inputSnapshot.IsScreenActive)
            {
                _graphicsMouseEventsRead++;
                RecordGraphicsMouseEventRead(mouseEvent.Kind);
            }

            return mouseEvent;
        }

        return default;
    }

    public void ClearMouseState()
    {
        _inputSnapshot.ClearMouseState();
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        InvokeOnDispatcher(() => RestoreOutputCheckpointCore(checkpoint));
    }

    private void SubmitInputCore()
    {
        if (_pendingFrame is null)
        {
            return;
        }

        _submittedText = CurrentInputText;
        if (_inputSnapshot.IsScreenOutputActive)
        {
            WriteGraphicsTextCore(CurrentPromptText + CurrentInputText, newLine: true);
            IsWaitingForInput = false;
            PublishGraphicsFrame();
        }
        else
        {
            AppendTranscriptLine(CurrentPromptText + CurrentInputText);
        }

        _traceService.Write(
            "console.input.submit",
            $"text=\"{SanitizeForLog(CurrentInputText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "input",
            EventName = "input.submitted",
            Source = nameof(ProgramConsoleService),
            TextLength = CurrentInputText.Length,
            TextPreview = CurrentInputText,
            OutputLength = OutputCheckpoint
        });
        _pendingFrame.Continue = false;
        _pendingFrame = null;
    }

    private void AppendTranscriptLine(string line)
    {
        OutputText = string.IsNullOrEmpty(OutputText)
            ? line
            : $"{OutputText}{Environment.NewLine}{line}";

        _traceService.Write("console.output", SanitizeForLog(line));
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "output",
            EventName = "output.changed",
            Source = nameof(ProgramConsoleService),
            TextLength = line.Length,
            TextPreview = line,
            OutputLength = OutputCheckpoint
        });
    }

    private void RestoreOutputCheckpointCore(int checkpoint)
    {
        if (checkpoint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint must not be negative.");
        }

        if (string.IsNullOrEmpty(OutputText))
        {
            if (checkpoint == 0)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
        }

        var lines = OutputText.Split(Environment.NewLine);
        if (checkpoint > lines.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
        }

        OutputText = checkpoint == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines.Take(checkpoint));

        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "output",
            EventName = "output.restored",
            Source = nameof(ProgramConsoleService),
            OutputLength = checkpoint
        });
    }

    private void ResetInputState()
    {
        IsWaitingForInput = false;
        CurrentPromptText = string.Empty;
        CurrentInputText = string.Empty;
    }

    private void ResetSessionState()
    {
        if (_pendingFrame is not null)
        {
            _pendingFrame.Continue = false;
            _pendingFrame = null;
        }

        _submittedText = string.Empty;
        _isCancellationRequested = false;
        _inputSnapshot.Reset();
        _isScreenActive = false;
        _isScreenOutputActive = false;
        _screenMode = 0;
        _screenWidth = 0;
        _screenHeight = 0;
        _framebuffer = null;
        GraphicsFrame = null;
        _graphicsTextBuffer.Clear();
        GraphicsText = string.Empty;
        CancelScheduledGraphicsVisualRefresh();
        OutputText = string.Empty;
        ResetInputState();
        OnPropertyChanged(nameof(IsScreenActive));
        OnPropertyChanged(nameof(IsScreenOutputActive));
        OnPropertyChanged(nameof(ScreenMode));
        OnPropertyChanged(nameof(ScreenWidth));
        OnPropertyChanged(nameof(ScreenHeight));
        CloseGraphicsWindow();
    }

    private void ClearOutputCore()
    {
        if (string.IsNullOrEmpty(OutputText))
        {
            return;
        }

        OutputText = string.Empty;
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "output",
            EventName = "output.cleared",
            Source = nameof(ProgramConsoleService),
            OutputLength = 0
        });
    }

    private void InvokeOnDispatcher(Action action, DispatcherPriority priority = DispatcherPriority.Send)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, priority);
    }

    private static string CreateConsolePrompt(ProgramInputRequest request)
    {
        var promptText = request.PromptText?.TrimEnd();

        if (string.IsNullOrWhiteSpace(promptText))
        {
            return "? ";
        }

        return promptText.EndsWith('?') || promptText.EndsWith(':')
            ? $"{promptText} "
            : $"{promptText}? ";
    }

    private static string SanitizeForLog(string text)
    {
        return text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();
    }

    private void EnqueueMouseEventCore(MouseInputEvent mouseEvent)
    {
        _inputSnapshot.EnqueueMouseEvent(mouseEvent);
        if (_inputSnapshot.IsScreenActive)
        {
            _graphicsMouseEventsEnqueued++;
            RecordGraphicsMouseEventEnqueued(mouseEvent.Kind);
            _graphicsMaxPendingMouseEvents = Math.Max(_graphicsMaxPendingMouseEvents, _inputSnapshot.PendingMouseEventCount);
        }
    }

    private void RecordGraphicsMouseEventEnqueued(MouseInputEventKind eventKind)
    {
        switch (eventKind)
        {
            case MouseInputEventKind.Move:
                _graphicsMouseMoveEnqueued++;
                break;
            case MouseInputEventKind.Press:
                _graphicsMousePressEnqueued++;
                break;
            case MouseInputEventKind.Release:
                _graphicsMouseReleaseEnqueued++;
                break;
            case MouseInputEventKind.Wheel:
                _graphicsMouseWheelEnqueued++;
                break;
        }
    }

    private void RecordGraphicsMouseEventRead(MouseInputEventKind eventKind)
    {
        switch (eventKind)
        {
            case MouseInputEventKind.Move:
                _graphicsMouseMoveRead++;
                break;
            case MouseInputEventKind.Press:
                _graphicsMousePressRead++;
                break;
            case MouseInputEventKind.Release:
                _graphicsMouseReleaseRead++;
                break;
            case MouseInputEventKind.Wheel:
                _graphicsMouseWheelRead++;
                break;
        }
    }

    private void WriteGraphicsEvent(string eventName, string message)
    {
        _traceService.Write(eventName, message);
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "graphics",
            EventName = eventName,
            Source = nameof(ProgramConsoleService),
            TextPreview = message
        });
    }

    private static bool IsSupportedMouseButton(int buttonNumber) => buttonNumber is >= 1 and <= 3;

    private void EnsureGraphicsWindow()
    {
        if (_graphicsWindow is not null)
        {
            _graphicsWindow.Activate();
            return;
        }

        _graphicsWindow = new NativeGraphicsScreenWindow(this, _screenWidth, _screenHeight);
        _graphicsWindow.Closed += OnGraphicsWindowClosed;
        _graphicsWindow.Show();
        _graphicsWindow.PresentFrame(GraphicsFrame);
    }

    private void CloseGraphicsWindow()
    {
        if (_graphicsWindow is null)
        {
            return;
        }

        var window = _graphicsWindow;
        _graphicsWindow = null;
        window.Closed -= OnGraphicsWindowClosed;
        window.Close();
    }

    private void OnGraphicsWindowClosed(object? sender, EventArgs e)
    {
        if (_graphicsWindow is null)
        {
            return;
        }

        _graphicsWindow.Closed -= OnGraphicsWindowClosed;
        _graphicsWindow = null;
        _isCancellationRequested = true;
        WriteGraphicsEvent("screen.closed-by-user", "SCREEN window was closed by the user; run cancellation requested.");
    }

    private void WriteGraphicsTextCore(string text, bool newLine)
    {
        _graphicsTextBuffer.Write(_graphicsRow, _graphicsColumn, text, _graphicsForeground, _graphicsBackground);

        if (newLine)
        {
            _graphicsRow++;
            _graphicsColumn = 1;
            _graphicsTextBuffer.EnsureRow(_graphicsRow);
        }
        else
        {
            _graphicsColumn += text.Length;
        }

        GraphicsText = _graphicsTextBuffer.ToPlainText();
    }

    private void SetPixelCore(long x, long y, int color)
    {
        if (!IsInsideScreen(x, y))
        {
            return;
        }

        _framebuffer?.SetPixel(x, y, color);
    }

    private int GetPixelCore(long x, long y)
    {
        return _framebuffer?.GetPixel(x, y) ?? 0;
    }

    private bool IsInsideScreen(long x, long y) =>
        _inputSnapshot.IsScreenActive &&
        x >= 0 &&
        y >= 0 &&
        x < _screenWidth &&
        y < _screenHeight;

    private void DrawLineCore(long x1, long y1, long x2, long y2, int color)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            SetPixelCore(x1, y1, color);
            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var doubledError = 2 * error;
            if (doubledError >= dy)
            {
                error += dy;
                x1 += sx;
            }

            if (doubledError <= dx)
            {
                error += dx;
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
            AddFillCore(left, top, right, bottom, color);

            return;
        }

        DrawLineCore(left, top, right, top, color);
        DrawLineCore(right, top, right, bottom, color);
        DrawLineCore(right, bottom, left, bottom, color);
        DrawLineCore(left, bottom, left, top, color);
    }

    private void DrawCircleCore(long centerX, long centerY, long radius, int color)
    {
        if (radius < 0)
        {
            return;
        }

        var x = radius;
        var y = 0L;
        var error = 1 - x;

        while (x >= y)
        {
            PlotCircleOctants(centerX, centerY, x, y, color);
            y++;

            if (error < 0)
            {
                error += 2 * y + 1;
            }
            else
            {
                x--;
                error += 2 * (y - x) + 1;
            }
        }
    }

    private void PlotCircleOctants(long centerX, long centerY, long x, long y, int color)
    {
        SetPixelCore(centerX + x, centerY + y, color);
        SetPixelCore(centerX + y, centerY + x, color);
        SetPixelCore(centerX - y, centerY + x, color);
        SetPixelCore(centerX - x, centerY + y, color);
        SetPixelCore(centerX - x, centerY - y, color);
        SetPixelCore(centerX - y, centerY - x, color);
        SetPixelCore(centerX + y, centerY - x, color);
        SetPixelCore(centerX + x, centerY - y, color);
    }

    private void RefreshGraphicsVisuals()
    {
        PublishGraphicsFrame(coalesced: true);
    }

    private void PublishGraphicsFrame(bool coalesced = false)
    {
        var frame = _framebuffer?.CreateSnapshot(
            _graphicsTextBuffer,
            _graphicsForeground,
            _isWaitingForInput ? CurrentPromptText + CurrentInputText : null,
            _graphicsRow,
            _graphicsColumn);
        GraphicsFrame = frame;
        _graphicsWindow?.PresentFrame(frame);
        RecordGraphicsFramePublished(coalesced);
    }

    private void ScheduleGraphicsVisualRefresh()
    {
        RecordGraphicsCommandQueued();

        if (Application.Current is null)
        {
            RefreshGraphicsVisuals();
            return;
        }

        if (_graphicsVisualRefreshPending)
        {
            return;
        }

        _graphicsVisualRefreshPending = true;
        var dispatcher = Application.Current.Dispatcher;
        _graphicsVisualRefreshTimer?.Stop();
        _graphicsVisualRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = GraphicsFrameCoalescingInterval
        };
        _graphicsVisualRefreshTimer.Tick += (_, _) =>
        {
            _graphicsVisualRefreshTimer?.Stop();
            _graphicsVisualRefreshTimer = null;
            _graphicsVisualRefreshPending = false;
            RefreshGraphicsVisuals();
        };
        _graphicsVisualRefreshTimer.Start();
    }

    private void CancelScheduledGraphicsVisualRefresh()
    {
        _graphicsVisualRefreshTimer?.Stop();
        _graphicsVisualRefreshTimer = null;
        _graphicsVisualRefreshPending = false;
    }

    private void ResetGraphicsFrameMetrics()
    {
        _graphicsFrameMetricsStartedAt = DateTimeOffset.UtcNow;
        _lastGraphicsFramePublishedAt = null;
        _graphicsFramePublishCount = 0;
        _graphicsImmediateFramePublishCount = 0;
        _graphicsCoalescedFramePublishCount = 0;
        _graphicsQueuedCommandCount = 0;
        _graphicsQueuedCommandsSinceLastFrame = 0;
        _graphicsMaxCommandsPerFrame = 0;
        _graphicsTotalFrameIntervalMs = 0;
        _graphicsMaxFrameIntervalMs = 0;
        _graphicsKeysEnqueued = 0;
        _graphicsKeysRead = 0;
        _graphicsMouseEventsEnqueued = 0;
        _graphicsMouseEventsRead = 0;
        _graphicsMouseMoveEnqueued = 0;
        _graphicsMousePressEnqueued = 0;
        _graphicsMouseReleaseEnqueued = 0;
        _graphicsMouseWheelEnqueued = 0;
        _graphicsMouseMoveRead = 0;
        _graphicsMousePressRead = 0;
        _graphicsMouseReleaseRead = 0;
        _graphicsMouseWheelRead = 0;
        _graphicsMaxPendingKeys = 0;
        _graphicsMaxPendingMouseEvents = 0;
        _graphicsInputEventsAtLastSummary = 0;
    }

    private void RecordGraphicsCommandQueued()
    {
        if (!_inputSnapshot.IsScreenOutputActive)
        {
            return;
        }

        _graphicsQueuedCommandCount++;
        _graphicsQueuedCommandsSinceLastFrame++;
    }

    private void RecordGraphicsFramePublished(bool coalesced)
    {
        if (!_inputSnapshot.IsScreenOutputActive)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_lastGraphicsFramePublishedAt is not null)
        {
            var intervalMs = (now - _lastGraphicsFramePublishedAt.Value).TotalMilliseconds;
            _graphicsTotalFrameIntervalMs += intervalMs;
            _graphicsMaxFrameIntervalMs = Math.Max(_graphicsMaxFrameIntervalMs, intervalMs);
        }

        _lastGraphicsFramePublishedAt = now;
        _graphicsFramePublishCount++;
        if (coalesced)
        {
            _graphicsCoalescedFramePublishCount++;
        }
        else
        {
            _graphicsImmediateFramePublishCount++;
        }

        _graphicsMaxCommandsPerFrame = Math.Max(_graphicsMaxCommandsPerFrame, _graphicsQueuedCommandsSinceLastFrame);
        _graphicsQueuedCommandsSinceLastFrame = 0;

        if (_graphicsFramePublishCount % GraphicsFrameSummaryInterval == 0)
        {
            WriteGraphicsFrameSummary(final: false);
            WriteGraphicsInputSummary(final: false);
        }
    }

    private void WriteGraphicsFrameSummary(bool final)
    {
        if (_graphicsFramePublishCount == 0)
        {
            return;
        }

        var intervalCount = Math.Max(0, _graphicsFramePublishCount - 1);
        var averageIntervalMs = intervalCount == 0 ? 0 : _graphicsTotalFrameIntervalMs / intervalCount;
        var elapsedMs = _graphicsFrameMetricsStartedAt is null
            ? 0
            : (DateTimeOffset.UtcNow - _graphicsFrameMetricsStartedAt.Value).TotalMilliseconds;

        WriteGraphicsEvent(
            "screen.frame.summary",
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"final={final} | frames={_graphicsFramePublishCount} | immediateFrames={_graphicsImmediateFramePublishCount} | coalescedFrames={_graphicsCoalescedFramePublishCount} | commands={_graphicsQueuedCommandCount} | maxCommandsPerFrame={_graphicsMaxCommandsPerFrame} | avgIntervalMs={averageIntervalMs:F2} | maxIntervalMs={_graphicsMaxFrameIntervalMs:F2} | elapsedMs={elapsedMs:F2}"));
    }

    private void WriteGraphicsInputSummary(bool final)
    {
        var inputEvents =
            _graphicsKeysEnqueued
            + _graphicsKeysRead
            + _graphicsMouseEventsEnqueued
            + _graphicsMouseEventsRead;
        if (inputEvents == 0 || (!final && inputEvents == _graphicsInputEventsAtLastSummary))
        {
            return;
        }

        _graphicsInputEventsAtLastSummary = inputEvents;
        WriteGraphicsEvent(
            "screen.input.summary",
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"final={final} | keysEnqueued={_graphicsKeysEnqueued} | keysRead={_graphicsKeysRead} | mouseEventsEnqueued={_graphicsMouseEventsEnqueued} | mouseEventsRead={_graphicsMouseEventsRead} | moveEnqueued={_graphicsMouseMoveEnqueued} | moveRead={_graphicsMouseMoveRead} | pressEnqueued={_graphicsMousePressEnqueued} | pressRead={_graphicsMousePressRead} | releaseEnqueued={_graphicsMouseReleaseEnqueued} | releaseRead={_graphicsMouseReleaseRead} | wheelEnqueued={_graphicsMouseWheelEnqueued} | wheelRead={_graphicsMouseWheelRead} | maxPendingKeys={_graphicsMaxPendingKeys} | maxPendingMouseEvents={_graphicsMaxPendingMouseEvents}"));
    }

    private void AddFillCore(long left, long top, long right, long bottom, int color)
    {
        if (!_inputSnapshot.IsScreenActive)
        {
            return;
        }

        var clippedLeft = Math.Max(0, left);
        var clippedTop = Math.Max(0, top);
        var clippedRight = Math.Min(_screenWidth - 1L, right);
        var clippedBottom = Math.Min(_screenHeight - 1L, bottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        _framebuffer?.FillRectangle(clippedLeft, clippedTop, clippedRight, clippedBottom, color);
    }

    private static (int Width, int Height) ResolveScreenSize(int mode) => mode switch
    {
        0 => (80 * ScreenTextRasterizer.CellWidth, 25 * ScreenTextRasterizer.CellHeight),
        1 => (320, 200),
        2 => (640, 480),
        3 => (800, 600),
        4 => (1024, 768),
        5 => (1280, 720),
        6 => (1366, 768),
        7 => (1600, 900),
        8 => (1920, 1080),
        9 => (2560, 1440),
        10 => (1280, 720),
        _ => (0, 0)
    };
}
