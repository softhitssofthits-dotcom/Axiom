using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()
    {
        const string text = "DIM items[] AS STRING\r\nPRINT items[1]";

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
        const string text = "DIM source[1 TO 2] AS INTEGER\r\nDIM target[] AS INTEGER\r\nsource[1] = 10\r\nsource[2] = 20\r\ntarget[] = source[]\r\nPRINT target[1]\r\nPRINT target[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCanBePassedToSubAndReturnedFromFunction()
    {
        const string text = "DIM values[1 TO 2] AS INTEGER\r\nvalues[1] = 10\r\nvalues[2] = 20\r\n\r\nBump(values[])\r\n\r\nDIM copy[] AS INTEGER\r\ncopy[] = Echo(values[])\r\n\r\nPRINT values[1]\r\nPRINT copy[2]\r\n\r\nSUB Bump(items[] AS INTEGER)\r\n    items[1] = items[1] + 1\r\nEND SUB\r\n\r\nFUNCTION Echo(items[] AS INTEGER) AS INTEGER[]\r\n    RETURN items[]\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11", "20"], host.Output);
    }

    [Fact]
    public void Execute_RedimAllocatesDynamicArray()
    {
        const string text = "DIM values[] AS INTEGER\r\nREDIM values[1 TO 3]\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nvalues[3] = 30\r\nPRINT values[1]\r\nPRINT values[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "30"], host.Output);
    }

    [Fact]
    public void Execute_RedimResetsSharedArrayStorage()
    {
        const string text = "DIM source[1 TO 2] AS INTEGER\r\nDIM alias[] AS INTEGER\r\nsource[1] = 10\r\nalias[] = source[]\r\nREDIM alias[5 TO 6]\r\nPRINT source[5]\r\nPRINT source[6]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveKeepsOverlappingValues()
    {
        const string text = "DIM values[1 TO 3] AS INTEGER\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nvalues[3] = 30\r\nREDIM PRESERVE values[2 TO 4]\r\nPRINT values[2]\r\nPRINT values[3]\r\nPRINT values[4]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "30", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveMutatesSharedArrayStorage()
    {
        const string text = "DIM source[1 TO 2] AS INTEGER\r\nDIM alias[] AS INTEGER\r\nsource[1] = 10\r\nsource[2] = 20\r\nalias[] = source[]\r\nREDIM PRESERVE alias[2 TO 3]\r\nPRINT source[2]\r\nPRINT source[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayProgram()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nREDIM grid[1 TO 2, 10 TO 11]\r\ngrid[1, 10] = 7\r\ngrid[2, 11] = 9\r\nPRINT ARRAYISALLOCATED(grid[])\r\nPRINT LBOUND(grid[], 1)\r\nPRINT UBOUND(grid[], 1)\r\nPRINT LBOUND(grid[], 2)\r\nPRINT UBOUND(grid[], 2)\r\nPRINT grid[1, 10]\r\nPRINT grid[2, 11]\r\nREDIM PRESERVE grid[2 TO 3, 11 TO 12]\r\nPRINT grid[2, 11]\r\nPRINT grid[3, 12]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "10", "11", "7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()
    {
        const string text = "DIM cube[,,] AS INTEGER\r\nDIM bounds[,] AS INTEGER\r\nREDIM cube[2 TO 3, 10 TO 11, -1 TO 0]\r\nPRINT ARRAYRANK(cube[])\r\nPRINT ARRAYRANK(bounds[])\r\nbounds[] = ARRAYBOUNDS(cube[])\r\nPRINT LBOUND(bounds[], ROW)\r\nPRINT UBOUND(bounds[], ROW)\r\nPRINT LBOUND(bounds[], COLUMN)\r\nPRINT UBOUND(bounds[], COLUMN)\r\nPRINT bounds[1, 1]\r\nPRINT bounds[1, 2]\r\nPRINT bounds[2, 1]\r\nPRINT bounds[2, 2]\r\nPRINT bounds[3, 1]\r\nPRINT bounds[3, 2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "2", "1", "3", "1", "2", "2", "3", "10", "11", "-1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBoundsReportsUnallocatedArrayState()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nPRINT ARRAYBOUNDS(grid[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("выдел", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PartialMultidimensionalIndexingReturnsSharedRowView()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nDIM rowView[] AS INTEGER\r\nDIM firstOnly[] AS INTEGER\r\nREDIM grid[1 TO 2, 10 TO 11]\r\ngrid[2, 10] = 7\r\ngrid[2, 11] = 9\r\nrowView[] = grid[2]\r\nrowView[10] = 70\r\nfirstOnly[] = ARRAYTAKE(rowView[], 1)\r\nPRINT grid[2, 10]\r\nPRINT rowView[11]\r\nPRINT LBOUND(rowView[])\r\nPRINT UBOUND(rowView[])\r\nPRINT ARRAYLENGTH(rowView[])\r\nPRINT firstOnly[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["70", "9", "10", "11", "2", "70"], host.Output);
    }

    [Fact]
    public void Execute_RangeMultidimensionalIndexingReturnsSharedViews()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nDIM rowView[] AS INTEGER\r\nDIM block[,] AS INTEGER\r\nREDIM grid[1 TO 3, 10 TO 14]\r\ngrid[2, 11] = 21\r\ngrid[2, 12] = 22\r\ngrid[3, 12] = 32\r\nrowView[] = grid[2, 11 TO 12]\r\nblock[] = grid[2 TO 3, 11 TO 12]\r\nrowView[11] = 210\r\nblock[3, 12] = 320\r\nPRINT grid[2, 11]\r\nPRINT rowView[12]\r\nPRINT grid[3, 12]\r\nPRINT LBOUND(rowView[])\r\nPRINT UBOUND(rowView[])\r\nPRINT LBOUND(block[], 1)\r\nPRINT UBOUND(block[], 1)\r\nPRINT LBOUND(block[], 2)\r\nPRINT UBOUND(block[], 2)\r\nPRINT ARRAYLENGTH(block[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["210", "22", "320", "11", "12", "2", "3", "11", "12", "4"], host.Output);
    }

    [Fact]
    public void Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nDIM rowView[] AS INTEGER\r\nREDIM grid[1 TO 2, 10 TO 11]\r\nrowView[] = grid[2]\r\nREDIM rowView[1 TO 2]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REDIM", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()
    {
        const string text = "DIM grid[,] AS INTEGER\r\nDIM rowView[] AS INTEGER\r\nREDIM grid[1 TO 2, 10 TO 11]\r\nrowView[] = grid[2]\r\nERASE rowView[]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ERASE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("представления", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = "DIM values[] AS INTEGER\r\nREDIM values[1 TO 0]\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_DimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimRejectsNonCanonicalEmptyRange()
    {
        const string text = "DIM values[] AS INTEGER\r\nREDIM values[5 TO 3]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 TO 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DimRejectsMinimumIntegerLowerBound()
    {
        const string text = "DIM values[-2147483648 TO -2147483648] AS INTEGER";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimRejectsMinimumIntegerLowerBound()
    {
        const string text = "DIM values[] AS INTEGER\r\nREDIM values[-2147483648 TO -2147483648]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimPreserveCanShrinkToCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 2] AS INTEGER\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nREDIM PRESERVE values[1 TO 0]\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_EraseMakesArrayUnallocatedAgain()
    {
        const string text = "DIM values[] AS INTEGER\r\nREDIM values[1 TO 2]\r\nvalues[1] = 10\r\nERASE values[]\r\nPRINT values[1]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()
    {
        const string text = "DIM source[5 TO 6] AS INTEGER\r\nDIM copy[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\ncopy[] = ARRAYCLONE(source[])\r\ncopy[5] = 99\r\nPRINT LBOUND(copy[])\r\nPRINT UBOUND(copy[])\r\nPRINT ARRAYLENGTH(copy[])\r\nPRINT source[5]\r\nPRINT copy[5]\r\nPRINT source[6]\r\nPRINT copy[6]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "2", "10", "99", "20", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayClonePreservesUnallocatedAndCanonicalEmptyStates()
    {
        const string text = "DIM pending[] AS STRING\r\nDIM pendingCopy[] AS STRING\r\npendingCopy[] = ARRAYCLONE(pending[])\r\nPRINT ARRAYISALLOCATED(pendingCopy[])\r\nREDIM pending[1 TO 0]\r\npendingCopy[] = ARRAYCLONE(pending[])\r\nPRINT ARRAYISALLOCATED(pendingCopy[])\r\nPRINT ARRAYLENGTH(pendingCopy[])\r\nPRINT LBOUND(pendingCopy[])\r\nPRINT UBOUND(pendingCopy[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesMultidimensionalVisibleShapeIntoIndependentArray()
    {
        const string text = "DIM source[1 TO 3, 10 TO 12] AS INTEGER\r\nDIM copy[,] AS INTEGER\r\nsource[1, 10] = 10\r\nsource[2, 11] = 20\r\nsource[3, 12] = 30\r\ncopy[] = ARRAYCLONE(source[2 TO 3, 11 TO 12])\r\ncopy[2, 11] = 99\r\nPRINT LBOUND(copy[], 1)\r\nPRINT UBOUND(copy[], 1)\r\nPRINT LBOUND(copy[], 2)\r\nPRINT UBOUND(copy[], 2)\r\nPRINT source[2, 11]\r\nPRINT copy[2, 11]\r\nPRINT copy[3, 12]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "3", "11", "12", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReturnsCompactIndependentSlice()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM part[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\npart[] = ARRAYSLICE(source[], 6, 2)\r\npart[1] = 99\r\nPRINT LBOUND(part[])\r\nPRINT UBOUND(part[])\r\nPRINT ARRAYLENGTH(part[])\r\nPRINT source[6]\r\nPRINT part[1]\r\nPRINT source[7]\r\nPRINT part[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceAllowsZeroCountAsCanonicalEmptyArray()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM part[] AS INTEGER\r\npart[] = ARRAYSLICE(source[], 999, 0)\r\nPRINT ARRAYISALLOCATED(part[])\r\nPRINT ARRAYLENGTH(part[])\r\nPRINT LBOUND(part[])\r\nPRINT UBOUND(part[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReportsUnallocatedSource()
    {
        const string text = "DIM source[] AS INTEGER\r\nDIM part[] AS INTEGER\r\npart[] = ARRAYSLICE(source[], 1, 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySliceReportsOutOfRangeSlice()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM part[] AS INTEGER\r\npart[] = ARRAYSLICE(source[], 7, 2)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReturnsCompactIndependentArray()
    {
        const string text = "DIM left[5 TO 6] AS INTEGER\r\nDIM right[10 TO 11] AS INTEGER\r\nDIM merged[] AS INTEGER\r\nleft[5] = 10\r\nleft[6] = 20\r\nright[10] = 30\r\nright[11] = 40\r\nmerged[] = ARRAYCONCAT(left[], right[])\r\nmerged[1] = 99\r\nPRINT LBOUND(merged[])\r\nPRINT UBOUND(merged[])\r\nPRINT ARRAYLENGTH(merged[])\r\nPRINT left[5]\r\nPRINT merged[1]\r\nPRINT merged[2]\r\nPRINT merged[3]\r\nPRINT merged[4]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "10", "99", "20", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatAllowsCanonicalEmptyOperands()
    {
        const string text = "DIM left[1 TO 0] AS INTEGER\r\nDIM right[10 TO 11] AS INTEGER\r\nDIM merged[] AS INTEGER\r\nright[10] = 30\r\nright[11] = 40\r\nmerged[] = ARRAYCONCAT(left[], right[])\r\nPRINT ARRAYISALLOCATED(merged[])\r\nPRINT LBOUND(merged[])\r\nPRINT UBOUND(merged[])\r\nPRINT ARRAYLENGTH(merged[])\r\nPRINT merged[10]\r\nPRINT merged[11]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "10", "11", "2", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatReportsUnallocatedOperand()
    {
        const string text = "DIM left[] AS INTEGER\r\nDIM right[1 TO 1] AS INTEGER\r\nDIM merged[] AS INTEGER\r\nmerged[] = ARRAYCONCAT(left[], right[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedReturnsCompactIndependentArray()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM reversed[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\nreversed[] = ARRAYTOREVERSED(source[])\r\nreversed[1] = 99\r\nPRINT LBOUND(reversed[])\r\nPRINT UBOUND(reversed[])\r\nPRINT ARRAYLENGTH(reversed[])\r\nPRINT source[7]\r\nPRINT reversed[1]\r\nPRINT reversed[2]\r\nPRINT reversed[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "30", "99", "20", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedAllowsCanonicalEmptyResult()
    {
        const string text = "DIM source[1 TO 0] AS INTEGER\r\nDIM reversed[] AS INTEGER\r\nreversed[] = ARRAYTOREVERSED(source[])\r\nPRINT ARRAYISALLOCATED(reversed[])\r\nPRINT ARRAYLENGTH(reversed[])\r\nPRINT LBOUND(reversed[])\r\nPRINT UBOUND(reversed[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedReportsUnallocatedSource()
    {
        const string text = "DIM source[] AS INTEGER\r\nDIM reversed[] AS INTEGER\r\nreversed[] = ARRAYTOREVERSED(source[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOREVERSED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedSupportsMultidimensionalDimension()
    {
        const string text = "DIM board[1 TO 2, 10 TO 12] AS INTEGER\r\nDIM byColumn[,] AS INTEGER\r\nDIM byRow[,] AS INTEGER\r\nboard[1, 10] = 110\r\nboard[1, 11] = 111\r\nboard[1, 12] = 112\r\nboard[2, 10] = 210\r\nboard[2, 11] = 211\r\nboard[2, 12] = 212\r\nbyColumn[] = ARRAYTOREVERSED(board[], COLUMN)\r\nbyRow[] = ARRAYTOREVERSED(board[], ROW)\r\nbyColumn[1, 10] = 999\r\nPRINT LBOUND(byColumn[], ROW)\r\nPRINT UBOUND(byColumn[], ROW)\r\nPRINT LBOUND(byColumn[], COLUMN)\r\nPRINT UBOUND(byColumn[], COLUMN)\r\nPRINT byColumn[1, 10]\r\nPRINT byColumn[2, 12]\r\nPRINT byRow[1, 10]\r\nPRINT byRow[2, 12]\r\nPRINT board[1, 12]\r\nPRINT board[2, 10]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "999", "210", "210", "112", "112", "210"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReturnsCompactIndependentPrefix()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM part[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\npart[] = ARRAYTAKE(source[], 2)\r\npart[1] = 99\r\nPRINT LBOUND(part[])\r\nPRINT UBOUND(part[])\r\nPRINT ARRAYLENGTH(part[])\r\nPRINT source[5]\r\nPRINT part[1]\r\nPRINT part[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "10", "99", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeAllowsZeroAndExcessiveCount()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM emptyPart[] AS INTEGER\r\nDIM wholePart[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\nemptyPart[] = ARRAYTAKE(source[], 0)\r\nwholePart[] = ARRAYTAKE(source[], 99)\r\nPRINT ARRAYISALLOCATED(emptyPart[])\r\nPRINT ARRAYLENGTH(emptyPart[])\r\nPRINT LBOUND(emptyPart[])\r\nPRINT UBOUND(emptyPart[])\r\nPRINT LBOUND(wholePart[])\r\nPRINT UBOUND(wholePart[])\r\nPRINT wholePart[1]\r\nPRINT wholePart[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "3", "10", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReportsUnallocatedSource()
    {
        const string text = "DIM source[] AS INTEGER\r\nDIM part[] AS INTEGER\r\npart[] = ARRAYTAKE(source[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeReportsNegativeCount()
    {
        const string text = "DIM source[1 TO 1] AS INTEGER\r\nPRINT ARRAYTAKE(source[], -1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReturnsCompactIndependentSuffix()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM part[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\npart[] = ARRAYSKIP(source[], 1)\r\npart[1] = 99\r\nPRINT LBOUND(part[])\r\nPRINT UBOUND(part[])\r\nPRINT ARRAYLENGTH(part[])\r\nPRINT source[6]\r\nPRINT part[1]\r\nPRINT part[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipAllowsZeroAndExcessiveCount()
    {
        const string text = "DIM source[5 TO 7] AS INTEGER\r\nDIM samePart[] AS INTEGER\r\nDIM emptyPart[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 30\r\nsamePart[] = ARRAYSKIP(source[], 0)\r\nemptyPart[] = ARRAYSKIP(source[], 99)\r\nPRINT LBOUND(samePart[])\r\nPRINT UBOUND(samePart[])\r\nPRINT samePart[1]\r\nPRINT samePart[3]\r\nPRINT ARRAYISALLOCATED(emptyPart[])\r\nPRINT ARRAYLENGTH(emptyPart[])\r\nPRINT LBOUND(emptyPart[])\r\nPRINT UBOUND(emptyPart[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "10", "30", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipReportsUnallocatedSource()
    {
        const string text = "DIM source[] AS INTEGER\r\nDIM part[] AS INTEGER\r\npart[] = ARRAYSKIP(source[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReportsNegativeCount()
    {
        const string text = "DIM source[1 TO 1] AS INTEGER\r\nPRINT ARRAYSKIP(source[], -1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()
    {
        const string text = "DIM board[1 TO 3, 10 TO 13] AS INTEGER\r\nDIM slice[,] AS INTEGER\r\nDIM taken[,] AS INTEGER\r\nDIM skipped[,] AS INTEGER\r\nDIM leftPart[,] AS INTEGER\r\nDIM rightPart[,] AS INTEGER\r\nDIM merged[,] AS INTEGER\r\n\r\nboard[1, 10] = 110\r\nboard[1, 11] = 111\r\nboard[1, 12] = 112\r\nboard[1, 13] = 113\r\nboard[2, 10] = 210\r\nboard[2, 11] = 211\r\nboard[2, 12] = 212\r\nboard[2, 13] = 213\r\nboard[3, 10] = 310\r\nboard[3, 11] = 311\r\nboard[3, 12] = 312\r\nboard[3, 13] = 313\r\n\r\nslice[] = ARRAYSLICE(board[], 11, 2, COLUMN)\r\ntaken[] = ARRAYTAKE(board[], 2, ROW)\r\nskipped[] = ARRAYSKIP(board[], 1, ROW)\r\nleftPart[] = ARRAYTAKE(board[], 2, COLUMN)\r\nrightPart[] = ARRAYSKIP(board[], 2, COLUMN)\r\nmerged[] = ARRAYCONCAT(leftPart[], rightPart[], COLUMN)\r\n\r\nPRINT LBOUND(slice[], ROW)\r\nPRINT UBOUND(slice[], ROW)\r\nPRINT LBOUND(slice[], COLUMN)\r\nPRINT UBOUND(slice[], COLUMN)\r\nPRINT slice[2, 11]\r\nPRINT slice[3, 12]\r\nPRINT LBOUND(taken[], ROW)\r\nPRINT UBOUND(taken[], ROW)\r\nPRINT taken[2, 13]\r\nPRINT LBOUND(skipped[], ROW)\r\nPRINT UBOUND(skipped[], ROW)\r\nPRINT skipped[2, 10]\r\nPRINT LBOUND(merged[], COLUMN)\r\nPRINT UBOUND(merged[], COLUMN)\r\nPRINT merged[3, 13]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "11", "12", "211", "312", "1", "2", "213", "2", "3", "210", "10", "13", "313"], host.Output);
    }

    [Fact]
    public void Execute_ArrayShapeBuiltinsRequireExplicitDimensionForMultidimensionalArrays()
    {
        const string text = """
DIM board[1 TO 2, 1 TO 2] AS INTEGER
DIM part[,] AS INTEGER
part[] = ARRAYTAKE(board[], 1)
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
        const string text = "DIM leftPart[1 TO 2, 10 TO 11] AS INTEGER\r\nDIM rightPart[2 TO 3, 12 TO 13] AS INTEGER\r\nDIM merged[,] AS INTEGER\r\nmerged[] = ARRAYCONCAT(leftPart[], rightPart[], COLUMN)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("right", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeAndSkipCanProduceAllocatedEmptyMultidimensionalResult()
    {
        const string text = "DIM board[1 TO 2, 10 TO 12] AS INTEGER\r\nDIM emptyTake[,] AS INTEGER\r\nDIM emptySkip[,] AS INTEGER\r\nemptyTake[] = ARRAYTAKE(board[], 0, COLUMN)\r\nemptySkip[] = ARRAYSKIP(board[], 99, COLUMN)\r\nPRINT ARRAYISALLOCATED(emptyTake[])\r\nPRINT ARRAYLENGTH(emptyTake[])\r\nPRINT LBOUND(emptyTake[], ROW)\r\nPRINT UBOUND(emptyTake[], ROW)\r\nPRINT LBOUND(emptyTake[], COLUMN)\r\nPRINT UBOUND(emptyTake[], COLUMN)\r\nPRINT ARRAYISALLOCATED(emptySkip[])\r\nPRINT ARRAYLENGTH(emptySkip[])\r\nPRINT LBOUND(emptySkip[], COLUMN)\r\nPRINT UBOUND(emptySkip[], COLUMN)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "2", "1", "0", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReturnsCompactUniqueArray()
    {
        const string text = "DIM source[5 TO 9] AS INTEGER\r\nDIM uniqueValues[] AS INTEGER\r\nsource[5] = 10\r\nsource[6] = 20\r\nsource[7] = 10\r\nsource[8] = 30\r\nsource[9] = 20\r\nuniqueValues[] = ARRAYDISTINCT(source[])\r\nuniqueValues[1] = 99\r\nPRINT LBOUND(uniqueValues[])\r\nPRINT UBOUND(uniqueValues[])\r\nPRINT ARRAYLENGTH(uniqueValues[])\r\nPRINT source[5]\r\nPRINT uniqueValues[1]\r\nPRINT uniqueValues[2]\r\nPRINT uniqueValues[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "10", "99", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctAllowsCanonicalEmptyResult()
    {
        const string text = "DIM source[1 TO 0] AS INTEGER\r\nDIM uniqueValues[] AS INTEGER\r\nuniqueValues[] = ARRAYDISTINCT(source[])\r\nPRINT ARRAYISALLOCATED(uniqueValues[])\r\nPRINT ARRAYLENGTH(uniqueValues[])\r\nPRINT LBOUND(uniqueValues[])\r\nPRINT UBOUND(uniqueValues[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReportsUnallocatedSource()
    {
        const string text = "DIM source[] AS INTEGER\r\nDIM uniqueValues[] AS INTEGER\r\nuniqueValues[] = ARRAYDISTINCT(source[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYDISTINCT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayDistinctOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = "DIM source[1 TO 2, 10 TO 12] AS INTEGER\r\nDIM uniqueValues[] AS INTEGER\r\nsource[1, 10] = 5\r\nsource[1, 11] = 7\r\nsource[1, 12] = 5\r\nsource[2, 10] = 9\r\nsource[2, 11] = 7\r\nsource[2, 12] = 11\r\nuniqueValues[] = ARRAYDISTINCT(source[])\r\nPRINT LBOUND(uniqueValues[])\r\nPRINT UBOUND(uniqueValues[])\r\nPRINT ARRAYLENGTH(uniqueValues[])\r\nPRINT uniqueValues[1]\r\nPRINT uniqueValues[2]\r\nPRINT uniqueValues[3]\r\nPRINT uniqueValues[4]\r\nPRINT ARRAYISALLOCATED(uniqueValues[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "5", "7", "9", "11", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFlattenOnMultidimensionalArrayReturnsCompactVisibleOrderResult()
    {
        const string text = "DIM board[1 TO 2, 10 TO 12] AS INTEGER\r\nDIM flat[] AS INTEGER\r\nboard[1, 10] = 3\r\nboard[1, 11] = 1\r\nboard[1, 12] = 2\r\nboard[2, 10] = 6\r\nboard[2, 11] = 4\r\nboard[2, 12] = 5\r\nflat[] = ARRAYFLATTEN(board[])\r\nPRINT ARRAYISALLOCATED(flat[])\r\nPRINT LBOUND(flat[])\r\nPRINT UBOUND(flat[])\r\nPRINT flat[1]\r\nPRINT flat[2]\r\nPRINT flat[3]\r\nPRINT flat[4]\r\nPRINT flat[5]\r\nPRINT flat[6]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "6", "3", "1", "2", "6", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReshapeRebuildsTemplateShapeFromSortedFlat()
    {
        const string text = "DIM board[1 TO 2, 10 TO 12] AS INTEGER\r\nDIM flat[] AS INTEGER\r\nDIM sorted[] AS INTEGER\r\nDIM reshaped[,] AS INTEGER\r\nboard[1, 10] = 3\r\nboard[1, 11] = 1\r\nboard[1, 12] = 2\r\nboard[2, 10] = 6\r\nboard[2, 11] = 4\r\nboard[2, 12] = 5\r\nflat[] = ARRAYFLATTEN(board[])\r\nsorted[] = ARRAYTOSORTED(flat[])\r\nreshaped[] = ARRAYRESHAPE(sorted[], board[])\r\nPRINT LBOUND(reshaped[], ROW)\r\nPRINT UBOUND(reshaped[], ROW)\r\nPRINT LBOUND(reshaped[], COLUMN)\r\nPRINT UBOUND(reshaped[], COLUMN)\r\nPRINT reshaped[1, 10]\r\nPRINT reshaped[1, 11]\r\nPRINT reshaped[1, 12]\r\nPRINT reshaped[2, 10]\r\nPRINT reshaped[2, 11]\r\nPRINT reshaped[2, 12]\r\nPRINT board[1, 10]\r\nPRINT board[2, 12]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "10", "12", "1", "2", "3", "4", "5", "6", "3", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReshapeReportsMismatchedVisibleElementCount()
    {
        const string text = "DIM source[1 TO 3] AS INTEGER\r\nDIM template[1 TO 2, 1 TO 2] AS INTEGER\r\nDIM reshaped[,] AS INTEGER\r\nreshaped[] = ARRAYRESHAPE(source[], template[])";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYRESHAPE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("template", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToSortedPreservesBoundsAndDoesNotMutateSource()
    {
        const string text = "DIM source[5 TO 9] AS INTEGER\r\nDIM sortedValues[] AS INTEGER\r\nsource[5] = 30\r\nsource[6] = 10\r\nsource[7] = 20\r\nsource[8] = 20\r\nsource[9] = 10\r\nsortedValues[] = ARRAYTOSORTED(source[])\r\nsortedValues[5] = 99\r\nPRINT LBOUND(sortedValues[])\r\nPRINT UBOUND(sortedValues[])\r\nPRINT source[5]\r\nPRINT source[6]\r\nPRINT source[7]\r\nPRINT source[8]\r\nPRINT source[9]\r\nPRINT sortedValues[5]\r\nPRINT sortedValues[6]\r\nPRINT sortedValues[7]\r\nPRINT sortedValues[8]\r\nPRINT sortedValues[9]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "9", "30", "10", "20", "20", "10", "99", "10", "20", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedSupportsStringsAndCanonicalEmptyArray()
    {
        const string text = "DIM names[2 TO 5] AS STRING\r\nDIM sortedNames[] AS STRING\r\nDIM emptyNames[1 TO 0] AS STRING\r\nDIM emptySorted[] AS STRING\r\nnames[2] = \"Cara\"\r\nnames[3] = \"Ann\"\r\nnames[4] = \"Bob\"\r\nnames[5] = \"Bob\"\r\nsortedNames[] = ARRAYTOSORTED(names[])\r\nemptySorted[] = ARRAYTOSORTED(emptyNames[])\r\nPRINT LBOUND(sortedNames[])\r\nPRINT UBOUND(sortedNames[])\r\nPRINT sortedNames[2]\r\nPRINT sortedNames[3]\r\nPRINT sortedNames[4]\r\nPRINT sortedNames[5]\r\nPRINT ARRAYISALLOCATED(emptySorted[])\r\nPRINT LBOUND(emptySorted[])\r\nPRINT UBOUND(emptySorted[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "5", "Ann", "Bob", "Bob", "Cara", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nDIM sortedValues[] AS INTEGER\r\nsortedValues[] = ARRAYTOSORTED(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOSORTED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFillMutatesArrayInPlace()
    {
        const string text = "DIM values[5 TO 7] AS INTEGER\r\nvalues[5] = 1\r\nvalues[6] = 2\r\nvalues[7] = 3\r\nARRAYFILL(values[], 42)\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT values[7]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "42", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillMutatesMultidimensionalVisibleElements()
    {
        const string text = "DIM values[1 TO 3, 1 TO 3] AS INTEGER\r\nvalues[1, 1] = 1\r\nvalues[2, 2] = 2\r\nvalues[3, 3] = 3\r\nARRAYFILL(values[2 TO 3, 2 TO 3], 8)\r\nPRINT values[1, 1]\r\nPRINT values[2, 2]\r\nPRINT values[2, 3]\r\nPRINT values[3, 2]\r\nPRINT values[3, 3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "8", "8", "8", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseMutatesArrayInPlace()
    {
        const string text = "DIM values[5 TO 8] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nvalues[8] = 40\r\nARRAYREVERSE(values[])\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT values[7]\r\nPRINT values[8]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["40", "30", "20", "10", "5", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseSupportsMultidimensionalDimension()
    {
        const string text = "DIM board[1 TO 2, 10 TO 12] AS INTEGER\r\nboard[1, 10] = 110\r\nboard[1, 11] = 111\r\nboard[1, 12] = 112\r\nboard[2, 10] = 210\r\nboard[2, 11] = 211\r\nboard[2, 12] = 212\r\nARRAYREVERSE(board[], COLUMN)\r\nPRINT board[1, 10]\r\nPRINT board[1, 11]\r\nPRINT board[1, 12]\r\nPRINT board[2, 10]\r\nPRINT board[2, 11]\r\nPRINT board[2, 12]\r\nARRAYREVERSE(board[], ROW)\r\nPRINT board[1, 10]\r\nPRINT board[1, 11]\r\nPRINT board[1, 12]\r\nPRINT board[2, 10]\r\nPRINT board[2, 11]\r\nPRINT board[2, 12]\r\nPRINT LBOUND(board[], ROW)\r\nPRINT UBOUND(board[], ROW)\r\nPRINT LBOUND(board[], COLUMN)\r\nPRINT UBOUND(board[], COLUMN)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "111", "110", "212", "211", "210", "212", "211", "210", "112", "111", "110", "1", "2", "10", "12"], host.Output);
    }

    [Fact]
    public void Execute_ArraySortMutatesArrayInPlaceAcrossTypes()
    {
        const string text = "DIM numbers[5 TO 9] AS INTEGER\r\nDIM names[2 TO 5] AS STRING\r\nnumbers[5] = 30\r\nnumbers[6] = 10\r\nnumbers[7] = 20\r\nnumbers[8] = 20\r\nnumbers[9] = 10\r\nnames[2] = \"Cara\"\r\nnames[3] = \"Ann\"\r\nnames[4] = \"Bob\"\r\nnames[5] = \"Bob\"\r\nARRAYSORT(numbers[])\r\nARRAYSORT(names[])\r\nPRINT numbers[5]\r\nPRINT numbers[6]\r\nPRINT numbers[7]\r\nPRINT numbers[8]\r\nPRINT numbers[9]\r\nPRINT LBOUND(numbers[])\r\nPRINT UBOUND(numbers[])\r\nPRINT names[2]\r\nPRINT names[3]\r\nPRINT names[4]\r\nPRINT names[5]\r\nPRINT LBOUND(names[])\r\nPRINT UBOUND(names[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10", "20", "20", "30", "5", "9", "Ann", "Bob", "Bob", "Cara", "2", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMutatingProceduresAllowCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nARRAYFILL(values[], 1)\r\nARRAYREVERSE(values[])\r\nARRAYSORT(values[])\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nARRAYFILL(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReverseReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nARRAYREVERSE(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREVERSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySortReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nARRAYSORT(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSORT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushAppendsValueAndPreservesLowerBound()
    {
        const string text = "DIM values[5 TO 6] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nARRAYPUSH(values[], 30)\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT values[7]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAllowsCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nARRAYPUSH(values[], 42)\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopReturnsLastElementAndShrinksArray()
    {
        const string text = "DIM values[5 TO 7] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nPRINT ARRAYPOP(values[])\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["30", "10", "20", "5", "6", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = "DIM values[5 TO 5] AS INTEGER\r\nvalues[5] = 99\r\nPRINT ARRAYPOP(values[])\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nARRAYPUSH(values[], 77)\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT values[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nARRAYPUSH(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYPOP(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYPOP(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushReportsUpperBoundOverflow()
    {
        const string text = "DIM values[2147483647 TO 2147483647] AS INTEGER\r\nARRAYPUSH(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertShiftsTailAndPreservesLowerBound()
    {
        const string text = "DIM values[5 TO 7] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 30\r\nvalues[7] = 40\r\nARRAYINSERT(values[], 6, 20)\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT values[7]\r\nPRINT values[8]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "40", "5", "8", "4"], host.Output);
    }

    [Fact]
    public void Execute_ArrayInsertAllowsCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nARRAYINSERT(values[], 1, 42)\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveReturnsRemovedElementAndShiftsTail()
    {
        const string text = "DIM values[5 TO 8] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nvalues[8] = 40\r\nPRINT ARRAYREMOVE(values[], 6)\r\nPRINT values[5]\r\nPRINT values[6]\r\nPRINT values[7]\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "10", "30", "40", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = "DIM values[5 TO 5] AS INTEGER\r\nvalues[5] = 99\r\nPRINT ARRAYREMOVE(values[], 5)\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nARRAYINSERT(values[], 1, 77)\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT values[1]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()
    {
        const string text = "DIM board[1 TO 2, 10 TO 11] AS INTEGER\r\nDIM newColumn[1 TO 2] AS INTEGER\r\nDIM removedColumn[] AS INTEGER\r\nboard[1, 10] = 110\r\nboard[1, 11] = 111\r\nboard[2, 10] = 210\r\nboard[2, 11] = 211\r\nnewColumn[1] = 112\r\nnewColumn[2] = 212\r\nARRAYPUSH(board[], newColumn[], COLUMN)\r\nPRINT board[1, 12]\r\nPRINT board[2, 12]\r\nremovedColumn[] = ARRAYPOP(board[], COLUMN)\r\nPRINT LBOUND(removedColumn[])\r\nPRINT UBOUND(removedColumn[])\r\nPRINT removedColumn[1]\r\nPRINT removedColumn[2]\r\nARRAYINSERT(board[], 11, newColumn[], COLUMN)\r\nremovedColumn[] = ARRAYREMOVE(board[], 10, COLUMN)\r\nPRINT removedColumn[1]\r\nPRINT removedColumn[2]\r\nPRINT LBOUND(board[], COLUMN)\r\nPRINT UBOUND(board[], COLUMN)\r\nPRINT board[1, 10]\r\nPRINT board[1, 11]\r\nPRINT board[2, 10]\r\nPRINT board[2, 11]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["112", "212", "1", "2", "112", "212", "110", "210", "10", "11", "112", "111", "212", "211"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayInsertReportsIncompatibleValueShape()
    {
        const string text = "DIM board[1 TO 2, 10 TO 11] AS INTEGER\r\nDIM wrongRow[10 TO 11] AS INTEGER\r\nARRAYINSERT(board[], 10, wrongRow[], COLUMN)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures()
    {
        const string text = "DIM values[5 TO 8] AS INTEGER\r\nDIM threshold AS INTEGER = 25\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nvalues[8] = 40\r\n\r\nPRINT ARRAYEVERY(values[], IsPositive)\r\nPRINT ARRAYSOME(values[], GreaterThanThreshold)\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION\r\n\r\nFUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > threshold\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()
    {
        const string text = "DIM values[2 TO 3, 4 TO 5] AS INTEGER\r\nDIM mapped[,] AS STRING\r\nvalues[2, 4] = 10\r\nvalues[2, 5] = 20\r\nvalues[3, 4] = 30\r\nvalues[3, 5] = 40\r\n\r\nPRINT ARRAYEVERY(values[], IsPositive)\r\nPRINT ARRAYSOME(values[2 TO 3, 5 TO 5], IsLarge)\r\nmapped[] = ARRAYMAP(values[2 TO 3, 5 TO 5], FormatValue)\r\nPRINT LBOUND(mapped[], 1)\r\nPRINT UBOUND(mapped[], 1)\r\nPRINT LBOUND(mapped[], 2)\r\nPRINT UBOUND(mapped[], 2)\r\nPRINT mapped[2, 5]\r\nPRINT mapped[3, 5]\r\nPRINT ARRAYREDUCE(values[2 TO 3, 4 TO 5], \"\", AppendValue)\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION\r\n\r\nFUNCTION IsLarge(value AS INTEGER) AS BOOLEAN\r\n    RETURN value >= 40\r\nEND FUNCTION\r\n\r\nFUNCTION FormatValue(value AS INTEGER) AS STRING\r\n    RETURN \"N\" + STR(value)\r\nEND FUNCTION\r\n\r\nFUNCTION AppendValue(accumulator AS STRING, value AS INTEGER) AS STRING\r\n    RETURN accumulator + \"[\" + STR(value) + \"]\"\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "2", "3", "5", "5", "N20", "N40", "[10][20][30][40]"], host.Output);
    }

    [Fact]
    public void Execute_ArrayEveryTreatsCanonicalEmptyArrayAsTrueAndArraySomeAsFalse()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYEVERY(values[], IsPositive)\r\nPRINT ARRAYSOME(values[], IsPositive)\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReturnsCompactArrayAndPreservesSource()
    {
        const string text = "DIM values[5 TO 8] AS INTEGER\r\nDIM threshold AS INTEGER = 25\r\nDIM filtered[] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nvalues[8] = 40\r\nfiltered[] = ARRAYFILTER(values[], GreaterThanThreshold)\r\nfiltered[1] = 99\r\nPRINT LBOUND(filtered[])\r\nPRINT UBOUND(filtered[])\r\nPRINT ARRAYLENGTH(filtered[])\r\nPRINT values[7]\r\nPRINT filtered[1]\r\nPRINT filtered[2]\r\n\r\nFUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > threshold\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "30", "99", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterAllowsCanonicalEmptyInputAndResult()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nDIM filtered[] AS INTEGER\r\nfiltered[] = ARRAYFILTER(values[], IsPositive)\r\nPRINT ARRAYISALLOCATED(filtered[])\r\nPRINT LBOUND(filtered[])\r\nPRINT UBOUND(filtered[])\r\nPRINT ARRAYLENGTH(filtered[])\r\n\r\nREDIM values[5 TO 6]\r\nvalues[5] = 1\r\nvalues[6] = 2\r\nfiltered[] = ARRAYFILTER(values[], IsLarge)\r\nPRINT ARRAYISALLOCATED(filtered[])\r\nPRINT LBOUND(filtered[])\r\nPRINT UBOUND(filtered[])\r\nPRINT ARRAYLENGTH(filtered[])\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION\r\n\r\nFUNCTION IsLarge(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 100\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0", "TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()
    {
        const string text = "DIM values[2 TO 3, 4 TO 6] AS INTEGER\r\nDIM filtered[] AS INTEGER\r\nvalues[2, 4] = 10\r\nvalues[2, 5] = 21\r\nvalues[2, 6] = 30\r\nvalues[3, 4] = 41\r\nvalues[3, 5] = 50\r\nvalues[3, 6] = 61\r\nfiltered[] = ARRAYFILTER(values[2 TO 3, 4 TO 6], IsOdd)\r\nPRINT LBOUND(filtered[])\r\nPRINT UBOUND(filtered[])\r\nPRINT ARRAYLENGTH(filtered[])\r\nPRINT filtered[1]\r\nPRINT filtered[2]\r\nPRINT filtered[3]\r\n\r\nFUNCTION IsOdd(value AS INTEGER) AS BOOLEAN\r\n    RETURN value MOD 2 = 1\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["1", "3", "3", "21", "41", "61"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nDIM filtered[] AS INTEGER\r\nfiltered[] = ARRAYFILTER(values[], IsPositive)\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFilterReportsEmptyFunctionValue()
    {
        const string text = "DIM values[1 TO 2] AS INTEGER\r\nDIM predicate AS FUNCTION(INTEGER) AS BOOLEAN\r\nDIM filtered[] AS INTEGER\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nfiltered[] = ARRAYFILTER(values[], predicate)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapPreservesTransformedArrayShapeAndPreservesSource()
    {
        const string text = "DIM values[5 TO 8] AS INTEGER\r\nDIM factor AS INTEGER = 3\r\nDIM mapped[] AS INTEGER\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nvalues[8] = 40\r\nmapped[] = ARRAYMAP(values[], Multiply)\r\nmapped[5] = 99\r\nPRINT LBOUND(mapped[])\r\nPRINT UBOUND(mapped[])\r\nPRINT ARRAYLENGTH(mapped[])\r\nPRINT values[5]\r\nPRINT mapped[5]\r\nPRINT mapped[6]\r\nPRINT mapped[7]\r\nPRINT mapped[8]\r\n\r\nFUNCTION Multiply(value AS INTEGER) AS INTEGER\r\n    RETURN value * factor\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "8", "4", "10", "99", "60", "90", "120"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapSupportsPolymorphicResultType()
    {
        const string text = "DIM values[5 TO 7] AS INTEGER\r\nDIM prefix AS STRING = \"item:\"\r\nDIM mapped[] AS STRING\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nmapped[] = ARRAYMAP(values[], FormatValue)\r\nPRINT LBOUND(mapped[])\r\nPRINT UBOUND(mapped[])\r\nPRINT mapped[5]\r\nPRINT mapped[6]\r\nPRINT mapped[7]\r\n\r\nFUNCTION FormatValue(value AS INTEGER) AS STRING\r\n    RETURN prefix + STR(value)\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "7", "item:10", "item:20", "item:30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapAllowsCanonicalEmptyInput()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nDIM mapped[] AS INTEGER\r\nmapped[] = ARRAYMAP(values[], DoubleIt)\r\nPRINT ARRAYISALLOCATED(mapped[])\r\nPRINT LBOUND(mapped[])\r\nPRINT UBOUND(mapped[])\r\nPRINT ARRAYLENGTH(mapped[])\r\n\r\nFUNCTION DoubleIt(value AS INTEGER) AS INTEGER\r\n    RETURN value * 2\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapPreservesMultidimensionalEmptyVisibleShape()
    {
        const string text = "DIM values[1 TO 2, 1 TO 2] AS INTEGER\r\nDIM empty[,] AS INTEGER\r\nempty[] = ARRAYTAKE(values[], 0, 1)\r\nDIM mapped[,] AS INTEGER\r\nmapped[] = ARRAYMAP(empty[], DoubleIt)\r\nPRINT ARRAYISALLOCATED(mapped[])\r\nPRINT LBOUND(mapped[], 1)\r\nPRINT UBOUND(mapped[], 1)\r\nPRINT LBOUND(mapped[], 2)\r\nPRINT UBOUND(mapped[], 2)\r\nPRINT ARRAYLENGTH(mapped[])\r\n\r\nFUNCTION DoubleIt(value AS INTEGER) AS INTEGER\r\n    RETURN value * 2\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "1", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nDIM mapped[] AS INTEGER\r\nmapped[] = ARRAYMAP(values[], DoubleIt)\r\n\r\nFUNCTION DoubleIt(value AS INTEGER) AS INTEGER\r\n    RETURN value * 2\r\nEND FUNCTION";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYMAP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapReportsEmptyFunctionValue()
    {
        const string text = "DIM values[1 TO 2] AS INTEGER\r\nDIM selector AS FUNCTION(INTEGER) AS STRING\r\nDIM mapped[] AS STRING\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nmapped[] = ARRAYMAP(values[], selector)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceUsesSeedAndClosure()
    {
        const string text = "DIM values[5 TO 7] AS INTEGER\r\nDIM bonus AS INTEGER = 1\r\nvalues[5] = 10\r\nvalues[6] = 20\r\nvalues[7] = 30\r\nPRINT ARRAYREDUCE(values[], 5, AddWithBonus)\r\nPRINT values[5]\r\n\r\nFUNCTION AddWithBonus(acc AS INTEGER, value AS INTEGER) AS INTEGER\r\n    RETURN acc + value + bonus\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["68", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceSupportsPolymorphicAccumulatorType()
    {
        const string text = "DIM words[3 TO 5] AS STRING\r\nwords[3] = \"alpha\"\r\nwords[4] = \"bb\"\r\nwords[5] = \"ccc\"\r\nPRINT ARRAYREDUCE(words[], 0, AddLength)\r\n\r\nFUNCTION AddLength(acc AS INTEGER, value AS STRING) AS INTEGER\r\n    RETURN acc + LEN(value)\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReturnsSeedForCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYREDUCE(values[], 7, Add)\r\n\r\nFUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER\r\n    RETURN acc + value\r\nEND FUNCTION";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYREDUCE(values[], 0, Add)\r\n\r\nFUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER\r\n    RETURN acc + value\r\nEND FUNCTION";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREDUCE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceReportsEmptyFunctionValue()
    {
        const string text = "DIM values[1 TO 2] AS STRING\r\nDIM reducer AS FUNCTION(INTEGER, STRING) AS INTEGER\r\nvalues[1] = \"aa\"\r\nvalues[2] = \"bbb\"\r\nPRINT ARRAYREDUCE(values[], 0, reducer)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYSOME(values[], IsPositive)\r\n\r\nFUNCTION IsPositive(value AS INTEGER) AS BOOLEAN\r\n    RETURN value > 0\r\nEND FUNCTION";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSOME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportEmptyFunctionValue()
    {
        const string text = "DIM values[1 TO 2] AS INTEGER\r\nDIM predicate AS FUNCTION(INTEGER) AS BOOLEAN\r\nvalues[1] = 10\r\nvalues[2] = 20\r\nPRINT ARRAYEVERY(values[], predicate)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nARRAYINSERT(values[], 1, 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYREMOVE(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndex()
    {
        const string text = "DIM values[5 TO 6] AS INTEGER\r\nARRAYINSERT(values[], 4, 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("диапазон", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsInvalidIndex()
    {
        const string text = "DIM values[5 TO 6] AS INTEGER\r\nPRINT ARRAYREMOVE(values[], 7)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("существующий логический индекс", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYREMOVE(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndexForCanonicalEmptyArray()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nARRAYINSERT(values[], 2, 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("равен 1", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUpperBoundOverflow()
    {
        const string text = "DIM values[2147483647 TO 2147483647] AS INTEGER\r\nARRAYINSERT(values[], 2147483647, 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsUseLogicalIndexesAcrossLowerBounds()
    {
        const string text = "DIM oneBased[1 TO 3] AS INTEGER\r\nDIM zeroBased[0 TO 2] AS INTEGER\r\nDIM shifted[5 TO 7] AS INTEGER\r\nDIM negative[-3 TO -1] AS INTEGER\r\noneBased[1] = 10\r\noneBased[2] = 20\r\noneBased[3] = 20\r\nzeroBased[0] = 10\r\nzeroBased[1] = 20\r\nzeroBased[2] = 30\r\nshifted[5] = 10\r\nshifted[6] = 20\r\nshifted[7] = 30\r\nnegative[-3] = 10\r\nnegative[-2] = 20\r\nnegative[-1] = 20\r\nPRINT ARRAYCONTAINS(oneBased[], 20)\r\nPRINT ARRAYCOUNT(oneBased[], 20)\r\nPRINT ARRAYINDEXOF(oneBased[], 20)\r\nPRINT ARRAYLASTINDEXOF(oneBased[], 20)\r\nPRINT ARRAYINDEXOF(oneBased[], 99)\r\nPRINT ARRAYINDEXOF(zeroBased[], 20)\r\nPRINT ARRAYINDEXOF(zeroBased[], 99)\r\nPRINT ARRAYINDEXOF(shifted[], 20)\r\nPRINT ARRAYINDEXOF(shifted[], 99)\r\nPRINT ARRAYLASTINDEXOF(negative[], 20)\r\nPRINT ARRAYINDEXOF(negative[], 99)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "2", "3", "0", "1", "-1", "6", "4", "-1", "-4"], host.Output);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYCONTAINS(values[], 1)\r\nPRINT ARRAYCOUNT(values[], 1)\r\nPRINT ARRAYINDEXOF(values[], 1)\r\nPRINT ARRAYLASTINDEXOF(values[], 1)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsAndCountScanMultidimensionalVisibleElements()
    {
        const string text = "DIM values[1 TO 3, 1 TO 3] AS INTEGER\r\nvalues[1, 1] = 5\r\nvalues[1, 2] = 7\r\nvalues[2, 2] = 7\r\nvalues[3, 3] = 7\r\nPRINT ARRAYCONTAINS(values[], 7)\r\nPRINT ARRAYCOUNT(values[], 7)\r\nPRINT ARRAYCONTAINS(values[2 TO 3, 2 TO 3], 5)\r\nPRINT ARRAYCOUNT(values[2 TO 3, 2 TO 3], 7)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "3", "FALSE", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFindAllReturnsCompactArrayOfLogicalIndexes()
    {
        const string text = "DIM shifted[5 TO 8] AS INTEGER\r\nDIM negative[-3 TO 0] AS INTEGER\r\nDIM found[] AS INTEGER\r\nDIM negativeHits[] AS INTEGER\r\nDIM missing[] AS INTEGER\r\nshifted[5] = 10\r\nshifted[6] = 20\r\nshifted[7] = 10\r\nshifted[8] = 30\r\nnegative[-3] = 20\r\nnegative[-2] = 10\r\nnegative[-1] = 20\r\nnegative[0] = 20\r\nfound[] = ARRAYFINDALL(shifted[], 10)\r\nnegativeHits[] = ARRAYFINDALL(negative[], 20)\r\nmissing[] = ARRAYFINDALL(shifted[], 99)\r\nPRINT ARRAYISALLOCATED(found[])\r\nPRINT LBOUND(found[])\r\nPRINT UBOUND(found[])\r\nPRINT found[1]\r\nPRINT found[2]\r\nPRINT ARRAYISALLOCATED(negativeHits[])\r\nPRINT LBOUND(negativeHits[])\r\nPRINT UBOUND(negativeHits[])\r\nPRINT negativeHits[1]\r\nPRINT negativeHits[2]\r\nPRINT negativeHits[3]\r\nPRINT ARRAYISALLOCATED(missing[])\r\nPRINT LBOUND(missing[])\r\nPRINT UBOUND(missing[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "5", "7", "TRUE", "1", "3", "-3", "-1", "0", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCoordinateSearchFunctionsReturnCoordinateVectorsAndTables()
    {
        const string text = "DIM grid[1 TO 2, 5 TO 7] AS INTEGER\r\nDIM cube[0 TO 1, 10 TO 11, -1 TO 0] AS INTEGER\r\nDIM firstHit[] AS INTEGER\r\nDIM lastHit[] AS INTEGER\r\nDIM allHits[,] AS INTEGER\r\nDIM cubeHit[] AS INTEGER\r\nDIM missing[] AS INTEGER\r\nDIM noHits[,] AS INTEGER\r\ngrid[1, 5] = 10\r\ngrid[1, 6] = 20\r\ngrid[2, 6] = 20\r\ngrid[2, 7] = 20\r\ncube[1, 11, 0] = 7\r\nfirstHit[] = ARRAYINDEXOF(grid[], 20)\r\nlastHit[] = ARRAYLASTINDEXOF(grid[], 20)\r\nallHits[] = ARRAYFINDALL(grid[], 20)\r\ncubeHit[] = ARRAYINDEXOF(cube[], 7)\r\nmissing[] = ARRAYINDEXOF(grid[], 99)\r\nnoHits[] = ARRAYFINDALL(grid[], 99)\r\nPRINT ARRAYISALLOCATED(firstHit[])\r\nPRINT LBOUND(firstHit[])\r\nPRINT UBOUND(firstHit[])\r\nPRINT firstHit[1]\r\nPRINT firstHit[2]\r\nPRINT lastHit[1]\r\nPRINT lastHit[2]\r\nPRINT LBOUND(allHits[], 1)\r\nPRINT UBOUND(allHits[], 1)\r\nPRINT LBOUND(allHits[], 2)\r\nPRINT UBOUND(allHits[], 2)\r\nPRINT allHits[1, 1]\r\nPRINT allHits[1, 2]\r\nPRINT allHits[2, 1]\r\nPRINT allHits[2, 2]\r\nPRINT allHits[3, 1]\r\nPRINT allHits[3, 2]\r\nPRINT cubeHit[1]\r\nPRINT cubeHit[2]\r\nPRINT cubeHit[3]\r\nPRINT ARRAYISALLOCATED(missing[])\r\nPRINT LBOUND(missing[])\r\nPRINT UBOUND(missing[])\r\nPRINT ARRAYISALLOCATED(noHits[])\r\nPRINT LBOUND(noHits[], 1)\r\nPRINT UBOUND(noHits[], 1)\r\nPRINT LBOUND(noHits[], 2)\r\nPRINT UBOUND(noHits[], 2)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "6", "2", "7", "1", "3", "1", "2", "1", "6", "2", "6", "2", "7", "1", "11", "0", "TRUE", "1", "0", "TRUE", "1", "0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReturnsFirstLogicalIndexAcrossLowerBounds()
    {
        const string text = "DIM numbers[5 TO 9] AS INTEGER\r\nDIM zeroBased[0 TO 3] AS INTEGER\r\nDIM names[2 TO 5] AS STRING\r\nnumbers[5] = 10\r\nnumbers[6] = 20\r\nnumbers[7] = 20\r\nnumbers[8] = 20\r\nnumbers[9] = 30\r\nzeroBased[0] = 10\r\nzeroBased[1] = 20\r\nzeroBased[2] = 30\r\nzeroBased[3] = 40\r\nnames[2] = \"Ann\"\r\nnames[3] = \"Bob\"\r\nnames[4] = \"Bob\"\r\nnames[5] = \"Cara\"\r\nPRINT ARRAYBINARYSEARCH(numbers[], 20)\r\nPRINT ARRAYBINARYSEARCH(numbers[], 15)\r\nPRINT ARRAYBINARYSEARCH(zeroBased[], 10)\r\nPRINT ARRAYBINARYSEARCH(zeroBased[], 35)\r\nPRINT ARRAYBINARYSEARCH(names[], \"Bob\")\r\nPRINT ARRAYBINARYSEARCH(names[], \"Aaron\")";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "4", "0", "-1", "3", "1"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchTreatsCanonicalEmptyArrayAsNotFound()
    {
        const string text = "DIM values[1 TO 0] AS INTEGER\r\nPRINT ARRAYBINARYSEARCH(values[], 1)";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalSortingGroupSortsAlongExplicitDimension()
    {
        const string text = "DIM board[1 TO 2, 1 TO 3] AS INTEGER\r\nDIM sortedByColumn[1 TO 2, 1 TO 3] AS INTEGER\r\nboard[1, 1] = 4\r\nboard[1, 2] = 2\r\nboard[1, 3] = 6\r\nboard[2, 1] = 3\r\nboard[2, 2] = 1\r\nboard[2, 3] = 5\r\nsortedByColumn[] = ARRAYTOSORTED(board[], COLUMN)\r\nPRINT board[1, 1]\r\nPRINT sortedByColumn[1, 1]\r\nPRINT sortedByColumn[1, 2]\r\nPRINT sortedByColumn[1, 3]\r\nPRINT sortedByColumn[2, 1]\r\nPRINT sortedByColumn[2, 2]\r\nPRINT sortedByColumn[2, 3]\r\nARRAYSORT(board[], ROW)\r\nPRINT board[1, 1]\r\nPRINT board[1, 2]\r\nPRINT board[1, 3]\r\nPRINT board[2, 1]\r\nPRINT board[2, 2]\r\nPRINT board[2, 3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4", "2", "4", "6", "1", "3", "5", "3", "1", "5", "4", "2", "6"], host.Output);
    }

    [Fact]
    public void Execute_MultidimensionalArrayBinarySearchReturnsCoordinateVectorOrEmptyArray()
    {
        const string text = "DIM board[1 TO 2, 1 TO 4] AS INTEGER\r\nDIM firstHit[] AS INTEGER\r\nDIM missing[] AS INTEGER\r\nboard[1, 1] = 1\r\nboard[1, 2] = 4\r\nboard[1, 3] = 4\r\nboard[1, 4] = 7\r\nboard[2, 1] = 2\r\nboard[2, 2] = 4\r\nboard[2, 3] = 5\r\nboard[2, 4] = 8\r\nfirstHit[] = ARRAYBINARYSEARCH(board[], 4, ROW)\r\nmissing[] = ARRAYBINARYSEARCH(board[], 6, ROW)\r\nPRINT ARRAYISALLOCATED(firstHit[])\r\nPRINT LBOUND(firstHit[])\r\nPRINT UBOUND(firstHit[])\r\nPRINT firstHit[1]\r\nPRINT firstHit[2]\r\nPRINT ARRAYISALLOCATED(missing[])\r\nPRINT LBOUND(missing[])\r\nPRINT UBOUND(missing[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "1", "2", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYCONTAINS(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONTAINS", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayIndexOfReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYINDEXOF(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayLastIndexOfReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYLASTINDEXOF(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYLASTINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCountReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYCOUNT(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFindAllReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nDIM matches[] AS INTEGER\r\nmatches[] = ARRAYFINDALL(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFINDALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReportsUnallocatedSource()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYBINARYSEARCH(values[], 1)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYBINARYSEARCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseMutatesSharedArrayStorage()
    {
        const string text = "DIM source[1 TO 2] AS INTEGER\r\nDIM alias[] AS INTEGER\r\nalias[] = source[]\r\nERASE alias[]\r\nPRINT source[1]";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinArrayInformationFunctionsReportCurrentState()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nREDIM values[3 TO 5]\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT ARRAYLENGTH(values[])\r\nERASE values[]\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "TRUE", "3", "5", "3", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_LBoundReportsUnallocatedArrayState()
    {
        const string text = "DIM values[] AS INTEGER\r\nPRINT LBOUND(values[])";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("LBOUND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
