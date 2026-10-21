using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;

namespace Axiom.DesktopHost;

internal static class ProjectCommands
{
    private static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
    public static object Execute(string method,JsonElement payload)
    {
        var path=Path.GetFullPath(payload.GetProperty("path").GetString()!);
        if(method=="project.open" && Directory.Exists(path)) {
            AxiomProject.RejectLinks(path);
            var candidates=Directory.EnumerateFiles(path).Where(p=>Path.GetExtension(p).Equals(".axproj",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(candidates.Length==0) return new {legacy=true,rootDirectory=path};
            if(candidates.Length>1) throw new ArgumentException("В папке несколько .axproj. Откройте нужный файл проекта.");
            path=candidates[0];
        }
        if(!Path.GetExtension(path).Equals(".axproj",StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Ожидался путь .axproj.");
        if(method=="project.create") {
            AxiomProject.RejectLinks(path);
            var bytes=Encoding.UTF8.GetBytes(payload.GetProperty("text").GetString()!);
            AxiomProject.Parse(path,bytes);
            using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes);stream.Flush(true);}
            return Describe(AxiomProject.Load(path));
        }
        var project=AxiomProject.Load(path);
        if(method=="project.save" || method=="project.entry") {
            var expected=payload.GetProperty("expectedHash").GetString();
            var bytes=method=="project.entry" ? AxiomProject.ChangeEntry(project,payload.GetProperty("entry").ValueKind==JsonValueKind.Null?null:payload.GetProperty("entry").GetString()) : Encoding.UTF8.GetBytes(payload.GetProperty("text").GetString()!);
            AxiomProject.Parse(path,bytes);
            SaveAtomic(path,bytes,expected);
            project=AxiomProject.Load(path);
        }
        if(method=="project.check") {
            var snapshots=payload.TryGetProperty("documents",out var docs)?docs.EnumerateArray().Select(d=>new SourceDocument(d.GetProperty("sourceFilePath").GetString()!,d.GetProperty("sourceText").GetString()!)).ToArray():[];
            var diagnostics=project.Check(snapshots).Select(d=>{var item=ProgramDiagnosticItem.FromDiagnostic(d);return new {item.Code,item.Stage,item.Message,item.Line,item.Column,item.Length,item.SpanStart,item.SpanLength,item.SourceFilePath,severity=d.Severity.ToString().ToLowerInvariant()};}).ToArray();
            return new {status=diagnostics.Any(d=>d.severity=="error")?"Failure":"Success",diagnostics};
        }
        return Describe(project);
    }
    private static void SaveAtomic(string path,byte[] bytes,string? expected)
    {
        using var mutex=new Mutex(false,"AxiomProject_"+AxiomProject.Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant())));
        try { mutex.WaitOne(); } catch(AbandonedMutexException) { }
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using var original=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
            using var content=new MemoryStream();original.CopyTo(content);
            if(AxiomProject.Hash(content.ToArray())!=expected) throw new IOException("Описание проекта изменено извне. Обновите проект перед сохранением.");
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes);stream.Flush(true);}
            File.Replace(temporary,path,null);
        } finally {if(File.Exists(temporary))File.Delete(temporary);mutex.ReleaseMutex();}
    }
    private static object Describe(AxiomProject project) => new {project.ProjectFilePath,project.RootDirectory,project.Name,project.EntryPath,project.EntryError,project.SourceFiles,project.AssetFiles,project.ContentHash,text=File.ReadAllText(project.ProjectFilePath)};
}
