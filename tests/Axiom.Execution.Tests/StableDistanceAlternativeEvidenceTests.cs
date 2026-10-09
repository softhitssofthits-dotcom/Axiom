using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class StableDistanceAlternativeEvidenceTests
{
    private const string Function = """
FUNCTION StableDistance(x AS DOUBLE, y AS DOUBLE) AS DOUBLE
    DIM scale AS DOUBLE = MAX(ABS(x), ABS(y))
    IF scale = 0 THEN
        RETURN 0
    END IF
    DIM scaledX AS DOUBLE = x / scale
    DIM scaledY AS DOUBLE = y / scale
    RETURN scale * SQR(scaledX * scaledX + scaledY * scaledY)
END FUNCTION

""";

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("3", "4", "5")]
    [InlineData("-3", "-4", "5")]
    [InlineData("3e200", "4e200", "5e200")]
    [InlineData("3e-200", "4e-200", "5e-200")]
    [InlineData("1e308", "1e308", "1.4142135623730951e308")]
    [InlineData("1e308", "1e-308", "1e308")]
    [InlineData("1.7976931348623157e308", "0", "1.7976931348623157e308")]
    [InlineData("4.9406564584124654e-324", "0", "4.9406564584124654e-324")]
    [InlineData("1.4821969375237396e-323", "1.9762625833649862e-323", "2.4703282292062327e-323")]
    public void ExistingFunctionsCanComputeFiniteDistanceWithoutIntermediateOverflow(string x, string y, string expectedText)
    {
        var expected = double.Parse(expectedText, CultureInfo.InvariantCulture);
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = Run(Function + $"\nPRINT StableDistance(VAL(\"{x}\"), VAL(\"{y}\"))", mode);
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            var actual = double.Parse(Assert.Single(run.Output), CultureInfo.InvariantCulture);
            Assert.True(double.IsFinite(actual));
            Assert.True(actual >= 0);
            var ulps = Math.Abs(BitConverter.DoubleToInt64Bits(actual) - BitConverter.DoubleToInt64Bits(expected));
            Assert.True(ulps <= 4, $"{mode}: {actual:R}, expected {expected:R}, ULP={ulps}");
        }
    }

    [Fact]
    public void NaiveFormulaLosesSmallNonzeroDistance()
    {
        const string source = """
DIM x AS DOUBLE = VAL("3e-200")
DIM y AS DOUBLE = VAL("4e-200")
PRINT SQR(x*x + y*y)
""";
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = Run(source, mode);
            Assert.True(run.Succeeded);
            Assert.Equal("0", Assert.Single(run.Output));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverflowDistinguishesIntermediateProductFromUnrepresentableDistance(bool useScaledFunction)
    {
        var source = useScaledFunction
            ? Function + "\nPRINT StableDistance(VAL(\"1.7e308\"), VAL(\"1.7e308\"))"
            : "DIM x AS DOUBLE = VAL(\"3e200\")\nDIM y AS DOUBLE = VAL(\"4e200\")\nPRINT SQR(x*x + y*y)";
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = Run(source, mode);
            Assert.False(run.Succeeded);
            Assert.Contains(run.Diagnostics, d => d.Code == "MB3008");
        }
    }

    private static ExecutionResult Run(string source, ExecutionMode mode)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.False(run.UsedInterpreterFallback);
        return run;
    }
}
