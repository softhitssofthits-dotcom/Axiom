namespace Axiom.Execution.Tests;

public sealed class FileByteOverwriteEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        {
            foreach (var position in new[] { 1, 2, 3, 4 }) yield return ["UTF-8", position];
            foreach (var name in new[] { "UTF-16LE", "UTF-16BE" })
            foreach (var position in new[] { 2, 4 }) yield return [name, position];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ByteOverwriteIsExactButDoesNotGuaranteeValidText(string name, int position)
    {
        var path = Path.GetTempFileName();
        try
        {
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR OUTPUT AS f USING "{{name}}"
                PRINT WRITE(f, "A😀B")
                PRINT SEEKFILE(f, {{position}})
                PRINT WRITE(f, "Z")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                TRY
                    PRINT READFILE("{{path}}", "{{name}}")
                CATCH err
                    PRINT err.Code
                END TRY
                TRY
                    OPEN "{{path}}" FOR INPUT AS f USING "{{name}}"
                CATCH err
                    PRINT err.Code
                END TRY
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(result.Status == ExecutionStatus.Success, string.Join(" | ", result.Diagnostics));
            Assert.Empty(result.Diagnostics);
            byte[] content = name switch
            {
                "UTF-8" => [0x41, 0xF0, 0x9F, 0x98, 0x80, 0x42],
                "UTF-16LE" => [0x41, 0, 0x3D, 0xD8, 0, 0xDE, 0x42, 0],
                _ => [0, 0x41, 0xD8, 0x3D, 0xDE, 0, 0, 0x42]
            };
            byte[] replacement = name switch { "UTF-8" => [0x5A], "UTF-16LE" => [0x5A, 0], _ => [0, 0x5A] };
            replacement.CopyTo(content, position);
            byte[] preamble = name switch { "UTF-8" => [], "UTF-16LE" => [0xFF, 0xFE], _ => [0xFE, 0xFF] };
            Assert.Equal(["TRUE", "TRUE", "TRUE", (position + replacement.Length).ToString(), content.Length.ToString(), "MB3007", "MB3007"], result.Output);
            Assert.Equal(preamble.Concat(content).ToArray(), File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
