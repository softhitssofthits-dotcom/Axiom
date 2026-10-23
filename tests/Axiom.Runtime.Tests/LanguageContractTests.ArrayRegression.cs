using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    private static DiagnosticBag GetBindingDiagnostics(string text)
    {
        var syntax = Parser.Parse(text, out var parseDiagnostics);
        Assert.Empty(parseDiagnostics);

        _ = Binder.Bind(syntax, out var bindDiagnostics, checkGraphicsModes: false);
        return bindDiagnostics;
    }


    [Fact]
    public void Execute_RedimRejectsAllocationBeyondElementLimit()
    {
        const string text = "DIM a[,] AS INTEGER\nREDIM a[1 TO 100000, 1 TO 100000]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3013");
        Assert.Contains("67108864", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_DimRejectsAllocationBeyondElementLimit()
    {
        const string text = "DIM a[1 TO 100000000] AS INTEGER";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3013");
        Assert.Contains("67108864", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_ArrayCloneOneDimensionalRecordsIsIndependent()
    {
        const string text = "TYPE Point\n    X AS INTEGER\nEND TYPE\n\nDIM source[1 TO 2] AS Point\nsource[1].X = 1\nsource[2].X = 2\n\nDIM copy[] AS Point\ncopy[] = ARRAYCLONE(source[])\ncopy[1].X = 99\n\nPRINT source[1].X\nPRINT copy[1].X";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "99"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneTwoDimensionalRecordsIsIndependent()
    {
        const string text = "TYPE Point\n    X AS INTEGER\nEND TYPE\n\nDIM source[1 TO 2, 1 TO 2] AS Point\nsource[1, 1].X = 1\n\nDIM copy[,] AS Point\ncopy[] = ARRAYCLONE(source[])\ncopy[1, 1].X = 77\n\nPRINT source[1, 1].X\nPRINT copy[1, 1].X";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneOneDimensionalFixedStringKeepsDeclaredLength()
    {
        const string text = "TYPE Holder\n    Names[1 TO 2] AS STRING * 3\nEND TYPE\n\nDIM longOnes[1 TO 2] AS STRING\nlongOnes[1] = \"ABCDE\"\nlongOnes[2] = \"FGHIJ\"\n\nDIM holder AS Holder\nholder.Names[] = longOnes[]\n\nPRINT LEN(holder.Names[1])\nPRINT holder.Names[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "ABC"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneTwoDimensionalFixedStringKeepsDeclaredLength()
    {
        const string text = "TYPE Holder\n    Names[1 TO 2, 1 TO 2] AS STRING * 3\nEND TYPE\n\nDIM longGrid[1 TO 2, 1 TO 2] AS STRING\nlongGrid[1, 1] = \"ABCDE\"\n\nDIM holder AS Holder\nholder.Names[] = longGrid[]\n\nPRINT LEN(holder.Names[1, 1])\nPRINT holder.Names[1, 1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "ABC"], host.Output);
    }

    [Fact]
    public void Execute_ArrayViewAfterRedimReportsExpiredView()
    {
        const string text = "DIM grid[1 TO 3, 1 TO 3] AS INTEGER\ngrid[2, 2] = 7\n\nDIM rowView[] AS INTEGER\nrowView[] = grid[2]\n\nREDIM grid[1 TO 5, 1 TO 5]\ngrid[2, 2] = 9\n\nPRINT rowView[2]";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3014");
        Assert.Equal(10, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ArrayViewAfterEraseReportsExpiredView()
    {
        const string text = "DIM grid[1 TO 2, 1 TO 2] AS INTEGER\ngrid[1, 1] = 4\n\nDIM rowView[] AS INTEGER\nrowView[] = grid[1]\n\nERASE grid[]\n\nPRINT rowView[1]";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3014");
        Assert.Equal(9, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ExpiredArrayViewIsCatchableAndRootStaysUsable()
    {
        const string text = "DIM grid[1 TO 3, 1 TO 3] AS INTEGER\ngrid[2, 2] = 7\n\nDIM rowView[] AS INTEGER\nrowView[] = grid[2]\n\nREDIM grid[1 TO 5, 1 TO 5]\ngrid[2, 2] = 9\n\nTRY\n    PRINT rowView[2]\nCATCH err\n    PRINT err.Code\nEND TRY\n\nPRINT grid[2, 2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3014", "9"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAndPopKeepLogicalOrderAndBounds()
    {
        const string text = "DIM values[] AS INTEGER\nREDIM values[1 TO 0]\n\nFOR index = 1 TO 3\n    ARRAYPUSH(values[], index * 10)\nNEXT\n\nPRINT ARRAYLENGTH(values[])\nPRINT values[1]; values[2]; values[3]\nPRINT ARRAYPOP(values[])\nPRINT ARRAYPOP(values[])\nPRINT ARRAYLENGTH(values[])\nPRINT LBOUND(values[]); UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "102030", "30", "20", "1", "11"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReusesPoppedCapacity()
    {
        const string text = "DIM values[] AS INTEGER\nREDIM values[1 TO 2]\nvalues[1] = 5\nvalues[2] = 6\n\nPRINT ARRAYPOP(values[])\nPRINT ARRAYPOP(values[])\nPRINT ARRAYLENGTH(values[])\n\nARRAYPUSH(values[], 7)\nARRAYPUSH(values[], 8)\n\nPRINT ARRAYLENGTH(values[])\nPRINT values[1]; values[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "5", "0", "2", "78"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushPreservesNonCanonicalLowerBound()
    {
        const string text = "DIM values[] AS INTEGER\nREDIM values[5 TO 5]\nvalues[5] = 11\n\nARRAYPUSH(values[], 12)\n\nPRINT LBOUND(values[])\nPRINT UBOUND(values[])\nPRINT values[5]; values[6]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "1112"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctKeepsFirstOccurrenceOrder()
    {
        const string text = "DIM values[1 TO 8] AS INTEGER\nvalues[1] = 3\nvalues[2] = 1\nvalues[3] = 3\nvalues[4] = 2\nvalues[5] = 1\nvalues[6] = 4\nvalues[7] = 2\nvalues[8] = 5\n\nDIM distinctValues[] AS INTEGER\ndistinctValues[] = ARRAYDISTINCT(values[])\n\nPRINT ARRAYLENGTH(distinctValues[])\nFOR index = 1 TO ARRAYLENGTH(distinctValues[])\n    PRINT distinctValues[index];\nNEXT\nPRINT \"\"";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "31245"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctStringComparisonIsOrdinal()
    {
        const string text = "DIM values[1 TO 5] AS STRING\nvalues[1] = \"beta\"\nvalues[2] = \"alpha\"\nvalues[3] = \"beta\"\nvalues[4] = \"Alpha\"\nvalues[5] = \"alpha\"\n\nDIM distinctValues[] AS STRING\ndistinctValues[] = ARRAYDISTINCT(values[])\n\nPRINT ARRAYLENGTH(distinctValues[])\nFOR index = 1 TO ARRAYLENGTH(distinctValues[])\n    PRINT distinctValues[index];\nNEXT\nPRINT \"\"";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "betaalphaAlpha"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushExpiresExistingViewsOfRoot()
    {
        const string text = "DIM values[1 TO 3] AS INTEGER\nvalues[1] = 1\n\nDIM part[] AS INTEGER\npart[] = values[1 TO 2]\n\nARRAYPUSH(values[], 4)\n\nTRY\n    PRINT part[1]\nCATCH err\n    PRINT err.Code\nEND TRY\n\nPRINT values[4]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3014", "4"], host.Output);
    }

    [Fact]
    public void Execute_FractionalIndexReportsBuiltinArgument()
    {
        const string text = "DIM values[1 TO 3] AS INTEGER\nPRINT values[2.9]";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_FractionalIndexOnStoreReportsBuiltinArgument()
    {
        const string text = "DIM values[1 TO 3] AS INTEGER\nvalues[2.9] = 5";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_IntegralDivisionResultIsAcceptedAsIndex()
    {
        const string text = "DIM values[1 TO 3] AS INTEGER\nvalues[2] = 42\nDIM position AS INTEGER = 4\nPRINT values[position / 2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42"], host.Output);
    }

    [Fact]
    public void Execute_FractionalBoundReportsBuiltinArgument()
    {
        const string text = "DIM values[1.5 TO 3] AS INTEGER";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_DescendingRangeReportsArrayBounds()
    {
        const string text = "DIM values[1 TO 5] AS INTEGER\nDIM part[] AS INTEGER\npart[] = values[4 TO 2]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
        Assert.Contains("нижнюю границу больше верхней", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ZeroCountSliceUsesCanonicalEmptyBounds()
    {
        const string text = "DIM grid[1 TO 2, 1 TO 3] AS INTEGER\nDIM part[,] AS INTEGER\npart[] = ARRAYSLICE(grid[], 99, 0, 2)\n\nPRINT ARRAYLENGTH(part[])\nPRINT LBOUND(part[], 1); UBOUND(part[], 1)\nPRINT LBOUND(part[], 2); UBOUND(part[], 2)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "12", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatRankMismatchReportsRank()
    {
        const string text = """
DIM left[1 TO 2] AS INTEGER
DIM right[1 TO 2, 1 TO 2] AS INTEGER
DIM merged[] AS INTEGER
merged[] = ARRAYCONCAT(left[], right[])
""";

        var diagnostics = GetBindingDiagnostics(text);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("Ранг правого массива", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ArrayInitializerReportsSemanticDiagnostic()
    {
        const string text = """
DIM values[1 TO 3] AS INTEGER = 5
""";

        var diagnostics = GetBindingDiagnostics(text);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("нельзя инициализировать", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => d.Code == "BC3001");
    }

    [Fact]
    public void Execute_ArrayMinAcceptsTemporalElements()
    {
        // Прежнее ограничение отвергало DATE в ARRAYMIN. Порядковое сравнение
        // временных скаляров уже поддержано, поэтому агрегат использует его же.
        const string text = "DIM values[1 TO 2] AS DATE\nvalues[1] = \"2024-02-29\"\nvalues[2] = \"2024-01-15\"\nPRINT ARRAYMIN(values[])";

        var diagnostics = GetBindingDiagnostics(text);

        Assert.DoesNotContain(diagnostics, static d => d.Code == "MB2003");
    }
}
