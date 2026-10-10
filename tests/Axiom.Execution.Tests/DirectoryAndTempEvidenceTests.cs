namespace Axiom.Execution.Tests;

public sealed class DirectoryAndTempEvidenceTests
{
    [Fact]
    public void ChangeDirIsHostLocalAndFailurePreservesBase()
    {
        var processDirectory = Environment.CurrentDirectory;
        var root = Path.Combine(Path.GetTempPath(), "axiom-cwd-" + Guid.NewGuid().ToString("N"));
        var child = Path.Combine(root, "папка😀");
        Directory.CreateDirectory(child);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT CURRENTDIR() = "{{root}}"
                PRINT CHANGEDIR("папка😀")
                PRINT CURRENTDIR() = "{{child}}"
                PRINT WRITEFILE("data.txt", "Сохранить😀")
                TRY
                    PRINT CHANGEDIR("missing")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT CURRENTDIR() = "{{child}}"
                PRINT READFILE("data.txt")
                """, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { "TRUE", "TRUE", "TRUE", "TRUE", "MB3007", "TRUE", "Сохранить😀" }, result.Output);
            Assert.Equal("Сохранить😀", File.ReadAllText(Path.Combine(child, "data.txt")));
            Assert.False(File.Exists(Path.Combine(root, "data.txt")));
            Assert.Equal(processDirectory, Environment.CurrentDirectory);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TemporaryFunctionsCreateDistinctEmptyObjectsWithPrefix()
    {
        var prefix = "axiom_" + Guid.NewGuid().ToString("N") + "_";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            PRINT MAKETEMPFILE("{{prefix}}")
            PRINT MAKETEMPFILE("{{prefix}}")
            PRINT MAKETEMPDIR("{{prefix}}")
            PRINT MAKETEMPDIR("{{prefix}}")
            TRY
                PRINT MAKETEMPFILE("sub/prefix")
            CATCH err
                PRINT err.Code
            END TRY
            TRY
                PRINT MAKETEMPDIR("sub/prefix")
            CATCH err
                PRINT err.Code
            END TRY
            """));
        var paths = result.Output.Take(4).ToArray();
        try
        {
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(6, result.Output.Count);
            Assert.Equal(4, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(new[] { "MB3007", "MB3007" }, result.Output.Skip(4));
            foreach (var path in paths)
            {
                Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(path));
                Assert.StartsWith(prefix, Path.GetFileName(path));
            }
            foreach (var path in paths.Take(2)) Assert.Empty(File.ReadAllBytes(path));
            foreach (var path in paths.Skip(2)) Assert.Empty(Directory.EnumerateFileSystemEntries(path));
        }
        finally
        {
            foreach (var path in paths)
                if (Path.GetDirectoryName(path) == Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) && Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
                {
                    if (File.Exists(path)) File.Delete(path);
                    if (Directory.Exists(path)) Directory.Delete(path);
                }
        }
    }
}
