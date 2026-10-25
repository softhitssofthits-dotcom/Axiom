namespace Axiom.Execution.Tests;

public sealed class ForEachContractTests
{
    [Fact]
    public void ByValWalksValuesInOrder()
    {
        const string source =
            "DIM fruits[] AS STRING = [\"яблоко\", \"банан\", \"апельсин\"]\r\n" +
            "FOR EACH fruit IN fruits[]\r\n" +
            "    PRINT fruit\r\n" +
            "NEXT fruit";

        CheckBoth(source, new[] { "яблоко", "банан", "апельсин" });
    }

    [Fact]
    public void ByValDoesNotWriteBackToSource()
    {
        const string source =
            "DIM values[1 TO 3] AS INTEGER\r\n" +
            "values[1] = 1\r\n" +
            "values[2] = 2\r\n" +
            "values[3] = 3\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    item = 99\r\n" +
            "NEXT\r\n" +
            "PRINT values[1], values[2], values[3]";

        CheckBoth(source, new[] { "1 2 3" });
    }

    [Fact]
    public void MultidimensionalOrderChangesLastDimensionFastest()
    {
        const string source =
            "DIM grid[1 TO 2, 1 TO 3] AS INTEGER\r\n" +
            "FOR r = 1 TO 2\r\n" +
            "    FOR c = 1 TO 3\r\n" +
            "        grid[r, c] = r * 10 + c\r\n" +
            "    NEXT c\r\n" +
            "NEXT r\r\n" +
            "FOR EACH cell IN grid[]\r\n" +
            "    PRINT cell\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "11", "12", "13", "21", "22", "23" });
    }

    [Fact]
    public void NegativeAndZeroLowerBoundsAreHonoured()
    {
        const string source =
            "DIM values[-1 TO 1] AS INTEGER\r\n" +
            "values[-1] = 7\r\n" +
            "values[0] = 8\r\n" +
            "values[1] = 9\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "7", "8", "9" });
    }

    [Fact]
    public void EmptyArrayRunsZeroIterations()
    {
        const string source =
            "DIM values[] AS INTEGER = []\r\n" +
            "DIM count AS INTEGER\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    count = count + 1\r\n" +
            "NEXT\r\n" +
            "PRINT count";

        CheckBoth(source, new[] { "0" });
    }

    [Fact]
    public void SourceIsEvaluatedOnce()
    {
        const string source =
            "DIM calls AS INTEGER\r\n" +
            "FUNCTION Make() AS INTEGER[]\r\n" +
            "    calls = calls + 1\r\n" +
            "    DIM local[] AS INTEGER = [1, 2, 3]\r\n" +
            "    RETURN ARRAYCLONE(local[])\r\n" +
            "END FUNCTION\r\n" +
            "FOR EACH item IN Make()\r\n" +
            "    PRINT item\r\n" +
            "NEXT\r\n" +
            "PRINT calls";

        CheckBoth(source, new[] { "1", "2", "3", "1" });
    }

    [Fact]
    public void AsClauseConvertsEachValue()
    {
        const string source =
            "DIM values[1 TO 2] AS INTEGER\r\n" +
            "values[1] = 5\r\n" +
            "values[2] = 7\r\n" +
            "FOR EACH item AS DOUBLE IN values[]\r\n" +
            "    PRINT item / 2\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "2.5", "3.5" });
    }

    [Fact]
    public void ContinueAndExitForWorkInsideForEach()
    {
        const string source =
            "DIM values[1 TO 5] AS INTEGER\r\n" +
            "FOR index = 1 TO 5\r\n" +
            "    values[index] = index\r\n" +
            "NEXT index\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    IF item = 3 THEN\r\n" +
            "        CONTINUE FOR\r\n" +
            "    END IF\r\n" +
            "    IF item = 5 THEN\r\n" +
            "        EXIT FOR\r\n" +
            "    END IF\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "1", "2", "4" });
    }

    [Fact]
    public void RecordsAreCopiedByVal()
    {
        const string source =
            "TYPE Product\r\n" +
            "    Name AS STRING\r\n" +
            "    Price AS INTEGER\r\n" +
            "END TYPE\r\n" +
            "DIM basket[] AS Product = [Product(\"яблоко\", 30), Product(\"банан\", 20)]\r\n" +
            "FOR EACH item IN basket[]\r\n" +
            "    item.Price = 0\r\n" +
            "    PRINT item.Name\r\n" +
            "NEXT\r\n" +
            "PRINT basket[1].Price";

        CheckBoth(source, new[] { "яблоко", "банан", "30" });
    }

    [Fact]
    public void NestedForEachLoopsWalkBothArrays()
    {
        const string source =
            "DIM outer[1 TO 2] AS INTEGER\r\n" +
            "outer[1] = 1\r\n" +
            "outer[2] = 2\r\n" +
            "DIM inner[1 TO 2] AS INTEGER\r\n" +
            "inner[1] = 10\r\n" +
            "inner[2] = 20\r\n" +
            "FOR EACH a IN outer[]\r\n" +
            "    FOR EACH b IN inner[]\r\n" +
            "        PRINT a + b\r\n" +
            "    NEXT\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "11", "21", "12", "22" });
    }

    [Fact]
    public void LastElementAtIntegerMaxDoesNotOverflowCounter()
    {
        // Единственный элемент с верхней границей INTEGER_MAX раньше увеличивался
        // до переполнения ещё до проверки исчерпания диапазона.
        const string source =
            "DIM values[2147483647 TO 2147483647] AS INTEGER\r\n" +
            "values[2147483647] = 7\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT\r\n" +
            "PRINT \"готово\"";

        CheckBoth(source, new[] { "7", "готово" });
    }

    [Fact]
    public void ByRefModifierIsRejectedAtCheck()
    {
        const string source =
            "DIM values[] AS INTEGER = [1, 2]\r\n" +
            "FOR EACH BYREF item IN values[]\r\n" +
            "    item = 99\r\n" +
            "NEXT";

        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(run.Succeeded);
        Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Code == "MB2064");
    }

    [Fact]
    public void UnallocatedSourceFailsAtRuntime()
    {
        const string source =
            "DIM values[] AS INTEGER\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(run.Succeeded);
        Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Code == "MB3005");
    }

    private static void CheckBoth(string source, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, run.Output);
    }
}
