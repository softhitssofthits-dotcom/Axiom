namespace Axiom.Execution.Tests;

public sealed class FileLineEndingContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void WriteLineAndNewLine_ProduceIdenticalLfBytes(ExecutionMode mode)
    {
        var path = Path.GetTempFileName();
        try
        {
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path.Replace("\"", "\"\"")}}" FOR OUTPUT AS f
                PRINT WRITELINE(f, "А")
                PRINT FILEPOS(f)
                PRINT NEWLINE(f)
                PRINT FILELENGTH(f)
                CLOSE f
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["TRUE", "3", "TRUE", "4"], result.Output);
            Assert.Equal(new byte[] { 0xD0, 0x90, 0x0A, 0x0A }, File.ReadAllBytes(path));
            Assert.False(result.UsedInterpreterFallback);
        }
        finally { File.Delete(path); }
    }
}
