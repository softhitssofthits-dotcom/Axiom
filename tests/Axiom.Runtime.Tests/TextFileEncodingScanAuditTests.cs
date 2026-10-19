using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class TextFileEncodingScanAuditTests
{
    [Fact]
    public void EncodingDetectionValidatesEveryByteAfterSplitSupplementaryScalar()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "encoding-scan", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "boundary.txt");
        try
        {
            var bytes = Enumerable.Repeat((byte)'a', 8192).ToArray();
            // Символ из двух единиц UTF-16 пересекает границу блоков.
            bytes[4093] = 0xF0;
            bytes[4094] = 0x9F;
            bytes[4095] = 0x98;
            bytes[4096] = 0x80;
            bytes[^1] = 0xFF;
            File.WriteAllBytes(path, bytes);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var expected = Encoding.GetEncoding(1251).GetString(bytes);
            using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
            Assert.Equal(expected, file.ReadToEnd());
            Assert.Equal(bytes.Length, file.Position);
        }
        finally { Directory.Delete(root, true); }
    }
}
