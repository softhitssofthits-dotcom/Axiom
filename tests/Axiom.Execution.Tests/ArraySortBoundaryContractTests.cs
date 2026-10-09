namespace Axiom.Execution.Tests;

public sealed class ArraySortBoundaryContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, 1, false)]
    [InlineData(ExecutionMode.Interpreter, 2, false)]
    [InlineData(ExecutionMode.Bytecode, 1, false)]
    [InlineData(ExecutionMode.Bytecode, 2, false)]
    [InlineData(ExecutionMode.Interpreter, 1, true)]
    [InlineData(ExecutionMode.Interpreter, 2, true)]
    [InlineData(ExecutionMode.Bytecode, 1, true)]
    [InlineData(ExecutionMode.Bytecode, 2, true)]
    public void MultidimensionalSortCanEndAtMaximumIntegerIndex(ExecutionMode mode, int axis, bool copy)
    {
        var bounds = axis == 1 ? "2147483646 TO 2147483647, 7 TO 7" : "7 TO 7, 2147483646 TO 2147483647";
        var first = axis == 1 ? "2147483646, 7" : "7, 2147483646";
        var last = axis == 1 ? "2147483647, 7" : "7, 2147483647";
        var statement = copy ? $"result = ARRAYTOSORTED(a, {axis})" : $"ARRAYSORT(a, {axis})\nresult = ARRAYCLONE(a)";
        var source = $"DIM a({bounds}) AS INTEGER\nDIM result(,) AS INTEGER\na({first}) = 30\na({last}) = 10\n{statement}\nPRINT LBOUND(result, {axis})\nPRINT UBOUND(result, {axis})\nPRINT result({first})\nPRINT result({last})\nPRINT a({first})";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, mode + "\n" + string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(new[] { "2147483646", "2147483647", "10", "30", copy ? "30" : "10" }, run.Output);
    }
}
