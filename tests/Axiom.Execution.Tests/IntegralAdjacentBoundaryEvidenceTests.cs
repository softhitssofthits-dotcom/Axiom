namespace Axiom.Execution.Tests;

public sealed class IntegralAdjacentBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Call, string Expected)[]
        {
            ("CLNG(9223372036854774784.0)", "9223372036854774784"),
            ("CLNG(9223372036854775808.0)", "MB3008"),
            ("CLNG(9223372036854777856.0)", "MB3008"),
            ("CLNG(-9223372036854774784.0)", "-9223372036854774784"),
            ("CLNG(-9223372036854775808.0)", "-9223372036854775808"),
            ("CLNG(-9223372036854777856.0)", "MB3008"),
            ("CINT(2147483647.9999998)", "2147483647"),
            ("CINT(2147483648.0)", "MB3008"),
            ("CINT(-2147483648.9999995)", "-2147483648"),
            ("CINT(-2147483649.0)", "MB3008"),
            ("CINT(CDEC(2147483647) + CDEC(99) / CDEC(100))", "2147483647"),
            ("CINT(CDEC(-2147483648) - CDEC(99) / CDEC(100))", "-2147483648"),
            ("CLNG(CDEC(9223372036854775807) + CDEC(99) / CDEC(100))", "9223372036854775807"),
            ("CLNG((-CDEC(9223372036854775807) - CDEC(1)) - CDEC(99) / CDEC(100))", "-9223372036854775808"),
            ("CINT(CDEC(-1) / CDEC(2))", "0"),
            ("CLNG(CDEC(-1) / CDEC(2))", "0"),
            ("CINT(CDEC(1) / CDEC(2))", "0"),
            ("CLNG(CDEC(1) / CDEC(2))", "0")
        };

        foreach (var (call, expected) in cases) yield return [call, expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AdjacentRepresentableValuesAndDecimalFractionsRespectTruncation(string call, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{call}}
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(new[] { expected, "Завершено", "Продолжение" }, result.Output);
    }
}
