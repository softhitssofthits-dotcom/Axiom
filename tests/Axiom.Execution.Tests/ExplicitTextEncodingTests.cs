using System.Text;

namespace Axiom.Execution.Tests;

public sealed class ExplicitTextEncodingTests
{
    [Theory]
    [InlineData("UTF-8")]
    [InlineData("WINDOWS-1251")]
    [InlineData("UTF-16LE")]
    [InlineData("UTF-16BE")]
    public void WholeFileFormsRoundTripAndAppendExactlyOnePreamble(string name)
    {
        InDirectory(root =>
        {
            var first = name == "WINDOWS-1251" ? "А" : "А😀";
            var source = $$"""
PRINT WRITEFILE("text.txt", "{{first}}" + CHR(10) + "я", "{{name}}")
PRINT APPENDFILE("text.txt", "!" + CHR(10), "{{name}}")
PRINT READFILE("text.txt", "{{name}}") = "{{first}}" + CHR(10) + "я!" + CHR(10)
DIM lines() AS STRING
lines = READALLLINES("text.txt", "{{name}}")
PRINT ARRAYLENGTH(lines)
PRINT lines(1) = "{{first}}"
PRINT lines(2) = "я!"
""";
            AssertProgram(source, root, ["TRUE", "TRUE", "TRUE", "2", "TRUE", "TRUE"]);
            var encoding = EncodingFor(name);
            Assert.Equal(encoding.GetPreamble().Concat(encoding.GetBytes(first + "\nя!\n")), File.ReadAllBytes(Path.Combine(root, "text.txt")));
        });
    }

    [Fact]
    public void ExplicitAppendCreatesFileAndValidatesExistingPayloadBeforeWriting()
    {
        InDirectory(root =>
        {
            AssertProgram("PRINT APPENDFILE(\"new.txt\", \"А\", \"WINDOWS-1251\")", root, ["TRUE"]);
            Assert.Equal(new byte[] { 0xC0 }, File.ReadAllBytes(Path.Combine(root, "new.txt")));
            File.WriteAllBytes(Path.Combine(root, "bad.txt"), [0xEF, 0xBB, 0xBF, 0xC0]);
            var original = File.ReadAllBytes(Path.Combine(root, "bad.txt"));
            AssertProgram("TRY\nPRINT APPENDFILE(\"bad.txt\", \"я\", \"UTF-8\")\nCATCH err\nPRINT err.Code\nEND TRY", root, ["MB3007"]);
            Assert.Equal(original, File.ReadAllBytes(Path.Combine(root, "bad.txt")));
        });
    }

    [Fact]
    public void InvalidEncodingTextAndConflictingBomLeaveDestinationUnchanged()
    {
        InDirectory(root =>
        {
            var path = Path.Combine(root, "old.txt");
            byte[] original = [0xFF, 0xFE, 0x41, 0x00];
            foreach (var call in new[]
            {
                "WRITEFILE(\"old.txt\", \"текст\", \"UTF-7\")",
                "WRITEFILE(\"old.txt\", \"😀\", \"WINDOWS-1251\")",
                "WRITEFILE(\"old.txt\", LEFT(\"😀\", 1), \"UTF-8\")",
                "APPENDFILE(\"old.txt\", \"я\", \"UTF-8\")",
                "READFILE(\"old.txt\", \"UTF-8\")",
                "READALLLINES(\"old.txt\", \"UTF-16BE\")"
            })
            {
                File.WriteAllBytes(path, original);
                AssertProgram($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", root, ["MB3007"]);
                Assert.Equal(original, File.ReadAllBytes(path));
            }
            AssertProgram("TRY\nPRINT WRITEFILE(\"absent.txt\", \"😀\", \"WINDOWS-1251\")\nCATCH err\nPRINT err.Code\nEND TRY", root, ["MB3007"]);
            Assert.False(File.Exists(Path.Combine(root, "absent.txt")));
        });
    }

    [Theory]
    [InlineData("UTF-8", "C0AF")]
    [InlineData("UTF-16LE", "FFFE41")]
    [InlineData("UTF-16BE", "FEFFD800")]
    public void MalformedInputFailsStrictlyForReadAndAppend(string name, string hex)
    {
        InDirectory(root =>
        {
            var bytes = Convert.FromHexString(hex);
            File.WriteAllBytes(Path.Combine(root, "bad.txt"), bytes);
            foreach (var call in new[] { $"READFILE(\"bad.txt\", \"{name}\")", $"APPENDFILE(\"bad.txt\", \"я\", \"{name}\")" })
                AssertProgram($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", root, ["MB3007"]);
            Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(root, "bad.txt")));
        });
    }

    [Fact]
    public void ExternalBomAndBomlessFilesUseChosenEncodingAndPreservePrefixOnAppend()
    {
        InDirectory(root =>
        {
            foreach (var name in new[] { "UTF-8", "WINDOWS-1251", "UTF-16LE", "UTF-16BE" })
            foreach (var withBom in new[] { false, true })
            {
                var encoding = EncodingFor(name);
                var bom = name == "UTF-8" ? new byte[] { 0xEF, 0xBB, 0xBF } : encoding.GetPreamble();
                var prefix = (withBom ? bom : []).Concat(encoding.GetBytes("А")).ToArray();
                File.WriteAllBytes(Path.Combine(root, "external.txt"), prefix);
                AssertProgram($"PRINT READFILE(\"external.txt\", \"{name}\") = \"А\"\nPRINT APPENDFILE(\"external.txt\", \"я\", \"{name}\")", root, ["TRUE", "TRUE"]);
                Assert.Equal(prefix.Concat(encoding.GetBytes("я")), File.ReadAllBytes(Path.Combine(root, "external.txt")));
            }
            File.WriteAllBytes(Path.Combine(root, "boundary.txt"), new UTF8Encoding(false).GetBytes(new string('a', 4095) + "😀"));
            AssertProgram("PRINT APPENDFILE(\"boundary.txt\", \"я\", \"UTF-8\")", root, ["TRUE"]);
            Assert.Equal(new string('a', 4095) + "😀я", File.ReadAllText(Path.Combine(root, "boundary.txt"), new UTF8Encoding(false, true)));
        });
    }

    [Fact]
    public void Windows1251TreatsBomLikePrefixesAsOrdinaryText()
    {
        InDirectory(root =>
        {
            foreach (var text in new[] { "яюА", "юяА", "п»їА" })
            {
                AssertProgram($"PRINT WRITEFILE(\"prefix.txt\", \"{text}\", \"WINDOWS-1251\")\nPRINT APPENDFILE(\"prefix.txt\", \"я\", \"WINDOWS-1251\")\nPRINT READFILE(\"prefix.txt\", \"WINDOWS-1251\") = \"{text}я\"", root, ["TRUE", "TRUE", "TRUE"]);
                Assert.Equal(EncodingFor("WINDOWS-1251").GetBytes(text + "я"), File.ReadAllBytes(Path.Combine(root, "prefix.txt")));
            }
        });
    }

    [Fact]
    public void OpenChosenEncodingRoundTripsAllModesAndBytePositions()
    {
        InDirectory(root =>
        {
            foreach (var name in new[] { "UTF-8", "WINDOWS-1251", "UTF-16LE", "UTF-16BE" })
            {
                var text = name == "WINDOWS-1251" ? "Ая" : "А😀";
                var bytes = EncodingFor(name).GetBytes(text + "\n!").Length;
                var source = $$"""
DIM f AS FILE
DIM encoding AS STRING = "{{name}}"
OPEN "stream.txt" FOR OUTPUT AS f USING encoding
PRINT FILEPOS(f)
PRINT WRITE(f, "{{text}}")
PRINT WRITELINE(f, "")
CLOSE f
OPEN "stream.txt" FOR APPEND AS f USING encoding
PRINT WRITE(f, "!")
PRINT FILEPOS(f)
CLOSE f
OPEN "stream.txt" FOR INPUT AS f USING encoding
PRINT FILEPOS(f)
PRINT READTEXT(f, 2) = "{{text}}"
PRINT READLINE(f) = ""
PRINT READLINE(f) = "!"
CLOSE f
""";
                AssertProgram(source, root, ["0", "TRUE", "TRUE", "TRUE", bytes.ToString(), "0", "TRUE", "TRUE", "TRUE"]);
                var enc = EncodingFor(name);
                Assert.Equal(enc.GetPreamble().Concat(enc.GetBytes(text + "\n!")), File.ReadAllBytes(Path.Combine(root, "stream.txt")));
            }
        });
    }

    [Fact]
    public void OpenRejectsEncodingAndMalformedInputWithoutChangingFile()
    {
        InDirectory(root =>
        {
            byte[] original = [0xFF, 0xFE, 0x00, 0xD8];
            var path = Path.Combine(root, "bad.txt");
            File.WriteAllBytes(path, original);
            foreach (var form in new[] { "INPUT AS f USING \"UTF-16LE\"", "APPEND AS f USING \"UTF-16LE\"", "OUTPUT AS f USING \"UTF-7\"" })
            {
                AssertProgram($"DIM f AS FILE\nTRY\nOPEN \"bad.txt\" FOR {form}\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ISOPEN(f)", root, ["MB3007", "FALSE"]);
                Assert.Equal(original, File.ReadAllBytes(path));
                using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
        });
    }

    [Theory]
    [InlineData("UTF-16LE")]
    [InlineData("UTF-16BE")]
    public void OpenUtf16SeekLineNumbersAndInvalidReadPositions(string name)
    {
        InDirectory(root =>
        {
            var enc = EncodingFor(name);
            File.WriteAllBytes(Path.Combine(root, "seek.txt"), enc.GetPreamble().Concat(enc.GetBytes("Ċਁ😀\r\nБ\rВ\nГ")).ToArray());
            var source = $$"""
DIM f AS FILE
OPEN "seek.txt" FOR INPUT AS f USING "{{name}}"
PRINT READLINE(f) = "Ċਁ😀"
PRINT FILEPOS(f)
PRINT LINENUMBER(f)
PRINT READLINE(f) = "Б"
PRINT LINENUMBER(f)
PRINT READLINE(f) = "В"
PRINT LINENUMBER(f)
PRINT SEEKFILE(f, 4)
PRINT READTEXT(f, 1) = "😀"
PRINT FILEPOS(f)
PRINT SEEKFILE(f, 6)
TRY
    PRINT READTEXT(f, 1)
CATCH err
    PRINT err.Code
END TRY
PRINT FILEPOS(f)
PRINT SEEKFILE(f, 1)
PRINT READTEXT(f, 0) = ""
TRY
    PRINT READLINE(f)
CATCH err
    PRINT err.Code
END TRY
PRINT FILEPOS(f)
TRY
    PRINT LINENUMBER(f)
CATCH err
    PRINT err.Code
END TRY
PRINT FILEPOS(f)
PRINT REWIND(f)
PRINT READTEXT(f, 3) = "Ċਁ😀"
CLOSE f
""";
            AssertProgram(source, root, ["TRUE", "12", "2", "TRUE", "3", "TRUE", "4", "TRUE", "TRUE", "8", "TRUE", "MB3007", "6", "TRUE", "TRUE", "MB3007", "1", "MB3007", "1", "TRUE", "TRUE"]);
        });
    }

    [Fact]
    public void OpenStrictWriteFailurePreservesBufferedContentPositionAndLength()
    {
        InDirectory(root =>
        {
            foreach (var name in new[] { "UTF-8", "WINDOWS-1251", "UTF-16LE", "UTF-16BE" })
            {
                var bad = name == "WINDOWS-1251" ? "\"😀\"" : "LEFT(\"😀\", 1)";
                var source = $$"""
DIM f AS FILE
OPEN "write.txt" FOR OUTPUT AS f USING "{{name}}"
PRINT WRITE(f, "А")
DIM saved AS LONG = FILEPOS(f)
TRY
    PRINT WRITELINE(f, {{bad}})
CATCH err
    PRINT err.Code
END TRY
PRINT FILEPOS(f) = saved
PRINT FILELENGTH(f) = saved
PRINT WRITE(f, "я")
PRINT FLUSH(f)
CLOSE f
OPEN "write.txt" FOR APPEND AS f USING "{{name}}"
saved = FILEPOS(f)
TRY
    PRINT WRITE(f, {{bad}})
CATCH err
    PRINT err.Code
END TRY
PRINT FILEPOS(f) = saved
PRINT FILELENGTH(f) = saved
PRINT WRITE(f, "!")
CLOSE f
""";
                AssertProgram(source, root, ["TRUE", "MB3007", "TRUE", "TRUE", "TRUE", "TRUE", "MB3007", "TRUE", "TRUE", "TRUE"]);
                var enc = EncodingFor(name);
                Assert.Equal(enc.GetPreamble().Concat(enc.GetBytes("Ая!")), File.ReadAllBytes(Path.Combine(root, "write.txt")));
            }
        });
    }

    private static Encoding EncodingFor(string name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return name switch
        {
            "UTF-8" => new UTF8Encoding(false, true),
            "UTF-16LE" => new UnicodeEncoding(false, true, true),
            "UTF-16BE" => new UnicodeEncoding(true, true, true),
            _ => Encoding.GetEncoding(1251, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
        };
    }

    private static void AssertProgram(string source, string root, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }

    private static void InDirectory(Action<string> action)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "encoding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(root); }
        finally { Directory.Delete(root, recursive: true); }
    }
}
