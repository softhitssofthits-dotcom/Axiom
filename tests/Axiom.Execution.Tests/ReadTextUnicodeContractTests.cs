using System.Text;

namespace Axiom.Execution.Tests;

public sealed class ReadTextUnicodeContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    public void ReadText_EmojiAdvancesExactlyFourBytesAndRewindWorks(ExecutionMode mode, bool bom)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "😀А", new UTF8Encoding(bom));
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path.Replace("\"", "\"\"")}}" FOR INPUT AS f
                PRINT READTEXT(f, 0)
                PRINT FILEPOS(f)
                TRY
                    PRINT READTEXT(f, 1)
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT "Завершено"
                END TRY
                PRINT FILEPOS(f)
                PRINT EOF(f)
                PRINT READTEXT(f, 1)
                PRINT FILEPOS(f)
                PRINT EOF(f)
                PRINT REWIND(f)
                PRINT READTEXT(f, 2)
                PRINT SEEKFILE(f, 1)
                TRY
                    PRINT READTEXT(f, 1)
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT FILEPOS(f)
                END TRY
                PRINT SEEKFILE(f, 4)
                PRINT READTEXT(f, 1)
                CLOSE f
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["", "0", "😀", "Завершено", "4", "FALSE", "А", "6", "TRUE", "TRUE", "😀А", "TRUE", "MB3007", "1", "TRUE", "А"], result.Output);
            Assert.False(result.UsedInterpreterFallback);
        }
        finally { File.Delete(path); }
    }
}
