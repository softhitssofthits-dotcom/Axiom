namespace Axiom.Ide.Services.Diagnostics;

public sealed class NullIdeTraceService : IIdeTraceService
{
    public static NullIdeTraceService Instance { get; } = new();

    private NullIdeTraceService()
    {
    }

    public string LogsDirectoryPath => string.Empty;

    public string CurrentSessionLogPath => string.Empty;

    public void BeginSession(string sessionName)
    {
    }

    public void Write(string category, string message)
    {
    }

    public void WriteException(string category, Exception exception, string? note = null)
    {
    }
}
