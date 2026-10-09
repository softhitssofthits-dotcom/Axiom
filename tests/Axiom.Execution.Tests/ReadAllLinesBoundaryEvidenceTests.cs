namespace Axiom.Execution.Tests;

public sealed class ReadAllLinesBoundaryEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CarriageReturnAcceptanceSampleRuns(ExecutionMode mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Axiom.sln"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var source = File.ReadAllText(Path.Combine(repository.FullName, "samples/functions/readalllines/carriage_return.axm"));
        var root = Path.GetFullPath(Path.Combine(".artifacts", "read-all-lines-sample-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(["TRUE", "TRUE", "3", "А/Б/В", "TRUE"], result.Output);
            Assert.Empty(Directory.GetFiles(Path.Combine(root, ".artifacts")));
        }
        finally
        {
            var path = Path.Combine(root, ".artifacts", "readalllines_carriage_return.txt");
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(Path.Combine(root, ".artifacts"))) Directory.Delete(Path.Combine(root, ".artifacts"));
            Directory.Delete(root);
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            yield return [mode, Array.Empty<byte>(), "0|TRUE|1|0|"];
            yield return [mode, new byte[] { 10 }, "1|TRUE|1|1|"];
            yield return [mode, new byte[] { 13, 10, 13, 10 }, "2|TRUE|1|2|/"];
            yield return [mode, new byte[] { 0xD0, 0x90, 0, 0xF0, 0x9F, 0x98, 0x80, 13, 0x65, 0xCC, 0x81, 10 }, "2|TRUE|1|2|А\0😀/é"];
            yield return [mode, new byte[] { 13 }, "1|TRUE|1|1|"];
            yield return [mode, new byte[] { 13, 13 }, "2|TRUE|1|2|/"];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LiteralBytesDetermineLineShapeAndKeepFileUnchanged(ExecutionMode mode, byte[] bytes, string expected)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "read-all-lines-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "строки😀.txt");
        File.WriteAllBytes(path, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM lines() AS STRING
                lines = READALLLINES("{{path}}")
                PRINT ARRAYLENGTH(lines)
                PRINT ARRAYISALLOCATED(lines)
                PRINT LBOUND(lines)
                PRINT UBOUND(lines)
                PRINT JOIN(lines, "/")
                TRY
                    lines = READALLLINES("{{path}}.missing")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT JOIN(lines, "/")
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(expected.Split('|').Concat(["MB3007", expected[(expected.LastIndexOf('|') + 1)..]]), result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            Assert.False(File.Exists(path + ".missing"));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
