using System.Text.Json;
using System.Text.RegularExpressions;
using Axiom.Runtime;
using Axiom.Semantics;
using Axiom.Core;
using Axiom.Syntax;

static class Inventory
{
    public static void Write(string root, string output)
    {
        var sources = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories);
        var tests = Directory.GetFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories);
        var samples = Directory.GetFiles(Path.Combine(root, "samples"), "*.axm", SearchOption.AllDirectories);
        var manuals = Directory.GetFiles(Path.Combine(root, "docs/manual"), "*.html");
        var cache = sources.Concat(tests).Concat(samples).Concat(manuals).Distinct().ToDictionary(p => p, File.ReadAllText);
        var defects = new Dictionary<string,string>
        {
            ["ARRAYSUM"]="LA26-001/002/008", ["ARRAYMIN"]="LA26-001/008/009", ["ARRAYMAX"]="LA26-001/008; LA26-009 — общий путь",
            ["ARRAYAVERAGE"]="LA26-007/008", ["CINT"]="LA26-003/006", ["CLNG"]="LA26-004", ["VAL"]="LA26-005",
            ["REVERSE"]="LA26-012", ["READTEXT"]="LA26-010/011", ["READTOEND"]="LA26-010", ["READLINE"]="LA26-010 — общий путь",
            ["WRITELINE"]="LA26-013", ["NEWLINE"]="LA26-013 — общий путь"
        };
        string Relative(string p) => Path.GetRelativePath(root, p).Replace('\\', '/');
        string[] Mentions(IEnumerable<string> files, string token) => files.Where(p => Regex.IsMatch(cache[p], @"\b" + Regex.Escape(token) + @"\b", RegexOptions.IgnoreCase)).Select(Relative).ToArray();
        var rows = BuiltinFunctions.All.SelectMany(f =>
        {
            BuiltinFunctions.TryGetDescriptors(f.Name, out var family);
            return family;
        }).Select(d => new
        {
            Id = d.Id.ToString(), d.Name, Arity = d.Parameters.Count,
            Parameters = d.Parameters.Select(p => new { p.Name, Type = p.Type.Name }).ToArray(),
            ReturnType = d.DeclaredReturnType.Name, Binding = d.BindingKind.ToString(),
            OperandPolicy = d.OperandBindingPolicy.ToString(), ResultPolicy = d.ResultTypePolicy.ToString(),
            VmDispatch = BytecodeVm.SupportsBuiltin(d.Id, d.Parameters.Count),
            Vm = Mentions(sources.Where(p => Path.GetFileName(p).StartsWith("BytecodeVm")), "BuiltinFunctionId." + d.Id),
            Helpers = Mentions(sources.Where(p => p.Contains("Builtins")), d.Name),
            Tests = Mentions(tests, d.Name), Samples = Mentions(samples, d.Name), Manual = Mentions(manuals, d.Name),
            HistoricalIssue = defects.TryGetValue(d.Name, out var issue) ? issue : null,
            Status = "UNVERIFIED_PER_FORM"
        }).ToArray();
        var reserved = Enum.GetValues<BuiltinFunctionId>().Where(id => !BuiltinFunctions.TryGetDescriptor(id, out _)).Select(id => id.ToString()).ToArray();
        var integrity = new
        {
            MissingVmDispatch = rows.Where(r => !r.VmDispatch).Select(r => r.Id).Distinct().ToArray(),
            DuplicateSignatures = rows.GroupBy(r => r.Name + "(" + string.Join(",",r.Parameters.Select(p=>p.Type)) + ")").Where(g=>g.Count()>1).Select(g=>g.Key).ToArray(),
            Keywords = AxiomLanguageKeywords.Keywords.Order().ToArray(),
            TypeNames = AxiomLanguageKeywords.TypeNames.Order().ToArray(),
            SyntaxKinds = Enum.GetNames<SyntaxKind>(),
            BoundKinds = Enum.GetNames<BoundNodeKind>(),
            BoundStatements = sources.Where(p=>p.Contains("BoundNodes")).SelectMany(p=>Regex.Matches(cache[p],@"class\s+(\w+Statement)\b").Select(m=>new {Name=m.Groups[1].Value,File=Relative(p)})).ToArray(),
            SyntaxStatements = sources.Where(p=>p.Contains("Axiom.Syntax")).SelectMany(p=>Regex.Matches(cache[p],@"class\s+(\w+StatementSyntax)\b").Select(m=>new {Name=m.Groups[1].Value,File=Relative(p)})).ToArray(),
            ValueTypes = sources.Where(p=>Path.GetFileName(p).EndsWith("TypeSymbol.cs")).Select(Relative).ToArray()
        };
        File.WriteAllText(Path.Combine(output, "inventory.json"), JsonSerializer.Serialize(new { GeneratedAt = DateTimeOffset.Now, Descriptors = rows.Length,
            Names = rows.Select(r => r.Name).Distinct().Count(), Ids = Enum.GetValues<BuiltinFunctionId>().Length, Reserved = reserved, Integrity = integrity, Rows = rows }, new JsonSerializerOptions { WriteIndented = true }));
        var lines = new List<string> { $"# Независимая матрица реестра — {DateTime.Now:yyyy-MM-dd}", "",
            "Получена из загруженного реестра, включая перегрузки. Тип результата при специальной политике определяется связывателем; объявленный тип не является окончательным. Упоминания в тестах, примерах и справочнике — карта поиска, не доказательство выполнения. Семантический статус каждой строки: UNVERIFIED_PER_FORM. Историческая находка хранится отдельно и не означает текущий дефект.", "",
            $"Дескрипторов: {rows.Length}; имён: {rows.Select(r => r.Name).Distinct().Count()}; ID: {Enum.GetValues<BuiltinFunctionId>().Length}; незарегистрированные ID: {string.Join(", ", reserved)}.", "",
            "| Имя | ID | Параметры | Объявленный результат | Связывание / операнды / результат | BytecodeVm | Тесты / примеры / manual |",
            "|---|---|---|---|---|---|---|" };
        lines.AddRange(rows.Select(r => $"| {r.Name} | {r.Id} | {string.Join(", ", r.Parameters.Select(p => p.Name + ": " + p.Type))} | {r.ReturnType} | {r.Binding} / {r.OperandPolicy} / {r.ResultPolicy} | {r.VmDispatch} | {r.Tests.Length} / {r.Samples.Length} / {r.Manual.Length} |"));
        File.WriteAllLines(Path.Combine(output, "matrix.md"), lines);
    }
}
