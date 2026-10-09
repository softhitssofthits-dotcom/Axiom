namespace Axiom.Execution.Tests;

public sealed class ArraySearchContractTests
{
    [Theory]
    [InlineData(-4)]
    [InlineData(5)]
    [InlineData(-2147483647)]
    [InlineData(2147483645)]
    public void OneDimensionalSearchReturnsLogicalIndicesAndLowerMinusOne(int lower)
    {
        var middle = lower + 1;
        var upper = lower + 2;
        var source = $"DIM a({lower} TO {upper}) AS INTEGER\nDIM hits() AS INTEGER\na({lower}) = 2\na({middle}) = 2\na({upper}) = 4\nPRINT ARRAYCONTAINS(a, 2)\nPRINT ARRAYCONTAINS(a, 99)\nPRINT ARRAYCOUNT(a, 2)\nPRINT ARRAYINDEXOF(a, 2)\nPRINT ARRAYLASTINDEXOF(a, 2)\nPRINT ARRAYBINARYSEARCH(a, 2)\nPRINT ARRAYBINARYSEARCH(a, 4, 1)\nPRINT ARRAYINDEXOF(a, 99)\nPRINT ARRAYLASTINDEXOF(a, 99)\nPRINT ARRAYBINARYSEARCH(a, 99)\nhits = ARRAYFINDALL(a, 2)\nPRINT LBOUND(hits)\nPRINT UBOUND(hits)\nPRINT hits(1)\nPRINT hits(2)";
        var absent = ((long)lower - 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        CheckBoth(source, new[] { "TRUE", "FALSE", "2", lower.ToString(), middle.ToString(), lower.ToString(), upper.ToString(), absent, absent, absent, "1", "2", lower.ToString(), middle.ToString() });
    }

    [Fact]
    public void MultidimensionalSearchReturnsCoordinatesInVisibleTraversalOrder()
    {
        const string source = """
DIM a(-2 TO -1, 4 TO 6) AS INTEGER
DIM position() AS INTEGER
DIM hits(,) AS INTEGER
a(-2, 4) = 2
a(-2, 5) = 2
a(-2, 6) = 4
a(-1, 4) = 1
a(-1, 5) = 2
a(-1, 6) = 3
PRINT ARRAYCOUNT(a, 2)
position = ARRAYINDEXOF(a, 2)
PRINT position(1)
PRINT position(2)
position = ARRAYLASTINDEXOF(a, 2)
PRINT position(1)
PRINT position(2)
hits = ARRAYFINDALL(a, 2)
PRINT ARRAYRANK(hits)
PRINT LBOUND(hits, 1)
PRINT UBOUND(hits, 1)
PRINT LBOUND(hits, 2)
PRINT UBOUND(hits, 2)
FOR i = 1 TO 3
FOR j = 1 TO 2
PRINT hits(i, j)
NEXT j
NEXT i
position = ARRAYINDEXOF(a, 99)
PRINT ARRAYISALLOCATED(position)
PRINT ARRAYLENGTH(position)
PRINT LBOUND(position)
PRINT UBOUND(position)
hits = ARRAYFINDALL(a, 99)
PRINT ARRAYLENGTH(hits)
PRINT UBOUND(hits, 1)
PRINT UBOUND(hits, 2)
""";
        CheckBoth(source, new[] { "3", "-2", "4", "-1", "5", "2", "1", "3", "1", "2", "-2", "4", "-2", "5", "-1", "5", "TRUE", "0", "1", "0", "0", "0", "2" });
    }

    [Theory]
    [InlineData(1, "-1", "4")]
    [InlineData(2, "-2", "5")]
    public void BinarySearchUsesFirstMatchingLineOfSelectedDimension(int axis, string first, string second)
    {
        var source = $"DIM a(-2 TO -1, 4 TO 6) AS INTEGER\nDIM position() AS INTEGER\na(-2, 4) = 1\na(-2, 5) = 2\na(-2, 6) = 2\na(-1, 4) = 2\na(-1, 5) = 3\na(-1, 6) = 4\nposition = ARRAYBINARYSEARCH(a, 2, {axis})\nPRINT position(1)\nPRINT position(2)\nposition = ARRAYBINARYSEARCH(a, 99, {axis})\nPRINT ARRAYLENGTH(position)";
        CheckBoth(source, new[] { first, second, "0" });
    }

    [Fact]
    public void EmptyAllocatedSearchResultsKeepTheirDifferentContracts()
    {
        const string source = """
DIM seed(-4 TO -4, 7 TO 8) AS INTEGER
DIM empty(,) AS INTEGER
DIM flat() AS INTEGER
DIM position() AS INTEGER
DIM hits(,) AS INTEGER
empty = ARRAYTAKE(seed, 0, 1)
flat = ARRAYFLATTEN(empty)
PRINT ARRAYCONTAINS(flat, 0)
PRINT ARRAYCOUNT(flat, 0)
PRINT ARRAYINDEXOF(flat, 0)
PRINT ARRAYLASTINDEXOF(flat, 0)
PRINT ARRAYBINARYSEARCH(flat, 0)
position = ARRAYINDEXOF(empty, 0)
PRINT ARRAYLENGTH(position)
hits = ARRAYFINDALL(empty, 0)
PRINT ARRAYRANK(hits)
PRINT UBOUND(hits, 1)
PRINT UBOUND(hits, 2)
position = ARRAYBINARYSEARCH(empty, 0, 1)
PRINT ARRAYLENGTH(position)
""";
        CheckBoth(source, new[] { "FALSE", "0", "0", "0", "0", "0", "2", "0", "2", "0" });
    }

    [Fact]
    public void SingleElementAtMaximumIndexCanBeFoundWithoutOverflow()
    {
        const string source = """
DIM a(2147483647 TO 2147483647) AS INTEGER
PRINT ARRAYCONTAINS(a, 42)
PRINT ARRAYINDEXOF(a, 0)
PRINT ARRAYLASTINDEXOF(a, 0)
PRINT ARRAYBINARYSEARCH(a, 0)
PRINT ARRAYINDEXOF(a, 42)
""";
        CheckBoth(source, new[] { "FALSE", "2147483647", "2147483647", "2147483647", "2147483646" });
    }


    [Theory]
    [InlineData("ARRAYCONTAINS")]
    [InlineData("ARRAYCOUNT")]
    [InlineData("ARRAYINDEXOF")]
    [InlineData("ARRAYLASTINDEXOF")]
    [InlineData("ARRAYFINDALL")]
    [InlineData("ARRAYBINARYSEARCH")]
    public void UnallocatedInputHasMatchingDiagnostic(string function)
    {
        var source = "DIM a() AS INTEGER\nPRINT " + function + "(a, 1)";
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal("MB3005", Assert.Single(first.Diagnostics).Code);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }

    [Theory]
    [InlineData("ARRAYCONTAINS")]
    [InlineData("ARRAYCOUNT")]
    [InlineData("ARRAYINDEXOF")]
    [InlineData("ARRAYLASTINDEXOF")]
    [InlineData("ARRAYFINDALL")]
    [InlineData("ARRAYBINARYSEARCH")]
    public void SearchValueMustMatchArrayElementType(string function)
    {
        var source = "DIM a(1 TO 1) AS INTEGER\nPRINT " + function + "(a, TRUE)";
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.False(second.UsedInterpreterFallback);
        Assert.NotEmpty(first.Diagnostics);
        Assert.All(first.Diagnostics, d => Assert.StartsWith("MB2", d.Code));
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }

    [Fact]
    public void UnicodeStringsAreMatchedAsWholeValuesAndCaseIsSignificant()
    {
        const string source = """
DIM a(-2 TO 1) AS STRING
a(-2) = "Ёж"
a(-1) = "ёж"
a(0) = "😀"
a(1) = "Ёж"
PRINT ARRAYCONTAINS(a, "Ёж")
PRINT ARRAYCOUNT(a, "Ёж")
PRINT ARRAYINDEXOF(a, "Ёж")
PRINT ARRAYLASTINDEXOF(a, "Ёж")
PRINT ARRAYINDEXOF(a, "😀")
PRINT ARRAYINDEXOF(a, "Еж")
PRINT ARRAYCOUNT(a, "ёж")
""";
        CheckBoth(source, new[] { "TRUE", "2", "-2", "1", "0", "-3", "1" });
    }

    private static void CheckBoth(string source, string[] expected)
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(expected, run.Output);
        }
    }
}
