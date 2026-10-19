namespace Axiom.Execution.Tests;

public sealed class StringEvaluationOrderContractTests
{
    [Theory]
    [InlineData("+")]
    [InlineData("&")]
    public void AppendReadsGlobalBeforeCallingRightOperand(string operation)
    {
        Check($$"""
DIM text AS STRING = "A"
text = text {{operation}} Change()
PRINT text
FUNCTION Change() AS STRING
    text = "X"
    RETURN "B"
END FUNCTION
""", "AB");
    }

    [Theory]
    [InlineData("+")]
    [InlineData("&")]
    public void AppendReadsCapturedLocalBeforeCallingRightOperand(string operation)
    {
        Check($$"""
SUB Run()
    DIM text AS STRING = "A"
    text = text {{operation}} Change()
    PRINT text
    FUNCTION Change() AS STRING
        text = "X"
        RETURN "B"
    END FUNCTION
END SUB
Run()
""", "AB");
    }

    [Theory]
    [InlineData("+")]
    [InlineData("&")]
    public void AppendReadsByRefBeforeCallingRightOperand(string operation)
    {
        Check($$"""
DIM text AS STRING = "A"
Run(text)
PRINT text
SUB Run(BYREF value AS STRING)
    value = value {{operation}} Change(value)
END SUB
FUNCTION Change(BYREF value AS STRING) AS STRING
    value = "X"
    RETURN "B"
END FUNCTION
""", "AB");
    }

    private static void Check(string source, params string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
