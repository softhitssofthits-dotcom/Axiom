using System.Windows;
using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public sealed class ProgramConsoleService : ViewModelBase, IProgramConsoleService
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private DispatcherFrame? _pendingFrame;
    private readonly Queue<string> _pendingKeys = [];
    private readonly Queue<MouseInputEvent> _pendingMouseEvents = [];
    private readonly HashSet<string> _pressedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<int> _pressedMouseButtons = [];
    private string _currentInputText = string.Empty;
    private string _currentPromptText = string.Empty;
    private bool _isWaitingForInput;
    private string _outputText = string.Empty;
    private string _submittedText = string.Empty;
    private long _mouseX = -1;
    private long _mouseY = -1;
    private bool _isScreenActive;
    private int _screenMode;
    private int _screenWidth;
    private int _screenHeight;
    private string _graphicsText = string.Empty;
    private int _graphicsForeground = 15;
    private int? _graphicsBackground;
    private int _graphicsRow = 1;
    private int _graphicsColumn = 1;
    private int _graphicsInputRequestCount;
    private ScreenFramebuffer? _framebuffer;
    private ScreenFramebufferSnapshot? _graphicsFrame;
    private bool _graphicsVisualRefreshPending;
    private NativeGraphicsScreenWindow? _graphicsWindow;

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
            if (SetProperty(ref _currentInputText, value) && _isScreenActive && _isWaitingForInput)
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
            if (SetProperty(ref _currentPromptText, value) && _isScreenActive)
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
            if (SetProperty(ref _isWaitingForInput, value) && _isScreenActive)
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

    public bool IsScreenActive
    {
        get
        {
            var value = false;
            InvokeOnDispatcher(() => value = _isScreenActive, DispatcherPriority.Background);
            return value;
        }
    }

    public int ScreenMode
    {
        get
        {
            var value = 0;
            InvokeOnDispatcher(() => value = _screenMode, DispatcherPriority.Background);
            return value;
        }
    }

    public int ScreenWidth
    {
        get
        {
            var value = 0;
            InvokeOnDispatcher(() => value = _screenWidth, DispatcherPriority.Background);
            return value;
        }
    }

    public int ScreenHeight
    {
        get
        {
            var value = 0;
            InvokeOnDispatcher(() => value = _screenHeight, DispatcherPriority.Background);
            return value;
        }
    }

    public int OutputCheckpoint =>
        string.IsNullOrEmpty(OutputText)
            ? 0
            : OutputText.Split(Environment.NewLine).Length;

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
        InvokeOnDispatcher(() => AppendTranscriptLine(text));
    }

    public void ActivateScreen(int mode)
    {
        InvokeOnDispatcher(() =>
        {
            var (width, height) = ResolveScreenSize(mode);
            _isScreenActive = true;
            _screenMode = mode;
            _screenWidth = width;
            _screenHeight = height;
            _framebuffer = new ScreenFramebuffer(width, height);
            GraphicsText = string.Empty;
            _graphicsVisualRefreshPending = false;
            _graphicsForeground = 15;
            _graphicsBackground = null;
            _graphicsRow = 1;
            _graphicsColumn = 1;
            PublishGraphicsFrame();
            OnPropertyChanged(nameof(IsScreenActive));
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
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _framebuffer = null;
            GraphicsFrame = null;
            GraphicsText = string.Empty;
            _graphicsVisualRefreshPending = false;
            OnPropertyChanged(nameof(IsScreenActive));
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
            _framebuffer?.Clear();
            PublishGraphicsFrame();
            _graphicsVisualRefreshPending = false;
            _graphicsRow = 1;
            _graphicsColumn = 1;
        });
    }

    public void SetTextColor(int foreground, int? background)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsForeground = foreground;
            _graphicsBackground = background;
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

        InvokeOnDispatcher(() =>
        {
            _pendingKeys.Enqueue(keyText);
            if (_isScreenActive)
            {
                WriteGraphicsEvent(
                    "screen.key.enqueued",
                    $"key=\"{SanitizeForLog(keyText)}\" | pendingKeys={_pendingKeys.Count}");
            }
        });
    }

    public void SetKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        InvokeOnDispatcher(() => _pressedKeys.Add(KeyboardInputNormalizer.Normalize(keyText)));
    }

    public void SetKeyUp(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        InvokeOnDispatcher(() => _pressedKeys.Remove(KeyboardInputNormalizer.Normalize(keyText)));
    }

    public string ReadKey()
    {
        var keyText = string.Empty;
        InvokeOnDispatcher(() =>
        {
            keyText = _pendingKeys.Count > 0 ? _pendingKeys.Dequeue() : string.Empty;
            if (_isScreenActive && !string.IsNullOrEmpty(keyText))
            {
                WriteGraphicsEvent(
                    "screen.key.read",
                    $"key=\"{SanitizeForLog(keyText)}\" | pendingKeys={_pendingKeys.Count}");
            }
        }, DispatcherPriority.Background);

        return keyText;
    }

    public bool IsKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return false;
        }

        var isDown = false;
        var normalizedKey = KeyboardInputNormalizer.Normalize(keyText);
        InvokeOnDispatcher(() => isDown = _pressedKeys.Contains(normalizedKey), DispatcherPriority.Background);
        return isDown;
    }

    public void ClearKeyState()
    {
        InvokeOnDispatcher(_pressedKeys.Clear);
    }

    public void SetMousePosition(long x, long y)
    {
        InvokeOnDispatcher(() =>
        {
            _mouseX = x;
            _mouseY = y;
        });
    }

    public void SetMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        InvokeOnDispatcher(() => _pressedMouseButtons.Add(buttonNumber));
    }

    public void EnqueueMouseButtonPress(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        InvokeOnDispatcher(() => EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
    }

    public void EnqueueMouseButtonRelease(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        InvokeOnDispatcher(() => EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber)));
    }

    public void EnqueueMouseMove()
    {
        InvokeOnDispatcher(() => EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Move, 0)));
    }

    public void EnqueueMouseWheel(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        InvokeOnDispatcher(() => EnqueueMouseEventCore(new MouseInputEvent(MouseInputEventKind.Wheel, delta)));
    }

    public void SetMouseButtonUp(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        InvokeOnDispatcher(() => _pressedMouseButtons.Remove(buttonNumber));
    }

    public long GetMouseX()
    {
        var mouseX = -1L;
        InvokeOnDispatcher(() => mouseX = _mouseX, DispatcherPriority.Background);
        return mouseX;
    }

    public long GetMouseY()
    {
        var mouseY = -1L;
        InvokeOnDispatcher(() => mouseY = _mouseY, DispatcherPriority.Background);
        return mouseY;
    }

    public bool IsMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return false;
        }

        var isDown = false;
        InvokeOnDispatcher(() => isDown = _pressedMouseButtons.Contains(buttonNumber), DispatcherPriority.Background);
        return isDown;
    }

    public int ReadMouseButtonPress()
    {
        var buttonNumber = 0;
        InvokeOnDispatcher(() =>
        {
            var deferredEvents = new List<MouseInputEvent>();

            while (_pendingMouseEvents.Count > 0)
            {
                var candidate = _pendingMouseEvents.Dequeue();
                if (buttonNumber == 0 && candidate.Kind == MouseInputEventKind.Press)
                {
                    buttonNumber = candidate.ButtonNumber;
                    continue;
                }

                deferredEvents.Add(candidate);
            }

            foreach (var deferredEvent in deferredEvents)
            {
                _pendingMouseEvents.Enqueue(deferredEvent);
            }
        }, DispatcherPriority.Background);

        return buttonNumber;
    }

    public MouseInputEvent ReadMouseEvent()
    {
        var mouseEvent = default(MouseInputEvent);
        InvokeOnDispatcher(() =>
        {
            mouseEvent = _pendingMouseEvents.Count > 0 ? _pendingMouseEvents.Dequeue() : default;
            if (_isScreenActive && mouseEvent != default)
            {
                WriteGraphicsEvent(
                    "screen.mouse.read",
                    $"{FormatMouseEvent(mouseEvent)} | pendingMouseEvents={_pendingMouseEvents.Count}");
            }
        }, DispatcherPriority.Background);

        return mouseEvent;
    }

    public void ClearMouseState()
    {
        InvokeOnDispatcher(() =>
        {
            _mouseX = -1;
            _mouseY = -1;
            _pendingMouseEvents.Clear();
            _pressedMouseButtons.Clear();
        });
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
        AppendTranscriptLine(CurrentPromptText + CurrentInputText);
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
        _pendingKeys.Clear();
        _pendingMouseEvents.Clear();
        _pressedKeys.Clear();
        _pressedMouseButtons.Clear();
        _mouseX = -1;
        _mouseY = -1;
        _isScreenActive = false;
        _screenMode = 0;
        _screenWidth = 0;
        _screenHeight = 0;
        _framebuffer = null;
        GraphicsFrame = null;
        GraphicsText = string.Empty;
        _graphicsVisualRefreshPending = false;
        OutputText = string.Empty;
        ResetInputState();
        OnPropertyChanged(nameof(IsScreenActive));
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
        _pendingMouseEvents.Enqueue(mouseEvent);
        if (_isScreenActive)
        {
            WriteGraphicsEvent(
                "screen.mouse.enqueued",
                $"{FormatMouseEvent(mouseEvent)} | pendingMouseEvents={_pendingMouseEvents.Count}");
        }
    }

    private static string FormatMouseEvent(MouseInputEvent mouseEvent) =>
        $"kind={mouseEvent.Kind} | value={mouseEvent.ButtonNumber}";

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
    }

    private void WriteGraphicsTextCore(string text, bool newLine)
    {
        var lines = GraphicsText.Length == 0
            ? new List<string>()
            : GraphicsText.Split(Environment.NewLine).ToList();

        EnsureGraphicsLine(lines, _graphicsRow);
        var lineIndex = _graphicsRow - 1;
        var columnIndex = Math.Max(0, _graphicsColumn - 1);
        var line = lines[lineIndex];

        lines[lineIndex] = OverwriteText(line, columnIndex, text);

        if (newLine)
        {
            _graphicsRow++;
            _graphicsColumn = 1;
            EnsureGraphicsLine(lines, _graphicsRow);
        }
        else
        {
            _graphicsColumn += text.Length;
        }

        GraphicsText = string.Join(Environment.NewLine, lines);
    }

    private static void EnsureGraphicsLine(List<string> lines, int row)
    {
        while (lines.Count < row)
        {
            lines.Add(string.Empty);
        }
    }

    private static string OverwriteText(string line, int columnIndex, string text)
    {
        if (line.Length < columnIndex)
        {
            line = line.PadRight(columnIndex);
        }

        var prefix = line[..columnIndex];
        var suffixStart = columnIndex + text.Length;
        var suffix = suffixStart < line.Length ? line[suffixStart..] : string.Empty;
        return prefix + text + suffix;
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
        _isScreenActive &&
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
        PublishGraphicsFrame();
    }

    private void PublishGraphicsFrame()
    {
        var frame = _framebuffer?.CreateSnapshot(
            GraphicsText,
            _graphicsForeground,
            _graphicsBackground,
            _isWaitingForInput ? CurrentPromptText + CurrentInputText : null);
        GraphicsFrame = frame;
        _graphicsWindow?.PresentFrame(frame);
    }

    private void ScheduleGraphicsVisualRefresh()
    {
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
        dispatcher.BeginInvoke(() =>
        {
            _graphicsVisualRefreshPending = false;
            RefreshGraphicsVisuals();
        }, DispatcherPriority.Render);
    }

    private void AddFillCore(long left, long top, long right, long bottom, int color)
    {
        if (!_isScreenActive)
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
