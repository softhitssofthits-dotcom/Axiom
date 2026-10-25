namespace Axiom.Execution.Tests;

public sealed class ArraySortByFieldContractTests
{
    private const string Declaration =
        "TYPE Product\r\n" +
        "    Name AS STRING\r\n" +
        "    Price AS INTEGER\r\n" +
        "END TYPE\r\n";

    private const string Basket =
        "DIM basket[1 TO 3] AS Product\r\n" +
        "basket[1] = Product(\"яблоко\", 30)\r\n" +
        "basket[2] = Product(\"апельсин\", 40)\r\n" +
        "basket[3] = Product(\"банан\", 20)\r\n";

    [Fact]
    public void SortedCopyOrdersByFieldAndLeavesSourceIntact()
    {
        const string source = Declaration + Basket +
            "DIM sorted[] AS Product\r\n" +
            "sorted[] = ARRAYTOSORTED(basket[], \"Price\")\r\n" +
            "PRINT sorted[1].Name; \" \"; sorted[1].Price\r\n" +
            "PRINT sorted[2].Name; \" \"; sorted[2].Price\r\n" +
            "PRINT sorted[3].Name; \" \"; sorted[3].Price\r\n" +
            "PRINT basket[1].Name";

        CheckBoth(source, new[] { "банан 20", "яблоко 30", "апельсин 40", "яблоко" });
    }

    [Fact]
    public void DescendingDirectionOrdersLargestFirst()
    {
        const string source = Declaration + Basket +
            "DIM sorted[] AS Product\r\n" +
            "sorted[] = ARRAYTOSORTED(basket[], \"Price\", STEP -1)\r\n" +
            "PRINT sorted[1].Price\r\n" +
            "PRINT sorted[2].Price\r\n" +
            "PRINT sorted[3].Price";

        CheckBoth(source, new[] { "40", "30", "20" });
    }

    [Fact]
    public void ExplicitAscendingMatchesImplicitAscending()
    {
        const string source = Declaration + Basket +
            "DIM implicit[] AS Product\r\n" +
            "DIM explicit[] AS Product\r\n" +
            "implicit[] = ARRAYTOSORTED(basket[], \"Price\")\r\n" +
            "explicit[] = ARRAYTOSORTED(basket[], \"Price\", STEP 1)\r\n" +
            "FOR index = 1 TO 3\r\n" +
            "PRINT implicit[index].Price = explicit[index].Price\r\n" +
            "NEXT index";

        CheckBoth(source, new[] { "TRUE", "TRUE", "TRUE" });
    }

    [Fact]
    public void InPlaceSortByStringFieldOrdersNames()
    {
        const string source = Declaration + Basket +
            "ARRAYSORT(basket[], \"Name\")\r\n" +
            "PRINT basket[1].Name\r\n" +
            "PRINT basket[2].Name\r\n" +
            "PRINT basket[3].Name";

        CheckBoth(source, new[] { "апельсин", "банан", "яблоко" });
    }

    [Fact]
    public void EqualKeysKeepOriginalOrder()
    {
        const string source = Declaration +
            "DIM basket[1 TO 3] AS Product\r\n" +
            "basket[1] = Product(\"яблоко\", 30)\r\n" +
            "basket[2] = Product(\"банан\", 20)\r\n" +
            "basket[3] = Product(\"груша\", 30)\r\n" +
            "DIM sorted[] AS Product\r\n" +
            "sorted[] = ARRAYTOSORTED(basket[], \"Price\")\r\n" +
            "PRINT sorted[1].Name\r\n" +
            "PRINT sorted[2].Name\r\n" +
            "PRINT sorted[3].Name";

        CheckBoth(source, new[] { "банан", "яблоко", "груша" });
    }

    [Fact]
    public void DescendingKeepsEqualKeysInOriginalOrder()
    {
        const string source = Declaration +
            "DIM basket[1 TO 3] AS Product\r\n" +
            "basket[1] = Product(\"яблоко\", 30)\r\n" +
            "basket[2] = Product(\"банан\", 20)\r\n" +
            "basket[3] = Product(\"груша\", 30)\r\n" +
            "DIM sorted[] AS Product\r\n" +
            "sorted[] = ARRAYTOSORTED(basket[], \"Price\", STEP -1)\r\n" +
            "PRINT sorted[1].Name\r\n" +
            "PRINT sorted[2].Name\r\n" +
            "PRINT sorted[3].Name";

        CheckBoth(source, new[] { "яблоко", "груша", "банан" });
    }

    [Fact]
    public void TemporalFieldSortsChronologically()
    {
        const string source =
            "TYPE Moment\r\n" +
            "    Tag AS STRING\r\n" +
            "    At AS DATETIME\r\n" +
            "END TYPE\r\n" +
            "DIM marks[1 TO 3] AS Moment\r\n" +
            "marks[1].Tag = \"позже\"\r\n" +
            "marks[1].At = \"2024-01-01 12:00:00\"\r\n" +
            "marks[2].Tag = \"раньше\"\r\n" +
            "marks[2].At = \"2024-01-01 08:00:00\"\r\n" +
            "marks[3].Tag = \"середина\"\r\n" +
            "marks[3].At = \"2024-01-01 10:00:00\"\r\n" +
            "ARRAYSORT(marks[], \"At\")\r\n" +
            "PRINT marks[1].Tag\r\n" +
            "PRINT marks[2].Tag\r\n" +
            "PRINT marks[3].Tag";

        CheckBoth(source, new[] { "раньше", "середина", "позже" });
    }

    [Fact]
    public void EmptyAndSingleElementArraysAreSafe()
    {
        const string source = Declaration +
            "DIM single[1 TO 1] AS Product\r\n" +
            "single[1] = Product(\"яблоко\", 30)\r\n" +
            "ARRAYSORT(single[], \"Price\")\r\n" +
            "DIM seed[7 TO 7] AS Product\r\n" +
            "DIM empty[] AS Product\r\n" +
            "empty[] = ARRAYTAKE(seed[], 0)\r\n" +
            "DIM copy[] AS Product\r\n" +
            "copy[] = ARRAYTOSORTED(empty[], \"Price\", STEP -1)\r\n" +
            "PRINT single[1].Name\r\n" +
            "PRINT ARRAYISALLOCATED(copy[])\r\n" +
            "PRINT ARRAYLENGTH(copy[])";

        CheckBoth(source, new[] { "яблоко", "TRUE", "0" });
    }

    [Fact]
    public void NestedRecordFieldIsNotSortableButInnerScalarIs()
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
            "DIM receipts[1 TO 2] AS Receipt\r\n" +
            "receipts[1].Number = 1\r\n" +
            "receipts[1].Item.Label = \"чай\"\r\n" +
            "receipts[1].Item.Price = 55\r\n" +
            "receipts[2].Number = 2\r\n" +
            "receipts[2].Item.Label = \"кофе\"\r\n" +
            "receipts[2].Item.Price = 40\r\n" +
            "ARRAYSORT(receipts[], \"Number\")\r\n" +
            "PRINT receipts[1].Item.Label\r\n" +
            "PRINT receipts[2].Item.Label";

        CheckBoth(source, new[] { "чай", "кофе" });
    }

    private static void CheckBoth(string source, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, run.Output);
    }
}
