using Axiom.Execution;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

/// <summary>
/// Тип момента UTCDATETIME (DTA-005). Значение означает однозначную точку на
/// шкале UTC, поэтому два разных момента не склеиваются даже тогда, когда их
/// местная запись совпадает при переходе часов. Смешение с календарным DATETIME
/// отвергается при проверке программы.
/// </summary>
public sealed class InstantTypeContractTests
{
    [Fact]
    public void DistinctInstantsStayDistinctAcrossClockTransition()
    {
        // Сценарий аудита: в Нью-Йорке 3 ноября 2024 года час 01:00-02:00
        // повторяется, поэтому два разных момента дают одинаковую местную запись.
        const string source = """
DIM first AS UTCDATETIME = "2024-11-03T05:30:00Z"
DIM second AS UTCDATETIME = "2024-11-03T06:30:00Z"
PRINT first = second
PRINT first < second
PRINT FROMINSTANT(first, "America/New_York")
PRINT FROMINSTANT(second, "America/New_York")
PRINT first
PRINT second
""";

        var result = Run(source);

        Assert.Equal(
            [
                "FALSE",
                "TRUE",
                "2024-11-03 01:30:00",
                "2024-11-03 01:30:00",
                "2024-11-03T05:30:00Z",
                "2024-11-03T06:30:00Z"
            ],
            result.Output);
    }

    [Fact]
    public void InstantRoundTripsThroughText()
    {
        const string source = """
DIM moment AS UTCDATETIME = "2024-02-29T12:34:56.125Z"
DIM text AS STRING = moment
DIM restored AS UTCDATETIME = text
PRINT text
PRINT restored = moment
PRINT $"момент {moment}"
""";

        var result = Run(source);

        Assert.Equal(
            ["2024-02-29T12:34:56.125Z", "TRUE", "момент 2024-02-29T12:34:56.125Z"],
            result.Output);
    }

    [Fact]
    public void WholeSecondsPrintWithoutFraction()
    {
        const string source = """
DIM moment AS UTCDATETIME = "2024-02-29T12:34:56Z"
PRINT moment
PRINT LEN(moment)
""";

        var result = Run(source);

        Assert.Equal(["2024-02-29T12:34:56Z", "20"], result.Output);
    }

    [Fact]
    public void MixingInstantWithCalendarKindsIsRejected()
    {
        var programs = new[]
        {
            """
            DIM moment AS UTCDATETIME = "2024-02-29T12:34:56Z"
            DIM localValue AS DATETIME = "2024-02-29 12:34:56"
            PRINT moment = localValue
            """,
            """
            DIM moment AS UTCDATETIME = "2024-02-29T12:34:56Z"
            DIM dayValue AS DATE = moment
            """,
            """
            DIM localValue AS DATETIME = "2024-02-29 12:34:56"
            DIM moment AS UTCDATETIME = localValue
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            Assert.Contains(result.Diagnostics, static d => d.Code == "MB2003");
            Assert.Empty(result.Output);
        }
    }

    [Fact]
    public void OffsetTextIsRejected()
    {
        foreach (var text in new[] { "2024-02-29T12:34:56+03:00", "2024-02-29 12:34:56", "2024-02-29T12:34:56" })
        {
            var source = $$"""
                TRY
                    PRINT INSTANTVALUE("{{text}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """;

            var result = Run(source);

            Assert.Equal(["MB3007", "Продолжение"], result.Output);
        }
    }

    [Fact]
    public void ConversionBetweenInstantAndCalendarUsesZone()
    {
        const string source = """
DIM localValue AS DATETIME = "2024-02-29 15:34:56.125"
DIM moment AS UTCDATETIME = TOINSTANT(localValue, "Europe/Moscow")
PRINT moment
PRINT FROMINSTANT(moment, "Europe/Moscow")
PRINT TOINSTANT(FROMINSTANT(moment, "Europe/Moscow"), "Europe/Moscow") = moment
""";

        var result = Run(source);

        Assert.Equal(
            [
                "2024-02-29T12:34:56.125Z",
                "2024-02-29 15:34:56.125",
                "TRUE"
            ],
            result.Output);
    }

    [Fact]
    public void AmbiguousLocalTimeIsRejectedByConversion()
    {
        const string source = """
DIM ambiguous AS DATETIME = "2024-11-03 01:30:00"
TRY
    PRINT TOINSTANT(ambiguous, "America/New_York")
CATCH err
    PRINT err.Code
END TRY
PRINT "Продолжение"
""";

        var result = Run(source);

        Assert.Equal(["MB3007", "Продолжение"], result.Output);
    }

    [Fact]
    public void InstantWorksInRecordsArraysAndSelectCase()
    {
        const string source = """
TYPE Event
    Sent AS UTCDATETIME = "2024-02-29T12:34:56.125Z"
    Label AS STRING = "событие"
END TYPE

DIM item AS Event
PRINT item.Sent
PRINT item.Label

DIM list(1 TO 2) AS UTCDATETIME
list(1) = item.Sent
list(2) = "2024-02-29T10:00:00Z"
PRINT ARRAYMIN(list)
PRINT ARRAYMAX(list)

SELECT CASE list(1)
    CASE IS > list(2)
        PRINT "позже"
    CASE ELSE
        PRINT "не позже"
END SELECT
""";

        var result = Run(source);

        Assert.Equal(
            [
                "2024-02-29T12:34:56.125Z",
                "событие",
                "2024-02-29T10:00:00Z",
                "2024-02-29T12:34:56.125Z",
                "позже"
            ],
            result.Output);
    }

    [Fact]
    public void InstantInputUsesCanonicalForm()
    {
        const string source = """
DIM moment AS UTCDATETIME = "2024-01-01T00:00:00Z"
TRY
    INPUT moment
CATCH err
    PRINT err.Code
END TRY
PRINT moment
""";

        var rejected = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, inputProvider: new QueueInputProvider("2024-02-29T12:34:56+03:00")));

        Assert.Equal(["MB3003", "2024-01-01T00:00:00Z"], rejected.Output);

        const string accepted = """
DIM moment AS UTCDATETIME
INPUT moment
PRINT moment
""";

        var ok = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(accepted, inputProvider: new QueueInputProvider("2024-02-29T12:34:56.125Z")));

        Assert.Equal(["2024-02-29T12:34:56.125Z"], ok.Output);
    }

    [Fact]
    public void NowInstantIsUtcAndSecondPrecise()
    {
        const string source = """
PRINT NOWINSTANT()
PRINT LEN(NOWINSTANT()) = 20
""";

        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, runtimeClock: new FixedClock()));

        // Местное 00:30:15 при смещении +03:00 — это 21:30:15 UTC предыдущего дня.
        Assert.Equal(["2024-02-29T21:30:15Z", "TRUE"], result.Output);
    }

    [Fact]
    public void DefaultValueIsMinimumInstant()
    {
        const string source = """
DIM moment AS UTCDATETIME
PRINT moment
""";

        var result = Run(source);

        Assert.Equal(["0001-01-01T00:00:00Z"], result.Output);
    }

    private sealed class FixedClock : IRuntimeClock
    {
        private readonly TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone(
            "AxiomFixed", TimeSpan.FromHours(3), "AxiomFixed", "AxiomFixed");

        public DateTimeOffset GetLocalNow() => new DateTimeOffset(2024, 3, 1, 0, 30, 15, TimeSpan.FromHours(3)).AddMilliseconds(125.5);

        public TimeZoneInfo GetLocalTimeZone() => zone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = id.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : zone;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
                || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class QueueInputProvider : IInputProvider
    {
        private readonly Queue<string> _values;

        public QueueInputProvider(params string[] values) => _values = new Queue<string>(values);

        public string ReadInput(InputRequest request) => _values.Count > 0 ? _values.Dequeue() : string.Empty;
    }

    private static ExecutionResult Run(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        return result;
    }
}
