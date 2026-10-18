using Axiom.Core;
using Axiom.Execution;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

/// <summary>
/// Постоянная матрица договора временных преобразований. Аудит DTA-002/DTA-003
/// показал, что запрет усечения зависел от синтаксической оболочки: скобки,
/// строковая переменная, аргумент, RETURN и сравнение обходили его. Здесь каждый
/// контекст проверяется независимо, чтобы договор нельзя было снова сузить до
/// одного вида инструкции.
/// </summary>
public sealed class TemporalConversionContractTests
{
    private const string DateTimeText = "2024-02-29 12:34:56";

    [Fact]
    public void EveryAssignmentContextRejectsImplicitComponentLoss()
    {
        var programs = new (string Name, string Source)[]
        {
            ("DIM literal", $"""DIM value AS DATE = "{DateTimeText}" """.TrimEnd()),
            ("DIM parenthesized", $"""DIM value AS DATE = ("{DateTimeText}")"""),
            ("DIM through STRING", $$"""
                DIM text AS STRING = "{{DateTimeText}}"
                DIM value AS DATE = text
                """),
            ("assignment literal", $"""
                DIM value AS DATE
                value = "{DateTimeText}"
                """),
            ("assignment parenthesized", $"""
                DIM value AS DATE
                value = ("{DateTimeText}")
                """),
            ("assignment through STRING", $$"""
                DIM text AS STRING = "{{DateTimeText}}"
                DIM value AS DATE
                value = text
                """),
            ("assignment from DATETIME", """
                DIM moment AS DATETIME = "2024-02-29 12:34:56"
                DIM value AS DATE
                value = moment
                """),
            ("TIME from DATETIME", """
                DIM moment AS DATETIME = "2024-02-29 12:34:56"
                DIM value AS TIME = moment
                """),
            ("array element", $"""
                DIM values(1 TO 2) AS DATE
                values(1) = "{DateTimeText}"
                """),
            ("record field", $"""
                TYPE Entry
                    Value AS DATE
                END TYPE
                DIM item AS Entry
                item.Value = "{DateTimeText}"
                """),
        };

        foreach (var (name, source) in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics);

            // Текст литерала известен связыванию (MB2003), содержимое строковой
            // переменной — только исполнению (MB3005). Оба случая обязаны отказать
            // и не менять значение цели.
            Assert.True(
                diagnostic.Code is "MB2003" or "MB3005",
                $"{name}: неожиданный код {diagnostic.Code}.");
            Assert.Contains("truncated", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(result.Output.Count == 0, $"{name}: программа не должна исполняться.");
        }
    }

    [Fact]
    public void EveryCallAndReturnContextRejectsImplicitComponentLoss()
    {
        var programs = new (string Name, string Source)[]
        {
            ("argument literal", $"""
                SUB Show(value AS DATE)
                    PRINT value
                END SUB
                Show("{DateTimeText}")
                """),
            ("argument DATETIME", """
                SUB Show(value AS DATE)
                    PRINT value
                END SUB
                DIM moment AS DATETIME = "2024-02-29 12:34:56"
                Show(moment)
                """),
            ("function argument", """
                FUNCTION Echo(value AS DATE) AS DATE
                    RETURN value
                END FUNCTION
                DIM moment AS DATETIME = "2024-02-29 12:34:56"
                PRINT Echo(moment)
                """),
            ("RETURN DATETIME", """
                FUNCTION CalendarPart(value AS DATETIME) AS DATE
                    RETURN value
                END FUNCTION
                PRINT CalendarPart("2024-02-29 12:34:56")
                """),
            ("nested function argument", """
                FUNCTION Echo(value AS DATE) AS DATE
                    RETURN value
                END FUNCTION
                FUNCTION CalendarPart(value AS DATETIME) AS DATE
                    RETURN Echo(value)
                END FUNCTION
                PRINT CalendarPart("2024-02-29 12:34:56")
                """),
        };

        foreach (var (name, source) in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB2003", diagnostic.Code);
            Assert.Contains("truncated", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(result.Output.Count == 0, $"{name}: программа не должна исполняться.");
        }
    }

    [Fact]
    public void ComparisonWithFullTextDoesNotSilentlyTruncate()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
PRINT dayValue = "2024-02-29 12:34:56"
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2003", diagnostic.Code);
        Assert.Contains("truncated", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComparisonWithTypedDateTimeStillComparesMidnight()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM moment AS DATETIME = "2024-02-29 12:34:56"
PRINT dayValue = moment
PRINT dayValue < moment
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["FALSE", "TRUE"], result.Output);
    }

    [Fact]
    public void ExplicitPartExtractionRemainsAllowed()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56"
PRINT DATEVALUE("2024-02-29 12:34:56")
PRINT TIMEVALUE("2024-02-29 12:34:56")
PRINT YEAR(moment)
PRINT HOUR(moment)
PRINT STARTOFMONTH(moment)
DIM dayValue AS DATE = DATEVALUE("2024-02-29 12:34:56")
PRINT dayValue
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29", "12:34:56", "2024", "12", "2024-02-01", "2024-02-29"], result.Output);
    }

    [Fact]
    public void WideningConversionsRemainAllowed()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM clockTime AS TIME = "12:34:56"
DIM fromDate AS DATETIME = dayValue
DIM fromTime AS DATETIME = clockTime
PRINT fromDate
PRINT fromTime
DIM text AS STRING = fromDate
PRINT text
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 00:00:00", "0001-01-01 12:34:56", "2024-02-29 00:00:00"], result.Output);
    }

    [Fact]
    public void LossyConversionAtRuntimeKeepsTargetAndIsCatchable()
    {
        // Содержимое строки известно только во время исполнения: CONST, переменная
        // и результат вызова не проверяются связыванием. Отказ обязан остаться
        // языковой диагностикой MB3005 и не менять прежнее значение цели.
        const string source = """
DIM value AS DATE = "2024-01-01"
DIM text AS STRING = "2024-02-29 12:34:56"
TRY
    value = text
    PRINT "присваивание прошло"
CATCH err
    PRINT err.Code
END TRY
PRINT value
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["MB3005", "2024-01-01"], result.Output);
    }

    [Fact]
    public void PartExtractionRejectsForeignTemporalKinds()
    {
        var programs = new[]
        {
            """
            DIM clockTime AS TIME = "12:34:56"
            PRINT YEAR(clockTime)
            """,
            """
            DIM dayValue AS DATE = "2024-02-29"
            PRINT HOUR(dayValue)
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB2003", diagnostic.Code);
        }
    }

    [Fact]
    public void CurrentTimeInZoneReportsLanguageDiagnosticAtRangeLimit()
    {
        // Аудит DTA-004: раньше предельный момент источника часов выпускал
        // System.ArgumentOutOfRangeException за границы конвейера, и CATCH не срабатывал.
        foreach (var call in new[] { """DATE("Asia/Tokyo")""", """TIME("Asia/Tokyo")""", """NOW("Asia/Tokyo")""" })
        {
            var source = $$"""
                TRY
                    PRINT {{call}}
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """;

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, runtimeClock: new ExtremeClock()));

            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(["MB3007", "Продолжение"], result.Output);
        }
    }

    internal sealed class ExtremeClock : Axiom.Runtime.IRuntimeClock
    {
        public DateTimeOffset GetLocalNow() => DateTimeOffset.MaxValue;

        public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Utc;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            if (id.Equals("Asia/Tokyo", StringComparison.OrdinalIgnoreCase)
                && TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Tokyo", out var tokyo))
            {
                timeZone = tokyo;
                return true;
            }

            timeZone = TimeZoneInfo.Utc;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
                || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }
    }
}
