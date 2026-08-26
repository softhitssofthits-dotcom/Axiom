using System.Globalization;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromDate(DateOnly.FromDateTime(_host.GetLocalNow().DateTime));
    }

    private RuntimeValue EvaluateDateBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "DATE", "timeZone", 0, timeZoneId);
        var currentDateTime = BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromDate(DateOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(_host.GetLocalNow().DateTime));
    }

    private RuntimeValue EvaluateTimeBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "TIME", "timeZone", 0, timeZoneId);
        var currentDateTime = BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromDateTime(_host.GetLocalNow().DateTime);
    }

    private RuntimeValue EvaluateNowBuiltin(BoundCallExpression expression, string timeZoneId)
    {
        var timeZone = ResolveTimeZoneArgument(expression, "NOW", "timeZone", 0, timeZoneId);
        var currentDateTime = BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromDateTime(currentDateTime);
    }

    private RuntimeValue EvaluateUtcDateBuiltin()
    {
        return RuntimeValue.FromDate(DateOnly.FromDateTime(_host.GetLocalNow().ToUniversalTime().DateTime));
    }

    private RuntimeValue EvaluateUtcTimeBuiltin()
    {
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(_host.GetLocalNow().ToUniversalTime().DateTime));
    }

    private RuntimeValue EvaluateUtcNowBuiltin()
    {
        return RuntimeValue.FromDateTime(_host.GetLocalNow().ToUniversalTime().DateTime);
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
            return RuntimeValue.FromDate(new DateOnly(year, month, day));
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

        return RuntimeValue.FromTime(new TimeOnly(hour, minute, second));
    }

    private RuntimeValue EvaluateYearBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractDateValue(expression, "YEAR", value, 0).Year);
    }

    private RuntimeValue EvaluateMonthBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractDateValue(expression, "MONTH", value, 0).Month);
    }

    private RuntimeValue EvaluateMonthNameBuiltin(BoundCallExpression expression, int month)
    {
        EnsureCalendarMonthArgument(expression, "MONTHNAME", "month", 0, month);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalMonthNames[month - 1]);
    }

    private RuntimeValue EvaluateDayBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractDateValue(expression, "DAY", value, 0).Day);
    }

    private RuntimeValue EvaluateHourBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue(expression, "HOUR", value, 0).Hour);
    }

    private RuntimeValue EvaluateMinuteBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue(expression, "MINUTE", value, 0).Minute);
    }

    private RuntimeValue EvaluateSecondBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue(expression, "SECOND", value, 0).Second);
    }

    private RuntimeValue EvaluateWeekdayBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.GetMondayFirstWeekdayNumber(ExtractDateValue(expression, "WEEKDAY", value, 0)));
    }

    private RuntimeValue EvaluateWeekdayNameBuiltin(BoundCallExpression expression, int weekday)
    {
        EnsureWeekdayNameWeekday(expression, weekday);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalWeekdayNamesMondayFirst[weekday - 1]);
    }

    private RuntimeValue EvaluateDateAddBuiltin(BoundCallExpression expression, string interval, int number, RuntimeValue value)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEADD", "interval", 0, interval);
        var temporal = BuiltinDateTimeHelpers.ToTemporalValue(value);
        var result = AddTemporalValue(expression, "DATEADD", temporal, intervalToken.Kind, number);
        return BuiltinDateTimeHelpers.ToRuntimeTemporalValue(result);
    }

    private RuntimeValue EvaluateDateDiffBuiltin(BoundCallExpression expression, string interval, RuntimeValue fromValue, RuntimeValue toValue)
    {
        var intervalToken = ParseDateTimeIntervalToken(expression, "DATEDIFF", "interval", 0, interval);
        var fromTemporal = BuiltinDateTimeHelpers.ToTemporalValue(fromValue);
        var toTemporal = BuiltinDateTimeHelpers.ToTemporalValue(toValue);
        return RuntimeValue.FromLong(BuiltinDateTimeHelpers.EvaluateDateDiffTemporal(
            "DATEDIFF",
            intervalToken.Kind,
            fromTemporal,
            toTemporal,
            GetBuiltInArgumentLocation(expression, 1),
            ReportBuiltInArgumentError));
    }

    private RuntimeValue EvaluateDatePartBuiltin(BoundCallExpression expression, string part, RuntimeValue value)
    {
        var partToken = ParseDateTimePartToken(expression, "DATEPART", "part", 0, part);
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateDatePartTemporal(
            "DATEPART",
            partToken,
            BuiltinDateTimeHelpers.ToTemporalValue(value),
            GetBuiltInArgumentLocation(expression, 1),
            ReportBuiltInArgumentError));
    }

    private RuntimeValue EvaluateDateValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromDate(ParseCanonicalDateBasedArgument(expression, "DATEVALUE", "value", 0, value).Date);
    }

    private RuntimeValue EvaluateTimeValueBuiltin(BoundCallExpression expression, string value)
    {
        return RuntimeValue.FromTime(ParseCanonicalTimeBasedArgument(expression, "TIMEVALUE", "value", 0, value).Time);
    }

    private RuntimeValue EvaluateIsDateBuiltin(string value)
    {
        return RuntimeValue.FromBoolean(BuiltinDateTimeHelpers.IsCanonicalTemporalText(value));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin()
    {
        return RuntimeValue.FromInteger(checked((int)ResolveCurrentLocalUtcOffset().TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var dateTime = ExtractDateTimeValue(expression, "UTCOFFSET", value, 0);
        var offset = ResolveLocalUtcOffsetForLocalDateTime(expression, "UTCOFFSET", "value", 0, dateTime);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(BoundCallExpression expression, RuntimeValue value, string timeZoneId)
    {
        var dateTime = ExtractDateTimeValue(expression, "UTCOFFSET", value, 0);
        var timeZone = ResolveTimeZoneArgument(expression, "UTCOFFSET", "timeZone", 1, timeZoneId);
        var offset = ResolveUtcOffsetForLocalDateTime(expression, "UTCOFFSET", "value", 0, timeZone, dateTime);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateToUtcBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var dateTime = ExtractDateTimeValue(expression, "TOUTC", value, 0);

        try
        {
            var offset = ResolveLocalUtcOffsetForLocalDateTime(expression, "TOUTC", "value", 0, dateTime);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromDateTime(utcDateTime);
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

    private RuntimeValue EvaluateToUtcBuiltin(BoundCallExpression expression, RuntimeValue value, string timeZoneId)
    {
        var dateTime = ExtractDateTimeValue(expression, "TOUTC", value, 0);
        var timeZone = ResolveTimeZoneArgument(expression, "TOUTC", "timeZone", 1, timeZoneId);

        try
        {
            var offset = ResolveUtcOffsetForLocalDateTime(expression, "TOUTC", "value", 0, timeZone, dateTime);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromDateTime(utcDateTime);
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

    private RuntimeValue EvaluateFromUtcBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var dateTime = ExtractDateTimeValue(expression, "FROMUTC", value, 0);

        try
        {
            var localDateTime = ConvertUtcToLocalDateTime(dateTime);
            return RuntimeValue.FromDateTime(localDateTime);
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

    private RuntimeValue EvaluateFromUtcBuiltin(BoundCallExpression expression, RuntimeValue value, string timeZoneId)
    {
        var dateTime = ExtractDateTimeValue(expression, "FROMUTC", value, 0);
        var timeZone = ResolveTimeZoneArgument(expression, "FROMUTC", "timeZone", 1, timeZoneId);

        try
        {
            var localDateTime = BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(dateTime, timeZone);
            return RuntimeValue.FromDateTime(localDateTime);
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

    private RuntimeValue EvaluateStartOfMonthBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var date = ExtractDateValue(expression, "STARTOFMONTH", value, 0);
        return RuntimeValue.FromDate(new DateOnly(date.Year, date.Month, 1));
    }

    private RuntimeValue EvaluateEndOfMonthBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var date = ExtractDateValue(expression, "ENDOFMONTH", value, 0);
        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return RuntimeValue.FromDate(new DateOnly(date.Year, date.Month, lastDay));
    }

    private RuntimeValue EvaluateStartOfYearBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var date = ExtractDateValue(expression, "STARTOFYEAR", value, 0);
        return RuntimeValue.FromDate(new DateOnly(date.Year, 1, 1));
    }

    private RuntimeValue EvaluateEndOfYearBuiltin(BoundCallExpression expression, RuntimeValue value)
    {
        var date = ExtractDateValue(expression, "ENDOFYEAR", value, 0);
        return RuntimeValue.FromDate(new DateOnly(date.Year, 12, 31));
    }
}
