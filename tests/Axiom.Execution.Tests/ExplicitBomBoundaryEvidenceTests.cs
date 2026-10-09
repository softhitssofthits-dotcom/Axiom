namespace Axiom.Execution.Tests;

public sealed class ExplicitBomBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var data = new (string Encoding, string Hex, string Expected)[]
        {
            ("UTF-8", "", ""), ("UTF-8", "EFBBBF", ""),
            ("UTF-8", "EFBBBFEFBBBF", "\ufeff"), ("UTF-8", "EFBB", "MB3007"),
            ("UTF-16LE", "", ""), ("UTF-16LE", "FFFE", ""),
            ("UTF-16LE", "FFFEFFFE", "\ufeff"), ("UTF-16LE", "FF", "MB3007"),
            ("UTF-16BE", "", ""), ("UTF-16BE", "FEFF", ""),
            ("UTF-16BE", "FEFFFEFF", "\ufeff"), ("UTF-16BE", "FE", "MB3007"),
            ("UTF-8", "FFFE", "MB3007"), ("UTF-8", "FEFF", "MB3007"),
            ("UTF-16LE", "EFBBBF", "MB3007"), ("UTF-16LE", "FEFF", "MB3007"),
            ("UTF-16BE", "EFBBBF", "MB3007"), ("UTF-16BE", "FFFE", "MB3007")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var (encoding, hex, expected) in data) yield return [mode, encoding, hex, expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LiteralBytesRespectSelectedEncodingAndOnlyOneSignature(ExecutionMode mode, string encoding, string hex, string expected)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "bom-boundary-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "граница😀.txt");
        var bytes = Convert.FromHexString(hex);
        File.WriteAllBytes(path, bytes);
        try
        {
            var source = $$"""
                DIM encoding AS STRING = " {{encoding.ToLowerInvariant()}} "
                TRY
                    PRINT READFILE("{{path}}", encoding) = "{{expected}}"
                CATCH err
                    PRINT err.Code
                END TRY
                TRY
                    DIM lines() AS STRING
                    lines = READALLLINES("{{path}}", encoding)
                    PRINT ARRAYLENGTH(lines)
                    IF ARRAYLENGTH(lines) > 0 THEN
                        PRINT lines(1) = "{{expected}}"
                    END IF
                CATCH err
                    PRINT err.Code
                END TRY
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(expected == "MB3007" ? new[] { "MB3007", "MB3007" }
                : expected.Length == 0 ? new[] { "TRUE", "0" } : new[] { "TRUE", "1", "TRUE" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
