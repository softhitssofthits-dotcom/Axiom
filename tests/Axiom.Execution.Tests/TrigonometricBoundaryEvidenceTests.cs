namespace Axiom.Execution.Tests;

public sealed class TrigonometricBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("SETSIGN(1, SIN(-0.0))", "-1"),
            ("SETSIGN(1, TAN(-0.0))", "-1"),
            ("SETSIGN(1, ATN(-0.0))", "-1"),
            ("SETSIGN(1, ASIN(-0.0))", "-1"),
            ("SETSIGN(1, CBRT(-0.0))", "-1"),
            ("COS(-0.0)", "1"),
            ("ACOS(1)", "0"),
            ("CBRT(-1)", "-1"),
            ("CBRT(1)", "1"),
            ("ATN2(-0.0, -1) > -3.142 AND ATN2(-0.0, -1) < -3.141", "TRUE"),
            ("ATN2(0.0, -1) > 3.141 AND ATN2(0.0, -1) < 3.142", "TRUE"),
            ("SETSIGN(1, ATN2(-0.0, 1))", "-1"),
            ("ASIN(1) > 1.57 AND ASIN(1) < 1.58", "TRUE"),
            ("ASIN(-1) > -1.58 AND ASIN(-1) < -1.57", "TRUE"),
            ("ACOS(-1) > 3.14 AND ACOS(-1) < 3.15", "TRUE"),
            ("ASIN(1.0000000000000002)", "MB3007"),
            ("ASIN(-1.0000000000000002)", "MB3007"),
            ("ACOS(1.0000000000000002)", "MB3007"),
            ("ACOS(-1.0000000000000002)", "MB3007"),
            ("CBRT(VAL(\"5e-324\")) > VAL(\"1e-108\") AND CBRT(VAL(\"5e-324\")) < VAL(\"2e-108\")", "TRUE"),
            ("CBRT(VAL(\"-5e-324\")) > VAL(\"-2e-108\") AND CBRT(VAL(\"-5e-324\")) < VAL(\"-1e-108\")", "TRUE"),
            ("CBRT(VAL(\"1.7976931348623157e308\")) > VAL(\"5e102\") AND CBRT(VAL(\"1.7976931348623157e308\")) < VAL(\"6e102\")", "TRUE")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var c in cases) yield return [mode, c.Expression, c.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TrigonometricEdgesHaveLiteralSignsDomainsAndBounds(ExecutionMode mode, string expression, string expected)
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
