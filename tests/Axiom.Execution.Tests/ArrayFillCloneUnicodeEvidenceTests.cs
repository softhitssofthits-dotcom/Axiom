namespace Axiom.Execution.Tests;

public sealed class ArrayFillCloneUnicodeEvidenceTests
{
    [Fact]
    public void FillAndCloneRespectVisibleOwnerAndIndependentCopy()
    {
        var source = "DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM copied[,] AS STRING\r\nDIM token AS STRING = LEFT(\"😀\", 1) + CHRW(0) + RIGHT(\"😀\", 1)\r\ngrid[-1, 5] = \"Сосед\"\r\ngrid[0, 5] = \"Левый\"\r\nARRAYFILL(grid[0 TO 1, 6 TO 7], token)\r\nPRINT grid[0, 6] = token\r\nPRINT grid[0, 7] = token\r\nPRINT grid[1, 6] = token\r\nPRINT grid[1, 7] = token\r\nPRINT grid[-1, 5] = \"Сосед\"\r\nPRINT grid[0, 5] = \"Левый\"\r\nPRINT grid[1, 5] = \"\"\r\ncopied[] = ARRAYCLONE(grid[0 TO 1, 6 TO 7])\r\nPRINT LBOUND(copied[], 1)\r\nPRINT UBOUND(copied[], 1)\r\nPRINT LBOUND(copied[], 2)\r\nPRINT UBOUND(copied[], 2)\r\nARRAYFILL(copied[], \"Замена\")\r\nPRINT grid[0, 6] = token\r\nPRINT grid[1, 7] = token\r\nPRINT copied[0, 6] = \"Замена\"\r\nPRINT copied[1, 7] = \"Замена\"\r\nDIM absent[] AS STRING\r\nDIM absentCopy[] AS STRING\r\nabsentCopy[] = ARRAYCLONE(absent[])\r\nPRINT ARRAYISALLOCATED(absentCopy[])\r\nTRY\r\n    ARRAYFILL(absent[], token)\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT ARRAYISALLOCATED(absent[])";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(Enumerable.Repeat("TRUE", 7).Concat(new[] { "0", "1", "6", "7", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "MB3005", "FALSE" }), run.Output);
    }
}
