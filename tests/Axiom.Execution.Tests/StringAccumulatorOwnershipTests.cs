namespace Axiom.Execution.Tests;

public sealed class StringAccumulatorOwnershipTests
{
    [Theory]
    [InlineData("+")]
    [InlineData("&")]
    public void EvaluatedOperandKeepsItsValueWhenLaterCallAppendsToSource(string operation)
    {
        Check($$"""
DIM text AS STRING = "A"
text = text + "B"
DIM result AS STRING = text {{operation}} Change()
PRINT result
PRINT text
FUNCTION Change() AS STRING
    text = text + "C"
    RETURN "D"
END FUNCTION
""", "ABD", "ABC");
    }

    [Fact]
    public void EarlierArgumentKeepsValueWhileLaterArgumentChangesCapturedSource()
    {
        Check("""
SUB Run()
    DIM text AS STRING = "A"
    text = text + "B"
    Show(text, Change())
    PRINT text
    FUNCTION Change() AS STRING
        text = text + "C"
        RETURN "D"
    END FUNCTION
END SUB
SUB Show(first AS STRING, second AS STRING)
    PRINT first + second
END SUB
Run()
""", "ABD", "ABC");
    }

    [Fact]
    public void ComparisonAndStoredCopiesKeepValuesBeforeMutation()
    {
        Check("""
DIM text AS STRING = "A"
text = text + "B"
DIM saved(1 TO 1) AS STRING
saved(1) = text
PRINT text = Change()
PRINT saved(1)
PRINT text
FUNCTION Change() AS STRING
    text = text + "C"
    RETURN "AB"
END FUNCTION
""", "TRUE", "AB", "ABC");
    }

    private static void Check(string source, params string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
