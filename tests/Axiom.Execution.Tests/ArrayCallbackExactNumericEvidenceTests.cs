namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackExactNumericEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void NumericCallbacksKeepExactValuesAndPreserveResultOnOverflow(ExecutionMode mode)
    {
        var source = """
            DIM longs(5 TO 6) AS LONG
            DIM mappedLongs() AS LONG
            DIM amounts(7 TO 8) AS DECIMAL
            DIM mappedAmounts() AS DECIMAL
            DIM calls AS INTEGER
            longs(5) = 9007199254740992
            longs(6) = 9007199254740993
            mappedLongs = ARRAYMAP(longs, KeepLong)
            PRINT mappedLongs(5)
            PRINT mappedLongs(6)
            PRINT ARRAYREDUCE(longs, CLNG(0), SumLong)
            amounts(7) = 79228162514264337593543950335.0
            amounts(8) = 79228162514264337593543950334.0
            mappedAmounts = ARRAYMAP(amounts, KeepDecimal)
            PRINT mappedAmounts(7)
            PRINT mappedAmounts(8)
            PRINT ARRAYREDUCE(amounts, CDEC(0), Difference)
            calls = 0
            TRY
                mappedAmounts = ARRAYMAP(amounts, OverflowDecimal)
            CATCH err
                PRINT err.Code
            END TRY
            PRINT calls
            PRINT mappedAmounts(7)
            PRINT mappedAmounts(8)
            PRINT amounts(7)
            FUNCTION KeepLong(value AS LONG) AS LONG
                RETURN value
            END FUNCTION
            FUNCTION SumLong(acc AS LONG, value AS LONG) AS LONG
                RETURN acc + value
            END FUNCTION
            FUNCTION KeepDecimal(value AS DECIMAL) AS DECIMAL
                RETURN value
            END FUNCTION
            FUNCTION Difference(acc AS DECIMAL, value AS DECIMAL) AS DECIMAL
                RETURN value - acc
            END FUNCTION
            FUNCTION OverflowDecimal(value AS DECIMAL) AS DECIMAL
                calls = calls + 1
                RETURN value + CDEC(1)
            END FUNCTION
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "9007199254740992", "9007199254740993", "18014398509481985", "79228162514264337593543950335", "79228162514264337593543950334", "-1", "MB3008", "1", "79228162514264337593543950335", "79228162514264337593543950334", "79228162514264337593543950335" }, run.Output);
    }
}
