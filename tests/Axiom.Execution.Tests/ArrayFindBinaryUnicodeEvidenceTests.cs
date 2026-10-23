namespace Axiom.Execution.Tests;

public sealed class ArrayFindBinaryUnicodeEvidenceTests
{
    [Fact]
    public void BinarySearchUsesSortedUtf16ValuesAndExactLongs()
    {
        Check("DIM words[-3 TO 3] AS STRING\r\nDIM high AS STRING = LEFT(\"😀\", 1)\r\nwords[-3] = \"\"\r\nwords[-2] = CHRW(0)\r\nwords[-1] = \"é\"\r\nwords[0] = \"é\"\r\nwords[1] = high\r\nwords[2] = \"😀\"\r\nwords[3] = CHRW(57344)\r\nPRINT ARRAYBINARYSEARCH(words[], \"\")\r\nPRINT ARRAYBINARYSEARCH(words[], CHRW(0), 1)\r\nPRINT ARRAYBINARYSEARCH(words[], \"é\")\r\nPRINT ARRAYBINARYSEARCH(words[], \"é\", 1)\r\nPRINT ARRAYBINARYSEARCH(words[], high)\r\nPRINT ARRAYBINARYSEARCH(words[], \"😀\", 1)\r\nPRINT ARRAYBINARYSEARCH(words[], CHRW(57344))\r\nPRINT ARRAYBINARYSEARCH(words[], RIGHT(\"😀\", 1))\r\nDIM numbers[5 TO 8] AS LONG\r\nnumbers[5] = 9007199254740992\r\nnumbers[6] = 9007199254740993\r\nnumbers[7] = 9007199254740993\r\nnumbers[8] = 9223372036854775807\r\nPRINT ARRAYBINARYSEARCH(numbers[], 9007199254740993)\r\nPRINT ARRAYBINARYSEARCH(numbers[], 9007199254740993, 1)\r\nPRINT ARRAYBINARYSEARCH(numbers[], 9223372036854775806)\r\nPRINT ARRAYBINARYSEARCH(numbers[], 9223372036854775807, 1)\r\nPRINT numbers[6]", ["-3", "-2", "-1", "0", "1", "2", "3", "-4", "6", "6", "4", "8", "9007199254740993"]);
    }

    [Fact]
    public void FindAllReturnsVisibleCoordinatesForWholeStrings()
    {
        Check("DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM hits[,] AS INTEGER\r\nDIM target AS STRING = LEFT(\"😀\", 1) + CHRW(0)\r\ngrid[-1, 5] = target\r\ngrid[0, 6] = target\r\ngrid[1, 7] = target\r\ngrid[0, 7] = \"😀\"\r\nhits[] = ARRAYFINDALL(grid[0 TO 1, 6 TO 7], target)\r\nPRINT ARRAYRANK(hits[])\r\nPRINT LBOUND(hits[], 1)\r\nPRINT UBOUND(hits[], 1)\r\nPRINT LBOUND(hits[], 2)\r\nPRINT UBOUND(hits[], 2)\r\nPRINT hits[1, 1]\r\nPRINT hits[1, 2]\r\nPRINT hits[2, 1]\r\nPRINT hits[2, 2]\r\nhits[] = ARRAYFINDALL(grid[], RIGHT(\"😀\", 1))\r\nPRINT ARRAYLENGTH(hits[])\r\nPRINT grid[-1, 5] = target\r\nPRINT grid[0, 7] = \"😀\"\r\nDIM words[-2 TO 0] AS STRING\r\nDIM indices[] AS INTEGER\r\nwords[-2] = \"é\"\r\nwords[-1] = \"é\"\r\nwords[0] = \"é\"\r\nindices[] = ARRAYFINDALL(words[], \"é\")\r\nPRINT indices[1]\r\nPRINT indices[2]", ["2", "1", "2", "1", "2", "0", "6", "1", "7", "0", "TRUE", "TRUE", "-2", "0"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
