namespace Axiom.Ide.Services.EventLog;

public sealed class NullIdeEventLogViewerService : IIdeEventLogViewerService
{
    public static NullIdeEventLogViewerService Instance { get; } = new();

    private NullIdeEventLogViewerService()
    {
    }

    public void Show(string logFilePath)
    {
    }
}
