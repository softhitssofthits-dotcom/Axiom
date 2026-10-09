namespace Axiom.Execution.Tests;

public sealed class NumericIndependentContractTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void SignedZeroConstantsRemainDistinctInEitherOrder(ExecutionMode mode, bool negativeFirst)
    {
        var source = negativeFirst
            ? "DIM negative AS DOUBLE = -0.0\nDIM positive AS DOUBLE = 0.0\n"
            : "DIM positive AS DOUBLE = 0.0\nDIM negative AS DOUBLE = -0.0\n";
        source += "PRINT SETSIGN(3, positive)\nPRINT SETSIGN(3, negative)\nPRINT ATN2(negative, -1) < 0\nPRINT ATN2(positive, -1) > 0\nPRINT SETSIGN(3, -negative)";
        Check(mode, source, ["3", "-3", "TRUE", "TRUE", "3"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExactIntegersAndRoundingBoundariesArePreserved(ExecutionMode mode)
    {
        Check(mode, """
DIM large AS LONG = 9007199254740993
DIM minimum AS INTEGER = -2147483647 - 1
PRINT MIN(large, large + CLNG(1)) = large
PRINT MAX(large, large - CLNG(1)) = large
PRINT CLAMP(large, large - CLNG(1), large + CLNG(1)) = large
PRINT FLOOR(large) = large
PRINT CEIL(large) = large
PRINT ROUND(large) = large
PRINT FIX(large) = large
PRINT SETSIGN(minimum, -1) = minimum
PRINT SGN(minimum)
PRINT ROUND(2.5)
PRINT ROUND(-2.5)
PRINT ROUND(CDEC(2.5))
PRINT ROUND(CDEC(-2.5))
PRINT FLOOR(-2.5)
PRINT CEIL(-2.5)
PRINT FIX(-2.5)
PRINT FLOOR(CDEC(-2.5))
PRINT CEIL(CDEC(-2.5))
PRINT FIX(CDEC(-2.5))
PRINT POW(0, 0)
PRINT POW(0, 2)
PRINT CLAMP(9, 4, 4)
PRINT SETSIGN(3, -0.0)
""", ["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "-1", "3", "-3", "3", "-3", "-3", "-2", "-2", "-3", "-2", "-2", "1", "0", "4", "-3"]);
    }

    [Theory]
    [InlineData("SQR(-1)", "MB3007")]
    [InlineData("LOG(0)", "MB3007")]
    [InlineData("LOG10(-1)", "MB3007")]
    [InlineData("LOG2(0)", "MB3007")]
    [InlineData("ASIN(2)", "MB3007")]
    [InlineData("ACOS(-2)", "MB3007")]
    [InlineData("ACOSH(0)", "MB3007")]
    [InlineData("ATANH(1)", "MB3007")]
    [InlineData("ATANH(-1)", "MB3007")]
    [InlineData("POW(-1, 0.5)", "MB3007")]
    [InlineData("CLAMP(1, 4, 2)", "MB3007")]
    [InlineData("ABS(-2147483647 - 1)", "MB3008")]
    [InlineData("SETSIGN(-2147483647 - 1, 1)", "MB3008")]
    [InlineData("EXP(1000)", "MB3008")]
    [InlineData("POW(10, 1000)", "MB3008")]
    [InlineData("POW(0, -1)", "MB3008")]
    [InlineData("SINH(1000)", "MB3008")]
    [InlineData("COSH(1000)", "MB3008")]
    public void InvalidDomainsAndOverflowMatchAndCanBeCaught(string call, string code)
    {
        var source = "PRINT " + call;
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal(code, Assert.Single(first.Diagnostics).Code);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
            Check(mode, "TRY\n PRINT " + call + "\nCATCH err\n PRINT err.Code\nEND TRY\nPRINT \"Продолжение\"", [code, "Продолжение"]);
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(expected, result.Output);
    }
}
