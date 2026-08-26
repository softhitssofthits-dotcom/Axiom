namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetDateTimeDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.Date, "DATE", TypeSymbol.Date, []),
            CreateDefault(BuiltinFunctionId.DateInZone, "DATE", TypeSymbol.Date, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Time, "TIME", TypeSymbol.Time, []),
            CreateDefault(BuiltinFunctionId.TimeInZone, "TIME", TypeSymbol.Time, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.Now, "NOW", TypeSymbol.DateTime, []),
            CreateDefault(BuiltinFunctionId.NowInZone, "NOW", TypeSymbol.DateTime, [Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UtcDate, "UTCDATE", TypeSymbol.Date, []),
            CreateDefault(BuiltinFunctionId.UtcTime, "UTCTIME", TypeSymbol.Time, []),
            CreateDefault(BuiltinFunctionId.UtcNow, "UTCNOW", TypeSymbol.DateTime, []),
            CreateDefault(BuiltinFunctionId.Timer, "TIMER", TypeSymbol.Double, []),
            CreateDefault(BuiltinFunctionId.DateSerial, "DATESERIAL", TypeSymbol.Date, [Parameter("year", TypeSymbol.Integer), Parameter("month", TypeSymbol.Integer), Parameter("day", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.TimeSerial, "TIMESERIAL", TypeSymbol.Time, [Parameter("hour", TypeSymbol.Integer), Parameter("minute", TypeSymbol.Integer), Parameter("second", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Year, "YEAR", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.Month, "MONTH", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.MonthName, "MONTHNAME", TypeSymbol.String, [Parameter("month", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Day, "DAY", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.Hour, "HOUR", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.Minute, "MINUTE", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.Second, "SECOND", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.Weekday, "WEEKDAY", TypeSymbol.Integer, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.WeekdayName, "WEEKDAYNAME", TypeSymbol.String, [Parameter("weekday", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.DateAddDateValue, "DATEADD", TypeSymbol.Date, [Parameter("interval", TypeSymbol.String), Parameter("number", TypeSymbol.Integer), Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.DateAddTimeValue, "DATEADD", TypeSymbol.Time, [Parameter("interval", TypeSymbol.String), Parameter("number", TypeSymbol.Integer), Parameter("value", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.DateAddDateTimeValue, "DATEADD", TypeSymbol.DateTime, [Parameter("interval", TypeSymbol.String), Parameter("number", TypeSymbol.Integer), Parameter("value", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.DateDiffDateValues, "DATEDIFF", TypeSymbol.Long, [Parameter("interval", TypeSymbol.String), Parameter("fromValue", TypeSymbol.Date), Parameter("toValue", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.DateDiffTimeValues, "DATEDIFF", TypeSymbol.Long, [Parameter("interval", TypeSymbol.String), Parameter("fromValue", TypeSymbol.Time), Parameter("toValue", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.DateDiffDateTimeValues, "DATEDIFF", TypeSymbol.Long, [Parameter("interval", TypeSymbol.String), Parameter("fromValue", TypeSymbol.DateTime), Parameter("toValue", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.DatePartDateValue, "DATEPART", TypeSymbol.Integer, [Parameter("part", TypeSymbol.String), Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.DatePartTimeValue, "DATEPART", TypeSymbol.Integer, [Parameter("part", TypeSymbol.String), Parameter("value", TypeSymbol.Time)]),
            CreateDefault(BuiltinFunctionId.DatePartDateTimeValue, "DATEPART", TypeSymbol.Integer, [Parameter("part", TypeSymbol.String), Parameter("value", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.DateValue, "DATEVALUE", TypeSymbol.Date, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TimeValue, "TIMEVALUE", TypeSymbol.Time, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsDate, "ISDATE", TypeSymbol.Boolean, [Parameter("value", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.UtcOffset, "UTCOFFSET", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.UtcOffsetValue, "UTCOFFSET", TypeSymbol.Integer, [Parameter("value", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.UtcOffsetValueZone, "UTCOFFSET", TypeSymbol.Integer, [Parameter("value", TypeSymbol.DateTime), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ToUtc, "TOUTC", TypeSymbol.DateTime, [Parameter("value", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.ToUtcZone, "TOUTC", TypeSymbol.DateTime, [Parameter("value", TypeSymbol.DateTime), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FromUtc, "FROMUTC", TypeSymbol.DateTime, [Parameter("value", TypeSymbol.DateTime)]),
            CreateDefault(BuiltinFunctionId.FromUtcZone, "FROMUTC", TypeSymbol.DateTime, [Parameter("value", TypeSymbol.DateTime), Parameter("timeZone", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsLeapYear, "ISLEAPYEAR", TypeSymbol.Boolean, [Parameter("year", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.DaysInMonth, "DAYSINMONTH", TypeSymbol.Integer, [Parameter("year", TypeSymbol.Integer), Parameter("month", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.StartOfMonth, "STARTOFMONTH", TypeSymbol.Date, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.EndOfMonth, "ENDOFMONTH", TypeSymbol.Date, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.StartOfYear, "STARTOFYEAR", TypeSymbol.Date, [Parameter("value", TypeSymbol.Date)]),
            CreateDefault(BuiltinFunctionId.EndOfYear, "ENDOFYEAR", TypeSymbol.Date, [Parameter("value", TypeSymbol.Date)])
        ];
    }
}
