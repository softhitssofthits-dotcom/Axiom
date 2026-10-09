using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class TextFileExplicitEncodingTests
{
    [Theory]
    [InlineData("UTF-16LE", false)]
    [InlineData("UTF-16BE", true)]
    public void Utf16LinesUseCodeUnitsAndPositionsExcludeBom(string name, bool bigEndian)
    {
        WithPath(path =>
        {
            var encoding = new UnicodeEncoding(bigEndian, true, true);
            const string text = "Ċਁ😀\r\nА\nя\rПоследняя";
            File.WriteAllBytes(path, encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());
            using var file = Open(path, RuntimeTextFileMode.Input, name);
            Assert.Equal(0, file.Position);
            Assert.Equal(encoding.GetByteCount(text), file.Length);
            Assert.Equal("Ċਁ😀", file.ReadLine());
            Assert.Equal(12, file.Position);
            Assert.Equal(2, file.LineNumber);
            Assert.Equal("А", file.ReadLine());
            Assert.Equal(3, file.LineNumber);
            Assert.Equal("я", file.ReadLine());
            Assert.Equal(4, file.LineNumber);
            Assert.Equal("Последняя", file.ReadLine());
            Assert.True(file.IsEndOfFile);
            Assert.Equal(file.Length, file.Position);
            Assert.Equal(4, file.LineNumber);
        });
    }

    [Theory]
    [InlineData("UTF-16LE", false)]
    [InlineData("UTF-16BE", true)]
    public void Utf16ScalarReadsRejectOddAndSurrogateInteriorPositionsWithoutAdvancing(string name, bool bigEndian)
    {
        WithPath(path =>
        {
            var encoding = new UnicodeEncoding(bigEndian, true, true);
            File.WriteAllBytes(path, encoding.GetPreamble().Concat(encoding.GetBytes("😀А")).ToArray());
            using var file = Open(path, RuntimeTextFileMode.Input, name);
            Assert.Equal("😀", file.ReadText(1));
            Assert.Equal(4, file.Position);
            Assert.Equal("А", file.ReadText(1));
            file.Seek(2);
            Assert.Throws<InvalidOperationException>(() => file.ReadText(1));
            Assert.Equal(2, file.Position);
            file.Seek(1);
            Assert.Throws<InvalidOperationException>(() => file.ReadLine());
            Assert.Throws<InvalidOperationException>(() => file.ReadText(1));
            Assert.Equal(1, file.Position);
            Assert.Equal(string.Empty, file.ReadText(0));
            file.Rewind();
            Assert.Equal("😀А", file.ReadToEnd());
        });
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("WINDOWS-1251")]
    [InlineData("UTF-16LE")]
    [InlineData("UTF-16BE")]
    public void OutputAndAppendUseOnePreambleAndConsistentBytePositions(string name)
    {
        WithPath(path =>
        {
            var encoding = EncodingFor(name);
            using (var output = Open(path, RuntimeTextFileMode.Output, name))
            {
                Assert.Equal(0, output.Position);
                Assert.Equal(0, output.Length);
                output.Write("А", appendLineBreak: true);
                Assert.Equal(encoding.GetByteCount("А\n"), output.Position);
                output.Flush();
            }
            using (var append = Open(path, RuntimeTextFileMode.Append, name))
            {
                Assert.Equal(encoding.GetByteCount("А\n"), append.Position);
                append.Write("я", appendLineBreak: false);
                Assert.Equal(encoding.GetByteCount("А\nя"), append.Length);
            }
            Assert.Equal(encoding.GetPreamble().Concat(encoding.GetBytes("А\nя")), File.ReadAllBytes(path));
            using var input = Open(path, RuntimeTextFileMode.Input, name);
            Assert.Equal("А", input.ReadLine());
            Assert.Equal("я", input.ReadText(1));
        });
    }

    [Theory]
    [InlineData("UTF-16LE", false)]
    [InlineData("UTF-16BE", true)]
    public void AppendPreservesBomlessFileAndStrictWriteFailurePreservesBuffer(string name, bool bigEndian)
    {
        WithPath(path =>
        {
            var encoding = new UnicodeEncoding(bigEndian, false, true);
            File.WriteAllBytes(path, encoding.GetBytes("А"));
            using (var file = Open(path, RuntimeTextFileMode.Append, name))
            {
                Assert.Equal(2, file.Position);
                Assert.Throws<EncoderFallbackException>(() => file.Write("\ud800", false));
                Assert.Equal(2, file.Position);
                Assert.Equal(2, file.Length);
                file.Write("я", false);
            }
            Assert.Equal(encoding.GetBytes("Ая"), File.ReadAllBytes(path));
        });
    }

    [Theory]
    [InlineData(RuntimeTextFileMode.Input)]
    [InlineData(RuntimeTextFileMode.Append)]
    public void InvalidInputOpenDisposesStreamAndLeavesBytesUnchanged(RuntimeTextFileMode mode)
    {
        WithPath(path =>
        {
            byte[] bytes = [0xFF, 0xFE, 0x00, 0xD8];
            File.WriteAllBytes(path, bytes);
            Assert.Throws<DecoderFallbackException>(() => Open(path, mode, "UTF-16LE"));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.Equal(bytes.Length, exclusive.Length);
            var actual = new byte[bytes.Length];
            exclusive.ReadExactly(actual);
            Assert.Equal(bytes, actual);
        });
    }

    private static RuntimeTextFile Open(string path, RuntimeTextFileMode mode, string name) =>
        new(path, mode, new UTF8Encoding(false), explicitEncodingName: name);

    private static Encoding EncodingFor(string name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return name switch
        {
            "UTF-16LE" => new UnicodeEncoding(false, true, true),
            "UTF-16BE" => new UnicodeEncoding(true, true, true),
            "WINDOWS-1251" => Encoding.GetEncoding(1251),
            _ => new UTF8Encoding(false, true)
        };
    }

    private static void WithPath(Action<string> action)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "explicit-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(Path.Combine(root, "text.dat")); }
        finally { Directory.Delete(root, recursive: true); }
    }
}
