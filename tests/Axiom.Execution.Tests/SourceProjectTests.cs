namespace Axiom.Execution.Tests;

public sealed class SourceProjectTests
{
    [Fact]
    public void OpenDocumentsOverrideDiskAndProvideTransitiveImports()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "project-snapshots", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = Path.Combine(directory, "main.axm");
            var helper = Path.Combine(directory, "helper.axm");
            var nested = Path.Combine(directory, "nested.axm");
            File.WriteAllText(helper, "PRINT missing");
            File.WriteAllText(nested, "PRINT 1");
            SourceDocument[] open = [new(helper,"IMPORT nested\nPUBLIC DIM value AS INTEGER=42"), new(nested,"PRINT 2"),
                new(Path.Combine(directory,"unrelated.axm"),"PRINT missing"),
                new(Path.Combine(directory,"other","helper.axm"),"PRINT missing")];
            var documents = SourceProject.LoadDependencies("IMPORT HeLpEr\nPRINT value",entry,open);
            Assert.Equal(2,documents.Count);
            Assert.Equal(open[0].Text,documents[0].Text);
            Assert.Equal("PRINT 2",documents[1].Text);
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT helper\nPRINT value",sourceName:entry,additionalDocuments:documents));
            Assert.Equal(ExecutionStatus.Success,run.Status);
            Assert.Equal(["2","42"],run.Output);
            Assert.Equal("PRINT missing",File.ReadAllText(helper));
        }
        finally { Directory.Delete(directory,recursive:true); }
    }

    [Fact]
    public void NamedOpenDocumentMayBeMissingOnDiskButCannotReplaceTheEntry()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "project-snapshots", Guid.NewGuid().ToString("N")));
        var entry = Path.Combine(directory,"main.axm");
        var documents = SourceProject.LoadDependencies("IMPORT helper",entry,
            [new SourceDocument(Path.Combine(directory,"helper.axm"),"PUBLIC DIM value AS INTEGER=9"),new SourceDocument(entry,"IMPORT other")]);
        Assert.Single(documents);
        Assert.Equal("helper",documents[0].ModuleName);
    }

    [Fact]
    public void ImportSpellingDoesNotChangeThePhysicalDocumentIdentity()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "modules", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var helper = Path.Combine(directory, "helper.axm");
            File.WriteAllText(helper, "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION");
            var documents = SourceProject.LoadDependencies("IMPORT HeLpEr\nPRINT Answer()", Path.Combine(directory, "main.axm"));
            Assert.Equal(helper, Assert.Single(documents).Name);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("IMPORT HeLpEr\nPRINT Answer()",
                sourceName:Path.Combine(directory,"main.axm"), additionalDocuments:documents,
                sourceBreakpoints:[new Axiom.Runtime.BytecodeSourceBreakpoint(2,document:helper)]));
            Assert.True(result.Stopped);
            Assert.Equal(helper,result.StoppedAt!.SourceLocation.Document);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void LoadsOnlyImportedDependenciesAndRefreshesTheirText()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "modules", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "helper.axm"), "PUBLIC DIM value AS INTEGER=1");
            File.WriteAllText(Path.Combine(directory, "unrelated.axm"), "PRINT missing");
            var entry = Path.Combine(directory, "main.axm");
            var first = SourceProject.LoadDependencies("IMPORT helper\nPRINT value", entry);
            Assert.Single(first);
            Assert.Contains("=1", first[0].Text);
            File.WriteAllText(Path.Combine(directory, "helper.axm"), "PUBLIC DIM value AS INTEGER=2");
            var second = SourceProject.LoadDependencies("IMPORT helper\nPRINT value", entry);
            Assert.Contains("=2", Assert.Single(second).Text);
            Assert.Empty(SourceProject.LoadDependencies("PRINT 1", entry));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void ResolvesTransitiveImportsOnceAndLeavesMissingModulesToTheBinder()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "modules", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "first.axm"), "IMPORT second\nPRINT 1");
            File.WriteAllText(Path.Combine(directory, "second.axm"), "IMPORT first\nIMPORT missing\nPRINT 2");
            var documents = SourceProject.LoadDependencies("IMPORT first", Path.Combine(directory, "main.axm"));
            Assert.Equal(2, documents.Count);
            Assert.Equal(2, documents.Select(d => d.Name).Distinct().Count());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
