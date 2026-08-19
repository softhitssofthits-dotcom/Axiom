using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()
    {
        const string text = """
DIM items() AS STRING
PRINT items(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("items", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_WholeArrayAssignmentSharesAllocatedArray()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM target() AS INTEGER
source(1) = 10
source(2) = 20
target = source
PRINT target(1)
PRINT target(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCanBePassedToSubAndReturnedFromFunction()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20

Bump(values)

DIM copy() AS INTEGER
copy = Echo(values)

PRINT values(1)
PRINT copy(2)

SUB Bump(items AS INTEGER())
    items(1) = items(1) + 1
END SUB

FUNCTION Echo(items AS INTEGER()) AS INTEGER()
    RETURN items
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11", "20"], host.Output);
    }

    [Fact]
    public void Execute_RedimAllocatesDynamicArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 3)
values(1) = 10
values(2) = 20
values(3) = 30
PRINT values(1)
PRINT values(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "30"], host.Output);
    }

    [Fact]
    public void Execute_RedimResetsSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
alias = source
REDIM alias(5 TO 6)
PRINT source(5)
PRINT source(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveKeepsOverlappingValues()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(1) = 10
values(2) = 20
values(3) = 30
REDIM PRESERVE values(2 TO 4)
PRINT values(2)
PRINT values(3)
PRINT values(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "30", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
source(2) = 20
alias = source
REDIM PRESERVE alias(2 TO 3)
PRINT source(2)
PRINT source(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayProgram()
    {
        const string text = """
DIM grid(,) AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(1, 10) = 7
grid(2, 11) = 9
PRINT ARRAYISALLOCATED(grid)
PRINT LBOUND(grid, 1)
PRINT UBOUND(grid, 1)
PRINT LBOUND(grid, 2)
PRINT UBOUND(grid, 2)
PRINT grid(1, 10)
PRINT grid(2, 11)
REDIM PRESERVE grid(2 TO 3, 11 TO 12)
PRINT grid(2, 11)
PRINT grid(3, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "10", "11", "7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()
    {
        const string text = """
DIM cube(,,) AS INTEGER
DIM bounds(,) AS INTEGER
REDIM cube(2 TO 3, 10 TO 11, -1 TO 0)
PRINT ARRAYRANK(cube)
PRINT ARRAYRANK(bounds)
bounds = ARRAYBOUNDS(cube)
PRINT LBOUND(bounds, ROW)
PRINT UBOUND(bounds, ROW)
PRINT LBOUND(bounds, COLUMN)
PRINT UBOUND(bounds, COLUMN)
PRINT bounds(1, 1)
PRINT bounds(1, 2)
PRINT bounds(2, 1)
PRINT bounds(2, 2)
PRINT bounds(3, 1)
PRINT bounds(3, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "2", "1", "3", "1", "2", "2", "3", "10", "11", "-1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBoundsReportsUnallocatedArrayState()
    {
        const string text = """
DIM grid(,) AS INTEGER
PRINT ARRAYBOUNDS(grid)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("выдел", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PartialMultidimensionalIndexingReturnsSharedRowView()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
DIM firstOnly() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(2, 10) = 7
grid(2, 11) = 9
rowView = grid(2)
rowView(10) = 70
firstOnly = ARRAYTAKE(rowView, 1)
PRINT grid(2, 10)
PRINT rowView(11)
PRINT LBOUND(rowView)
PRINT UBOUND(rowView)
PRINT ARRAYLENGTH(rowView)
PRINT firstOnly(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["70", "9", "10", "11", "2", "70"], host.Output);
    }

    [Fact]
    public void Execute_RangeMultidimensionalIndexingReturnsSharedViews()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
DIM block(,) AS INTEGER
REDIM grid(1 TO 3, 10 TO 14)
grid(2, 11) = 21
grid(2, 12) = 22
grid(3, 12) = 32
rowView = grid(2, 11 TO 12)
block = grid(2 TO 3, 11 TO 12)
rowView(11) = 210
block(3, 12) = 320
PRINT grid(2, 11)
PRINT rowView(12)
PRINT grid(3, 12)
PRINT LBOUND(rowView)
PRINT UBOUND(rowView)
PRINT LBOUND(block, 1)
PRINT UBOUND(block, 1)
PRINT LBOUND(block, 2)
PRINT UBOUND(block, 2)
PRINT ARRAYLENGTH(block)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["210", "22", "320", "11", "12", "2", "3", "11", "12", "4"], host.Output);
    }

    [Fact]
    public void Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
rowView = grid(2)
REDIM rowView(1 TO 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REDIM", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
rowView = grid(2)
ERASE rowView
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ERASE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_DimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimRejectsNonCanonicalEmptyRange()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(5 TO 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 TO 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values(-2147483648 TO -2147483648) AS INTEGER
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(-2147483648 TO -2147483648)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimPreserveCanShrinkToCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20
REDIM PRESERVE values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_EraseMakesArrayUnallocatedAgain()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 2)
values(1) = 10
ERASE values
PRINT values(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()
    {
        const string text = """
DIM source(5 TO 6) AS INTEGER
DIM copy() AS INTEGER
source(5) = 10
source(6) = 20
copy = ARRAYCLONE(source)
copy(5) = 99
PRINT LBOUND(copy)
PRINT UBOUND(copy)
PRINT ARRAYLENGTH(copy)
PRINT source(5)
PRINT copy(5)
PRINT source(6)
PRINT copy(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "2", "10", "99", "20", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayClonePreservesUnallocatedAndCanonicalEmptyStates()
    {
        const string text = """
DIM pending() AS STRING
DIM pendingCopy() AS STRING
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
REDIM pending(1 TO 0)
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
PRINT ARRAYLENGTH(pendingCopy)
PRINT LBOUND(pendingCopy)
PRINT UBOUND(pendingCopy)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesMultidimensionalVisibleShapeIntoIndependentArray()
    {
        const string text = """
DIM source(1 TO 3, 10 TO 12) AS INTEGER
DIM copy(,) AS INTEGER
source(1, 10) = 10
source(2, 11) = 20
source(3, 12) = 30
copy = ARRAYCLONE(source(2 TO 3, 11 TO 12))
copy(2, 11) = 99
PRINT LBOUND(copy, 1)
PRINT UBOUND(copy, 1)
PRINT LBOUND(copy, 2)
PRINT UBOUND(copy, 2)
PRINT source(2, 11)
PRINT copy(2, 11)
PRINT copy(3, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "3", "11", "12", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReturnsCompactIndependentSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSLICE(source, 6, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT source(7)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceAllowsZeroCountAsCanonicalEmptyArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 999, 0)
PRINT ARRAYISALLOCATED(part)
PRINT ARRAYLENGTH(part)
PRINT LBOUND(part)
PRINT UBOUND(part)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySliceReportsOutOfRangeSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 7, 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReturnsCompactIndependentArray()
    {
        const string text = """
DIM left(5 TO 6) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
left(5) = 10
left(6) = 20
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
merged(1) = 99
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT left(5)
PRINT merged(1)
PRINT merged(2)
PRINT merged(3)
PRINT merged(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "10", "99", "20", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatAllowsCanonicalEmptyOperands()
    {
        const string text = """
DIM left(1 TO 0) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
PRINT ARRAYISALLOCATED(merged)
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT merged(10)
PRINT merged(11)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "10", "11", "2", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatReportsUnallocatedOperand()
    {
        const string text = """
DIM left() AS INTEGER
DIM right(1 TO 1) AS INTEGER
DIM merged() AS INTEGER
merged = ARRAYCONCAT(left, right)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedReturnsCompactIndependentArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM reversed() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
reversed = ARRAYTOREVERSED(source)
reversed(1) = 99
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT source(7)
PRINT reversed(1)
PRINT reversed(2)
PRINT reversed(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "30", "99", "20", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
PRINT ARRAYISALLOCATED(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOREVERSED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedSupportsMultidimensionalDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM byColumn(,) AS INTEGER
DIM byRow(,) AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
byColumn = ARRAYTOREVERSED(board, COLUMN)
byRow = ARRAYTOREVERSED(board, ROW)
byColumn(1, 10) = 999
PRINT LBOUND(byColumn, ROW)
PRINT UBOUND(byColumn, ROW)
PRINT LBOUND(byColumn, COLUMN)
PRINT UBOUND(byColumn, COLUMN)
PRINT byColumn(1, 10)
PRINT byColumn(2, 12)
PRINT byRow(1, 10)
PRINT byRow(2, 12)
PRINT board(1, 12)
PRINT board(2, 10)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "999", "210", "210", "112", "112", "210"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReturnsCompactIndependentPrefix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYTAKE(source, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(5)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "10", "99", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM emptyPart() AS INTEGER
DIM wholePart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
emptyPart = ARRAYTAKE(source, 0)
wholePart = ARRAYTAKE(source, 99)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
PRINT LBOUND(wholePart)
PRINT UBOUND(wholePart)
PRINT wholePart(1)
PRINT wholePart(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "3", "10", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYTAKE(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYTAKE(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReturnsCompactIndependentSuffix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSKIP(source, 1)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM samePart() AS INTEGER
DIM emptyPart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
samePart = ARRAYSKIP(source, 0)
emptyPart = ARRAYSKIP(source, 99)
PRINT LBOUND(samePart)
PRINT UBOUND(samePart)
PRINT samePart(1)
PRINT samePart(3)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "10", "30", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSKIP(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYSKIP(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()
    {
        const string text = """
DIM board(1 TO 3, 10 TO 13) AS INTEGER
DIM slice(,) AS INTEGER
DIM taken(,) AS INTEGER
DIM skipped(,) AS INTEGER
DIM leftPart(,) AS INTEGER
DIM rightPart(,) AS INTEGER
DIM merged(,) AS INTEGER

board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(1, 13) = 113
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
board(2, 13) = 213
board(3, 10) = 310
board(3, 11) = 311
board(3, 12) = 312
board(3, 13) = 313

slice = ARRAYSLICE(board, 11, 2, COLUMN)
taken = ARRAYTAKE(board, 2, ROW)
skipped = ARRAYSKIP(board, 1, ROW)
leftPart = ARRAYTAKE(board, 2, COLUMN)
rightPart = ARRAYSKIP(board, 2, COLUMN)
merged = ARRAYCONCAT(leftPart, rightPart, COLUMN)

PRINT LBOUND(slice, ROW)
PRINT UBOUND(slice, ROW)
PRINT LBOUND(slice, COLUMN)
PRINT UBOUND(slice, COLUMN)
PRINT slice(2, 11)
PRINT slice(3, 12)
PRINT LBOUND(taken, ROW)
PRINT UBOUND(taken, ROW)
PRINT taken(2, 13)
PRINT LBOUND(skipped, ROW)
PRINT UBOUND(skipped, ROW)
PRINT skipped(2, 10)
PRINT LBOUND(merged, COLUMN)
PRINT UBOUND(merged, COLUMN)
PRINT merged(3, 13)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "11", "12", "211", "312", "1", "2", "213", "2", "3", "210", "10", "13", "313"], host.Output);
    }

    [Fact]
    public void Execute_ArrayShapeBuiltinsRequireExplicitDimensionForMultidimensionalArrays()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 2) AS INTEGER
DIM part(,) AS INTEGER
part = ARRAYTAKE(board, 1)
""";

        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        _ = Binder.Bind(syntax, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("dimension", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReportsIncompatibleMultidimensionalShapes()
    {
        const string text = """
DIM leftPart(1 TO 2, 10 TO 11) AS INTEGER
DIM rightPart(2 TO 3, 12 TO 13) AS INTEGER
DIM merged(,) AS INTEGER
merged = ARRAYCONCAT(leftPart, rightPart, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("right", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeAndSkipCanProduceAllocatedEmptyMultidimensionalResult()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM emptyTake(,) AS INTEGER
DIM emptySkip(,) AS INTEGER
emptyTake = ARRAYTAKE(board, 0, COLUMN)
emptySkip = ARRAYSKIP(board, 99, COLUMN)
PRINT ARRAYISALLOCATED(emptyTake)
PRINT ARRAYLENGTH(emptyTake)
PRINT LBOUND(emptyTake, ROW)
PRINT UBOUND(emptyTake, ROW)
PRINT LBOUND(emptyTake, COLUMN)
PRINT UBOUND(emptyTake, COLUMN)
PRINT ARRAYISALLOCATED(emptySkip)
PRINT ARRAYLENGTH(emptySkip)
PRINT LBOUND(emptySkip, COLUMN)
PRINT UBOUND(emptySkip, COLUMN)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "2", "10", "9", "TRUE", "0", "13", "12"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReturnsCompactUniqueArray()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM uniqueValues() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 10
source(8) = 30
source(9) = 20
uniqueValues = ARRAYDISTINCT(source)
uniqueValues(1) = 99
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT source(5)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT uniqueValues(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "10", "99", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
PRINT ARRAYISALLOCATED(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYDISTINCT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayDistinctOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = """
DIM source(1 TO 2, 10 TO 12) AS INTEGER
DIM uniqueValues() AS INTEGER
source(1, 10) = 5
source(1, 11) = 7
source(1, 12) = 5
source(2, 10) = 9
source(2, 11) = 7
source(2, 12) = 11
uniqueValues = ARRAYDISTINCT(source)
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT uniqueValues(3)
PRINT uniqueValues(4)
PRINT ARRAYISALLOCATED(uniqueValues)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "5", "7", "9", "11", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFlattenOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM flat() AS INTEGER
board(1, 10) = 3
board(1, 11) = 1
board(1, 12) = 2
board(2, 10) = 6
board(2, 11) = 4
board(2, 12) = 5
flat = ARRAYFLATTEN(board)
PRINT ARRAYISALLOCATED(flat)
PRINT LBOUND(flat)
PRINT UBOUND(flat)
PRINT flat(1)
PRINT flat(2)
PRINT flat(3)
PRINT flat(4)
PRINT flat(5)
PRINT flat(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "6", "3", "1", "2", "6", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReshapeRebuildsTemplateShapeFromSortedFlat()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM flat() AS INTEGER
DIM sorted() AS INTEGER
DIM reshaped(,) AS INTEGER
board(1, 10) = 3
board(1, 11) = 1
board(1, 12) = 2
board(2, 10) = 6
board(2, 11) = 4
board(2, 12) = 5
flat = ARRAYFLATTEN(board)
sorted = ARRAYTOSORTED(flat)
reshaped = ARRAYRESHAPE(sorted, board)
PRINT LBOUND(reshaped, ROW)
PRINT UBOUND(reshaped, ROW)
PRINT LBOUND(reshaped, COLUMN)
PRINT UBOUND(reshaped, COLUMN)
PRINT reshaped(1, 10)
PRINT reshaped(1, 11)
PRINT reshaped(1, 12)
PRINT reshaped(2, 10)
PRINT reshaped(2, 11)
PRINT reshaped(2, 12)
PRINT board(1, 10)
PRINT board(2, 12)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "1", "2", "3", "4", "5", "6", "3", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReshapeReportsMismatchedVisibleElementCount()
    {
        const string text = """
DIM source(1 TO 3) AS INTEGER
DIM template(1 TO 2, 1 TO 2) AS INTEGER
DIM reshaped(,) AS INTEGER
reshaped = ARRAYRESHAPE(source, template)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYRESHAPE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("template", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToSortedPreservesBoundsAndDoesNotMutateSource()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM sortedValues() AS INTEGER
source(5) = 30
source(6) = 10
source(7) = 20
source(8) = 20
source(9) = 10
sortedValues = ARRAYTOSORTED(source)
sortedValues(5) = 99
PRINT LBOUND(sortedValues)
PRINT UBOUND(sortedValues)
PRINT source(5)
PRINT source(6)
PRINT source(7)
PRINT source(8)
PRINT source(9)
PRINT sortedValues(5)
PRINT sortedValues(6)
PRINT sortedValues(7)
PRINT sortedValues(8)
PRINT sortedValues(9)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "9", "30", "10", "20", "20", "10", "99", "10", "20", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedSupportsStringsAndCanonicalEmptyArray()
    {
        const string text = """
DIM names(2 TO 5) AS STRING
DIM sortedNames() AS STRING
DIM emptyNames(1 TO 0) AS STRING
DIM emptySorted() AS STRING
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
sortedNames = ARRAYTOSORTED(names)
emptySorted = ARRAYTOSORTED(emptyNames)
PRINT LBOUND(sortedNames)
PRINT UBOUND(sortedNames)
PRINT sortedNames(2)
PRINT sortedNames(3)
PRINT sortedNames(4)
PRINT sortedNames(5)
PRINT ARRAYISALLOCATED(emptySorted)
PRINT LBOUND(emptySorted)
PRINT UBOUND(emptySorted)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "5", "Ann", "Bob", "Bob", "Cara", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM sortedValues() AS INTEGER
sortedValues = ARRAYTOSORTED(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOSORTED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFillMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 1
values(6) = 2
values(7) = 3
ARRAYFILL(values, 42)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "42", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillMutatesMultidimensionalVisibleElements()
    {
        const string text = """
DIM values(1 TO 3, 1 TO 3) AS INTEGER
values(1, 1) = 1
values(2, 2) = 2
values(3, 3) = 3
ARRAYFILL(values(2 TO 3, 2 TO 3), 8)
PRINT values(1, 1)
PRINT values(2, 2)
PRINT values(2, 3)
PRINT values(3, 2)
PRINT values(3, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "8", "8", "8", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
ARRAYREVERSE(values)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["40", "30", "20", "10", "5", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseSupportsMultidimensionalDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(1, 12) = 112
board(2, 10) = 210
board(2, 11) = 211
board(2, 12) = 212
ARRAYREVERSE(board, COLUMN)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(1, 12)
PRINT board(2, 10)
PRINT board(2, 11)
PRINT board(2, 12)
ARRAYREVERSE(board, ROW)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(1, 12)
PRINT board(2, 10)
PRINT board(2, 11)
PRINT board(2, 12)
PRINT LBOUND(board, ROW)
PRINT UBOUND(board, ROW)
PRINT LBOUND(board, COLUMN)
PRINT UBOUND(board, COLUMN)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "111", "110", "212", "211", "210", "212", "211", "210", "112", "111", "110", "1", "2", "10", "12"], host.Output);
    }

    [Fact]
    public void Execute_ArraySortMutatesArrayInPlaceAcrossTypes()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 30
numbers(6) = 10
numbers(7) = 20
numbers(8) = 20
numbers(9) = 10
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
ARRAYSORT(numbers)
ARRAYSORT(names)
PRINT numbers(5)
PRINT numbers(6)
PRINT numbers(7)
PRINT numbers(8)
PRINT numbers(9)
PRINT LBOUND(numbers)
PRINT UBOUND(numbers)
PRINT names(2)
PRINT names(3)
PRINT names(4)
PRINT names(5)
PRINT LBOUND(names)
PRINT UBOUND(names)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10", "20", "20", "30", "5", "9", "Ann", "Bob", "Bob", "Cara", "2", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMutatingProceduresAllowCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYFILL(values, 1)
ARRAYREVERSE(values)
ARRAYSORT(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYFILL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReverseReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYREVERSE(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREVERSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySortReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYSORT(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSORT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushAppendsValueAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
values(5) = 10
values(6) = 20
ARRAYPUSH(values, 30)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYPUSH(values, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopReturnsLastElementAndShrinksArray()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYPOP(values)
PRINT values(5)
PRINT values(6)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["30", "10", "20", "5", "6", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYPOP(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYPUSH(values, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertShiftsTailAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 30
values(7) = 40
ARRAYINSERT(values, 6, 20)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "40", "5", "8", "4"], host.Output);
    }

    [Fact]
    public void Execute_ArrayInsertAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 1, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveReturnsRemovedElementAndShiftsTail()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
PRINT ARRAYREMOVE(values, 6)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "10", "30", "40", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYREMOVE(values, 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYINSERT(values, 1, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM newColumn(1 TO 2) AS INTEGER
DIM removedColumn() AS INTEGER
board(1, 10) = 110
board(1, 11) = 111
board(2, 10) = 210
board(2, 11) = 211
newColumn(1) = 112
newColumn(2) = 212
ARRAYPUSH(board, newColumn, COLUMN)
PRINT board(1, 12)
PRINT board(2, 12)
removedColumn = ARRAYPOP(board, COLUMN)
PRINT LBOUND(removedColumn)
PRINT UBOUND(removedColumn)
PRINT removedColumn(1)
PRINT removedColumn(2)
ARRAYINSERT(board, 11, newColumn, COLUMN)
removedColumn = ARRAYREMOVE(board, 10, COLUMN)
PRINT removedColumn(1)
PRINT removedColumn(2)
PRINT LBOUND(board, COLUMN)
PRINT UBOUND(board, COLUMN)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(2, 10)
PRINT board(2, 11)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "212", "1", "2", "112", "212", "110", "210", "10", "11", "112", "111", "212", "211"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayInsertReportsIncompatibleValueShape()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM wrongRow(10 TO 11) AS INTEGER
ARRAYINSERT(board, 10, wrongRow, COLUMN)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, GreaterThanThreshold)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()
    {
        const string text = """
DIM values(2 TO 3, 4 TO 5) AS INTEGER
DIM mapped(,) AS STRING
values(2, 4) = 10
values(2, 5) = 20
values(3, 4) = 30
values(3, 5) = 40

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values(2 TO 3, 5 TO 5), IsLarge)
mapped = ARRAYMAP(values(2 TO 3, 5 TO 5), FormatValue)
PRINT LBOUND(mapped, 1)
PRINT UBOUND(mapped, 1)
PRINT LBOUND(mapped, 2)
PRINT UBOUND(mapped, 2)
PRINT mapped(2, 5)
PRINT mapped(3, 5)
PRINT ARRAYREDUCE(values(2 TO 3, 4 TO 5), "", AppendValue)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value >= 40
END FUNCTION

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN "N" + STR(value)
END FUNCTION

FUNCTION AppendValue(accumulator AS STRING, value AS INTEGER) AS STRING
    RETURN accumulator + "[" + STR(value) + "]"
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "2", "3", "5", "5", "N20", "N40", "[10][20][30][40]"], host.Output);
    }

    [Fact]
    public void Execute_ArrayEveryTreatsCanonicalEmptyArrayAsTrueAndArraySomeAsFalse()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReturnsCompactArrayAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
DIM filtered() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
filtered = ARRAYFILTER(values, GreaterThanThreshold)
filtered(1) = 99
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)
PRINT values(7)
PRINT filtered(1)
PRINT filtered(2)

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "30", "99", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterAllowsCanonicalEmptyInputAndResult()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

REDIM values(5 TO 6)
values(5) = 1
values(6) = 2
filtered = ARRAYFILTER(values, IsLarge)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > 100
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0", "TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()
    {
        const string text = """
DIM values(2 TO 3, 4 TO 6) AS INTEGER
DIM filtered() AS INTEGER
values(2, 4) = 10
values(2, 5) = 21
values(2, 6) = 30
values(3, 4) = 41
values(3, 5) = 50
values(3, 6) = 61
filtered = ARRAYFILTER(values(2 TO 3, 4 TO 6), IsOdd)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)
PRINT filtered(1)
PRINT filtered(2)
PRINT filtered(3)

FUNCTION IsOdd(value AS INTEGER) AS BOOLEAN
    RETURN value MOD 2 = 1
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["1", "3", "3", "21", "41", "61"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFilterReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
DIM filtered() AS INTEGER
values(1) = 10
values(2) = 20
filtered = ARRAYFILTER(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapPreservesTransformedArrayShapeAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM factor AS INTEGER = 3
DIM mapped() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
mapped = ARRAYMAP(values, Multiply)
mapped(5) = 99
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)
PRINT values(5)
PRINT mapped(5)
PRINT mapped(6)
PRINT mapped(7)
PRINT mapped(8)

FUNCTION Multiply(value AS INTEGER) AS INTEGER
    RETURN value * factor
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "8", "4", "10", "99", "60", "90", "120"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapSupportsPolymorphicResultType()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM prefix AS STRING = "item:"
DIM mapped() AS STRING
values(5) = 10
values(6) = 20
values(7) = 30
mapped = ARRAYMAP(values, FormatValue)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT mapped(5)
PRINT mapped(6)
PRINT mapped(7)

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "7", "item:10", "item:20", "item:30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapAllowsCanonicalEmptyInput()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)
PRINT ARRAYISALLOCATED(mapped)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapPreservesMultidimensionalEmptyVisibleShape()
    {
        const string text = """
DIM values(1 TO 2, 1 TO 2) AS INTEGER
DIM mapped(,) AS INTEGER
mapped = ARRAYMAP(values(2 TO 1, 1 TO 2), DoubleIt)
PRINT ARRAYISALLOCATED(mapped)
PRINT LBOUND(mapped, 1)
PRINT UBOUND(mapped, 1)
PRINT LBOUND(mapped, 2)
PRINT UBOUND(mapped, 2)
PRINT ARRAYLENGTH(mapped)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "1", "1", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYMAP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM selector AS FUNCTION(INTEGER) AS STRING
DIM mapped() AS STRING
values(1) = 10
values(2) = 20
mapped = ARRAYMAP(values, selector)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceUsesSeedAndClosure()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM bonus AS INTEGER = 1
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYREDUCE(values, 5, AddWithBonus)
PRINT values(5)

FUNCTION AddWithBonus(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value + bonus
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["68", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceSupportsPolymorphicAccumulatorType()
    {
        const string text = """
DIM words(3 TO 5) AS STRING
words(3) = "alpha"
words(4) = "bb"
words(5) = "ccc"
PRINT ARRAYREDUCE(words, 0, AddLength)

FUNCTION AddLength(acc AS INTEGER, value AS STRING) AS INTEGER
    RETURN acc + LEN(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReturnsSeedForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREDUCE(values, 7, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREDUCE(values, 0, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREDUCE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS STRING
DIM reducer AS FUNCTION(INTEGER, STRING) AS INTEGER
values(1) = "aa"
values(2) = "bbb"
PRINT ARRAYREDUCE(values, 0, reducer)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSOME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
values(1) = 10
values(2) = 20
PRINT ARRAYEVERY(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYINSERT(values, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
ARRAYINSERT(values, 4, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("диапазон", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
PRINT ARRAYREMOVE(values, 7)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("существующий логический индекс", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndexForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 2, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("равен 1", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYINSERT(values, 2147483647, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsUseLogicalIndexesAcrossLowerBounds()
    {
        const string text = """
DIM oneBased(1 TO 3) AS INTEGER
DIM zeroBased(0 TO 2) AS INTEGER
DIM shifted(5 TO 7) AS INTEGER
DIM negative(-3 TO -1) AS INTEGER
oneBased(1) = 10
oneBased(2) = 20
oneBased(3) = 20
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
shifted(5) = 10
shifted(6) = 20
shifted(7) = 30
negative(-3) = 10
negative(-2) = 20
negative(-1) = 20
PRINT ARRAYCONTAINS(oneBased, 20)
PRINT ARRAYCOUNT(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 20)
PRINT ARRAYLASTINDEXOF(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 99)
PRINT ARRAYINDEXOF(zeroBased, 20)
PRINT ARRAYINDEXOF(zeroBased, 99)
PRINT ARRAYINDEXOF(shifted, 20)
PRINT ARRAYINDEXOF(shifted, 99)
PRINT ARRAYLASTINDEXOF(negative, 20)
PRINT ARRAYINDEXOF(negative, 99)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "2", "3", "0", "1", "-1", "6", "4", "-1", "-4"], host.Output);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
PRINT ARRAYCOUNT(values, 1)
PRINT ARRAYINDEXOF(values, 1)
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsAndCountScanMultidimensionalVisibleElements()
    {
        const string text = """
DIM values(1 TO 3, 1 TO 3) AS INTEGER
values(1, 1) = 5
values(1, 2) = 7
values(2, 2) = 7
values(3, 3) = 7
PRINT ARRAYCONTAINS(values, 7)
PRINT ARRAYCOUNT(values, 7)
PRINT ARRAYCONTAINS(values(2 TO 3, 2 TO 3), 5)
PRINT ARRAYCOUNT(values(2 TO 3, 2 TO 3), 7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "3", "FALSE", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFindAllReturnsCompactArrayOfLogicalIndexes()
    {
        const string text = """
DIM shifted(5 TO 8) AS INTEGER
DIM negative(-3 TO 0) AS INTEGER
DIM found() AS INTEGER
DIM negativeHits() AS INTEGER
DIM missing() AS INTEGER
shifted(5) = 10
shifted(6) = 20
shifted(7) = 10
shifted(8) = 30
negative(-3) = 20
negative(-2) = 10
negative(-1) = 20
negative(0) = 20
found = ARRAYFINDALL(shifted, 10)
negativeHits = ARRAYFINDALL(negative, 20)
missing = ARRAYFINDALL(shifted, 99)
PRINT ARRAYISALLOCATED(found)
PRINT LBOUND(found)
PRINT UBOUND(found)
PRINT found(1)
PRINT found(2)
PRINT ARRAYISALLOCATED(negativeHits)
PRINT LBOUND(negativeHits)
PRINT UBOUND(negativeHits)
PRINT negativeHits(1)
PRINT negativeHits(2)
PRINT negativeHits(3)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "5", "7", "TRUE", "1", "3", "-3", "-1", "0", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCoordinateSearchFunctionsReturnCoordinateVectorsAndTables()
    {
        const string text = """
DIM grid(1 TO 2, 5 TO 7) AS INTEGER
DIM cube(0 TO 1, 10 TO 11, -1 TO 0) AS INTEGER
DIM firstHit() AS INTEGER
DIM lastHit() AS INTEGER
DIM allHits(,) AS INTEGER
DIM cubeHit() AS INTEGER
DIM missing() AS INTEGER
DIM noHits(,) AS INTEGER
grid(1, 5) = 10
grid(1, 6) = 20
grid(2, 6) = 20
grid(2, 7) = 20
cube(1, 11, 0) = 7
firstHit = ARRAYINDEXOF(grid, 20)
lastHit = ARRAYLASTINDEXOF(grid, 20)
allHits = ARRAYFINDALL(grid, 20)
cubeHit = ARRAYINDEXOF(cube, 7)
missing = ARRAYINDEXOF(grid, 99)
noHits = ARRAYFINDALL(grid, 99)
PRINT ARRAYISALLOCATED(firstHit)
PRINT LBOUND(firstHit)
PRINT UBOUND(firstHit)
PRINT firstHit(1)
PRINT firstHit(2)
PRINT lastHit(1)
PRINT lastHit(2)
PRINT LBOUND(allHits, 1)
PRINT UBOUND(allHits, 1)
PRINT LBOUND(allHits, 2)
PRINT UBOUND(allHits, 2)
PRINT allHits(1, 1)
PRINT allHits(1, 2)
PRINT allHits(2, 1)
PRINT allHits(2, 2)
PRINT allHits(3, 1)
PRINT allHits(3, 2)
PRINT cubeHit(1)
PRINT cubeHit(2)
PRINT cubeHit(3)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
PRINT ARRAYISALLOCATED(noHits)
PRINT LBOUND(noHits, 1)
PRINT UBOUND(noHits, 1)
PRINT LBOUND(noHits, 2)
PRINT UBOUND(noHits, 2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "6", "2", "7", "1", "3", "1", "2", "1", "6", "2", "6", "2", "7", "1", "11", "0", "TRUE", "1", "0", "TRUE", "1", "0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReturnsFirstLogicalIndexAcrossLowerBounds()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM zeroBased(0 TO 3) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 10
numbers(6) = 20
numbers(7) = 20
numbers(8) = 20
numbers(9) = 30
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
zeroBased(3) = 40
names(2) = "Ann"
names(3) = "Bob"
names(4) = "Bob"
names(5) = "Cara"
PRINT ARRAYBINARYSEARCH(numbers, 20)
PRINT ARRAYBINARYSEARCH(numbers, 15)
PRINT ARRAYBINARYSEARCH(zeroBased, 10)
PRINT ARRAYBINARYSEARCH(zeroBased, 35)
PRINT ARRAYBINARYSEARCH(names, "Bob")
PRINT ARRAYBINARYSEARCH(names, "Aaron")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "4", "0", "-1", "3", "1"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchTreatsCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalSortingGroupSortsAlongExplicitDimension()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER
DIM sortedByColumn(1 TO 2, 1 TO 3) AS INTEGER
board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5
sortedByColumn = ARRAYTOSORTED(board, COLUMN)
PRINT board(1, 1)
PRINT sortedByColumn(1, 1)
PRINT sortedByColumn(1, 2)
PRINT sortedByColumn(1, 3)
PRINT sortedByColumn(2, 1)
PRINT sortedByColumn(2, 2)
PRINT sortedByColumn(2, 3)
ARRAYSORT(board, ROW)
PRINT board(1, 1)
PRINT board(1, 2)
PRINT board(1, 3)
PRINT board(2, 1)
PRINT board(2, 2)
PRINT board(2, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4", "2", "4", "6", "1", "3", "5", "3", "1", "5", "4", "2", "6"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayBinarySearchReturnsCoordinateVectorOrEmptyArray()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 4) AS INTEGER
DIM firstHit() AS INTEGER
DIM missing() AS INTEGER
board(1, 1) = 1
board(1, 2) = 4
board(1, 3) = 4
board(1, 4) = 7
board(2, 1) = 2
board(2, 2) = 4
board(2, 3) = 5
board(2, 4) = 8
firstHit = ARRAYBINARYSEARCH(board, 4, ROW)
missing = ARRAYBINARYSEARCH(board, 6, ROW)
PRINT ARRAYISALLOCATED(firstHit)
PRINT LBOUND(firstHit)
PRINT UBOUND(firstHit)
PRINT firstHit(1)
PRINT firstHit(2)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "2", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONTAINS", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayLastIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYLASTINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCountReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCOUNT(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFindAllReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM matches() AS INTEGER
matches = ARRAYFINDALL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFINDALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYBINARYSEARCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
alias = source
ERASE alias
PRINT source(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinArrayInformationFunctionsReportCurrentState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
REDIM values(3 TO 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ERASE values
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "TRUE", "3", "5", "3", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_LBoundReportsUnallocatedArrayState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT LBOUND(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("LBOUND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
