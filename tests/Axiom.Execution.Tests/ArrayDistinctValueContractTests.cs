namespace Axiom.Execution.Tests;

public sealed class ArrayDistinctValueContractTests
{
    [Theory]
    [InlineData("LONG", "9007199254740992", "9007199254740993")]
    [InlineData("DECIMAL", "9007199254740992", "9007199254740993")]
    public void AdjacentExactValuesAboveDoublePrecisionRemainDistinct(string type, string first, string second)
    {
        var source = $"DIM a(5 TO 7) AS {type}\nDIM result() AS {type}\na(5) = {first}\na(6) = {second}\na(7) = {first}\nresult = ARRAYDISTINCT(a)\nPRINT LBOUND(result)\nPRINT UBOUND(result)\nPRINT result(1)\nPRINT result(2)";
        CheckBoth(source, new[] { "1", "2", first, second });
    }

    [Fact]
    public void UnicodeDistinctUsesWholeOrdinalValuesAndFirstOccurrenceOrder()
    {
        const string source = """
DIM a(5 TO 10) AS STRING
DIM result() AS STRING
a(5) = "Ёж"
a(6) = "ёж"
a(7) = "😀"
a(8) = "Ёж"
a(9) = "é"
a(10) = "e" + CHRW(769)
result = ARRAYDISTINCT(a)
PRINT LBOUND(result)
PRINT UBOUND(result)
FOR i = 1 TO 5
PRINT result(i)
NEXT i
""";
        CheckBoth(source, new[] { "1", "5", "Ёж", "ёж", "😀", "é", "e\u0301" });
    }

    [Fact]
    public void EmptyDistinctResultIsAllocatedAndCompact()
    {
        const string source = """
DIM seed(7 TO 7) AS INTEGER
DIM a() AS INTEGER
DIM result() AS INTEGER
a = ARRAYTAKE(seed, 0)
result = ARRAYDISTINCT(a)
PRINT ARRAYISALLOCATED(result)
PRINT LBOUND(result)
PRINT UBOUND(result)
PRINT ARRAYLENGTH(result)
""";
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
