namespace Axiom.Execution.Tests;

public sealed class ReadLinesLargeCountEvidenceTests
{
    [Fact]
    public void MaximumCountSampleRuns()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "readlines-sample-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Axiom.sln"))) repository = repository.Parent;
            Assert.NotNull(repository);
            var source = File.ReadAllText(Path.Combine(repository.FullName, "samples/functions/readlines/maximum_count.axm"));
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { "TRUE", "TRUE", "Прочитано строк: 2", "Первая строка", "Вторая строка", "TRUE" }, result.Output);
            Assert.Empty(Directory.GetFiles(Path.Combine(root, ".artifacts")));
        }
        finally
        {
            var file = Path.Combine(root, ".artifacts", "readlines_maximum_count.txt");
            if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(Path.Combine(root, ".artifacts"))) Directory.Delete(Path.Combine(root, ".artifacts"));
            Directory.Delete(root);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaximumCountReadsOnlyActualRows(bool oneLine)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "readlines-large-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "строка.txt");
        var bytes = oneLine ? new byte[] { 0xD0, 0x90 } : Array.Empty<byte>();
        File.WriteAllBytes(path, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                DIM lines() AS STRING
                OPEN "{{path}}" FOR INPUT AS f USING "UTF-8"
                lines = READLINES(f, 2147483647)
                PRINT ARRAYLENGTH(lines)
                IF ARRAYLENGTH(lines) > 0 THEN
                    PRINT lines(1)
                END IF
                PRINT EOF(f)
                CLOSE f
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(oneLine ? new[] { "1", "А", "TRUE" } : new[] { "0", "TRUE" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
