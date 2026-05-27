using System.Text;
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
    private readonly IScreenModeSizeProvider _screenModeSizeProvider;
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
    private readonly StringBuilder _pendingGraphicsTraceLine = new();
    private int _graphicsForeground = 15;
    private int? _graphicsBackground;
    private int _graphicsLastBackgroundColor;
    private int _graphicsRow = 1;
    private int _graphicsColumn = 1;
    private long _graphicsCurrentX;
    private long _graphicsCurrentY;
    private bool _viewportEnabled;
    private bool _viewportRelativeCoordinates;
    private long _viewportLeft;
    private long _viewportTop;
    private long _viewportRight = -1;
    private long _viewportBottom = -1;
    private bool _windowEnabled;
    private bool _windowUsesScreenCoordinates;
    private double _windowLeft;
    private double _windowTop;
    private double _windowRight;
    private double _windowBottom;
    private int _graphicsInputRequestCount;
    private ScreenFramebuffer? _framebuffer;
    private ScreenFramebufferSnapshot? _graphicsFrame;
    private bool _graphicsVisualRefreshPending;
    private DispatcherTimer? _graphicsVisualRefreshTimer;
    private NativeGraphicsScreenWindow? _graphicsWindow;
    private int _graphicsBatchDepth;
    private bool _graphicsBatchHasChanges;
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
    private volatile bool _isSessionActive;

    public ProgramConsoleService(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null,
        IScreenModeSizeProvider? screenModeSizeProvider = null)
    {
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
        _screenModeSizeProvider = screenModeSizeProvider ?? ScreenModeSizeProvider.Instance;
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

    public int CurrentForegroundColor => _graphicsForeground;

    public int CurrentBackgroundColor => _graphicsLastBackgroundColor;

    public bool IsTextBackgroundTransparent => !_graphicsBackground.HasValue;

    public long CurrentGraphicsX => _graphicsCurrentX;

    public long CurrentGraphicsY => _graphicsCurrentY;

    public bool ViewportEnabled => _viewportEnabled;

    public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

    public long ViewportLeft => _viewportLeft;

    public long ViewportTop => _viewportTop;

    public long ViewportRight => _viewportRight;

    public long ViewportBottom => _viewportBottom;

    public bool WindowEnabled => _windowEnabled;

    public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

    public double WindowLeft => _windowLeft;

    public double WindowTop => _windowTop;

    public double WindowRight => _windowRight;

    public double WindowBottom => _windowBottom;

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
        _isSessionActive = true;
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

    public void EndSession()
    {
        _isSessionActive = false;
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

            var (width, height) = _screenModeSizeProvider.GetSize(mode);
            _isScreenActive = mode != 0;
            _isScreenOutputActive = true;
            _screenMode = mode;
            _screenWidth = width;
            _screenHeight = height;
            _inputSnapshot.SetScreenState(_isScreenActive, _isScreenOutputActive, _screenMode, _screenWidth, _screenHeight);
            ResetViewportCore();
            ResetWindowCore();
            _framebuffer = new ScreenFramebuffer(width, height);
            _graphicsTextBuffer.Clear();
            _pendingGraphicsTraceLine.Clear();
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            _graphicsForeground = 15;
            _graphicsBackground = null;
            _graphicsLastBackgroundColor = 0;
            _graphicsRow = 1;
            _graphicsColumn = 1;
            _graphicsCurrentX = 0;
            _graphicsCurrentY = 0;
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
            ResetViewportCore();
            ResetWindowCore();
            FlushPendingGraphicsTraceLine(logEmpty: false);
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
            FlushPendingGraphicsTraceLine(logEmpty: false);
            GraphicsText = string.Empty;
            _graphicsTextBuffer.Clear();
            _framebuffer?.Clear();
            PublishGraphicsFrame();
            CancelScheduledGraphicsVisualRefresh();
            _graphicsRow = 1;
            _graphicsColumn = 1;
            _graphicsCurrentX = 0;
            _graphicsCurrentY = 0;
        });
    }

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsForeground = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _graphicsBackground = background;
                    _graphicsLastBackgroundColor = background;
                    break;
                case TextBackgroundMode.Transparent:
                    _graphicsBackground = null;
                    break;
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
            SetLogicalPixelCore(x, y, color);
            SetGraphicsCurrentPositionCore(x, y);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public int GetPixel(long x, long y)
    {
        var color = 0;
        InvokeOnDispatcher(() => color = GetLogicalPixelCore(x, y), DispatcherPriority.Background);
        return color;
    }

    public void SetAbsolutePixel(long x, long y, int color)
    {
        SetPixelCore(x, y, color);
    }

    public int GetAbsolutePixel(long x, long y)
    {
        return GetPixelCore(x, y);
    }

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        InvokeOnDispatcher(() =>
        {
            GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
            ScheduleGraphicsVisualRefresh();
        });
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
        InvokeOnDispatcher(() =>
        {
            GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawCircle(long x, long y, long radius, int color)
    {
        DrawCircle(x, y, radius, color, relativeCenter: false, startAngle: null, endAngle: null, aspect: 1d, drawStartRadius: false, drawEndRadius: false);
    }

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        InvokeOnDispatcher(() => SetGraphicsCurrentPositionCore(x, y));
    }

    public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
    {
        var applied = false;
        InvokeOnDispatcher(() =>
        {
            applied = TrySetViewportCore(left, top, right, bottom, fillColor, borderColor, useScreenCoordinates);
            if (applied)
            {
                ScheduleGraphicsVisualRefresh();
            }
        });

        return applied;
    }

    public void ResetViewport()
    {
        InvokeOnDispatcher(() =>
        {
            ResetViewportCore(preserveCurrentPosition: true);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
    {
        var applied = false;
        InvokeOnDispatcher(() =>
        {
            applied = TrySetWindowCore(left, top, right, bottom, useScreenCoordinates);
            if (applied)
            {
                ScheduleGraphicsVisualRefresh();
            }
        });

        return applied;
    }

    public void ResetWindow()
    {
        InvokeOnDispatcher(() =>
        {
            ResetWindowCore(preserveCurrentPosition: true);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void Paint(GraphicsPaintRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        InvokeOnDispatcher(() =>
        {
            RunBatchedGraphicsUpdate(() => GraphicsHostDrawing.Paint(this, request));
            SetGraphicsCurrentPositionCore(request.X, request.Y);
        });
    }

    public void Paint(long x, long y, int fillColor, int borderColor)
    {
        Paint(GraphicsPaintRequest.CreateSolid(x, y, fillColor, borderColor));
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
        _isSessionActive = false;
        _inputSnapshot.Reset();
        _isScreenActive = false;
        _isScreenOutputActive = false;
        _screenMode = 0;
        _screenWidth = 0;
        _screenHeight = 0;
        _framebuffer = null;
        GraphicsFrame = null;
        _graphicsTextBuffer.Clear();
        _pendingGraphicsTraceLine.Clear();
        GraphicsText = string.Empty;
        CancelScheduledGraphicsVisualRefresh();
        _graphicsForeground = 15;
        _graphicsBackground = null;
        _graphicsLastBackgroundColor = 0;
        _graphicsRow = 1;
        _graphicsColumn = 1;
        _graphicsCurrentX = 0;
        _graphicsCurrentY = 0;
        ResetViewportCore();
        ResetWindowCore();
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

    private void WriteGraphicsOutputTrace(string line)
    {
        _traceService.Write("screen.output", SanitizeForLog(line));
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "graphics",
            EventName = "screen.output",
            Source = nameof(ProgramConsoleService),
            TextLength = line.Length,
            TextPreview = line
        });
    }

    private void FlushPendingGraphicsTraceLine(bool logEmpty)
    {
        if (_pendingGraphicsTraceLine.Length == 0 && !logEmpty)
        {
            return;
        }

        WriteGraphicsOutputTrace(_pendingGraphicsTraceLine.ToString());
        _pendingGraphicsTraceLine.Clear();
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
        if (!_isSessionActive)
        {
            return;
        }

        _isCancellationRequested = true;
        WriteGraphicsEvent("screen.closed-by-user", "SCREEN window was closed by the user; run cancellation requested.");
    }

    private void WriteGraphicsTextCore(string text, bool newLine)
    {
        _graphicsTextBuffer.Write(_graphicsRow, _graphicsColumn, text, _graphicsForeground, _graphicsBackground);
        _pendingGraphicsTraceLine.Append(text);

        if (newLine)
        {
            _graphicsRow++;
            _graphicsColumn = 1;
            _graphicsTextBuffer.EnsureRow(_graphicsRow);
            FlushPendingGraphicsTraceLine(logEmpty: true);
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
        _graphicsBatchHasChanges = true;
    }

    private void SetLogicalPixelCore(long x, long y, int color)
    {
        if (!TryTranslateLogicalToAbsoluteCore(x, y, out var absoluteX, out var absoluteY))
        {
            return;
        }

        SetPixelCore(absoluteX, absoluteY, color);
    }

    private void SetGraphicsCurrentPositionCore(long x, long y)
    {
        _graphicsCurrentX = x;
        _graphicsCurrentY = y;
    }

    private int GetPixelCore(long x, long y)
    {
        return _framebuffer?.GetPixel(x, y) ?? 0;
    }

    private int GetLogicalPixelCore(long x, long y)
    {
        return TryTranslateLogicalToAbsoluteCore(x, y, out var absoluteX, out var absoluteY)
            ? GetPixelCore(absoluteX, absoluteY)
            : 0;
    }

    private void PaintCore(GraphicsPaintRequest request)
    {
        GraphicsHostDrawing.Paint(this, request);
    }

    private void RunBatchedGraphicsUpdate(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _graphicsBatchDepth++;
        try
        {
            action();
        }
        finally
        {
            _graphicsBatchDepth--;
            if (_graphicsBatchDepth == 0 && _graphicsBatchHasChanges)
            {
                _graphicsBatchHasChanges = false;
                ScheduleGraphicsVisualRefresh();
            }
        }
    }

    private bool IsInsideScreen(long x, long y) =>
        _inputSnapshot.IsScreenActive &&
        x >= 0 &&
        y >= 0 &&
        x < _screenWidth &&
        y < _screenHeight;

    private GraphicsViewport GetEffectiveViewportCore()
    {
        if (_viewportEnabled)
        {
            return new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates);
        }

        var right = _screenWidth > 0 ? _screenWidth - 1L : -1L;
        var bottom = _screenHeight > 0 ? _screenHeight - 1L : -1L;
        return new GraphicsViewport(0, 0, right, bottom, UsesRelativeCoordinates: false);
    }

    private bool TryTranslateLogicalToAbsoluteCore(long x, long y, out long absoluteX, out long absoluteY)
    {
        return GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out absoluteX, out absoluteY);
    }

    private (long X, long Y) ResolveCurrentAbsolutePositionCore()
    {
        return GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY)
            ? (absoluteX, absoluteY)
            : (0L, 0L);
    }

    private void ResetViewportCore(bool preserveCurrentPosition = false)
    {
        var absolutePosition = preserveCurrentPosition ? ResolveCurrentAbsolutePositionCore() : (0L, 0L);
        _viewportEnabled = false;
        _viewportRelativeCoordinates = false;
        _viewportLeft = 0;
        _viewportTop = 0;
        _viewportRight = _screenWidth > 0 ? _screenWidth - 1L : -1L;
        _viewportBottom = _screenHeight > 0 ? _screenHeight - 1L : -1L;
        if (preserveCurrentPosition)
        {
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, absolutePosition.Item1, absolutePosition.Item2, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        }
    }

    private bool TrySetViewportCore(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
    {
        if (_screenWidth <= 0 || _screenHeight <= 0 || left > right || top > bottom)
        {
            return false;
        }

        var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
        var baseTop = _viewportEnabled ? _viewportTop : 0L;
        var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
        var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
        var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
        var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
        if (!IsInsideScreen(absoluteLeft, absoluteTop) || !IsInsideScreen(absoluteRight, absoluteBottom))
        {
            return false;
        }

        var previousAbsolutePosition = ResolveCurrentAbsolutePositionCore();
        RunBatchedGraphicsUpdate(() =>
        {
            if (fillColor.HasValue)
            {
                _framebuffer?.FillRectangle(absoluteLeft, absoluteTop, absoluteRight, absoluteBottom, fillColor.Value);
                _graphicsBatchHasChanges = true;
            }

            if (borderColor.HasValue)
            {
                DrawAbsoluteLineCore(absoluteLeft, absoluteTop, absoluteRight, absoluteTop, borderColor.Value);
                DrawAbsoluteLineCore(absoluteRight, absoluteTop, absoluteRight, absoluteBottom, borderColor.Value);
                DrawAbsoluteLineCore(absoluteRight, absoluteBottom, absoluteLeft, absoluteBottom, borderColor.Value);
                DrawAbsoluteLineCore(absoluteLeft, absoluteBottom, absoluteLeft, absoluteTop, borderColor.Value);
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, previousAbsolutePosition.X, previousAbsolutePosition.Y, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        });

        return true;
    }

    private void ResetWindowCore(bool preserveCurrentPosition = false)
    {
        var absolutePosition = preserveCurrentPosition ? ResolveCurrentAbsolutePositionCore() : (0L, 0L);
        _windowEnabled = false;
        _windowUsesScreenCoordinates = false;
        _windowLeft = 0d;
        _windowTop = 0d;
        _windowRight = 0d;
        _windowBottom = 0d;
        if (preserveCurrentPosition)
        {
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, absolutePosition.Item1, absolutePosition.Item2, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        }
    }

    private bool TrySetWindowCore(double left, double top, double right, double bottom, bool useScreenCoordinates)
    {
        var viewport = GetEffectiveViewportCore();
        if (_screenWidth <= 0 || _screenHeight <= 0 || viewport.Width < 2 || viewport.Height < 2)
        {
            return false;
        }

        if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
        {
            return false;
        }

        var previousAbsolutePosition = ResolveCurrentAbsolutePositionCore();
        _windowEnabled = true;
        _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
        _windowLeft = window.Left;
        _windowTop = window.Top;
        _windowRight = window.Right;
        _windowBottom = window.Bottom;
        if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, previousAbsolutePosition.X, previousAbsolutePosition.Y, out var logicalX, out var logicalY))
        {
            SetGraphicsCurrentPositionCore(logicalX, logicalY);
        }

        return true;
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
            SetLogicalPixelCore(x1, y1, color);
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

        SetGraphicsCurrentPositionCore(x2, y2);
    }

    private void DrawAbsoluteLineCore(long x1, long y1, long x2, long y2, int color)
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
            AddFillCore(left, top, right, bottom, color);

            return;
        }

        DrawLineCore(left, top, right, top, color);
        DrawLineCore(right, top, right, bottom, color);
        DrawLineCore(right, bottom, left, bottom, color);
        DrawLineCore(left, bottom, left, top, color);
    }

    private void DrawCircleCore(
        long centerX,
        long centerY,
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

        if (relativeCenter)
        {
            if (!GraphicsCoordinateValidation.TryResolvePoint(_graphicsCurrentX, _graphicsCurrentY, centerX, centerY, out centerX, out centerY))
            {
                return;
            }
        }
        else if (!GraphicsCoordinateValidation.IsSupportedCircleCenter(centerX, centerY))
        {
            return;
        }

        SetGraphicsCurrentPositionCore(centerX, centerY);

        var radiusX = aspect <= 1d ? radius : radius / aspect;
        var radiusY = aspect <= 1d ? radius * aspect : radius;

        if (radiusX <= 0 || radiusY <= 0)
        {
            SetLogicalPixelCore(centerX, centerY, color);
            return;
        }

        var arcStart = NormalizeCircleAngle(startAngle) ?? 0d;
        var normalizedEnd = NormalizeCircleAngle(endAngle);
        var fullCircle = !startAngle.HasValue && !endAngle.HasValue;
        if (!fullCircle && startAngle.HasValue && normalizedEnd.HasValue)
        {
            fullCircle = Math.Abs(arcStart - normalizedEnd.Value) <= 1e-9;
        }

        var sweep = fullCircle ? Math.Tau : (normalizedEnd ?? Math.Tau) - arcStart;
        if (!fullCircle && sweep <= 0d)
        {
            sweep += Math.Tau;
        }

        var startPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart);

        if (drawStartRadius)
        {
            DrawLineCore(centerX, centerY, startPoint.X, startPoint.Y, color);
        }

        GraphicsEllipseRasterizer.Rasterize(
            centerX,
            centerY,
            radiusX,
            radiusY,
            fullCircle,
            arcStart,
            sweep,
            GetRasterWidthCore(),
            GetRasterHeightCore(),
            (x, y) => SetLogicalPixelCore(x, y, color));

        if (drawEndRadius)
        {
            var endPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart + sweep);
            DrawLineCore(centerX, centerY, endPoint.X, endPoint.Y, color);
        }

        SetGraphicsCurrentPositionCore(centerX, centerY);
    }

    private static double? NormalizeCircleAngle(double? angle) =>
        angle switch
        {
            null => null,
            < 0 => GraphicsEllipseRasterizer.NormalizeAngle(-angle.Value),
            _ => GraphicsEllipseRasterizer.NormalizeAngle(angle.Value)
        };

    private int GetRasterWidthCore()
    {
        var viewport = GetEffectiveViewportCore();
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Width))
            : _screenWidth;
    }

    private int GetRasterHeightCore()
    {
        var viewport = GetEffectiveViewportCore();
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Height))
            : _screenHeight;
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
            _graphicsBackground,
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

}
