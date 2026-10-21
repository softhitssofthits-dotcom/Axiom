using System.Text.Json;
using Axiom.Core;

namespace Axiom.Execution.Tests;

public sealed class AxiomProjectTests : IDisposable
{
    private readonly string root=Path.GetFullPath(Path.Combine(".artifacts","tests","axproj",Guid.NewGuid().ToString("N")));
    public AxiomProjectTests(){Directory.CreateDirectory(root);Write("main.axm","PRINT 42");}
    private string Write(string name,string text){var file=Path.Combine(root,name);Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,text);return file;}
    private string Project(string? entry="main.axm",string[]? sources=null,string[]? assets=null,string[]? exclude=null) => Write("game.axproj",JsonSerializer.Serialize(new {formatVersion=1,name="Game",entry,sources=sources??["**/*.axm"],assets=assets??[],exclude=exclude??[]}));
    [Fact] public void IncludesSourcesAndAssetsWithPortablePathsAndExclusions(){
        Write("nested/helper.axm","PRINT 1");Write("skip.axm","PRINT missing");Write("art/pixel.txt","asset");Write("obj/hidden.axm","PRINT missing");
        var project=AxiomProject.Load(Project(assets:["art/**"],exclude:["skip.axm"]));
        Assert.Equal("Game",project.Name);Assert.Equal(2,project.SourceFiles.Length);Assert.Single(project.AssetFiles);Assert.Null(project.EntryError);
        Assert.DoesNotContain(project.SourceFiles,p=>p.Contains("hidden"));
    }
    [Theory][InlineData("../main.axm")][InlineData("/main.axm")][InlineData("C:/main.axm")][InlineData("nested/../main.axm")][InlineData("nested\\main.axm")]
    public void RejectsPathsOutsideContract(string entry){Assert.Throws<ArgumentException>(()=>AxiomProject.Load(Project(entry)));}
    [Fact] public void UnknownVersionAndDuplicatePropertiesAreRejected(){
        Assert.Throws<ArgumentException>(()=>AxiomProject.Load(Write("game.axproj","{\"formatVersion\":2}")));
        Assert.Throws<ArgumentException>(()=>AxiomProject.Load(Write("game.axproj","{\"formatVersion\":1,\"formatVersion\":1}")));
    }
    [Fact] public void MissingEntryCanBeRepairedButCannotRun(){var project=AxiomProject.Load(Project("absent.axm"));Assert.NotNull(project.EntryError);Assert.Throws<ArgumentException>(project.RequireEntry);}
    [Fact] public void ProjectCheckFindsErrorsInUnimportedSource(){
        var spare=Write("spare.axm","PRINT missing");var project=AxiomProject.Load(Project());
        Assert.Contains(project.Check(),d=>d.Severity==DiagnosticSeverity.Error&&d.Location.Document==spare);
    }
    [Fact] public void ExcludedImportCannotBypassMembership(){
        Write("main.axm","IMPORT helper\nPRINT value");Write("helper.axm","PUBLIC DIM value AS INTEGER=7");
        var project=AxiomProject.Load(Project(exclude:["helper.axm"]));Assert.Throws<ArgumentException>(()=>project.Check());
    }
    [Fact] public void UnsavedModuleOverridesDiskAndUnrelatedTopLevelIsNotExecuted(){
        var entry=Write("main.axm","IMPORT helper\nPRINT value");var helper=Write("helper.axm","PRINT missing");Write("spare.axm","PRINT 99");
        var project=AxiomProject.Load(Project());var dependencies=project.Dependencies(File.ReadAllText(entry),entry,[new(helper,"PUBLIC DIM value AS INTEGER=7")]);
        var result=new AxiomExecutionPipeline().Run(new ExecutionRequest(File.ReadAllText(entry),sourceName:entry,additionalDocuments:dependencies));
        Assert.Equal(ExecutionStatus.Success,result.Status);Assert.Equal(["7"],result.Output);
    }
    [Fact] public void RootGlobAndRecursiveGlobHaveDifferentMeaning(){
        Write("nested/spare.axm","PRINT 1");Assert.Single(AxiomProject.Load(Project(sources:["*.axm"])).SourceFiles);
        Assert.Equal(2,AxiomProject.Load(Project()).SourceFiles.Length);
    }
    public void Dispose()=>Directory.Delete(root,true);
}
