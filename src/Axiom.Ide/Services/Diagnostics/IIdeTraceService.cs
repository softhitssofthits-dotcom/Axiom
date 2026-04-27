namespace Axiom.Ide.Services.Diagnostics;

public interface IIdeTraceService
{
    string LogsDirectoryPath { get; }

    string CurrentSessionLogPath { get; }

    void BeginSession(string sessionName);

    void Write(string category, string message);

    void WriteException(string category, Exception exception, string? note = null);
}
