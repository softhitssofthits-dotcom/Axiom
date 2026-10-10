namespace Axiom.Execution.Tests;

public sealed class PathEnvironmentEvidenceTests
{
    [Fact]
    public void EnvironmentSampleDoesNotCreateTargetFile()
    {
        var target = Environment.ExpandEnvironmentVariables("%TEMP%\\данные.txt");
        var expectedPath = Path.GetFullPath(target);
        var existed = File.Exists(expectedPath);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            TestProgramLoader.LoadText("samples/functions/isabsolutepath/environment_expansion.axm")));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[]
        {
            "Соединение без раскрытия: %TEMP%\\данные.txt",
            "Полный путь: " + expectedPath,
            "Абсолютность после раскрытия: " + (Path.IsPathFullyQualified(target) ? "TRUE" : "FALSE")
        }, result.Output);
        Assert.Equal(existed, File.Exists(expectedPath));
    }

    [Fact]
    public void KnownVariableResolvesRealFileWhileCombineRemainsLexical()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "path-environment-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var name = "AXIOM_AUDIT_" + Guid.NewGuid().ToString("N");
        var file = Path.Combine(root, "данные😀.txt");
        File.WriteAllText(file, "Проверенные данные😀");
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, root);
            var source = $$"""
                DIM path AS STRING = "%{{name}}%\данные😀.txt"
                PRINT FULLPATH(path) = "{{file}}"
                PRINT READFILE(path)
                PRINT FILEEXISTS(path)
                PRINT FILENAME(path)
                PRINT DIRNAME(path) = "{{root}}"
                PRINT EXTNAME(path)
                PRINT ISABSOLUTEPATH(path)
                PRINT COMBINEPATH("%{{name}}%", "данные😀.txt") = path
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(new[] { "TRUE", "Проверенные данные😀", "TRUE", "данные😀.txt", "TRUE", ".txt", "TRUE", "TRUE" }, result.Output);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
            File.Delete(file);
            Directory.Delete(root);
        }
    }

    [Fact]
    public void UnknownVariableRemainsLiteralAndRelative()
    {
        var name = "AXIOM_UNKNOWN_" + Guid.NewGuid().ToString("N");
        Assert.Null(Environment.GetEnvironmentVariable(name));
        var root = Path.GetFullPath(Path.Combine(".artifacts", "path-environment-base-" + Guid.NewGuid().ToString("N")));
        var path = $"%{name}%\\данные.txt";
        var expected = root + "\\" + path;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            PRINT FULLPATH("{{path}}") = "{{expected}}"
            PRINT ISABSOLUTEPATH("{{path}}")
            PRINT COMBINEPATH("%{{name}}%", "данные.txt") = "{{path}}"
            """, resourceBaseDirectory: root));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "TRUE", "FALSE", "TRUE" }, result.Output);
        Assert.False(Directory.Exists(root));
    }
}
