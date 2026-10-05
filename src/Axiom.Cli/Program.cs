using System.Text;

namespace Axiom.Cli;

/// <summary>
/// Тонкая точка входа: передаёт консольные потоки и код возврата в <see cref="CliApp"/>.
/// Вся логика живёт в <see cref="CliApp"/>, чтобы тесты гоняли CLI без консоли.
/// </summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        // При перенаправленных потоках (дочерний процесс IDE, пайп CI) и без attached-консоли
        // .NET откатывается на устаревшую OEM-кодировку (CP866 на ru-RU Windows), и русский
        // вывод превращается в mojibake для UTF-8-читателей. Принудительно UTF-8 на весь контракт.
        Console.OutputEncoding = Encoding.UTF8;
        if (Console.IsInputRedirected)
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
            }
            catch
            {
                // некоторые хосты запрещают смену входной кодировки; фикс вывода всё равно действует
            }
        }

        // Приватный транспорт для IDE: диагностки не попадают в интерактивный терминал,
        // куда IDE направляет только вывод программы (тот же приём, что XSHARP_IDE_DIAGNOSTICS_PATH).
        var diagnosticsPath = Environment.GetEnvironmentVariable("AXIOM_IDE_DIAGNOSTICS_PATH");
        if (args.Length > 0 && args[0] == "run" && !string.IsNullOrEmpty(diagnosticsPath))
        {
            using var diagnostics = new StreamWriter(diagnosticsPath, false, new UTF8Encoding(false)) { AutoFlush = true };
            try
            {
                return CliApp.Run(args, Console.Out, diagnostics, Console.In);
            }
            catch (Exception error)
            {
                diagnostics.WriteLine($"AXIOM_IDE_RUNTIME_ERROR: {error.GetType().Name}: {error.Message}");
                return 1;
            }
        }

        return CliApp.Run(args, Console.Out, Console.Error, Console.In);
    }
}
