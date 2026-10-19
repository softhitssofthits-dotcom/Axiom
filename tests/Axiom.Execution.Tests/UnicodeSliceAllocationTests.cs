namespace Axiom.Execution.Tests;

public sealed class UnicodeSliceAllocationTests
{
    [Theory]
    [InlineData("LEFT(text, 1, \"CODEPOINT\")", "a")]
    [InlineData("MID(text, 2147483647, 1, \"CODEPOINT\")", "")]
    public void SmallSliceDoesNotAllocateFullBoundaryIndex(string expression, string expected)
    {
        var source = $"DIM text AS STRING = \"{new string('a', 100000)}\"\nPRINT {expression}";
        var pipeline = new AxiomExecutionPipeline();
        var compiled = pipeline.Compile(new CompilationRequest(source));
        Assert.True(compiled.Succeeded);
        var request = new ExecutionRequest(source);
        pipeline.RunCompiled(compiled.CompiledProgram!, request);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = pipeline.RunCompiled(compiled.CompiledProgram!, request);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(result.Succeeded);
        Assert.Equal(new[] { expected }, result.Output);
        Assert.InRange(allocated, 0, 500000);
    }
}
