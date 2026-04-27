using System.Windows;
using System.Windows.Threading;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Services.Input;

public sealed class ProgramConsoleService : ViewModelBase, IProgramConsoleService
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private DispatcherFrame? _pendingFrame;
    private string _currentInputText = string.Empty;
    private string _currentPromptText = string.Empty;
    private bool _isWaitingForInput;
    private string _outputText = string.Empty;
    private string _submittedText = string.Empty;

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

    private void InvokeOnDispatcher(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
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
}
