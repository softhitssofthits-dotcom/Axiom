namespace Axiom.Execution.Tests;

public sealed class ArrayMetadataBoundaryEvidenceTests
{
    [Fact]
    public void ExtremeBoundsAndEmptyShapeRemainExactAndIndependent()
    {
        var source = "DIM grid[-2147483647 TO -2147483646, 2147483646 TO 2147483647] AS STRING\r\nDIM bounds[,] AS INTEGER\r\nDIM empty[,] AS STRING\r\nPRINT LBOUND(grid[])\r\nPRINT UBOUND(grid[])\r\nPRINT LBOUND(grid[], 2)\r\nPRINT UBOUND(grid[], 2)\r\nPRINT ARRAYRANK(grid[])\r\nPRINT ARRAYLENGTH(grid[])\r\nPRINT ARRAYISALLOCATED(grid[])\r\nbounds[] = ARRAYBOUNDS(grid[])\r\nPRINT ARRAYRANK(bounds[])\r\nPRINT ARRAYLENGTH(bounds[])\r\nPRINT bounds[1, 1]\r\nPRINT bounds[1, 2]\r\nPRINT bounds[2, 1]\r\nPRINT bounds[2, 2]\r\nbounds[1, 1] = 99\r\nPRINT LBOUND(grid[])\r\nempty[] = ARRAYTAKE(grid[], 0, 2)\r\nPRINT ARRAYISALLOCATED(empty[])\r\nPRINT ARRAYLENGTH(empty[])\r\nPRINT ARRAYRANK(empty[])\r\nbounds[] = ARRAYBOUNDS(empty[])\r\nPRINT bounds[1, 1]\r\nPRINT bounds[1, 2]\r\nPRINT bounds[2, 1]\r\nPRINT bounds[2, 2]\r\nDIM absent[,] AS STRING\r\nPRINT ARRAYISALLOCATED(absent[])\r\nPRINT ARRAYLENGTH(absent[])\r\nPRINT ARRAYRANK(absent[])\r\nTRY\r\n    bounds[] = ARRAYBOUNDS(absent[])\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT bounds[2, 2]";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "-2147483647", "-2147483646", "2147483646", "2147483647", "2", "4", "TRUE", "2", "4", "-2147483647", "-2147483646", "2147483646", "2147483647", "-2147483647", "TRUE", "0", "2", "-2147483647", "-2147483646", "1", "0", "FALSE", "0", "2", "MB3005", "0" }, run.Output);
    }
}
