namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackUnicodeEvidenceTests
{
    [Fact]
    public void CallbacksReceiveWholeVisibleStringsInOrder()
    {
        var source = """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM mapped(,) AS STRING
            DIM filtered() AS STRING
            DIM calls AS INTEGER
            DIM token AS STRING = LEFT("😀", 1) + CHRW(0)
            grid(-1, 5) = "Внешний"
            grid(0, 6) = token
            grid(0, 7) = "é"
            grid(1, 6) = "😀"
            grid(1, 7) = "é"
            PRINT ARRAYEVERY(grid(0 TO 1, 6 TO 7), IsToken)
            PRINT calls
            calls = 0
            PRINT ARRAYSOME(grid(0 TO 1, 6 TO 7), IsToken)
            PRINT calls
            calls = 0
            filtered = ARRAYFILTER(grid(0 TO 1, 6 TO 7), IsToken)
            PRINT calls
            PRINT ARRAYLENGTH(filtered)
            PRINT filtered(1) = token
            calls = 0
            mapped = ARRAYMAP(grid(0 TO 1, 6 TO 7), Label)
            PRINT calls
            PRINT LBOUND(mapped, 1)
            PRINT UBOUND(mapped, 2)
            PRINT mapped(0, 6) = "[" + token + "]"
            PRINT mapped(0, 7) = "[é]"
            PRINT mapped(1, 6) = "[😀]"
            PRINT mapped(1, 7) = "[é]"
            calls = 0
            PRINT ARRAYREDUCE(grid(0 TO 1, 6 TO 7), "Начало:", AppendValue) = "Начало:" + token + "|é|😀|é|"
            PRINT calls
            mapped(0, 6) = "Замена"
            filtered(1) = "Другая замена"
            PRINT grid(0, 6) = token
            PRINT grid(-1, 5) = "Внешний"
            FUNCTION IsToken(value AS STRING) AS BOOLEAN
                calls = calls + 1
                RETURN value = token
            END FUNCTION
            FUNCTION Label(value AS STRING) AS STRING
                calls = calls + 1
                RETURN "[" + value + "]"
            END FUNCTION
            FUNCTION AppendValue(acc AS STRING, value AS STRING) AS STRING
                calls = calls + 1
                RETURN acc + value + "|"
            END FUNCTION
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "FALSE", "2", "TRUE", "1", "4", "1", "TRUE", "4", "0", "7", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "4", "TRUE", "TRUE" }, run.Output);
    }
}
