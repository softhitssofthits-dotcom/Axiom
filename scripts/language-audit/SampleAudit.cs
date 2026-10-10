using System.Text.Json;
using System.Text.RegularExpressions;
using Axiom.Execution;
using Axiom.Runtime;

static class SampleAudit
{
    public static void Write(string root, string output)
    {
        var results = new List<object>();
        foreach (var path in Directory.GetFiles(Path.Combine(root,"samples"),"*.axm",SearchOption.AllDirectories).Order())
        {
            var relative = Path.GetRelativePath(root,path).Replace('\\','/');
            var source = File.ReadAllText(path);
            var compilation = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
            // Сначала проверяется весь корпус. Затем безопасные автономные примеры
            // выполняются в BytecodeVm, а файловые примеры — в отдельных копиях.
            var code = MaskCommentsAndStrings(source);
            var candidate = true;
            var hasGraphics = Regex.IsMatch(code,
                @"\b(SCREEN|CANVAS|LOADIMAGE|LOADSPRITE|LOADTILESET|LOADTILEMAP|DRAWTILE|DRAWTILEMAP|DRAWTILELAYER|DRAWRECT|FILLRECT|DRAWLINE|DRAWCIRCLE|FILLCIRCLE|DRAWTEXT|PRESENT)\b",
                RegexOptions.IgnoreCase);
            // INPUT # и OPEN ... FOR INPUT относятся к файловому вводу, а не к
            // ожиданию человека. Убираем эти формы перед поиском интерактивного INPUT.
            var interactionSource = Regex.Replace(code, @"\bOPEN\b[^\r\n]*\bFOR\s+INPUT\b", "OPEN_FILE_FOR_INPUT", RegexOptions.IgnoreCase);
            interactionSource = Regex.Replace(interactionSource, @"\bINPUT\s*#\s*\d+", "FILE_INPUT", RegexOptions.IgnoreCase);
            var hasInteraction = Regex.IsMatch(interactionSource,
                @"\b(INPUT|INKEY|KEYDOWN|KEYPRESS|MOUSEBUTTON|MOUSEPRESS|MOUSEX|MOUSEY|SLEEP)\b",
                RegexOptions.IgnoreCase);
            var hasExternalEffects = Regex.IsMatch(code,
                @"\b(OPEN|CLOSE|WRITEFILE|APPENDFILE|COPYFILE|MOVEFILE|RENAMEFILE|DELETEFILE|COPYDIR|MOVEDIR|DELETEDIR|DELETEDIRRECURSIVE|MAKEDIR|MAKETEMPFILE|MAKETEMPDIR|CHANGEDIR|SETFILEDATE|SETFILECREATEDATE|SETFILEACCESSEDDATE|SETFILEATTR|SETREADONLY|SETHIDDEN|READFILE|READALLLINES|DIRSIZE|GETFILES|GETDIRS)\b",
                RegexOptions.IgnoreCase);
            var hasUnsafeAbsolutePath = Regex.IsMatch(source,
                "\\\"(?:[A-Za-z]:[\\\\/]|\\\\\\\\)[^\\\"]*\\\"",
                RegexOptions.IgnoreCase);
            var needsPreparedLinks = relative.Equals("samples/smoke/directory_link_boundaries.axm", StringComparison.OrdinalIgnoreCase);
            var excluded = hasGraphics || hasInteraction || (hasExternalEffects && (hasUnsafeAbsolutePath || needsPreparedLinks));
            object[]? runs = null;
            if (candidate && !excluded && compilation.Succeeded)
            {
                runs = [RunSample(source, root, relative, output, hasExternalEffects)];
            }
            var reason = runs is not null
                ? hasExternalEffects
                    ? "Запуск выполнен в BytecodeVm в отдельной копии примера внутри .artifacts; ожидаемый вывод отдельно не утверждается"
                    : "Запуск выполнен в BytecodeVm; ожидаемый вывод отдельно не утверждается"
                : !compilation.Succeeded
                    ? "Запуск не выполнялся: пример содержит ожидаемую ошибку компиляции"
                    : hasGraphics
                        ? "Проверена полная компиляция; для запуска нужен графический режим настоящей IDE"
                        : hasInteraction
                            ? "Проверена полная компиляция; программа ждёт клавиатуру, мышь, ввод или паузу"
                            : needsPreparedLinks
                                ? "Проверена полная компиляция; для запуска нужны заранее созданные ссылки и отдельная Windows-подготовка"
                                : hasUnsafeAbsolutePath
                                    ? "Проверена полная компиляция; пример использует абсолютный путь и не запускался, чтобы не затронуть внешние файлы"
                            : hasExternalEffects
                                ? "Проверена полная компиляция; пример работает с файлами или каталогами и требует отдельной подготовки"
                                : candidate
                                    ? "Полная компиляция проверена; автономный запуск не выбран консервативной политикой"
                                    : "Полная компиляция проверена; этот путь не входит в отобранные автономные семейства";
            results.Add(new {Path=relative,Compilation=compilation.Status.ToString(),Diagnostics=compilation.Diagnostics.Concat(compilation.BackendDiagnostics).Select(d=>new {d.Code,d.Message}).ToArray(),
                Bytecode=compilation.BytecodeAvailable,Runs=runs, Reason=reason});
        }
        File.WriteAllText(Path.Combine(output,"samples.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"Проверена компиляция {results.Count} примеров.");
    }
    private static object RunSample(string source, string root, string relative, string output, bool hasExternalEffects)
    {
        string? sandbox = null;
        string? previousTemp = null;
        string? previousTmp = null;
        try
        {
            var host = new ControlledHost();
            var resourceBaseDirectory = root;
            if (hasExternalEffects)
            {
                sandbox = CreateSampleSandbox(root, relative, output);
                previousTemp = Environment.GetEnvironmentVariable("TEMP");
                previousTmp = Environment.GetEnvironmentVariable("TMP");
                var temporaryDirectory = Path.Combine(sandbox, "temp");
                Directory.CreateDirectory(temporaryDirectory);
                Environment.SetEnvironmentVariable("TEMP", temporaryDirectory);
                Environment.SetEnvironmentVariable("TMP", temporaryDirectory);
                resourceBaseDirectory = Path.GetDirectoryName(Path.Combine(sandbox, relative.Replace('/', Path.DirectorySeparatorChar)))!;
            }

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source,
                runtimeClock: host, runtimeRandom: host,
                cancellationProvider: new Deadline(TimeSpan.FromSeconds(10)),
                resourceBaseDirectory: resourceBaseDirectory));
            return new { Status = result.Status.ToString(), Engine = result.ActualEngine?.ToString(),
                result.Output, result.PendingOutput,
                Diagnostics = result.Diagnostics.Select(d => new { d.Code, d.Message, Location = d.Location.ToString() }).ToArray() };
        }
        catch (Exception exception)
        {
            return new { Exception = exception.GetType().Name, Message = exception.Message };
        }
        finally
        {
            if (hasExternalEffects)
            {
                Environment.SetEnvironmentVariable("TEMP", previousTemp);
                Environment.SetEnvironmentVariable("TMP", previousTmp);
                if (sandbox is not null)
                {
                    var outputRoot = Path.GetFullPath(output);
                    var sandboxPath = Path.GetFullPath(sandbox);
                    var relativeSandbox = Path.GetRelativePath(outputRoot, sandboxPath);
                    if (!Path.IsPathRooted(relativeSandbox)
                        && relativeSandbox != "."
                        && relativeSandbox != ".."
                        && !relativeSandbox.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && !relativeSandbox.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
                    {
                        try { Directory.Delete(sandboxPath, recursive: true); }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
        }
    }

    private static string CreateSampleSandbox(string root, string relative, string output)
    {
        var outputRoot = Path.GetFullPath(output);
        var sandboxName = relative.Replace('/', '_').Replace('\\', '_').Replace('.', '_');
        var sandbox = Path.GetFullPath(Path.Combine(outputRoot, "sample-sandboxes", sandboxName));
        var relativeSandbox = Path.GetRelativePath(outputRoot, sandbox);
        if (Path.IsPathRooted(relativeSandbox)
            || relativeSandbox == ".."
            || relativeSandbox.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relativeSandbox.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Каталог изоляции вышел за границы .artifacts.");

        var sourceSamples = Path.Combine(root, "samples");
        var destinationSamples = Path.Combine(sandbox, "samples");
        CopyTreeWithoutReparsePoints(sourceSamples, destinationSamples);
        File.WriteAllText(Path.Combine(sandbox, "PROJECT.md"), "Изолированная копия для аудита примеров.\n");
        return sandbox;
    }

    private static void CopyTreeWithoutReparsePoints(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) continue;
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;
            CopyTreeWithoutReparsePoints(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string MaskCommentsAndStrings(string source)
    {
        var result = source.ToCharArray();
        var inString = false;
        var inComment = false;
        for (var index = 0; index < result.Length; index++)
        {
            var current = result[index];
            if (inComment)
            {
                if (current is '\r' or '\n') inComment = false;
                else result[index] = ' ';
                continue;
            }
            if (inString)
            {
                if (current == '"')
                {
                    if (index + 1 < result.Length && result[index + 1] == '"')
                    {
                        result[index] = ' ';
                        result[index + 1] = ' ';
                        index++;
                        continue;
                    }
                    inString = false;
                }
                result[index] = ' ';
                continue;
            }
            if (current == '"')
            {
                result[index] = ' ';
                inString = true;
            }
            else if (current == '\'')
            {
                result[index] = ' ';
                inComment = true;
            }
        }
        return new string(result);
    }

}

sealed class ControlledHost : IRuntimeClock, IRuntimeRandom
{
    private Random random = new(7012026);
    public double NextRandomDouble() => random.NextDouble();
    public void Reseed(int seed) => random = new(seed);
    public DateTimeOffset GetLocalNow() => new(2024,2,29,12,34,56,TimeSpan.Zero);
    public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Utc;
    public bool TryResolveTimeZone(string id,out TimeZoneInfo zone)
    {
        if(id.Equals("LOCAL",StringComparison.OrdinalIgnoreCase) || id.Equals("UTC",StringComparison.OrdinalIgnoreCase)) {zone=TimeZoneInfo.Utc;return true;}
        try {zone=TimeZoneInfo.FindSystemTimeZoneById(id);return true;}
        catch(TimeZoneNotFoundException) {zone=TimeZoneInfo.Utc;return false;}
    }
}
