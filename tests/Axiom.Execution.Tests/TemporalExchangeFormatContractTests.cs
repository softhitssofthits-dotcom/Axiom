using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// Формат обмена (DTA-013). Строгий внутренний формат и формат обмена разделены:
/// неявное преобразование строки в DATETIME по-прежнему принимает только
/// каноническую запись, а разделитель T читается явной функцией DATETIMEVALUE.
/// Смещение и суффикс Z не принимаются нигде, потому что DATETIME не хранит
/// часовой пояс и отбрасывание смещения потеряло бы информацию о моменте.
/// </summary>
public sealed class TemporalExchangeFormatContractTests
{
    [Fact]
    public void ExchangeFormatIsAcceptedExplicitly()
    {
        const string source = """
PRINT DATETIMEVALUE("2024-02-29T12:34:56")
PRINT DATETIMEVALUE("2024-02-29T12:34:56.125")
PRINT DATETIMEVALUE("2024-02-29 12:34:56")
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(
            ["2024-02-29 12:34:56", "2024-02-29 12:34:56.125", "2024-02-29 12:34:56"],
            result.Output);
    }

    [Fact]
    public void ImplicitConversionStaysStrict()
    {
        // Неявное преобразование не принимает формат обмена: он читается только
        // явной функцией, поэтому программа получает отказ, а не тихое значение.
        const string source = """
DIM moment AS DATETIME = "2024-02-29T12:34:56"
PRINT moment
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Empty(result.Output);
    }

    [Fact]
    public void OffsetAndUtcSuffixAreRejected()
    {
        foreach (var text in new[] { "2024-02-29T12:34:56Z", "2024-02-29T12:34:56+03:00", "2024-02-29T12:34:56-05:00" })
        {
            var source = $$"""
                TRY
                    PRINT DATETIMEVALUE("{{text}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """;

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(["MB3007", "Продолжение"], result.Output);
        }
    }

    [Fact]
    public void InvalidExchangeTextIsRejected()
    {
        foreach (var text in new[] { "2024-02-29T", "2024-02-29 12:34", "29.02.2024T12:34:56", "2024-02-29T12:34:56." })
        {
            var source = $"PRINT DATETIMEVALUE(\"{text}\")";

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            Assert.Contains(result.Diagnostics, static d => d.Code == "MB3007");
        }
    }

    [Fact]
    public void ParsedValueParticipatesInArithmeticAndComparison()
    {
        const string source = """
DIM moment AS DATETIME = DATETIMEVALUE("2024-02-29T12:34:56.125")
PRINT DATEADD("s", 1, moment)
PRINT moment = DATETIMEVALUE("2024-02-29T12:34:56.125")
PRINT moment > DATETIMEVALUE("2024-02-29T12:34:56")
PRINT DATEPART("h", moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:57.125", "TRUE", "TRUE", "12"], result.Output);
    }

    [Fact]
    public void IsDateKeepsItsOwnContract()
    {
        // ISDATE проверяет только канонические формы; формат обмена читается
        // отдельной функцией, поэтому ISDATE остаётся строгой.
        const string source = """
PRINT ISDATE("2024-02-29 12:34:56")
PRINT ISDATE("2024-02-29T12:34:56")
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["TRUE", "FALSE"], result.Output);
    }
}
