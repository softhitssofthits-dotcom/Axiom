namespace Axiom.Execution.Tests;

public sealed class ArrayConcatUnicodeViewEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ConcatenationKeepsWholeValuesAndIndependentVisibleCopies(ExecutionMode mode)
    {
        var source = """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM flat() AS STRING
            DIM merged(,) AS STRING
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0)
            grid(-1, 5) = "Внешний"
            grid(0, 6) = token
            grid(0, 7) = "é"
            grid(1, 6) = "😀"
            grid(1, 7) = "é"
            flat = ARRAYCONCAT(grid(0)(6 TO 7), grid(1)(6 TO 7))
            PRINT LBOUND(flat)
            PRINT UBOUND(flat)
            PRINT flat(1) = token
            PRINT flat(2) = "é"
            PRINT flat(3) = "😀"
            PRINT flat(4) = "é"
            merged = ARRAYCONCAT(grid(0 TO 1, 6 TO 7), grid(0 TO 1, 6 TO 7), 2)
            PRINT LBOUND(merged, 1)
            PRINT UBOUND(merged, 1)
            PRINT LBOUND(merged, 2)
            PRINT UBOUND(merged, 2)
            PRINT merged(0, 6) = token
            PRINT merged(0, 8) = token
            PRINT merged(1, 7) = "é"
            PRINT merged(1, 9) = "é"
            flat(1) = "Замена"
            merged(0, 6) = "Другая замена"
            PRINT grid(0, 6) = token
            PRINT merged(0, 8) = token
            PRINT grid(-1, 5) = "Внешний"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "1", "4", "TRUE", "TRUE", "TRUE", "TRUE", "0", "1", "6", "9", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE" }, run.Output);
    }
}
