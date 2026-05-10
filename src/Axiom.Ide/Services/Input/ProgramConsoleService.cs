using System.Windows;
using System.Windows.Threading;
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
        set => SetProperty(ref _currentInputText, value);
    }

    public string CurrentPromptText
    {
        get => _currentPromptText;
        private set => SetProperty(ref _currentPromptText, value);
    }

    public bool IsWaitingForInput
    {
        get => _isWaitingForInput;
        private set => SetProperty(ref _isWaitingForInput, value);
    }

    public string OutputText
    {
        get => _outputText;
        private set => SetProperty(ref _outputText, value);
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

    public void WriteOutput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() => AppendTranscriptLine(text));
    }

    public void EnqueueKey(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        InvokeOnDispatcher(() => _pendingKeys.Enqueue(keyText));
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

        InvokeOnDispatcher(() => _pendingMouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
    }

    public void EnqueueMouseButtonRelease(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        InvokeOnDispatcher(() => _pendingMouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber)));
    }

    public void EnqueueMouseMove()
    {
        InvokeOnDispatcher(() => _pendingMouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Move, 0)));
    }

    public void EnqueueMouseWheel(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        InvokeOnDispatcher(() => _pendingMouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Wheel, delta)));
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
        OutputText = string.Empty;
        ResetInputState();
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

    private static bool IsSupportedMouseButton(int buttonNumber) => buttonNumber is >= 1 and <= 3;
}
