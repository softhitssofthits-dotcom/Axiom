namespace Axiom.Execution.Tests;

public sealed class HexLiteralConversionTests
{
    [Theory]
    [InlineData("expressions/hex-literals/typed_values.axm", "255\n9223372036854775807\n-2147483648\n255")]
    [InlineData("expressions/binary-literals/values_and_masks.axm", "10\n00001010\n1000\n1110\n0110\n0010")]
    [InlineData("expressions/number-bases/display_formats.axm", "42\n2A\n101010\n002A\n00101010\nFFFFFFFF\nFFFFFFFFFFFFFFFF")]
    [InlineData("expressions/bitwise/word_width.axm", "-4294967296\n-2147483648\n1\n4294967296\n-9223372036854775808\n-2\n-1")]
    [InlineData("expressions/number-bases/format_type_error.axm", "MB3007\nNumeric format 'B' is not supported for DOUBLE.")]
    public void RadixAcceptanceSamplesHaveIndependentExpectations(string file, string expected)
    {
        var source = TestProgramLoader.LoadText("samples/" + file);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected.Split('\n'), result.Output);
    }

    [Fact]
    public void InvalidDigitSampleIsRejectedBeforeExecution()
    {
        var source = TestProgramLoader.LoadText("samples/expressions/binary-literals/invalid_digit_error.axm");
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, item => item.Code == "MB0003");
    }

    [Theory]
    [InlineData("B")]
    [InlineData("X")]
    [InlineData("Q")]
    public void InvalidNumericFormatIsCatchableLanguageError(string format)
    {
        var source = "TRY\n PRINT $\"{1.5:" + format + "}\"\nCATCH problem\n PRINT problem.Code\nEND TRY";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Equal(["MB3007"], result.Output);
    }

    [Theory]
    [InlineData("DIM n AS LONG = &HFF\nPRINT n", "255")]
    [InlineData("DIM n AS DECIMAL = &HFF\nPRINT n", "255")]
    [InlineData("CONST n AS LONG = &hFF\nPRINT n", "255")]
    [InlineData("DIM n AS INTEGER = -&H80000000\nPRINT n", "-2147483648")]
    [InlineData("DIM n AS LONG = 1024\nPRINT n OR &HFF", "1279")]
    [InlineData("SUB Show(value AS LONG)\n PRINT value\nEND SUB\nShow(&HFF)", "255")]
    [InlineData("DIM n AS DECIMAL = &H7FFFFFFFFFFFFFFF\nPRINT n", "9223372036854775807")]
    [InlineData("DIM n AS INTEGER = 0\nn = -&H80000000\nPRINT n", "-2147483648")]
    [InlineData("DIM n AS DECIMAL = -&HFF\nPRINT n", "-255")]
    public void HexLiteralRespectsExpectedNumericType(string source, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal([expected], result.Output);
    }

    [Theory]
    [InlineData("DIM n AS INTEGER = &H80000000", "MB2018")]
    [InlineData("DIM n AS INTEGER = -&H80000001", "MB2018")]
    [InlineData("PRINT &H8000000000000000", "MB0004")]
    public void HexLiteralStillRejectsOutOfRangeValues(string source, string code)
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }
}
