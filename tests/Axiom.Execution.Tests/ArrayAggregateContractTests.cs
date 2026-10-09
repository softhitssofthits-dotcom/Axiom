namespace Axiom.Execution.Tests;

public sealed class ArrayAggregateContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, "INTEGER")]
    [InlineData(ExecutionMode.Bytecode, "INTEGER")]
    [InlineData(ExecutionMode.Interpreter, "LONG")]
    [InlineData(ExecutionMode.Bytecode, "LONG")]
    [InlineData(ExecutionMode.Interpreter, "DOUBLE")]
    [InlineData(ExecutionMode.Bytecode, "DOUBLE")]
    [InlineData(ExecutionMode.Interpreter, "DECIMAL")]
    [InlineData(ExecutionMode.Bytecode, "DECIMAL")]
    public void Aggregates_SupportArithmeticAndTypeInference(ExecutionMode mode, string type)
    {
        var source = $$"""
            DIM numbers(1 TO 2) AS {{type}}
            numbers(1) = 2
            numbers(2) = 3
            DIM total AS {{type}} = ARRAYSUM(numbers)
            DIM inferred = ARRAYMAX(numbers)
            DIM smallest AS {{type}} = ARRAYMIN(numbers)
            PRINT total + smallest + inferred
            PRINT inferred + 1
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["10", "4"], result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Sum_UncaughtOverflowReportsLanguageDiagnostic(ExecutionMode mode)
    {
        var source = """
            DIM numbers(1 TO 2) AS INTEGER
            numbers(1) = 2147483647
            numbers(2) = 1
            PRINT ARRAYSUM(numbers)
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3008", diagnostic.Code);
        Assert.Contains("ARRAYSUM", diagnostic.Message);
        Assert.Contains("INTEGER", diagnostic.Message);
        Assert.Equal(source.IndexOf("ARRAYSUM", StringComparison.Ordinal), diagnostic.Location.Span.Start);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void Aggregates_ReturnAssignableScalars(ExecutionMode mode)
    {
        var source = """
            DIM numbers(1 TO 2) AS INTEGER
            numbers(1) = 2
            numbers(2) = 3
            DIM total AS INTEGER = ARRAYSUM(numbers)
            DIM smallest AS INTEGER = ARRAYMIN(numbers)
            DIM largest AS INTEGER = ARRAYMAX(numbers)
            PRINT total
            PRINT smallest
            PRINT largest
            DIM words(1 TO 2) AS STRING
            words(1) = "б"
            words(2) = "а"
            DIM first AS STRING = ARRAYMIN(words)
            DIM last AS STRING = ARRAYMAX(words)
            PRINT first
            PRINT last
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["5", "2", "3", "а", "б"], result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, "INTEGER", "2147483647", "1")]
    [InlineData(ExecutionMode.Bytecode, "INTEGER", "2147483647", "1")]
    [InlineData(ExecutionMode.Interpreter, "INTEGER", "-2147483648", "-1")]
    [InlineData(ExecutionMode.Bytecode, "INTEGER", "-2147483648", "-1")]
    [InlineData(ExecutionMode.Interpreter, "LONG", "9223372036854775807", "1")]
    [InlineData(ExecutionMode.Bytecode, "LONG", "9223372036854775807", "1")]
    [InlineData(ExecutionMode.Interpreter, "LONG", "-9223372036854775807 - 1", "-1")]
    [InlineData(ExecutionMode.Bytecode, "LONG", "-9223372036854775807 - 1", "-1")]
    [InlineData(ExecutionMode.Interpreter, "DECIMAL", "CDEC(4000000000000000000) * CDEC(10000000000)", "CDEC(4000000000000000000) * CDEC(10000000000)")]
    [InlineData(ExecutionMode.Bytecode, "DECIMAL", "CDEC(4000000000000000000) * CDEC(10000000000)", "CDEC(4000000000000000000) * CDEC(10000000000)")]
    [InlineData(ExecutionMode.Interpreter, "DECIMAL", "-CDEC(4000000000000000000) * CDEC(10000000000)", "-CDEC(4000000000000000000) * CDEC(10000000000)")]
    [InlineData(ExecutionMode.Bytecode, "DECIMAL", "-CDEC(4000000000000000000) * CDEC(10000000000)", "-CDEC(4000000000000000000) * CDEC(10000000000)")]
    public void Sum_OverflowIsCaughtAndFinallyRuns(ExecutionMode mode, string type, string first, string second)
    {
        var source = $$"""
            DIM numbers(1 TO 2) AS {{type}}
            numbers(1) = {{first}}
            numbers(2) = {{second}}
            TRY
                PRINT ARRAYSUM(numbers)
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3008", "Завершено", "Продолжение"], result.Output);
        Assert.False(result.UsedInterpreterFallback);
    }
}
