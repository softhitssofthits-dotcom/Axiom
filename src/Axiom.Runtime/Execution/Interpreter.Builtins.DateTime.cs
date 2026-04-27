using System.Globalization;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private const string CanonicalDateFormat = "yyyy-MM-dd";
    private const string CanonicalTimeFormat = "HH:mm:ss";
    private const string CanonicalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalDateFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateDateBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "DATE", "timeZone", 0, timeZoneId);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(FormatCanonicalDate(DateOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateTimeBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "TIME", "timeZone", 0, timeZoneId);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(FormatCanonicalTime(TimeOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateNowBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "NOW", "timeZone", 0, timeZoneId);
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

    private RuntimeValue EvaluateDateSerialBuiltin(BoundCallExpression expression, int year, int month, int day)
    {
        EnsureDateSerialYear(expression, year);
        EnsureDateSerialMonth(expression, month);
        EnsureDateSerialDay(expression, day);

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
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTimeSerialBuiltin(BoundCallExpression expression, int hour, int minute, int second)
    {
        EnsureTimeSerialHour(expression, hour);
        EnsureTimeSerialMinute(expression, minute);
        EnsureTimeSerialSecond(expression, second);

        return RuntimeValue.FromString(FormatCanonicalTime(new TimeOnly(hour, minute, second)));
    }

    private RuntimeValue EvaluateYearBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument(expression, "YEAR", value).Year);
    }

    private RuntimeValue EvaluateMonthBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument(expression, "MONTH", value).Month);
    }

    private RuntimeValue EvaluateMonthNameBuiltin(BoundCallExpression expression, int month)
    {
        EnsureMonthNameMonth(expression, month);
        return RuntimeValue.FromString(CanonicalMonthNames[month - 1]);
    }

    private RuntimeValue EvaluateDayBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalDateArgument(expression, "DAY", value).Day);
    }

    private RuntimeValue EvaluateHourBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument(expression, "HOUR", value).Hour);
    }

    private RuntimeValue EvaluateMinuteBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument(expression, "MINUTE", value).Minute);
    }

    private RuntimeValue EvaluateSecondBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(ParseCanonicalTimeArgument(expression, "SECOND", value).Second);
    }

    private RuntimeValue EvaluateWeekdayBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromInteger(GetMondayFirstWeekdayNumber(ParseCanonicalDateArgument(expression, "WEEKDAY", value)));
    }

    private RuntimeValue EvaluateWeekdayNameBuiltin(BoundCallExpression expression, int weekday)
    {
        EnsureWeekdayNameWeekday(expression, weekday);
        return RuntimeValue.FromString(CanonicalWeekdayNamesMondayFirst[weekday - 1]);
    }

    private RuntimeValue EvaluateDateAddBuiltin(BoundCallExpression expression, string interval, int number, string value)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEADD", "interval", 0, interval);
        var temporal = intervalToken.IsDateBased
            ? ParseCanonicalDateBasedArgument(expression, "DATEADD", "value", 2, value)
            : ParseCanonicalTimeBasedArgument(expression, "DATEADD", "value", 2, value);

        return RuntimeValue.FromString(FormatCanonicalTemporal(AddTemporalValue(expression, "DATEADD", temporal, intervalToken.Kind, number)));
    }

    private RuntimeValue EvaluateDateDiffBuiltin(BoundCallExpression expression, string interval, string fromValue, string toValue)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEDIFF", "interval", 0, interval);

        if (intervalToken.IsDateBased)
        {
            var fromTemporal = ParseCanonicalDateBasedArgument(expression, "DATEDIFF", "fromValue", 1, fromValue);
            var toTemporal = ParseCanonicalDateBasedArgument(expression, "DATEDIFF", "toValue", 2, toValue);
            return RuntimeValue.FromLong(EvaluateDateBasedDiff(intervalToken.Kind, fromTemporal.Date, toTemporal.Date));
        }

        var fromTimeTemporal = ParseCanonicalTimeBasedArgument(expression, "DATEDIFF", "fromValue", 1, fromValue);
        var toTimeTemporal = ParseCanonicalTimeBasedArgument(expression, "DATEDIFF", "toValue", 2, toValue);
        return RuntimeValue.FromLong(EvaluateTimeBasedDiff(intervalToken.Kind, fromTimeTemporal, toTimeTemporal));
    }

    private RuntimeValue EvaluateDatePartBuiltin(BoundCallExpression expression, string part, string value)
    {
        var partToken = ParseDateTimePartToken(expression, "DATEPART", "part", 0, part);

        if (partToken.IsDateBased)
        {
            var temporal = ParseCanonicalDateBasedArgument(expression, "DATEPART", "value", 1, value);
            return RuntimeValue.FromInteger(EvaluateDatePart(partToken.Kind, temporal.Date));
        }

        var timeTemporal = ParseCanonicalTimeBasedArgument(expression, "DATEPART", "value", 1, value);
        return RuntimeValue.FromInteger(EvaluateTimePart(partToken.Kind, timeTemporal.Time));
    }

    private RuntimeValue EvaluateDateValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromString(FormatCanonicalDate(ParseCanonicalDateBasedArgument(expression, "DATEVALUE", "value", 0, value).Date));
    }

    private RuntimeValue EvaluateTimeValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromString(FormatCanonicalTime(ParseCanonicalTimeBasedArgument(expression, "TIMEVALUE", "value", 0, value).Time));
    }

    private RuntimeValue EvaluateIsDateBuiltin(string value)
    {
        return RuntimeValue.FromBoolean(IsCanonicalTemporalText(value));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin()
    {
        return RuntimeValue.FromInteger(checked((int)ResolveCurrentLocalUtcOffset().TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(BoundCallExpression expression, string value)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "UTCOFFSET", "value", 0, value);
        var offset = ResolveLocalUtcOffsetForLocalDateTime(expression, "UTCOFFSET", "value", 0, dateTime);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(BoundCallExpression expression, string value, string timeZoneId)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "UTCOFFSET", "value", 0, value);
        var timeZone = ResolveTimeZoneArgument(expression, "UTCOFFSET", "timeZone", 1, timeZoneId);
        var offset = ResolveUtcOffsetForLocalDateTime(expression, "UTCOFFSET", "value", 0, timeZone, dateTime);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateToUtcBuiltin(BoundCallExpression expression, string value)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "TOUTC", "value", 0, value);

        try
        {
            var offset = ResolveLocalUtcOffsetForLocalDateTime(expression, "TOUTC", "value", 0, dateTime);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromString(FormatCanonicalDateTime(utcDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOUTC",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateToUtcBuiltin(BoundCallExpression expression, string value, string timeZoneId)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "TOUTC", "value", 0, value);
        var timeZone = ResolveTimeZoneArgument(expression, "TOUTC", "timeZone", 1, timeZoneId);

        try
        {
            var offset = ResolveUtcOffsetForLocalDateTime(expression, "TOUTC", "value", 0, timeZone, dateTime);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromString(FormatCanonicalDateTime(utcDateTime));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOUTC",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromUtcBuiltin(BoundCallExpression expression, string value)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "FROMUTC", "value", 0, value);

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
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromUtcBuiltin(BoundCallExpression expression, string value, string timeZoneId)
    {
        var dateTime = ParseCanonicalDateTimeArgument(expression, "FROMUTC", "value", 0, value);
        var timeZone = ResolveTimeZoneArgument(expression, "FROMUTC", "timeZone", 1, timeZoneId);

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
                GetBuiltInArgumentLocation(expression, 0));
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsLeapYearBuiltin(BoundCallExpression expression, int year)
    {
        EnsureCalendarYearArgument(expression, "ISLEAPYEAR", "year", 0, year);
        return RuntimeValue.FromBoolean(DateTime.IsLeapYear(year));
    }

    private RuntimeValue EvaluateDaysInMonthBuiltin(BoundCallExpression expression, int year, int month)
    {
        EnsureCalendarYearArgument(expression, "DAYSINMONTH", "year", 0, year);
        EnsureCalendarMonthArgument(expression, "DAYSINMONTH", "month", 1, month);
        return RuntimeValue.FromInteger(DateTime.DaysInMonth(year, month));
    }

    private RuntimeValue EvaluateStartOfMonthBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "STARTOFMONTH", value);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, date.Month, 1)));
    }

    private RuntimeValue EvaluateEndOfMonthBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "ENDOFMONTH", value);
        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, date.Month, lastDay)));
    }

    private RuntimeValue EvaluateStartOfYearBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "STARTOFYEAR", value);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, 1, 1)));
    }

    private RuntimeValue EvaluateEndOfYearBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "ENDOFYEAR", value);
        return RuntimeValue.FromString(FormatCanonicalDate(new DateOnly(date.Year, 12, 31)));
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

    private long EvaluateTimeBasedDiff(DateTimeIntervalKind interval, CanonicalTemporalValue fromTemporal, CanonicalTemporalValue toTemporal)
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
}
