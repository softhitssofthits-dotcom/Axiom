using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed class InterpreterTests
{
    [Fact]
    public void Execute_ArithmeticExpression()
    {
        var (_, host, result) = ExecuteProgram("PRINT 1 + 2 * 3");

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_IntegerDivisionAndRemainder()
    {
        const string text = """
PRINT 7 \ 2
PRINT 7 MOD 2
PRINT -7 \ 2
PRINT -7 MOD 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "1", "-3", "-1"], host.Output);
    }

    [Fact]
    public void Execute_IntegerDivisionAndRemainderPromoteToLong()
    {
        const string text = """
DIM value AS LONG = 21474836479
PRINT value \ 2
PRINT value MOD 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10737418239", "1"], host.Output);
    }

    [Fact]
    public void Execute_ExponentiationAndHexLiterals()
    {
        const string text = """
PRINT 2 ^ 3
PRINT 2 ^ 3 ^ 2
PRINT &HFF
PRINT &H80000000
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["8", "512", "255", "2147483648"], host.Output);
    }

    [Fact]
    public void Execute_BitwiseOperatorsAndShifts()
    {
        const string text = """
PRINT 6 AND 3
PRINT 6 OR 1
PRINT 6 XOR 3
PRINT 6 EQV 3
PRINT 6 IMP 3
PRINT NOT 0
PRINT TRUE XOR FALSE
PRINT TRUE EQV TRUE
PRINT TRUE IMP FALSE
PRINT 1 + 2 << 1
DIM mask AS LONG = &H100000000
PRINT mask >> 4
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "7", "5", "-6", "-5", "-1", "TRUE", "TRUE", "FALSE", "6", "268435456"], host.Output);
    }

    [Fact]
    public void Execute_EmptyPrint()
    {
        var (_, host, result) = ExecuteProgram("PRINT");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_PrintExpressionCanProduceEmptyString()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"\"");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\";");

        Assert.True(result.Succeeded);
        Assert.Equal(["A"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaUsesSoftSeparator()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaSeparatesNumericValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT 10, 20");

        Assert.True(result.Succeeded);
        Assert.Equal(["10 20"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithMultipleCommasUsesSingleSpaces()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\", \"C\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B C"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithTrailingCommaSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\",");

        Assert.True(result.Succeeded);
        Assert.Equal(["A "], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonConcatenatesValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\"; \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["AB"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithMixedListUsesSeparatorSemantics()
    {
        var (_, host, result) = ExecuteProgram("PRINT 1; 2, 3");

        Assert.True(result.Succeeded);
        Assert.Equal(["12 3"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaDoesNotUseFixedZones()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"ABCD\", \"E\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["ABCD E"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsNumericMask()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##.##\"; 3.14159");

        Assert.True(result.Succeeded);
        Assert.Equal([" 3.14"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsStringMasks()
    {
        const string text = """
PRINT USING "! & \    \"; "Hello"; "World"; "xy"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["H World xy    "], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingTrailingSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##\"; 12;");

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingReportsStringFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"!\"; 1");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PrintUsingReportsNumericFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"##\"; \"oops\"");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("числов", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

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
PRINT YEAR("2026-04-20")
PRINT MONTH("2026-04-20 14:05:06")
PRINT DAY("2026-04-20")
PRINT HOUR("14:05:06")
PRINT MINUTE("2026-04-20 14:05:06")
PRINT SECOND("14:05:06")
PRINT WEEKDAY("2026-04-20")
PRINT WEEKDAY("2026-04-26")
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

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("YEAR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
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
PRINT MONTHNAME(MONTH("2026-12-20"))
PRINT WEEKDAYNAME(1)
PRINT WEEKDAYNAME(WEEKDAY("2026-04-26"))
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
PRINT STARTOFMONTH("2026-04-20 14:05:06")
PRINT ENDOFMONTH("2024-02-11")
PRINT STARTOFYEAR("2026-04-20")
PRINT ENDOFYEAR("2026-04-20 14:05:06")
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

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STARTOFMONTH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UtcBuiltinsUseCurrentHostOffset()
    {
        const string text = """
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT UTCOFFSET()
PRINT TOUTC("2026-04-20 14:05:06")
PRINT FROMUTC("2026-04-20 11:05:06")
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

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("TOUTC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Execute_Assignment()
    {
        const string text = """
DIM value AS INTEGER
value = 10
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_GroupedDimDeclarations()
    {
        const string text = """
DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING
anotherVar = TRUE
choice = FALSE
birthDate = "2026-04-13"
PRINT anotherVar
PRINT choice
PRINT birthDate
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "2026-04-13"], host.Output);
    }

    [Fact]
    public void Execute_InferredDimDeclarations()
    {
        const string text = """
DIM count = 10
DIM name = "Alice"
DIM ready = TRUE
DIM price = 1.5
PRINT count
PRINT name
PRINT ready
PRINT price
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "Alice", "TRUE", "1.5"], host.Output);
    }

    [Fact]
    public void Execute_InferredLongDeclarationAndArithmetic()
    {
        const string text = """
DIM value = 21474836479
PRINT value
PRINT value + value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["21474836479", "42949672958"], host.Output);
    }

    [Fact]
    public void Execute_DynamicArrayDeclarationDoesNotFail()
    {
        const string text = """
DIM items() AS STRING
PRINT "ok"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["ok"], host.Output);
    }

    [Fact]
    public void Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()
    {
        const string text = """
DIM items() AS STRING
PRINT items(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("items", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_WholeArrayAssignmentSharesAllocatedArray()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM target() AS INTEGER
source(1) = 10
source(2) = 20
target = source
PRINT target(1)
PRINT target(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayCanBePassedToSubAndReturnedFromFunction()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20

Bump(values)

DIM copy() AS INTEGER
copy = Echo(values)

PRINT values(1)
PRINT copy(2)

SUB Bump(items AS INTEGER())
    items(1) = items(1) + 1
END SUB

FUNCTION Echo(items AS INTEGER()) AS INTEGER()
    RETURN items
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11", "20"], host.Output);
    }

    [Fact]
    public void Execute_RedimAllocatesDynamicArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 3)
values(1) = 10
values(2) = 20
values(3) = 30
PRINT values(1)
PRINT values(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "30"], host.Output);
    }

    [Fact]
    public void Execute_RedimResetsSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
alias = source
REDIM alias(5 TO 6)
PRINT source(5)
PRINT source(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveKeepsOverlappingValues()
    {
        const string text = """
DIM values(1 TO 3) AS INTEGER
values(1) = 10
values(2) = 20
values(3) = 30
REDIM PRESERVE values(2 TO 4)
PRINT values(2)
PRINT values(3)
PRINT values(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "30", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimPreserveMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
source(1) = 10
source(2) = 20
alias = source
REDIM PRESERVE alias(2 TO 3)
PRINT source(2)
PRINT source(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_DimAllowsCanonicalEmptyAllocatedArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_RedimRejectsNonCanonicalEmptyRange()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(5 TO 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 TO 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_DimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values(-2147483648 TO -2147483648) AS INTEGER
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimRejectsMinimumIntegerLowerBound()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(-2147483648 TO -2147483648)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("-2147483648", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RedimPreserveCanShrinkToCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20
REDIM PRESERVE values(1 TO 0)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_EraseMakesArrayUnallocatedAgain()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 2)
values(1) = 10
ERASE values
PRINT values(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()
    {
        const string text = """
DIM source(5 TO 6) AS INTEGER
DIM copy() AS INTEGER
source(5) = 10
source(6) = 20
copy = ARRAYCLONE(source)
copy(5) = 99
PRINT LBOUND(copy)
PRINT UBOUND(copy)
PRINT ARRAYLENGTH(copy)
PRINT source(5)
PRINT copy(5)
PRINT source(6)
PRINT copy(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "6", "2", "10", "99", "20", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayClonePreservesUnallocatedAndCanonicalEmptyStates()
    {
        const string text = """
DIM pending() AS STRING
DIM pendingCopy() AS STRING
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
REDIM pending(1 TO 0)
pendingCopy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(pendingCopy)
PRINT ARRAYLENGTH(pendingCopy)
PRINT LBOUND(pendingCopy)
PRINT UBOUND(pendingCopy)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReturnsCompactIndependentSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSLICE(source, 6, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT source(7)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceAllowsZeroCountAsCanonicalEmptyArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 999, 0)
PRINT ARRAYISALLOCATED(part)
PRINT ARRAYLENGTH(part)
PRINT LBOUND(part)
PRINT UBOUND(part)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySliceReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySliceReportsOutOfRangeSlice()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 7, 2)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSLICE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayConcatReturnsCompactIndependentArray()
    {
        const string text = """
DIM left(5 TO 6) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
left(5) = 10
left(6) = 20
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
merged(1) = 99
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT left(5)
PRINT merged(1)
PRINT merged(2)
PRINT merged(3)
PRINT merged(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "10", "99", "20", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatAllowsCanonicalEmptyOperands()
    {
        const string text = """
DIM left(1 TO 0) AS INTEGER
DIM right(10 TO 11) AS INTEGER
DIM merged() AS INTEGER
right(10) = 30
right(11) = 40
merged = ARRAYCONCAT(left, right)
PRINT ARRAYISALLOCATED(merged)
PRINT LBOUND(merged)
PRINT UBOUND(merged)
PRINT ARRAYLENGTH(merged)
PRINT merged(1)
PRINT merged(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "2", "30", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayConcatReportsUnallocatedOperand()
    {
        const string text = """
DIM left() AS INTEGER
DIM right(1 TO 1) AS INTEGER
DIM merged() AS INTEGER
merged = ARRAYCONCAT(left, right)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONCAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToReversedReturnsCompactIndependentArray()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM reversed() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
reversed = ARRAYTOREVERSED(source)
reversed(1) = 99
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT source(7)
PRINT reversed(1)
PRINT reversed(2)
PRINT reversed(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "30", "99", "20", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
PRINT ARRAYISALLOCATED(reversed)
PRINT ARRAYLENGTH(reversed)
PRINT LBOUND(reversed)
PRINT UBOUND(reversed)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToReversedReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM reversed() AS INTEGER
reversed = ARRAYTOREVERSED(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOREVERSED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeReturnsCompactIndependentPrefix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYTAKE(source, 2)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(5)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "10", "99", "20"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM emptyPart() AS INTEGER
DIM wholePart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
emptyPart = ARRAYTAKE(source, 0)
wholePart = ARRAYTAKE(source, 99)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
PRINT LBOUND(wholePart)
PRINT UBOUND(wholePart)
PRINT wholePart(1)
PRINT wholePart(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "3", "10", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayTakeReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYTAKE(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayTakeReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYTAKE(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReturnsCompactIndependentSuffix()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM part() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
part = ARRAYSKIP(source, 1)
part(1) = 99
PRINT LBOUND(part)
PRINT UBOUND(part)
PRINT ARRAYLENGTH(part)
PRINT source(6)
PRINT part(1)
PRINT part(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "20", "99", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipAllowsZeroAndExcessiveCount()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM samePart() AS INTEGER
DIM emptyPart() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 30
samePart = ARRAYSKIP(source, 0)
emptyPart = ARRAYSKIP(source, 99)
PRINT LBOUND(samePart)
PRINT UBOUND(samePart)
PRINT samePart(1)
PRINT samePart(3)
PRINT ARRAYISALLOCATED(emptyPart)
PRINT ARRAYLENGTH(emptyPart)
PRINT LBOUND(emptyPart)
PRINT UBOUND(emptyPart)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "10", "30", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArraySkipReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSKIP(source, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySkipReportsNegativeCount()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYSKIP(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayDistinctReturnsCompactUniqueArray()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM uniqueValues() AS INTEGER
source(5) = 10
source(6) = 20
source(7) = 10
source(8) = 30
source(9) = 20
uniqueValues = ARRAYDISTINCT(source)
uniqueValues(1) = 99
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT source(5)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT uniqueValues(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "3", "10", "99", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctAllowsCanonicalEmptyResult()
    {
        const string text = """
DIM source(1 TO 0) AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
PRINT ARRAYISALLOCATED(uniqueValues)
PRINT ARRAYLENGTH(uniqueValues)
PRINT LBOUND(uniqueValues)
PRINT UBOUND(uniqueValues)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayDistinctReportsUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM uniqueValues() AS INTEGER
uniqueValues = ARRAYDISTINCT(source)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYDISTINCT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayToSortedPreservesBoundsAndDoesNotMutateSource()
    {
        const string text = """
DIM source(5 TO 9) AS INTEGER
DIM sortedValues() AS INTEGER
source(5) = 30
source(6) = 10
source(7) = 20
source(8) = 20
source(9) = 10
sortedValues = ARRAYTOSORTED(source)
sortedValues(5) = 99
PRINT LBOUND(sortedValues)
PRINT UBOUND(sortedValues)
PRINT source(5)
PRINT source(6)
PRINT source(7)
PRINT source(8)
PRINT source(9)
PRINT sortedValues(5)
PRINT sortedValues(6)
PRINT sortedValues(7)
PRINT sortedValues(8)
PRINT sortedValues(9)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "9", "30", "10", "20", "20", "10", "99", "10", "20", "20", "30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedSupportsStringsAndCanonicalEmptyArray()
    {
        const string text = """
DIM names(2 TO 5) AS STRING
DIM sortedNames() AS STRING
DIM emptyNames(1 TO 0) AS STRING
DIM emptySorted() AS STRING
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
sortedNames = ARRAYTOSORTED(names)
emptySorted = ARRAYTOSORTED(emptyNames)
PRINT LBOUND(sortedNames)
PRINT UBOUND(sortedNames)
PRINT sortedNames(2)
PRINT sortedNames(3)
PRINT sortedNames(4)
PRINT sortedNames(5)
PRINT ARRAYISALLOCATED(emptySorted)
PRINT LBOUND(emptySorted)
PRINT UBOUND(emptySorted)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "5", "Ann", "Bob", "Bob", "Cara", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayToSortedReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM sortedValues() AS INTEGER
sortedValues = ARRAYTOSORTED(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYTOSORTED", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFillMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 1
values(6) = 2
values(7) = 3
ARRAYFILL(values, 42)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "42", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReverseMutatesArrayInPlace()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
ARRAYREVERSE(values)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["40", "30", "20", "10", "5", "8"], host.Output);
    }

    [Fact]
    public void Execute_ArraySortMutatesArrayInPlaceAcrossTypes()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 30
numbers(6) = 10
numbers(7) = 20
numbers(8) = 20
numbers(9) = 10
names(2) = "Cara"
names(3) = "Ann"
names(4) = "Bob"
names(5) = "Bob"
ARRAYSORT(numbers)
ARRAYSORT(names)
PRINT numbers(5)
PRINT numbers(6)
PRINT numbers(7)
PRINT numbers(8)
PRINT numbers(9)
PRINT LBOUND(numbers)
PRINT UBOUND(numbers)
PRINT names(2)
PRINT names(3)
PRINT names(4)
PRINT names(5)
PRINT LBOUND(names)
PRINT UBOUND(names)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10", "20", "20", "30", "5", "9", "Ann", "Bob", "Bob", "Cara", "2", "5"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMutatingProceduresAllowCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYFILL(values, 1)
ARRAYREVERSE(values)
ARRAYSORT(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFillReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYFILL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReverseReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYREVERSE(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREVERSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySortReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYSORT(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSORT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushAppendsValueAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
values(5) = 10
values(6) = 20
ARRAYPUSH(values, 30)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYPUSH(values, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopReturnsLastElementAndShrinksArray()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYPOP(values)
PRINT values(5)
PRINT values(6)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["30", "10", "20", "5", "6", "2"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPopNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYPOP(values)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYPUSH(values, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayPushReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPopReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPushReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYPUSH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYPUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertShiftsTailAndPreservesLowerBound()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
values(5) = 10
values(6) = 30
values(7) = 40
ARRAYINSERT(values, 6, 20)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT values(8)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "30", "40", "5", "8", "4"], host.Output);
    }

    [Fact]
    public void Execute_ArrayInsertAllowsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 1, 42)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "1", "1", "42"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveReturnsRemovedElementAndShiftsTail()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
PRINT ARRAYREMOVE(values, 6)
PRINT values(5)
PRINT values(6)
PRINT values(7)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["20", "10", "30", "40", "5", "7", "3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayRemoveNormalizesSingleElementArrayToCanonicalEmpty()
    {
        const string text = """
DIM values(5 TO 5) AS INTEGER
values(5) = 99
PRINT ARRAYREMOVE(values, 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ARRAYINSERT(values, 1, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["99", "TRUE", "1", "0", "0", "1", "1", "77"], host.Output);
    }

    [Fact]
    public void Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, GreaterThanThreshold)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayEveryTreatsCanonicalEmptyArrayAsTrueAndArraySomeAsFalse()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReturnsCompactArrayAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM threshold AS INTEGER = 25
DIM filtered() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
filtered = ARRAYFILTER(values, GreaterThanThreshold)
filtered(1) = 99
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)
PRINT values(7)
PRINT filtered(1)
PRINT filtered(2)

FUNCTION GreaterThanThreshold(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "2", "30", "99", "40"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterAllowsCanonicalEmptyInputAndResult()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

REDIM values(5 TO 6)
values(5) = 1
values(6) = 2
filtered = ARRAYFILTER(values, IsLarge)
PRINT ARRAYISALLOCATED(filtered)
PRINT LBOUND(filtered)
PRINT UBOUND(filtered)
PRINT ARRAYLENGTH(filtered)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > 100
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0", "TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFilterReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM filtered() AS INTEGER
filtered = ARRAYFILTER(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFILTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFilterReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
DIM filtered() AS INTEGER
values(1) = 10
values(2) = 20
filtered = ARRAYFILTER(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapReturnsCompactTransformedArrayAndPreservesSource()
    {
        const string text = """
DIM values(5 TO 8) AS INTEGER
DIM factor AS INTEGER = 3
DIM mapped() AS INTEGER
values(5) = 10
values(6) = 20
values(7) = 30
values(8) = 40
mapped = ARRAYMAP(values, Multiply)
mapped(1) = 99
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)
PRINT values(5)
PRINT mapped(1)
PRINT mapped(2)
PRINT mapped(3)
PRINT mapped(4)

FUNCTION Multiply(value AS INTEGER) AS INTEGER
    RETURN value * factor
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "4", "4", "10", "99", "60", "90", "120"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapSupportsPolymorphicResultType()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM prefix AS STRING = "item:"
DIM mapped() AS STRING
values(5) = 10
values(6) = 20
values(7) = 30
mapped = ARRAYMAP(values, FormatValue)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT mapped(1)
PRINT mapped(2)
PRINT mapped(3)

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "3", "item:10", "item:20", "item:30"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapAllowsCanonicalEmptyInput()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)
PRINT ARRAYISALLOCATED(mapped)
PRINT LBOUND(mapped)
PRINT UBOUND(mapped)
PRINT ARRAYLENGTH(mapped)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayMapReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM mapped() AS INTEGER
mapped = ARRAYMAP(values, DoubleIt)

FUNCTION DoubleIt(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYMAP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayMapReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM selector AS FUNCTION(INTEGER) AS STRING
DIM mapped() AS STRING
values(1) = 10
values(2) = 20
mapped = ARRAYMAP(values, selector)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceUsesSeedAndClosure()
    {
        const string text = """
DIM values(5 TO 7) AS INTEGER
DIM bonus AS INTEGER = 1
values(5) = 10
values(6) = 20
values(7) = 30
PRINT ARRAYREDUCE(values, 5, AddWithBonus)
PRINT values(5)

FUNCTION AddWithBonus(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value + bonus
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["68", "10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceSupportsPolymorphicAccumulatorType()
    {
        const string text = """
DIM words(3 TO 5) AS STRING
words(3) = "alpha"
words(4) = "bb"
words(5) = "ccc"
PRINT ARRAYREDUCE(words, 0, AddLength)

FUNCTION AddLength(acc AS INTEGER, value AS STRING) AS INTEGER
    RETURN acc + LEN(value)
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReturnsSeedForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREDUCE(values, 7, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7"], host.Output);
    }

    [Fact]
    public void Execute_ArrayReduceReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREDUCE(values, 0, Add)

FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + value
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREDUCE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayReduceReportsEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS STRING
DIM reducer AS FUNCTION(INTEGER, STRING) AS INTEGER
values(1) = "aa"
values(2) = "bbb"
PRINT ARRAYREDUCE(values, 0, reducer)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYSOME(values, IsPositive)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYSOME", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayPredicateFunctionsReportEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
values(1) = 10
values(2) = 20
PRINT ARRAYEVERY(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYINSERT(values, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
ARRAYINSERT(values, 4, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("диапазон", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsInvalidIndex()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
PRINT ARRAYREMOVE(values, 7)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("существующий логический индекс", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayRemoveReportsCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYREMOVE(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYREMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsInvalidIndexForCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 2, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустого выделенного массива", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("равен 1", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayInsertReportsUpperBoundOverflow()
    {
        const string text = """
DIM values(2147483647 TO 2147483647) AS INTEGER
ARRAYINSERT(values, 2147483647, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsUseLogicalIndexesAcrossLowerBounds()
    {
        const string text = """
DIM oneBased(1 TO 3) AS INTEGER
DIM zeroBased(0 TO 2) AS INTEGER
DIM shifted(5 TO 7) AS INTEGER
DIM negative(-3 TO -1) AS INTEGER
oneBased(1) = 10
oneBased(2) = 20
oneBased(3) = 20
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
shifted(5) = 10
shifted(6) = 20
shifted(7) = 30
negative(-3) = 10
negative(-2) = 20
negative(-1) = 20
PRINT ARRAYCONTAINS(oneBased, 20)
PRINT ARRAYCOUNT(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 20)
PRINT ARRAYLASTINDEXOF(oneBased, 20)
PRINT ARRAYINDEXOF(oneBased, 99)
PRINT ARRAYINDEXOF(zeroBased, 20)
PRINT ARRAYINDEXOF(zeroBased, 99)
PRINT ARRAYINDEXOF(shifted, 20)
PRINT ARRAYINDEXOF(shifted, 99)
PRINT ARRAYLASTINDEXOF(negative, 20)
PRINT ARRAYINDEXOF(negative, 99)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "2", "2", "3", "0", "1", "-1", "6", "4", "-1", "-4"], host.Output);
    }

    [Fact]
    public void Execute_ArraySearchFunctionsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
PRINT ARRAYCOUNT(values, 1)
PRINT ARRAYINDEXOF(values, 1)
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayFindAllReturnsCompactArrayOfLogicalIndexes()
    {
        const string text = """
DIM shifted(5 TO 8) AS INTEGER
DIM negative(-3 TO 0) AS INTEGER
DIM found() AS INTEGER
DIM negativeHits() AS INTEGER
DIM missing() AS INTEGER
shifted(5) = 10
shifted(6) = 20
shifted(7) = 10
shifted(8) = 30
negative(-3) = 20
negative(-2) = 10
negative(-1) = 20
negative(0) = 20
found = ARRAYFINDALL(shifted, 10)
negativeHits = ARRAYFINDALL(negative, 20)
missing = ARRAYFINDALL(shifted, 99)
PRINT ARRAYISALLOCATED(found)
PRINT LBOUND(found)
PRINT UBOUND(found)
PRINT found(1)
PRINT found(2)
PRINT ARRAYISALLOCATED(negativeHits)
PRINT LBOUND(negativeHits)
PRINT UBOUND(negativeHits)
PRINT negativeHits(1)
PRINT negativeHits(2)
PRINT negativeHits(3)
PRINT ARRAYISALLOCATED(missing)
PRINT LBOUND(missing)
PRINT UBOUND(missing)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "2", "5", "7", "TRUE", "1", "3", "-3", "-1", "0", "TRUE", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReturnsFirstLogicalIndexAcrossLowerBounds()
    {
        const string text = """
DIM numbers(5 TO 9) AS INTEGER
DIM zeroBased(0 TO 3) AS INTEGER
DIM names(2 TO 5) AS STRING
numbers(5) = 10
numbers(6) = 20
numbers(7) = 20
numbers(8) = 20
numbers(9) = 30
zeroBased(0) = 10
zeroBased(1) = 20
zeroBased(2) = 30
zeroBased(3) = 40
names(2) = "Ann"
names(3) = "Bob"
names(4) = "Bob"
names(5) = "Cara"
PRINT ARRAYBINARYSEARCH(numbers, 20)
PRINT ARRAYBINARYSEARCH(numbers, 15)
PRINT ARRAYBINARYSEARCH(zeroBased, 10)
PRINT ARRAYBINARYSEARCH(zeroBased, 35)
PRINT ARRAYBINARYSEARCH(names, "Bob")
PRINT ARRAYBINARYSEARCH(names, "Aaron")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6", "4", "0", "-1", "3", "1"], host.Output);
    }

    [Fact]
    public void Execute_ArrayBinarySearchTreatsCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_ArrayContainsReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCONTAINS(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCONTAINS", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayLastIndexOfReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYLASTINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYLASTINDEXOF", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayCountReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYCOUNT(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayFindAllReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
DIM matches() AS INTEGER
matches = ARRAYFINDALL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYFINDALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ArrayBinarySearchReportsUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("ARRAYBINARYSEARCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_EraseMutatesSharedArrayStorage()
    {
        const string text = """
DIM source(1 TO 2) AS INTEGER
DIM alias() AS INTEGER
alias = source
ERASE alias
PRINT source(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinArrayInformationFunctionsReportCurrentState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
REDIM values(3 TO 5)
PRINT ARRAYISALLOCATED(values)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT ARRAYLENGTH(values)
ERASE values
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "TRUE", "3", "5", "3", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_LBoundReportsUnallocatedArrayState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT LBOUND(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3005");
        Assert.Contains("LBOUND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SplitReturnsStringArrayWithOneBasedBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red,green,,blue", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(3)
PRINT parts(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue"], host.Output);
    }

    [Fact]
    public void Execute_SplitEmptyStringReturnsCanonicalEmptyArray()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT ARRAYLENGTH(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SplitReportsEmptySeparator()
    {
        const string text = """
PRINT SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_JoinUsesStringArrayBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red|green|blue", "|")
PRINT JOIN(parts, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["red/green/blue"], host.Output);
    }

    [Fact]
    public void Execute_JoinUnallocatedAndEmptyArraysReturnEmptyString()
    {
        const string text = """
DIM first() AS STRING
DIM second(1 TO 0) AS STRING
PRINT JOIN(first)
PRINT JOIN(second)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", ""], host.Output);
    }

    [Fact]
    public void Execute_DecimalDeclarationAndArithmetic()
    {
        const string text = """
DIM price AS DECIMAL = 10.10
DIM tax AS DECIMAL = 0.20
PRINT price + tax
PRINT (price + tax) / 2
PRINT STR(price + tax)
PRINT INT(price + tax)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10.30", "5.15", "10.30", "10"], host.Output);
    }

    [Fact]
    public void Execute_IfElse()
    {
        const string text = """
DIM result AS STRING
IF TRUE THEN
    result = "yes"
ELSE
    result = "no"
END IF
PRINT result
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["yes"], host.Output);
    }

    [Fact]
    public void Execute_ElseIfChain()
    {
        const string text = """
DIM score AS INTEGER = 7
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["mid"], host.Output);
    }

    [Fact]
    public void Execute_StringOrderedComparisonsUseOrdinalSemantics()
    {
        const string text = """
PRINT "Z" > "M"
PRINT "nuts" > "Nuts"
PRINT "abc" < "abd"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseMatchesFirstClauseOnly()
    {
        const string text = """
DIM x AS INTEGER = 5
SELECT CASE x
    CASE IS > 0
        PRINT "positive"
    CASE 5
        PRINT "five"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["positive"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsRangesAndStrings()
    {
        const string text = """
DIM score AS INTEGER = 8
DIM answer AS STRING = "y"

SELECT CASE score
    CASE 1, 3, 5
        PRINT "odd"
    CASE 6 TO 9
        PRINT "range"
    CASE IS > 100
        PRINT "big"
    CASE ELSE
        PRINT "other"
END SELECT

SELECT CASE answer
    CASE "Y", "y"
        PRINT "yes"
    CASE ELSE
        PRINT "no"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["range", "yes"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsStringRangesAndComparisons()
    {
        const string text = """
DIM command AS STRING = "Nuts"
DIM letter AS STRING = "K"

SELECT CASE command
    CASE IS > "M"
        PRINT "after"
    CASE ELSE
        PRINT "before"
END SELECT

SELECT CASE letter
    CASE "A" TO "M"
        PRINT "first-half"
    CASE ELSE
        PRINT "second-half"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["after", "first-half"], host.Output);
    }

    [Fact]
    public void Execute_While()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
WHILE i < 3
    total = total + i
    i = i + 1
END WHILE
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingWhileCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO WHILE i < 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingUntilCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO UNTIL i = 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingWhileConditionRunsAtLeastOnce()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP WHILE i < 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingUntilConditionRunsUntilConditionBecomesTrue()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP UNTIL i = 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithoutConditionUsesExitDo()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    IF i = 2 THEN
        EXIT DO
    END IF
    PRINT i
    i = i + 1
LOOP
PRINT "done"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "done"], host.Output);
    }

    [Fact]
    public void Execute_ForLoop()
    {
        const string text = """
DIM total AS INTEGER = 0
FOR i = 1 TO 3
    total = total + i
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_ExitForBreaksNearestLoop()
    {
        const string text = """
FOR i = 1 TO 2
    FOR j = 1 TO 3
        IF j = 2 THEN
            EXIT FOR
        END IF
        PRINT STR(i) + ":" + STR(j)
    NEXT
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:1", "2:1"], host.Output);
    }

    [Fact]
    public void Execute_ExitDoBreaksNearestDoLoop()
    {
        const string text = """
DIM outer AS INTEGER = 0
DIM inner AS INTEGER
DO WHILE outer < 2
    inner = 0
    DO WHILE inner < 3
        IF inner = 1 THEN
            EXIT DO
        END IF
        PRINT STR(outer) + ":" + STR(inner)
        inner = inner + 1
    LOOP
    outer = outer + 1
LOOP
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0:0", "1:0"], host.Output);
    }

    [Fact]
    public void Execute_NestedForAndWhileLoops()
    {
        const string text = """
DIM j AS INTEGER
DIM total AS INTEGER = 0
FOR i = 1 TO 2
    j = 1
    WHILE j <= 2
        total = total + 1
        j = j + 1
    END WHILE
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4"], host.Output);
    }

    [Fact]
    public void Execute_NestedDoLoopAndForLoops()
    {
        const string text = """
DIM count AS INTEGER = 0
FOR i = 1 TO 2
    DO WHILE count < 2
        PRINT STR(i) + ":" + STR(count)
        count = count + 1
    LOOP
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:0", "1:1"], host.Output);
    }

    [Fact]
    public void Execute_ReportsStepZero()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3004");
        Assert.Contains("STEP 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_FunctionCallWithReturn()
    {
        const string text = """
PRINT Add(2, 3)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_SubCall()
    {
        const string text = """
DIM total AS INTEGER = 0
Increment()
PRINT total

SUB Increment()
    total = total + 1
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubAndFunctionCanBeCalledBeforeTheirDeclaration()
    {
        const string text = """
DIM result AS INTEGER = 0
Outer()
PRINT result

SUB Outer()
    LocalIncrement()
    result = result + AddLocal(2, 3)

    SUB LocalIncrement()
        result = result + 1
    END SUB

    FUNCTION AddLocal(left AS INTEGER, right AS INTEGER) AS INTEGER
        RETURN left + right
    END FUNCTION
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCallItselfRecursively()
    {
        const string text = """
PRINT Outer(4)

FUNCTION Outer(value AS INTEGER) AS INTEGER
    RETURN Factorial(value)

    FUNCTION Factorial(n AS INTEGER) AS INTEGER
        IF n <= 1 THEN
            RETURN 1
        END IF

        RETURN n * Factorial(n - 1)
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["24"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubCanMutateOuterLocalVariable()
    {
        const string text = """
SUB Outer()
    DIM total AS INTEGER = 10
    AddTwo()
    PRINT total

    SUB AddTwo()
        total = total + 2
    END SUB
END SUB

Outer()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCaptureOuterParameterAndLocalVariable()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23"], host.Output);
    }

    [Fact]
    public void Execute_FunctionCanBeStoredInVariableAndCalled()
    {
        const string text = """
DIM op AS FUNCTION(INTEGER, INTEGER) AS INTEGER
op = Add
PRINT op(2, 3)

FUNCTION Add(left AS INTEGER, right AS INTEGER) AS INTEGER
    RETURN left + right
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_FunctionCanBePassedAsParameter()
    {
        const string text = """
PRINT ApplyTwice(DoubleValue, 3)

FUNCTION ApplyTwice(op AS FUNCTION(INTEGER) AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN op(op(value))
END FUNCTION

FUNCTION DoubleValue(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionPreservesCapturedValueAfterOuterFunctionReturns()
    {
        const string text = """
DIM addTen AS FUNCTION(INTEGER) AS INTEGER
addTen = MakeAdder(10)
PRINT addTen(5)

FUNCTION MakeAdder(baseValue AS INTEGER) AS FUNCTION(INTEGER) AS INTEGER
    FUNCTION Add(delta AS INTEGER) AS INTEGER
        RETURN baseValue + delta
    END FUNCTION

    RETURN Add
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanMutateSharedCapturedState()
    {
        const string text = """
DIM counter AS FUNCTION() AS INTEGER
counter = MakeCounter()
PRINT counter()
PRINT counter()
PRINT counter()

FUNCTION MakeCounter() AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = 0

    FUNCTION NextValue() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION

    RETURN NextValue
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanBeCalledImmediately()
    {
        const string text = """
PRINT MakeAdder()(5)

FUNCTION MakeAdder() AS FUNCTION(INTEGER) AS INTEGER
    DIM offset AS INTEGER = 10

    FUNCTION AddOffset(value AS INTEGER) AS INTEGER
        RETURN offset + value
    END FUNCTION

    RETURN AddOffset
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReportsCallingEmptyFunctionValue()
    {
        const string text = """
DIM missing AS FUNCTION(INTEGER) AS INTEGER
PRINT missing(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinLen()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEN(\"hello\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStrAndVal()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"12.5\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["12.5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinTrimVariants()
    {
        const string text = """
PRINT TRIM("  hello  ")
PRINT TRIM("--hello--", "-")
PRINT LTRIM("  hello  ")
PRINT RTRIM("  hello  ")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hello", "hello", "hello  ", "  hello", "hello", "hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReverse()
    {
        var (_, host, result) = ExecuteProgram("PRINT REVERSE(\"abcd\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["dcba"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatAndSpace()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT REPEAT("x", 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", ""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStringUsesFirstFillCharacter()
    {
        var (_, host, result) = ExecuteProgram("PRINT STRING(5, \"ab\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["aaaaa"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinPadLeftAndPadRight()
    {
        const string text = """
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADLEFT("7", 3, "0")
PRINT PADLEFT("word", 2, ".")
PRINT "[" + PADRIGHT("7", 3) + "]"
PRINT PADRIGHT("7", 3, ".")
PRINT PADRIGHT("word", 2, ".")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[  7]", "007", "word", "[7  ]", "7..", "word"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInsertAndRemove()
    {
        const string text = """
PRINT INSERT("abcd", 1, "<")
PRINT INSERT("abcd", 3, "-")
PRINT INSERT("abcd", 99, "!")
PRINT REMOVE("abcdef", 2, 3)
PRINT REMOVE("abcdef", 6, 10)
PRINT REMOVE("abcdef", 99, 2)
PRINT REMOVE("abcdef", 3, 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["<abcd", "ab-cd", "abcd!", "aef", "abcde", "abcdef", "abcdef"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPEAT(\"ha\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPEAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadLeftReportsNegativeWidth()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADLEFT(\"7\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADLEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadRightReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADRIGHT(\"7\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADRIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInsertReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSERT(\"abc\", 0, \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRemoveReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REMOVE(\"abc\", 1, -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinStringReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT STRING(3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSearchFunctionsUseOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT CONTAINS("Hello", "ELL")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT CONTAINS("Hello", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceUsesOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT REPLACE("one two two", "two", "three")
PRINT REPLACE("One one", "one", "two")
PRINT REPLACE("abc", "x", "y")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one three three", "One two", "abc"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReturnsOneBasedPositions()
    {
        const string text = """
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTR("banana", "NA")
PRINT INSTR(7, "banana", "")
PRINT INSTR(8, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "5", "0", "7", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTR(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReturnsLastOneBasedPosition()
    {
        const string text = """
PRINT INSTRREV("banana", "na")
PRINT INSTRREV("banana", "NA")
PRINT INSTRREV("abc", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "0", "4"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevCanSearchBackFromStartPosition()
    {
        const string text = """
PRINT INSTRREV(4, "banana", "na")
PRINT INSTRREV(2, "banana", "na")
PRINT INSTRREV(100, "banana", "na")
PRINT INSTRREV(4, "banana", "")
PRINT INSTRREV(100, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "0", "5", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTRREV(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTRREV", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscAndChrUseUnicodeCodePoints()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
PRINT ASC(CHR(1071))
PRINT ASC(CHR(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["65", "A", "1071", "128512"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterCentersText()
    {
        const string text = """
PRINT "[" + CENTER("A", 3) + "]"
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT "[" + CENTER("AB", 5, ".") + "]"
PRINT "[" + CENTER("Hello", 3, ".") + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[ A ]", "[.A..]", "[.AB..]", "[Hello]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT CENTER(\"A\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    public void Execute_BuiltinCenterReportsNegativeWidth(string widthText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CENTER(\"A\", {widthText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASC(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHR({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReturnsNonOverlappingCount()
    {
        const string text = """
PRINT SUBSTRCOUNT("banana", "na")
PRINT SUBSTRCOUNT("aaaa", "aa")
PRINT SUBSTRCOUNT("banana", "NA")
PRINT SUBSTRCOUNT("", "a")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReportsEmptyFragment()
    {
        var (_, _, result) = ExecuteProgram("PRINT SUBSTRCOUNT(\"abc\", \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SUBSTRCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fragment", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinReplaceReportsEmptyOldText()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPLACE(\"abc\", \"\", \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexIsMatchSupportsModes()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXISMATCH("one" + CHR(10) + "two", "^two", "m")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchReturnsFirstMatchOrEmptyString()
    {
        const string text = """
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXMATCH("A12", "a\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "[]", "A12"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReturnsRequestedGroup()
    {
        const string text = """
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXEXTRACT("a12", "([A-Z])(\d+)", 1, "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "[]", "a"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceSupportsGroupReferencesAndCount()
    {
        const string text = """
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#")
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one # two #", "one # two 34", "A-12 B-34", "# b34"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexCountReturnsMatchCount()
    {
        const string text = """
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
PRINT REGEXCOUNT("text", "\d+")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchesReturnsAllMatches()
    {
        const string text = """
DIM values() AS STRING
values = REGEXMATCHES("Order 42 and 100", "\d+")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("Order", "\d+")
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "42", "100", "2", "A12", "b34", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReturnsRequestedGroupForEveryMatch()
    {
        const string text = """
DIM values() AS STRING
values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 2)
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("a12 b34", "([A-Z])(\d+)", 1, "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 3)
PRINT "[" + values(1) + "]"
PRINT "[" + values(2) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "12", "34", "2", "a", "b", "[]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexSplitReturnsStringArray()
    {
        const string text = """
DIM parts() AS STRING
parts = REGEXSPLIT("one, two;three", "[,;]\s*")
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(2)
PRINT parts(3)

parts = REGEXSPLIT("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(parts)
PRINT "[" + parts(1) + "]"
PRINT "[" + parts(2) + "]"
PRINT "[" + parts(3) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "one", "two", "three", "3", "[]", "[ ]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsInvalidPattern()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"[\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsUnknownMode()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"a\", \"z\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXREPLACE(\"abc\", \"a\", \"x\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACT(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACTALL(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACTALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsWritesAndChecksPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");
            var dirPath = Path.Combine(root, "created");

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM dirPath AS STRING = "{ToBasicString(dirPath)}"

PRINT FILEEXISTS(filePath)
PRINT WRITEFILE(filePath, "Hello")
PRINT FILEEXISTS(filePath)
PRINT READFILE(filePath)
PRINT APPENDFILE(filePath, "!")
PRINT READFILE(filePath)
PRINT FILESIZE(filePath) > 0
PRINT LEN(FILEDATE(filePath)) > 0
PRINT MAKEDIR(dirPath)
PRINT DIREXISTS(dirPath)
PRINT PATHEXISTS(dirPath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["FALSE", "TRUE", "TRUE", "Hello", "TRUE", "Hello!", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "legacy.txt");
            File.WriteAllBytes(filePath, Encoding.GetEncoding(1251).GetBytes("Привет"));

            var text = $"""PRINT READFILE("{ToBasicString(filePath)}")""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["Привет"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemPathHelpersReturnExpectedValues()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "notes.txt");

        var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM filePath AS STRING = "{ToBasicString(filePath)}"

PRINT COMBINEPATH(root, "notes.txt") = filePath
PRINT FILENAME(filePath)
PRINT EXTNAME(filePath)
PRINT FULLPATH(filePath) = filePath
PRINT LEN(DIRNAME(filePath)) > 0
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "notes.txt", ".txt", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "a.txt"), "A");
            File.WriteAllText(Path.Combine(root, "b.log"), "B");
            Directory.CreateDirectory(Path.Combine(root, "alpha"));
            Directory.CreateDirectory(Path.Combine(root, "beta"));

            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM files() AS STRING
DIM textFiles() AS STRING
DIM dirs() AS STRING
DIM matchingDirs() AS STRING
DIM emptyFiles() AS STRING

files = GETFILES(root)
textFiles = GETFILES(root, "*.txt")
dirs = GETDIRS(root)
matchingDirs = GETDIRS(root, "b*")
emptyFiles = GETFILES(root, "*.none")

PRINT ARRAYLENGTH(files)
PRINT FILENAME(files(1))
PRINT FILENAME(files(2))
PRINT ARRAYLENGTH(textFiles)
PRINT FILENAME(textFiles(1))
PRINT ARRAYLENGTH(dirs)
PRINT FILENAME(dirs(1))
PRINT FILENAME(dirs(2))
PRINT ARRAYLENGTH(matchingDirs)
PRINT FILENAME(matchingDirs(1))
PRINT ARRAYISALLOCATED(emptyFiles)
PRINT ARRAYLENGTH(emptyFiles)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["2", "a.txt", "b.log", "1", "a.txt", "2", "alpha", "beta", "1", "beta", "TRUE", "0"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsAllLines()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");
            File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta", Encoding.UTF8);

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM lines() AS STRING

lines = READALLLINES(filePath)

PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
PRINT lines(1)
PRINT lines(2)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "2", "1", "2", "alpha", "beta"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReadsAllLinesFromEmptyFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "empty.txt");
            File.WriteAllText(filePath, string.Empty, Encoding.UTF8);

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM lines() AS STRING

lines = READALLLINES(filePath)

PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingFileForReadAllLines()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"), "missing.txt");

        var text = $"""
DIM lines() AS STRING
lines = READALLLINES("{ToBasicString(filePath)}")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READALLLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsEmptyEnumerationPattern()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM files() AS STRING
files = GETFILES(root, "")
""";

            var (_, _, result) = ExecuteProgram(text);

            var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
            Assert.Contains("GETFILES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "notes.txt");

            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"

PRINT WRITEFILE(filePath, "Hello")
PRINT PATHROOT(filePath) <> ""
PRINT ISABSOLUTEPATH(filePath)
PRINT LEN(DRIVENAME(filePath)) > 0
PRINT LEN(DRIVEFORMAT(filePath)) > 0
PRINT LEN(DRIVETYPE(filePath)) > 0
PRINT TOTALSPACE(filePath) > 0
PRINT TOTALFREESPACE(filePath) > 0
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var filePath = Path.Combine(root, "notes.txt");
        File.WriteAllText(filePath, "data");
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Hidden | FileAttributes.ReadOnly);

        try
        {
            var text = $"""
DIM filePath AS STRING = "{ToBasicString(filePath)}"
DIM dirPath AS STRING = "{ToBasicString(root)}"

PRINT LEN(FILECREATEDATE(filePath)) > 0
PRINT LEN(FILEACCESSEDDATE(filePath)) > 0
PRINT LEN(DIRDATE(dirPath)) > 0
PRINT ISREADONLY(filePath)
PRINT ISHIDDEN(filePath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "missing.txt");
        var text = $"""PRINT READFILE("{ToBasicString(filePath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingObjectForIsHidden()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(root, "missing.txt");
        var text = $"""PRINT ISHIDDEN("{ToBasicString(filePath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ISHIDDEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "source.txt");
            var copyPath = Path.Combine(root, "copy.txt");
            var movedPath = Path.Combine(root, "moved.txt");
            var renamedPath = Path.Combine(root, "renamed.txt");

            var text = $"""
DIM sourcePath AS STRING = "{ToBasicString(sourcePath)}"
DIM copyPath AS STRING = "{ToBasicString(copyPath)}"
DIM movedPath AS STRING = "{ToBasicString(movedPath)}"
DIM renamedPath AS STRING = "{ToBasicString(renamedPath)}"

PRINT WRITEFILE(sourcePath, "one")
PRINT COPYFILE(sourcePath, copyPath)
PRINT READFILE(copyPath)
PRINT WRITEFILE(sourcePath, "two")
PRINT COPYFILE(sourcePath, copyPath, TRUE)
PRINT READFILE(copyPath)
PRINT MOVEFILE(copyPath, movedPath)
PRINT FILEEXISTS(copyPath)
PRINT FILEEXISTS(movedPath)
PRINT RENAMEFILE(movedPath, "renamed.txt")
PRINT FILEEXISTS(renamedPath)
PRINT DELETEFILE(renamedPath)
PRINT FILEEXISTS(renamedPath)
PRINT DELETEFILE(renamedPath)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "one", "TRUE", "TRUE", "two", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories()
    {
            var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

        try
        {
            var sourceDir = Path.Combine(root, "source");
            var copiedPath = Path.Combine(root, "copy");
            var movedPath = Path.Combine(root, "moved");
            var renamedDir = Path.Combine(root, "renamed");
            var sourceFile = Path.Combine(sourceDir, "notes.txt");
            var copyFile = Path.Combine(copiedPath, "notes.txt");
            var renamedFile = Path.Combine(renamedDir, "notes.txt");
            var emptyDir = Path.Combine(root, "empty");

            var text = $"""
DIM sourceDir AS STRING = "{ToBasicString(sourceDir)}"
DIM copiedPath AS STRING = "{ToBasicString(copiedPath)}"
DIM movedPath AS STRING = "{ToBasicString(movedPath)}"
DIM renamedDir AS STRING = "{ToBasicString(renamedDir)}"
DIM sourceFile AS STRING = "{ToBasicString(sourceFile)}"
DIM copyFile AS STRING = "{ToBasicString(copyFile)}"
DIM renamedFile AS STRING = "{ToBasicString(renamedFile)}"
DIM emptyDir AS STRING = "{ToBasicString(emptyDir)}"

PRINT MAKEDIR(sourceDir)
PRINT WRITEFILE(sourceFile, "dir")
PRINT COPYDIR(sourceDir, copiedPath)
PRINT READFILE(copyFile)
PRINT MOVEDIR(copiedPath, movedPath)
PRINT DIREXISTS(copiedPath)
PRINT DIREXISTS(movedPath)
PRINT RENAMEDIR(movedPath, "renamed")
PRINT READFILE(renamedFile)
PRINT MAKEDIR(emptyDir)
PRINT DELETEDIR(emptyDir)
PRINT DIREXISTS(emptyDir)
PRINT DELETEDIR(emptyDir)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "dir", "TRUE", "FALSE", "TRUE", "TRUE", "dir", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsCopyFileMissingSource()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(root, "missing.txt");
        var destinationPath = Path.Combine(root, "copy.txt");
        var text = $"""PRINT COPYFILE("{ToBasicString(sourcePath)}", "{ToBasicString(destinationPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("COPYFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    #if false
    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            const string sourceText = "# РџР»Р°РЅ СЂР°Р±РѕС‚\r\n\r\n## Р§С‚Рѕ СѓР¶Рµ Р·Р°РІРµСЂС€РµРЅРѕ\r\n\r\n### Следующий точный шаг\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal("# План работ\r\n\r\n## Что уже завершено\r\n\r\n### Следующий точный шаг\r\n", File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    #endif

    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            const string expectedText = "# План работ\r\n\r\n## Что уже завершено\r\n\r\n### Следующий точный шаг\r\n";
            var sourceText =
                CreateUtf8Cp1251Mojibake("# План работ", passes: 2) + "\r\n\r\n" +
                CreateUtf8Cp1251Mojibake("## Что уже завершено", passes: 1) + "\r\n\r\n" +
                "### Следующий точный шаг\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            const string sourceText =
                "# План работ\r\n" +
                "- при будущих правках вёрстки manual проверять отдельно desktop- и mobile-поведение боковой навигации.\r\n" +
                "- Добавлена четвёртая чистая функция над одномерными массивами.\r\n" +
                "- `FOR`-control variable теперь объявляется самой строкой `FOR` и живёт только внутри цикла.\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 0",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(sourceText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsMixedUtf8AndWindows1251Bytes()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "PLANS.md");
            var workPath = Path.Combine(root, "PLANS.axiom-work.md");
            var windows1251 = Encoding.GetEncoding(1251);
            var sourceBytes = Encoding.UTF8.GetBytes("# План работ\r\n")
                .Concat(windows1251.GetBytes("## Обновление плана\r\n"))
                .ToArray();
            const string expectedText = "# План работ\r\n## Обновление плана\r\n";
            File.WriteAllBytes(sourcePath, sourceBytes);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 1",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "ARCHITECTURE.md");
            var workPath = Path.Combine(root, "ARCHITECTURE.axiom-work.md");
            var sourceText =
                "## Обновление 2026-04-21 - " + CreateUtf8Cp1251Mojibake("Смешанный заголовок", passes: 1) + "\r\n" +
                "- helper-" + CreateUtf8Cp1251Mojibake("слой", passes: 1) + " и " + CreateUtf8Cp1251Mojibake("граница", passes: 2) + "\r\n" +
                "- Уже исправленная строка\r\n";
            const string expectedText =
                "## Обновление 2026-04-21 - Смешанный заголовок\r\n" +
                "- helper-слой и граница\r\n" +
                "- Уже исправленная строка\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "SPECIFICATION.md");
            var workPath = Path.Combine(root, "SPECIFICATION.axiom-work.md");
            var sourceText =
                "# " + CreateUtf8Cp1251Mojibake("Спецификация", passes: 1) + " Axiom\r\n" +
                "- string-first " + CreateUtf8Cp1251Mojibake("договор", passes: 2) + "\r\n" +
                "- Уже исправленная строка\r\n";
            const string expectedText =
                "# Спецификация Axiom\r\n" +
                "- string-first договор\r\n" +
                "- Уже исправленная строка\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 2",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 0",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(expectedText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_SmokeSampleReportsUnrecoverableReplacementMarkers()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-smoke-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "WORKLOG2.md");
            var workPath = Path.Combine(root, "WORKLOG2.axiom-work.md");
            const string sourceText =
                "### пїЅпїЅпїЅпїЅ\r\n" +
                "- normal ASCII line\r\n";
            File.WriteAllText(sourcePath, sourceText, Encoding.UTF8);

            var samplePath = Path.Combine(FindRepositoryRoot(), "samples", "smoke", "repair_plans_mojibake.axm");
            var text = SetRepairSamplePaths(File.ReadAllText(samplePath, Encoding.UTF8), sourcePath, workPath);

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(
                [
                    "Копирование исходного файла в рабочую копию: TRUE",
                    "Исправлено строк с восстановимой битой кодировкой: 0",
                    "Строк с невосстановимыми маркерами пїЅ/U+FFFD: 1",
                    "Запись исправленной рабочей копии: TRUE",
                ],
                host.Output);
            Assert.Equal(sourceText, File.ReadAllText(workPath, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var nested = Path.Combine(root, "nested");
            var firstFile = Path.Combine(root, "a.txt");
            var secondFile = Path.Combine(nested, "b.txt");

            var text = $"""
DIM root AS STRING = "{ToBasicString(root)}"
DIM nested AS STRING = "{ToBasicString(nested)}"
DIM firstFile AS STRING = "{ToBasicString(firstFile)}"
DIM secondFile AS STRING = "{ToBasicString(secondFile)}"

PRINT LEN(CURRENTDIR()) > 0
PRINT MAKEDIR(nested)
PRINT WRITEFILE(firstFile, "1234")
PRINT WRITEFILE(secondFile, "12")
PRINT DIRSIZE(root)
PRINT FREESPACE(root) > 0
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "6", "TRUE"], host.Output);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var nested = Path.Combine(root, "nested");
            var nestedFile = Path.Combine(nested, "notes.txt");

            var text = $"""
DIM oldDir AS STRING = CURRENTDIR()
DIM root AS STRING = "{ToBasicString(root)}"
DIM nested AS STRING = "{ToBasicString(nested)}"
DIM nestedFile AS STRING = "{ToBasicString(nestedFile)}"

PRINT MAKEDIR(nested)
PRINT WRITEFILE(nestedFile, "data")
PRINT CHANGEDIR(root)
PRINT CURRENTDIR() = FULLPATH(root)
PRINT FILEEXISTS("nested\\notes.txt")
PRINT CHANGEDIR(oldDir)
PRINT CURRENTDIR() = FULLPATH(oldDir)
PRINT DELETEDIRRECURSIVE(root)
PRINT DIREXISTS(root)
PRINT DELETEDIRRECURSIVE(root)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(result.Succeeded);
            Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE"], host.Output);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingDirectoryForChangeDir()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var directoryPath = Path.Combine(root, "missing");
        var text = $"""PRINT CHANGEDIR("{ToBasicString(directoryPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHANGEDIR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFileSystemCreatesTemporaryFileAndDirectory()
    {
        const string text = """
DIM tempFile AS STRING = MAKETEMPFILE("sample_")
DIM tempDir AS STRING = MAKETEMPDIR("sample_")
PRINT FILEEXISTS(tempFile)
PRINT DIREXISTS(tempDir)
PRINT FILENAME(tempFile) <> ""
PRINT DIRNAME(tempDir) <> ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinFileSystemReportsMissingDirectoryForDirSize()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-fs-tests", Guid.NewGuid().ToString("N"));
        var directoryPath = Path.Combine(root, "missing");
        var text = $"""PRINT DIRSIZE("{ToBasicString(directoryPath)}")""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("DIRSIZE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRndReturnsDoubleInUnitInterval()
    {
        const string text = """
DIM value AS DOUBLE = RND()
PRINT value >= 0
PRINT value < 1
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Theory]
    [InlineData("PRINT ABS(-5)", "5")]
    [InlineData("PRINT SGN(-5)", "-1")]
    [InlineData("PRINT MIN(10, 3)", "3")]
    [InlineData("PRINT MAX(10, 3)", "10")]
    [InlineData("PRINT CLAMP(15, 1, 10)", "10")]
    [InlineData("PRINT ROUND(SIN(0))", "0")]
    [InlineData("PRINT ROUND(COS(0))", "1")]
    [InlineData("PRINT ROUND(TAN(0))", "0")]
    [InlineData("PRINT ROUND(ATN2(0, 1))", "0")]
    [InlineData("PRINT SQR(9)", "3")]
    [InlineData("PRINT CBRT(27)", "3")]
    [InlineData("PRINT EXP(0)", "1")]
    [InlineData("PRINT LOG(1)", "0")]
    [InlineData("PRINT LOG10(1000)", "3")]
    [InlineData("PRINT LOG2(8)", "3")]
    [InlineData("PRINT POW(2, 3)", "8")]
    [InlineData("PRINT ROUND(ASIN(0.5) * 1000)", "524")]
    [InlineData("PRINT ROUND(ACOS(0.5) * 1000)", "1047")]
    [InlineData("PRINT ROUND(ASINH(1) * 1000)", "881")]
    [InlineData("PRINT ROUND(ACOSH(2) * 1000)", "1317")]
    [InlineData("PRINT ROUND(ATANH(0.5) * 1000)", "549")]
    [InlineData("PRINT ROUND(SINH(1) * 1000)", "1175")]
    [InlineData("PRINT ROUND(COSH(1) * 1000)", "1543")]
    [InlineData("PRINT ROUND(TANH(1) * 1000)", "762")]
    [InlineData("PRINT SETSIGN(5, -1)", "-5")]
    [InlineData("PRINT FLOOR(-3.2)", "-4")]
    [InlineData("PRINT CEIL(-3.2)", "-3")]
    [InlineData("PRINT ROUND(-2.5)", "-3")]
    [InlineData("PRINT FIX(-2.5)", "-2")]
    public void Execute_MathBuiltinsReturnExpectedOutput(string source, string expectedOutput)
    {
        var (_, host, result) = ExecuteProgram(source);

        Assert.True(result.Succeeded);
        Assert.Equal([expectedOutput], host.Output);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinLeftReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT LEFT(\"Hello\", {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("LEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinRightReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT RIGHT(\"Hello\", {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("RIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(22, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_BuiltinMidReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT MID(\"Hello\", 0, 2)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MID", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(20, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("-1")]
    public void Execute_BuiltinMidReportsNonPositiveCount(string countText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT MID(\"Hello\", 1, {countText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("MID", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(23, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_BuiltinRightReturnsFullStringWhenCountExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT RIGHT(\"Hello\", 100)");

        Assert.True(result.Succeeded);
        Assert.Equal(["Hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinLeftReturnsFullStringWhenCountExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEFT(\"Hello\", 100)");

        Assert.True(result.Succeeded);
        Assert.Equal(["Hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinMidReturnsEmptyStringWhenStartExceedsLength()
    {
        var (_, host, result) = ExecuteProgram("PRINT MID(\"Hello\", 10, 2)");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinValReturnsZeroForInvalidText()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"abc\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinIntReportsOverflowAfterFloor()
    {
        var (_, _, result) = ExecuteProgram("PRINT INT(3000000000.5)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(11, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("PRINT SQR(-1)", "SQR", "value")]
    [InlineData("PRINT LOG(0)", "LOG", "value")]
    [InlineData("PRINT LOG10(-10)", "LOG10", "value")]
    [InlineData("PRINT CLAMP(5, 10, 1)", "CLAMP", "high")]
    [InlineData("PRINT POW(-1, 0.5)", "POW", "value")]
    [InlineData("PRINT ASIN(2)", "ASIN", "value")]
    [InlineData("PRINT ACOS(-2)", "ACOS", "value")]
    [InlineData("PRINT ACOSH(0.5)", "ACOSH", "value")]
    [InlineData("PRINT ATANH(1)", "ATANH", "value")]
    public void Execute_MathBuiltinsReportDomainErrors(string source, string functionName, string argumentName)
    {
        var (_, _, result) = ExecuteProgram(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains(functionName, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(argumentName, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinFixDiffersFromIntForNegativeFraction()
    {
        const string text = """
PRINT INT(-1.2)
PRINT FIX(-1.2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["-2", "-1"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSetSignPreservesDecimalType()
    {
        const string text = """
DIM value AS DECIMAL = 10.5
PRINT SETSIGN(value, 1)
PRINT SETSIGN(value, -1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10.5", "-10.5"], host.Output);
    }

    [Fact]
    public void Execute_MathBuiltinsReportDoubleOverflow()
    {
        var (_, _, result) = ExecuteProgram("PRINT EXP(10000)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_HyperbolicMathBuiltinsReportDoubleOverflow()
    {
        var (_, _, result) = ExecuteProgram("PRINT COSH(10000)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_InputSingleVariable()
    {
        const string text = """
DIM name AS STRING
INPUT name
PRINT name
""";

        var (_, host, result) = ExecuteProgram(text, "Ada");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Execute_InputMultipleVariables()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
PRINT firstName; age
""";

        var (_, host, result) = ExecuteProgram(text, "Ada, 42");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada42"], host.Output);
        Assert.Equal(2, Assert.Single(host.InputRequests).ValueCount);
    }

    [Fact]
    public void Execute_InputLongVariable()
    {
        const string text = """
DIM value AS LONG
INPUT value
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text, "21474836479");

        Assert.True(result.Succeeded);
        Assert.Equal(["21474836479"], host.Output);
    }

    [Fact]
    public void Execute_InputDecimalVariable()
    {
        const string text = """
DIM value AS DECIMAL
INPUT value
PRINT value
""";

        var (_, host, result) = ExecuteProgram(text, "125.75");

        Assert.True(result.Succeeded);
        Assert.Equal(["125.75"], host.Output);
    }

    [Fact]
    public void Execute_InputWithPromptCapturesRequestMetadata()
    {
        const string text = """
DIM name AS STRING
INPUT "Name"; name
PRINT name
""";

        var (_, host, result) = ExecuteProgram(text, "Ada");

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Equal("Name", request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Execute_OpenReadLineAndEof()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + Environment.NewLine + "omega");

        var text = $"""
DIM f AS FILE
DIM line AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
WHILE NOT EOF(f)
    line = READLINE(f)
    PRINT "[" + line + "]"
END WHILE
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[alpha]", "[]", "[omega]"], host.Output);
    }

    [Fact]
    public void Execute_FileStateBuiltins()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "state.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
PRINT ISOPEN(f)
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT FILEPATH(f)
CLOSE f
PRINT ISOPEN(f)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "INPUT", filePath, "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_FilePositionAndLengthForInput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-input.txt");
        const string content = "alpha\r\nbeta";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var firstLineBytes = Encoding.UTF8.GetByteCount("alpha\r\n");
        var totalBytes = Encoding.UTF8.GetByteCount(content);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
PRINT FILEPOS(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", totalBytes.ToString(), "alpha", firstLineBytes.ToString(), "beta", totalBytes.ToString()], host.Output);
    }

    [Fact]
    public void Execute_ReadToEndReturnsRemainingText()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtoend.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "beta"], host.Output);
    }

    [Fact]
    public void Execute_ReadTextCountsCharactersWhilePositionStaysByteBased()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readtext.txt");
        const string content = "AБВ";
        File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var firstTwoCharsBytes = Encoding.UTF8.GetByteCount("AБ");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READTEXT(f, 2)
PRINT FILEPOS(f)
PRINT READTOEND(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["AБ", firstTwoCharsBytes.ToString(), "В"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReadsUpToCountAndAdvancesPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");

        var text = $"""
DIM f AS FILE
DIM first() AS STRING
DIM rest() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
first = READLINES(f, 2)
rest = READLINES(f, 2)
PRINT ARRAYLENGTH(first)
PRINT first(1)
PRINT first(2)
PRINT LINENUMBER(f)
PRINT ARRAYLENGTH(rest)
PRINT rest(1)
PRINT EOF(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "alpha", "beta", "3", "1", "gamma", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_ReadLinesReturnsEmptyArrayForZeroCountAndEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "readlines-empty.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, 0)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
PRINT LINENUMBER(f)
lines = READLINES(f, 1)
lines = READLINES(f, 1)
PRINT ARRAYISALLOCATED(lines)
PRINT ARRAYLENGTH(lines)
PRINT LBOUND(lines)
PRINT UBOUND(lines)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0", "1", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SkipLineAndLineNumberTrackInputPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "lines.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma");
        var secondLinePosition = Encoding.UTF8.GetByteCount("alpha" + Environment.NewLine);

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT LINENUMBER(f)
PRINT SKIPLINE(f)
PRINT LINENUMBER(f)
PRINT READLINE(f)
PRINT LINENUMBER(f)
PRINT SEEKFILE(f, {secondLinePosition})
PRINT LINENUMBER(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "TRUE", "2", "beta", "3", "TRUE", "2"], host.Output);
    }

    [Fact]
    public void Execute_NewLineWritesEmptyLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "newline.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "alpha")
PRINT NEWLINE(f)
PRINT WRITELINE(f, "beta")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
        Assert.Equal($"alpha{Environment.NewLine}{Environment.NewLine}beta{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenOutputAndWriteLine()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITELINE(f, "first")
PRINT WRITELINE(f, "second")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
        Assert.Equal($"first{Environment.NewLine}second{Environment.NewLine}", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_OpenAppendAndWrite()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "A");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT WRITE(f, "B")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE"], host.Output);
        Assert.Equal("AB", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FlushMakesBufferedWriteVisible()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "flush.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT WRITE(f, "A")
PRINT FILESIZE("{ToBasicString(filePath)}")
PRINT FLUSH(f)
PRINT FILESIZE("{ToBasicString(filePath)}")
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "TRUE", "1"], host.Output);
        Assert.Equal("A", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_FilePositionAndSeekForOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "position-output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT FILELENGTH(f)
PRINT WRITE(f, "ABCDE")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
PRINT SEEKFILE(f, 2)
PRINT FILEPOS(f)
PRINT WRITE(f, "Z")
PRINT FILEPOS(f)
PRINT FILELENGTH(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "TRUE", "5", "5", "TRUE", "2", "TRUE", "3", "5"], host.Output);
        Assert.Equal("ABZDE", File.ReadAllText(filePath));
    }

    [Fact]
    public void Execute_RewindMovesFileBackToBeginning()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "rewind.txt");
        File.WriteAllText(filePath, "alpha" + Environment.NewLine + "beta");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT READLINE(f)
PRINT REWIND(f)
PRINT FILEPOS(f)
PRINT READLINE(f)
CLOSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["alpha", "TRUE", "0", "alpha"], host.Output);
    }

    [Fact]
    public void Execute_CloseReportsClosedFile()
    {
        const string text = """
DIM f AS FILE
CLOSE f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CLOSE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_OpenReportsAlreadyOpenFileVariable()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var firstPath = Path.Combine(root, "first.txt");
        var secondPath = Path.Combine(root, "second.txt");
        File.WriteAllText(firstPath, "1");
        File.WriteAllText(secondPath, "2");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(firstPath)}" FOR INPUT AS f
OPEN "{ToBasicString(secondPath)}" FOR INPUT AS f
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("OPEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WriteReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT WRITE(f, "beta")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("WRITE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_FlushReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT FLUSH(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("FLUSH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("записи", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadToEndReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
PRINT READTOEND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTOEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsWrongMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "output.txt");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR OUTPUT AS f
lines = READLINES(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("чтения", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadLinesReportsNegativeCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "input.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
DIM lines() AS STRING
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
lines = READLINES(f, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READLINES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SkipLineReportsEndOfFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "skip-eof.txt");
        File.WriteAllText(filePath, "alpha");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SKIPLINE(f)
PRINT SKIPLINE(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SKIPLINE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("конец", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReadTextReportsInvalidUtf8Boundary()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "utf8-boundary.txt");
        File.WriteAllText(filePath, "AБ", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 2)
PRINT READTEXT(f, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("READTEXT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UTF-8", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT SEEKFILE(f, 0)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_RewindReportsAppendMode()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "append-rewind.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR APPEND AS f
PRINT REWIND(f)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REWIND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APPEND", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_SeekFileReportsPositionBeyondEnd()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-file-io-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "beyond-end.txt");
        File.WriteAllText(filePath, "AB");

        var text = $"""
DIM f AS FILE
OPEN "{ToBasicString(filePath)}" FOR INPUT AS f
PRINT SEEKFILE(f, 3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SEEKFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("position", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_ReportsInvalidMultiInputHandling()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
""";

        var (_, _, result) = ExecuteProgram(text, "Ada");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDivisionByZero()
    {
        var (_, _, result) = ExecuteProgram("PRINT 10 / 0");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("PRINT 10 \\ 0")]
    [InlineData("PRINT 10 MOD 0")]
    public void Execute_ReportsDivisionByZeroForIntegralOperators(string source)
    {
        var (_, _, result) = ExecuteProgram(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_NestedRuntimeErrorReportsCallChain()
    {
        const string text = """
PRINT Outer()

FUNCTION Outer() AS DOUBLE
    RETURN Inner()

    FUNCTION Inner() AS DOUBLE
        RETURN 10 / 0
    END FUNCTION
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("Цепочка вызовов", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("локальная функция 'Inner'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("функция 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("объявлена внутри функции 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(7, diagnostic.Location.Line);
        Assert.Equal(19, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerArithmeticOverflow()
    {
        const string text = """
DIM value AS INTEGER = 2147483647
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerDivisionOverflow()
    {
        const string text = """
DIM value AS INTEGER = -2147483648
PRINT value \ -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongArithmeticOverflow()
    {
        const string text = """
DIM value AS LONG = 9223372036854775807
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongRemainderOverflow()
    {
        const string text = """
DIM value AS LONG = -9223372036854775807
value = value - 1
PRINT value MOD -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDoubleArithmeticOverflow()
    {
        const string text = """
DIM value AS DOUBLE = VAL("1e308")
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDecimalArithmeticOverflow()
    {
        const string text = """
DIM value AS DECIMAL = 79228162514264337593543950335.0
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DECIMAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsArrayBoundsViolation()
    {
        const string text = """
DIM arr(1 TO 2) AS INTEGER
PRINT arr(3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
        Assert.Contains("arr", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsInvalidInputConversion()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var (_, _, result) = ExecuteProgram(text, "oops");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_TypeFieldsAndNestedRecords()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
PRINT user.Name
PRINT user.Age
user.Name = "Ada"
user.Age = 36
user.Home.City = "London"
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London"], host.Output);
    }

    [Fact]
    public void Execute_TypeAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM second AS Person
first.Name = "Ada"
second = first
first.Name = "Grace"
PRINT second.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementsAndFields()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM people(1 TO 2) AS Person
PRINT people(1).Name
PRINT people(1).Age
people(1).Name = "Ada"
people(1).Age = 36
people(1).Home.City = "London"
people(2).Name = "Grace"
PRINT people(1).Name
PRINT people(1).Age
PRINT people(1).Home.City
PRINT people(2).Name
PRINT ARRAYLENGTH(people)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London", "Grace", "2"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM copy AS Person
DIM people(1 TO 1) AS Person
first.Name = "Ada"
people(1) = first
first.Name = "Grace"
copy = people(1)
people(1).Name = "Alan"
PRINT people(1).Name
PRINT copy.Name
PRINT first.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Alan", "Ada", "Grace"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayField()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2)
user.Scores(1) = 10
user.Scores(2) = 20
PRINT user.Scores(1)
PRINT user.Scores(2)
ERASE user.Scores
PRINT ARRAYISALLOCATED(user.Scores)
REDIM user.Scores(5 TO 5)
PRINT user.Scores(5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayFieldOnArrayElement()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(2 TO 3)
people(1).Scores(2) = 7
people(1).Scores(3) = 9
PRINT people(1).Scores(2)
PRINT people(1).Scores(3)
REDIM PRESERVE people(1).Scores(3 TO 4)
PRINT people(1).Scores(3)
PRINT people(1).Scores(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayField()
    {
        const string text = """
TYPE Person
    Scores(1 TO 3) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
DIM people(1 TO 1) AS Person
PRINT first.Scores(1)
first.Scores(1) = 10
first.Scores(3) = 30
second = first
first.Scores(1) = 99
people(1) = second
people(1).Scores(2) = 20
PRINT second.Scores(1)
PRINT second.Scores(3)
PRINT people(1).Scores(1)
PRINT people(1).Scores(2)
PRINT ARRAYLENGTH(people(1).Scores)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBounds()
    {
        const string text = """
CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBoundsDeclaredLater()
    {
        const string text = """
TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_DimFixedLengthStringPadsAndTruncates()
    {
        const string text = """
DIM code AS STRING * 4
PRINT "[" + code + "]"
code = "AB"
PRINT "[" + code + "]"
code = "Alphabet"
PRINT "[" + code + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedLengthStringFieldPadsAndTruncates()
    {
        const string text = """
TYPE Employee
    Name AS STRING * 5
    Role AS STRING * 8
END TYPE

DIM user AS Employee
PRINT "[" + user.Name + "]"
user.Name = "Ada"
user.Role = "Analyst"
PRINT "[" + user.Name + "]"
PRINT "[" + user.Role + "]"
user.Role = "Architect"
PRINT "[" + user.Role + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[     ]", "[Ada  ]", "[Analyst ]", "[Architec]"], host.Output);
    }

    [Fact]
    public void Execute_TypeGroupedFields()
    {
        const string text = """
TYPE Person
    Name, Alias AS STRING
    Scores(), Flags(1 TO 2) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Name
PRINT first.Alias
PRINT first.Flags(1)
REDIM first.Scores(1 TO 2)
first.Name = "Ada"
first.Alias = "Analyst"
first.Scores(1) = 10
first.Scores(2) = 20
first.Flags(2) = 7
second = first
first.Alias = "Changed"
first.Scores(1) = 99
first.Flags(2) = 42
PRINT second.Name
PRINT second.Alias
PRINT second.Scores(1)
PRINT second.Scores(2)
PRINT second.Flags(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "", "0", "Ada", "Analyst", "99", "20", "7"], host.Output);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, TimeZoneInfo.Local, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, localTimeZone, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones,
        params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs, localNow, localTimeZone, namedTimeZones);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static string ToBasicString(string value)
    {
        return value.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static string SetRepairSamplePaths(string text, string sourcePath, string workPath)
    {
        const string sourceDeclarationPrefix = "DIM sourcePath AS STRING = ";
        const string workDeclarationPrefix = "DIM workPath AS STRING = ";
        var sourceReplacement = sourceDeclarationPrefix + "\"" + ToBasicString(sourcePath) + "\"";
        var workReplacement = workDeclarationPrefix + "\"" + ToBasicString(workPath) + "\"";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sourcePathSet = false;
        var workPathSet = false;

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith(sourceDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = sourceReplacement;
                sourcePathSet = true;
                continue;
            }

            if (lines[index].StartsWith(workDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = workReplacement;
                workPathSet = true;
            }
        }

        if (!sourcePathSet || !workPathSet)
        {
            throw new InvalidOperationException("Could not locate source or work path declarations in repair sample.");
        }

        return string.Join("\n", lines);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Axiom.sln.");
    }

    private static string CreateUtf8Cp1251Mojibake(string text, int passes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passes);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var utf8 = Encoding.UTF8;
        var windows1251 = Encoding.GetEncoding(1251);
        var current = text;

        for (var i = 0; i < passes; i++)
        {
            current = windows1251.GetString(utf8.GetBytes(current));
        }

        return current;
    }

    private static TimeZoneInfo CreateTestSeasonalTimeZone()
    {
        var daylightDelta = TimeSpan.FromHours(1);
        var daylightTransitionStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            5,
            DayOfWeek.Sunday);
        var daylightTransitionEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0),
            10,
            5,
            DayOfWeek.Sunday);
        var adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2099, 12, 31),
            daylightDelta,
            daylightTransitionStart,
            daylightTransitionEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Axiom Runtime Tests Seasonal",
            TimeSpan.FromHours(2),
            "Axiom Runtime Tests Seasonal",
            "Axiom Runtime Tests Standard",
            "Axiom Runtime Tests Daylight",
            [adjustmentRule],
            disableDaylightSavingTime: false);
    }

    private static TimeZoneInfo CreateTestFixedTimeZone(string id, int offsetHours)
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            id,
            TimeSpan.FromHours(offsetHours),
            id,
            id);
    }

    private static IReadOnlyDictionary<string, TimeZoneInfo> CreateNamedTimeZones(params TimeZoneInfo[] timeZones)
    {
        return timeZones.ToDictionary(static timeZone => timeZone.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TestRuntimeHost : IRuntimeHost
    {
        private readonly Queue<string> _inputs;
        private readonly DateTimeOffset _localNow;
        private readonly TimeZoneInfo _localTimeZone;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _namedTimeZones;

        public TestRuntimeHost(
            IEnumerable<string> inputs,
            DateTimeOffset? localNow = null,
            TimeZoneInfo? localTimeZone = null,
            IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null)
        {
            _inputs = new Queue<string>(inputs);
            _localNow = localNow ?? DateTimeOffset.Now;
            _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;
            _namedTimeZones = namedTimeZones ?? new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public List<InputRequest> InputRequests { get; } = [];

        public List<string> Output { get; } = [];

        public string ReadInput(InputRequest request)
        {
            InputRequests.Add(request);
            return _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;
        }

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }

        public DateTimeOffset GetLocalNow() => _localNow;

        public TimeZoneInfo GetLocalTimeZone() => _localTimeZone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = _localTimeZone;
                return true;
            }

            if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = TimeZoneInfo.Utc;
                return true;
            }

            return _namedTimeZones.TryGetValue(id, out timeZone!);
        }
    }
}
