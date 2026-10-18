using Axiom.Execution;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

/// <summary>
/// Длительность и монотонное измерение (DTA-006). Отдельного типа длительности нет
/// по решению автора: прошедшее время выражается числом секунд. Форма без
/// аргументов читает монотонный счётчик, поэтому перевод системных часов и полночь
/// на результат не влияют. Форма с двумя моментами даёт знаковую разность.
/// </summary>
public sealed class ElapsedSecondsContractTests
{
    [Fact]
    public void CounterMeasuresElapsedTime()
    {
        const string source = """
DIM start AS DOUBLE = ELAPSEDSECONDS()
SLEEP 150
DIM spent AS DOUBLE = ELAPSEDSECONDS() - start
PRINT spent >= 0.1
PRINT spent < 5
""";

        var result = Run(source, new SteppingClock());

        Assert.Equal(["TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void CounterIgnoresCalendarClockChanges()
    {
        // Календарные часы сначала идут назад, затем прыгают вперёд: монотонный
        // счётчик обязан расти ровно, поэтому разность остаётся положительной.
        const string source = """
DIM start AS DOUBLE = ELAPSEDSECONDS()
SLEEP 20
DIM middle AS DOUBLE = ELAPSEDSECONDS()
SLEEP 20
DIM finish AS DOUBLE = ELAPSEDSECONDS()
PRINT middle >= start
PRINT finish >= middle
PRINT finish - start >= 0
""";

        var result = Run(source, new JumpingClock());

        Assert.Equal(["TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void BetweenInstantsReturnsSignedSeconds()
    {
        const string source = """
DIM first AS UTCDATETIME = "2024-02-29T12:00:00Z"
DIM second AS UTCDATETIME = "2024-02-29T14:30:00Z"
PRINT ELAPSEDSECONDS(first, second)
PRINT ELAPSEDSECONDS(second, first)
PRINT ELAPSEDSECONDS(first, first)
""";

        var result = Run(source, new SteppingClock());

        Assert.Equal(["9000", "-9000", "0"], result.Output);
    }

    [Fact]
    public void BetweenInstantsKeepsFractionalSeconds()
    {
        const string source = """
DIM first AS UTCDATETIME = "2024-02-29T12:00:00.125Z"
DIM second AS UTCDATETIME = "2024-02-29T12:00:01.375Z"
PRINT ELAPSEDSECONDS(first, second)
""";

        var result = Run(source, new SteppingClock());

        Assert.Equal(["1.25"], result.Output);
    }

    [Fact]
    public void BetweenInstantsSpansDaysAndClockTransitions()
    {
        // Между этими моментами в Нью-Йорке происходит перевод часов, но разность
        // моментов считается по шкале UTC и потому равна ровно 26 часам.
        const string source = """
DIM first AS UTCDATETIME = "2024-11-02T12:00:00Z"
DIM second AS UTCDATETIME = "2024-11-03T14:00:00Z"
PRINT ELAPSEDSECONDS(first, second)
""";

        var result = Run(source, new SteppingClock());

        Assert.Equal(["93600"], result.Output);
    }

    [Fact]
    public void CalendarValuesAreRejectedByDurationForm()
    {
        var programs = new[]
        {
            """
            DIM localValue AS DATETIME = "2024-02-29 12:00:00"
            PRINT ELAPSEDSECONDS(localValue, localValue)
            """,
            """
            DIM localValue AS DATETIME = "2024-02-29 12:00:00"
            PRINT ELAPSEDSECONDS(localValue)
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(
                new ExecutionRequest(source, runtimeClock: new SteppingClock()));

            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Fact]
    public void CounterStartsAtZeroForEachRun()
    {
        const string source = """
PRINT ELAPSEDSECONDS() < 1
""";

        var result = Run(source, new SteppingClock());

        Assert.Equal(["TRUE"], result.Output);
    }

    private sealed class SteppingClock : IRuntimeClock
    {
        private readonly TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone(
            "AxiomStep", TimeSpan.FromHours(3), "AxiomStep", "AxiomStep");
        private double _seconds;

        public DateTimeOffset GetLocalNow() => new(2024, 3, 1, 0, 30, 0, TimeSpan.FromHours(3));

        public TimeZoneInfo GetLocalTimeZone() => zone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = id.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : zone;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
                || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }

        // Каждое чтение счётчика сдвигает время на 0,1 с: так проверка не зависит
        // от реальной длительности SLEEP на конкретной машине.
        public double GetMonotonicSeconds() => _seconds += 0.1;
    }

    private sealed class JumpingClock : IRuntimeClock
    {
        private readonly TimeZoneInfo zone = TimeZoneInfo.Utc;
        private double _seconds;

        public DateTimeOffset GetLocalNow()
        {
            // Календарные часы намеренно нестабильны: назад, затем далеко вперёд.
            var offset = _seconds switch
            {
                < 1 => TimeSpan.FromMinutes(-30),
                < 2 => TimeSpan.FromHours(5),
                _ => TimeSpan.Zero
            };
            return new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.Zero) + offset;
        }

        public TimeZoneInfo GetLocalTimeZone() => zone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = TimeZoneInfo.Utc;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
                || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }

        public double GetMonotonicSeconds() => _seconds += 0.05;
    }

    private static ExecutionResult Run(string source, IRuntimeClock clock)
    {
        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, runtimeClock: clock));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        return result;
    }
}
