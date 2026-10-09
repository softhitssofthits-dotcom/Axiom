namespace Axiom.Execution.Tests;

public sealed class AuditRemainingBehaviorTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Average_PreservesTinyRemainderAfterLargeCancellation(ExecutionMode mode)
    {
        const string source = """
            DIM values(1 TO 3) AS DOUBLE
            values(1) = VAL("1e308")
            values(2) = -VAL("1e308")
            values(3) = VAL("1e-308")
            PRINT ARRAYAVERAGE(values) = VAL("1e-308") / 3
            values(2) = VAL("1e-308")
            values(3) = -VAL("1e308")
            PRINT ARRAYAVERAGE(values) = VAL("1e-308") / 3
            """;
        AssertProgram(source, mode, ["TRUE", "TRUE"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Average_RoundsSubnormalTiesAndMaximumFiniteValues(ExecutionMode mode)
    {
        const string source = """
            DIM values(1 TO 2) AS DOUBLE
            values(1) = VAL("4.9406564584124654e-324")
            values(2) = 0
            PRINT ARRAYAVERAGE(values) = 0
            values(2) = VAL("9.8813129168249309e-324")
            PRINT ARRAYAVERAGE(values) = values(2)
            values(1) = VAL("1.7976931348623157e308")
            values(2) = values(1)
            PRINT ARRAYAVERAGE(values) = values(1)
            values(1) = -values(1)
            values(2) = values(1)
            PRINT ARRAYAVERAGE(values) = values(1)
            """;
        AssertProgram(source, mode, ["TRUE", "TRUE", "TRUE", "TRUE"]);
    }
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Average_LargeFiniteValuesDoNotOverflow(ExecutionMode mode)
    {
        const string source = """
            DIM values(1 TO 2) AS DOUBLE
            values(1) = VAL("1e308")
            values(2) = VAL("1e308")
            PRINT ARRAYAVERAGE(values) = VAL("1e308")
            values(2) = -values(1)
            PRINT ARRAYAVERAGE(values)
            """;
        AssertProgram(source, mode, ["TRUE", "0"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Aggregates_AcceptMultidimensionalArrays(ExecutionMode mode)
    {
        const string source = """
            DIM values(1 TO 2, 1 TO 2) AS INTEGER
            values(1, 1) = 1
            values(1, 2) = 2
            values(2, 1) = 3
            values(2, 2) = 4
            PRINT ARRAYSUM(values)
            PRINT ARRAYMIN(values)
            PRINT ARRAYMAX(values)
            PRINT ARRAYAVERAGE(values)
            """;
        AssertProgram(source, mode, ["10", "1", "4", "2.5"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MinMax_RejectBooleanSingletons(ExecutionMode mode)
    {
        const string source = """
            DIM flags(1 TO 1) AS BOOLEAN
            flags(1) = TRUE
            TRY
                PRINT ARRAYMIN(flags)
            CATCH err
                PRINT err.Code
            END TRY
            TRY
                PRINT ARRAYMAX(flags)
            CATCH err
                PRINT err.Code
            END TRY
            """;
        AssertProgram(source, mode, ["MB3007", "MB3007"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Reverse_PreservesIsolatedSurrogateAndFullEmoji(ExecutionMode mode)
    {
        const string source = """
            DIM half AS STRING = LEFT("😀", 1)
            PRINT REVERSE(half) = half
            PRINT REVERSE(REVERSE(half + "А")) = half + "А"
            PRINT REVERSE("А😀Б")
            """;
        AssertProgram(source, mode, ["TRUE", "TRUE", "Б😀А"]);
    }

    private static void AssertProgram(string source, ExecutionMode mode, string[] output)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(output, result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }
}
