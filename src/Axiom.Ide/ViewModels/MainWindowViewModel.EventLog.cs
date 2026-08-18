using System.IO;
using Axiom.Ide.Services.EventLog;

namespace Axiom.Ide.ViewModels;

public sealed partial class MainWindowViewModel
{
    private bool CanUseIdeEventLogCommands() =>
        !IsShellInteractionLocked() && !string.IsNullOrWhiteSpace(_eventLogService.LogsDirectoryPath);

    private void ShowIdeEventLogViewer()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var logPath = _eventLogService.CurrentSessionLogPath;
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusCurrentMissing", "Current IDE event log is not available.");
                return;
            }

            _eventLogViewerService.Show(logPath);
            StatusBar.StatusText = GetLocalizedString("EventLog.Viewer.StatusOpened", "IDE event log viewer opened.");
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.viewer-opened",
                commandName: "ShowIdeEventLogViewer",
                textPreview: logPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.Viewer.StatusOpenFailed", "Could not open IDE event log viewer.");
        }
    }

    private void OpenIdeEventLogFile()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var logPath = _eventLogService.CurrentSessionLogPath;
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusCurrentMissing", "Current IDE event log is not available.");
                return;
            }

            _externalNavigationService.OpenPath(logPath);
            StatusBar.StatusText = GetLocalizedFormattedString("EventLog.StatusFileOpened", "IDE event log opened: {0}.", logPath);
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.file-opened",
                commandName: "OpenIdeEventLogFile",
                textPreview: logPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.StatusOpenFailed", "Could not open IDE event log.");
        }
    }

    private void OpenIdeEventLogFolder()
    {
        try
        {
            _eventLogService.BeginSession("Axiom IDE");
            _eventLogService.Flush();

            var directoryPath = _eventLogService.LogsDirectoryPath;
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                StatusBar.StatusText = GetLocalizedString("EventLog.StatusFolderMissing", "IDE event log folder is not available.");
                return;
            }

            Directory.CreateDirectory(directoryPath);
            _externalNavigationService.OpenPath(directoryPath);
            StatusBar.StatusText = GetLocalizedFormattedString("EventLog.StatusFolderOpened", "IDE event log folder opened: {0}.", directoryPath);
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "event-log.folder-opened",
                commandName: "OpenIdeEventLogFolder",
                textPreview: directoryPath);
        }
        catch (Exception exception)
        {
            _promptService.ShowError(exception.Message, GetLocalizedString("EventLog.ErrorTitle", "IDE Event Log"));
            StatusBar.StatusText = GetLocalizedString("EventLog.StatusOpenFolderFailed", "Could not open IDE event log folder.");
        }
    }

    private void ClearIdeEventLogs()
    {
        var title = GetLocalizedString("EventLog.Clear.Title", "Clear IDE Event Logs");
        var confirmed = _promptService.ShowConfirmation(
            GetLocalizedString(
                "EventLog.Clear.Confirm",
                "Delete inactive IDE event log files? The current session log will be kept."),
            title);

        if (!confirmed)
        {
            StatusBar.StatusText = GetLocalizedString("EventLog.Clear.Cancelled", "IDE event log cleanup cancelled.");
            return;
        }

        var deleted = _eventLogService.DeleteInactiveLogs();
        StatusBar.StatusText = GetLocalizedFormattedString("EventLog.Clear.Completed", "Deleted IDE event logs: {0}.", deleted);
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "event-log.cleaned",
            commandName: "ClearIdeEventLogs",
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["deleted"] = deleted.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }
}
