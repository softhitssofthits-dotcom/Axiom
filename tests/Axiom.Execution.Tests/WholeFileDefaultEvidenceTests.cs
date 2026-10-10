namespace Axiom.Execution.Tests;

public sealed class WholeFileDefaultEvidenceTests
{
    [Fact]
    public void DefaultUtf8FormsPreserveTextAndDistinguishEmptyLines()
    {
        var path = Path.GetTempFileName();
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT WRITEFILE("{{path}}", "А😀" + CHRW(13) + CHRW(10) + CHRW(10))
                PRINT APPENDFILE("{{path}}", "Б")
                PRINT READFILE("{{path}}") = "А😀" + CHRW(13) + CHRW(10) + CHRW(10) + "Б"
                DIM lines() AS STRING
                lines = READALLLINES("{{path}}")
                PRINT ARRAYLENGTH(lines)
                PRINT lines(1) = "А😀"
                PRINT lines(2) = ""
                PRINT lines(3) = "Б"
                PRINT WRITEFILE("{{path}}", "")
                PRINT READFILE("{{path}}") = ""
                lines = READALLLINES("{{path}}")
                PRINT ARRAYLENGTH(lines)
                PRINT WRITEFILE("{{path}}", "А😀" + CHRW(13) + CHRW(10) + CHRW(10))
                PRINT APPENDFILE("{{path}}", "Б")
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { "TRUE", "TRUE", "TRUE", "3", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "0", "TRUE", "TRUE" }, result.Output);
            Assert.Equal(new byte[] { 0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80, 13, 10, 10, 0xD0, 0x91 }, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }
}
