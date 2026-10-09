namespace Axiom.Execution.Tests;

public sealed class ReplaceMalformedUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ReplaceMatchesUtf16UnitsAndDoesNotRescanInsertedText(ExecutionMode mode)
    {
        Check(mode, """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            PRINT REPLACE("😀А😀", high, "Н") = "Н" + low + "АН" + low
            PRINT REPLACE("😀А😀", low, "") = high + "А" + high
            PRINT REPLACE(high + "А" + low, "😀", "Н") = high + "А" + low
            PRINT REPLACE(low + high, high, "😀") = low + "😀"
            PRINT REPLACE("😀😀", "😀", "😀😀") = "😀😀😀😀"
            PRINT REPLACE(high + high + high, high + high, "Н") = "Н" + high
            PRINT REPLACE("éе́", "é", "Н") = "Не́"
            PRINT REPLACE("А" + CHRW(0) + "Б", CHRW(0), high) = "А" + high + "Б"
            """, Enumerable.Repeat("TRUE", 8).ToArray());
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void ReplaceManyLongestRuleWinsRegardlessOfShortRuleOrder(ExecutionMode mode, bool pairFirst)
    {
        var first = pairFirst ? "\"😀\"" : "high";
        var second = pairFirst ? "high" : "\"😀\"";
        var firstNew = pairFirst ? "\"😀\"" : "\"Н\"";
        var secondNew = pairFirst ? "\"Н\"" : "\"😀\"";
        Check(mode, $$"""
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM oldItems(-3 TO 1) AS STRING
            DIM newItems(8 TO 12) AS STRING
            oldItems(-3) = {{first}}
            oldItems(-2) = {{second}}
            oldItems(-1) = "😀"
            oldItems(0) = low
            oldItems(1) = CHRW(0)
            newItems(8) = {{firstNew}}
            newItems(9) = {{secondNew}}
            newItems(10) = "Дубль"
            newItems(11) = ""
            newItems(12) = "Ноль"
            PRINT REPLACEMANY("😀" + high + "А" + low + CHRW(0), oldItems, newItems) = "😀НАНоль"
            PRINT oldItems(-3) = {{first}}
            PRINT newItems(8) = {{firstNew}}
            """, ["TRUE", "TRUE", "TRUE"]);
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
