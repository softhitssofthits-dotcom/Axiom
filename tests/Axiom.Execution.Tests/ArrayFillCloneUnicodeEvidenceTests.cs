namespace Axiom.Execution.Tests;

public sealed class ArrayFillCloneUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void FillAndCloneRespectVisibleOwnerAndIndependentCopy(ExecutionMode mode)
    {
        var source = """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM copied(,) AS STRING
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0) + RIGHT("😀", 1)
            grid(-1, 5) = "Сосед"
            grid(0, 5) = "Левый"
            ARRAYFILL(grid(0 TO 1, 6 TO 7), token)
            PRINT grid(0, 6) = token
            PRINT grid(0, 7) = token
            PRINT grid(1, 6) = token
            PRINT grid(1, 7) = token
            PRINT grid(-1, 5) = "Сосед"
            PRINT grid(0, 5) = "Левый"
            PRINT grid(1, 5) = ""
            copied = ARRAYCLONE(grid(0 TO 1, 6 TO 7))
            PRINT LBOUND(copied, 1)
            PRINT UBOUND(copied, 1)
            PRINT LBOUND(copied, 2)
            PRINT UBOUND(copied, 2)
            ARRAYFILL(copied, "Замена")
            PRINT grid(0, 6) = token
            PRINT grid(1, 7) = token
            PRINT copied(0, 6) = "Замена"
            PRINT copied(1, 7) = "Замена"
            DIM absent() AS STRING
            DIM absentCopy() AS STRING
            absentCopy = ARRAYCLONE(absent)
            PRINT ARRAYISALLOCATED(absentCopy)
            TRY
                ARRAYFILL(absent, token)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT ARRAYISALLOCATED(absent)
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(Enumerable.Repeat("TRUE", 7).Concat(new[] { "0", "1", "6", "7", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "MB3005", "FALSE" }), run.Output);
    }
}
