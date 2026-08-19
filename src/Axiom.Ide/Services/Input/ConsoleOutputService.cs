using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Управляет консольным выводом (transcript): текст, контрольные точки, очистка.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class ConsoleOutputService : ViewModelBase
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private string _outputText = string.Empty;

    public ConsoleOutputService(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null)
    {
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
    }

    /// <summary>
    /// Текст консольного вывода (transcript).
    /// </summary>
    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    /// <summary>
    /// Контрольная точка вывода (количество строк).
    /// </summary>
    public int OutputCheckpoint =>
        string.IsNullOrEmpty(OutputText)
            ? 0
            : OutputText.Split(Environment.NewLine).Length;

    /// <summary>
    /// Добавить строку в консольный вывод.
    /// </summary>
    public void AppendTranscriptLine(string line)
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
            Source = nameof(ConsoleOutputService),
            TextLength = line.Length,
            TextPreview = line,
            OutputLength = OutputCheckpoint
        });
    }

    /// <summary>
    /// Очистить консольный вывод.
    /// </summary>
    public void ClearOutput()
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
            Source = nameof(ConsoleOutputService),
            OutputLength = 0
        });
    }

    /// <summary>
    /// Восстановить вывод до контрольной точки.
    /// </summary>
    public void RestoreOutputCheckpoint(int checkpoint)
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
            Source = nameof(ConsoleOutputService),
            OutputLength = checkpoint
        });
    }

    /// <summary>
    /// Сбросить вывод.
    /// </summary>
    public void Reset()
    {
        OutputText = string.Empty;
    }

    private static string SanitizeForLog(string text)
    {
        if (text.Length <= 200)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, 200), "...");
    }
}
