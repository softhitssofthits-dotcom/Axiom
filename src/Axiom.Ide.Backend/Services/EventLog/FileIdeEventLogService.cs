using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Axiom.Ide.Services.EventLog;

public sealed class FileIdeEventLogService : IIdeEventLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly object _syncRoot = new();
    private readonly IdeEventLogOptions _options;
    private readonly Channel<QueuedItem> _channel;
    private readonly CancellationTokenSource _writerCts = new();
    private Task? _writerTask;
    private StreamWriter? _writer;
    private string _sessionFilePrefix = string.Empty;
    private int _rotationIndex;
    private long _sequence;
    private long _droppedEvents;
    private bool _sessionStarted;
    private bool _disposed;

    public FileIdeEventLogService(IdeEventLogOptions? options = null)
    {
        _options = options ?? new IdeEventLogOptions();
        LogsDirectoryPath = ResolveLogsDirectoryPath(_options.LogsDirectoryPath);

        var boundedOptions = new BoundedChannelOptions(Math.Max(16, _options.QueueCapacity))
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<QueuedItem>(boundedOptions);
    }

    public string LogsDirectoryPath { get; }

    public string CurrentSessionLogPath { get; private set; } = string.Empty;

    public string SessionId { get; private set; } = string.Empty;

    public void BeginSession(string sessionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionName);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            EnsureStartedCore(sessionName.Trim());
        }
    }

    public void Write(IdeEventLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            if (!_sessionStarted)
            {
                EnsureStartedCore("Axiom IDE");
            }
        }

        var normalized = Normalize(entry);
        if (!_channel.Writer.TryWrite(new QueuedItem(normalized)))
        {
            Interlocked.Increment(ref _droppedEvents);
        }
    }

    public void Flush()
    {
        lock (_syncRoot)
        {
            if (_disposed || !_sessionStarted || _writerTask is null)
            {
                return;
            }
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_channel.Writer.TryWrite(new QueuedItem(completion)))
        {
            completion.TrySetResult(false);
        }

        completion.Task.GetAwaiter().GetResult();
    }

    public int DeleteInactiveLogs()
    {
        Flush();

        try
        {
            var directory = new DirectoryInfo(LogsDirectoryPath);
            if (!directory.Exists)
            {
                return 0;
            }

            var currentPath = string.IsNullOrWhiteSpace(CurrentSessionLogPath)
                ? null
                : Path.GetFullPath(CurrentSessionLogPath);
            var deleted = 0;

            foreach (var file in directory.GetFiles("ide-events-*.jsonl"))
            {
                if (currentPath is not null
                    && string.Equals(file.FullName, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryDeleteFile(file))
                {
                    deleted++;
                }
            }

            return deleted;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _channel.Writer.TryComplete();

        try
        {
            _writerTask?.GetAwaiter().GetResult();
        }
        catch
        {
            // Нельзя ронять IDE из-за ошибок журнала.
        }

        _writerCts.Cancel();
        _writerCts.Dispose();
        _writer?.Dispose();
        _writer = null;
    }

    private IdeEventLogEntry Normalize(IdeEventLogEntry entry)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = nowUtc.ToLocalTime();
        var preview = NormalizePreview(entry.TextPreview);

        return new IdeEventLogEntry
        {
            SchemaVersion = entry.SchemaVersion <= 0 ? 1 : entry.SchemaVersion,
            SessionId = string.IsNullOrWhiteSpace(entry.SessionId) ? SessionId : entry.SessionId,
            Sequence = entry.Sequence > 0 ? entry.Sequence : Interlocked.Increment(ref _sequence),
            TimestampUtc = entry.TimestampUtc == default ? nowUtc : entry.TimestampUtc,
            TimestampLocal = entry.TimestampLocal == default ? nowLocal : entry.TimestampLocal,
            Level = entry.Level,
            Category = string.IsNullOrWhiteSpace(entry.Category) ? "general" : entry.Category.Trim(),
            EventName = string.IsNullOrWhiteSpace(entry.EventName) ? "event" : entry.EventName.Trim(),
            Source = string.IsNullOrWhiteSpace(entry.Source) ? "ide" : entry.Source.Trim(),
            ThreadId = entry.ThreadId == 0 ? Environment.CurrentManagedThreadId : entry.ThreadId,
            DocumentPath = string.IsNullOrWhiteSpace(entry.DocumentPath) ? null : Path.GetFullPath(entry.DocumentPath),
            DocumentName = string.IsNullOrWhiteSpace(entry.DocumentName) ? null : entry.DocumentName,
            Line = entry.Line,
            Column = entry.Column,
            CommandName = entry.CommandName,
            MenuName = entry.MenuName,
            DebugLine = entry.DebugLine,
            Opcode = entry.Opcode,
            CallDepth = entry.CallDepth,
            CanStepBack = entry.CanStepBack,
            OutputLength = entry.OutputLength,
            TextLength = entry.TextLength,
            TextPreview = preview,
            StatusBar = entry.StatusBar,
            Properties = entry.Properties
        };
    }

    private void EnsureStartedCore(string sessionName)
    {
        if (_sessionStarted)
        {
            return;
        }

        Directory.CreateDirectory(LogsDirectoryPath);
        CleanupLogsCore();

        SessionId = Guid.NewGuid().ToString("N");
        _sessionFilePrefix = $"ide-events-{DateTime.Now:yyyyMMdd-HHmmss}";
        _rotationIndex = 0;
        CurrentSessionLogPath = BuildSessionFilePath(_rotationIndex);
        _writer = CreateWriter(CurrentSessionLogPath);
        _writerTask = Task.Run(WriteLoopAsync);
        _sessionStarted = true;

        _channel.Writer.TryWrite(new QueuedItem(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "app",
            EventName = "session-begin",
            Source = "event-log",
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["sessionName"] = sessionName,
                ["logPath"] = CurrentSessionLogPath
            }
        }));
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var queued in _channel.Reader.ReadAllAsync(_writerCts.Token))
            {
                if (queued.FlushCompletion is not null)
                {
                    await FlushWriterCoreAsync().ConfigureAwait(false);
                    queued.FlushCompletion.TrySetResult(true);
                    continue;
                }

                if (queued.Entry is null)
                {
                    continue;
                }

                await WriteDroppedEventsCoreAsync().ConfigureAwait(false);
                await EnsureRotationCoreAsync().ConfigureAwait(false);

                var json = JsonSerializer.Serialize(queued.Entry, JsonOptions);
                await _writer!.WriteLineAsync(json).ConfigureAwait(false);
            }

            await FlushWriterCoreAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ошибки файловой записи не должны ронять IDE.
        }
    }

    private async Task EnsureRotationCoreAsync()
    {
        if (_writer is null)
        {
            return;
        }

        await _writer.FlushAsync().ConfigureAwait(false);

        try
        {
            var fileInfo = new FileInfo(CurrentSessionLogPath);
            if (!fileInfo.Exists || fileInfo.Length < _options.MaxFileBytes)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        _rotationIndex++;
        CurrentSessionLogPath = BuildSessionFilePath(_rotationIndex);
        _writer.Dispose();
        _writer = CreateWriter(CurrentSessionLogPath);
    }

    private async Task WriteDroppedEventsCoreAsync()
    {
        var dropped = Interlocked.Exchange(ref _droppedEvents, 0);
        if (dropped <= 0 || _writer is null)
        {
            return;
        }

        var droppedEntry = Normalize(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Warning,
            Category = "event-log",
            EventName = "events.dropped",
            Source = "event-log",
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["count"] = dropped.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        });

        var json = JsonSerializer.Serialize(droppedEntry, JsonOptions);
        await _writer.WriteLineAsync(json).ConfigureAwait(false);
    }

    private async Task FlushWriterCoreAsync()
    {
        if (_writer is null)
        {
            return;
        }

        await _writer.FlushAsync().ConfigureAwait(false);
    }

    private void CleanupLogsCore()
    {
        var directory = new DirectoryInfo(LogsDirectoryPath);
        if (!directory.Exists)
        {
            return;
        }

        var files = directory.GetFiles("ide-events-*.jsonl")
            .OrderBy(file => file.CreationTimeUtc)
            .ToList();

        if (files.Count == 0)
        {
            return;
        }

        var keepCount = Math.Max(0, _options.AlwaysKeepLatestSessions);
        var keepSet = files
            .TakeLast(keepCount)
            .Select(file => file.FullName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        foreach (var file in files)
        {
            if (keepSet.Contains(file.FullName))
            {
                continue;
            }

            var age = now - file.CreationTimeUtc;
            if (age.TotalDays <= _options.RetentionDays)
            {
                continue;
            }

            _ = TryDeleteFile(file);
        }

        files = directory.GetFiles("ide-events-*.jsonl")
            .OrderBy(file => file.CreationTimeUtc)
            .ToList();

        long totalBytes = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (totalBytes <= _options.MaxDirectoryBytes)
            {
                break;
            }

            if (keepSet.Contains(file.FullName))
            {
                continue;
            }

            totalBytes -= file.Length;
            _ = TryDeleteFile(file);
        }
    }

    private static bool TryDeleteFile(FileInfo file)
    {
        try
        {
            file.Delete();
            return true;
        }
        catch
        {
            // Ошибки очистки игнорируются.
            return false;
        }
    }

    private string BuildSessionFilePath(int rotationIndex)
    {
        return rotationIndex <= 0
            ? Path.Combine(LogsDirectoryPath, $"{_sessionFilePrefix}.jsonl")
            : Path.Combine(LogsDirectoryPath, $"{_sessionFilePrefix}-part{rotationIndex:D2}.jsonl");
    }

    private static StreamWriter CreateWriter(string path)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        return new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ResolveLogsDirectoryPath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                return Path.Combine(localAppData, "Axiom", "Logs", "Events");
            }
        }
        catch
        {
            // Fallback ниже.
        }

        return Path.Combine(AppContext.BaseDirectory, "Logs", "Events");
    }

    private static string? NormalizePreview(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview))
        {
            return null;
        }

        var normalized = preview
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();

        return normalized.Length <= 240
            ? normalized
            : normalized[..240];
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private readonly struct QueuedItem
    {
        public QueuedItem(IdeEventLogEntry entry)
        {
            Entry = entry;
            FlushCompletion = null;
        }

        public QueuedItem(TaskCompletionSource<bool> flushCompletion)
        {
            Entry = null;
            FlushCompletion = flushCompletion;
        }

        public IdeEventLogEntry? Entry { get; }

        public TaskCompletionSource<bool>? FlushCompletion { get; }
    }
}
