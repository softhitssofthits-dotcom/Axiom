namespace Axiom.Execution.Tests;

public sealed class BinaryFileAlternativeEvidenceTests
{
    public static IEnumerable<object[]> Payloads()
    {
        var allBytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        {
            yield return new object[] { "empty", Array.Empty<byte>() };
            yield return new object[] { "all-values", allBytes };
            yield return new object[] { "utf8-marker", new byte[] { 0xEF, 0xBB, 0xBF }.Concat(allBytes).ToArray() };
            yield return new object[] { "utf16le-marker", new byte[] { 0xFF, 0xFE }.Concat(allBytes).ToArray() };
            yield return new object[] { "utf16be-marker", new byte[] { 0xFE, 0xFF }.Concat(allBytes).ToArray() };
            yield return new object[] { "repeated-controls", new byte[] { 0, 13, 10, 26, 0, 255, 128, 152, 0 } };
        }
    }

    [Theory]
    [MemberData(nameof(Payloads))]
    public void ExistingCopyAndExplicitSingleByteRoundTripPreserveExactBytes(string scenario, byte[] payload)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "binary-alternative-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var input = Path.Combine(root, "input.bin");
            File.WriteAllBytes(input, payload);
            const string source = """
PRINT COPYFILE("input.bin", "copy.bin")
DIM text AS STRING = READFILE("input.bin", "WINDOWS-1251")
PRINT LEN(text)
PRINT WRITEFILE("roundtrip.bin", text, "WINDOWS-1251")
""";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: root));
            Assert.True(run.Succeeded, scenario + "\n" + string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "TRUE", payload.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), "TRUE" }, run.Output);
            Assert.Equal(payload, File.ReadAllBytes(input));
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(root, "copy.bin")));
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(root, "roundtrip.bin")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
