using System.Globalization;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinDateTimeHelpers
{
    internal const string CanonicalDateFormat = "yyyy-MM-dd";
    internal const string CanonicalTimeFormat = "HH:mm:ss";
    internal const string CanonicalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Дробная часть записывается только при ненулевых долях секунды, поэтому
    /// значения с точностью до секунды сохраняют прежнюю краткую форму. Спецификатор
    /// FFFFFFF отбрасывает конечные нули и тем самым даёт самую короткую точную запись.
    /// </summary>
    internal const string CanonicalTimeFormatWithFraction = "HH:mm:ss.FFFFFFF";
    internal const string CanonicalDateTimeFormatWithFraction = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

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

    /// <summary>
    /// Документированное разрешение функций текущего времени. DATE отбрасывает время
    /// суток, TIME и NOW — доли секунды; TIMER остаётся дробным и служит измерению.
    /// </summary>
    internal static DateTime TruncateToSecond(DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
    }

    internal static string FormatCanonicalTime(TimeOnly time)
    {
        return time.Ticks % TimeSpan.TicksPerSecond == 0
            ? time.ToString(CanonicalTimeFormat, DateTimeCulture)
            : time.ToString(CanonicalTimeFormatWithFraction, DateTimeCulture);
    }

    internal static string FormatCanonicalDateTime(DateTime dateTime)
    {
        return dateTime.Ticks % TimeSpan.TicksPerSecond == 0
            ? dateTime.ToString(CanonicalDateTimeFormat, DateTimeCulture)
            : dateTime.ToString(CanonicalDateTimeFormatWithFraction, DateTimeCulture);
    }

    /// <summary>
    /// Результат неявного временного преобразования. Потеря календарной даты или
    /// времени суток отличается от простого несовпадения видов: вызывающая сторона
    /// обязана сообщить пользователю именно о потере компонента и назвать явную
    /// функцию, которая выполняет такое преобразование осознанно.
    /// </summary>
    internal enum TemporalConversionFailure
    {
        None,
        Unsupported,
        LosesComponent
    }

    internal static bool TryConvertTemporalValue(
        RuntimeValue value,
        TypeSymbol targetType,
        out RuntimeValue converted,
        out TemporalConversionFailure failure)
    {
        converted = null!;
        failure = TemporalConversionFailure.None;

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

            failure = TemporalConversionFailure.Unsupported;
            return false;
        }

        if (targetType == TypeSymbol.Date || targetType == TypeSymbol.Time)
        {
            // Содержимое строки известно только здесь, поэтому полный текст
            // даты-времени отвергается именно на этой границе. Скобки и переменная
            // не меняют договор: DATEVALUE и TIMEVALUE остаются единственным
            // явным способом отбросить компонент.
            if (value.Type == TypeSymbol.String)
            {
                if (targetType == TypeSymbol.Date && TryParseCanonicalDateOnly(value.AsString(), out var date))
                {
                    converted = RuntimeValue.FromDate(date);
                    return true;
                }

                if (targetType == TypeSymbol.Time && TryParseCanonicalTimeOnly(value.AsString(), out var time))
                {
                    converted = RuntimeValue.FromTime(time);
                    return true;
                }

                failure = TryParseCanonicalDateTime(value.AsString(), out _)
                    ? TemporalConversionFailure.LosesComponent
                    : TemporalConversionFailure.Unsupported;
                return false;
            }

            failure = value.Type == TypeSymbol.DateTime
                ? TemporalConversionFailure.LosesComponent
                : TemporalConversionFailure.Unsupported;
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

            if (value.Type == TypeSymbol.String
                && TryParseCanonicalDateTime(value.AsString(), out var dateTime))
            {
                converted = RuntimeValue.FromDateTime(dateTime);
                return true;
            }

            failure = TemporalConversionFailure.Unsupported;
            return false;
        }

        failure = TemporalConversionFailure.Unsupported;
        return false;
    }

    internal static bool TryParseCanonicalDateOnly(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, CanonicalDateFormat, DateTimeCulture, DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Разбор принимает и краткую форму, и запись с долями секунды. Договор
    /// обратимый: любой текст, который выдаёт FormatCanonicalTime/FormatCanonicalDateTime,
    /// читается обратно в то же значение.
    /// </summary>
    private static readonly string[] CanonicalTimeFormats =
    [
        CanonicalTimeFormat,
        CanonicalTimeFormatWithFraction
    ];

    private static readonly string[] CanonicalDateTimeFormats =
    [
        CanonicalDateTimeFormat,
        CanonicalDateTimeFormatWithFraction
    ];

    internal static bool TryParseCanonicalTimeOnly(string value, out TimeOnly time)
    {
        if (!HasValidFractionalPart(value))
        {
            time = default;
            return false;
        }

        return TimeOnly.TryParseExact(value, CanonicalTimeFormats, DateTimeCulture, DateTimeStyles.None, out time);
    }

    internal static bool TryParseCanonicalDateTime(string value, out DateTime dateTime)
    {
        if (!HasValidFractionalPart(value))
        {
            dateTime = default;
            return false;
        }

        return DateTime.TryParseExact(value, CanonicalDateTimeFormats, DateTimeCulture, DateTimeStyles.None, out dateTime);
    }

    /// <summary>
    /// Формат обмена отличается от канонической записи только разделителем T между
    /// датой и временем. Смещение и суффикс Z здесь не принимаются: DATETIME не
    /// хранит часовой пояс, и молча отбрасывать смещение значило бы потерять
    /// информацию о моменте. Для момента нужен отдельный тип (DTA-005).
    /// </summary>
    private static readonly string[] ExchangeDateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
    ];

    internal static bool TryParseExchangeDateTime(string value, out DateTime dateTime)
    {
        if (!HasValidFractionalPart(value))
        {
            dateTime = default;
            return false;
        }

        return DateTime.TryParseExact(value, ExchangeDateTimeFormats, DateTimeCulture, DateTimeStyles.None, out dateTime);
    }

    /// <summary>
    /// Точка без цифр не является дробной частью. Платформа принимает такую запись,
    /// поэтому договор проверяется здесь явно: после точки должно быть от одной до
    /// семи цифр, иначе текст считается недопустимым целиком.
    /// </summary>
    private static bool HasValidFractionalPart(string value)
    {
        var dot = value.LastIndexOf('.');
        if (dot < 0)
        {
            return true;
        }

        var digits = value.Length - dot - 1;
        if (digits is < 1 or > 7)
        {
            return false;
        }

        for (var index = dot + 1; index < value.Length; index++)
        {
            if (!char.IsAsciiDigit(value[index]))
            {
                return false;
            }
        }

        return true;
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

    internal static CanonicalTemporalValue AddTimeBasedTemporal(CanonicalTemporalValue value, int number, long ticksPerUnit)
    {
        var effectiveNumber = value.Kind == TemporalValueKind.Time
            ? number % (TimeSpan.TicksPerDay / ticksPerUnit)
            : number;
        var deltaTicks = checked(effectiveNumber * ticksPerUnit);
        return value.Kind switch
        {
            TemporalValueKind.Time => CanonicalTemporalValue.FromTime(AddWrappedTime(value.Time, deltaTicks)),
            TemporalValueKind.DateTime => CanonicalTemporalValue.FromDateTime(value.ToDateTime().AddTicks(deltaTicks)),
            _ => throw new InvalidOperationException("Time-based arithmetic requires time or date-time input.")
        };
    }

    internal static TimeOnly AddWrappedTime(TimeOnly time, long deltaTicks)
    {
        var resultTicks = NormalizeTimeOfDayTicks(time.ToTimeSpan().Ticks + NormalizeTimeOfDayTicks(deltaTicks));
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
            $"must be a canonical date or date-time in form '{CanonicalDateFormat}' or '{CanonicalDateTimeFormat}' with optional fractional seconds.",
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
            $"must be a canonical time or date-time in form '{CanonicalTimeFormat}' or '{CanonicalDateTimeFormat}' with optional fractional seconds.",
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
            $"must be a canonical date-time in form '{CanonicalDateTimeFormat}' with optional fractional seconds.",
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

    internal static CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        string value,
        string functionName,
        string argumentName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (TryParseCanonicalDateOnly(value, out var date))
        {
            return CanonicalTemporalValue.FromDate(date);
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return CanonicalTemporalValue.FromDateTime(dateTime);
        }

        reportError(
            functionName,
            argumentName,
            $"must be a canonical date or date-time in form '{CanonicalDateFormat}' or '{CanonicalDateTimeFormat}' with optional fractional seconds.",
            location);
        throw new RuntimeFailureException();
    }

    internal static CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        string value,
        string functionName,
        string argumentName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (TryParseCanonicalTimeOnly(value, out var time))
        {
            return CanonicalTemporalValue.FromTime(time);
        }

        if (TryParseCanonicalDateTime(value, out var dateTime))
        {
            return CanonicalTemporalValue.FromDateTime(dateTime);
        }

        reportError(
            functionName,
            argumentName,
            $"must be a canonical time or date-time in form '{CanonicalTimeFormat}' or '{CanonicalDateTimeFormat}' with optional fractional seconds.",
            location);
        throw new RuntimeFailureException();
    }

    internal static TimeZoneInfo ResolveTimeZoneArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location,
        IRuntimeHost host,
        Action<string, string, string, TextLocation> reportError)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            reportError(
                functionName,
                argumentName,
                "must name 'UTC', 'LOCAL' or a known host time zone identifier.",
                location);
            throw new RuntimeFailureException();
        }

        if (host.TryResolveTimeZone(value, out var timeZone))
        {
            return timeZone;
        }

        reportError(
            functionName,
            argumentName,
            "must name 'UTC', 'LOCAL' or a known host time zone identifier.",
            location);
        throw new RuntimeFailureException();
    }

    internal static TimeSpan ResolveUtcOffsetAtCurrentInstant(TimeZoneInfo timeZone, DateTimeOffset localNow)
    {
        return timeZone.GetUtcOffset(localNow.UtcDateTime);
    }

    internal static TimeSpan ResolveCurrentLocalUtcOffset(IRuntimeHost host)
    {
        return ResolveUtcOffsetAtCurrentInstant(host.GetLocalTimeZone(), host.GetLocalNow());
    }

    internal static TimeSpan ResolveUtcOffsetForLocalDateTime(
        string functionName,
        string argumentName,
        TimeZoneInfo timeZone,
        DateTime localDateTime,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        var unspecifiedLocalDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(unspecifiedLocalDateTime))
        {
            reportError(
                functionName,
                argumentName,
                "must refer to an existing local date-time and not fall into a skipped clock transition.",
                location);
            throw new RuntimeFailureException();
        }

        if (timeZone.IsAmbiguousTime(unspecifiedLocalDateTime))
        {
            reportError(
                functionName,
                argumentName,
                "must refer to an unambiguous local date-time and not fall into a repeated clock transition.",
                location);
            throw new RuntimeFailureException();
        }

        return timeZone.GetUtcOffset(unspecifiedLocalDateTime);
    }

    internal static DateTime ConvertUtcToTimeZoneDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
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

    internal static DateTime ConvertUtcToLocalDateTime(DateTime utcDateTime, IRuntimeHost host)
    {
        return ConvertUtcToTimeZoneDateTime(utcDateTime, host.GetLocalTimeZone());
    }

    internal static CanonicalTemporalValue AddTemporalValue(
        string functionName,
        CanonicalTemporalValue value,
        DateTimeIntervalKind interval,
        int number,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        try
        {
            return interval switch
            {
                DateTimeIntervalKind.Year => AddDateBasedTemporal(value, static (date, amount) => date.AddYears(amount), static (dateTime, amount) => dateTime.AddYears(amount), number),
                DateTimeIntervalKind.Quarter => AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(checked(amount * 3)), static (dateTime, amount) => dateTime.AddMonths(checked(amount * 3)), number),
                DateTimeIntervalKind.Month => AddDateBasedTemporal(value, static (date, amount) => date.AddMonths(amount), static (dateTime, amount) => dateTime.AddMonths(amount), number),
                DateTimeIntervalKind.Day => AddDateBasedTemporal(value, static (date, amount) => date.AddDays(amount), static (dateTime, amount) => dateTime.AddDays(amount), number),
                DateTimeIntervalKind.Hour => AddTimeBasedTemporal(value, number, TimeSpan.TicksPerHour),
                DateTimeIntervalKind.Minute => AddTimeBasedTemporal(value, number, TimeSpan.TicksPerMinute),
                DateTimeIntervalKind.Second => AddTimeBasedTemporal(value, number, TimeSpan.TicksPerSecond),
                _ => throw new InvalidOperationException($"Unsupported interval '{interval}'.")
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            reportError(
                functionName,
                "value",
                "must remain within canonical date/time range after arithmetic.",
                location);
            throw new RuntimeFailureException();
        }
        catch (OverflowException)
        {
            reportError(
                functionName,
                "value",
                "must remain within canonical date/time range after arithmetic.",
                location);
            throw new RuntimeFailureException();
        }
        catch (InvalidOperationException ex)
        {
            reportError(
                functionName,
                "value",
                $"is not supported for the given date/time kind: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    internal static long EvaluateTimeBasedDiff(
        DateTimeIntervalKind interval,
        CanonicalTemporalValue fromTemporal,
        CanonicalTemporalValue toTemporal,
        string functionName,
        string argumentName,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (fromTemporal.Kind == TemporalValueKind.Time && toTemporal.Kind == TemporalValueKind.Time)
        {
            return GetTimeBoundaryIndex(toTemporal.Time, interval) - GetTimeBoundaryIndex(fromTemporal.Time, interval);
        }

        if (fromTemporal.Kind == TemporalValueKind.DateTime && toTemporal.Kind == TemporalValueKind.DateTime)
        {
            return GetDateTimeBoundaryIndex(toTemporal.ToDateTime(), interval) - GetDateTimeBoundaryIndex(fromTemporal.ToDateTime(), interval);
        }

        reportError(
            functionName,
            argumentName,
            "must use values of the same temporal kind (time or date-time).",
            location);
        throw new RuntimeFailureException();
    }

    internal static bool IsDateBasedInterval(DateTimeIntervalKind interval)
    {
        return interval is DateTimeIntervalKind.Year
            or DateTimeIntervalKind.Quarter
            or DateTimeIntervalKind.Month
            or DateTimeIntervalKind.Day;
    }

    internal static CanonicalTemporalValue ToTemporalValue(RuntimeValue value)
    {
        if (value.Type == TypeSymbol.Date)
        {
            return CanonicalTemporalValue.FromDate(value.AsDate());
        }

        if (value.Type == TypeSymbol.Time)
        {
            return CanonicalTemporalValue.FromTime(value.AsTime());
        }

        if (value.Type == TypeSymbol.DateTime)
        {
            return CanonicalTemporalValue.FromDateTime(value.AsDateTime());
        }

        throw new InvalidOperationException($"Unsupported temporal type '{value.Type.Name}'.");
    }

    internal static RuntimeValue ToRuntimeTemporalValue(CanonicalTemporalValue value)
    {
        return value.Kind switch
        {
            TemporalValueKind.Date => RuntimeValue.FromDate(value.Date),
            TemporalValueKind.Time => RuntimeValue.FromTime(value.Time),
            TemporalValueKind.DateTime => RuntimeValue.FromDateTime(value.ToDateTime()),
            _ => throw new InvalidOperationException($"Unsupported temporal kind '{value.Kind}'.")
        };
    }

    internal static long EvaluateDateDiffTemporal(
        string functionName,
        DateTimeIntervalKind interval,
        CanonicalTemporalValue fromTemporal,
        CanonicalTemporalValue toTemporal,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (IsDateBasedInterval(interval))
        {
            if (fromTemporal.Kind is TemporalValueKind.Date or TemporalValueKind.DateTime
                && toTemporal.Kind is TemporalValueKind.Date or TemporalValueKind.DateTime)
            {
                return EvaluateDateBasedDiff(interval, fromTemporal.Date, toTemporal.Date);
            }

            reportError(
                functionName,
                "fromValue",
                "must be a DATE or DATETIME value for date-based intervals.",
                location);
            throw new RuntimeFailureException();
        }

        if (fromTemporal.Kind is TemporalValueKind.Time or TemporalValueKind.DateTime
            && toTemporal.Kind is TemporalValueKind.Time or TemporalValueKind.DateTime)
        {
            return EvaluateTimeBasedDiff(interval, fromTemporal, toTemporal, functionName, "toValue", location, reportError);
        }

        reportError(
            functionName,
            "fromValue",
            "must be a TIME or DATETIME value for time-based intervals.",
            location);
        throw new RuntimeFailureException();
    }

    internal static int EvaluateDatePartTemporal(
        string functionName,
        PartTokenInfo part,
        CanonicalTemporalValue temporal,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (part.IsDateBased)
        {
            if (temporal.Kind is TemporalValueKind.Date or TemporalValueKind.DateTime)
            {
                return EvaluateDatePart(part.Kind, temporal.Date);
            }

            reportError(
                functionName,
                "value",
                "must be a DATE or DATETIME value for date-based parts.",
                location);
            throw new RuntimeFailureException();
        }

        if (temporal.Kind is TemporalValueKind.Time or TemporalValueKind.DateTime)
        {
            return EvaluateTimePart(part.Kind, temporal.Time);
        }

        reportError(
            functionName,
            "value",
            "must be a TIME or DATETIME value for time-based parts.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureCalendarYearArgument(
        string functionName,
        string argumentName,
        int year,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (year is >= 1 and <= 9999)
        {
            return;
        }

        reportError(
            functionName,
            argumentName,
            "must be in range 1..9999.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureCalendarMonthArgument(
        string functionName,
        string argumentName,
        int month,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (month is >= 1 and <= 12)
        {
            return;
        }

        reportError(
            functionName,
            argumentName,
            "must be in range 1..12.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureDateSerialDay(
        int day,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (day is >= 1 and <= 31)
        {
            return;
        }

        reportError(
            "DATESERIAL",
            "day",
            "must be in range 1..31.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureTimeSerialHour(
        int hour,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (hour is >= 0 and <= 23)
        {
            return;
        }

        reportError(
            "TIMESERIAL",
            "hour",
            "must be in range 0..23.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureTimeSerialMinute(
        int minute,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (minute is >= 0 and <= 59)
        {
            return;
        }

        reportError(
            "TIMESERIAL",
            "minute",
            "must be in range 0..59.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureTimeSerialSecond(
        int second,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (second is >= 0 and <= 59)
        {
            return;
        }

        reportError(
            "TIMESERIAL",
            "second",
            "must be in range 0..59.",
            location);
        throw new RuntimeFailureException();
    }

    internal static void EnsureWeekdayNameWeekday(
        int weekday,
        TextLocation location,
        Action<string, string, string, TextLocation> reportError)
    {
        if (weekday is >= 1 and <= 7)
        {
            return;
        }

        reportError(
            "WEEKDAYNAME",
            "weekday",
            "must be in range 1..7 according to Monday-first numbering.",
            location);
        throw new RuntimeFailureException();
    }
}
