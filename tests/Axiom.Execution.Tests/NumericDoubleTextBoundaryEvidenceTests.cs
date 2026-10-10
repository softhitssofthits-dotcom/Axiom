using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class NumericDoubleTextBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var data = new (string Literal, string Text, string Sign)[]
        {
            ("0.0", "0", "1"),
            ("-0.0", "-0", "-1"),
            ("VAL(\"1.7976931348623157e308\")", "1.7976931348623157E+308", "1"),
            ("VAL(\"-1.7976931348623157e308\")", "-1.7976931348623157E+308", "-1"),
            ("VAL(\"4.9406564584124654e-324\")", "5E-324", "1"),
            ("VAL(\"-4.9406564584124654e-324\")", "-5E-324", "-1"),
            ("VAL(\"2.2250738585072014e-308\")", "2.2250738585072014E-308", "1")
        };

        foreach (var culture in new[] { "en-US", "ru-RU", "tr-TR" })
        foreach (var (literal, text, sign) in data) yield return [culture, literal, text, sign];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ExtremeDoubleTextRoundtripPreservesValueAndZeroSign(string culture, string literal, string text, string sign)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM value AS DOUBLE = {{literal}}
                DIM text AS STRING = STR(value)
                PRINT text
                PRINT ISNUMERIC(text)
                PRINT VAL(text) = value
                PRINT SETSIGN(1, VAL(text))
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(new[] { text, "TRUE", "TRUE", sign }, result.Output);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
