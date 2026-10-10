namespace Axiom.Execution.Tests;

public sealed class NumericCoreEdgeEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("INT(2147483647.999)", "2147483647"),
            ("INT(-2147483648.001)", "MB3008"),
            ("INT(CLNG(-2147483648))", "-2147483648"),
            ("INT(CLNG(-2147483649))", "MB3008"),
            ("INT(VAL(\"-5e-324\"))", "-1"),
            ("INT(VAL(\"5e-324\"))", "0"),
            ("ABS(-9223372036854775807 - CLNG(1))", "MB3008"),
            ("ABS(-9223372036854775807)", "9223372036854775807"),
            ("ABS(-(CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591)))", "79228162514264337593543950335"),
            ("SETSIGN(1, ABS(-0.0))", "1"),
            ("ABS(VAL(\"-5e-324\")) = VAL(\"5e-324\")", "TRUE"),
            ("SQR(VAL(\"1267650600228229401496703205376\"))", "1125899906842624"),
            ("SQR(CDEC(1) / CDEC(4))", "0.5"),
            ("SETSIGN(1, SQR(-0.0))", "-1"),
            ("SQR(VAL(\"-5e-324\"))", "MB3007")
        };

        foreach (var c in cases) yield return [c.Expression, c.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NumericEdgesHaveLiteralResultsAndCatchableErrors(string expression, string expected)
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
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(new[] { expected, "Завершено", "Продолжение" }, result.Output);
    }
}
