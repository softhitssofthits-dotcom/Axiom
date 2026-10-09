namespace Axiom.Execution.Tests;

public sealed class HyperbolicBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("SETSIGN(1, SINH(-0.0))", "-1"),
            ("SETSIGN(1, TANH(-0.0))", "-1"),
            ("SETSIGN(1, ASINH(-0.0))", "-1"),
            ("SETSIGN(1, ATANH(-0.0))", "-1"),
            ("COSH(-0.0)", "1"),
            ("ACOSH(1)", "0"),
            ("SINH(VAL(\"5e-324\")) = VAL(\"5e-324\")", "TRUE"),
            ("TANH(VAL(\"-5e-324\")) = VAL(\"-5e-324\")", "TRUE"),
            ("ASINH(VAL(\"5e-324\")) = VAL(\"5e-324\")", "TRUE"),
            ("ATANH(VAL(\"-5e-324\")) = VAL(\"-5e-324\")", "TRUE"),
            ("COSH(VAL(\"5e-324\"))", "1"),
            ("TANH(VAL(\"1.7976931348623157e308\"))", "1"),
            ("TANH(VAL(\"-1.7976931348623157e308\"))", "-1"),
            ("ASINH(VAL(\"1.7976931348623157e308\")) > 710 AND ASINH(VAL(\"1.7976931348623157e308\")) < 711", "TRUE"),
            ("ASINH(VAL(\"-1.7976931348623157e308\")) > -711 AND ASINH(VAL(\"-1.7976931348623157e308\")) < -710", "TRUE"),
            ("ACOSH(VAL(\"1.7976931348623157e308\")) > 710 AND ACOSH(VAL(\"1.7976931348623157e308\")) < 711", "TRUE"),
            ("ACOSH(0.9999999999999999)", "MB3007"),
            ("ACOSH(1.0000000000000002) > VAL(\"2e-8\") AND ACOSH(1.0000000000000002) < VAL(\"3e-8\")", "TRUE"),
            ("ATANH(0.9999999999999999) > 18 AND ATANH(0.9999999999999999) < 19", "TRUE"),
            ("ATANH(-0.9999999999999999) > -19 AND ATANH(-0.9999999999999999) < -18", "TRUE"),
            ("ATANH(1.0000000000000002)", "MB3007"),
            ("ATANH(-1.0000000000000002)", "MB3007"),
            ("SINH(-1000)", "MB3008"),
            ("COSH(-1000)", "MB3008")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var c in cases) yield return [mode, c.Expression, c.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HyperbolicEdgesPreserveSignsDomainsAndFiniteInverseResults(ExecutionMode mode, string expression, string expected)
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
