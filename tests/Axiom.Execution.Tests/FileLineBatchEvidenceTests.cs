using System.Text;

namespace Axiom.Execution.Tests;

public sealed class FileLineBatchEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var encoding in new[] { "UTF-8", "UTF-16LE", "UTF-16BE" }) yield return [mode, encoding];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EmptyLineBatchSkipAndEofHaveDistinctResults(ExecutionMode mode, string encodingName)
    {
        var path = Path.GetTempFileName();
        try
        {
            Encoding encoding = encodingName switch
            {
                "UTF-8" => new UTF8Encoding(true, true),
                "UTF-16LE" => new UnicodeEncoding(false, true, true),
                _ => new UnicodeEncoding(true, true, true)
            };
            byte[] bytes = encoding.GetPreamble().Concat(encoding.GetBytes("😀\r\n\r\nЯ\nБ")).ToArray();
            File.WriteAllBytes(path, bytes);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                DIM lines() AS STRING
                OPEN "{{path}}" FOR INPUT AS f USING "{{encodingName}}"
                PRINT FILEPATH(f) = "{{path}}"
                PRINT FILEMODE(f)
                lines = READLINES(f, 0)
                PRINT ARRAYLENGTH(lines)
                PRINT FILEPOS(f)
                lines = READLINES(f, 2)
                PRINT ARRAYLENGTH(lines)
                PRINT lines(1) = "😀"
                PRINT lines(2) = ""
                PRINT LINENUMBER(f)
                PRINT SKIPLINE(f)
                PRINT LINENUMBER(f)
                lines = READLINES(f, 99)
                PRINT ARRAYLENGTH(lines)
                PRINT lines(1) = "Б"
                PRINT EOF(f)
                lines = READLINES(f, 1)
                PRINT ARRAYLENGTH(lines)
                TRY
                    PRINT SKIPLINE(f)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT EOF(f)
                CLOSE f
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(new[] { "TRUE", "INPUT", "0", "0", "2", "TRUE", "TRUE", "3", "TRUE", "4", "1", "TRUE", "TRUE", "0", "MB3007", "TRUE" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
