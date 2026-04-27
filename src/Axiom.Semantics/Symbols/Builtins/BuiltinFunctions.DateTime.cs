namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetDateTimeDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Date, "DATE", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.DateInZone, "DATE", TypeSymbol.String, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Time, "TIME", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.TimeInZone, "TIME", TypeSymbol.String, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Now, "NOW", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.NowInZone, "NOW", TypeSymbol.String, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UtcDate, "UTCDATE", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.UtcTime, "UTCTIME", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.UtcNow, "UTCNOW", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.Timer, "TIMER", TypeSymbol.Double, []),
            CreateDefault(BuiltinFunctionId.DateSerial, "DATESERIAL", TypeSymbol.String, [Parameter("year", TypeSymbol.Integer), Parameter("month", TypeSymbol.Integer), Parameter("day", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.TimeSerial, "TIMESERIAL", TypeSymbol.String, [Parameter("hour", TypeSymbol.Integer), Parameter("minute", TypeSymbol.Integer), Parameter("second", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Year, "YEAR", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Month, "MONTH", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MonthName, "MONTHNAME", TypeSymbol.String, [Parameter("month", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Day, "DAY", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Hour, "HOUR", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Minute, "MINUTE", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Second, "SECOND", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Weekday, "WEEKDAY", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.WeekdayName, "WEEKDAYNAME", TypeSymbol.String, [Parameter("weekday", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.DateAdd, "DATEADD", TypeSymbol.String, [Parameter("interval", TypeSymbol.String), Parameter("number", TypeSymbol.Integer), Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DateDiff, "DATEDIFF", TypeSymbol.Long, [Parameter("interval", TypeSymbol.String), Parameter("fromValue", TypeSymbol.String), Parameter("toValue", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DatePart, "DATEPART", TypeSymbol.Integer, [Parameter("part", TypeSymbol.String), Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DateValue, "DATEVALUE", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TimeValue, "TIMEVALUE", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsDate, "ISDATE", TypeSymbol.Boolean, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UtcOffset, "UTCOFFSET", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.UtcOffsetValue, "UTCOFFSET", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UtcOffsetValueZone, "UTCOFFSET", TypeSymbol.Integer, [Parameter("value", TypeSymbol.String), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ToUtc, "TOUTC", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ToUtcZone, "TOUTC", TypeSymbol.String, [Parameter("value", TypeSymbol.String), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FromUtc, "FROMUTC", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FromUtcZone, "FROMUTC", TypeSymbol.String, [Parameter("value", TypeSymbol.String), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsLeapYear, "ISLEAPYEAR", TypeSymbol.Boolean, [Parameter("year", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.DaysInMonth, "DAYSINMONTH", TypeSymbol.Integer, [Parameter("year", TypeSymbol.Integer), Parameter("month", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.StartOfMonth, "STARTOFMONTH", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.EndOfMonth, "ENDOFMONTH", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.StartOfYear, "STARTOFYEAR", TypeSymbol.String, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.EndOfYear, "ENDOFYEAR", TypeSymbol.String, [Parameter("value", TypeSymbol.String)])
        ];
    }
}
