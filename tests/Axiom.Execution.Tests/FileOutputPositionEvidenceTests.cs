using System.Text;

namespace Axiom.Execution.Tests;

public sealed class FileOutputPositionEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var encoding in new[] { "UTF-8", "UTF-16LE", "UTF-16BE" })
        foreach (var flush in new[] { false, true })
            yield return [encoding, flush];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void OverwritePreservesTailAndAppendRejectsSeek(
        string name, bool flushBeforeSeek)
    {
        var path = Path.GetTempFileName();
        try
        {
            Encoding encoding = name switch
            {
                "UTF-8" => new UTF8Encoding(false, true),
                "UTF-16LE" => new UnicodeEncoding(false, true, true),
                _ => new UnicodeEncoding(true, true, true)
            };
            var width = name == "UTF-8" ? 1 : 2;
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR OUTPUT AS f USING "{{name}}"
                PRINT WRITE(f, "A😀BC")
                PRINT FILEPOS(f)
                {{(flushBeforeSeek ? "PRINT FLUSH(f)" : "")}}
                PRINT SEEKFILE(f, {{width + 4}})
                PRINT WRITE(f, "Z")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                PRINT FLUSH(f)
                PRINT REWIND(f)
                PRINT WRITE(f, "Q")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                OPEN "{{path}}" FOR APPEND AS f USING "{{name}}"
                PRINT FILEPOS(f)
                TRY
                    PRINT SEEKFILE(f, 0)
                CATCH err
                    PRINT err.Code
                END TRY
                TRY
                    PRINT REWIND(f)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT FILEPOS(f)
                PRINT WRITE(f, "Я")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                PRINT NEWLINE(f)
                PRINT WRITELINE(f, "")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                PRINT READFILE("{{path}}", "{{name}}") = "Q😀ZCЯ" + CHRW(10) + CHRW(10)
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(result.Status == ExecutionStatus.Success, string.Join(" | ", result.Diagnostics));
            Assert.Empty(result.Diagnostics);
            var originalLength = 3 * width + 4;
            var expected = new List<string> { "TRUE", originalLength.ToString() };
            if (flushBeforeSeek) expected.Add("TRUE");
            expected.AddRange(["TRUE", "TRUE", (2 * width + 4).ToString(), originalLength.ToString(), "TRUE",
                "TRUE", "TRUE", width.ToString(), originalLength.ToString(), originalLength.ToString(),
                "MB3007", "MB3007", originalLength.ToString(), "TRUE", (originalLength + 2).ToString(),
                (originalLength + 2).ToString(), "TRUE", "TRUE", (originalLength + 2 + 2 * width).ToString(),
                (originalLength + 2 + 2 * width).ToString(), "TRUE"]);
            Assert.Equal(expected, result.Output);
            Assert.Equal(encoding.GetPreamble().Concat(encoding.GetBytes("Q😀ZCЯ\n\n")).ToArray(), File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
