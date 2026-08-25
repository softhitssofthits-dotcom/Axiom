using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromDate(DateOnly.FromDateTime(_host.GetLocalNow().DateTime));
    }

    private RuntimeValue EvaluateDateBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("DATE", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromDate(DateOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(_host.GetLocalNow().DateTime));
    }

    private RuntimeValue EvaluateTimeBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("TIME", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromDateTime(_host.GetLocalNow().DateTime);
    }

    private RuntimeValue EvaluateNowBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("NOW", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone);
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

    private RuntimeValue EvaluateDateSerialBuiltin(int year, int month, int day, TextLocation location)
    {
        EnsureCalendarYearArgument("DATESERIAL", "year", year, location);
        EnsureCalendarMonthArgument("DATESERIAL", "month", month, location);
        EnsureDateSerialDay(day, location);

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
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTimeSerialBuiltin(int hour, int minute, int second, TextLocation location)
    {
        EnsureTimeSerialHour(hour, location);
        EnsureTimeSerialMinute(minute, location);
        EnsureTimeSerialSecond(second, location);

        return RuntimeValue.FromTime(new TimeOnly(hour, minute, second));
    }

    private RuntimeValue EvaluateYearBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractDateValue("YEAR", value, location).Year);
    }

    private RuntimeValue EvaluateMonthBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractDateValue("MONTH", value, location).Month);
    }

    private RuntimeValue EvaluateMonthNameBuiltin(int month, TextLocation location)
    {
        EnsureCalendarMonthArgument("MONTHNAME", "month", month, location);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalMonthNames[month - 1]);
    }

    private RuntimeValue EvaluateDayBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractDateValue("DAY", value, location).Day);
    }

    private RuntimeValue EvaluateHourBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue("HOUR", value, location).Hour);
    }

    private RuntimeValue EvaluateMinuteBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue("MINUTE", value, location).Minute);
    }

    private RuntimeValue EvaluateSecondBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(ExtractTimeValue("SECOND", value, location).Second);
    }

    private RuntimeValue EvaluateWeekdayBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.GetMondayFirstWeekdayNumber(ExtractDateValue("WEEKDAY", value, location)));
    }

    private RuntimeValue EvaluateWeekdayNameBuiltin(int weekday, TextLocation location)
    {
        EnsureWeekdayNameWeekday(weekday, location);
        return RuntimeValue.FromString(BuiltinDateTimeHelpers.CanonicalWeekdayNamesMondayFirst[weekday - 1]);
    }

    private RuntimeValue EvaluateDateAddBuiltin(string interval, int number, string value, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEADD", "interval", interval, location);
        var temporal = intervalToken.IsDateBased
            ? ParseCanonicalDateBasedArgument("DATEADD", "value", value, location)
            : ParseCanonicalTimeBasedArgument("DATEADD", "value", value, location);

        try
        {
            return RuntimeValue.FromString(BuiltinDateTimeHelpers.FormatCanonicalTemporal(AddTemporalValue("DATEADD", temporal, intervalToken.Kind, number, location)));
        }
        catch (InvalidOperationException ex)
        {
            ReportBuiltInArgumentError(
                "DATEADD",
                "value",
                $"не поддерживается для данного типа даты/времени: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDateDiffBuiltin(string interval, string fromValue, string toValue, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEDIFF", "interval", interval, location);

        try
        {
            if (intervalToken.IsDateBased)
            {
                var fromTemporal = ParseCanonicalDateBasedArgument("DATEDIFF", "fromValue", fromValue, location);
                var toTemporal = ParseCanonicalDateBasedArgument("DATEDIFF", "toValue", toValue, location);
                return RuntimeValue.FromLong(BuiltinDateTimeHelpers.EvaluateDateBasedDiff(intervalToken.Kind, fromTemporal.Date, toTemporal.Date));
            }

            var fromTimeTemporal = ParseCanonicalTimeBasedArgument("DATEDIFF", "fromValue", fromValue, location);
            var toTimeTemporal = ParseCanonicalTimeBasedArgument("DATEDIFF", "toValue", toValue, location);
            return RuntimeValue.FromLong(EvaluateTimeBasedDiff(intervalToken.Kind, fromTimeTemporal, toTimeTemporal));
        }
        catch (InvalidOperationException ex)
        {
            ReportBuiltInArgumentError(
                "DATEDIFF",
                "interval",
                $"не поддерживается для данного типа даты/времени: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDatePartBuiltin(string part, string value, TextLocation location)
    {
        var partToken = ParseDateTimePartToken("DATEPART", "part", part, location);

        try
        {
            if (partToken.IsDateBased)
            {
                var temporal = ParseCanonicalDateBasedArgument("DATEPART", "value", value, location);
                return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateDatePart(partToken.Kind, temporal.Date));
            }

            var timeTemporal = ParseCanonicalTimeBasedArgument("DATEPART", "value", value, location);
            return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateTimePart(partToken.Kind, timeTemporal.Time));
        }
        catch (InvalidOperationException ex)
        {
            ReportBuiltInArgumentError(
                "DATEPART",
                "part",
                $"не поддерживается для данного типа даты/времени: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDateValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromDate(ParseCanonicalDateBasedArgument("DATEVALUE", "value", value, location).Date);
    }

    private RuntimeValue EvaluateTimeValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromTime(ParseCanonicalTimeBasedArgument("TIMEVALUE", "value", value, location).Time);
    }

    private RuntimeValue EvaluateIsDateBuiltin(string value)
    {
        return RuntimeValue.FromBoolean(BuiltinDateTimeHelpers.IsCanonicalTemporalText(value));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin()
    {
        return RuntimeValue.FromInteger(checked((int)ResolveCurrentLocalUtcOffset().TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(RuntimeValue value, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("UTCOFFSET", value, location);
        var offset = ResolveLocalUtcOffsetForLocalDateTime("UTCOFFSET", "value", dateTime, location);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateUtcOffsetBuiltin(RuntimeValue value, string timeZoneId, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("UTCOFFSET", value, location);
        var timeZone = ResolveTimeZoneArgument("UTCOFFSET", "timeZone", timeZoneId, location);
        var offset = ResolveUtcOffsetForLocalDateTime("UTCOFFSET", "value", timeZone, dateTime, location);
        return RuntimeValue.FromInteger(checked((int)offset.TotalMinutes));
    }

    private RuntimeValue EvaluateToUtcBuiltin(RuntimeValue value, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("TOUTC", value, location);

        try
        {
            var offset = ResolveLocalUtcOffsetForLocalDateTime("TOUTC", "value", dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromDateTime(utcDateTime);
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

    private RuntimeValue EvaluateToUtcBuiltin(RuntimeValue value, string timeZoneId, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("TOUTC", value, location);
        var timeZone = ResolveTimeZoneArgument("TOUTC", "timeZone", timeZoneId, location);

        try
        {
            var offset = ResolveUtcOffsetForLocalDateTime("TOUTC", "value", timeZone, dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromDateTime(utcDateTime);
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

    private RuntimeValue EvaluateFromUtcBuiltin(RuntimeValue value, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("FROMUTC", value, location);

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
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromUtcBuiltin(RuntimeValue value, string timeZoneId, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("FROMUTC", value, location);
        var timeZone = ResolveTimeZoneArgument("FROMUTC", "timeZone", timeZoneId, location);

        try
        {
            var localDateTime = ConvertUtcToTimeZoneDateTime(dateTime, timeZone);
            return RuntimeValue.FromDateTime(localDateTime);
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

    private RuntimeValue EvaluateStartOfMonthBuiltin(RuntimeValue value, TextLocation location)
    {
        var date = ExtractDateValue("STARTOFMONTH", value, location);
        return RuntimeValue.FromDate(new DateOnly(date.Year, date.Month, 1));
    }

    private RuntimeValue EvaluateEndOfMonthBuiltin(RuntimeValue value, TextLocation location)
    {
        var date = ExtractDateValue("ENDOFMONTH", value, location);
        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return RuntimeValue.FromDate(new DateOnly(date.Year, date.Month, lastDay));
    }

    private RuntimeValue EvaluateStartOfYearBuiltin(RuntimeValue value, TextLocation location)
    {
        var date = ExtractDateValue("STARTOFYEAR", value, location);
        return RuntimeValue.FromDate(new DateOnly(date.Year, 1, 1));
    }

    private RuntimeValue EvaluateEndOfYearBuiltin(RuntimeValue value, TextLocation location)
    {
        var date = ExtractDateValue("ENDOFYEAR", value, location);
        return RuntimeValue.FromDate(new DateOnly(date.Year, 12, 31));
    }

    private DateOnly ExtractDateValue(string functionName, RuntimeValue value, TextLocation location)
    {
        if (value.Type == TypeSymbol.Date)
        {
            return value.AsDate();
        }

        if (value.Type == TypeSymbol.DateTime)
        {
            return DateOnly.FromDateTime(value.AsDateTime());
        }

        if (value.Type == TypeSymbol.String)
        {
            return BuiltinDateTimeHelpers.ParseCanonicalDateArgument(value.AsString(), functionName, location, ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(functionName, "value", "must be a DATE, DATETIME or canonical date string.", location);
        throw new RuntimeFailureException();
    }

    private TimeOnly ExtractTimeValue(string functionName, RuntimeValue value, TextLocation location)
    {
        if (value.Type == TypeSymbol.Time)
        {
            return value.AsTime();
        }

        if (value.Type == TypeSymbol.DateTime)
        {
            return TimeOnly.FromDateTime(value.AsDateTime());
        }

        if (value.Type == TypeSymbol.String)
        {
            return BuiltinDateTimeHelpers.ParseCanonicalTimeArgument(value.AsString(), functionName, location, ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(functionName, "value", "must be a TIME, DATETIME or canonical time string.", location);
        throw new RuntimeFailureException();
    }

    private DateTime ExtractDateTimeValue(string functionName, RuntimeValue value, TextLocation location)
    {
        if (value.Type == TypeSymbol.DateTime)
        {
            return value.AsDateTime();
        }

        if (value.Type == TypeSymbol.Date)
        {
            return value.AsDate().ToDateTime(TimeOnly.MinValue);
        }

        if (value.Type == TypeSymbol.String)
        {
            return BuiltinDateTimeHelpers.ParseCanonicalDateTimeArgument(value.AsString(), functionName, "value", location, ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(functionName, "value", "must be a DATETIME, DATE or canonical date-time string.", location);
        throw new RuntimeFailureException();
    }

    private DateOnly ParseCanonicalDateArgument(string functionName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseCanonicalDateArgument(value, functionName, location, ReportBuiltInArgumentError);

    private TimeOnly ParseCanonicalTimeArgument(string functionName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseCanonicalTimeArgument(value, functionName, location, ReportBuiltInArgumentError);

    private DateTime ParseCanonicalDateTimeArgument(string functionName, string argumentName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseCanonicalDateTimeArgument(value, functionName, argumentName, location, ReportBuiltInArgumentError);

    private BuiltinDateTimeHelpers.IntervalTokenInfo ParseDateTimeIntervalToken(string functionName, string argumentName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseDateTimeIntervalToken(value, functionName, argumentName, location, ReportBuiltInArgumentError);

    private BuiltinDateTimeHelpers.PartTokenInfo ParseDateTimePartToken(string functionName, string argumentName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseDateTimePartToken(value, functionName, argumentName, location, ReportBuiltInArgumentError);

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

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        if (BuiltinDateTimeHelpers.TryParseCanonicalDateOnly(value, out var date))
        {
            return BuiltinDateTimeHelpers.CanonicalTemporalValue.FromDate(date);
        }

        if (BuiltinDateTimeHelpers.TryParseCanonicalDateTime(value, out var dateTime))
        {
            return BuiltinDateTimeHelpers.CanonicalTemporalValue.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical date or date-time in form '{BuiltinDateTimeHelpers.CanonicalDateFormat}' or '{BuiltinDateTimeHelpers.CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        if (BuiltinDateTimeHelpers.TryParseCanonicalTimeOnly(value, out var time))
        {
            return BuiltinDateTimeHelpers.CanonicalTemporalValue.FromTime(time);
        }

        if (BuiltinDateTimeHelpers.TryParseCanonicalDateTime(value, out var dateTime))
        {
            return BuiltinDateTimeHelpers.CanonicalTemporalValue.FromDateTime(dateTime);
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"must be a canonical time or date-time in form '{BuiltinDateTimeHelpers.CanonicalTimeFormat}' or '{BuiltinDateTimeHelpers.CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue AddTemporalValue(
        string functionName,
        BuiltinDateTimeHelpers.CanonicalTemporalValue value,
        BuiltinDateTimeHelpers.DateTimeIntervalKind interval,
        int number,
        TextLocation location)
    {
        try
        {
            return interval switch
            {
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Year => BuiltinDateTimeHelpers.AddDateBasedTemporal(value, static (date, amount) => date.AddYears(amount), static (dateTime, amount) => dateTime.AddYears(amount), number),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Quarter => BuiltinDateTimeHelpers.AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(checked(amount * 3)), static (dateTime, amount) => dateTime.AddMonths(checked(amount * 3)), number),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Month => BuiltinDateTimeHelpers.AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(amount), static (dateTime, amount) => dateTime.AddMonths(amount), number),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Day => BuiltinDateTimeHelpers.AddDateBasedTemporal(value, static (date, amount) => date.AddDays(amount), static (dateTime, amount) => dateTime.AddDays(amount), number),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Hour => BuiltinDateTimeHelpers.AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerHour)),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Minute => BuiltinDateTimeHelpers.AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerMinute)),
                BuiltinDateTimeHelpers.DateTimeIntervalKind.Second => BuiltinDateTimeHelpers.AddTimeBasedTemporal(value, checked((long)number * TimeSpan.TicksPerSecond)),
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
        catch (InvalidOperationException ex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                $"не поддерживается для данного типа даты/времени: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
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
}
