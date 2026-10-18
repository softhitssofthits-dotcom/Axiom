using Axiom.Execution;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

/// <summary>
/// Договор точности временных значений (DTA-001). Канонический текст обязан быть
/// обратимым: любой текст, который выдаёт значение, читается обратно в равное
/// значение. Доли секунды записываются только при ненулевой дробной части, поэтому
/// значения с точностью до секунды сохраняют краткую форму. Функции текущего
/// времени имеют документированное разрешение до секунды.
/// </summary>
public sealed class TemporalPrecisionContractTests
{
    [Fact]
    public void DateTimeTextRoundTripsFractionalSeconds()
    {
        const string source = """
DIM original AS DATETIME = "2024-02-29 12:34:56.125"
DIM text AS STRING = original
DIM restored AS DATETIME = text
PRINT text
PRINT original = restored
PRINT original = text
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:56.125", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void TimeTextRoundTripsFractionalSeconds()
    {
        const string source = """
DIM original AS TIME = "12:34:56.125"
DIM text AS STRING = original
DIM restored AS TIME = text
PRINT text
PRINT original = restored
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["12:34:56.125", "TRUE"], result.Output);
    }

    [Fact]
    public void WholeSecondsKeepBriefForm()
    {
        const string source = """
DIM exact AS DATETIME = "2024-02-29 12:34:56"
DIM exactTime AS TIME = "12:34:56"
PRINT exact
PRINT exactTime
PRINT LEN(exact) = 19
PRINT LEN(exactTime) = 8
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:56", "12:34:56", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void FractionSurvivesArithmeticAndInterpolation()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
PRINT DATEADD("s", 1, moment)
PRINT DATEDIFF("s", moment, DATEADD("s", 1, moment))
PRINT DATEPART("s", moment)
PRINT $"значение: {moment}"
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(
            ["2024-02-29 12:34:57.125", "1", "56", "значение: 2024-02-29 12:34:56.125"],
            result.Output);
    }

    [Fact]
    public void ValuesWithSameBriefPrintRemainDistinct()
    {
        const string source = """
DIM first AS DATETIME = "2024-02-29 12:34:56.100"
DIM second AS DATETIME = "2024-02-29 12:34:56.200"
PRINT first = second
PRINT first < second
PRINT first
PRINT second
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(
            ["FALSE", "TRUE", "2024-02-29 12:34:56.1", "2024-02-29 12:34:56.2"],
            result.Output);
    }

    [Fact]
    public void IsDateAcceptsFractionalSeconds()
    {
        const string source = """
PRINT ISDATE("2024-02-29 12:34:56.125")
PRINT ISDATE("12:34:56.125")
PRINT ISDATE("2024-02-29 12:34:56.")
PRINT ISDATE("2024-02-29 12:34:56.abc")
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["TRUE", "TRUE", "FALSE", "FALSE"], result.Output);
    }

    [Fact]
    public void CurrentClockFunctionsHaveSecondResolution()
    {
        const string source = """
PRINT TIME()
PRINT NOW()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT LEN(TIME()) = 8
PRINT LEN(NOW()) = 19
PRINT TIMER() > 0
""";

        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, runtimeClock: new FractionalClock()));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(
            [
                "00:30:15",
                "2024-03-01 00:30:15",
                "21:30:15",
                "2024-02-29 21:30:15",
                "TRUE",
                "TRUE",
                "TRUE"
            ],
            result.Output);
    }

    [Fact]
    public void CurrentClockValueRoundTripsThroughText()
    {
        const string source = """
DIM original AS DATETIME = NOW()
DIM text AS STRING = original
DIM restored AS DATETIME = text
PRINT original = restored
PRINT text
""";

        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, runtimeClock: new FractionalClock()));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["TRUE", "2024-03-01 00:30:15"], result.Output);
    }

    private sealed class FractionalClock : IRuntimeClock
    {
        private readonly TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone(
            "AxiomFractional",
            TimeSpan.FromHours(3),
            "AxiomFractional",
            "AxiomFractional");

        public DateTimeOffset GetLocalNow() => new DateTimeOffset(2024, 3, 1, 0, 30, 15, TimeSpan.FromHours(3)).AddMilliseconds(125.5);

        public TimeZoneInfo GetLocalTimeZone() => zone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = id.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : zone;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
                || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }
    }
}
