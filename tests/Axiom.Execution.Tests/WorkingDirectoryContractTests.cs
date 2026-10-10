namespace Axiom.Execution.Tests;

public sealed class WorkingDirectoryContractTests
{
    [Fact]
    public void RunCompiled_ReusedProgramDoesNotReuseWorkingDirectory()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "directory-cache-" + Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "first");
        var second = Path.Combine(root, "second");
        Directory.CreateDirectory(Path.Combine(first, "nested"));
        Directory.CreateDirectory(Path.Combine(second, "nested"));
        const string source = "PRINT CURRENTDIR()\nPRINT CHANGEDIR(\"nested\")\nPRINT CURRENTDIR()";
        try
        {
            var pipeline = new AxiomExecutionPipeline();
            var compilation = pipeline.Compile(new CompilationRequest(source));
            Assert.True(compilation.Succeeded);
            foreach (var directory in new[] { first, second })
            {
                var result = pipeline.RunCompiled(compilation.CompiledProgram!, new ExecutionRequest(source, resourceBaseDirectory: directory));
                Assert.Equal(ExecutionStatus.Success, result.Status);
                Assert.Equal([directory, "TRUE", Path.Combine(directory, "nested")], result.Output);
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void PausedRun_KeepsItsDirectoryAfterAnotherProgramRuns()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "directory-debug-" + Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "first");
        var second = Path.Combine(root, "second");
        Directory.CreateDirectory(Path.Combine(first, "nested"));
        Directory.CreateDirectory(Path.Combine(second, "nested"));
        try
        {
            var pipeline = new AxiomExecutionPipeline();
            var paused = pipeline.Run(new ExecutionRequest(
                "PRINT CHANGEDIR(\"nested\")\nPRINT WRITEFILE(\"resumed.txt\", \"Продолжено\")",
                sourceBreakpointLines: [2], resourceBaseDirectory: first));
            Assert.True(paused.Stopped);
            var other = pipeline.Run(new ExecutionRequest("PRINT CHANGEDIR(\"nested\")", resourceBaseDirectory: second));
            Assert.Equal(ExecutionStatus.Success, other.Status);
            Assert.True(paused.StopState!.Resume().Succeeded);
            Assert.Equal("Продолжено", File.ReadAllText(Path.Combine(first, "nested", "resumed.txt")));
            Assert.False(File.Exists(Path.Combine(second, "nested", "resumed.txt")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Run_UsesProgramDirectoryAndChangesItWithoutChangingProcessDirectory()
    {
        var processDirectory = Environment.CurrentDirectory;
        var root = Path.Combine(AppContext.BaseDirectory, "working-directory-" + Guid.NewGuid().ToString("N"));
        var prefix = "directory-recheck-" + Guid.NewGuid().ToString("N");
        var beforeName = $".artifacts/{prefix}-before.txt";
        var afterName = $".artifacts/{prefix}-after.txt";
        var chunkName = $".artifacts/{prefix}-chunk.txt";
        Directory.CreateDirectory(Path.Combine(root, "nested"));
        Directory.CreateDirectory(Path.Combine(root, ".artifacts"));
        Directory.CreateDirectory(Path.Combine(root, "nested", ".artifacts"));
        Directory.CreateDirectory(Path.Combine(processDirectory, ".artifacts"));
        try
        {
            var source = $$"""
                PRINT CURRENTDIR() = "{{root}}"
                PRINT WRITEFILE("{{beforeName}}", "До перехода")
                PRINT CHANGEDIR("nested")
                PRINT CURRENTDIR() = "{{root}}\nested"
                PRINT WRITEFILE("{{afterName}}", "После перехода")
                DIM f AS FILE
                OPEN "{{chunkName}}" FOR OUTPUT AS f
                PRINT WRITE(f, "😀")
                CLOSE f
                TRY
                    PRINT CHANGEDIR("missing")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT CURRENTDIR() = "{{root}}\nested"
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "MB3007", "TRUE"], result.Output);
            Assert.Equal(processDirectory, Environment.CurrentDirectory);
            Assert.Equal("До перехода", File.ReadAllText(Path.Combine(root, beforeName)));
            Assert.Equal("После перехода", File.ReadAllText(Path.Combine(root, "nested", afterName)));
            Assert.Equal("😀", File.ReadAllText(Path.Combine(root, "nested", chunkName)));
            var nextRun = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT CURRENTDIR()", resourceBaseDirectory: root));
            Assert.Equal([root], nextRun.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            foreach (var name in new[] { beforeName, afterName, chunkName })
            {
                var path = Path.Combine(processDirectory, name);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
