namespace Axiom.Execution.Tests;

public sealed class ArrayMetadataBoundaryEvidenceTests
{
    [Fact]
    public void ExtremeBoundsAndEmptyShapeRemainExactAndIndependent()
    {
        var source = """
            DIM grid(-2147483647 TO -2147483646, 2147483646 TO 2147483647) AS STRING
            DIM bounds(,) AS INTEGER
            DIM empty(,) AS STRING
            PRINT LBOUND(grid)
            PRINT UBOUND(grid)
            PRINT LBOUND(grid, 2)
            PRINT UBOUND(grid, 2)
            PRINT ARRAYRANK(grid)
            PRINT ARRAYLENGTH(grid)
            PRINT ARRAYISALLOCATED(grid)
            bounds = ARRAYBOUNDS(grid)
            PRINT ARRAYRANK(bounds)
            PRINT ARRAYLENGTH(bounds)
            PRINT bounds(1, 1)
            PRINT bounds(1, 2)
            PRINT bounds(2, 1)
            PRINT bounds(2, 2)
            bounds(1, 1) = 99
            PRINT LBOUND(grid)
            empty = ARRAYTAKE(grid, 0, 2)
            PRINT ARRAYISALLOCATED(empty)
            PRINT ARRAYLENGTH(empty)
            PRINT ARRAYRANK(empty)
            bounds = ARRAYBOUNDS(empty)
            PRINT bounds(1, 1)
            PRINT bounds(1, 2)
            PRINT bounds(2, 1)
            PRINT bounds(2, 2)
            DIM absent(,) AS STRING
            PRINT ARRAYISALLOCATED(absent)
            PRINT ARRAYLENGTH(absent)
            PRINT ARRAYRANK(absent)
            TRY
                bounds = ARRAYBOUNDS(absent)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT bounds(2, 2)
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "-2147483647", "-2147483646", "2147483646", "2147483647", "2", "4", "TRUE", "2", "4", "-2147483647", "-2147483646", "2147483646", "2147483647", "-2147483647", "TRUE", "0", "2", "-2147483647", "-2147483646", "1", "0", "FALSE", "0", "2", "MB3005", "0" }, run.Output);
    }
}
