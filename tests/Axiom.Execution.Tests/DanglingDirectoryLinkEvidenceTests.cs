namespace Axiom.Execution.Tests;

public sealed class DanglingDirectoryLinkEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void DanglingFileLinksAreSkippedInSourceAndRejectedInDestination(ExecutionMode mode, bool sourceLink)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-dangling-file-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        var target = Path.Combine(root, "missing.txt");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(destination);
        var link = Path.Combine(sourceLink ? source : destination, "data.txt");
        try
        {
            File.CreateSymbolicLink(link, target);
            Assert.NotNull(new FileInfo(link).LinkTarget);
            if (!sourceLink) File.WriteAllText(Path.Combine(source, "data.txt"), "Источник");
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT DIRSIZE("{{source}}")
                TRY
                    PRINT COPYDIR("{{source}}", "{{destination}}", TRUE)
                CATCH err
                    PRINT err.Code
                END TRY
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(sourceLink ? ["0", "TRUE"] : new[] { "16", "MB3007" }, result.Output);
            Assert.False(File.Exists(target));
            Assert.NotNull(new FileInfo(link).LinkTarget);
            if (sourceLink) Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
            else Assert.Equal("Источник", File.ReadAllText(Path.Combine(source, "data.txt")));
        }
        finally
        {
            File.Delete(link);
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CopyRejectsDanglingNestedDestinationWithoutRecreatingTarget(ExecutionMode mode)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-dangling-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        var target = Path.Combine(root, "outside");
        var link = Path.Combine(destination, "child");
        Directory.CreateDirectory(Path.Combine(source, "child"));
        Directory.CreateDirectory(destination);
        Directory.CreateDirectory(target);
        try
        {
            File.WriteAllText(Path.Combine(source, "child", "data.txt"), "Источник");
            DirectoryTraversalContractTests.CreateDirectoryLink(link, target);
            Directory.Delete(target);
            Assert.False(Directory.Exists(target));
            Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT COPYDIR("{{source}}", "{{destination}}", TRUE)
                CATCH err
                    PRINT err.Code
                END TRY
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(["MB3007"], result.Output);
            Assert.False(Directory.Exists(target));
            Assert.Equal("Источник", File.ReadAllText(Path.Combine(source, "child", "data.txt")));
            Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
        }
        finally
        {
            // Удаляем саму созданную ссылку, даже если её цель уже отсутствует.
            if (new DirectoryInfo(link).LinkTarget is not null) Directory.Delete(link);
            Directory.Delete(root, true);
        }
    }
}
