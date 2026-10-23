namespace Axiom.Execution.Tests;

public sealed class ArrayDistinctValueContractTests
{
    [Theory]
    [InlineData("LONG", "9007199254740992", "9007199254740993")]
    [InlineData("DECIMAL", "9007199254740992", "9007199254740993")]
    public void AdjacentExactValuesAboveDoublePrecisionRemainDistinct(string type, string first, string second)
    {
        var source = $"DIM a[5 TO 7] AS {type}\nDIM result[] AS {type}\na[5] = {first}\na[6] = {second}\na[7] = {first}\nresult[] = ARRAYDISTINCT(a[])\nPRINT LBOUND(result[])\nPRINT UBOUND(result[])\nPRINT result[1]\nPRINT result[2]";
        CheckBoth(source, new[] { "1", "2", first, second });
    }

    [Fact]
    public void UnicodeDistinctUsesWholeOrdinalValuesAndFirstOccurrenceOrder()
    {
        const string source = "DIM a[5 TO 10] AS STRING\r\nDIM result[] AS STRING\r\na[5] = \"Ёж\"\r\na[6] = \"ёж\"\r\na[7] = \"😀\"\r\na[8] = \"Ёж\"\r\na[9] = \"é\"\r\na[10] = \"e\" + CHRW(769)\r\nresult[] = ARRAYDISTINCT(a[])\r\nPRINT LBOUND(result[])\r\nPRINT UBOUND(result[])\r\nFOR i = 1 TO 5\r\nPRINT result[i]\r\nNEXT i";
        CheckBoth(source, new[] { "1", "5", "Ёж", "ёж", "😀", "é", "e\u0301" });
    }

    [Fact]
    public void EmptyDistinctResultIsAllocatedAndCompact()
    {
        const string source = "DIM seed[7 TO 7] AS INTEGER\r\nDIM a[] AS INTEGER\r\nDIM result[] AS INTEGER\r\na[] = ARRAYTAKE(seed[], 0)\r\nresult[] = ARRAYDISTINCT(a[])\r\nPRINT ARRAYISALLOCATED(result[])\r\nPRINT LBOUND(result[])\r\nPRINT UBOUND(result[])\r\nPRINT ARRAYLENGTH(result[])";
        CheckBoth(source, new[] { "TRUE", "1", "0", "0" });
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
