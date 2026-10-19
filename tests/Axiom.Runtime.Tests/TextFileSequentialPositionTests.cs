using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class TextFileSequentialPositionTests
{
    [Fact]
    public void LineNumberFollowsReadsBackwardSeeksAndCarriageReturnPairs()
    {
        var folder = Path.GetFullPath(Path.Combine(".artifacts", "string-file-position-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "text.txt");
        try
        {
            File.WriteAllText(path, "a\r\nb\rc\nd", new UTF8Encoding(false));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, new UTF8Encoding(false));
            Assert.Equal(1, file.LineNumber);
            Assert.Equal("a", file.ReadLine());
            Assert.Equal(2, file.LineNumber);
            Assert.Equal("b", file.ReadLine());
            Assert.Equal(3, file.LineNumber);
            Assert.Equal("c", file.ReadLine());
            Assert.Equal(4, file.LineNumber);
            file.Seek(2);
            Assert.Equal(2, file.LineNumber);
            file.Seek(3);
            Assert.Equal(2, file.LineNumber);
            file.Rewind();
            Assert.Equal(1, file.LineNumber);
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }

    [Fact]
    public void LargeBufferedWriteRemainsInvisibleUntilFlushAndSupportsSeek()
    {
        var folder = Path.GetFullPath(Path.Combine(".artifacts", "string-file-buffer-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "text.txt");
        try
        {
            using (var file = new RuntimeTextFile(path, RuntimeTextFileMode.Output, new UTF8Encoding(false)))
            {
                file.Write(new string('x', 200000), false);
                Assert.Equal(200000, file.Length);
                Assert.Equal(0, new FileInfo(path).Length);
                file.Flush();
                Assert.Equal(200000, new FileInfo(path).Length);
                file.Seek(0);
                file.Write("AB", false);
            }
            Assert.Equal("AB" + new string('x', 199998), File.ReadAllText(path));
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }
}
