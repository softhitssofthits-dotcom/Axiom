namespace Axiom.Ide.Services.EventLog;

public sealed class NullIdeEventLogService : IIdeEventLogService
{
    public static NullIdeEventLogService Instance { get; } = new();

    private NullIdeEventLogService()
    {
    }

    public string LogsDirectoryPath => string.Empty;

    public string CurrentSessionLogPath => string.Empty;

    public string SessionId => string.Empty;

    public void BeginSession(string sessionName)
    {
    }

    public void Write(IdeEventLogEntry entry)
    {
    }

    public void Flush()
    {
    }

    public int DeleteInactiveLogs() => 0;

    public void Dispose()
    {
    }
}
