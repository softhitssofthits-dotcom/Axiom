namespace Axiom.Execution.Tests;

public sealed class ArrayShapeUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void VisibleUnicodeOrderAndIndependentTemplateShapeArePreserved(ExecutionMode mode)
    {
        var source = """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM flat() AS STRING
            DIM shaped(,) AS STRING
            DIM pattern(8 TO 11, -2 TO -1) AS STRING
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0)
            grid(-1, 5) = "Внешний"
            grid(0, 6) = token
            grid(0, 7) = "é"
            grid(1, 6) = "😀"
            grid(1, 7) = "é"
            pattern(9, -2) = "Шаблон"
            flat = ARRAYFLATTEN(grid(0 TO 1, 6 TO 7))
            PRINT LBOUND(flat)
            PRINT UBOUND(flat)
            PRINT flat(1) = token
            PRINT flat(2) = "é"
            PRINT flat(3) = "😀"
            PRINT flat(4) = "é"
            shaped = ARRAYRESHAPE(flat, pattern(9 TO 10, -2 TO -1))
            PRINT LBOUND(shaped, 1)
            PRINT UBOUND(shaped, 1)
            PRINT LBOUND(shaped, 2)
            PRINT UBOUND(shaped, 2)
            PRINT shaped(9, -2) = token
            PRINT shaped(9, -1) = "é"
            PRINT shaped(10, -2) = "😀"
            PRINT shaped(10, -1) = "é"
            flat(1) = "Замена"
            PRINT grid(0, 6) = token
            PRINT shaped(9, -2) = token
            shaped(9, -2) = "Другая замена"
            PRINT pattern(9, -2) = "Шаблон"
            TRY
                shaped = ARRAYRESHAPE(flat, pattern)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT shaped(9, -2) = "Другая замена"
            PRINT grid(-1, 5) = "Внешний"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "1", "4", "TRUE", "TRUE", "TRUE", "TRUE", "9", "10", "-2", "-1", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "MB3007", "TRUE", "TRUE" }, run.Output);
    }
}
