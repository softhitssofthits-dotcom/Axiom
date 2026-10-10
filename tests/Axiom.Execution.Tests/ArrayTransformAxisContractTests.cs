namespace Axiom.Execution.Tests;

public sealed class ArrayTransformAxisContractTests
{
    public static IEnumerable<object[]> InvalidAxes()
    {
        foreach (var axis in new[] { "0", "2", "2147483647", "(-2147483647 - 1)" })
        {
            foreach (var call in new[]
            {
                $"result = ARRAYTOSORTED(a, {axis})", $"result = ARRAYTOREVERSED(a, {axis})",
                $"ARRAYSORT(a, {axis})", $"ARRAYREVERSE(a, {axis})",
                $"PRINT ARRAYBINARYSEARCH(a, 22, {axis})", $"ARRAYPUSH(a, 44, {axis})",
                $"PRINT ARRAYPOP(a, {axis})", $"ARRAYINSERT(a, 5, 44, {axis})",
                $"PRINT ARRAYREMOVE(a, 5, {axis})", $"result = ARRAYCONCAT(a, a, {axis})"
            })
                yield return new object[] { call, 3 };
            foreach (var length in new[] { 0, 1 })
                foreach (var call in new[] { $"result = ARRAYTOSORTED(a, {axis})", $"ARRAYSORT(a, {axis})" })
                    yield return new object[] { call, length };
        }
    }

    public static IEnumerable<object[]> ValidOperations()
    {
        yield return new object[] { "result = ARRAYTOSORTED(aAXIS)", "result", new[] { "10", "20", "30" } };
        yield return new object[] { "result = ARRAYTOREVERSED(aAXIS)", "result", new[] { "20", "10", "30" } };
        yield return new object[] { "ARRAYSORT(aAXIS)", "a", new[] { "10", "20", "30" } };
        yield return new object[] { "ARRAYREVERSE(aAXIS)", "a", new[] { "20", "10", "30" } };
        yield return new object[] { "PRINT ARRAYBINARYSEARCH(a, 20AXIS)", "a", new[] { "6", "10", "20", "30" } };
        yield return new object[] { "ARRAYPUSH(a, 44AXIS)", "a", new[] { "30", "10", "20", "44" } };
        yield return new object[] { "PRINT ARRAYPOP(aAXIS)", "a", new[] { "20", "30", "10" } };
        yield return new object[] { "ARRAYINSERT(a, 6, 44AXIS)", "a", new[] { "30", "44", "10", "20" } };
        yield return new object[] { "PRINT ARRAYREMOVE(a, 6AXIS)", "a", new[] { "10", "30", "20" } };
        yield return new object[] { "result = ARRAYCONCAT(a, aAXIS)", "result", new[] { "30", "10", "20", "30", "10", "20" } };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SortingShortMultidimensionalArraysStillValidatesAxis(bool empty)
    {
        var setup = "DIM seed(1 TO 1, 7 TO 7) AS INTEGER\nDIM a(,) AS INTEGER\nDIM result(,) AS INTEGER\na = " + (empty ? "ARRAYTAKE(seed, 0, 1)" : "ARRAYCLONE(seed)") + "\n";
        foreach (var axis in new[] { 0, 3 })
            foreach (var statement in new[] { $"ARRAYSORT(a, {axis})", $"result = ARRAYTOSORTED(a, {axis})" })
            {
                var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + "TRY\n" + statement + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a)\nPRINT LBOUND(a, 2)\nPRINT UBOUND(a, 2)"));
                Assert.True(run.Succeeded);
                Assert.Equal(new[] { "MB3007", empty ? "0" : "1", "7", "7" }, run.Output);
            }
        var valid = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + "ARRAYSORT(a, 2)\nresult = ARRAYTOSORTED(a, 2)\nPRINT ARRAYLENGTH(result)\nPRINT LBOUND(result, 2)\nPRINT UBOUND(result, 2)"));
        Assert.True(valid.Succeeded);
        Assert.Equal(new[] { empty ? "0" : "1", "7", "7" }, valid.Output);
    }

    [Theory]
    [MemberData(nameof(ValidOperations))]
    public void OneDimensionalOperationsWorkWithOmittedOrValidAxis(string template, string target, string[] expected)
    {
        var binarySearch = template.Contains("BINARYSEARCH", StringComparison.Ordinal);
        var setup = "DIM a() AS INTEGER\nREDIM a(5 TO 7)\nDIM result() AS INTEGER\n";
        setup += binarySearch ? "a(5) = 10\na(6) = 20\na(7) = 30\n" : "a(5) = 30\na(6) = 10\na(7) = 20\n";
        foreach (var axis in new[] { "", ", 1" })

            {
                var source = setup + template.Replace("AXIS", axis, StringComparison.Ordinal) + $"\nFOR i = LBOUND({target}) TO UBOUND({target})\nPRINT {target}(i)\nNEXT i";
                var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
                Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
                Assert.Equal(expected, run.Output);
            }
    }

    [Theory]
    [MemberData(nameof(InvalidAxes))]
    public void ExplicitInvalidAxisIsRejectedBeforeMutation(string statement, int length)
    {
        var setup = "DIM a() AS INTEGER\nDIM result() AS INTEGER\n";
        if (length == 0)
            setup += "DIM seed(1 TO 1) AS INTEGER\na = ARRAYTAKE(seed, 0)\n";
        else
        {
            setup += $"REDIM a(5 TO {4 + length})\n";
            for (var i = 0; i < length; i++)
                setup += $"a({5 + i}) = {(i + 1) * 11}\n";
        }
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + statement));
        Assert.False(second.Succeeded);
        var diagnostic = Assert.Single(second.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("dimension", diagnostic.Message, StringComparison.Ordinal);

        {
            var source = setup + "TRY\n" + statement + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a)\nFOR i = LBOUND(a) TO UBOUND(a)\nPRINT a(i)\nNEXT i";
            var caught = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(caught.Succeeded, string.Join("\n", caught.Diagnostics.Select(d => d.Message)));
            var expected = new[] { "MB3007", length.ToString(System.Globalization.CultureInfo.InvariantCulture) }.Concat(Enumerable.Range(1, length).Select(i => (i * 11).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.Equal(expected, caught.Output);
        }
    }
}
