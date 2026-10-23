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
        var source = $"DIM a[{lower} TO {upper}] AS INTEGER\nDIM hits[] AS INTEGER\na[{lower}] = 2\na[{middle}] = 2\na[{upper}] = 4\nPRINT ARRAYCONTAINS(a[], 2)\nPRINT ARRAYCONTAINS(a[], 99)\nPRINT ARRAYCOUNT(a[], 2)\nPRINT ARRAYINDEXOF(a[], 2)\nPRINT ARRAYLASTINDEXOF(a[], 2)\nPRINT ARRAYBINARYSEARCH(a[], 2)\nPRINT ARRAYBINARYSEARCH(a[], 4, 1)\nPRINT ARRAYINDEXOF(a[], 99)\nPRINT ARRAYLASTINDEXOF(a[], 99)\nPRINT ARRAYBINARYSEARCH(a[], 99)\nhits[] = ARRAYFINDALL(a[], 2)\nPRINT LBOUND(hits[])\nPRINT UBOUND(hits[])\nPRINT hits[1]\nPRINT hits[2]";
        var absent = ((long)lower - 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        CheckBoth(source, new[] { "TRUE", "FALSE", "2", lower.ToString(), middle.ToString(), lower.ToString(), upper.ToString(), absent, absent, absent, "1", "2", lower.ToString(), middle.ToString() });
    }

    [Fact]
    public void MultidimensionalSearchReturnsCoordinatesInVisibleTraversalOrder()
    {
        const string source = "DIM a[-2 TO -1, 4 TO 6] AS INTEGER\r\nDIM position[] AS INTEGER\r\nDIM hits[,] AS INTEGER\r\na[-2, 4] = 2\r\na[-2, 5] = 2\r\na[-2, 6] = 4\r\na[-1, 4] = 1\r\na[-1, 5] = 2\r\na[-1, 6] = 3\r\nPRINT ARRAYCOUNT(a[], 2)\r\nposition[] = ARRAYINDEXOF(a[], 2)\r\nPRINT position[1]\r\nPRINT position[2]\r\nposition[] = ARRAYLASTINDEXOF(a[], 2)\r\nPRINT position[1]\r\nPRINT position[2]\r\nhits[] = ARRAYFINDALL(a[], 2)\r\nPRINT ARRAYRANK(hits[])\r\nPRINT LBOUND(hits[], 1)\r\nPRINT UBOUND(hits[], 1)\r\nPRINT LBOUND(hits[], 2)\r\nPRINT UBOUND(hits[], 2)\r\nFOR i = 1 TO 3\r\nFOR j = 1 TO 2\r\nPRINT hits[i, j]\r\nNEXT j\r\nNEXT i\r\nposition[] = ARRAYINDEXOF(a[], 99)\r\nPRINT ARRAYISALLOCATED(position[])\r\nPRINT ARRAYLENGTH(position[])\r\nPRINT LBOUND(position[])\r\nPRINT UBOUND(position[])\r\nhits[] = ARRAYFINDALL(a[], 99)\r\nPRINT ARRAYLENGTH(hits[])\r\nPRINT UBOUND(hits[], 1)\r\nPRINT UBOUND(hits[], 2)";
        CheckBoth(source, new[] { "3", "-2", "4", "-1", "5", "2", "1", "3", "1", "2", "-2", "4", "-2", "5", "-1", "5", "TRUE", "0", "1", "0", "0", "0", "2" });
    }

    [Theory]
    [InlineData(1, "-1", "4")]
    [InlineData(2, "-2", "5")]
    public void BinarySearchUsesFirstMatchingLineOfSelectedDimension(int axis, string first, string second)
    {
        var source = $"DIM a[-2 TO -1, 4 TO 6] AS INTEGER\nDIM position[] AS INTEGER\na[-2, 4] = 1\na[-2, 5] = 2\na[-2, 6] = 2\na[-1, 4] = 2\na[-1, 5] = 3\na[-1, 6] = 4\nposition[] = ARRAYBINARYSEARCH(a[], 2, {axis})\nPRINT position[1]\nPRINT position[2]\nposition[] = ARRAYBINARYSEARCH(a[], 99, {axis})\nPRINT ARRAYLENGTH(position[])";
        CheckBoth(source, new[] { first, second, "0" });
    }

    [Fact]
    public void EmptyAllocatedSearchResultsKeepTheirDifferentContracts()
    {
        const string source = "DIM seed[-4 TO -4, 7 TO 8] AS INTEGER\r\nDIM empty[,] AS INTEGER\r\nDIM flat[] AS INTEGER\r\nDIM position[] AS INTEGER\r\nDIM hits[,] AS INTEGER\r\nempty[] = ARRAYTAKE(seed[], 0, 1)\r\nflat[] = ARRAYFLATTEN(empty[])\r\nPRINT ARRAYCONTAINS(flat[], 0)\r\nPRINT ARRAYCOUNT(flat[], 0)\r\nPRINT ARRAYINDEXOF(flat[], 0)\r\nPRINT ARRAYLASTINDEXOF(flat[], 0)\r\nPRINT ARRAYBINARYSEARCH(flat[], 0)\r\nposition[] = ARRAYINDEXOF(empty[], 0)\r\nPRINT ARRAYLENGTH(position[])\r\nhits[] = ARRAYFINDALL(empty[], 0)\r\nPRINT ARRAYRANK(hits[])\r\nPRINT UBOUND(hits[], 1)\r\nPRINT UBOUND(hits[], 2)\r\nposition[] = ARRAYBINARYSEARCH(empty[], 0, 1)\r\nPRINT ARRAYLENGTH(position[])";
        CheckBoth(source, new[] { "FALSE", "0", "0", "0", "0", "0", "2", "0", "2", "0" });
    }

    [Fact]
    public void SingleElementAtMaximumIndexCanBeFoundWithoutOverflow()
    {
        const string source = "DIM a[2147483647 TO 2147483647] AS INTEGER\r\nPRINT ARRAYCONTAINS(a[], 42)\r\nPRINT ARRAYINDEXOF(a[], 0)\r\nPRINT ARRAYLASTINDEXOF(a[], 0)\r\nPRINT ARRAYBINARYSEARCH(a[], 0)\r\nPRINT ARRAYINDEXOF(a[], 42)";
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
        var source = "DIM a[] AS INTEGER\nPRINT " + function + "(a[], 1)";
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.Equal("MB3005", Assert.Single(second.Diagnostics).Code);
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
        var source = "DIM a[1 TO 1] AS INTEGER\nPRINT " + function + "(a[], TRUE)";
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.NotEmpty(second.Diagnostics);
        Assert.All(second.Diagnostics, d => Assert.StartsWith("MB2", d.Code));
    }

    [Fact]
    public void UnicodeStringsAreMatchedAsWholeValuesAndCaseIsSignificant()
    {
        const string source = "DIM a[-2 TO 1] AS STRING\r\na[-2] = \"Ёж\"\r\na[-1] = \"ёж\"\r\na[0] = \"😀\"\r\na[1] = \"Ёж\"\r\nPRINT ARRAYCONTAINS(a[], \"Ёж\")\r\nPRINT ARRAYCOUNT(a[], \"Ёж\")\r\nPRINT ARRAYINDEXOF(a[], \"Ёж\")\r\nPRINT ARRAYLASTINDEXOF(a[], \"Ёж\")\r\nPRINT ARRAYINDEXOF(a[], \"😀\")\r\nPRINT ARRAYINDEXOF(a[], \"Еж\")\r\nPRINT ARRAYCOUNT(a[], \"ёж\")";
        CheckBoth(source, new[] { "TRUE", "2", "-2", "1", "0", "-3", "1" });
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
