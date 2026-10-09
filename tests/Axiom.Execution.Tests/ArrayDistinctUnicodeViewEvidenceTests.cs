namespace Axiom.Execution.Tests;

public sealed class ArrayDistinctUnicodeViewEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void DistinctPreservesMalformedUnitsAndVisibleFirstOccurrenceOrder(ExecutionMode mode)
    {
        var source = """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM result() AS STRING
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            grid(-1, 5) = "Внешнее"
            grid(0, 5) = high
            grid(0, 6) = high + CHRW(0)
            grid(0, 7) = "😀"
            grid(1, 5) = high
            grid(1, 6) = low
            grid(1, 7) = ""
            result = ARRAYDISTINCT(grid(0 TO 1, 5 TO 7))
            PRINT ARRAYRANK(result)
            PRINT LBOUND(result)
            PRINT UBOUND(result)
            PRINT result(1) = high
            PRINT result(2) = high + CHRW(0)
            PRINT result(3) = "😀"
            PRINT result(4) = low
            PRINT result(5) = ""
            result(1) = "Замена"
            PRINT grid(0, 5) = high
            PRINT grid(1, 5) = high
            PRINT grid(-1, 5) = "Внешнее"
            DIM amounts(4 TO 6) AS DECIMAL
            DIM uniqueAmounts() AS DECIMAL
            amounts(4) = 79228162514264337593543950335.0
            amounts(5) = 79228162514264337593543950334.0
            amounts(6) = 79228162514264337593543950335.0
            uniqueAmounts = ARRAYDISTINCT(amounts)
            PRINT ARRAYLENGTH(uniqueAmounts)
            PRINT uniqueAmounts(1)
            PRINT uniqueAmounts(2)
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "1", "1", "5", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "2", "79228162514264337593543950335", "79228162514264337593543950334" }, run.Output);
    }
}
