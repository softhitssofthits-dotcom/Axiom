using System.Windows.Threading;
using System.Windows;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Shell;

namespace Axiom.Ide;

public partial class App : Application
{
    private readonly object _exceptionSync = new();
    private bool _isShowingException;
    private IIdeTraceService _traceService = NullIdeTraceService.Instance;
    private IIdeEventLogService _eventLogService = NullIdeEventLogService.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        base.OnStartup(e);

        var compositionRoot = new IdeShellCompositionRoot();
        _traceService = compositionRoot.TraceService;
        _eventLogService = compositionRoot.EventLogService;
        _traceService.BeginSession("Axiom IDE");
        _eventLogService.BeginSession("Axiom IDE");
        _traceService.Write("app.startup", "Application startup completed.");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "app",
            EventName = "startup.completed",
            Source = nameof(App)
        });
        var window = compositionRoot.CreateMainWindow();

        MainWindow = window;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowUnhandledException(e.Exception);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ShowUnhandledException(exception);
        }
    }

    private void ShowUnhandledException(Exception exception)
    {
        lock (_exceptionSync)
        {
            if (_isShowingException)
            {
                return;
            }

            _isShowingException = true;
        }

        try
        {
            _traceService.WriteException("app.unhandled", exception, "Unhandled IDE exception.");
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Error,
                Category = "app",
                EventName = "exception.unhandled",
                Source = nameof(App),
                TextPreview = exception.Message,
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["exceptionType"] = exception.GetType().FullName
                }
            });

            var message = IdeLanguageResourceService.GetCurrentFormattedString(
                "App.Error.UnhandledMessage",
                "Unhandled IDE error.{0}{0}{1}{0}{0}Log: {2}",
                Environment.NewLine,
                exception.Message,
                _traceService.CurrentSessionLogPath);
            var title = IdeLanguageResourceService.GetCurrentString("App.Error.Title", "Axiom IDE Error");

            if (Current?.MainWindow is Window owner && owner.IsLoaded)
            {
                owner.Dispatcher.Invoke(() => MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch
        {
        }
        finally
        {
            lock (_exceptionSync)
            {
                _isShowingException = false;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _eventLogService.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Info,
                Category = "app",
                EventName = "shutdown",
                Source = nameof(App),
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["exitCode"] = e.ApplicationExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }
            });
            _eventLogService.Flush();
        }
        catch
        {
        }
        finally
        {
            _eventLogService.Dispose();
        }

        base.OnExit(e);
    }
}
