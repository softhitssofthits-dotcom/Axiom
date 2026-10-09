using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class TextFileUnicodeBoundaryTests
{
    [Theory]
    [InlineData(RuntimeTextFileMode.Output, false)]
    [InlineData(RuntimeTextFileMode.Output, true)]
    [InlineData(RuntimeTextFileMode.Append, false)]
    [InlineData(RuntimeTextFileMode.Append, true)]
    public void Write_EncodingOfEachCallDoesNotDependOnFlush(RuntimeTextFileMode mode, bool flushBetween)
    {
        var path = Path.GetTempFileName();
        try
        {
            var encoding = new UTF8Encoding(false);
            var chunks = new[] { "А", "\ud83d", "\ude00", "Б" };
            var expected = chunks.SelectMany(encoding.GetBytes).ToArray();
            using (var file = new RuntimeTextFile(path, mode, encoding))
            {
                foreach (var chunk in chunks)
                {
                    file.Write(chunk, appendLineBreak: false);
                    if (flushBetween) file.Flush();
                }
                Assert.Equal(expected.Length, file.Position);
                Assert.Equal(expected.Length, file.Length);
                file.Flush();
            }
            Assert.Equal(expected, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadText_FixedSeedMixedScalarsMatchTextAndBytePositions()
    {
        var path = Path.GetTempFileName();
        var random = new Random(1123);
        var alphabet = new[] { "А", "a", "😀", "\u0301", "\r", "\n", "𐐀" };
        try
        {
            for (var scenario = 0; scenario < 100; scenario++)
            {
                var scalars = Enumerable.Range(0, random.Next(1, 600))
                    .Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray();
                File.WriteAllText(path, string.Concat(scalars), new UTF8Encoding(scenario % 2 == 0));
                using var file = new RuntimeTextFile(path, RuntimeTextFileMode.Input, Encoding.UTF8);
                var offset = 0;
                var position = 0L;
                while (offset < scalars.Length)
                {
                    var count = random.Next(1, 260);
                    var expected = string.Concat(scalars.Skip(offset).Take(count));
                    Assert.Equal(expected, file.ReadText(count));
                    offset = Math.Min(scalars.Length, offset + count);
                    position += Encoding.UTF8.GetByteCount(expected);
                    Assert.Equal(position, file.Position);
                    Assert.Equal(offset == scalars.Length, file.IsEndOfFile);
                }
                Assert.Equal(string.Empty, file.ReadText(1));
            }
        }
        finally { File.Delete(path); }
    }
}
