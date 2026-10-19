namespace Axiom.Execution.Tests;

public sealed class StringAuditAcceptanceTests
{
    [Theory]
    [InlineData("unicode_scan", "50000|40000|30000|3000")]
    [InlineData("replace_rules", "TRUE")]
    [InlineData("file_positions", "50015000")]
    [InlineData("file_buffer", "0|1024000|1024000")]
    [InlineData("numeric_check", "10000")]
    [InlineData("append_encoding", "TRUE")]
    public void PerformanceSamplesHaveIndependentExpectedOutput(string name, string output)
    {
        var source = TestProgramLoader.LoadText($"samples/smoke/engine-compare/string_audit_{name}.axm");
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(output.Split('|'), result.Output);
    }
}
