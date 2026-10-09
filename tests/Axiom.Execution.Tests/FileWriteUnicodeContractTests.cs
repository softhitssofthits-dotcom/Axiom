using System.Text;

namespace Axiom.Execution.Tests;

public sealed class FileWriteUnicodeContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void Write_SplitSurrogatesHaveStableBytesAndPositions(ExecutionMode mode, bool flushBetween)
    {
        var path = Path.GetTempFileName();
        try
        {
            var flush = flushBetween ? "PRINT FLUSH(f)" : "";
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR OUTPUT AS f
                PRINT WRITE(f, LEFT("😀", 1))
                PRINT FILEPOS(f)
                {{flush}}
                PRINT WRITE(f, RIGHT("😀", 1))
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                PRINT READFILE("{{path}}") = "��"
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(flushBetween
                ? ["TRUE", "3", "TRUE", "TRUE", "6", "6", "TRUE"]
                : new[] { "TRUE", "3", "TRUE", "6", "6", "TRUE" }, result.Output);
            Assert.Equal(Encoding.UTF8.GetBytes("��"), File.ReadAllBytes(path));
            Assert.False(result.UsedInterpreterFallback);
        }
        finally { File.Delete(path); }
    }
}
