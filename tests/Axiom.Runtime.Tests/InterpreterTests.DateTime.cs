using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_DateTimeBuiltinsUseCanonicalLocalFormats()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT TIMER()
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-20", "14:05:06", "2026-04-20 14:05:06", "50706"], host.Output);
    }

    [Fact]
    public void Execute_TimerPreservesFractionalSeconds()
    {
        const string text = "PRINT TIMER()";
        var localNow = new DateTimeOffset(2026, 04, 20, 00, 00, 01, 250, TimeSpan.FromHours(3));

        var (_, host, result) = ExecuteProgramAt(text, localNow);

        Assert.True(result.Succeeded);
        Assert.Equal(["1.25"], host.Output);
    }

    [Fact]
    public void Execute_DateTimeStage2FunctionsUseCanonicalContracts()
    {
        const string text = """
PRINT DATESERIAL(2026, 4, 20)
PRINT TIMESERIAL(14, 5, 6)
PRINT YEAR(DATESERIAL(2026, 4, 20))
PRINT MONTH(DATESERIAL(2026, 4, 20))
PRINT DAY(DATESERIAL(2026, 4, 20))
PRINT HOUR(TIMESERIAL(14, 5, 6))
PRINT MINUTE(TIMESERIAL(14, 5, 6))
PRINT SECOND(TIMESERIAL(14, 5, 6))
PRINT WEEKDAY(DATESERIAL(2026, 4, 20))
PRINT WEEKDAY(DATESERIAL(2026, 4, 26))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-20", "14:05:06", "2026", "4", "20", "14", "5", "6", "1", "7"], host.Output);
    }

    [Fact]
    public void Execute_DateTimeStage2ReportsInvalidCanonicalText()
    {
        const string text = "PRINT YEAR(\"20.04.2026\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DATE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateSerialReportsInvalidCalendarDate()
    {
        const string text = "PRINT DATESERIAL(2025, 2, 29)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("day", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_TimeSerialReportsOutOfRangeHour()
    {
        const string text = "PRINT TIMESERIAL(24, 0, 0)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TIMESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hour", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateTimeStage3FunctionsUseCanonicalContracts()
    {
        const string text = """
PRINT DATEADD("m", 1, "2026-01-31")
PRINT DATEADD("h", 2, "2026-04-20 23:05:06")
PRINT DATEADD("s", 10, "23:59:55")
PRINT DATEDIFF("m", "2026-01-31", "2026-04-20")
PRINT DATEDIFF("d", "2026-04-20 23:59:59", "2026-04-21 00:00:00")
PRINT DATEDIFF("n", "14:05:59", "14:06:00")
PRINT DATEPART("q", "2026-04-20")
PRINT DATEPART("y", "2026-04-21")
PRINT DATEPART("w", "2026-04-26")
PRINT DATEPART("h", "2026-04-20 14:05:06")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20")
PRINT ISDATE("14:05:06")
PRINT ISDATE("2026-04-20 14:05:06")
PRINT ISDATE("20.04.2026")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-02-28",
                "2026-04-21 01:05:06",
                "00:00:05",
                "3",
                "1",
                "1",
                "2",
                "111",
                "7",
                "14",
                "2026-04-20",
                "14:05:06",
                "TRUE",
                "TRUE",
                "TRUE",
                "FALSE"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_DateTimeNameHelpersReturnCanonicalEnglishNames()
    {
        const string text = """
PRINT MONTHNAME(4)
PRINT MONTHNAME(MONTH(DATESERIAL(2026, 12, 20)))
PRINT WEEKDAYNAME(1)
PRINT WEEKDAYNAME(WEEKDAY(DATESERIAL(2026, 4, 26)))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "April",
                "December",
                "Monday",
                "Sunday"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_DateAddReportsInvalidIntervalToken()
    {
        const string text = "PRINT DATEADD(\"ww\", 1, \"2026-04-20\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATEADD", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("interval", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_TimeValueReportsInvalidCanonicalText()
    {
        const string text = "PRINT TIMEVALUE(\"2:24PM\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TIMEVALUE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_MonthNameReportsOutOfRangeMonth()
    {
        const string text = "PRINT MONTHNAME(13)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MONTHNAME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("month", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WeekdayNameReportsOutOfRangeWeekday()
    {
        const string text = "PRINT WEEKDAYNAME(0)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("WEEKDAYNAME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weekday", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CalendarHelperBuiltinsReturnExpectedResults()
    {
        const string text = """
PRINT ISLEAPYEAR(2024)
PRINT ISLEAPYEAR(2025)
PRINT DAYSINMONTH(2024, 2)
PRINT STARTOFMONTH(DATESERIAL(2026, 4, 20))
PRINT ENDOFMONTH(DATESERIAL(2024, 2, 11))
PRINT STARTOFYEAR(DATESERIAL(2026, 4, 20))
PRINT ENDOFYEAR(DATESERIAL(2026, 4, 20))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "TRUE",
                "FALSE",
                "29",
                "2026-04-01",
                "2024-02-29",
                "2026-01-01",
                "2026-12-31"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_IsLeapYearReportsOutOfRangeYear()
    {
        const string text = "PRINT ISLEAPYEAR(10000)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ISLEAPYEAR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("year", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DaysInMonthReportsOutOfRangeMonth()
    {
        const string text = "PRINT DAYSINMONTH(2026, 13)";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DAYSINMONTH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("month", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_StartOfMonthReportsInvalidCanonicalText()
    {
        const string text = "PRINT STARTOFMONTH(\"20.04.2026\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DATE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcBuiltinsUseCurrentHostOffset()
    {
        const string text = """
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT UTCOFFSET()
PRINT TOUTC(NOW())
PRINT FROMUTC(UTCNOW())
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["2026-04-20", "11:05:06", "2026-04-20 11:05:06", "180", "2026-04-20 11:05:06", "2026-04-20 14:05:06"],
            host.Output);
    }

    [Fact]
    public void Execute_UtcOffsetArgumentAndUtcConversionsUseSpecificDateOffsetRules()
    {
        const string text = """
PRINT UTCOFFSET()
PRINT UTCOFFSET("2026-01-20 12:00:00")
PRINT UTCOFFSET("2026-07-20 12:00:00")
PRINT TOUTC("2026-01-20 12:00:00")
PRINT TOUTC("2026-07-20 12:00:00")
PRINT FROMUTC("2026-01-20 10:00:00")
PRINT FROMUTC("2026-07-20 09:00:00")
""";

        var localTimeZone = CreateTestSeasonalTimeZone();
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["180", "120", "180", "2026-01-20 10:00:00", "2026-07-20 09:00:00", "2026-01-20 12:00:00", "2026-07-20 12:00:00"],
            host.Output);
    }

    [Fact]
    public void Execute_TimeZoneAwareCurrentBuiltinsUseRequestedZone()
    {
        const string text = """
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT DATE("Axiom Runtime Tests West")
PRINT TIME("Axiom Runtime Tests West")
PRINT NOW("Axiom Runtime Tests West")
PRINT UTCOFFSET(NOW("Axiom Runtime Tests West"), "Axiom Runtime Tests West")
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 00, 30, 45, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();
        var westTimeZone = CreateTestFixedTimeZone("Axiom Runtime Tests West", -5);
        var namedTimeZones = CreateNamedTimeZones(westTimeZone);
        var (_, host, result) = ExecuteProgramAt(text, localNow, localTimeZone, namedTimeZones);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["2026-04-19", "21:30:45", "2026-04-19 21:30:45", "2026-04-19", "16:30:45", "2026-04-19 16:30:45", "-300"],
            host.Output);
    }

    [Fact]
    public void Execute_TimeZoneAwareUtcConversionsUseRequestedZoneRules()
    {
        const string text = """
PRINT UTCOFFSET("2026-01-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT UTCOFFSET("2026-07-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT TOUTC("2026-01-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT TOUTC("2026-07-20 12:00:00", "Axiom Runtime Tests Seasonal")
PRINT FROMUTC("2026-01-20 10:00:00", "Axiom Runtime Tests Seasonal")
PRINT FROMUTC("2026-07-20 09:00:00", "Axiom Runtime Tests Seasonal")
""";

        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);
        var (_, host, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["120", "180", "2026-01-20 10:00:00", "2026-07-20 09:00:00", "2026-01-20 12:00:00", "2026-07-20 12:00:00"],
            host.Output);
    }

    [Fact]
    public void Execute_ToUtcAndFromUtcCanCrossDateBoundary()
    {
        const string text = """
PRINT TOUTC("2026-04-20 01:15:00")
PRINT FROMUTC("2026-04-20 22:15:00")
""";

        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var (_, host, result) = ExecuteProgramAt(text, localNow);

        Assert.True(result.Succeeded);
        Assert.Equal(["2026-04-19 22:15:00", "2026-04-21 01:15:00"], host.Output);
    }

    [Fact]
    public void Execute_ToUtcReportsMissingTimePart()
    {
        const string text = "PRINT TOUTC(\"2026-04-20\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DATETIME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DateReportsUnknownTimeZoneIdentifier()
    {
        const string text = "PRINT DATE(\"Missing/Zone\")";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DATE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timeZone", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ToUtcReportsInvalidLocalDateTimeDuringSkippedTransition()
    {
        const string text = "PRINT TOUTC(\"2026-03-29 02:30:00\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ToUtcWithTimeZoneReportsInvalidLocalDateTimeDuringSkippedTransition()
    {
        const string text = "PRINT TOUTC(\"2026-03-29 02:30:00\", \"Axiom Runtime Tests Seasonal\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);

        var (_, _, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcOffsetReportsAmbiguousLocalDateTimeDuringRepeatedTransition()
    {
        const string text = "PRINT UTCOFFSET(\"2026-10-25 02:30:00\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("UTCOFFSET", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcOffsetWithTimeZoneReportsAmbiguousLocalDateTimeDuringRepeatedTransition()
    {
        const string text = "PRINT UTCOFFSET(\"2026-10-25 02:30:00\", \"Axiom Runtime Tests Seasonal\")";
        var localNow = new DateTimeOffset(2026, 07, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var seasonalTimeZone = CreateTestSeasonalTimeZone();
        var namedTimeZones = CreateNamedTimeZones(seasonalTimeZone);

        var (_, _, result) = ExecuteProgramAt(text, localNow, TimeZoneInfo.Utc, namedTimeZones);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("UTCOFFSET", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_FromUtcReportsOutOfRangeAfterConversion()
    {
        const string text = "PRINT FROMUTC(\"9999-12-31 23:59:59\")";
        var localNow = new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3));
        var localTimeZone = CreateTestSeasonalTimeZone();

        var (_, _, result) = ExecuteProgramAt(text, localNow, localTimeZone);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("FROMUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
