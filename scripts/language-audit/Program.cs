using System.Text;
using System.Text.Json;
using Axiom.Execution;
using Axiom.Runtime;
using Axiom.Semantics;

// Независимый аудит: сохраняет наблюдения, не меняет реализацию языка.
var root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
Directory.SetCurrentDirectory(root);
var options = args.Skip(1).ToArray();
var inventoryOnly = options.Contains("--inventory-only", StringComparer.Ordinal);
var outputOption = Array.IndexOf(options, "--output-dir");
string output;
if (outputOption >= 0)
{
    if (outputOption + 1 >= options.Length || options[outputOption + 1].StartsWith("--", StringComparison.Ordinal))
        throw new ArgumentException("Для --output-dir укажите каталог внутри .artifacts.");

    var artifactsRoot = Path.GetFullPath(Path.Combine(root, ".artifacts"));
    output = Path.GetFullPath(options[outputOption + 1], root);
    var relativeOutput = Path.GetRelativePath(artifactsRoot, output);
    if (Path.IsPathRooted(relativeOutput)
        || relativeOutput == ".."
        || relativeOutput.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
        || relativeOutput.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        throw new ArgumentException("Для --output-dir укажите каталог внутри .artifacts.");
}
else
{
    output = Path.Combine(root, inventoryOnly ? ".artifacts/language-inventory-current" : ".artifacts/language-audit-20261007");
}
Directory.CreateDirectory(output);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Inventory.Write(root, output);
if (inventoryOnly)
{
    Console.WriteLine($"Снимок текущего реестра сохранён: {output}");
    return 0;
}
var cases = AuditCases.Create(output);
cases.AddRange(FileCases.Create(output));
cases.AddRange(CoreCases.Create());
var observations = new List<object>();
var violations = 0;
foreach (var item in cases)
{
        {
        try
        {
            item.Prepare?.Invoke();
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(item.Source,
                cancellationProvider: new Deadline(TimeSpan.FromSeconds(8))));
            var actual = string.Join("\n", result.Output) + result.PendingOutput;
            var diagnostics = result.Diagnostics.Select(d => new { d.Code, d.Message, Location = d.Location.ToString() }).ToArray();
            var pass = item.ErrorCode is not null
                ? result.Diagnostics.Any(d => d.Code == item.ErrorCode)
                : item.Expected is not null ? result.Succeeded && actual == item.Expected : (bool?)null;
            if (pass is false) violations++;
            observations.Add(new { item.Id, item.Source, item.Expected, item.ErrorCode,
                Status = result.Status.ToString(), Engine = result.ActualEngine?.ToString(),
                Actual = actual, Utf16 = string.Join(" ", actual.Select(c => ((int)c).ToString("X4"))), Diagnostics = diagnostics,
                Pass = pass, SideEffect = item.Inspect?.Invoke() });
        }
        catch (Exception e)
        {
            violations++;
            observations.Add(new { item.Id, item.Source, Exception = e.ToString(), Pass = false });
        }
    }
}
File.WriteAllText(Path.Combine(output, "observations.json"), JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Сохранено {cases.Count} сценариев, {observations.Count} запусков.");
SampleAudit.Write(root, output);
Console.WriteLine($"Нарушений проверенных ожиданий: {violations}. Результаты требуют классификации по отчёту, а не автоматического исправления.");
return violations == 0 ? 0 : 1;

sealed class Deadline(TimeSpan limit) : IRuntimeCancellationProvider
{
    private readonly DateTime until = DateTime.UtcNow + limit;
    public bool IsCancellationRequested => DateTime.UtcNow > until;
}

record AuditCase(string Id, string Source, string? Expected = null, string? ErrorCode = null,
    Action? Prepare = null, Func<string>? Inspect = null);
