namespace Axiom.Execution.Tests;

public sealed class ArrayEmptyInsertContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, -4)]
    [InlineData(ExecutionMode.Interpreter, 9)]
    [InlineData(ExecutionMode.Bytecode, -4)]
    [InlineData(ExecutionMode.Bytecode, 9)]
    [InlineData(ExecutionMode.Interpreter, 2147483647)]
    [InlineData(ExecutionMode.Bytecode, 2147483647)]
    [InlineData(ExecutionMode.Interpreter, -2147483647)]
    [InlineData(ExecutionMode.Bytecode, -2147483647)]
    public void InsertIntoEmptySelectedDimensionUsesCanonicalFirstIndex(ExecutionMode mode, int lower)
    {
        foreach (var dimension in new[] { 1, 2 })
        {
            var bounds = dimension == 1 ? $"{lower} TO {lower}, 7 TO 8" : $"7 TO 8, {lower} TO {lower}";
            var first = dimension == 1 ? "1, 7" : "7, 1";
            var last = dimension == 1 ? "1, 8" : "8, 1";
            var other = 3 - dimension;
            var source = $"DIM seed({bounds}) AS INTEGER\nDIM a(,) AS INTEGER\nDIM sliceValues(7 TO 8) AS INTEGER\nsliceValues(7) = 42\nsliceValues(8) = 43\na = ARRAYTAKE(seed, 0, {dimension})\nARRAYINSERT(a, 1, sliceValues, {dimension})\nPRINT LBOUND(a, {dimension})\nPRINT UBOUND(a, {dimension})\nPRINT LBOUND(a, {other})\nPRINT UBOUND(a, {other})\nPRINT a({first})\nPRINT a({last})";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(new[] { "1", "1", "7", "8", "42", "43" }, run.Output);
        }
    }
    [Theory]
    [InlineData(ExecutionMode.Interpreter, -4)]
    [InlineData(ExecutionMode.Bytecode, -4)]
    [InlineData(ExecutionMode.Interpreter, 9)]
    [InlineData(ExecutionMode.Bytecode, 9)]
    public void InsertIntoEmptyOneDimensionalShapeUsesFirstIndex(ExecutionMode mode, int lower)
    {
        var source = $"DIM seed(7 TO 7, {lower} TO {lower}) AS INTEGER\nDIM empty(,) AS INTEGER\nDIM target() AS INTEGER\nempty = ARRAYTAKE(seed, 0, 2)\ntarget = ARRAYRESHAPE(ARRAYFLATTEN(empty), empty(7))\nARRAYINSERT(target, 1, 42)\nPRINT LBOUND(target)\nPRINT UBOUND(target)\nPRINT target(1)";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(new[] { "1", "1", "42" }, run.Output);
    }

}
