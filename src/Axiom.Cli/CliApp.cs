using System.Text;
using Axiom.Core;
using Axiom.Execution;
using Axiom.Formatting;

namespace Axiom.Cli;

/// <summary>
/// Контракт CLI (docs/ELECTRON_IDE_PLAN.md, раздел 4): stdout несёт ТОЛЬКО вывод программы
/// при `run` и продукт команды при `fmt`/`check --json`/`metadata`; все диагностические
/// сообщения, usage и версия идут в stderr. Коды возврата: 0 — успех, 1 — ошибки компиляции
/// или рантайма, 2 — неверный usage или файл не найден.
/// </summary>
internal static class CliApp
{
    public const string Version = "0.1.0";

    private const int MaxSourceBytes = 16 * 1024 * 1024;

    /// <summary>Подмена stdin для in-process тестов; продакшн читает переданный TextReader.</summary>
    internal static Func<TextReader>? StdinOverride;

    private static readonly string UsageText =
        $"""
        usage: axiom <command> [options] <file>
          commands: run | check | fmt | metadata
          options:  --json    машинно-читаемые диагностки (check)
                    --stdin   читать источник из stdin (check, fmt)
                    --help    показать эту справку
                    --version показать версию и git-ревизию
        axiom {Version}
        """;

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, TextReader? stdin = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        stdin ??= StdinOverride is not null ? StdinOverride() : Console.In;

        if (args.Length == 0)
        {
            return Usage(stderr);
        }

        switch (args[0])
        {
            case "--version":
                return args.Length == 1 ? ShowVersion(stderr) : Usage(stderr);
            case "--help":
            case "-h":
                return args.Length == 1 ? ShowHelp(stderr) : Usage(stderr);
            case "run":
                return RunProgram(args, stdout, stderr, stdin);
            case "check":
                return RunCheck(args, stdout, stderr, stdin);
            case "fmt":
                return RunFormat(args, stdout, stderr, stdin);
            case "metadata":
                return args.Length == 1 ? RunMetadata(stdout) : Usage(stderr);
            default:
                return Usage(stderr);
        }
    }

    // ---- "run" ----

    private static int RunProgram(string[] args, TextWriter stdout, TextWriter stderr, TextReader stdin)
    {
        var path = ParseSingleFileArg(args);
        if (path is null)
        {
            return Usage(stderr);
        }

        var source = ReadSource(path, stderr);
        if (source is null)
        {
            return 2;
        }

        var host = new ConsoleHost(stdout, stdin);
        var sink = new CollectingDiagnosticSink();
        var pipeline = new AxiomExecutionPipeline();

        // Ctrl+C в реальной консоли превращается в штатную отмену исполнения, а не в обрыв процесса.
        ConsoleCancelEventHandler? cancelHandler = null;
        if (ReferenceEquals(stdin, Console.In))
        {
            cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                host.RequestCancellation();
            };
            Console.CancelKeyPress += cancelHandler;
        }

        int exitCode;
        try
        {
            var result = pipeline.Run(new ExecutionRequest(
                source,
                inputProvider: host,
                outputWriter: host,
                diagnosticSink: sink,
                runtimeClock: host,
                runtimeRandom: host,
                cancellationProvider: host));
            exitCode = result.Status == ExecutionStatus.Success ? 0 : 1;
        }
        catch (ConsoleGraphicsNotSupportedException error)
        {
            stderr.WriteLine($"axiom: {error.Message}");
            return 1;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            stderr.WriteLine($"axiom: {error.Message}");
            return 1;
        }
        finally
        {
            if (cancelHandler is not null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        PrintDiagnostics(sink.Diagnostics, stderr, path);
        return exitCode;
    }

    // ---- "check" ----

    private static int RunCheck(string[] args, TextWriter stdout, TextWriter stderr, TextReader stdin)
    {
        if (args.Length >= 2 && args[1] == "--stdin")
        {
            var json = args.Length == 3 && args[2] == "--json";
            if (args.Length is not (2 or 3) || (args.Length == 3 && !json))
            {
                return Usage(stderr);
            }

            var stdinSource = ReadStdin(stdin, stderr);
            if (stdinSource is null)
            {
                return 2;
            }

            var (diagnostics, hasError) = CheckSource(stdinSource);
            var fileName = "<buffer>";
            if (json)
            {
                stdout.Write(DiagnosticsJson(diagnostics, !hasError, fileName));
            }
            else
            {
                PrintDiagnostics(diagnostics, stderr, fileName);
            }

            return hasError ? 1 : 0;
        }

        var wantsJson = Array.Exists(args, static argument => argument == "--json");
        var fileArgs = wantsJson ? args.Where(static argument => argument != "--json").ToArray() : args;
        var path = ParseSingleFileArg(fileArgs);
        if (path is null)
        {
            return Usage(stderr);
        }

        var source = ReadSource(path, stderr);
        if (source is null)
        {
            return 2;
        }

        var (checkDiagnostics, checkHasError) = CheckSource(source);
        if (wantsJson)
        {
            stdout.Write(DiagnosticsJson(checkDiagnostics, !checkHasError, path));
        }

        PrintDiagnostics(checkDiagnostics, stderr, path);
        return checkHasError ? 1 : 0;
    }

    private static (List<Diagnostic> Diagnostics, bool HasError) CheckSource(string source)
    {
        var sink = new CollectingDiagnosticSink();
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source, diagnosticSink: sink));

        // Диагностики Axiom не делятся на ошибки/предупреждения: любая диагностика — ошибка.
        var diagnostics = sink.Diagnostics
            .OrderBy(static diagnostic => diagnostic.Location.Line, Comparer<int>.Default)
            .ThenBy(static diagnostic => diagnostic.Location.Column, Comparer<int>.Default)
            .ToList();
        return (diagnostics, !compilation.Succeeded || diagnostics.Count > 0);
    }

    private static string DiagnosticsJson(IReadOnlyList<Diagnostic> diagnostics, bool ok, string fileName)
    {
        const string Q = "\"";
        var builder = new StringBuilder(256);
        builder.Append('{').Append(Q).Append("ok").Append(Q).Append(':').Append(ok ? "true" : "false");
        builder.Append(',').Append(Q).Append("diagnostics").Append(Q).Append(":[");
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('{')
                .Append(Q).Append("file").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(fileName)).Append(Q)
                .Append(',').Append(Q).Append("line").Append(Q).Append(':').Append(diagnostic.Location.Line)
                .Append(',').Append(Q).Append("col").Append(Q).Append(':').Append(diagnostic.Location.Column)
                .Append(',').Append(Q).Append("length").Append(Q).Append(':').Append(diagnostic.Location.Span.Length)
                .Append(',').Append(Q).Append("severity").Append(Q).Append(':').Append(Q).Append("error").Append(Q)
                .Append(',').Append(Q).Append("code").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(diagnostic.Code)).Append(Q)
                .Append(',').Append(Q).Append("message").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(diagnostic.Message)).Append(Q)
                .Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    // ---- "fmt" ----

    private static int RunFormat(string[] args, TextWriter stdout, TextWriter stderr, TextReader stdin)
    {
        var formatting = new AxiomCodeFormattingService();

        if (args.Length == 2 && args[1] == "--stdin")
        {
            var source = ReadStdin(stdin, stderr);
            if (source is null)
            {
                return 2;
            }

            stdout.Write(formatting.FormatDocument(source, CodeFormattingStyle.Classic));
            return 0;
        }

        var path = ParseSingleFileArg(args);
        if (path is null)
        {
            return Usage(stderr);
        }

        var fileSource = ReadSource(path, stderr);
        if (fileSource is null)
        {
            return 2;
        }

        stdout.Write(formatting.FormatDocument(fileSource, CodeFormattingStyle.Classic));
        return 0;
    }

    // ---- "metadata" ----

    private static int RunMetadata(TextWriter stdout)
    {
        stdout.Write(AxiomMetadata.ToJson());
        return 0;
    }

    // ---- общие помощники ----

    private static string? ParseSingleFileArg(string[] args)
    {
        string? path = null;
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (path is not null)
            {
                return null;
            }

            path = args[index];
        }

        return path;
    }

    private static string? ReadSource(string path, TextWriter stderr)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Exists && info.Length > MaxSourceBytes)
            {
                stderr.WriteLine($"axiom: '{path}' превышает лимит размера источника в 16 МБ");
                return null;
            }

            return File.ReadAllText(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            stderr.WriteLine($"axiom: не удалось прочитать '{path}': {error.Message}");
            return null;
        }
    }

    private static string? ReadStdin(TextReader stdin, TextWriter stderr)
    {
        try
        {
            var text = stdin.ReadToEnd();
            if (Encoding.UTF8.GetByteCount(text) > MaxSourceBytes)
            {
                stderr.WriteLine("axiom: источник из stdin превышает лимит размера в 16 МБ");
                return null;
            }

            return text;
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            stderr.WriteLine($"axiom: не удалось прочитать stdin: {error.Message}");
            return null;
        }
    }

    private static void PrintDiagnostics(IReadOnlyList<Diagnostic> diagnostics, TextWriter stderr, string fileName)
    {
        foreach (var diagnostic in diagnostics)
        {
            stderr.WriteLine($"{fileName}({diagnostic.Location.Line},{diagnostic.Location.Column}): ошибка {diagnostic.Code}: {diagnostic.Message}");
        }
    }

    private static int ShowVersion(TextWriter stderr)
    {
        stderr.WriteLine($"axiom {Version} (revision {AxiomMetadata.SourceRevision})");
        return 0;
    }

    private static int ShowHelp(TextWriter stderr)
    {
        stderr.Write(UsageText);
        return 0;
    }

    private static int Usage(TextWriter stderr)
    {
        stderr.WriteLine(UsageText);
        return 2;
    }

    private sealed class CollectingDiagnosticSink : IDiagnosticSink
    {
        public List<Diagnostic> Diagnostics { get; } = [];

        public void ReportDiagnostic(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
    }
}
