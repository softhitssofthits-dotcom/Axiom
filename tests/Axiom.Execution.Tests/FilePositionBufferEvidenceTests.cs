using System.Text;

namespace Axiom.Execution.Tests;

public sealed class FilePositionBufferEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var encoding in new[] { "UTF-8", "UTF-16LE", "UTF-16BE" })
        foreach (var prefix in new[] { 0, 127, 128, 254, 255, 256 })
            yield return [mode, encoding, prefix];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ScalarReadAcrossBufferPreservesBytePositionsAndLineBoundary(
        ExecutionMode mode, string encodingName, int prefixLength)
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
            var prefix = new string('A', prefixLength);
            File.WriteAllBytes(path, encoding.GetPreamble().Concat(encoding.GetBytes(prefix + "😀\r\nБ")).ToArray());
            // Ожидания заданы по формату: ASCII 1/2 байта, emoji 4,
            // CRLF 2/4, кириллица 2; BOM исключён из пользовательских позиций.
            var unit = encodingName == "UTF-8" ? 1 : 2;
            var afterEmoji = prefixLength * unit + 4;
            var afterLine = afterEmoji + 2 * unit;
            var length = afterLine + 2;
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path.Replace("\"", "\"\"")}}" FOR INPUT AS f USING "{{encodingName}}"
                PRINT FILELENGTH(f)
                PRINT FILEPOS(f)
                PRINT READTEXT(f, {{prefixLength + 1}}) = "{{prefix}}😀"
                PRINT FILEPOS(f)
                PRINT READLINE(f) = ""
                PRINT FILEPOS(f)
                PRINT LINENUMBER(f)
                PRINT READTEXT(f, 1) = "Б"
                PRINT FILEPOS(f)
                PRINT EOF(f)
                TRY
                    PRINT READLINE(f)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT READTEXT(f, 1) = ""
                PRINT FILEPOS(f)
                TRY
                    PRINT SEEKFILE(f, -1)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT FILEPOS(f)
                TRY
                    PRINT SEEKFILE(f, {{length + 1}})
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT FILEPOS(f)
                PRINT REWIND(f)
                PRINT FILEPOS(f)
                PRINT EOF(f)
                PRINT READTOEND(f) = "{{prefix}}😀" + CHRW(13) + CHRW(10) + "Б"
                PRINT FILEPOS(f)
                CLOSE f
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(result.Status == ExecutionStatus.Success,
                string.Join(" | ", result.Diagnostics.Select(d => d.ToString())) + " OUTPUT: " + string.Join(" | ", result.Output));
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(new[] {
                length.ToString(), "0", "TRUE", afterEmoji.ToString(), "TRUE", afterLine.ToString(), "2",
                "TRUE", length.ToString(), "TRUE", "MB3007", "TRUE", length.ToString(),
                "MB3007", length.ToString(), "MB3007", length.ToString(), "TRUE", "0", "FALSE", "TRUE", length.ToString()
            }, result.Output);
        }
        finally { File.Delete(path); }
    }
}
