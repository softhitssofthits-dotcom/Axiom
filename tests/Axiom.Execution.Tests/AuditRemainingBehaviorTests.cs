namespace Axiom.Execution.Tests;

public sealed class AuditRemainingBehaviorTests
{
    [Fact]
    public void Average_PreservesTinyRemainderAfterLargeCancellation()
    {
        const string source = "DIM values[1 TO 3] AS DOUBLE\r\nvalues[1] = VAL(\"1e308\")\r\nvalues[2] = -VAL(\"1e308\")\r\nvalues[3] = VAL(\"1e-308\")\r\nPRINT ARRAYAVERAGE(values[]) = VAL(\"1e-308\") / 3\r\nvalues[2] = VAL(\"1e-308\")\r\nvalues[3] = -VAL(\"1e308\")\r\nPRINT ARRAYAVERAGE(values[]) = VAL(\"1e-308\") / 3";
        AssertProgram(source, ["TRUE", "TRUE"]);
    }

    [Fact]
    public void Average_RoundsSubnormalTiesAndMaximumFiniteValues()
    {
        const string source = "DIM values[1 TO 2] AS DOUBLE\r\nvalues[1] = VAL(\"4.9406564584124654e-324\")\r\nvalues[2] = 0\r\nPRINT ARRAYAVERAGE(values[]) = 0\r\nvalues[2] = VAL(\"9.8813129168249309e-324\")\r\nPRINT ARRAYAVERAGE(values[]) = values[2]\r\nvalues[1] = VAL(\"1.7976931348623157e308\")\r\nvalues[2] = values[1]\r\nPRINT ARRAYAVERAGE(values[]) = values[1]\r\nvalues[1] = -values[1]\r\nvalues[2] = values[1]\r\nPRINT ARRAYAVERAGE(values[]) = values[1]";
        AssertProgram(source, ["TRUE", "TRUE", "TRUE", "TRUE"]);
    }
    [Fact]
    public void Average_LargeFiniteValuesDoNotOverflow()
    {
        const string source = "DIM values[1 TO 2] AS DOUBLE\r\nvalues[1] = VAL(\"1e308\")\r\nvalues[2] = VAL(\"1e308\")\r\nPRINT ARRAYAVERAGE(values[]) = VAL(\"1e308\")\r\nvalues[2] = -values[1]\r\nPRINT ARRAYAVERAGE(values[])";
        AssertProgram(source, ["TRUE", "0"]);
    }

    [Fact]
    public void Aggregates_AcceptMultidimensionalArrays()
    {
        const string source = "DIM values[1 TO 2, 1 TO 2] AS INTEGER\r\nvalues[1, 1] = 1\r\nvalues[1, 2] = 2\r\nvalues[2, 1] = 3\r\nvalues[2, 2] = 4\r\nPRINT ARRAYSUM(values[])\r\nPRINT ARRAYMIN(values[])\r\nPRINT ARRAYMAX(values[])\r\nPRINT ARRAYAVERAGE(values[])";
        AssertProgram(source, ["10", "1", "4", "2.5"]);
    }

    [Fact]
    public void MinMax_RejectBooleanSingletons()
    {
        foreach (var functionName in new[] { "ARRAYMIN", "ARRAYMAX" })
        {
            var source = $"""
                DIM flags[1 TO 1] AS BOOLEAN
                flags[1] = TRUE
                PRINT {functionName}(flags[])
                """;

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB2003", diagnostic.Code);
            Assert.Contains(functionName, diagnostic.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Reverse_PreservesIsolatedSurrogateAndFullEmoji()
    {
        const string source = """
            DIM half AS STRING = LEFT("😀", 1)
            PRINT REVERSE(half) = half
            PRINT REVERSE(REVERSE(half + "А")) = half + "А"
            PRINT REVERSE("А😀Б")
            """;
        AssertProgram(source, ["TRUE", "TRUE", "Б😀А"]);
    }

    private static void AssertProgram(string source, string[] output)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(output, result.Output);
    }
}
