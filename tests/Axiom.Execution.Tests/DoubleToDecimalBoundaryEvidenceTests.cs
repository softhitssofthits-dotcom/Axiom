namespace Axiom.Execution.Tests;

public sealed class DoubleToDecimalBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Text, string Expected)[]
        {
            ("0", "0"), ("-0", "0"), ("0.125", "0.125"), ("-0.125", "-0.125"),
            ("1e-28", "0.0000000000000000000000000001"),
            ("-1e-28", "-0.0000000000000000000000000001"),
            ("5e-324", "0"), ("-5e-324", "0"),
            ("7.9e28", "79000000000000000000000000000"),
            ("-7.9e28", "-79000000000000000000000000000"),
            ("79228162514264337593543950336", "MB3008"),
            ("-79228162514264337593543950336", "MB3008"),
            ("1.7976931348623157e308", "MB3008"),
            ("-1.7976931348623157e308", "MB3008")
        };

        foreach (var c in cases) yield return [c.Text, c.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DoubleConversionHasLiteralRangeAndPrecisionEvidence(string text, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            DIM value AS DOUBLE = VAL("{{text}}")
            TRY
                PRINT CDEC(value)
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

    [Fact]
    public void UncaughtOverflowPointsToCdecCall()
    {
        const string source = "PRINT CDEC(VAL(\"79228162514264337593543950336\"))";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Empty(result.Output);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3008", diagnostic.Code);
        Assert.Equal(source.IndexOf("CDEC", StringComparison.Ordinal), diagnostic.Location.Span.Start);
    }
}
