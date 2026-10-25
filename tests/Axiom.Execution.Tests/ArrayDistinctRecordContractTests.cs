namespace Axiom.Execution.Tests;

public sealed class ArrayDistinctRecordContractTests
{
    private const string Declaration =
        "TYPE Product\r\n" +
        "    Name AS STRING\r\n" +
        "    Price AS INTEGER\r\n" +
        "END TYPE\r\n";

    [Fact]
    public void RecordsKeepFirstOccurrenceOrderAndLeaveSourceIntact()
    {
        const string source = Declaration +
            "DIM basket[1 TO 3] AS Product\r\n" +
            "basket[1] = Product(\"яблоко\", 30)\r\n" +
            "basket[2] = Product(\"банан\", 20)\r\n" +
            "basket[3] = Product(\"яблоко\", 30)\r\n" +
            "DIM unique[] AS Product\r\n" +
            "unique[] = ARRAYDISTINCT(basket[])\r\n" +
            "PRINT ARRAYLENGTH(unique[])\r\n" +
            "PRINT unique[1].Name\r\n" +
            "PRINT unique[1].Price\r\n" +
            "PRINT unique[2].Name\r\n" +
            "PRINT basket[1].Name\r\n" +
            "PRINT basket[3].Price";

        CheckBoth(source, new[] { "2", "яблоко", "30", "банан", "яблоко", "30" });
    }

    [Fact]
    public void SameNameWithDifferentPriceStaysDistinct()
    {
        const string source = Declaration +
            "DIM basket[1 TO 3] AS Product\r\n" +
            "basket[1] = Product(\"яблоко\", 30)\r\n" +
            "basket[2] = Product(\"яблоко\", 40)\r\n" +
            "basket[3] = Product(\"яблоко\", 30)\r\n" +
            "DIM unique[] AS Product\r\n" +
            "unique[] = ARRAYDISTINCT(basket[])\r\n" +
            "PRINT ARRAYLENGTH(unique[])\r\n" +
            "PRINT unique[1].Price\r\n" +
            "PRINT unique[2].Price";

        CheckBoth(source, new[] { "2", "30", "40" });
    }

    [Fact]
    public void NestedRecordFieldsParticipateInDistinct()
    {
        const string source =
            "TYPE Good\r\n" +
            "    Label AS STRING\r\n" +
            "    Price AS INTEGER\r\n" +
            "END TYPE\r\n" +
            "TYPE Receipt\r\n" +
            "    Number AS INTEGER\r\n" +
            "    Item AS Good\r\n" +
            "END TYPE\r\n" +
            "DIM receipts[1 TO 3] AS Receipt\r\n" +
            "receipts[1].Number = 1\r\n" +
            "receipts[1].Item.Label = \"чай\"\r\n" +
            "receipts[1].Item.Price = 40\r\n" +
            "receipts[2].Number = 2\r\n" +
            "receipts[2].Item.Label = \"кофе\"\r\n" +
            "receipts[2].Item.Price = 55\r\n" +
            "receipts[3].Number = 1\r\n" +
            "receipts[3].Item.Label = \"чай\"\r\n" +
            "receipts[3].Item.Price = 40\r\n" +
            "DIM unique[] AS Receipt\r\n" +
            "unique[] = ARRAYDISTINCT(receipts[])\r\n" +
            "PRINT ARRAYLENGTH(unique[])\r\n" +
            "PRINT unique[1].Item.Label\r\n" +
            "PRINT unique[2].Item.Label";

        CheckBoth(source, new[] { "2", "чай", "кофе" });
    }

    [Fact]
    public void TemporalFieldsParticipateInDistinct()
    {
        const string source =
            "TYPE Moment\r\n" +
            "    Tag AS STRING\r\n" +
            "    At AS DATETIME\r\n" +
            "END TYPE\r\n" +
            "DIM marks[1 TO 3] AS Moment\r\n" +
            "marks[1].Tag = \"начало\"\r\n" +
            "marks[1].At = \"2024-01-01 10:00:00\"\r\n" +
            "marks[2].Tag = \"конец\"\r\n" +
            "marks[2].At = \"2024-01-01 10:00:01\"\r\n" +
            "marks[3].Tag = \"начало\"\r\n" +
            "marks[3].At = \"2024-01-01 10:00:00\"\r\n" +
            "DIM unique[] AS Moment\r\n" +
            "unique[] = ARRAYDISTINCT(marks[])\r\n" +
            "PRINT ARRAYLENGTH(unique[])\r\n" +
            "PRINT unique[1].Tag\r\n" +
            "PRINT unique[2].Tag\r\n" +
            "PRINT unique[1].At\r\n" +
            "PRINT unique[2].At";

        CheckBoth(source, new[] { "2", "начало", "конец", "2024-01-01 10:00:00", "2024-01-01 10:00:01" });
    }

    [Fact]
    public void AllocatedEmptyRecordArrayGivesCompactResult()
    {
        const string source = Declaration +
            "DIM seed[7 TO 7] AS Product\r\n" +
            "DIM empty[] AS Product\r\n" +
            "DIM result[] AS Product\r\n" +
            "empty[] = ARRAYTAKE(seed[], 0)\r\n" +
            "result[] = ARRAYDISTINCT(empty[])\r\n" +
            "PRINT ARRAYISALLOCATED(result[])\r\n" +
            "PRINT LBOUND(result[])\r\n" +
            "PRINT UBOUND(result[])\r\n" +
            "PRINT ARRAYLENGTH(result[])";

        CheckBoth(source, new[] { "TRUE", "1", "0", "0" });
    }

    [Fact]
    public void ViewInputDistinctKeepsFirstOccurrenceOrder()
    {
        const string source = Declaration +
            "DIM basket[1 TO 4] AS Product\r\n" +
            "basket[1] = Product(\"яблоко\", 30)\r\n" +
            "basket[2] = Product(\"банан\", 20)\r\n" +
            "basket[3] = Product(\"яблоко\", 30)\r\n" +
            "basket[4] = Product(\"груша\", 25)\r\n" +
            "DIM unique[] AS Product\r\n" +
            "unique[] = ARRAYDISTINCT(ARRAYTAKE(basket[], 3))\r\n" +
            "PRINT ARRAYLENGTH(unique[])\r\n" +
            "PRINT unique[1].Name\r\n" +
            "PRINT unique[2].Name";

        CheckBoth(source, new[] { "2", "яблоко", "банан" });
    }

    private static void CheckBoth(string source, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, run.Output);
    }
}
