using System.Globalization;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinDateTimeHelpers
{
    internal const string CanonicalDateFormat = "yyyy-MM-dd";
    internal const string CanonicalTimeFormat = "HH:mm:ss";
    internal const string CanonicalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    internal static readonly CultureInfo DateTimeCulture = CultureInfo.InvariantCulture;

    internal static readonly IReadOnlyDictionary<string, IntervalTokenInfo> DateTimeIntervalTokens =
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

    internal static readonly IReadOnlyDictionary<string, PartTokenInfo> DateTimePartTokens =
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

    internal static readonly string[] CanonicalMonthNames =
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

    internal static readonly string[] CanonicalWeekdayNamesMondayFirst =
    [
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday"
    ];

    internal enum TemporalValueKind
    {
        Date,
        Time,
        DateTime
    }

    internal enum DateTimeIntervalKind
    {
        Year,
        Quarter,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

    internal enum DateTimePartKind
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

    internal readonly record struct CanonicalTemporalValue(TemporalValueKind Kind, DateOnly Date, TimeOnly Time)
    {
        public static CanonicalTemporalValue FromDate(DateOnly date) => new(TemporalValueKind.Date, date, default);

        public static CanonicalTemporalValue FromTime(TimeOnly time) => new(TemporalValueKind.Time, default, time);

        public static CanonicalTemporalValue FromDateTime(DateTime dateTime) => new(
            TemporalValueKind.DateTime,
            DateOnly.FromDateTime(dateTime),
            TimeOnly.FromDateTime(dateTime));

        public DateTime ToDateTime() => Date.ToDateTime(Time);
    }

    internal readonly record struct IntervalTokenInfo(DateTimeIntervalKind Kind, bool IsDateBased);

    internal readonly record struct PartTokenInfo(DateTimePartKind Kind, bool IsDateBased);

    internal static string FormatCanonicalDate(DateOnly date)
    {
        return date.ToString(CanonicalDateFormat, DateTimeCulture);
    }

    internal static string FormatCanonicalTime(TimeOnly time)
    {
        return time.ToString(CanonicalTimeFormat, DateTimeCulture);
    }

    internal static string FormatCanonicalDateTime(DateTime dateTime)
    {
        return dateTime.ToString(CanonicalDateTimeFormat, DateTimeCulture);
    }

    internal static string FormatCanonicalTemporal(CanonicalTemporalValue value)
    {
        return value.Kind switch
        {
            TemporalValueKind.Date => FormatCanonicalDate(value.Date),
            TemporalValueKind.Time => FormatCanonicalTime(value.Time),
            TemporalValueKind.DateTime => FormatCanonicalDateTime(value.ToDateTime()),
            _ => throw new InvalidOperationException($"Unsupported temporal kind '{value.Kind}'.")
        };
    }

    internal static bool TryConvertTemporalValue(RuntimeValue value, TypeSymbol targetType, out RuntimeValue converted)
    {
        converted = null!;

        if (targetType == TypeSymbol.String)
        {
            if (value.Type == TypeSymbol.Date)
            {
                converted = RuntimeValue.FromString(FormatCanonicalDate(value.AsDate()));
                return true;
            }

            if (value.Type == TypeSymbol.Time)
            {
                converted = RuntimeValue.FromString(FormatCanonicalTime(value.AsTime()));
                return true;
            }

            if (value.Type == TypeSymbol.DateTime)
            {
                converted = RuntimeValue.FromString(FormatCanonicalDateTime(value.AsDateTime()));
                return true;
            }

            return false;
        }

        if (targetType == TypeSymbol.Date)
        {
            if (value.Type == TypeSymbol.DateTime)
            {
                converted = RuntimeValue.FromDate(DateOnly.FromDateTime(value.AsDateTime()));
                return true;
            }

            if (value.Type == TypeSymbol.String)
            {
                if (TryParseCanonicalDateOnly(value.AsString(), out var date))
                {
                    converted = RuntimeValue.FromDate(date);
                    return true;
                }

                if (TryParseCanonicalDateTime(value.AsString(), out var dateTime))
                {
                    converted = RuntimeValue.FromDate(DateOnly.FromDateTime(dateTime));
                    return true;
                }

                return false;
            }

            return false;
        }

        if (targetType == TypeSymbol.Time)
        {
            if (value.Type == TypeSymbol.DateTime)
            {
                converted = RuntimeValue.FromTime(TimeOnly.FromDateTime(value.AsDateTime()));
                return true;
            }

            if (value.Type == TypeSymbol.String)
            {
                if (TryParseCanonicalTimeOnly(value.AsString(), out var time))
                {
                    converted = RuntimeValue.FromTime(time);
                    return true;
                }

                if (TryParseCanonicalDateTime(value.AsString(), out var dateTime))
                {
                    converted = RuntimeValue.FromTime(TimeOnly.FromDateTime(dateTime));
                    return true;
                }

                return false;
            }

            return false;
        }

        if (targetType == TypeSymbol.DateTime)
        {
            if (value.Type == TypeSymbol.Date)
            {
                converted = RuntimeValue.FromDateTime(value.AsDate().ToDateTime(TimeOnly.MinValue));
                return true;
            }

            if (value.Type == TypeSymbol.Time)
            {
                converted = RuntimeValue.FromDateTime(DateOnly.MinValue.ToDateTime(value.AsTime()));
                return true;
            }

            if (value.Type == TypeSymbol.String)
            {
                if (TryParseCanonicalDateTime(value.AsString(), out var dateTime))
                {
                    converted = RuntimeValue.FromDateTime(dateTime);
                    return true;
                }

                return false;
            }

            return false;
        }

        return false;
    }

    internal static bool TryParseCanonicalDateOnly(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, CanonicalDateFormat, DateTimeCulture, DateTimeStyles.None, out date);
    }

    internal static bool TryParseCanonicalTimeOnly(string value, out TimeOnly time)
    {
        return TimeOnly.TryParseExact(value, CanonicalTimeFormat, DateTimeCulture, DateTimeStyles.None, out time);
    }

    internal static bool TryParseCanonicalDateTime(string value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(value, CanonicalDateTimeFormat, DateTimeCulture, DateTimeStyles.None, out dateTime);
    }

    internal static bool TryParseCanonicalTemporal(string value, out CanonicalTemporalValue temporal)
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

    internal static bool IsCanonicalTemporalText(string value)
    {
        return TryParseCanonicalTemporal(value, out _);
    }

    internal static CanonicalTemporalValue AddDateBasedTemporal(
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

    internal static CanonicalTemporalValue AddTimeBasedTemporal(CanonicalTemporalValue value, long deltaTicks)
    {
        return value.Kind switch
        {
            TemporalValueKind.Time => CanonicalTemporalValue.FromTime(AddWrappedTime(value.Time, deltaTicks)),
            TemporalValueKind.DateTime => CanonicalTemporalValue.FromDateTime(value.ToDateTime().AddTicks(deltaTicks)),
            _ => throw new InvalidOperationException("Time-based arithmetic requires time or date-time input.")
        };
    }

    internal static TimeOnly AddWrappedTime(TimeOnly time, long deltaTicks)
    {
        var resultTicks = NormalizeTimeOfDayTicks(time.ToTimeSpan().Ticks + deltaTicks);
        return new TimeOnly(resultTicks);
    }

    internal static long NormalizeTimeOfDayTicks(long ticks)
    {
        var normalized = ticks % TimeSpan.TicksPerDay;

        if (normalized < 0)
        {
            normalized += TimeSpan.TicksPerDay;
        }

        return normalized;
    }

    internal static long GetQuarterBoundaryIndex(DateOnly date)
    {
        return checked((date.Year * 4L) + ((date.Month - 1) / 3));
    }

    internal static long EvaluateDateBasedDiff(DateTimeIntervalKind interval, DateOnly fromDate, DateOnly toDate)
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

    internal static int EvaluateDatePart(DateTimePartKind part, DateOnly date)
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

    internal static int EvaluateTimePart(DateTimePartKind part, TimeOnly time)
    {
        return part switch
        {
            DateTimePartKind.Hour => time.Hour,
            DateTimePartKind.Minute => time.Minute,
            DateTimePartKind.Second => time.Second,
            _ => throw new InvalidOperationException($"Part '{part}' is not time-based.")
        };
    }

    internal static long GetMonthBoundaryIndex(DateOnly date)
    {
        return checked((date.Year * 12L) + (date.Month - 1L));
    }

    internal static long GetTimeBoundaryIndex(TimeOnly time, DateTimeIntervalKind interval)
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

    internal static long GetDateTimeBoundaryIndex(DateTime dateTime, DateTimeIntervalKind interval)
    {
        return interval switch
        {
            DateTimeIntervalKind.Hour => dateTime.Ticks / TimeSpan.TicksPerHour,
            DateTimeIntervalKind.Minute => dateTime.Ticks / TimeSpan.TicksPerMinute,
            DateTimeIntervalKind.Second => dateTime.Ticks / TimeSpan.TicksPerSecond,
            _ => throw new InvalidOperationException($"Interval '{interval}' is not time-based.")
        };
    }

    internal static int GetMondayFirstWeekdayNumber(DateOnly date)
    {
        return ((int)date.DayOfWeek + 6) % 7 + 1;
    }

    internal static DateOnly ParseCanonicalDateArgument(
        string temporalText,
        string functionName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (TryParseCanonicalDateOnly(temporalText, out var date))
        {
            return date;
        }

        if (TryParseCanonicalDateTime(temporalText, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        reportError(
            functionName,
            "value",
            $"must be a canonical date or date-time in form '{CanonicalDateFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    internal static TimeOnly ParseCanonicalTimeArgument(
        string temporalText,
        string functionName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (TryParseCanonicalTimeOnly(temporalText, out var time))
        {
            return time;
        }

        if (TryParseCanonicalDateTime(temporalText, out var dateTime))
        {
            return TimeOnly.FromDateTime(dateTime);
        }

        reportError(
            functionName,
            "value",
            $"must be a canonical time or date-time in form '{CanonicalTimeFormat}' or '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    internal static DateTime ParseCanonicalDateTimeArgument(
        string temporalText,
        string functionName,
        string argName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (TryParseCanonicalDateTime(temporalText, out var dateTime))
        {
            return dateTime;
        }

        reportError(
            functionName,
            argName,
            $"must be a canonical date-time in form '{CanonicalDateTimeFormat}'.",
            location);
        throw new RuntimeFailureException();
    }

    internal static IntervalTokenInfo ParseDateTimeIntervalToken(
        string token,
        string functionName,
        string argName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (DateTimeIntervalTokens.TryGetValue(token, out var info))
        {
            return info;
        }

        reportError(
            functionName,
            argName,
            "must be one of: yyyy, q, m, d, h, n, s.",
            location);
        throw new RuntimeFailureException();
    }

    internal static PartTokenInfo ParseDateTimePartToken(
        string token,
        string functionName,
        string argName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (DateTimePartTokens.TryGetValue(token, out var info))
        {
            return info;
        }

        reportError(
            functionName,
            argName,
            "must be one of: yyyy, q, m, d, y, w, h, n, s.",
            location);
        throw new RuntimeFailureException();
    }
}
