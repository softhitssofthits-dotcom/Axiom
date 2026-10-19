using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;
using System.Text;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Управляет консольным выводом (transcript): текст, контрольные точки, очистка.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class ConsoleOutputService : ViewModelBase
{
    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly StringBuilder _outputBuilder = new();
    private readonly List<int> _lineEnds = [];
    private string? _outputSnapshot;

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
        get => _outputSnapshot ??= _outputBuilder.ToString();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _outputBuilder.Clear();
            _lineEnds.Clear();
            _outputBuilder.Append(value);
            RebuildLineEnds(value);
            _outputSnapshot = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Контрольная точка вывода (количество строк).
    /// </summary>
    public int OutputCheckpoint => _lineEnds.Count;

    /// <summary>
    /// Добавить строку в консольный вывод.
    /// </summary>
    public void AppendTranscriptLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var parts = line.Split(Environment.NewLine, StringSplitOptions.None);
        foreach (var part in parts)
        {
            if (_lineEnds.Count > 0) _outputBuilder.Append(Environment.NewLine);
            _outputBuilder.Append(part);
            _lineEnds.Add(_outputBuilder.Length);
        }
        _outputSnapshot = null;
        OnPropertyChanged(nameof(OutputText));

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
        if (_lineEnds.Count == 0)
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

        if (checkpoint > _lineEnds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
        }

        var targetLength = checkpoint == 0 ? 0 : _lineEnds[checkpoint - 1];
        _outputBuilder.Length = targetLength;
        _lineEnds.RemoveRange(checkpoint, _lineEnds.Count - checkpoint);
        _outputSnapshot = null;
        OnPropertyChanged(nameof(OutputText));

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

    private void RebuildLineEnds(string value)
    {
        if (value.Length == 0) return;

        var start = 0;
        while (start <= value.Length)
        {
            var separator = value.IndexOf(Environment.NewLine, start, StringComparison.Ordinal);
            if (separator < 0)
            {
                _lineEnds.Add(value.Length);
                return;
            }

            _lineEnds.Add(separator);
            start = separator + Environment.NewLine.Length;
            if (start == value.Length)
            {
                _lineEnds.Add(value.Length);
                return;
            }
        }
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
