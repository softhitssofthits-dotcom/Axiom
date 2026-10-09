namespace Axiom.Execution.Tests;

public sealed class NumericSelectionBoundaryEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void AdjacentExtremeLongsAreSelectedWithoutDoubleRounding(ExecutionMode mode)
    {
        Check(mode, """
            DIM low AS LONG = -9223372036854775807 - CLNG(1)
            DIM high AS LONG = 9223372036854775807
            PRINT MIN(high, high - CLNG(1))
            PRINT MAX(low, low + CLNG(1))
            PRINT CLAMP(low, low + CLNG(1), high)
            PRINT CLAMP(high, low, high - CLNG(1))
            PRINT CLAMP(high, low, high)
            TRY
                PRINT CLAMP(low, high, low)
            CATCH err
                PRINT err.Code
            END TRY
            """, ["9223372036854775806", "-9223372036854775807", "-9223372036854775807", "9223372036854775806", "9223372036854775807", "MB3007"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void AdjacentExtremeDecimalsAndCollapsedIntervalStayExact(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS DECIMAL = CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591)
            DIM low AS DECIMAL = -high
            PRINT MIN(high, high - CDEC(1))
            PRINT MAX(low, low + CDEC(1))
            PRINT CLAMP(low, low + CDEC(1), high)
            PRINT CLAMP(high, low, high - CDEC(1))
            PRINT CLAMP(low, high, high)
            PRINT CLAMP(high, low, low)
            PRINT CLAMP(high, low, high)
            TRY
                PRINT CLAMP(CDEC(0), high, low)
            CATCH err
                PRINT err.Code
            END TRY
            """, ["79228162514264337593543950334", "-79228162514264337593543950334", "-79228162514264337593543950334", "79228162514264337593543950334", "79228162514264337593543950335", "-79228162514264337593543950335", "79228162514264337593543950335", "MB3007"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MinimumDecimalFractionIsComparedWithoutRoundingToZero(ExecutionMode mode)
    {
        Check(mode, """
            DIM tiny AS DECIMAL = CDEC(1) / (CDEC(1000000000) * CDEC(1000000000) * CDEC(10000000000))
            PRINT MIN(tiny, CDEC(0))
            PRINT MAX(-tiny, CDEC(0))
            PRINT MIN(-tiny, CDEC(0))
            PRINT MAX(tiny, CDEC(0))
            PRINT CLAMP(CDEC(0), tiny, tiny)
            PRINT CLAMP(CDEC(0), -tiny, -tiny)
            """, ["0", "0", "-0.0000000000000000000000000001", "0.0000000000000000000000000001", "0.0000000000000000000000000001", "-0.0000000000000000000000000001"]);
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
