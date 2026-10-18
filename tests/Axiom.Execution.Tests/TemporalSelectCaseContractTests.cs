using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// SELECT CASE для временных типов (DTA-008). Раньше оператор отвергал даже
/// DATE = DATE, хотя обычные сравнения таких значений работают. Теперь точное
/// значение, CASE IS и диапазон TO поддержаны для одного временного вида;
/// смешение видов и потеря компонента отклоняются тем же договором, что и в
/// присваивании.
/// </summary>
public sealed class TemporalSelectCaseContractTests
{
    [Fact]
    public void ExactValueMatchesEveryTemporalKind()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM clockTime AS TIME = "12:34:56"
DIM moment AS DATETIME = "2024-02-29 12:34:56"

SELECT CASE dayValue
    CASE "2024-02-29"
        PRINT "дата совпала"
END SELECT

SELECT CASE clockTime
    CASE "12:34:56"
        PRINT "время совпало"
END SELECT

SELECT CASE moment
    CASE "2024-02-29 12:34:56"
        PRINT "дата-время совпало"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["дата совпала", "время совпало", "дата-время совпало"], result.Output);
    }

    [Fact]
    public void TypedSelectorsMatchWithoutText()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM otherDay AS DATE = "2024-03-01"

SELECT CASE dayValue
    CASE otherDay
        PRINT "равны"
    CASE ELSE
        PRINT "разные"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["разные"], result.Output);
    }

    [Fact]
    public void RangeAndComparisonSelectorsUseInclusiveBounds()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM clockTime AS TIME = "12:34:56"
DIM moment AS DATETIME = "2024-02-29 12:34:56"

SELECT CASE dayValue
    CASE "2024-03-01" TO "2024-03-31"
        PRINT "март"
    CASE "2024-02-01" TO "2024-02-29"
        PRINT "февраль"
END SELECT

SELECT CASE clockTime
    CASE IS > "18:00:00"
        PRINT "вечер"
    CASE IS <= "12:34:56"
        PRINT "до полудня и в полдень"
END SELECT

SELECT CASE moment
    CASE IS < "2024-02-29 00:00:00"
        PRINT "раньше"
    CASE IS >= "2024-02-29 12:34:56"
        PRINT "не раньше момента"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["февраль", "до полудня и в полдень", "не раньше момента"], result.Output);
    }

    [Fact]
    public void ReversedRangeMatchesNothing()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-15"
SELECT CASE dayValue
    CASE "2024-03-01" TO "2024-02-01"
        PRINT "попал"
    CASE ELSE
        PRINT "не попал"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["не попал"], result.Output);
    }

    [Fact]
    public void FractionalSecondsRemainDistinctInSelectors()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
SELECT CASE moment
    CASE "2024-02-29 12:34:56.124"
        PRINT "раньше"
    CASE "2024-02-29 12:34:56.125"
        PRINT "точно"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["точно"], result.Output);
    }

    [Fact]
    public void MixingTemporalKindsIsRejected()
    {
        var programs = new[]
        {
            """
            DIM dayValue AS DATE = "2024-02-29"
            DIM clockTime AS TIME = "12:34:56"
            SELECT CASE dayValue
                CASE clockTime
                    PRINT "плохо"
            END SELECT
            """,
            """
            DIM dayValue AS DATE = "2024-02-29"
            DIM moment AS DATETIME = "2024-02-29 12:34:56"
            SELECT CASE dayValue
                CASE moment
                    PRINT "плохо"
            END SELECT
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB2003", diagnostic.Code);
            Assert.Empty(result.Output);
        }
    }

    [Fact]
    public void LossyStringSelectorIsRejected()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
SELECT CASE dayValue
    CASE "2024-02-29 12:34:56"
        PRINT "плохо"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2003", diagnostic.Code);
        Assert.Contains("truncated", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NumericSelectorsKeepPreviousConversionRules()
    {
        const string source = """
DIM score AS INTEGER = 3
SELECT CASE score
    CASE 1, 3
        PRINT "совпало"
END SELECT

DIM amount AS DOUBLE = 2.75
SELECT CASE amount
    CASE 2
        PRINT "два"
    CASE ELSE
        PRINT "не два"
END SELECT
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["совпало", "не два"], result.Output);
    }
}
