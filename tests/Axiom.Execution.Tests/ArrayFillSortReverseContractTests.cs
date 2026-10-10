namespace Axiom.Execution.Tests;

public sealed class ArrayFillSortReverseContractTests
{
    private const string Setup = """
DIM a(-2 TO -1, 7 TO 9) AS INTEGER
DIM result(,) AS INTEGER
a(-2, 7) = 30
a(-2, 8) = 10
a(-2, 9) = 20
a(-1, 7) = 6
a(-1, 8) = 4
a(-1, 9) = 5
""";
    public static IEnumerable<object[]> Cases()
    {
        foreach (var copy in new[] { false, true })
        {
            yield return new object[] { copy ? "result = ARRAYTOSORTED(a, 1)" : "ARRAYSORT(a, 1)\nresult = ARRAYCLONE(a)", copy, new[] { "6", "4", "5", "30", "10", "20" } };
            yield return new object[] { copy ? "result = ARRAYTOSORTED(a, 2)" : "ARRAYSORT(a, 2)\nresult = ARRAYCLONE(a)", copy, new[] { "10", "20", "30", "4", "5", "6" } };
            yield return new object[] { copy ? "result = ARRAYTOREVERSED(a, 1)" : "ARRAYREVERSE(a, 1)\nresult = ARRAYCLONE(a)", copy, new[] { "6", "4", "5", "30", "10", "20" } };
            yield return new object[] { copy ? "result = ARRAYTOREVERSED(a, 2)" : "ARRAYREVERSE(a, 2)\nresult = ARRAYCLONE(a)", copy, new[] { "20", "10", "30", "5", "4", "6" } };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MultidimensionalTransformPreservesBoundsAndExpectedLineOrder(string statement, bool copy, string[] values)
    {
        var source = Setup + "\n" + statement + "\nPRINT LBOUND(result, 1)\nPRINT UBOUND(result, 1)\nPRINT LBOUND(result, 2)\nPRINT UBOUND(result, 2)\nFOR i = -2 TO -1\nFOR j = 7 TO 9\nPRINT result(i, j)\nNEXT j\nNEXT i\nPRINT a(-2, 7)";
        CheckBoth(source, new[] { "-2", "-1", "7", "9" }.Concat(values).Append(copy ? "30" : values[0]).ToArray());
    }

    [Fact]
    public void FillThroughViewChangesOnlyVisibleOwnerElements()
    {
        var source = Setup + "\nARRAYFILL(a(-2), 99)\nFOR i = -2 TO -1\nFOR j = 7 TO 9\nPRINT a(i, j)\nNEXT j\nNEXT i\nPRINT LBOUND(a, 1)\nPRINT UBOUND(a, 2)";
        CheckBoth(source, new[] { "99", "99", "99", "6", "4", "5", "-2", "9" });
    }

    [Fact]
    public void FillHandlesEmptyAndMaximumIndexWithoutChangingShape()
    {
        var source = "DIM a(2147483647 TO 2147483647) AS INTEGER\nDIM empty() AS INTEGER\nARRAYFILL(a, 42)\nempty = ARRAYTAKE(a, 0)\nARRAYFILL(empty, 99)\nPRINT a(2147483647)\nPRINT LBOUND(a)\nPRINT UBOUND(a)\nPRINT ARRAYISALLOCATED(empty)\nPRINT ARRAYLENGTH(empty)";
        CheckBoth(source, new[] { "42", "2147483647", "2147483647", "TRUE", "0" });
    }

    private static void CheckBoth(string source, string[] expected)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
}
