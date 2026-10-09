using System.Text;
using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class TextFileLineEndingTests
{
    [Theory]
    [InlineData(RuntimeTextFileMode.Output)]
    [InlineData(RuntimeTextFileMode.Append)]
    public void WriteLine_UsesLfAndBytePositions(RuntimeTextFileMode mode)
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var file = new RuntimeTextFile(path, mode, new UTF8Encoding(false)))
            {
                file.Write("А", true);
                file.Write("", true);
                Assert.Equal(4, file.Position);
                Assert.Equal(4, file.Length);
            }
            Assert.Equal(new byte[] { 0xD0, 0x90, 0x0A, 0x0A }, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
