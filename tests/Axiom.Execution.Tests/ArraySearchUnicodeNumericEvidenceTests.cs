namespace Axiom.Execution.Tests;

public sealed class ArraySearchUnicodeNumericEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void StringsMatchWholeUtf16ValuesAndVisibleViews(ExecutionMode mode)
    {
        Check(mode, """
            DIM words(-3 TO 3) AS STRING
            DIM high AS STRING = LEFT("😀", 1)
            words(-3) = "é"
            words(-2) = "é"
            words(-1) = high
            words(0) = "😀"
            words(1) = "А" + CHRW(0)
            words(2) = high
            words(3) = ""
            PRINT ARRAYCOUNT(words, high)
            PRINT ARRAYINDEXOF(words, high)
            PRINT ARRAYLASTINDEXOF(words, high)
            PRINT ARRAYCOUNT(words, "é")
            PRINT ARRAYCOUNT(words, "é")
            PRINT ARRAYCOUNT(words, "😀")
            PRINT ARRAYCONTAINS(words, "А")
            PRINT ARRAYCOUNT(words, "А" + CHRW(0))
            PRINT ARRAYCOUNT(words, "")
            PRINT ARRAYCONTAINS(words(-2 TO 1), high)
            PRINT ARRAYCOUNT(words(-2 TO 1), high)
            PRINT ARRAYINDEXOF(words(-2 TO 1), high)
            PRINT ARRAYLASTINDEXOF(words(-2 TO 1), high)
            PRINT words(-3) = "é"
            PRINT words(2) = high
            """, ["2", "-1", "2", "1", "1", "1", "FALSE", "1", "1", "TRUE", "1", "-1", "-1", "TRUE", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void LongAndDecimalNeighborsRemainDistinct(ExecutionMode mode)
    {
        Check(mode, """
            DIM values(-2 TO 1) AS LONG
            values(-2) = 9007199254740992
            values(-1) = 9007199254740993
            values(0) = 9223372036854775807
            values(1) = 9007199254740993
            PRINT ARRAYCOUNT(values, 9007199254740993)
            PRINT ARRAYINDEXOF(values, 9007199254740993)
            PRINT ARRAYLASTINDEXOF(values, 9007199254740993)
            PRINT ARRAYCONTAINS(values, 9223372036854775806)
            DIM amounts(4 TO 6) AS DECIMAL
            amounts(4) = 79228162514264337593543950335.0
            amounts(5) = 79228162514264337593543950334.0
            amounts(6) = 79228162514264337593543950335.0
            PRINT ARRAYCOUNT(amounts, 79228162514264337593543950335.0)
            PRINT ARRAYINDEXOF(amounts, 79228162514264337593543950334.0)
            PRINT ARRAYLASTINDEXOF(amounts, 79228162514264337593543950335.0)
            PRINT ARRAYCONTAINS(amounts, 79228162514264337593543950333.0)
            """, ["2", "-1", "1", "FALSE", "2", "5", "6", "FALSE"]);
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
