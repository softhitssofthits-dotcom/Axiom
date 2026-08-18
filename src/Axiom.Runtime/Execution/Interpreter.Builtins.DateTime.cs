using System.Globalization;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(BuiltinDateTimeHelpers.CanonicalDateFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateDateBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "DATE", "timeZone", 0, timeZoneId);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(DateOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(BuiltinDateTimeHelpers.CanonicalTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateTimeBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "TIME", "timeZone", 0, timeZoneId);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalTime(TimeOnly.FromDateTime(currentDateTime)));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateNowBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "NOW", "timeZone", 0, timeZoneId);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateUtcDateBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(BuiltinDateTimeHelpers.CanonicalDateFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateUtcTimeBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(BuiltinDateTimeHelpers.CanonicalTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateUtcNowBuiltin()
    {
        return RuntimeValue.FromString(_host.GetLocalNow().ToUniversalTime().ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
    }

    private RuntimeValue EvaluateTimerBuiltin()
    {
        return RuntimeValue.FromDouble(_host.GetLocalNow().TimeOfDay.TotalSeconds);
    }

    private RuntimeValue EvaluateDateSerialBuiltin(BoundCallExpression expression, int year, int month, int day)
    {
        EnsureCalendarYearArgument(expression, "DATESERIAL", "year", 0, year);
        EnsureCalendarMonthArgument(expression, "DATESERIAL", "month", 1, month);
        EnsureDateSerialDay(expression, day);

        try
        {
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(new DateOnly(year, month, day)));
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

        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalTime(new TimeOnly(hour, minute, second)));
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
        EnsureCalendarMonthArgument(expression, "MONTHNAME", "month", 0, month);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalMonthNames[month - 1]);
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
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.GetMondayFirstWeekdayNumber(ParseCanonicalDateArgument(expression, "WEEKDAY", value)));
    }

    private RuntimeValue EvaluateWeekdayNameBuiltin(BoundCallExpression expression, int weekday)
    {
        EnsureWeekdayNameWeekday(expression, weekday);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalWeekdayNamesMondayFirst[weekday - 1]);
    }

    private RuntimeValue EvaluateDateAddBuiltin(BoundCallExpression expression, string interval, int number, string value)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEADD", "interval", 0, interval);
        var temporal = intervalToken.IsDateBased
            ? ParseCanonicalDateBasedArgument(expression, "DATEADD", "value", 2, value)
            : ParseCanonicalTimeBasedArgument(expression, "DATEADD", "value", 2, value);

        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalTemporal(AddTemporalValue(expression, "DATEADD", temporal, intervalToken.Kind, number)));
    }

    private RuntimeValue EvaluateDateDiffBuiltin(BoundCallExpression expression, string interval, string fromValue, string toValue)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEDIFF", "interval", 0, interval);

        if (intervalToken.IsDateBased)
        {
            var fromTemporal = ParseCanonicalDateBasedArgument(expression, "DATEDIFF", "fromValue", 1, fromValue);
            var toTemporal = ParseCanonicalDateBasedArgument(expression, "DATEDIFF", "toValue", 2, toValue);
            return RuntimeValue.FromLong(BuiltinDateTimeHelpers.EvaluateDateBasedDiff(intervalToken.Kind, fromTemporal.Date, toTemporal.Date));
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
            return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateDatePart(partToken.Kind, temporal.Date));
        }

        var timeTemporal = ParseCanonicalTimeBasedArgument(expression, "DATEPART", "value", 1, value);
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateTimePart(partToken.Kind, timeTemporal.Time));
    }

    private RuntimeValue EvaluateDateValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(ParseCanonicalDateBasedArgument(expression, "DATEVALUE", "value", 0, value).Date));
    }

    private RuntimeValue EvaluateTimeValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalTime(ParseCanonicalTimeBasedArgument(expression, "TIMEVALUE", "value", 0, value).Time));
    }

    private RuntimeValue EvaluateIsDateBuiltin(string value)
    {
        return RuntimeValue.FromBoolean(BuiltinDateTimeHelpers.IsCanonicalTemporalText(value));
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
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDateTime(utcDateTime));
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
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDateTime(utcDateTime));
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
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDateTime(localDateTime));
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
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDateTime(localDateTime));
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
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(new DateOnly(date.Year, date.Month, 1)));
    }

    private RuntimeValue EvaluateEndOfMonthBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "ENDOFMONTH", value);
        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(new DateOnly(date.Year, date.Month, lastDay)));
    }

    private RuntimeValue EvaluateStartOfYearBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "STARTOFYEAR", value);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(new DateOnly(date.Year, 1, 1)));
    }

    private RuntimeValue EvaluateEndOfYearBuiltin(BoundCallExpression expression, string value)
    {
        var date = ParseCanonicalDateArgument(expression, "ENDOFYEAR", value);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalDate(new DateOnly(date.Year, 12, 31)));
    }

    private long EvaluateTimeBasedDiff(BuiltinDateTimeHelpers.DateTimeIntervalKind interval, BuiltinDateTimeHelpers.CanonicalTemporalValue fromTemporal, BuiltinDateTimeHelpers.CanonicalTemporalValue toTemporal)
    {
        if (fromTemporal.Kind == BuiltinDateTimeHelpers.TemporalValueKind.Time && toTemporal.Kind == BuiltinDateTimeHelpers.TemporalValueKind.Time)
        {
            return BuiltinDateTimeHelpers.GetTimeBoundaryIndex(toTemporal.Time, interval) - BuiltinDateTimeHelpers.GetTimeBoundaryIndex(fromTemporal.Time, interval);
        }

        if (fromTemporal.Kind == BuiltinDateTimeHelpers.TemporalValueKind.DateTime && toTemporal.Kind == BuiltinDateTimeHelpers.TemporalValueKind.DateTime)
        {
            return BuiltinDateTimeHelpers.GetDateTimeBoundaryIndex(toTemporal.ToDateTime(), interval) - BuiltinDateTimeHelpers.GetDateTimeBoundaryIndex(fromTemporal.ToDateTime(), interval);
        }

        return BuiltinDateTimeHelpers.GetTimeBoundaryIndex(toTemporal.Time, interval) - BuiltinDateTimeHelpers.GetTimeBoundaryIndex(fromTemporal.Time, interval);
    }
}
