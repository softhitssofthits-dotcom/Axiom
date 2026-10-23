namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateContractTests
{
    [Theory]
    [InlineData("INTEGER")]
    [InlineData("LONG")]
    [InlineData("DOUBLE")]
    [InlineData("DECIMAL")]
    public void Aggregates_SupportArithmeticAndTypeInference(string type)
    {
        var source = $$"""
            DIM numbers[1 TO 2] AS {{type}}
            numbers[1] = 2
            numbers[2] = 3
            DIM total AS {{type}} = ARRAYSUM(numbers[])
            DIM inferred = ARRAYMAX(numbers[])
            DIM smallest AS {{type}} = ARRAYMIN(numbers[])
            PRINT total + smallest + inferred
            PRINT inferred + 1
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["10", "4"], result.Output);
    }

    [Fact]
    public void Sum_UncaughtOverflowReportsLanguageDiagnostic()
    {
        var source = "DIM numbers[1 TO 2] AS INTEGER\r\nnumbers[1] = 2147483647\r\nnumbers[2] = 1\r\nPRINT ARRAYSUM(numbers[])";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3008", diagnostic.Code);
        Assert.Contains("ARRAYSUM", diagnostic.Message);
        Assert.Contains("INTEGER", diagnostic.Message);
        Assert.Equal(source.IndexOf("ARRAYSUM", StringComparison.Ordinal), diagnostic.Location.Span.Start);
    }

    [Fact]
    public void Aggregates_ReturnAssignableScalars()
    {
        var source = "DIM numbers[1 TO 2] AS INTEGER\r\nnumbers[1] = 2\r\nnumbers[2] = 3\r\nDIM total AS INTEGER = ARRAYSUM(numbers[])\r\nDIM smallest AS INTEGER = ARRAYMIN(numbers[])\r\nDIM largest AS INTEGER = ARRAYMAX(numbers[])\r\nPRINT total\r\nPRINT smallest\r\nPRINT largest\r\nDIM words[1 TO 2] AS STRING\r\nwords[1] = \"б\"\r\nwords[2] = \"а\"\r\nDIM first AS STRING = ARRAYMIN(words[])\r\nDIM last AS STRING = ARRAYMAX(words[])\r\nPRINT first\r\nPRINT last";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["5", "2", "3", "а", "б"], result.Output);
    }

    [Theory]
    [InlineData("INTEGER", "2147483647", "1")]
    [InlineData("INTEGER", "-2147483648", "-1")]
    [InlineData("LONG", "9223372036854775807", "1")]
    [InlineData("LONG", "-9223372036854775807 - 1", "-1")]
    [InlineData("DECIMAL", "CDEC(4000000000000000000) * CDEC(10000000000)", "CDEC(4000000000000000000) * CDEC(10000000000)")]
    [InlineData("DECIMAL", "-CDEC(4000000000000000000) * CDEC(10000000000)", "-CDEC(4000000000000000000) * CDEC(10000000000)")]
    public void Sum_OverflowIsCaughtAndFinallyRuns(string type, string first, string second)
    {
        var source = $$"""
            DIM numbers[1 TO 2] AS {{type}}
            numbers[1] = {{first}}
            numbers[2] = {{second}}
            TRY
                PRINT ARRAYSUM(numbers[])
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3008", "Завершено", "Продолжение"], result.Output);
    }
}
