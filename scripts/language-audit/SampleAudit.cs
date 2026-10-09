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
            // Весь корпус разбирается. Запуск без GUI и внешних действий разрешён
            // только для чистых языковых семейств; ошибки примеров не исправляются.
            var candidate = relative.StartsWith("samples/functions/") || relative.StartsWith("samples/control-flow/")
                || relative.StartsWith("samples/expressions/")
                || new[] {"type","sub","function","dim","const","if","select","for","while","do","try","error","print","randomize"}.Any(name=>relative.StartsWith("samples/operators/"+name+"/"));
            var excluded = Regex.IsMatch(source, @"\b(OPEN|CLOSE|WRITEFILE|APPENDFILE|COPYFILE|MOVEFILE|RENAMEFILE|DELETEFILE|COPYDIR|MOVEDIR|DELETEDIR|DELETEDIRRECURSIVE|MAKEDIR|MAKETEMPFILE|MAKETEMPDIR|CHANGEDIR|SETFILEDATE|SETFILECREATEDATE|SETFILEACCESSEDDATE|SETFILEATTR|SETREADONLY|SETHIDDEN|SCREEN|CANVAS|LOADIMAGE|LOADTILESET|LOADTILEMAP|DRAWTILE|INPUT|INKEY|SLEEP|READFILE|READALLLINES|DIRSIZE|GETFILES|GETDIRS)\b",RegexOptions.IgnoreCase);
            object[]? runs = null;
            if (candidate && !excluded && compilation.Succeeded)
            {
                runs = new[] {ExecutionMode.Interpreter,ExecutionMode.Bytecode}.Select(mode =>
                {
                    try
                    {
                        var host = new ControlledHost();
                        var r = new AxiomExecutionPipeline().Run(new ExecutionRequest(source,mode:mode,runtimeClock:host,runtimeRandom:host,cancellationProvider:new Deadline(TimeSpan.FromSeconds(2))));
                        return (object)new {Mode=mode.ToString(),Status=r.Status.ToString(),Engine=r.ActualEngine?.ToString(),r.UsedInterpreterFallback,
                            Output=r.Output, r.PendingOutput, Diagnostics=r.Diagnostics.Select(d=>new {d.Code,d.Message,Location=d.Location.ToString()}).ToArray()};
                    }
                    catch(Exception e) { return new {Mode=mode.ToString(),Exception=e.GetType().Name,Message=e.Message}; }
                }).ToArray();
            }
            results.Add(new {Path=relative,Compilation=compilation.Status.ToString(),Diagnostics=compilation.Diagnostics.Select(d=>new {d.Code,d.Message}).ToArray(),
                Bytecode=compilation.BytecodeAvailable,Runs=runs, Reason=runs is null ? "Проверен только frontend; отсутствует безопасный автономный контекст или есть диагностика" : "Проверка паритета, ожидаемый вывод отдельно не утверждается"});
        }
        File.WriteAllText(Path.Combine(output,"samples.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"Проверен разбор {results.Count} примеров.");
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
