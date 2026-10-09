using System.Text;
using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class ReadTextUnicodeTests
{
    [Theory]
    [InlineData("😀А", false)]
    [InlineData("аб", false)]
    [InlineData("Я", true)]
    [InlineData("Яabc", true)]
    public void InputEncodingDetection_ExaminesWholeFile(string text, bool windows1251)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = windows1251 ? Encoding.GetEncoding(1251) : new UTF8Encoding(false);
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, text, encoding);
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal(text, file.ReadToEnd());
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(254)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    public void ReadText_CountsUnicodeScalarsAndPreservesBytePosition(int prefixLength)
    {
        var path = Path.GetTempFileName();
        var prefix = new string('А', prefixLength);
        try
        {
            File.WriteAllText(path, prefix + "😀Б", new UTF8Encoding(true));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal(string.Empty, file.ReadText(0));
            Assert.Equal(prefix + "😀", file.ReadText(prefixLength + 1));
            Assert.Equal(Encoding.UTF8.GetByteCount(prefix + "😀"), file.Position);
            Assert.Equal("Б", file.ReadText(1));
            Assert.Equal(Encoding.UTF8.GetByteCount(prefix + "😀Б"), file.Position);
            Assert.Equal(string.Empty, file.ReadText(1));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadText_CountsCombiningMarksSeparatelyAndReadsEmojiTogether()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "е\u0301😀😀", new UTF8Encoding(true));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal("е", file.ReadText(1));
            Assert.Equal("\u0301", file.ReadText(1));
            Assert.Equal("😀😀", file.ReadText(2));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadText_InvalidUtf8BoundaryRestoresPosition()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "😀А", new UTF8Encoding(true));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            file.Seek(1);
            Assert.Throws<InvalidOperationException>(() => file.ReadText(1));
            Assert.Equal(1, file.Position);
            file.Rewind();
            Assert.Equal("😀", file.ReadText(1));
            Assert.Equal("А", file.ReadToEnd());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadText_Utf8WithoutBomPreservesFollowingText()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "abc😀А", new UTF8Encoding(false));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal("abc", file.ReadText(3));
            Assert.Equal("😀", file.ReadText(1));
            Assert.Equal(7, file.Position);
            Assert.Equal("А", file.ReadToEnd());
        }
        finally { File.Delete(path); }
    }
}
