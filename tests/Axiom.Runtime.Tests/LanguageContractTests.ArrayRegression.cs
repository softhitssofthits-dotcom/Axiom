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
        const string text = """
DIM a(,) AS INTEGER
REDIM a(1 TO 100000, 1 TO 100000)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3013");
        Assert.Contains("67108864", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_DimRejectsAllocationBeyondElementLimit()
    {
        const string text = """
DIM a(1 TO 100000000) AS INTEGER
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3013");
        Assert.Contains("67108864", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_ArrayCloneOneDimensionalRecordsIsIndependent()
    {
        const string text = """
TYPE Point
    X AS INTEGER
END TYPE

DIM source(1 TO 2) AS Point
source(1).X = 1
source(2).X = 2

DIM copy() AS Point
copy = ARRAYCLONE(source)
copy(1).X = 99

PRINT source(1).X
PRINT copy(1).X
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "99"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneTwoDimensionalRecordsIsIndependent()
    {
        const string text = """
TYPE Point
    X AS INTEGER
END TYPE

DIM source(1 TO 2, 1 TO 2) AS Point
source(1, 1).X = 1

DIM copy(,) AS Point
copy = ARRAYCLONE(source)
copy(1, 1).X = 77

PRINT source(1, 1).X
PRINT copy(1, 1).X
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneOneDimensionalFixedStringKeepsDeclaredLength()
    {
        const string text = """
TYPE Holder
    Names(1 TO 2) AS STRING * 3
END TYPE

DIM longOnes(1 TO 2) AS STRING
longOnes(1) = "ABCDE"
longOnes(2) = "FGHIJ"

DIM holder AS Holder
holder.Names = longOnes

PRINT LEN(holder.Names(1))
PRINT holder.Names(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "ABC"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneTwoDimensionalFixedStringKeepsDeclaredLength()
    {
        const string text = """
TYPE Holder
    Names(1 TO 2, 1 TO 2) AS STRING * 3
END TYPE

DIM longGrid(1 TO 2, 1 TO 2) AS STRING
longGrid(1, 1) = "ABCDE"

DIM holder AS Holder
holder.Names = longGrid

PRINT LEN(holder.Names(1, 1))
PRINT holder.Names(1, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "ABC"], host.Output);
    }

    [Fact]
    public void Execute_ArrayViewAfterRedimReportsExpiredView()
    {
        const string text = """
DIM grid(1 TO 3, 1 TO 3) AS INTEGER
grid(2, 2) = 7

DIM rowView() AS INTEGER
rowView = grid(2)

REDIM grid(1 TO 5, 1 TO 5)
grid(2, 2) = 9

PRINT rowView(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3014");
        Assert.Equal(10, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ArrayViewAfterEraseReportsExpiredView()
    {
        const string text = """
DIM grid(1 TO 2, 1 TO 2) AS INTEGER
grid(1, 1) = 4

DIM rowView() AS INTEGER
rowView = grid(1)

ERASE grid

PRINT rowView(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3014");
        Assert.Equal(9, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ExpiredArrayViewIsCatchableAndRootStaysUsable()
    {
        const string text = """
DIM grid(1 TO 3, 1 TO 3) AS INTEGER
grid(2, 2) = 7

DIM rowView() AS INTEGER
rowView = grid(2)

REDIM grid(1 TO 5, 1 TO 5)
grid(2, 2) = 9

TRY
    PRINT rowView(2)
CATCH err
    PRINT err.Code
END TRY

PRINT grid(2, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3014", "9"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAndPopKeepLogicalOrderAndBounds()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 0)

FOR index = 1 TO 3
    ARRAYPUSH(values, index * 10)
NEXT

PRINT ARRAYLENGTH(values)
PRINT values(1); values(2); values(3)
PRINT ARRAYPOP(values)
PRINT ARRAYPOP(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values); UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "102030", "30", "20", "1", "11"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReusesPoppedCapacity()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 2)
values(1) = 5
values(2) = 6

PRINT ARRAYPOP(values)
PRINT ARRAYPOP(values)
PRINT ARRAYLENGTH(values)

ARRAYPUSH(values, 7)
ARRAYPUSH(values, 8)

PRINT ARRAYLENGTH(values)
PRINT values(1); values(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "5", "0", "2", "78"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushPreservesNonCanonicalLowerBound()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(5 TO 5)
values(5) = 11

ARRAYPUSH(values, 12)

PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(5); values(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "1112"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctKeepsFirstOccurrenceOrder()
    {
        const string text = """
DIM values(1 TO 8) AS INTEGER
values(1) = 3
values(2) = 1
values(3) = 3
values(4) = 2
values(5) = 1
values(6) = 4
values(7) = 2
values(8) = 5

DIM distinctValues() AS INTEGER
distinctValues = ARRAYDISTINCT(values)

PRINT ARRAYLENGTH(distinctValues)
FOR index = 1 TO ARRAYLENGTH(distinctValues)
    PRINT distinctValues(index);
NEXT
PRINT ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "31245"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctStringComparisonIsOrdinal()
    {
        const string text = """
DIM values(1 TO 5) AS STRING
values(1) = "beta"
values(2) = "alpha"
values(3) = "beta"
values(4) = "Alpha"
values(5) = "alpha"

DIM distinctValues() AS STRING
distinctValues = ARRAYDISTINCT(values)

PRINT ARRAYLENGTH(distinctValues)
FOR index = 1 TO ARRAYLENGTH(distinctValues)
    PRINT distinctValues(index);
NEXT
PRINT ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "betaalphaAlpha"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushExpiresExistingViewsOfRoot()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(1) = 1

DIM part() AS INTEGER
part = values(1 TO 2)

ARRAYPUSH(values, 4)

TRY
    PRINT part(1)
CATCH err
    PRINT err.Code
END TRY

PRINT values(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3014", "4"], host.Output);
    }

    [Fact]
    public void Execute_FractionalIndexReportsBuiltinArgument()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
PRINT values(2.9)
""";

        var (_, host, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_FractionalIndexOnStoreReportsBuiltinArgument()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(2.9) = 5
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_IntegralDivisionResultIsAcceptedAsIndex()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(2) = 42
DIM position AS INTEGER = 4
PRINT values(position / 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42"], host.Output);
    }

    [Fact]
    public void Execute_FractionalBoundReportsBuiltinArgument()
    {
        const string text = """
DIM values(1.5 TO 3) AS INTEGER
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("целое число", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_DescendingRangeReportsArrayBounds()
    {
        const string text = """
DIM values(1 TO 5) AS INTEGER
DIM part() AS INTEGER
part = values(4 TO 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
        Assert.Contains("нижнюю границу больше верхней", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ZeroCountSliceUsesCanonicalEmptyBounds()
    {
        const string text = """
DIM grid(1 TO 2, 1 TO 3) AS INTEGER
DIM part(,) AS INTEGER
part = ARRAYSLICE(grid, 99, 0, 2)

PRINT ARRAYLENGTH(part)
PRINT LBOUND(part, 1); UBOUND(part, 1)
PRINT LBOUND(part, 2); UBOUND(part, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "12", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatRankMismatchReportsRank()
    {
        const string text = """
DIM left(1 TO 2) AS INTEGER
DIM right(1 TO 2, 1 TO 2) AS INTEGER
DIM merged() AS INTEGER
merged = ARRAYCONCAT(left, right)
""";

        var diagnostics = GetBindingDiagnostics(text);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("Ранг правого массива", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ArrayInitializerReportsSemanticDiagnostic()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER = 5
""";

        var diagnostics = GetBindingDiagnostics(text);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("нельзя инициализировать", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => d.Code == "BC3001");
    }

    [Fact]
    public void Execute_ArrayMinRejectsDateElementsAtBinding()
    {
        const string text = """
DIM values(1 TO 2) AS DATE
PRINT ARRAYMIN(values)
""";

        var diagnostics = GetBindingDiagnostics(text);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ARRAYMIN", diagnostic.Message, StringComparison.Ordinal);
    }
}
