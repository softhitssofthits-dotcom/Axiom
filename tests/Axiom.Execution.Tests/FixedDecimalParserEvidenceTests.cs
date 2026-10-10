namespace Axiom.Execution.Tests;

public sealed class FixedDecimalParserEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Text, string Expected)[]
        {
            ("9007199254740993", "9007199254740993"),
            ("1.0000000000000000000000000001", "1.0000000000000000000000000001"),
            ("-0.1234567890123456789012345678", "-0.1234567890123456789012345678"),
            ("79228162514264337593543950335", "79228162514264337593543950335"),
            ("-79228162514264337593543950335", "-79228162514264337593543950335"),
            ("+00012.50", "12.5"), (".125", "0.125"), ("-0", "0"),
            ("", "APP_DECIMAL"), ("+", "APP_DECIMAL"), (".", "APP_DECIMAL"),
            ("1.2.3", "APP_DECIMAL"), ("1e2", "APP_DECIMAL"),
            (" 1", "APP_DECIMAL"), ("١", "APP_DECIMAL"),
            ("79228162514264337593543950335.0", "MB3008"),
            ("0.00000000000000000000000000001", "MB3008")
        };

        foreach (var item in cases) yield return [item.Text, item.Expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void UserFunctionHasExactUsefulSubsetAndExplicitLimits(string text, string expected)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var sample = File.ReadAllText(Path.Combine(directory.FullName, "samples/functions/cdec/fixed_decimal_parser.axm"));
        var function = sample[..(sample.IndexOf("END FUNCTION", StringComparison.Ordinal) + "END FUNCTION".Length)];
        var source = function + $$"""

            TRY
                PRINT ParseFixedDecimal("{{text}}")
            CATCH err
                PRINT err.Code
            END TRY
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { expected }, result.Output);
    }
}
