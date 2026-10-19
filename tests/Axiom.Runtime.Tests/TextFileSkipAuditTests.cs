using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class TextFileSkipAuditTests
{
    [Fact]
    public void FailedBlockReadDoesNotReusePreviousBlockAtNewPosition()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "locked.txt");
        try
        {
            File.WriteAllText(path, new string('a', 4095) + "\n" + new string('b', 4095) + "\n", new UTF8Encoding(false));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal(new string('a', 4095), file.ReadLine());
            using var blocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            blocker.Lock(4096, 4096);
            try
            {
                Assert.Throws<IOException>(() => file.ReadLine());
                Assert.Equal(4096, file.Position);
            }
            finally { blocker.Unlock(4096, 4096); }
            Assert.Equal(new string('b', 4095), file.ReadLine());
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ShortLinesReuseBlockBuffers()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "short.txt");
        try
        {
            File.WriteAllText(path, string.Concat(Enumerable.Repeat("a\n", 1000)));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) Assert.Equal("a", file.ReadLine());
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1_000_000);
            Assert.True(file.IsEndOfFile);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("line")]
    [InlineData("text")]
    [InlineData("skip")]
    [InlineData("number")]
    public void CancellationInsideReadRestoresPosition(string operation)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "cancel.txt");
        var armed = false;
        var checks = 0;
        try
        {
            File.WriteAllText(path, new string('a', 200_000));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8,
                checkCancellation: () => { if (armed && ++checks == 3) throw new OperationCanceledException(); });
            if (operation == "number") file.Seek(file.Length);
            var original = file.Position;
            armed = true;
            Assert.Throws<OperationCanceledException>(() =>
            {
                switch (operation)
                {
                    case "line": file.ReadLine(); break;
                    case "text": file.ReadToEnd(); break;
                    case "skip": file.SkipLine(); break;
                    default: _ = file.LineNumber; break;
                }
            });
            Assert.Equal(original, file.Position);
            armed = false;
            file.Rewind();
            Assert.Equal("a", file.ReadText(1));
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void LineScannerPreservesDelimiterAcrossBlockBoundary(string delimiter)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "boundary.txt");
        try
        {
            var first = new string('x', 4095);
            File.WriteAllText(path, first + delimiter + "😀tail", new UTF8Encoding(false));
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal(first, file.ReadLine());
            Assert.Equal(4095 + delimiter.Length, file.Position);
            Assert.Equal("😀tail", file.ReadLine());
            Assert.True(file.IsEndOfFile);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("UTF-16LE")]
    [InlineData("UTF-16BE")]
    public void SkipLongLineDoesNotCreateStringResult(string encodingName)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "long.txt");
        var encoding = encodingName == "UTF-8" ? (Encoding)new UTF8Encoding(false, true)
            : new UnicodeEncoding(encodingName == "UTF-16BE", true, true);
        try
        {
            using (var writer = new StreamWriter(path, false, encoding))
            {
                var block = new string('x', 4096);
                for (var i = 0; i < 4096; i++) writer.Write(block);
                writer.Write("x\r\n😀tail");
            }
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, encoding, encodingName);
            var before = GC.GetAllocatedBytesForCurrentThread();
            file.SkipLine();
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 200_000);
            Assert.Equal(2, file.LineNumber);
            Assert.Equal("😀tail", file.ReadToEnd());
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("UTF-8", 1)]
    [InlineData("UTF-16LE", 2)]
    [InlineData("UTF-16BE", 2)]
    public void SkipInvalidBoundaryRestoresPosition(string name, int position)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "skip-audit", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "text.txt");
        var encoding = name == "UTF-8" ? (Encoding)new UTF8Encoding(false, true)
            : new UnicodeEncoding(name == "UTF-16BE", true, true);
        try
        {
            File.WriteAllText(path, "😀\r\nnext", encoding);
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, encoding, name);
            file.Seek(position);
            Assert.Throws<InvalidOperationException>(file.SkipLine);
            Assert.Equal(position, file.Position);
        }
        finally { Directory.Delete(root, true); }
    }
}
