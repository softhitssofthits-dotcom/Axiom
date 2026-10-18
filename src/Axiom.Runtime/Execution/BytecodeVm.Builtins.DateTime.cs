using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    /// <summary>
    /// Функции текущего времени имеют разрешение до секунды: DATE отбрасывает время
    /// суток, TIME и NOW — доли секунды. Это документированная точность самих функций,
    /// а не скрытая потеря при записи. Значение с долями секунды остаётся выразимым
    /// каноническим текстом и печатается вместе с ними.
    /// </summary>
    private DateTime CurrentLocalInstant()
    {
        return BuiltinDateTimeHelpers.TruncateToSecond(_host.GetLocalNow().DateTime);
    }

    private RuntimeValue EvaluateDateBuiltin()
    {
        return RuntimeValue.FromDate(DateOnly.FromDateTime(CurrentLocalInstant()));
    }

    private RuntimeValue EvaluateDateBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("DATE", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertCurrentUtcToZone("DATE", timeZone, location);
        return RuntimeValue.FromDate(DateOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateTimeBuiltin()
    {
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(CurrentLocalInstant()));
    }

    private RuntimeValue EvaluateTimeBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("TIME", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertCurrentUtcToZone("TIME", timeZone, location);
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(currentDateTime));
    }

    private RuntimeValue EvaluateNowBuiltin()
    {
        return RuntimeValue.FromDateTime(CurrentLocalInstant());
    }

    private RuntimeValue EvaluateNowBuiltin(string timeZoneId, TextLocation location)
    {
        var timeZone = ResolveTimeZoneArgument("NOW", "timeZone", timeZoneId, location);
        var currentDateTime = ConvertCurrentUtcToZone("NOW", timeZone, location);
        return RuntimeValue.FromDateTime(currentDateTime);
    }

    /// <summary>
    /// Текущие часы в именованном поясе используют тот же языковой отказ, что и
    /// TOUTC/FROMUTC. Источник часов задаётся извне и может вернуть предельный
    /// момент; .NET-исключение не должно выходить за границы конвейера и обходить
    /// TRY/CATCH программы.
    /// </summary>
    private DateTime ConvertCurrentUtcToZone(string functionName, TimeZoneInfo timeZone, TextLocation location)
    {
        try
        {
            return BuiltinDateTimeHelpers.TruncateToSecond(
                BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(_host.GetLocalNow().UtcDateTime, timeZone));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                functionName,
                "timeZone",
                "must keep the current moment within canonical date-time range after time zone conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateUtcDateBuiltin()
    {
        return RuntimeValue.FromDate(DateOnly.FromDateTime(CurrentUtcInstant()));
    }

    private RuntimeValue EvaluateUtcTimeBuiltin()
    {
        return RuntimeValue.FromTime(TimeOnly.FromDateTime(CurrentUtcInstant()));
    }

    private RuntimeValue EvaluateUtcNowBuiltin()
    {
        return RuntimeValue.FromDateTime(CurrentUtcInstant());
    }

    private RuntimeValue EvaluateTimerBuiltin()
    {
        return RuntimeValue.FromDouble(_host.GetLocalNow().TimeOfDay.TotalSeconds);
    }

    private DateTime CurrentUtcInstant()
    {
        return BuiltinDateTimeHelpers.TruncateToSecond(_host.GetLocalNow().ToUniversalTime().DateTime);
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

    /// <summary>
    /// Сборка DATETIME из готовых DATE и TIME без промежуточного текста. Обе части
    /// уже проверены своими типами, поэтому дополнительных ограничений нет:
    /// календарная дата берётся целиком, время суток — целиком вместе с долями секунды.
    /// </summary>
    private RuntimeValue EvaluateDateTimeSerialBuiltin(RuntimeValue dateValue, RuntimeValue timeValue, TextLocation location)
    {
        var date = ExtractDateValue("DATETIMESERIAL", dateValue, location);
        var time = ExtractTimeValue("DATETIMESERIAL", timeValue, location);

        return RuntimeValue.FromDateTime(date.ToDateTime(time));
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

    private RuntimeValue EvaluateDateAddBuiltin(string interval, int number, RuntimeValue value, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEADD", "interval", interval, location);
        var temporal = BuiltinDateTimeHelpers.ToTemporalValue(value);
        var result = AddTemporalValue("DATEADD", temporal, intervalToken.Kind, number, location);
        return BuiltinDateTimeHelpers.ToRuntimeTemporalValue(result);
    }

    private RuntimeValue EvaluateDateDiffBuiltin(string interval, RuntimeValue fromValue, RuntimeValue toValue, TextLocation location)
    {
        var intervalToken = ParseDateTimeIntervalToken("DATEDIFF", "interval", interval, location);
        var fromTemporal = BuiltinDateTimeHelpers.ToTemporalValue(fromValue);
        var toTemporal = BuiltinDateTimeHelpers.ToTemporalValue(toValue);
        return RuntimeValue.FromLong(BuiltinDateTimeHelpers.EvaluateDateDiffTemporal(
            "DATEDIFF",
            intervalToken.Kind,
            fromTemporal,
            toTemporal,
            location,
            ReportBuiltInArgumentError));
    }

    private RuntimeValue EvaluateDatePartBuiltin(string part, RuntimeValue value, TextLocation location)
    {
        var partToken = ParseDateTimePartToken("DATEPART", "part", part, location);
        return RuntimeValue.FromInteger(BuiltinDateTimeHelpers.EvaluateDatePartTemporal(
            "DATEPART",
            partToken,
            BuiltinDateTimeHelpers.ToTemporalValue(value),
            location,
            ReportBuiltInArgumentError));
    }

    private RuntimeValue EvaluateDateValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromDate(ParseCanonicalDateBasedArgument("DATEVALUE", "value", value, location).Date);
    }

    /// <summary>
    /// Типизированное извлечение даты из DATETIME. Отдельная форма нужна, чтобы
    /// получить DATE без текста и без риска потерять компонент незаметно: вызов
    /// явно называет намерение, а не полагается на неявное преобразование.
    /// </summary>
    private RuntimeValue EvaluateDateValueFromDateTimeBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromDate(ExtractDateValue("DATEVALUE", value, location));
    }

    private RuntimeValue EvaluateTimeValueFromDateTimeBuiltin(RuntimeValue value, TextLocation location)
    {
        return RuntimeValue.FromTime(ExtractTimeValue("TIMEVALUE", value, location));
    }

    /// <summary>
    /// Явный разбор момента из текста с признаком Z. Смещение вида +03:00 не
    /// принимается: языка пока не имеет формы, сохраняющей смещение, и молчаливое
    /// приведение скрыло бы потерю.
    /// </summary>
    private RuntimeValue EvaluateInstantValueBuiltin(string value, TextLocation location)
    {
        if (BuiltinDateTimeHelpers.TryParseCanonicalInstant(value, out var instant))
        {
            return RuntimeValue.FromUtcDateTime(instant);
        }

        ReportBuiltInArgumentError(
            "INSTANTVALUE",
            "value",
            "must be a UTC instant in form 'yyyy-MM-ddTHH:mm:ssZ' with optional fractional seconds.",
            location);
        throw new RuntimeFailureException();
    }

    /// <summary>
    /// Прошедшее время этого запуска по монотонному счётчику. Перевод системных
    /// часов и полночь на значение не влияют, поэтому результат годится для
    /// измерения длительности, в отличие от разности TIMER.
    /// </summary>
    private RuntimeValue EvaluateElapsedSecondsSinceStartBuiltin()
    {
        var elapsed = _host.GetMonotonicSeconds() - _monotonicOriginSeconds;
        return RuntimeValue.FromDouble(elapsed < 0 ? 0 : elapsed);
    }

    /// <summary>
    /// Прошедшие секунды между двумя моментами. Знак сохраняется: отрицательный
    /// результат означает, что конечное событие раньше начального. Разность
    /// моментов однозначна и не зависит от пояса или перевода часов.
    /// </summary>
    private RuntimeValue EvaluateElapsedSecondsBetweenBuiltin(RuntimeValue fromValue, RuntimeValue toValue, TextLocation location)
    {
        var from = ExtractInstantValue("ELAPSEDSECONDS", fromValue, location);
        var to = ExtractInstantValue("ELAPSEDSECONDS", toValue, location);

        return RuntimeValue.FromDouble((to - from).TotalSeconds);
    }

    private RuntimeValue EvaluateNowInstantBuiltin()
    {
        return RuntimeValue.FromUtcDateTime(
            BuiltinDateTimeHelpers.TruncateToSecond(_host.GetLocalNow().UtcDateTime));
    }

    private RuntimeValue EvaluateToInstantBuiltin(RuntimeValue value, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("TOINSTANT", value, location);

        try
        {
            var offset = ResolveLocalUtcOffsetForLocalDateTime("TOINSTANT", "value", dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromUtcDateTime(utcDateTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOINSTANT",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateToInstantBuiltin(RuntimeValue value, string timeZoneId, TextLocation location)
    {
        var dateTime = ExtractDateTimeValue("TOINSTANT", value, location);
        var timeZone = ResolveTimeZoneArgument("TOINSTANT", "timeZone", timeZoneId, location);

        try
        {
            var offset = ResolveUtcOffsetForLocalDateTime("TOINSTANT", "value", timeZone, dateTime, location);
            var utcDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), offset).UtcDateTime;
            return RuntimeValue.FromUtcDateTime(utcDateTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "TOINSTANT",
                "value",
                "must remain within canonical date-time range after UTC conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    /// <summary>
    /// Обратное отображение момента в календарное время. Два разных момента могут
    /// дать одинаковую местную запись при переходе часов; это свойство календарного
    /// представления, а не дефект: сам момент остаётся однозначным.
    /// </summary>
    private RuntimeValue EvaluateFromInstantBuiltin(RuntimeValue value, TextLocation location)
    {
        var instant = ExtractInstantValue("FROMINSTANT", value, location);

        try
        {
            return RuntimeValue.FromDateTime(ConvertUtcToLocalDateTime(instant));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "FROMINSTANT",
                "value",
                "must remain within canonical date-time range after local conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFromInstantBuiltin(RuntimeValue value, string timeZoneId, TextLocation location)
    {
        var instant = ExtractInstantValue("FROMINSTANT", value, location);
        var timeZone = ResolveTimeZoneArgument("FROMINSTANT", "timeZone", timeZoneId, location);

        try
        {
            return RuntimeValue.FromDateTime(
                BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(instant, timeZone));
        }
        catch (ArgumentOutOfRangeException)
        {
            ReportBuiltInArgumentError(
                "FROMINSTANT",
                "value",
                "must remain within canonical date-time range after local conversion.",
                location);
            throw new RuntimeFailureException();
        }
    }

    private DateTime ExtractInstantValue(string functionName, RuntimeValue value, TextLocation location)
    {
        if (value.Type == TypeSymbol.UtcDateTime)
        {
            return value.AsUtcDateTime();
        }

        ReportBuiltInArgumentError(functionName, "value", "must be a UTCDATETIME value.", location);
        throw new RuntimeFailureException();
    }

    private RuntimeValue EvaluateTimeValueBuiltin(string value, TextLocation location)
    {
        return RuntimeValue.FromTime(ParseCanonicalTimeBasedArgument("TIMEVALUE", "value", value, location).Time);
    }

    /// <summary>
    /// Явный разбор текста формата обмена. Неявное преобразование строки в DATETIME
    /// остаётся строгим: здесь читается только каноническая запись и вариант с
    /// разделителем T. Смещение и суффикс Z не принимаются, потому что DATETIME не
    /// хранит часовой пояс.
    /// </summary>
    private RuntimeValue EvaluateDateTimeValueBuiltin(string value, TextLocation location)
    {
        if (BuiltinDateTimeHelpers.TryParseCanonicalDateTime(value, out var canonical))
        {
            return RuntimeValue.FromDateTime(canonical);
        }

        if (BuiltinDateTimeHelpers.TryParseExchangeDateTime(value, out var exchange))
        {
            return RuntimeValue.FromDateTime(exchange);
        }

        ReportBuiltInArgumentError(
            "DATETIMEVALUE",
            "value",
            "must be a canonical date-time in form 'yyyy-MM-dd HH:mm:ss' or an exchange date-time in form 'yyyy-MM-ddTHH:mm:ss', with optional fractional seconds and without a time zone offset.",
            location);
        throw new RuntimeFailureException();
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
            var localDateTime = BuiltinDateTimeHelpers.ConvertUtcToTimeZoneDateTime(dateTime, timeZone);
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

    private BuiltinDateTimeHelpers.IntervalTokenInfo ParseDateTimeIntervalToken(string functionName, string argumentName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseDateTimeIntervalToken(value, functionName, argumentName, location, ReportBuiltInArgumentError);

    private BuiltinDateTimeHelpers.PartTokenInfo ParseDateTimePartToken(string functionName, string argumentName, string value, TextLocation location)
        => BuiltinDateTimeHelpers.ParseDateTimePartToken(value, functionName, argumentName, location, ReportBuiltInArgumentError);

    private TimeZoneInfo ResolveTimeZoneArgument(string functionName, string argumentName, string value, TextLocation location)
    {
        return BuiltinDateTimeHelpers.ResolveTimeZoneArgument(
            functionName,
            argumentName,
            value,
            location,
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
        return BuiltinDateTimeHelpers.ResolveUtcOffsetForLocalDateTime(
            functionName,
            argumentName,
            timeZone,
            localDateTime,
            location,
            ReportBuiltInArgumentError);
    }

    private DateTime ConvertUtcToLocalDateTime(DateTime utcDateTime)
    {
        return BuiltinDateTimeHelpers.ConvertUtcToLocalDateTime(utcDateTime, _host);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalDateBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalDateBasedArgument(
            value,
            functionName,
            argumentName,
            location,
            ReportBuiltInArgumentError);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue ParseCanonicalTimeBasedArgument(
        string functionName,
        string argumentName,
        string value,
        TextLocation location)
    {
        return BuiltinDateTimeHelpers.ParseCanonicalTimeBasedArgument(
            value,
            functionName,
            argumentName,
            location,
            ReportBuiltInArgumentError);
    }

    private BuiltinDateTimeHelpers.CanonicalTemporalValue AddTemporalValue(
        string functionName,
        BuiltinDateTimeHelpers.CanonicalTemporalValue value,
        BuiltinDateTimeHelpers.DateTimeIntervalKind interval,
        int number,
        TextLocation location)
    {
        return BuiltinDateTimeHelpers.AddTemporalValue(
            functionName,
            value,
            interval,
            number,
            location,
            ReportBuiltInArgumentError);
    }

    private void EnsureCalendarYearArgument(string functionName, string argumentName, int year, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureCalendarYearArgument(
            functionName,
            argumentName,
            year,
            location,
            ReportBuiltInArgumentError);
    }

    private void EnsureCalendarMonthArgument(string functionName, string argumentName, int month, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureCalendarMonthArgument(
            functionName,
            argumentName,
            month,
            location,
            ReportBuiltInArgumentError);
    }

    private void EnsureDateSerialDay(int day, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureDateSerialDay(day, location, ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialHour(int hour, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialHour(hour, location, ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialMinute(int minute, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialMinute(minute, location, ReportBuiltInArgumentError);
    }

    private void EnsureTimeSerialSecond(int second, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureTimeSerialSecond(second, location, ReportBuiltInArgumentError);
    }

    private void EnsureWeekdayNameWeekday(int weekday, TextLocation location)
    {
        BuiltinDateTimeHelpers.EnsureWeekdayNameWeekday(weekday, location, ReportBuiltInArgumentError);
    }
}
