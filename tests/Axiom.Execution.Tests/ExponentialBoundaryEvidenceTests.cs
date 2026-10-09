namespace Axiom.Execution.Tests;

public sealed class ExponentialBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("LOG2(VAL(\"5e-324\"))", "-1074"),
            ("LOG2(VAL(\"2.2250738585072014e-308\"))", "-1022"),
            ("LOG2(VAL(\"1267650600228229401496703205376\"))", "100"),
            ("LOG10(VAL(\"1e-300\"))", "-300"),
            ("LOG(VAL(\"5e-324\")) > -745 AND LOG(VAL(\"5e-324\")) < -744", "TRUE"),
            ("LOG(-0.0)", "MB3007"),
            ("LOG10(VAL(\"-5e-324\"))", "MB3007"),
            ("EXP(-1000)", "0"),
            ("SETSIGN(1, EXP(-1000))", "1"),
            ("EXP(-0.0)", "1"),
            ("POW(2, -1074) = VAL(\"5e-324\")", "TRUE"),
            // Проверяется только интервал; известное ограничение Math.Pow описано в отчёте Session 1308.
            ("POW(2, -1075) >= 0 AND POW(2, -1075) <= VAL(\"5e-324\")", "TRUE"),
            ("POW(2, -1076)", "0"),
            ("POW(2, 1024)", "MB3008"),
            ("SETSIGN(1, POW(-0.0, 3))", "-1"),
            ("SETSIGN(1, POW(-0.0, 2))", "1"),
            ("POW(-0.0, -3)", "MB3008"),
            ("POW(-2, 3)", "-8"),
            ("POW(-2, 0.25)", "MB3007")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var c in cases) yield return [mode, c.Expression, c.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ExponentialEdgesHaveLiteralOrBoundedExpectations(ExecutionMode mode, string expression, string expected)
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
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(new[] { expected, "Завершено", "Продолжение" }, result.Output);
    }
}
