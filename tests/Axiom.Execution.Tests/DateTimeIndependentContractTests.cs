namespace Axiom.Execution.Tests;

public sealed class DateTimeIndependentContractTests
{
    [Fact]
    public void CalendarBoundariesUseGregorianRules()
    {
        Check("""
PRINT ISLEAPYEAR(1900)
PRINT ISLEAPYEAR(2000)
PRINT ISLEAPYEAR(2100)
PRINT DAYSINMONTH(2000, 2)
PRINT DAYSINMONTH(1900, 2)
PRINT DATESERIAL(1, 1, 1)
PRINT DATESERIAL(9999, 12, 31)
PRINT DATEADD("m", 1, DATESERIAL(2024, 1, 31))
PRINT DATEADD("yyyy", 1, DATESERIAL(2024, 2, 29))
PRINT DATEADD("m", -1, DATESERIAL(2024, 3, 31))
PRINT STARTOFMONTH(DATESERIAL(2024, 2, 29))
PRINT ENDOFMONTH(DATESERIAL(2024, 2, 1))
PRINT STARTOFYEAR(DATESERIAL(9999, 12, 31))
PRINT ENDOFYEAR(DATESERIAL(1, 1, 1))
PRINT DATEDIFF("d", DATESERIAL(2000, 3, 1), DATESERIAL(2000, 2, 28))
PRINT DATEPART("y", DATESERIAL(2000, 12, 31))
PRINT DATEPART("w", DATESERIAL(2024, 1, 1))
""", ["FALSE", "TRUE", "FALSE", "29", "28", "0001-01-01", "9999-12-31", "2024-02-29", "2025-02-28", "2024-02-29", "2024-02-01", "2024-02-29", "9999-01-01", "0001-12-31", "-2", "366", "1"]);
    }

    [Fact]
    public void TimeWrapsAndDiffCountsBoundaries()
    {
        Check("""
PRINT TIMESERIAL(0, 0, 0)
PRINT TIMESERIAL(23, 59, 59)
PRINT DATEADD("s", 1, TIMESERIAL(23, 59, 59))
PRINT DATEADD("s", -1, TIMESERIAL(0, 0, 0))
PRINT DATEDIFF("h", TIMESERIAL(0, 59, 59), TIMESERIAL(1, 0, 0))
PRINT DATEDIFF("s", TIMESERIAL(23, 59, 59), TIMESERIAL(0, 0, 0))
PRINT DATEPART("n", TIMESERIAL(23, 59, 59))
""", ["00:00:00", "23:59:59", "00:00:00", "23:59:59", "1", "-86399", "59"]);
    }

    [Fact]
    public void InvalidCalendarAndArithmeticRemainCatchable()
    {
        foreach (var call in new[] {
            "DATESERIAL(1900, 2, 29)", "DATESERIAL(2024, 4, 31)",
            "DATESERIAL(0, 1, 1)", "DATESERIAL(10000, 1, 1)",
            "DATESERIAL(2024, 13, 1)", "DATESERIAL(2024, 1, 0)",
            "TIMESERIAL(24, 0, 0)", "TIMESERIAL(0, 60, 0)", "TIMESERIAL(0, 0, 60)",
            "DATEADD(\"d\", -1, DATESERIAL(1, 1, 1))",
            "DATEADD(\"d\", 1, DATESERIAL(9999, 12, 31))",
            "DATEADD(\"q\", 2147483647, DATESERIAL(2024, 1, 1))",
            "DATEPART(\"h\", DATESERIAL(2024, 1, 1))",
            "DATEADD(\"d\", 1, TIMESERIAL(0, 0, 0))" })
            Check($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT \"Продолжение\"", ["MB3007", "Продолжение"]);
    }

    [Fact]
    public void ExplicitZoneDistinguishesMissingAndRepeatedLocalTimes()
    {
        Check("""
DIM moment AS DATETIME
moment = "2024-01-15 12:00:00"
PRINT UTCOFFSET(moment, "America/New_York")
PRINT TOUTC(moment, "America/New_York")
moment = "2024-07-15 12:00:00"
PRINT UTCOFFSET(moment, "America/New_York")
PRINT TOUTC(moment, "America/New_York")
moment = "2024-11-03 05:30:00"
PRINT FROMUTC(moment, "America/New_York")
moment = "2024-11-03 06:30:00"
PRINT FROMUTC(moment, "America/New_York")
""", ["-300", "2024-01-15 17:00:00", "-240", "2024-07-15 16:00:00", "2024-11-03 01:30:00", "2024-11-03 01:30:00"]);
        foreach (var value in new[] { "2024-03-10 02:30:00", "2024-11-03 01:30:00" })
            foreach (var name in new[] { "TOUTC", "UTCOFFSET" })
                Check($"DIM moment AS DATETIME\n moment = \"{value}\"\nTRY\nPRINT {name}(moment, \"America/New_York\")\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
    }

    [Fact]
    public void TemporalDeclarationsResetOnEachLoopIteration()
    {
        Check("""
FOR i = 1 TO 2
DIM d AS DATE
DIM t AS TIME
DIM dt AS DATETIME
PRINT d
PRINT t
PRINT dt
d = DATESERIAL(2024, 2, 29)
t = TIMESERIAL(23, 59, 59)
dt = "2024-02-29 23:59:59"
NEXT i
""", ["0001-01-01", "00:00:00", "0001-01-01 00:00:00", "0001-01-01", "00:00:00", "0001-01-01 00:00:00"]);
    }

    [Fact]
    public void CanonicalParsingDoesNotDependOnCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "ru-RU", "en-US", "ar-SA", "th-TH" })
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
                Check("""
PRINT DATEVALUE("2000-02-29")
PRINT DATEVALUE("2000-02-29 23:59:59")
PRINT TIMEVALUE("00:00:00")
PRINT TIMEVALUE("2000-02-29 23:59:59")
PRINT ISDATE("1900-02-29")
PRINT ISDATE("2000-02-29")
PRINT ISDATE("23:59:59")
PRINT ISDATE("2000-02-29 23:59:59")
PRINT ISDATE("29.02.2000")
PRINT ISDATE("2000-2-29")
PRINT ISDATE("24:00:00")
PRINT ISDATE("23:59:60")
PRINT ISDATE("2000-02-29T23:59:59Z")
PRINT ISDATE("")
""", ["2000-02-29", "2000-02-29", "00:00:00", "23:59:59", "FALSE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE", "FALSE"]);
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
        foreach (var call in new[] { "DATEVALUE(\"1900-02-29\")", "DATEVALUE(\"23:59:59\")", "TIMEVALUE(\"2000-02-29\")", "TIMEVALUE(\"23:59:60\")" })
            Check($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
    }

    [Fact]
    public void ZoneErrorsAndUtcRangeOverflowAreCatchable()
    {
        foreach (var call in new[] {
            "DATE(\"Axiom/NoSuchZone\")", "TIME(\"\")", "NOW(\"Axiom/NoSuchZone\")",
            "TOUTC(moment, \"Axiom/NoSuchZone\")", "FROMUTC(moment, \"Axiom/NoSuchZone\")", "UTCOFFSET(moment, \"Axiom/NoSuchZone\")" })
            Check($"DIM moment AS DATETIME = \"2024-01-01 00:00:00\"\nTRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
        foreach (var scenario in new[] {
            ("0001-01-01 00:00:00", "TOUTC", "Asia/Tokyo"),
            ("9999-12-31 23:59:59", "TOUTC", "America/New_York"),
            ("0001-01-01 00:00:00", "FROMUTC", "America/New_York"),
            ("9999-12-31 23:59:59", "FROMUTC", "Asia/Tokyo") })
            Check($"DIM moment AS DATETIME = \"{scenario.Item1}\"\nTRY\nPRINT {scenario.Item2}(moment, \"{scenario.Item3}\")\nCATCH err\nPRINT err.Code\nEND TRY", ["MB3007"]);
    }

    [Fact]
    public void CurrentFormsUseOneControlledInstantAndCrossUtcDay()
    {
        var clock = new ControlledClock();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT TIMER()
PRINT UTCOFFSET()
PRINT DATE("LOCAL")
PRINT TIME("LOCAL")
PRINT NOW("LOCAL")
""", runtimeClock: clock));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "2024-03-01", "00:30:00", "2024-03-01 00:30:00", "2024-02-29", "21:30:00", "2024-02-29 21:30:00", "2024-02-29", "21:30:00", "2024-02-29 21:30:00", "1800", "180", "2024-03-01", "00:30:00", "2024-03-01 00:30:00" }, result.Output);
    }

    [Fact]
    public void DateTimeArithmeticAndPartsCoverEveryToken()
    {
        Check("""
DIM moment AS DATETIME = "2024-02-29 23:59:59"
DIM later AS DATETIME = "2025-03-01 00:00:00"
PRINT DATEADD("YYYY", 1, moment)
PRINT DATEADD("q", 1, moment)
PRINT DATEADD("m", -1, moment)
PRINT DATEADD("d", 1, moment)
PRINT DATEADD("h", 1, moment)
PRINT DATEADD("n", 1, moment)
PRINT DATEADD("s", 1, moment)
PRINT DATEDIFF("yyyy", moment, later)
PRINT DATEDIFF("q", moment, later)
PRINT DATEDIFF("m", moment, later)
PRINT DATEDIFF("d", moment, later)
PRINT DATEDIFF("h", moment, later)
PRINT DATEDIFF("n", moment, later)
PRINT DATEDIFF("s", moment, later)
PRINT DATEPART("yyyy", moment)
PRINT DATEPART("q", moment)
PRINT DATEPART("m", moment)
PRINT DATEPART("d", moment)
PRINT DATEPART("y", moment)
PRINT DATEPART("w", moment)
PRINT DATEPART("h", moment)
PRINT DATEPART("n", moment)
PRINT DATEPART("s", moment)
""", ["2025-02-28 23:59:59", "2024-05-29 23:59:59", "2024-01-29 23:59:59", "2024-03-01 23:59:59", "2024-03-01 00:59:59", "2024-03-01 00:00:59", "2024-03-01 00:00:00", "1", "4", "13", "366", "8761", "525601", "31536001", "2024", "1", "2", "29", "60", "4", "23", "59", "59"]);
    }

    [Fact]
    public void InvalidTemporalTokensAndBoundsHaveIdenticalRuntimeLocations()
    {
        foreach (var call in new[] {
            "DATEADD(\"bad\", 1, DATESERIAL(2024, 1, 1))",
            "DATEADD(\"bad\", 1, TIMESERIAL(0, 0, 0))", "DATEADD(\"bad\", 1, moment)",
            "DATEDIFF(\"bad\", DATESERIAL(2024, 1, 1), DATESERIAL(2024, 1, 2))",
            "DATEDIFF(\"bad\", TIMESERIAL(0, 0, 0), TIMESERIAL(0, 0, 1))", "DATEDIFF(\"bad\", moment, moment)",
            "DATEPART(\"bad\", DATESERIAL(2024, 1, 1))", "DATEPART(\"bad\", TIMESERIAL(0, 0, 0))", "DATEPART(\"bad\", moment)",
            "MONTHNAME(0)", "WEEKDAYNAME(8)", "DAYSINMONTH(2024, 0)", "ISLEAPYEAR(0)" })
        {
            var source = "DIM moment AS DATETIME = \"2024-02-29 23:59:59\"\nPRINT " + call;
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.False(second.Succeeded);
            var b = Assert.Single(second.Diagnostics);
            Assert.Equal("MB3007", b.Code);
        }
    }

    [Fact]
    public void TimeArithmeticWrapsLargeIntegerHoursWithoutTickOverflow()
    {
        Check("""
PRINT DATEADD("h", 2147483647, TIMESERIAL(23, 59, 59))
PRINT DATEADD("h", (-2147483647 - 1), TIMESERIAL(0, 0, 0))
PRINT DATEADD("h", 256204778, TIMESERIAL(23, 59, 59))
PRINT DATEADD("n", 2147483647, TIMESERIAL(23, 59, 59))
PRINT DATEADD("n", (-2147483647 - 1), TIMESERIAL(23, 59, 59))
PRINT DATEADD("s", 2147483647, TIMESERIAL(23, 59, 59))
PRINT DATEADD("s", (-2147483647 - 1), TIMESERIAL(23, 59, 59))
""", ["06:59:59", "16:00:00", "01:59:59", "02:06:59", "21:51:59", "03:14:06", "20:45:51"]);
    }

    internal sealed class ControlledClock : Axiom.Runtime.IRuntimeClock
    {
        private readonly TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone("AxiomFixed", TimeSpan.FromHours(3), "AxiomFixed", "AxiomFixed");
        public DateTimeOffset GetLocalNow() => new(2024, 3, 1, 0, 30, 0, TimeSpan.FromHours(3));
        public TimeZoneInfo GetLocalTimeZone() => zone;
        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = id.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : zone;
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase) || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
