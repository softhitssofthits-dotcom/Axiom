namespace Axiom.Execution.Tests;

public sealed class FileLineEndingContractTests
{
    [Fact]
    public void WriteLineAndNewLine_ProduceIdenticalLfBytes()
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
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["TRUE", "3", "TRUE", "4"], result.Output);
            Assert.Equal(new byte[] { 0xD0, 0x90, 0x0A, 0x0A }, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
