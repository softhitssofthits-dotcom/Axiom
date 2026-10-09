namespace Axiom.Execution.Tests;

public sealed class IntegralConversionEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("CINT(2.5)", "2"), ("CINT(-2.5)", "-2"),
            ("CLNG(3.5)", "3"), ("CLNG(-3.5)", "-3"),
            ("CINT(CDEC(2147483647) + CDEC(0.5))", "2147483647"),
            ("CINT(CDEC(-2147483648) - CDEC(0.5))", "-2147483648"),
            ("CINT(CDEC(2147483647) + CDEC(1))", "MB3008"),
            ("CINT(CDEC(-2147483648) - CDEC(1))", "MB3008"),
            ("CLNG(CDEC(9223372036854775807) + CDEC(0.5))", "9223372036854775807"),
            ("CLNG((-CDEC(9223372036854775807) - CDEC(1)) - CDEC(0.5))", "-9223372036854775808"),
            ("CLNG(CDEC(9223372036854775807) + CDEC(1))", "MB3008"),
            ("CLNG((-CDEC(9223372036854775807) - CDEC(1)) - CDEC(1))", "MB3008"),
            ("CLNG(9007199254740993)", "9007199254740993"),
            ("CLNG(CDBL(9007199254740993))", "9007199254740992")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var (expression, expected) in cases) yield return [mode, expression, expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TruncationAndExactBoundsHaveIndependentExpectedValues(ExecutionMode mode, string expression, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{expression}}
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { expected, "Завершено", "Продолжение" }, result.Output);
    }
}
