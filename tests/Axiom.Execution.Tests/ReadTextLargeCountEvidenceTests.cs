namespace Axiom.Execution.Tests;

public sealed class ReadTextLargeCountEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MaximumCountSampleRuns(ExecutionMode mode)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "readtext-sample-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Axiom.sln"))) repository = repository.Parent;
            Assert.NotNull(repository);
            var source = File.ReadAllText(Path.Combine(repository.FullName, "samples/functions/readtext/maximum_count.axm"));
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(new[] { "TRUE", "TRUE", "А😀", "Прочитано байт: 6", "TRUE" }, result.Output);
            Assert.Empty(Directory.GetFiles(Path.Combine(root, ".artifacts")));
        }
        finally
        {
            var file = Path.Combine(root, ".artifacts", "readtext_maximum_count.txt");
            if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(Path.Combine(root, ".artifacts"))) Directory.Delete(Path.Combine(root, ".artifacts"));
            Directory.Delete(root);
        }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void MaximumCountReadsOnlyExistingText(ExecutionMode mode, bool hasText)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "readtext-large-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "текст.txt");
        byte[] bytes = hasText ? [0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80] : [];
        File.WriteAllBytes(path, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR INPUT AS f USING "UTF-8"
                PRINT READTEXT(f, 2147483647)
                PRINT FILEPOS(f)
                PRINT EOF(f)
                CLOSE f
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(hasText ? new[] { "А😀", "6", "TRUE" } : new[] { "", "0", "TRUE" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
