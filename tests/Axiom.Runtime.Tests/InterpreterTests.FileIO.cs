using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_OpenReadLineAndEof()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + Environment.NewLine + "omega");

        var text = $"""
DIM f AS FILE
DIM line AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
WHILE NOT EOF(f)
    line = READLINE(f)
    PRINT "[" + line + "]"
END WHILE
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[alpha]", "[]", "[omega]"], host.Output);
    }

    [Fact]
    public void Execute_OpenInputStripsUtf8Bom()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "bom.txt");
        File.WriteAllBytes(filePath, new byte[] { 0xEF, 0xBB, 0xBF }.Concat(System.Text.Encoding.UTF8.GetBytes("тест")).ToArray());

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READTOEND(f)
PRINT FILEPOS(f)
REWIND(f)
PRINT FILEPOS(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["тест", "8", "0"], host.Output);
    }

    [Fact]
    public void Execute_OpenInputFallsBackToWindows1251()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "cp1251.txt");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        File.WriteAllBytes(filePath, System.Text.Encoding.GetEncoding(1251).GetBytes("тест"));

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["тест"], host.Output);
    }

    [Fact]
    public void Execute_FileStateBuiltins()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "state.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
PRINT ISOPEN(f)
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT FILEPATH(f)
CLOSE f
PRINT ISOPEN(f)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "INPUT", filePath, "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_FilePositionAndLengthForInput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-input.txt");
        const string content = "alpha\r\nbeta";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var firstLineBytes = Encoding.UTF8.GetByteCount("alpha\r\n");
        var totalBytes = Encoding.UTF8.GetByteCount(content);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", totalBytes.ToString(), "alpha", firstLineBytes.ToString(), "beta", totalBytes.ToString()], host.Output);
    }

    [Fact]
    public void Execute_ReadToEndReturnsRemainingText()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtoend.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "beta"], host.Output);
    }

    [Fact]
    public void Execute_ReadTextCountsCharactersWhilePositionStaysByteBased()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtext.txt");
        const string content = "AБВ";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var firstTwoCharsBytes = Encoding.UTF8.GetByteCount("AБ");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READTEXT(f, 2)
PRINT FILEPOS(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["AБ", firstTwoCharsBytes.ToString(), "В"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReadsUpToCountAndAdvancesPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");

        var text = $"""
DIM f AS FILE
DIM first() AS STRING
DIM rest() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
first = READLINES(f, 2)
rest = READLINES(f, 2)
PRINT ARRAYLENGTH(first)
PRINT first(1)
PRINT first(2)
PRINT LINENUMBER(f)
PRINT ARRAYLENGTH(rest)
PRINT rest(1)
PRINT EOF(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "alpha", "beta", "3", "1", "gamma", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReturnsEmptyArrayForZeroCountAndEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines-empty.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, 0)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
PRINT LINENUMBER(f)
lines = READLINES(f, 1)
lines = READLINES(f, 1)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SkipLineAndLineNumberTrackInputPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "lines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");
        var secondLinePosition = Encoding.UTF8.GetByteCount("alpha" + Environment.NewLine);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT LINENUMBER(f)
PRINT SKIPLINE(f)
PRINT LINENUMBER(f)
PRINT READLINE(f)
PRINT LINENUMBER(f)
PRINT SEEKFILE(f, {secondLinePosition})
PRINT LINENUMBER(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "TRUE", "2", "beta", "3", "TRUE", "2"], host.Output);
    }

    [Fact]
    public void Execute_NewLineWritesEmptyLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "newline.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "alpha")
PRINT NEWLINE(f)
PRINT WRITELINE(f, "beta")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
        Assert.Equal($"alpha{Environment.NewLine}{Environment.NewLine}beta{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenOutputAndWriteLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "first")
PRINT WRITELINE(f, "second")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
        Assert.Equal($"first{Environment.NewLine}second{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenAppendAndWrite()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "A");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT WRITE(f, "B")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE"], host.Output);
        Assert.Equal("AB", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FlushMakesBufferedWriteVisible()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "flush.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITE(f, "A")
PRINT FILESIZE("{ToBasicString(filePath)}")
PRINT FLUSH(f)
PRINT FILESIZE("{ToBasicString(filePath)}")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "TRUE", "1"], host.Output);
        Assert.Equal("A", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FilePositionAndSeekForOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT FILELENGTH(f)
PRINT WRITE(f, "ABCDE")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT SEEKFILE(f, 2)
PRINT FILEPOS(f)
PRINT WRITE(f, "Z")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "TRUE", "5", "5", "TRUE", "2", "TRUE", "3", "5"], host.Output);
        Assert.Equal("ABZDE", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_RewindMovesFileBackToBeginning()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "rewind.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT REWIND(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "TRUE", "0", "alpha"], host.Output);
    }

    [Fact]
    public void Execute_CloseReportsClosedFile()
    {
        const string text = """
DIM f AS FILE
CLOSE f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CLOSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_OpenReportsAlreadyOpenFileVariable()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var firstPath = Path.Combine(root, "first.txt");
        var secondPath = Path.Combine(root, "second.txt");
        File.WriteAllText(firstPath, "1");
        File.WriteAllText(secondPath, "2");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(firstPath)}" FOR INPUT AS f
OPEN "{ToBasicString(secondPath)}" FOR INPUT AS f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("OPEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WriteReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT WRITE(f, "beta")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("WRITE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_FlushReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FLUSH(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("FLUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadToEndReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT READTOEND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTOEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
lines = READLINES(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsNegativeCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SkipLineReportsEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "skip-eof.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SKIPLINE(f)
PRINT SKIPLINE(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SKIPLINE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("конец", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadTextReportsInvalidUtf8Boundary()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "utf8-boundary.txt");
        File.WriteAllText(filePath, "AБ", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 2)
PRINT READTEXT(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTEXT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UTF-8", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT SEEKFILE(f, 0)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RewindReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append-rewind.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT REWIND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REWIND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsPositionBeyondEnd()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "beyond-end.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("position", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
