namespace Axiom.Execution.Tests;

public sealed class RootDirectoryLinkEvidenceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExplicitDanglingRootLinkIsResolvedBeforeCopy(bool sourceLink)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-root-link-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        var target = Path.Combine(root, "target");
        var alias = Path.Combine(root, "alias");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(target);
        try
        {
            File.WriteAllText(Path.Combine(source, "data.txt"), "Источник");
            DirectoryTraversalContractTests.CreateDirectoryLink(alias, target);
            Directory.Delete(target);
            var from = sourceLink ? alias : source;
            var to = sourceLink ? destination : alias;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT COPYDIR("{{from}}", "{{to}}")
                CATCH err
                    PRINT err.Code
                END TRY
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.NotNull(new DirectoryInfo(alias).LinkTarget);
            Assert.Equal("Источник", File.ReadAllText(Path.Combine(source, "data.txt")));
            if (sourceLink)
            {
                Assert.Equal(["MB3007"], result.Output);
                Assert.False(Directory.Exists(target));
                Assert.True(Directory.Exists(destination));
                Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
            }
            else
            {
                Assert.Equal(["TRUE"], result.Output);
                Assert.Equal("Источник", File.ReadAllText(Path.Combine(target, "data.txt")));
            }
        }
        finally
        {
            if (new DirectoryInfo(alias).LinkTarget is not null) Directory.Delete(alias);
            Directory.Delete(root, true);
        }
    }
}
