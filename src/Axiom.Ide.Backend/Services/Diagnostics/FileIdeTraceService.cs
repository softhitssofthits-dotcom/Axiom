using System.IO;
using System.Text;

namespace Axiom.Ide.Services.Diagnostics;

public sealed class FileIdeTraceService : IIdeTraceService
{
    private readonly object _syncRoot = new();
    private readonly string _logsDirectoryPath;
    private string? _sessionLogPath;

    public FileIdeTraceService(string? logsDirectoryPath = null)
    {
        _logsDirectoryPath = string.IsNullOrWhiteSpace(logsDirectoryPath)
            ? Path.Combine(AppContext.BaseDirectory, "Logs")
            : Path.GetFullPath(logsDirectoryPath);
    }

    public string LogsDirectoryPath => _logsDirectoryPath;

    public string CurrentSessionLogPath
    {
        get
        {
            EnsureSessionLogPath();
            return _sessionLogPath!;
        }
    }

    public void BeginSession(string sessionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionName);

        EnsureSessionLogPath();
        AppendLine(string.Empty);
        AppendLine($"===== {sessionName} session {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} =====");
        AppendLine($"Log file: {CurrentSessionLogPath}");
    }

    public void Write(string category, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(message);

        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff zzz");
        var lines = message.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        if (lines.Length == 0)
        {
            AppendLine($"[{timestamp}] {category}");
            return;
        }

        AppendLine($"[{timestamp}] {category} | {Sanitize(lines[0])}");

        for (var index = 1; index < lines.Length; index++)
        {
            AppendLine($"    {Sanitize(lines[index])}");
        }
    }

    public void WriteException(string category, Exception exception, string? note = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(note))
        {
            builder.AppendLine(note);
        }

        builder.Append(exception);
        Write(category, builder.ToString());
    }

    private void AppendLine(string text)
    {
        lock (_syncRoot)
        {
            EnsureSessionLogPath();
            File.AppendAllText(_sessionLogPath!, text + Environment.NewLine, Encoding.UTF8);
        }
    }

    private void EnsureSessionLogPath()
    {
        if (!string.IsNullOrWhiteSpace(_sessionLogPath))
        {
            return;
        }

        Directory.CreateDirectory(_logsDirectoryPath);
        _sessionLogPath = Path.Combine(
            _logsDirectoryPath,
            $"ide-session-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    private static string Sanitize(string text)
    {
        return text
            .Replace('\t', ' ')
            .TrimEnd();
    }
}
