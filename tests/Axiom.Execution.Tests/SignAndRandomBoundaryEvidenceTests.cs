using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class SignAndRandomBoundaryEvidenceTests
{
    public static IEnumerable<object[]> SignCases()
    {
        var values = new (string Value, string Negative, string Positive)[]
        {
            ("-2147483647 - 1", "-2147483648", "MB3008"),
            ("-9223372036854775807 - CLNG(1)", "-9223372036854775808", "MB3008"),
            ("VAL(\"-5e-324\")", "-5E-324", "5E-324"),
            ("-(CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591))", "-79228162514264337593543950335", "79228162514264337593543950335")
        };
        var signs = new (string Negative, string Positive)[] { ("-1", "0"), ("CLNG(-1)", "CLNG(0)"), ("-0.0", "0.0"), ("CDEC(-1)", "CDEC(0)") };

        foreach (var v in values)
        foreach (var s in signs)
        foreach (var negative in new[] { false, true })
            yield return [v.Value, negative ? s.Negative : s.Positive, negative ? v.Negative : v.Positive];
    }

    [Theory]
    [MemberData(nameof(SignCases))]
    public void SignSourceTypesPreserveExactMagnitudeOrReportOverflow(string value, string sign, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT SETSIGN({{value}}, {{sign}})
            CATCH err
                PRINT err.Code
            END TRY
            PRINT "Продолжение"
            """));
        Check(result, [expected, "Продолжение"]);
    }

    [Fact]
    public void RndConsumesOneHostValuePerCallWithoutRoundingEndpoints()
    {
        var random = new FixedRandom();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            PRINT RND() = 0
            PRINT RND() = VAL("5e-324")
            PRINT RND() = 0.5
            PRINT RND() = VAL("0.9999999999999999")
            """, runtimeRandom: random));
        Check(result, ["TRUE", "TRUE", "TRUE", "TRUE"]);
        Assert.Equal(4, random.Calls);
    }

    private static void Check(ExecutionResult result, string[] expected)
    {
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }

    private sealed class FixedRandom : IRuntimeRandom
    {
        private readonly Queue<double> values = new(new[] { 0d, double.Epsilon, 0.5d, 0.9999999999999999d });
        public int Calls { get; private set; }
        public double NextRandomDouble() { Calls++; return values.Dequeue(); }
        public void Reseed(int seed) => throw new InvalidOperationException("Неожиданная смена начального значения генератора.");
    }
}
