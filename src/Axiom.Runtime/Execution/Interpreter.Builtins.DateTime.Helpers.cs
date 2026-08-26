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
        return BuiltinDateTimeHelpers.ResolveTimeZoneArgument(
            functionName,
            argumentName,
            value,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            _host,
            ReportBuiltInArgumentError);
    }

    private TimeSpan ResolveCurrentLocalUtcOffset()
    {
        return BuiltinDateTimeHelpers.ResolveCurrentLocalUtcOffset(_host);
    }

    private TimeSpan ResolveUtcOffsetAtCurrentInstant(TimeZoneInfo timeZone)
    {
        return BuiltinDateTimeHelpers.ResolveUtcOffsetAtCurrentInstant(timeZone, _host.GetLocalNow());
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
            _host.GetLocalTimeZone(),
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
        return BuiltinDateTimeHelpers.ResolveUtcOffsetForLocalDateTime(
            functionName,
            argumentName,
            timeZone,
            localDateTime,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private DateTime ConvertUtcToLocalDateTime(DateTime utcDateTime)
    {
        return BuiltinDateTimeHelpers.ConvertUtcToLocalDateTime(utcDateTime, _host);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalDateBasedArgument(
            value,
            functionName,
            argumentName,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalTimeBasedArgument(
            value,
            functionName,
            argumentName,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
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
        return BuiltinDateTimeHelpers.AddTemporalValue(
            functionName,
            value,
            interval,
            number,
            GetBuiltInArgumentLocation(expression, 2),
            ReportBuiltInArgumentError);
    }

    private void EnsureCalendarYearArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int year)
    {
        BuiltinDateTimeHelpers.EnsureCalendarYearArgument(
            functionName,
            argumentName,
            year,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private void EnsureCalendarMonthArgument(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int month)
    {
        BuiltinDateTimeHelpers.EnsureCalendarMonthArgument(
            functionName,
            argumentName,
            month,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            ReportBuiltInArgumentError);
    }

    private void EnsureDateSerialDay(BoundCallExpression expression, int day)
    {
        BuiltinDateTimeHelpers.EnsureDateSerialDay(
            day,
            GetBuiltInArgumentLocation(expression, 2),
            ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialHour(BoundCallExpression expression, int hour)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialHour(
            hour,
            GetBuiltInArgumentLocation(expression, 0),
            ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialMinute(BoundCallExpression expression, int minute)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialMinute(
            minute,
            GetBuiltInArgumentLocation(expression, 1),
            ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialSecond(BoundCallExpression expression, int second)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialSecond(
            second,
            GetBuiltInArgumentLocation(expression, 2),
            ReportBuiltInArgumentError);
    }

    private void EnsureWeekdayNameWeekday(BoundCallExpression expression, int weekday)
    {
        BuiltinDateTimeHelpers.EnsureWeekdayNameWeekday(
            weekday,
            GetBuiltInArgumentLocation(expression, 0),
            ReportBuiltInArgumentError);
    }
}
