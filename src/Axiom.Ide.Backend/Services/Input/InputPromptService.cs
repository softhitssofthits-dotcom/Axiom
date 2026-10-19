using System.Windows;
using System.Windows.Threading;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Управляет вводом пользователя (INPUT prompt): запрос, подтверждение, ввод символов.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class InputPromptService : ViewModelBase
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private DispatcherFrame? _pendingFrame;
    private string _submittedText = string.Empty;
    private string _currentInputText = string.Empty;
    private string _currentPromptText = string.Empty;
    private bool _isWaitingForInput;

    public InputPromptService(
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
        set => SetProperty(ref _currentPromptText, value);
    }

    public bool IsWaitingForInput
    {
        get => _isWaitingForInput;
        set => SetProperty(ref _isWaitingForInput, value);
    }

    /// <summary>
    /// Запросить ввод от пользователя. Блокирует вызывающий поток до подтверждения.
    /// </summary>
    /// <param name="request">Запрос ввода.</param>
    /// <param name="onSubmit">Коллбэк при подтверждении: (promptText, inputText, isScreenOutput).</param>
    /// <param name="isScreenOutputActive">Активен ли экран.</param>
    /// <returns>Введённый текст.</returns>
    public string RequestInput(
        ProgramInputRequest request,
        Action<string, string, bool>? onSubmit,
        bool isScreenOutputActive,
        Action? onWaitingChanged = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (!dispatcher.CheckAccess())
        {
            return dispatcher.Invoke(() => RequestInput(request, onSubmit, isScreenOutputActive, onWaitingChanged));
        }

        if (_pendingFrame is not null)
        {
            throw new InvalidOperationException("Concurrent INPUT requests are not supported.");
        }

        _submittedText = string.Empty;
        CurrentPromptText = CreateConsolePrompt(request);
        CurrentInputText = string.Empty;
        IsWaitingForInput = true;
        onWaitingChanged?.Invoke();
        _traceService.Write(
            "console.input.request",
            $"request={request.RequestNumber} | values={request.ValueCount} | prompt=\"{SanitizeForLog(CurrentPromptText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "input",
            EventName = "input.requested",
            Source = nameof(InputPromptService),
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

    public void SubmitInput(Action<string, string, bool>? onSubmit, bool isScreenOutputActive)
    {
        if (_pendingFrame is null)
        {
            return;
        }

        // Значения захватываются до сброса IsWaitingForInput: подписчики этого
        // свойства синхронно очищают CurrentInputText в нижней панели.
        var promptText = CurrentPromptText;
        var inputText = CurrentInputText;
        _submittedText = inputText;
        IsWaitingForInput = false;
        onSubmit?.Invoke(promptText, inputText, isScreenOutputActive);

        _traceService.Write(
            "console.input.submit",
            $"text=\"{SanitizeForLog(inputText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "input",
            EventName = "input.submitted",
            Source = nameof(InputPromptService),
            TextLength = inputText.Length,
            TextPreview = inputText
        });
        _pendingFrame.Continue = false;
        _pendingFrame = null;
    }

    public void AppendInputCharacter(char character)
    {
        if (!_isWaitingForInput || char.IsControl(character))
        {
            return;
        }

        CurrentInputText += character;
    }

    public void BackspaceInputCharacter()
    {
        if (!_isWaitingForInput || CurrentInputText.Length == 0)
        {
            return;
        }

        var elements = System.Globalization.StringInfo.ParseCombiningCharacters(CurrentInputText);
        CurrentInputText = CurrentInputText[..elements[^1]];
    }

    public void ResetInputState()
    {
        IsWaitingForInput = false;
        CurrentPromptText = string.Empty;
        CurrentInputText = string.Empty;
    }

    public void CancelPendingFrame()
    {
        if (_pendingFrame is not null)
        {
            _pendingFrame.Continue = false;
            _pendingFrame = null;
        }
    }

    private static string CreateConsolePrompt(ProgramInputRequest request)
    {
        // Компилятор уже сформировал приглашение по разделителю INPUT.
        // Пустое приглашение и пробелы сохраняются буквально.
        return request.PromptText ?? "? ";
    }

    private static string SanitizeForLog(string text)
    {
        return text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();
    }
}
