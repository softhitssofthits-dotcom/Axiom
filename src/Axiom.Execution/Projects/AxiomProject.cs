using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Axiom.Core;

namespace Axiom.Execution;

/// <summary>Переносимый состав проекта; зависимости выполнения по-прежнему задаёт IMPORT.</summary>
public sealed class AxiomProject
{
    public string ProjectFilePath { get; private init; } = "";
    public string RootDirectory { get; private init; } = "";
    public string Name { get; private init; } = "";
    public string? EntryPath { get; private init; }
    public string? EntryError { get; private init; }
    public string[] SourceFiles { get; private init; } = [];
    public string[] AssetFiles { get; private init; } = [];
    public string ContentHash { get; private init; } = "";
    public static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase) {".git", ".artifacts", "node_modules", "bin", "obj"};
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AxiomProject Load(string file)
    {
        file = Path.GetFullPath(file);
        if (!string.Equals(Path.GetExtension(file), ".axproj", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Ожидался файл .axproj.");
        RejectLinks(file);
        var bytes = File.ReadAllBytes(file);
        return Parse(file, bytes);
    }
    public static AxiomProject Parse(string file, byte[] bytes)
    {
        file=Path.GetFullPath(file);
        if (bytes.Length > 1024 * 1024) throw new ArgumentException("Описание проекта превышает 1 MiB.");
        using var json = JsonDocument.Parse(bytes.AsMemory().Span.StartsWith(new byte[]{239,187,191}) ? bytes.AsMemory(3) : bytes.AsMemory());
        var data = json.RootElement;
        if (data.ValueKind != JsonValueKind.Object) throw new ArgumentException("Описание проекта должно быть объектом JSON.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in data.EnumerateObject())
            if(!names.Add(property.Name) || property.Name is not ("formatVersion" or "name" or "entry" or "sources" or "assets" or "exclude"))
                throw new ArgumentException("Повторное или неизвестное свойство проекта: " + property.Name);
        if (!data.TryGetProperty("formatVersion", out var version) || !version.TryGetInt32(out var number) || number != 1) throw new ArgumentException("Поддерживается formatVersion 1.");
        var root = Path.GetDirectoryName(file)!;
        var name = data.TryGetProperty("name", out var title) ? title.GetString() : Path.GetFileNameWithoutExtension(file);
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Имя проекта не должно быть пустым.");
        var sources = Patterns(data,"sources", ["**/*.axm"]);
        var assets = Patterns(data,"assets", []);
        var exclude = Patterns(data,"exclude", []);
        var all = Enumerate(root).Order(PathComparer).ToArray();
        bool Matches(string path, string[] patterns) => patterns.Any(pattern => Match(Path.GetRelativePath(root,path).Replace('\\','/'),pattern));
        var sourceFiles = all.Where(p => Path.GetExtension(p).Equals(".axm",StringComparison.OrdinalIgnoreCase) && Matches(p,sources) && !Matches(p,exclude)).ToArray();
        var assetFiles = all.Where(p => !PathComparer.Equals(p,file) && !sourceFiles.Contains(p,PathComparer) && Matches(p,assets) && !Matches(p,exclude)).ToArray();
        string? entry = null, error = null;
        if (!data.TryGetProperty("entry",out var entryValue) || entryValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(entryValue.GetString())) error = "Не назначен файл запуска проекта.";
        else {
            entry = Resolve(root,entryValue.GetString()!);
            if(!sourceFiles.Contains(entry,PathComparer)) error = "Файл запуска отсутствует или исключён из исходников проекта: " + entryValue.GetString();
        }
        return new AxiomProject { ProjectFilePath=file, RootDirectory=root, Name=name!, EntryPath=entry, EntryError=error, SourceFiles=sourceFiles, AssetFiles=assetFiles, ContentHash=Hash(bytes) };
    }
    public void RequireEntry() { if(EntryError is not null || EntryPath is null) throw new ArgumentException(EntryError ?? "Не назначен файл запуска."); }
    public bool Contains(string file) => SourceFiles.Contains(Path.GetFullPath(file),PathComparer) || AssetFiles.Contains(Path.GetFullPath(file),PathComparer);
    public IReadOnlyList<SourceDocument> Dependencies(string text,string file,IEnumerable<SourceDocument>? snapshots=null)
    {
        if(!SourceFiles.Contains(Path.GetFullPath(file),PathComparer)) throw new ArgumentException("Исходник не входит в проект: " + file);
        var dependencies=SourceProject.LoadDependencies(text,file,snapshots,new HashSet<string>(SourceFiles,PathComparer));
        foreach(var dependency in dependencies)
            if(!SourceFiles.Contains(Path.GetFullPath(dependency.Name),PathComparer)) throw new ArgumentException("IMPORT обращается к исключённому исходнику: " + dependency.Name);
        return dependencies;
    }
    public IReadOnlyList<Diagnostic> Check(IEnumerable<SourceDocument>? snapshots=null)
    {
        RequireEntry();
        var open=(snapshots??[]).ToDictionary(d=>Path.GetFullPath(d.Name),d=>d.Text,PathComparer);
        foreach(var file in open.Keys) if(!SourceFiles.Contains(file,PathComparer)) throw new ArgumentException("Снимок не входит в исходники проекта: " + file);
        var diagnostics=new List<Diagnostic>();
        var checkedFiles=new HashSet<string>(PathComparer);
        foreach(var file in new[]{EntryPath!}.Concat(SourceFiles)) {
            if(!checkedFiles.Add(file)) continue;
            var text=open.TryGetValue(file,out var value)?value:File.ReadAllText(file);
            var dependencies=Dependencies(text,file,open.Select(d=>new SourceDocument(d.Key,d.Value)));
            var result=new AxiomExecutionPipeline().Compile(new CompilationRequest(text,sourceName:file,additionalDocuments:dependencies,includeUsageWarnings:true));
            diagnostics.AddRange(result.Diagnostics);diagnostics.AddRange(result.BackendDiagnostics);
            foreach(var dependency in dependencies) checkedFiles.Add(dependency.Name);
        }
        return diagnostics.DistinctBy(d=>(d.Code,d.Location.Document,d.Location.Span,d.Message)).ToArray();
    }
    public static string Resolve(string root,string relative)
    {
        ValidatePattern(relative,allowWildcards:false);
        var result=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));
        var rel=Path.GetRelativePath(root,result);
        if(Path.IsPathRooted(rel)||rel==".."||rel.StartsWith(".."+Path.DirectorySeparatorChar)) throw new ArgumentException("Путь выходит за корень проекта.");
        RejectLinks(result);return result;
    }
    public static void RejectLinks(string path)
    {
        for(var current=Path.GetFullPath(path);current is not null;current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0) throw new ArgumentException("Ссылки и точки повторной обработки в проекте не поддерживаются: " + current);
    }
    private static IEnumerable<string> Enumerate(string root)
    {
        foreach(var path in Directory.EnumerateFileSystemEntries(root)) {
            if(Ignored.Contains(Path.GetFileName(path))) continue;
            var attributes=File.GetAttributes(path);
            if((attributes&(FileAttributes.ReparsePoint|FileAttributes.Hidden|FileAttributes.System))!=0) continue;
            if((attributes&FileAttributes.Directory)!=0) { foreach(var child in Enumerate(path)) yield return child; }
            else yield return Path.GetFullPath(path);
        }
    }
    private static string[] Patterns(JsonElement data,string key,string[] fallback)
    {
        if(!data.TryGetProperty(key,out var value)) return fallback;
        if(value.ValueKind!=JsonValueKind.Array) throw new ArgumentException(key+" должен быть массивом строк.");
        var result=value.EnumerateArray().Select(item=>item.ValueKind==JsonValueKind.String?item.GetString()!:throw new ArgumentException(key+" содержит не строку.")).ToArray();
        foreach(var pattern in result) ValidatePattern(pattern,true);
        return result;
    }
    private static void ValidatePattern(string value,bool allowWildcards)
    {
        if(string.IsNullOrWhiteSpace(value)||value.Contains('\\')||value.Contains(':')||value.StartsWith('/')||value.Contains('?')||value.Split('/').Any(p=>p is "" or "." or "..")||(!allowWildcards&&value.Contains('*')))
            throw new ArgumentException("Недопустимый относительный путь или маска: " + value);
        if(value.Split('/').Any(p=>p.Contains("**")&&p!="**")) throw new ArgumentException("** допустимо только как отдельный сегмент пути.");
    }
    private static bool Match(string path,string pattern)
    {
        var expression=string.Join("/",pattern.Split('/').Select(p=>p=="**"?".*":Regex.Escape(p).Replace("\\*","[^/]*")));
        expression=expression.Replace(".*/","(?:.*/)?");
        return Regex.IsMatch(path,"\\A"+expression+"\\z",OperatingSystem.IsWindows()?RegexOptions.IgnoreCase:RegexOptions.None,TimeSpan.FromSeconds(1));
    }
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static byte[] ChangeEntry(AxiomProject project,string? relative)
    {
        if(relative is not null) { var path=Resolve(project.RootDirectory,relative);if(!project.SourceFiles.Contains(path,PathComparer)) throw new ArgumentException("Файл запуска должен входить в исходники проекта."); }
        var data=JsonNode.Parse(File.ReadAllText(project.ProjectFilePath))!.AsObject();data["entry"]=relative;
        return System.Text.Encoding.UTF8.GetBytes(data.ToJsonString(JsonOptions)+"\n");
    }
}
