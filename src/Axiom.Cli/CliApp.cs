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
/// Команда `check` выполняет полную компиляционную проверку (frontend и backend), не исполняет программу и не создаёт файлы.
/// </summary>
internal static partial class CliApp
{
    public const string Version = AxiomVersionInfo.CompilerVersion;

    private const int MaxSourceBytes = 16 * 1024 * 1024;

    /// <summary>Подмена stdin для in-process тестов; продакшн читает переданный TextReader.</summary>
    internal static Func<TextReader>? StdinOverride;

    private static readonly string UsageText =
        $"""
        usage: axiom <command> [options] <file>
          commands: run | check | fmt | metadata | migrate-arrays
          projects: run/check принимают .axproj; fmt работает с исходниками .axm
          options:  --json    машинно-читаемые диагностики (check) или результат миграции (migrate-arrays)
                    --stdin   читать источник из stdin (check, fmt, migrate-arrays)
                    --help    показать эту справку
                    --version показать версию и git-ревизию
          notes:    check — полная компиляционная проверка (frontend и backend), без запуска программы и без файлов
                    migrate-arrays [--json] [--stdin | <file>] — только предварительный просмотр; запись не поддерживается
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

        if(args[0] is "run" or "check" && args.Any(a=>a.EndsWith(".axproj",StringComparison.OrdinalIgnoreCase)))
            return RunProject(args,stdout,stderr,stdin);

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
            case "migrate-arrays":
                return RunArrayMigration(args, stdout, stderr, stdin);
            default:
                return Usage(stderr);
        }
    }

    private static int RunProject(string[] args,TextWriter stdout,TextWriter stderr,TextReader stdin)
    {
        var json=args.Contains("--json");
        var clean=args.Where(a=>a!="--json").ToArray();
        if(clean.Length!=2 || (json&&args[0]!="check") || args.Length!=(json?3:2)) return Usage(stderr);
        try {
            var project=AxiomProject.Load(clean[1]);project.RequireEntry();
            if(args[0]=="run") return RunProgram(["run",project.EntryPath!],stdout,stderr,stdin,project);
            var diagnostics=project.Check();
            var ok=!diagnostics.Any(d=>d.Severity==DiagnosticSeverity.Error);
            if(json) stdout.Write(DiagnosticsJson(diagnostics,ok,project.ProjectFilePath));
            PrintDiagnostics(diagnostics,stderr,project.ProjectFilePath);
            return ok?0:1;
        } catch(Exception error) when(error is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or InvalidOperationException) {
            stderr.WriteLine("axiom: ошибка проекта: "+error.Message);
            if(json) stdout.Write(System.Text.Json.JsonSerializer.Serialize(new {ok=false,diagnostics=new[]{new {code="AXPROJECT",message=error.Message,file=clean[1],severity="error"}}}));
            return 2;
        }
    }

    // ---- "run" ----

    private static int RunProgram(string[] args, TextWriter stdout, TextWriter stderr, TextReader stdin, AxiomProject? project = null)
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

        // Чтение зависимостей общее с check: отказ файловой системы даёт
        // контролируемое сообщение и код 2, а не выброшенное исключение.
        IReadOnlyList<SourceDocument> dependencies;
        if(project is not null) dependencies=project.Dependencies(source,path);
        else if (!TryLoadDependencies(source, path, stderr, out dependencies)) return 2;

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
                cancellationProvider: host,
                sourceName: Path.GetFullPath(path),
                additionalDocuments: dependencies, resourceBaseDirectory:project?.RootDirectory));
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

        // При внешнем отказе зависимости контракт машинного ответа сохраняется:
        // --json всё равно получает объект с ok=false и диагностикой отказа.
        if (!TryLoadDependencies(source, path, stderr, out var dependencies, out var dependencyFailure))
        {
            if (wantsJson)
            {
                stdout.Write(DiagnosticsJson([dependencyFailure], ok: false, path));
            }

            return 2;
        }

        var (checkDiagnostics, checkHasError) = CheckSource(source, path, dependencies);
        if (wantsJson)
        {
            stdout.Write(DiagnosticsJson(checkDiagnostics, !checkHasError, path));
        }

        PrintDiagnostics(checkDiagnostics, stderr, path);
        return checkHasError ? 1 : 0;
    }

    private static (List<Diagnostic> Diagnostics, bool HasError) CheckSource(
        string source,
        string? path = null,
        IReadOnlyList<SourceDocument>? dependencies = null)
    {
        var sink = new CollectingDiagnosticSink();
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source, diagnosticSink: sink,
            sourceName: path is null ? null : Path.GetFullPath(path),
            additionalDocuments: dependencies ?? SourceProject.LoadDependencies(source, path)));

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
                .Append(Q).Append("file").Append(Q).Append(':').Append(Q).Append(JsonText.Escape(diagnostic.Location.Document ?? fileName)).Append(Q)
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

    /// <summary>
    /// Общая загрузка зависимостей для check и run. Отказ файловой системы
    /// возвращает false и уже выведенное сообщение; неизвестный модуль остаётся
    /// языковой диагностикой MB2049 и проходит обычным путём компиляции.
    /// </summary>
    private static bool TryLoadDependencies(
        string source,
        string? path,
        TextWriter stderr,
        out IReadOnlyList<SourceDocument> dependencies)
    {
        return TryLoadDependencies(source, path, stderr, out dependencies, out _);
    }

    private static bool TryLoadDependencies(
        string source,
        string? path,
        TextWriter stderr,
        out IReadOnlyList<SourceDocument> dependencies,
        out Diagnostic failure)
    {
        failure = null!;

        try
        {
            dependencies = SourceProject.LoadDependencies(source, path);
            return true;
        }
        catch (Exception error) when (error is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            dependencies = [];
            failure = new Diagnostic(
                DiagnosticCodes.DependencyReadFailure,
                $"Не удалось прочитать зависимость точки входа: {error.Message}",
                new TextLocation(new TextSpan(0, 0), 1, 1, path));
            stderr.WriteLine($"axiom: не удалось прочитать зависимость '{path}': {error.Message}");
            return false;
        }
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
            stderr.WriteLine($"{diagnostic.Location.Document ?? fileName}({diagnostic.Location.Line},{diagnostic.Location.Column}): ошибка {diagnostic.Code}: {diagnostic.Message}");
        }
    }

    private static int ShowVersion(TextWriter stderr)
    {
        stderr.WriteLine($"axiom {Version} (language Axiom {AxiomVersionInfo.LanguageVersion}; revision {AxiomMetadata.SourceRevision})");
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
