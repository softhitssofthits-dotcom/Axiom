using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
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

    private DateOnly ParseCanonicalDateArgument(BoundCallExpression expression, string functionName, string value)
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
            GetBuiltInArgumentLocation(expression, 0));
        throw new RuntimeFailureException();
    }

    private TimeOnly ParseCanonicalTimeArgument(BoundCallExpression expression, string functionName, string value)
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
            GetBuiltInArgumentLocation(expression, 0));
        throw new RuntimeFailureException();
    }

    private DateTime ParseCanonicalDateTimeArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return dateTime;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical date-time in form '{CanonicalDateTimeFormat}'.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private TimeZoneInfo ResolveTimeZoneArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must name 'UTC', 'LOCAL' or a known host time zone identifier.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
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
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private TimeZoneInfo GetLocalTimeZone()
    {
        return _host.GetLocalTimeZone();
    }

    private TimeSpan ResolveCurrentLocalUtcOffset()
    {
        return ResolveUtcOffsetAtCurrentInstant(GetLocalTimeZone());
    }

    private TimeSpan ResolveUtcOffsetAtCurrentInstant(TimeZoneInfo timeZone)
    {
        return timeZone.GetUtcOffset(_host.GetLocalNow().UtcDateTime);
    }

    private TimeSpan ResolveLocalUtcOffsetForLocalDateTime(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        DateTime localDateTime)
    {
        return ResolveUtcOffsetForLocalDateTime(
            expression,
            functionName,
            argumentName,
            argumentIndex,
            GetLocalTimeZone(),
            localDateTime);
    }

    private TimeSpan ResolveUtcOffsetForLocalDateTime(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        TimeZoneInfo timeZone,
        DateTime localDateTime)
    {
        var unspecifiedLocalDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(unspecifiedLocalDateTime))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must refer to an existing local date-time and not fall into a skipped clock transition.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        if (timeZone.IsAmbiguousTime(unspecifiedLocalDateTime))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "must refer to an unambiguous local date-time and not fall into a repeated clock transition.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return timeZone.GetUtcOffset(unspecifiedLocalDateTime);
    }

    private TimeSpan ResolveLocalUtcOffsetForUtcDateTime(DateTime utcDateTime)
    {
        return GetLocalTimeZone().GetUtcOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
    }

    private DateTime ConvertUtcToLocalDateTime(DateTime utcDateTime)
    {
        return ConvertUtcToTimeZoneDateTime(utcDateTime, GetLocalTimeZone());
    }

    private DateTime ConvertUtcToTimeZoneDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
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
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
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
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
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
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private IntervalTokenInfo ParseDateTimeIntervalToken(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        if (DateTimeIntervalTokens.TryGetValue(value, out var token))
        {
            return token;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be one of: yyyy, q, m, d, h, n, s.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private PartTokenInfo ParseDateTimePartToken(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        if (DateTimePartTokens.TryGetValue(value, out var token))
        {
            return token;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be one of: yyyy, q, m, d, y, w, h, n, s.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private CanonicalTemporalValue AddTemporalValue(
        BoundCallExpression expression,
        string functionName,
        CanonicalTemporalValue value,
        DateTimeIntervalKind interval,
        int number)
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
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }
        catch (OverflowException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                "must remain within canonical date/time range after arithmetic.",
                GetBuiltInArgumentLocation(expression, 2));
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

    private void EnsureDateSerialYear(BoundCallExpression expression, int year)
    {
        EnsureCalendarYearArgument(expression, "DATESERIAL", "year", 0, year);
    }

    private void EnsureCalendarYearArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int year)
    {
        if (year is >= 1 and <= 9999)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be in range 1..9999.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private void EnsureDateSerialMonth(BoundCallExpression expression, int month)
    {
        EnsureCalendarMonthArgument(expression, "DATESERIAL", "month", 1, month);
    }

    private void EnsureCalendarMonthArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int month)
    {
        if (month is >= 1 and <= 12)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "must be in range 1..12.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private void EnsureDateSerialDay(BoundCallExpression expression, int day)
    {
        if (day is >= 1 and <= 31)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "DATESERIAL",
            "day",
            "must be in range 1..31.",
            GetBuiltInArgumentLocation(expression, 2));
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialHour(BoundCallExpression expression, int hour)
    {
        if (hour is >= 0 and <= 23)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "hour",
            "must be in range 0..23.",
            GetBuiltInArgumentLocation(expression, 0));
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialMinute(BoundCallExpression expression, int minute)
    {
        if (minute is >= 0 and <= 59)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "minute",
            "must be in range 0..59.",
            GetBuiltInArgumentLocation(expression, 1));
        throw new RuntimeFailureException();
    }

    private void EnsureTimeSerialSecond(BoundCallExpression expression, int second)
    {
        if (second is >= 0 and <= 59)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "TIMESERIAL",
            "second",
            "must be in range 0..59.",
            GetBuiltInArgumentLocation(expression, 2));
        throw new RuntimeFailureException();
    }

    private void EnsureMonthNameMonth(BoundCallExpression expression, int month)
    {
        EnsureCalendarMonthArgument(expression, "MONTHNAME", "month", 0, month);
    }

    private void EnsureWeekdayNameWeekday(BoundCallExpression expression, int weekday)
    {
        if (weekday is >= 1 and <= 7)
        {
            return;
        }

        ReportBuiltInArgumentError(
            "WEEKDAYNAME",
            "weekday",
            "must be in range 1..7 according to Monday-first numbering.",
            GetBuiltInArgumentLocation(expression, 0));
        throw new RuntimeFailureException();
    }

    private static int GetMondayFirstWeekdayNumber(DateOnly date)
    {
        return ((int)date.DayOfWeek + 6) % 7 + 1;
    }
}
