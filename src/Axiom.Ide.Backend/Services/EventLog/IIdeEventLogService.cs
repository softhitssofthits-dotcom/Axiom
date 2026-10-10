namespace Axiom.Ide.Services.EventLog;

public interface IIdeEventLogService : IDisposable
{
    string LogsDirectoryPath { get; }

    string CurrentSessionLogPath { get; }

    string SessionId { get; }

    void BeginSession(string sessionName);

    void Write(IdeEventLogEntry entry);

    void Flush();

    int DeleteInactiveLogs();
}
