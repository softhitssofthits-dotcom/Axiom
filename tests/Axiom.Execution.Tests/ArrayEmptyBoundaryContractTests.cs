namespace Axiom.Execution.Tests;

public sealed class ArrayEmptyBoundaryContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, 1)]
    [InlineData(ExecutionMode.Interpreter, 2)]
    [InlineData(ExecutionMode.Bytecode, 1)]
    [InlineData(ExecutionMode.Bytecode, 2)]
    public void EmptyWindowsAtIntegerEdgesKeepRankAndOtherDimensions(ExecutionMode mode, int dimension)
    {
        foreach (var item in new[]
        {
            (Bound: "-2147483647", Call: "ARRAYTAKE(a, 0, D)", Lower: "-2147483647", Upper: "-2147483648"),
            (Bound: "-2147483647", Call: "ARRAYSLICE(a, (-2147483647 - 1), 0, D)", Lower: "1", Upper: "0"),
            (Bound: "2147483647", Call: "ARRAYSKIP(a, 1, D)", Lower: "1", Upper: "0"),
            (Bound: "2147483647", Call: "ARRAYSLICE(a, 2147483647, 0, D)", Lower: "2147483647", Upper: "2147483646"),
            (Bound: "-4", Call: "ARRAYTAKE(a, 0, D)", Lower: "-4", Upper: "-5"),
            (Bound: "-4", Call: "ARRAYSKIP(a, 1, D)", Lower: "-3", Upper: "-4")
        })
        {
            var bounds = dimension == 1 ? $"{item.Bound} TO {item.Bound}, 7 TO 7" : $"7 TO 7, {item.Bound} TO {item.Bound}";
            var call = item.Call.Replace("D)", dimension + ")", StringComparison.Ordinal);
            var other = dimension == 1 ? 2 : 1;
            var source = $"DIM a({bounds}) AS INTEGER\nDIM part(,) AS INTEGER\npart = {call}\nPRINT ARRAYISALLOCATED(part)\nPRINT ARRAYLENGTH(part)\nPRINT ARRAYRANK(part)\nPRINT LBOUND(part, {dimension})\nPRINT UBOUND(part, {dimension})\nPRINT LBOUND(part, {other})\nPRINT UBOUND(part, {other})";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(run.Succeeded, call + "\n" + string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(new[] { "TRUE", "0", "2", item.Lower, item.Upper, "7", "7" }, run.Output);
        }
    }
}
