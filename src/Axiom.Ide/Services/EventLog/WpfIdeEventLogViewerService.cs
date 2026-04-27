using System.IO;
using System.Windows;

namespace Axiom.Ide.Services.EventLog;

public sealed class WpfIdeEventLogViewerService : IIdeEventLogViewerService
{
    public void Show(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            return;
        }

        var normalizedPath = Path.GetFullPath(logFilePath);
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null)
        {
            return;
        }

        void ShowCore()
        {
            var window = new IdeEventLogViewerWindow(normalizedPath)
            {
                Owner = Application.Current?.MainWindow
            };

            window.Show();
            window.Activate();
        }

        if (dispatcher.CheckAccess())
        {
            ShowCore();
            return;
        }

        dispatcher.Invoke(ShowCore);
    }
}
