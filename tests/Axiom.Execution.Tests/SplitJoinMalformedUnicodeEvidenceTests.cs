namespace Axiom.Execution.Tests;

public sealed class SplitJoinMalformedUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void SurrogateHalfSeparatorHasLiteralPartsAndJoinOutput(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM parts() AS STRING
            parts = SPLIT("😀А😀", high)
            PRINT ARRAYLENGTH(parts)
            PRINT parts(1) = ""
            PRINT parts(2) = low + "А"
            PRINT parts(3) = low
            PRINT JOIN(parts, "|") = "|" + low + "А|" + low
            parts = SPLIT("😀А😀", low)
            PRINT ARRAYLENGTH(parts)
            PRINT parts(1) = high
            PRINT parts(2) = "А" + high
            PRINT parts(3) = ""
            PRINT JOIN(parts) = high + ",А" + high + ","
            """, ["3", "TRUE", "TRUE", "TRUE", "TRUE", "3", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void NullSeparatorKeepsEachEmptyElement(ExecutionMode mode)
    {
        Check(mode, """
            DIM zero AS STRING = CHRW(0)
            DIM parts() AS STRING
            parts = SPLIT(zero + "А" + zero + zero + "Б" + zero, zero)
            PRINT ARRAYLENGTH(parts)
            PRINT parts(1) = ""
            PRINT parts(2) = "А"
            PRINT parts(3) = ""
            PRINT parts(4) = "Б"
            PRINT parts(5) = ""
            PRINT JOIN(parts, ":") = ":А::Б:"
            PRINT JOIN(parts, zero) = zero + "А" + zero + zero + "Б" + zero
            """, ["5", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void SeparatorIsWholeFragmentRatherThanCharacterSet(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM parts() AS STRING
            parts = SPLIT("😀А", high + "А")
            PRINT ARRAYLENGTH(parts)
            PRINT parts(1) = "😀А"
            parts = SPLIT(high + "А" + low, high + "А")
            PRINT ARRAYLENGTH(parts)
            PRINT parts(1) = ""
            PRINT parts(2) = low
            PRINT JOIN(parts, high) = "😀"
            """, ["1", "TRUE", "2", "TRUE", "TRUE", "TRUE"]);
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
