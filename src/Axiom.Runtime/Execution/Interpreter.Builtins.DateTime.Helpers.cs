using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private DateOnly ExtractDateValue(BoundCallExpression expression, string functionName, RuntimeValue value, int argumentIndex)
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
            return BuiltinDateTimeHelpers.ParseCanonicalDateArgument(
                value.AsString(),
                functionName,
                GetBuiltInArgumentLocation(expression, argumentIndex),
                ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(
            functionName,
            "value",
            "must be a DATE, DATETIME or canonical date string.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private TimeOnly ExtractTimeValue(BoundCallExpression expression, string functionName, RuntimeValue value, int argumentIndex)
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
            return BuiltinDateTimeHelpers.ParseCanonicalTimeArgument(
                value.AsString(),
                functionName,
                GetBuiltInArgumentLocation(expression, argumentIndex),
                ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(
            functionName,
            "value",
            "must be a TIME, DATETIME or canonical time string.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private DateTime ExtractDateTimeValue(BoundCallExpression expression, string functionName, RuntimeValue value, int argumentIndex)
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
            return BuiltinDateTimeHelpers.ParseCanonicalDateTimeArgument(
                value.AsString(),
                functionName,
                "value",
                GetBuiltInArgumentLocation(expression, argumentIndex),
                ReportBuiltInArgumentError);
        }

        ReportBuiltInArgumentError(
            functionName,
            "value",
            "must be a DATETIME, DATE or canonical date-time string.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private DateOnly ParseCanonicalDateArgument(BoundCallExpression expression, string functionName, string value)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalDateArgument(
            value,
            functionName,
            GetBuiltInArgumentLocation(expression, 0),
            ReportBuiltInArgumentError);
    }

    private TimeOnly ParseCanonicalTimeArgument(BoundCallExpression expression, string functionName, string value)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalTimeArgument(
            value,
            functionName,
            GetBuiltInArgumentLocation(expression, 0),
            ReportBuiltInArgumentError);
    }

    private DateTime ParseCanonicalDateTimeArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalDateTimeArgument(
            value,
            functionName,
            argumentName,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
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

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
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
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
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
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private BuiltinDateTimeHelpers.IntervalTokenInfo ParseDateTimeIntervalToken(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        return BuiltinDateTimeHelpers.ParseDateTimeIntervalToken(
            value,
            functionName,
            argumentName,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private BuiltinDateTimeHelpers.PartTokenInfo ParseDateTimePartToken(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        return BuiltinDateTimeHelpers.ParseDateTimePartToken(
            value,
            functionName,
            argumentName,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue AddTemporalValue(
        BoundCallExpression expression,
        string functionName,
        BuiltinDateTimeHelpers.CanonicalTemporalValue value,
        BuiltinDateTimeHelpers.DateTimeIntervalKind interval,
        int number)
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
        catch (InvalidOperationException ex)
        {
            ReportBuiltInArgumentError(
                functionName,
                "value",
                $"не поддерживается для данного типа даты/времени: {ex.Message}",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }
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
}
