namespace Axiom.Execution.Tests;

public sealed class ArrayLiteralContractTests
{
    [Fact]
    public void LiteralCreatesArrayStartingAtOne()
    {
        const string source =
            "DIM fruits[] AS STRING = [\"яблоко\", \"банан\", \"апельсин\"]\r\n" +
            "PRINT fruits[1]\r\n" +
            "PRINT fruits[2]\r\n" +
            "PRINT fruits[3]\r\n" +
            "PRINT ARRAYLENGTH(fruits[])\r\n" +
            "PRINT LBOUND(fruits[])\r\n" +
            "PRINT UBOUND(fruits[])";

        CheckBoth(source, new[] { "яблоко", "банан", "апельсин", "3", "1", "3" });
    }

    [Fact]
    public void EmptyLiteralKeepsCanonicalEmptyBounds()
    {
        const string source =
            "DIM values[] AS INTEGER = []\r\n" +
            "PRINT ARRAYLENGTH(values[])\r\n" +
            "PRINT LBOUND(values[])\r\n" +
            "PRINT UBOUND(values[])\r\n" +
            "PRINT ARRAYISALLOCATED(values[])";

        CheckBoth(source, new[] { "0", "1", "0", "TRUE" });
    }

    [Fact]
    public void ElementTypeIsInferredFromValues()
    {
        const string source =
            "DIM names[] = [\"груша\", \"слива\"]\r\n" +
            "DIM mixed[] = [1, 2.5]\r\n" +
            "PRINT names[2]\r\n" +
            "PRINT mixed[1]\r\n" +
            "PRINT mixed[2]\r\n" +
            "PRINT mixed[1] + mixed[2]";

        CheckBoth(source, new[] { "слива", "1", "2.5", "3.5" });
    }

    [Fact]
    public void ElementsAreEvaluatedOnceInOrder()
    {
        const string source =
            "DIM counter AS INTEGER\r\n" +
            "FUNCTION Bump() AS INTEGER\r\n" +
            "    counter = counter + 1\r\n" +
            "    RETURN counter\r\n" +
            "END FUNCTION\r\n" +
            "DIM values[] AS INTEGER = [Bump(), Bump(), Bump()]\r\n" +
            "PRINT values[1]\r\n" +
            "PRINT values[2]\r\n" +
            "PRINT values[3]\r\n" +
            "PRINT counter";

        CheckBoth(source, new[] { "1", "2", "3", "3" });
    }

    [Fact]
    public void LiteralAssignsWholeArrayAndKeepsOwnership()
    {
        const string source =
            "DIM values[] AS INTEGER\r\n" +
            "values[] = [7, 8, 9]\r\n" +
            "values[2] = 80\r\n" +
            "PRINT ARRAYLENGTH(values[])\r\n" +
            "PRINT values[1]\r\n" +
            "PRINT values[2]\r\n" +
            "PRINT values[3]";

        CheckBoth(source, new[] { "3", "7", "80", "9" });
    }

    [Fact]
    public void LiteralWorksAsArgumentAndReturnValue()
    {
        const string source =
            "FUNCTION Total(items[] AS INTEGER) AS INTEGER\r\n" +
            "    RETURN ARRAYSUM(items[])\r\n" +
            "END FUNCTION\r\n" +
            "FUNCTION MakeFruits() AS STRING[]\r\n" +
            "    RETURN [\"груша\", \"слива\"]\r\n" +
            "END FUNCTION\r\n" +
            "PRINT Total([4, 5, 6])\r\n" +
            "PRINT MakeFruits()[2]\r\n" +
            "PRINT ARRAYLENGTH(MakeFruits())";

        CheckBoth(source, new[] { "15", "слива", "2" });
    }

    [Fact]
    public void LiteralKeepsRecordValues()
    {
        const string source =
            "TYPE Product\r\n" +
            "    Name AS STRING\r\n" +
            "    Price AS INTEGER\r\n" +
            "END TYPE\r\n" +
            "DIM basket[] AS Product = [Product(\"яблоко\", 30), Product(\"банан\", 20)]\r\n" +
            "PRINT basket[1].Name; \" \"; basket[1].Price\r\n" +
            "PRINT basket[2].Name; \" \"; basket[2].Price\r\n" +
            "PRINT ARRAYLENGTH(basket[])";

        CheckBoth(source, new[] { "яблоко 30", "банан 20", "2" });
    }

    [Fact]
    public void LiteralIsAValueThatCanBeIndexedAndPassed()
    {
        const string source =
            "PRINT [10, 20, 30][2]\r\n" +
            "PRINT ARRAYSUM([1, 2, 3])\r\n" +
            "PRINT JOIN([\"я\", \"б\"], \"-\")";

        CheckBoth(source, new[] { "20", "6", "я-б" });
    }

    [Fact]
    public void EmptyLiteralIsAcceptedWhereParameterTypeIsKnown()
    {
        const string source =
            "FUNCTION Count(items[] AS INTEGER) AS INTEGER\r\n" +
            "    RETURN ARRAYLENGTH(items[])\r\n" +
            "END FUNCTION\r\n" +
            "PRINT Count([])";

        CheckBoth(source, new[] { "0" });
    }

    [Fact]
    public void LiteralArrayParticipatesInArrayFunctions()
    {
        const string source =
            "DIM values[] AS INTEGER = [30, 10, 20]\r\n" +
            "DIM sorted[] AS INTEGER = ARRAYTOSORTED(values[])\r\n" +
            "PRINT sorted[1], sorted[2], sorted[3]\r\n" +
            "PRINT ARRAYMIN(values[])\r\n" +
            "PRINT ARRAYMAX(values[])\r\n" +
            "DIM unique[] AS INTEGER = ARRAYDISTINCT([1, 1, 2])\r\n" +
            "PRINT ARRAYLENGTH(unique[])";

        CheckBoth(source, new[] { "10 20 30", "10", "30", "2" });
    }

    private static void CheckBoth(string source, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, run.Output);
    }
}
