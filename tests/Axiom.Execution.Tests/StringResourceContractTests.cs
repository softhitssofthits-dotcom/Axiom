namespace Axiom.Execution.Tests;

public sealed class StringResourceContractTests
{
    [Fact]
    public void Utf8BomDoesNotConsumeResultBudget()
    {
        var folder = Path.GetFullPath(Path.Combine(".artifacts", "string-bom-limit-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "text.txt");
        try
        {
            File.WriteAllText(path, new string('a', 16777216), new System.Text.UTF8Encoding(true));
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT LEN(READFILE(\"{path}\"))"));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "16777216" }, run.Output);
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }

    [Theory]
    [InlineData("REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"x\", 2000))")]
    [InlineData("REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"$&\", 2000))")]
    public void ExpandingReplacementIsCatchable(string expression) => OversizeResultIsCatchableAndPreservesDestination(expression);

    [Theory]
    [InlineData("SPACE(16777217)")]
    [InlineData("REPEAT(\"ab\", 8388609)")]
    [InlineData("PADLEFT(\"x\", 16777217)")]
    public void OversizeResultIsCatchableAndPreservesDestination(string expression)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
DIM text AS STRING = "kept"
TRY
    text = {{expression}}
CATCH problem
    PRINT problem.Code
END TRY
PRINT text
"""));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "MB3007", "kept" }, result.Output);
    }

    [Fact]
    public void WriteLineRejectsLineBreakBeyondStringLimitBeforeWriting()
    {
        var folder = Path.GetFullPath(Path.Combine(".artifacts", "string-writeline-limit-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "text.txt");
        try
        {
            var source = $$"""
DIM text AS STRING = REPEAT("x", 16777216)
TRY
    DIM handle AS FILE
    OPEN "{{path}}" FOR OUTPUT AS handle
    WRITELINE(handle, text)
    CLOSE handle
    PRINT "unexpected"
CATCH problem
    PRINT problem.Code
END TRY
PRINT FILESIZE("{{path}}")
""";

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "MB3007", "0" }, result.Output);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(folder)) Directory.Delete(folder);
        }
    }
}
