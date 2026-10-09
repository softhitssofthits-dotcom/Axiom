namespace Axiom.Execution.Tests;

public sealed class ArrayConcatBoundaryContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, 1)]
    [InlineData(ExecutionMode.Interpreter, 2)]
    [InlineData(ExecutionMode.Bytecode, 1)]
    [InlineData(ExecutionMode.Bytecode, 2)]
    public void TwoElementsCanEndAtMaximumIntegerIndex(ExecutionMode mode, int dimension)
    {
        var bounds = dimension == 1 ? "2147483646 TO 2147483646, 7 TO 7" : "7 TO 7, 2147483646 TO 2147483646";
        var first = dimension == 1 ? "2147483646, 7" : "7, 2147483646";
        var last = dimension == 1 ? "2147483647, 7" : "7, 2147483647";
        var source = $"DIM a({bounds}) AS INTEGER\nDIM result(,) AS INTEGER\na({first}) = 42\nresult = ARRAYCONCAT(a, a, {dimension})\nPRINT ARRAYLENGTH(result)\nPRINT LBOUND(result, {dimension})\nPRINT UBOUND(result, {dimension})\nPRINT result({first})\nPRINT result({last})";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(new[] { "2", "2147483646", "2147483647", "42", "42" }, run.Output);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, 1)]
    [InlineData(ExecutionMode.Interpreter, 2)]
    [InlineData(ExecutionMode.Bytecode, 1)]
    [InlineData(ExecutionMode.Bytecode, 2)]
    public void UnrepresentableResultBoundsAreCatchableAndLeaveSourceUnchanged(ExecutionMode mode, int dimension)
    {
        var bounds = dimension == 1 ? "2147483647 TO 2147483647, 7 TO 7" : "7 TO 7, 2147483647 TO 2147483647";
        var index = dimension == 1 ? "2147483647, 7" : "7, 2147483647";
        var source = $"DIM a({bounds}) AS INTEGER\nDIM result(,) AS INTEGER\na({index}) = 42\nTRY\nresult = ARRAYCONCAT(a, a, {dimension})\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a)\nPRINT a({index})";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(new[] { "MB3007", "1", "42" }, run.Output);
    }
}
