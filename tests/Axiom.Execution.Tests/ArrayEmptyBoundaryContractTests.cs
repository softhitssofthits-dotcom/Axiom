namespace Axiom.Execution.Tests;

public sealed class ArrayEmptyBoundaryContractTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void EmptyWindowsAtIntegerEdgesKeepRankAndOtherDimensions(int dimension)
    {
        foreach (var item in new[]
        {
            (Bound: "-2147483647", Call: "ARRAYTAKE(a, 0, D)"),
            (Bound: "-2147483647", Call: "ARRAYSLICE(a, (-2147483647 - 1), 0, D)"),
            (Bound: "2147483647", Call: "ARRAYSKIP(a, 1, D)"),
            (Bound: "2147483647", Call: "ARRAYSLICE(a, 2147483647, 0, D)"),
            (Bound: "-4", Call: "ARRAYTAKE(a, 0, D)"),
            (Bound: "-4", Call: "ARRAYSKIP(a, 1, D)")
        })
        {
            var bounds = dimension == 1 ? $"{item.Bound} TO {item.Bound}, 7 TO 7" : $"7 TO 7, {item.Bound} TO {item.Bound}";
            var call = item.Call.Replace("D)", dimension + ")", StringComparison.Ordinal);
            var other = dimension == 1 ? 2 : 1;
            var source = $"DIM a({bounds}) AS INTEGER\nDIM part(,) AS INTEGER\npart = {call}\nPRINT ARRAYISALLOCATED(part)\nPRINT ARRAYLENGTH(part)\nPRINT ARRAYRANK(part)\nPRINT LBOUND(part, {dimension})\nPRINT UBOUND(part, {dimension})\nPRINT LBOUND(part, {other})\nPRINT UBOUND(part, {other})";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, call + "\n" + string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "TRUE", "0", "2", "1", "0", "7", "7" }, run.Output);
        }
    }

    [Theory]
    [InlineData("ARRAYSLICE(a, 4, 0)")]
    [InlineData("ARRAYTAKE(a, 0)")]
    [InlineData("ARRAYSKIP(a, 5)")]
    public void EmptyOneDimensionalWindowsUseCanonicalBounds(string call)
    {
        var source = $"DIM a(1 TO 5) AS INTEGER\nDIM part() AS INTEGER\npart = {call}\nPRINT ARRAYISALLOCATED(part)\nPRINT ARRAYLENGTH(part)\nPRINT LBOUND(part)\nPRINT UBOUND(part)";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "TRUE", "0", "1", "0" }, run.Output);
    }
}
