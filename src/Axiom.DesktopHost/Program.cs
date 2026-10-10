using System.Text;
using System.Windows;

namespace Axiom.DesktopHost;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        string? framePipe = args.Length == 2 && args[0] == "--frame-pipe" ? args[1] : null;
        if (args.Length != 0 && framePipe is null) { Console.Error.WriteLine("Ожидалось --frame-pipe <имя>."); return 2; }
        using var host = new DesktopHost(application, framePipe);
        application.Startup += (_, _) => host.Start();
        application.DispatcherUnhandledException += (_, e) =>
        {
            Console.Error.WriteLine(e.Exception);
            e.Handled = true;
            host.Shutdown();
        };
        return application.Run();
    }
}
