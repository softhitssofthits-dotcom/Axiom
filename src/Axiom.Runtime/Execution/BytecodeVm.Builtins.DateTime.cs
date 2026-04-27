using System.Globalization;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private const string CanonicalDateFormat = "yyyy-MM-dd";
    private const string CanonicalTimeFormat = "HH:mm:ss";
    private const string CanonicalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private static readonly CultureInfo DateTimeCulture = CultureInfo.InvariantCulture;
    private static readonly IReadOnlyDictionary<string, IntervalTokenInfo> DateTimeIntervalTokens =
        new Dictionary<string, IntervalTokenInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["yyyy"] = new(DateTimeIntervalKind.Year, true),
            ["q"] = new(DateTimeIntervalKind.Quarter, true),
            ["m"] = new(DateTimeIntervalKind.Month, true),
            ["d"] = new(DateTimeIntervalKind.Day, true),
            ["h"] = new(DateTimeIntervalKind.Hour, false),
            ["n"] = new(DateTimeIntervalKind.Minute, false),
            ["s"] = new(DateTimeIntervalKind.Second, false)
        };
    private static readonly IReadOnlyDictionary<string, PartTokenInfo> DateTimePartTokens =
        new Dictionary<string, PartTokenInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["yyyy"] = new(DateTimePartKind.Year, true),
            ["q"] = new(DateTimePartKind.Quarter, true),
            ["m"] = new(DateTimePartKind.Month, true),
            ["d"] = new(DateTimePartKind.Day, true),
            ["y"] = new(DateTimePartKind.DayOfYear, true),
            ["w"] = new(DateTimePartKind.Weekday, true),
            ["h"] = new(DateTimePartKind.Hour, false),
            ["n"] = new(DateTimePartKind.Minute, false),
            ["s"] = new(DateTimePartKind.Second, false)
        };
    private static readonly string[] CanonicalMonthNames =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];
    private static readonly string[] CanonicalWeekdayNamesMondayFirst =
    [
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday"
    ];

    private enum TemporalValueKind
    {
        Date,
        Time,
        DateTime
    }

    private enum DateTimeIntervalKind
    {
        Year,
        Quarter,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    private enum DateTimePartKind
    {
        Year,
        Quarter,
        Month,
        Day,
        DayOfYear,
        Weekday,
        Hour,
        Minute,
        Second
    }

    private readonly record struct CanonicalTemporalValue(TemporalValueKind Kind, DateOnly Date, TimeOnly Time)
    {
        public static CanonicalTemporalValue FromDate(DateOnly date) => new(TemporalValueKind.Date, date, default);

        public static CanonicalTemporalValue FromTime(TimeOnly time) => new(TemporalValueKind.Time, default, time);

        public static CanonicalTemporalValue FromDateTime(DateTime dateTime) => new(
            TemporalValueKind.DateTime,
            DateOnly.FromDateTime(dateTime),
            TimeOnly.FromDateTime(dateTime));

        public DateTime ToDateTime() => Date.ToDateTime(Time);
    }

    private readonly record struct IntervalTokenInfo(DateTimeIntervalKind Kind, bool IsDateBased);

    private readonly record struct PartTokenInfo(DateTimePartKind Kind, bool IsDateBased);

    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalDateFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateDateBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("DATE", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(FormatCanonicalDate(DateOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateTimeBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("TIME", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(FormatCanonicalTime(TimeOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateNowBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("NOW", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(FormatCanonicalDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateUtcDateBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(CanonicalDateFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateUtcTimeBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(CanonicalTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateUtcNowBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateTimerBuiltin()
    {
        return RuntimeValue.FromDouble(_host.GetLocalNow().TimeOfDay.TotalSeconds);
    }

    private RuntimeValue EvaluateDateSerialBuiltin(int year, int month, int day, TextLocation location)
    {
        EnsureCalendarYearArgument("DATESERIAL", "year", year, location);
        EnsureCalendarMonthArgument("DATESERIAL", "month", month, location);
        EnsureDateSerialDay(day, location);

        try
        {
            return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(year, month, day)));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "DATESERIAL",
                "day",
                "must form a valid calendar date together with arguments 'year' and 'month'.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTimeSerialBuiltin(int hour, int minute, int second, TextLocation location)
    {
        EnsureTimeSerialHour(hour, location);
        EnsureTimeSerialMinute(minute, location);
        EnsureTimeSerialSecond(second, location);

        return RuntimeValue.FromString(FormatCanonicalTime(new TimeOnly(hour, minute, second)));
    }

    private RuntimeValue EvaluateYearBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument("YEAR", value, location).Year);
    }

    private RuntimeValue EvaluateMonthBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument("MONTH", value, location).Month);
    }

    private RuntimeValue EvaluateMonthNameBuiltin(int month, TextLocation location)
    {
        EnsureCalendarMonthArgument("MONTHNAME", "month", month, location);
        return RuntimeValue.FromString(CanonicalMonthNames[month - 1]);
    }

    private RuntimeValue EvaluateDayBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument("DAY", value, location).Day);
    }

    private RuntimeValue EvaluateHourBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument("HOUR", value, location).Hour);
    }

    private RuntimeValue EvaluateMinuteBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument("MINUTE", value, location).Minute);
    }

    private RuntimeValue EvaluateSecondBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument("SECOND", value, location).Second);
    }

    private RuntimeValue EvaluateWeekdayBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromInteger(GetMondayFirstWeekdayNumber(ParseCanonicalDateArgument("WEEKDAY", value, location)));
    }

    private RuntimeValue EvaluateWeekdayNameBuiltin(int weekday, TextLocation location)
    {
        EnsureWeekdayNameWeekday(weekday, location);
        return RuntimeValue.FromString(CanonicalWeekdayNamesMondayFirst[weekday - 1]);
    }

    private RuntimeValue EvaluateDateAddBuiltin(string interval, int number, string value, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEADD", "interval", interval, location);
        var temporal = intervalToken.IsDateBased
            ? ParseCanonicalDateBasedArgument("DATEADD", "value", value, location)
            : ParseCanonicalTimeBasedArgument("DATEADD", "value", value, location);

        return RuntimeValue.FromString(FormatCanonicalTemporal(AddTemporalValue("DATEADD", temporal, intervalToken.Kind, number, location)));
    }

    private RuntimeValue EvaluateDateDiffBuiltin(string interval, string fromValue, string toValue, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEDIFF", "interval", interval, location);

        if (intervalToken.IsDateBased)
        {
            var fromTemporal = ParseCanonicalDateBasedArgument("DATEDIFF", "fromValue", fromValue, location);
            var toTemporal = ParseCanonicalDateBasedArgument("DATEDIFF", "toValue", toValue, location);
            return RuntimeValue.FromLong(EvaluateDateBasedDiff(intervalToken.Kind, fromTemporal.Date, toTemporal.Date));
        }

        var fromTimeTemporal = ParseCanonicalTimeBasedArgument("DATEDIFF", "fromValue", fromValue, location);
        var toTimeTemporal = ParseCanonicalTimeBasedArgument("DATEDIFF", "toValue", toValue, location);
        return RuntimeValue.FromLong(EvaluateTimeBasedDiff(intervalToken.Kind, fromTimeTemporal, toTimeTemporal));
    }

    private RuntimeValue EvaluateDatePartBuiltin(string part, string value, TextLocation location)
    {
        var partToken = ParseDateTimePartToken("DATEPART", "part", part, location);

        if (partToken.IsDateBased)
        {
            var temporal = ParseCanonicalDateBasedArgument("DATEPART", "value", value, location);
            return RuntimeValue.FromInteger(EvaluateDatePart(partToken.Kind, temporal.Date));
        }

        var timeTemporal = ParseCanonicalTimeBasedArgument("DATEPART", "value", value, location);
        return RuntimeValue.FromInteger(EvaluateTimePart(partToken.Kind, timeTemporal.Time));
    }

    private RuntimeValue EvaluateDateValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromString(FormatCanonicalDate(ParseCanonicalDateBasedArgument("DATEVALUE", "value", value, location).Date));
    }

    private RuntimeValue EvaluateTimeValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromString(FormatCanonicalTime(ParseCanonicalTimeBasedArgument("TIMEVALUE", "value", value, location).Time));
    }

    private RuntimeValue EvaluateIsDateBuiltin(string value)
    {
        return RuntimeValue.FromBoolean(IsCanonicalTemporalText(value));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin()
    {
        return RuntimeValue.FromInteger(checked((int)ResolveCurrentLocalUtcOffset().TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(string value, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("UTCOFFSET", "value", value, location);
        var offset = ResolveLocalUtcOffsetForLocalDateTime("UTCOFFSET", "value", dateTime, location);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(string value, string timeZoneId, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("UTCOFFSET", "value", value, location);
        var timeZone = ResolveTimeZoneArgument("UTCOFFSET", "timeZone", timeZoneId, location);
        var offset = ResolveUtcOffsetForLocalDateTime("UTCOFFSET", "value", timeZone, dateTime, location);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateToUtcBuiltin(string value, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("TOUTC", "value", value, location);

        try
        {
            var offset = ResolveLocalUtcOffsetForLocalDateTime("TOUTC", "value", dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromString(FormatCanonicalDateTime(utcDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOUTC",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateToUtcBuiltin(string value, string timeZoneId, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("TOUTC", "value", value, location);
        var timeZone = ResolveTimeZoneArgument("TOUTC", "timeZone", timeZoneId, location);

        try
        {
            var offset = ResolveUtcOffsetForLocalDateTime("TOUTC", "value", timeZone, dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromString(FormatCanonicalDateTime(utcDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOUTC",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromUtcBuiltin(string value, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("FROMUTC", "value", value, location);

        try
        {
            var localDateTime = ConvertUtcToLocalDateTime(dateTime);
            return RuntimeValue.FromString(FormatCanonicalDateTime(localDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "FROMUTC",
                "value",
                "must remain within canonical date-time range after local conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromUtcBuiltin(string value, string timeZoneId, TextLocation location)
    {
        var dateTime = ParseCanonicalDateTimeArgument("FROMUTC", "value", value, location);
        var timeZone = ResolveTimeZoneArgument("FROMUTC", "timeZone", timeZoneId, location);

        try
        {
            var localDateTime = ConvertUtcToTimeZoneDateTime(dateTime, timeZone);
            return RuntimeValue.FromString(FormatCanonicalDateTime(localDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "FROMUTC",
                "value",
                "must remain within canonical date-time range after local conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsLeapYearBuiltin(int year, TextLocation location)
    {
        EnsureCalendarYearArgument("ISLEAPYEAR", "year", year, location);
        return RuntimeValue.FromBoolean(DateTime.IsLeapYear(year));
    }

    private RuntimeValue EvaluateDaysInMonthBuiltin(int year, int month, TextLocation location)
    {
        EnsureCalendarYearArgument("DAYSINMONTH", "year", year, location);
        EnsureCalendarMonthArgument("DAYSINMONTH", "month", month, location);
        return RuntimeValue.FromInteger(DateTime.DaysInMonth(year, month));
    }

    private RuntimeValue EvaluateStartOfMonthBuiltin(string value, TextLocation location)
    {
        var date = ParseCanonicalDateArgument("STARTOFMONTH", value, location);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, date.Month, 1)));
    }

    private RuntimeValue EvaluateEndOfMonthBuiltin(string value, TextLocation location)
    {
        var date = ParseCanonicalDateArgument("ENDOFMONTH", value, location);
        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, date.Month, lastDay)));
    }

    private RuntimeValue EvaluateStartOfYearBuiltin(string value, TextLocation location)
    {
        var date = ParseCanonicalDateArgument("STARTOFYEAR", value, location);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, 1, 1)));
    }

    private RuntimeValue EvaluateEndOfYearBuiltin(string value, TextLocation location)
    {
        var date = ParseCanonicalDateArgument("ENDOFYEAR", value, location);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, 12, 31)));
    }

    private static string FormatCanonicalDate(DateOnly date)
    {
        return date.ToString(CanonicalDateFormat, DateTimeCulture);
    }

    private static string FormatCanonicalTime(TimeOnly time)
    {
        return time.ToString(CanonicalTimeFormat, DateTimeCulture);
    }

    private static string FormatCanonicalDateTime(DateTime dateTime)
    {
        return dateTime.ToString(CanonicalDateTimeFormat, DateTimeCulture);
    }

    private static string FormatCanonicalTemporal(CanonicalTemporalValue value)
    {
        return value.Kind switch
        {
            TemporalValueKind.Date => FormatCanonicalDate(value.Date),
            TemporalValueKind.Time => FormatCanonicalTime(value.Time),
            TemporalValueKind.DateTime => FormatCanonicalDateTime(value.ToDateTime()),
            _ => throw new InvalidOperationException($"Unsupported temporal kind '{value.Kind}'.")
        };
    }

    private static bool TryParseCanonicalDateOnly(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, CanonicalDateFormat, DateTimeCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseCanonicalTimeOnly(string value, out TimeOnly time)
    {
        return TimeOnly.TryParseExact(value, CanonicalTimeFormat, DateTimeCulture, DateTimeStyles.None, out time);
    }

    private static bool TryParseCanonicalDateTime(string value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(value, CanonicalDateTimeFormat, DateTimeCulture, DateTimeStyles.None, out dateTime);
    }

    private static bool TryParseCanonicalTemporal(string value, out CanonicalTemporalValue temporal)
    {
        if (TryParseCanonicalDateOnly(value, out var date))
        {
            temporal = CanonicalTemporalValue.FromDate(date);
            return true;
        }

        if (TryParseCanonicalTimeOnly(value, out var time))
        {
            temporal = CanonicalTemporalValue.FromTime(time);
            return true;
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            temporal = CanonicalTemporalValue.FromDateTime(dateTime);
            return true;
        }

        temporal = default;
        return false;
    }

    private static bool IsCanonicalTemporalText(string value)
    {
        return TryParseCanonicalTemporal(value, out _);
    }

    private DateOnly ParseCanonicalDateArgument(string functionName, string value, TextLocation location)
    {
        if (TryParseCanonicalDateOnly(value, out var date))
        {
            return date;
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            "value",
            $"must be a canonical date or date-time in form '{CanonicalDateFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private TimeOnly ParseCanonicalTimeArgument(string functionName, string value, TextLocation location)
    {
        if (TryParseCanonicalTimeOnly(value, out var time))
        {
            return time;
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return TimeOnly.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            "value",
            $"must be a canonical time or date-time in form '{CanonicalTimeFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private DateTime ParseCanonicalDateTimeArgument(string functionName, string argumentName, string value, TextLocation location)
    {
        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return dateTime;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical date-time in form '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private TimeZoneInfo ResolveTimeZoneArgument(string functionName, string argumentName, string value, TextLocation location)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must name 'UTC', 'LOCAL' or a known host time zone identifier.",
                location);
            throw new RuntimeFailureException();
        }

        if (_host.TryResolveTimeZone(value, out var timeZone))
        {
            return timeZone;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must name 'UTC', 'LOCAL' or a known host time zone identifier.",
            location);
        throw new RuntimeFailureException();
    }

    private TimeSpan ResolveCurrentLocalUtcOffset()
    {
        return ResolveUtcOffsetAtCurrentInstant(_host.GetLocalTimeZone());
    }

    private TimeSpan ResolveUtcOffsetAtCurrentInstant(TimeZoneInfo timeZone)
    {
        return timeZone.GetUtcOffset(_host.GetLocalNow().UtcDateTime);
    }

    private TimeSpan ResolveLocalUtcOffsetForLocalDateTime(
        string functionName,
        string argumentName,
        DateTime localDateTime,
        TextLocation location)
    {
        return ResolveUtcOffsetForLocalDateTime(
            functionName,
            argumentName,
            _host.GetLocalTimeZone(),
            localDateTime,
            location);
    }

    private TimeSpan ResolveUtcOffsetForLocalDateTime(
        string functionName,
        string argumentName,
        TimeZoneInfo timeZone,
        DateTime localDateTime,
        TextLocation location)
    {
        var unspecifiedLocalDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(unspecifiedLocalDateTime))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must refer to an existing local date-time and not fall into a skipped clock transition.",
                location);
            throw new RuntimeFailureException();
        }

        if (timeZone.IsAmbiguousTime(unspecifiedLocalDateTime))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must refer to an unambiguous local date-time and not fall into a repeated clock transition.",
                location);
            throw new RuntimeFailureException();
        }

        return timeZone.GetUtcOffset(unspecifiedLocalDateTime);
    }

    private DateTime ConvertUtcToLocalDateTime(DateTime utcDateTime)
    {
        return ConvertUtcToTimeZoneDateTime(utcDateTime, _host.GetLocalTimeZone());
    }

    private static DateTime ConvertUtcToTimeZoneDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        var offset = timeZone.GetUtcOffset(utc);
        var convertedTicks = utc.Ticks + offset.Ticks;

        if (convertedTicks < DateTime.MinValue.Ticks || convertedTicks > DateTime.MaxValue.Ticks)
        {
            throw new ArgumentOutOfRangeException(nameof(utcDateTime));
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
    }

    private CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        if (TryParseCanonicalDateOnly(value, out var date))
        {
            return CanonicalTemporalValue.FromDate(date);
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return CanonicalTemporalValue.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical date or date-time in form '{CanonicalDateFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        if (TryParseCanonicalTimeOnly(value, out var time))
        {
            return CanonicalTemporalValue.FromTime(time);
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return CanonicalTemporalValue.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical time or date-time in form '{CanonicalTimeFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private IntervalTokenInfo ParseDateTimeIntervalToken(string functionName, string argumentName, string value, TextLocation location)
    {
        if (DateTimeIntervalTokens.TryGetValue(value, out var token))
        {
            return token;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be one of: yyyy, q, m, d, h, n, s.",
            location);
        throw new RuntimeFailureException();
    }

    private PartTokenInfo ParseDateTimePartToken(string functionName, string argumentName, string value, TextLocation location)
    {
        if (DateTimePartTokens.TryGetValue(value, out var token))
        {
            return token;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be one of: yyyy, q, m, d, y, w, h, n, s.",
            location);
        throw new RuntimeFailureException();
    }

    private CanonicalTemporalValue AddTemporalValue(
        string functionName,
        CanonicalTemporalValue value,
        DateTimeIntervalKind interval,
        int number,
        TextLocation location)
    {
        try
        {
            return interval switch
            {
                DateTimeIntervalKind.Year => AddDateBasedTemporal(value, static (date, amount) => date.AddYears(amount), static (dateTime, amount) => dateTime.AddYears(amount), number),
                DateTimeIntervalKind.Quarter => AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(checked(amount * 3)), static (dateTime, amount) => dateTime.AddMonths(checked(amount * 3)), number),
                DateTimeIntervalKind.Month => AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(amount), static (dateTime, amount) => dateTime.AddMonths(amount), number),
                DateTimeIntervalKind.Day => AddDateBasedTemporal(value, static (date, amount) => date.AddDays(amount), static (dateTime, amount) => dateTime.AddDays(amount), number),
                DateTimeIntervalKind.Hour => AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerHour)),
                DateTimeIntervalKind.Minute => AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerMinute)),
                DateTimeIntervalKind.Second => AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerSecond)),
                _ => throw new InvalidOperationException($"Unsupported interval '{interval}'.")
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "must remain within canonical date/time range after arithmetic.",
                location);
            throw new RuntimeFailureException();
        }
        catch (OverflowException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "must remain within canonical date/time range after arithmetic.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private static CanonicalTemporalValue AddDateBasedTemporal(
        CanonicalTemporalValue value,
        Func<DateOnly, int, DateOnly> addDate,
        Func<DateTime, int, DateTime> addDateTime,
        int amount)
    {
        return value.Kind switch
        {
            TemporalValueKind.Date => CanonicalTemporalValue.FromDate(addDate(value.Date, amount)),
            TemporalValueKind.DateTime => CanonicalTemporalValue.FromDateTime(addDateTime(value.ToDateTime(), amount)),
            _ => throw new InvalidOperationException("Date-based arithmetic requires date or date-time input.")
        };
    }

    private static CanonicalTemporalValue AddTimeBasedTemporal(CanonicalTemporalValue value, long deltaTicks)
    {
        return value.Kind switch
        {
            TemporalValueKind.Time => CanonicalTemporalValue.FromTime(AddWrappedTime(value.Time, deltaTicks)),
            TemporalValueKind.DateTime => CanonicalTemporalValue.FromDateTime(value.ToDateTime().AddTicks(deltaTicks)),
            _ => throw new InvalidOperationException("Time-based arithmetic requires time or date-time input.")
        };
    }

    private static TimeOnly AddWrappedTime(TimeOnly time, long deltaTicks)
    {
        var resultTicks = NormalizeTimeOfDayTicks(time.ToTimeSpan().Ticks + deltaTicks);
        return new TimeOnly(resultTicks);
    }

    private static long NormalizeTimeOfDayTicks(long ticks)
    {
        var normalized = ticks % TimeSpan.TicksPerDay;

        if (normalized < 0)
        {
            normalized += TimeSpan.TicksPerDay;
        }

        return normalized;
    }

    private static long EvaluateDateBasedDiff(DateTimeIntervalKind interval, DateOnly fromDate, DateOnly toDate)
    {
        return interval switch
        {
            DateTimeIntervalKind.Year => toDate.Year - fromDate.Year,
            DateTimeIntervalKind.Quarter => GetQuarterBoundaryIndex(toDate) - GetQuarterBoundaryIndex(fromDate),
            DateTimeIntervalKind.Month => GetMonthBoundaryIndex(toDate) - GetMonthBoundaryIndex(fromDate),
            DateTimeIntervalKind.Day => toDate.DayNumber - fromDate.DayNumber,
            _ => throw new InvalidOperationException($"Interval '{interval}' is not date-based.")
        };
    }

    private static long EvaluateTimeBasedDiff(DateTimeIntervalKind interval, CanonicalTemporalValue fromTemporal, CanonicalTemporalValue toTemporal)
    {
        if (fromTemporal.Kind == TemporalValueKind.Time && toTemporal.Kind == TemporalValueKind.Time)
        {
            return GetTimeBoundaryIndex(toTemporal.Time, interval) - GetTimeBoundaryIndex(fromTemporal.Time, interval);
        }

        if (fromTemporal.Kind == TemporalValueKind.DateTime && toTemporal.Kind == TemporalValueKind.DateTime)
        {
            return GetDateTimeBoundaryIndex(toTemporal.ToDateTime(), interval) - GetDateTimeBoundaryIndex(fromTemporal.ToDateTime(), interval);
        }

        return GetTimeBoundaryIndex(toTemporal.Time, interval) - GetTimeBoundaryIndex(fromTemporal.Time, interval);
    }

    private static int EvaluateDatePart(DateTimePartKind part, DateOnly date)
    {
        return part switch
        {
            DateTimePartKind.Year => date.Year,
            DateTimePartKind.Quarter => ((date.Month - 1) / 3) + 1,
            DateTimePartKind.Month => date.Month,
            DateTimePartKind.Day => date.Day,
            DateTimePartKind.DayOfYear => date.DayOfYear,
            DateTimePartKind.Weekday => GetMondayFirstWeekdayNumber(date),
            _ => throw new InvalidOperationException($"Part '{part}' is not date-based.")
        };
    }

    private static int EvaluateTimePart(DateTimePartKind part, TimeOnly time)
    {
        return part switch
        {
            DateTimePartKind.Hour => time.Hour,
            DateTimePartKind.Minute => time.Minute,
            DateTimePartKind.Second => time.Second,
            _ => throw new InvalidOperationException($"Part '{part}' is not time-based.")
        };
    }

    private static long GetQuarterBoundaryIndex(DateOnly date)
    {
        return checked((date.Year * 4L) + ((date.Month - 1) / 3));
    }

    private static long GetMonthBoundaryIndex(DateOnly date)
    {
        return checked((date.Year * 12L) + (date.Month - 1L));
    }

    private static long GetTimeBoundaryIndex(TimeOnly time, DateTimeIntervalKind interval)
    {
        var ticks = time.ToTimeSpan().Ticks;
        return interval switch
        {
            DateTimeIntervalKind.Hour => ticks / TimeSpan.TicksPerHour,
            DateTimeIntervalKind.Minute => ticks / TimeSpan.TicksPerMinute,
            DateTimeIntervalKind.Second => ticks / TimeSpan.TicksPerSecond,
            _ => throw new InvalidOperationException($"Interval '{interval}' is not time-based.")
        };
    }

    private static long GetDateTimeBoundaryIndex(DateTime dateTime, DateTimeIntervalKind interval)
    {
        return interval switch
        {
            DateTimeIntervalKind.Hour => dateTime.Ticks / TimeSpan.TicksPerHour,
            DateTimeIntervalKind.Minute => dateTime.Ticks / TimeSpan.TicksPerMinute,
            DateTimeIntervalKind.Second => dateTime.Ticks / TimeSpan.TicksPerSecond,
            _ => throw new InvalidOperationException($"Interval '{interval}' is not time-based.")
        };
    }

    private void EnsureCalendarYearArgument(string functionName, string argumentName, int year, TextLocation location)
    {
        if (year is >= 1 and <= 9999)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be in range 1..9999.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureCalendarMonthArgument(string functionName, string argumentName, int month, TextLocation location)
    {
        if (month is >= 1 and <= 12)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be in range 1..12.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureDateSerialDay(int day, TextLocation location)
    {
        if (day is >= 1 and <= 31)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "DATESERIAL",
            "day",
            "must be in range 1..31.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialHour(int hour, TextLocation location)
    {
        if (hour is >= 0 and <= 23)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "hour",
            "must be in range 0..23.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialMinute(int minute, TextLocation location)
    {
        if (minute is >= 0 and <= 59)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "minute",
            "must be in range 0..59.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialSecond(int second, TextLocation location)
    {
        if (second is >= 0 and <= 59)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "second",
            "must be in range 0..59.",
            location);
        throw new RuntimeFailureException();
    }

    private void EnsureWeekdayNameWeekday(int weekday, TextLocation location)
    {
        if (weekday is >= 1 and <= 7)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "WEEKDAYNAME",
            "weekday",
            "must be in range 1..7 according to Monday-first numbering.",
            location);
        throw new RuntimeFailureException();
    }

    private static int GetMondayFirstWeekdayNumber(DateOnly date)
    {
        return ((int)date.DayOfWeek + 6) % 7 + 1;
    }
}
