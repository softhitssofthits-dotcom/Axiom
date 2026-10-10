using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class NumericPresentationEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Expression, string Expected)[]
        {
            ("INT(-0.5)", "-1"), ("INT(0.5)", "0"),
            ("INT(CDEC(-2147483648) - CDEC(0.1))", "MB3008"),
            ("INT(CDEC(2147483647) + CDEC(0.9))", "2147483647"),
            ("INT(CLNG(2147483648))", "MB3008"),
            ("STR(12.5)", "12.5"), ("STR(-12.5)", "-12.5"),
            ("STR(9007199254740993)", "9007199254740993"),
            ("STR(CDEC(9223372036854775807))", "9223372036854775807"),
            ("STR(CDEC(1) / CDEC(8))", "0.125"),
            ("CDEC(9223372036854775807) * CDEC(8589934592) + CDEC(8589934591)", "79228162514264337593543950335"),
            ("CDEC(VAL(\"1e29\"))", "MB3008"),
            ("CDEC(VAL(\"1e-29\"))", "0")
        };
        foreach (var culture in new[] { "en-US", "ru-RU", "tr-TR" })

        foreach (var (expression, expected) in cases) yield return [culture, expression, expected];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NumericPresentationAndBoundsAreCultureIndependent(string culture, string expression, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{expression}}
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { expected, "Продолжение" }, result.Output);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
