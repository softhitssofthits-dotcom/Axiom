using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class DateTimeFormEvidenceTests
{
    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "Date", "DATE()", "2024-03-01", "DATE" };
        yield return new object[] { "DateInZone", "DATE(\"UTC\")", "2024-02-29", "DATE" };
        yield return new object[] { "Time", "TIME()", "00:30:00", "TIME" };
        yield return new object[] { "TimeInZone", "TIME(\"UTC\")", "21:30:00", "TIME" };
        yield return new object[] { "Now", "NOW()", "2024-03-01 00:30:00", "DATETIME" };
        yield return new object[] { "NowInZone", "NOW(\"UTC\")", "2024-02-29 21:30:00", "DATETIME" };
        yield return new object[] { "UtcDate", "UTCDATE()", "2024-02-29", "DATE" };
        yield return new object[] { "UtcTime", "UTCTIME()", "21:30:00", "TIME" };
        yield return new object[] { "UtcNow", "UTCNOW()", "2024-02-29 21:30:00", "DATETIME" };
        yield return new object[] { "Timer", "TIMER()", "1800", "DOUBLE" };
        yield return new object[] { "DateSerial", "DATESERIAL(2024, 2, 29)", "2024-02-29", "DATE" };
        yield return new object[] { "TimeSerial", "TIMESERIAL(23, 59, 59)", "23:59:59", "TIME" };
        yield return new object[] { "Year", "YEAR(DATESERIAL(2024, 2, 29))", "2024", "INTEGER" };
        yield return new object[] { "Month", "MONTH(DATESERIAL(2024, 2, 29))", "2", "INTEGER" };
        yield return new object[] { "MonthName", "MONTHNAME(2)", "February", "STRING" };
        yield return new object[] { "Day", "DAY(DATESERIAL(2024, 2, 29))", "29", "INTEGER" };
        yield return new object[] { "Hour", "HOUR(TIMESERIAL(23, 59, 59))", "23", "INTEGER" };
        yield return new object[] { "Minute", "MINUTE(TIMESERIAL(23, 59, 59))", "59", "INTEGER" };
        yield return new object[] { "Second", "SECOND(TIMESERIAL(23, 59, 59))", "59", "INTEGER" };
        yield return new object[] { "Weekday", "WEEKDAY(DATESERIAL(2024, 2, 29))", "4", "INTEGER" };
        yield return new object[] { "WeekdayName", "WEEKDAYNAME(4)", "Thursday", "STRING" };
        yield return new object[] { "DateAddDateValue", "DATEADD(\"m\", 1, DATESERIAL(2024, 2, 29))", "2024-03-29", "DATE" };
        yield return new object[] { "DateAddTimeValue", "DATEADD(\"s\", 1, TIMESERIAL(23, 59, 59))", "00:00:00", "TIME" };
        yield return new object[] { "DateAddDateTimeValue", "DATEADD(\"s\", 1, moment)", "2024-03-01 00:00:00", "DATETIME" };
        yield return new object[] { "DateDiffDateValues", "DATEDIFF(\"d\", DATESERIAL(2024, 2, 29), DATESERIAL(2024, 3, 1))", "1", "LONG" };
        yield return new object[] { "DateDiffTimeValues", "DATEDIFF(\"s\", TIMESERIAL(23, 59, 59), TIMESERIAL(0, 0, 0))", "-86399", "LONG" };
        yield return new object[] { "DateDiffDateTimeValues", "DATEDIFF(\"s\", moment, later)", "1", "LONG" };
        yield return new object[] { "DatePartDateValue", "DATEPART(\"y\", DATESERIAL(2024, 2, 29))", "60", "INTEGER" };
        yield return new object[] { "DatePartTimeValue", "DATEPART(\"s\", TIMESERIAL(23, 59, 59))", "59", "INTEGER" };
        yield return new object[] { "DatePartDateTimeValue", "DATEPART(\"q\", moment)", "1", "INTEGER" };
        yield return new object[] { "DateValue", "DATEVALUE(\"2024-02-29 23:59:59\")", "2024-02-29", "DATE" };
        yield return new object[] { "TimeValue", "TIMEVALUE(\"2024-02-29 23:59:59\")", "23:59:59", "TIME" };
        yield return new object[] { "IsDate", "ISDATE(\"2024-02-29 23:59:59\")", "TRUE", "BOOLEAN" };
        yield return new object[] { "UtcOffset", "UTCOFFSET()", "180", "INTEGER" };
        yield return new object[] { "UtcOffsetValue", "UTCOFFSET(moment)", "180", "INTEGER" };
        yield return new object[] { "UtcOffsetValueZone", "UTCOFFSET(moment, \"UTC\")", "0", "INTEGER" };
        yield return new object[] { "ToUtc", "TOUTC(moment)", "2024-02-29 20:59:59", "DATETIME" };
        yield return new object[] { "ToUtcZone", "TOUTC(moment, \"UTC\")", "2024-02-29 23:59:59", "DATETIME" };
        yield return new object[] { "FromUtc", "FROMUTC(moment)", "2024-03-01 02:59:59", "DATETIME" };
        yield return new object[] { "FromUtcZone", "FROMUTC(moment, \"UTC\")", "2024-02-29 23:59:59", "DATETIME" };
        yield return new object[] { "IsLeapYear", "ISLEAPYEAR(2024)", "TRUE", "BOOLEAN" };
        yield return new object[] { "DaysInMonth", "DAYSINMONTH(2024, 2)", "29", "INTEGER" };
        yield return new object[] { "StartOfMonth", "STARTOFMONTH(DATESERIAL(2024, 2, 29))", "2024-02-01", "DATE" };
        yield return new object[] { "EndOfMonth", "ENDOFMONTH(DATESERIAL(2024, 2, 29))", "2024-02-29", "DATE" };
        yield return new object[] { "StartOfYear", "STARTOFYEAR(DATESERIAL(2024, 2, 29))", "2024-01-01", "DATE" };
        yield return new object[] { "EndOfYear", "ENDOFYEAR(DATESERIAL(2024, 2, 29))", "2024-12-31", "DATE" };
    }

    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "Date-arity", "DATE(TRUE)" };
        yield return new object[] { "DateInZone-arg1", "DATE(TRUE)" };
        yield return new object[] { "Time-arity", "TIME(TRUE)" };
        yield return new object[] { "TimeInZone-arg1", "TIME(TRUE)" };
        yield return new object[] { "Now-arity", "NOW(TRUE)" };
        yield return new object[] { "NowInZone-arg1", "NOW(TRUE)" };
        yield return new object[] { "UtcDate-arity", "UTCDATE(TRUE)" };
        yield return new object[] { "UtcTime-arity", "UTCTIME(TRUE)" };
        yield return new object[] { "UtcNow-arity", "UTCNOW(TRUE)" };
        yield return new object[] { "Timer-arity", "TIMER(TRUE)" };
        yield return new object[] { "DateSerial-arg1", "DATESERIAL(TRUE, 2, 29)" };
        yield return new object[] { "DateSerial-arg2", "DATESERIAL(2024, TRUE, 29)" };
        yield return new object[] { "DateSerial-arg3", "DATESERIAL(2024, 2, TRUE)" };
        yield return new object[] { "TimeSerial-arg1", "TIMESERIAL(TRUE, 59, 59)" };
        yield return new object[] { "TimeSerial-arg2", "TIMESERIAL(23, TRUE, 59)" };
        yield return new object[] { "TimeSerial-arg3", "TIMESERIAL(23, 59, TRUE)" };
        yield return new object[] { "Year-arg1", "YEAR(TRUE)" };
        yield return new object[] { "Month-arg1", "MONTH(TRUE)" };
        yield return new object[] { "MonthName-arg1", "MONTHNAME(TRUE)" };
        yield return new object[] { "Day-arg1", "DAY(TRUE)" };
        yield return new object[] { "Hour-arg1", "HOUR(TRUE)" };
        yield return new object[] { "Minute-arg1", "MINUTE(TRUE)" };
        yield return new object[] { "Second-arg1", "SECOND(TRUE)" };
        yield return new object[] { "Weekday-arg1", "WEEKDAY(TRUE)" };
        yield return new object[] { "WeekdayName-arg1", "WEEKDAYNAME(TRUE)" };
        yield return new object[] { "DateAddDateValue-arg1", "DATEADD(TRUE, 1, DATESERIAL(2024, 2, 29))" };
        yield return new object[] { "DateAddDateValue-arg2", "DATEADD(\"m\", TRUE, DATESERIAL(2024, 2, 29))" };
        yield return new object[] { "DateAddDateValue-arg3", "DATEADD(\"m\", 1, TRUE)" };
        yield return new object[] { "DateAddTimeValue-arg1", "DATEADD(TRUE, 1, TIMESERIAL(23, 59, 59))" };
        yield return new object[] { "DateAddTimeValue-arg2", "DATEADD(\"s\", TRUE, TIMESERIAL(23, 59, 59))" };
        yield return new object[] { "DateAddTimeValue-arg3", "DATEADD(\"s\", 1, TRUE)" };
        yield return new object[] { "DateAddDateTimeValue-arg1", "DATEADD(TRUE, 1, moment)" };
        yield return new object[] { "DateAddDateTimeValue-arg2", "DATEADD(\"s\", TRUE, moment)" };
        yield return new object[] { "DateAddDateTimeValue-arg3", "DATEADD(\"s\", 1, TRUE)" };
        yield return new object[] { "DateDiffDateValues-arg1", "DATEDIFF(TRUE, DATESERIAL(2024, 2, 29), DATESERIAL(2024, 3, 1))" };
        yield return new object[] { "DateDiffDateValues-arg2", "DATEDIFF(\"d\", TRUE, DATESERIAL(2024, 3, 1))" };
        yield return new object[] { "DateDiffDateValues-arg3", "DATEDIFF(\"d\", DATESERIAL(2024, 2, 29), TRUE)" };
        yield return new object[] { "DateDiffTimeValues-arg1", "DATEDIFF(TRUE, TIMESERIAL(23, 59, 59), TIMESERIAL(0, 0, 0))" };
        yield return new object[] { "DateDiffTimeValues-arg2", "DATEDIFF(\"s\", TRUE, TIMESERIAL(0, 0, 0))" };
        yield return new object[] { "DateDiffTimeValues-arg3", "DATEDIFF(\"s\", TIMESERIAL(23, 59, 59), TRUE)" };
        yield return new object[] { "DateDiffDateTimeValues-arg1", "DATEDIFF(TRUE, moment, later)" };
        yield return new object[] { "DateDiffDateTimeValues-arg2", "DATEDIFF(\"s\", TRUE, later)" };
        yield return new object[] { "DateDiffDateTimeValues-arg3", "DATEDIFF(\"s\", moment, TRUE)" };
        yield return new object[] { "DatePartDateValue-arg1", "DATEPART(TRUE, DATESERIAL(2024, 2, 29))" };
        yield return new object[] { "DatePartDateValue-arg2", "DATEPART(\"y\", TRUE)" };
        yield return new object[] { "DatePartTimeValue-arg1", "DATEPART(TRUE, TIMESERIAL(23, 59, 59))" };
        yield return new object[] { "DatePartTimeValue-arg2", "DATEPART(\"s\", TRUE)" };
        yield return new object[] { "DatePartDateTimeValue-arg1", "DATEPART(TRUE, moment)" };
        yield return new object[] { "DatePartDateTimeValue-arg2", "DATEPART(\"q\", TRUE)" };
        yield return new object[] { "DateValue-arg1", "DATEVALUE(TRUE)" };
        yield return new object[] { "TimeValue-arg1", "TIMEVALUE(TRUE)" };
        yield return new object[] { "IsDate-arg1", "ISDATE(TRUE)" };
        yield return new object[] { "UtcOffset-arity", "UTCOFFSET(TRUE)" };
        yield return new object[] { "UtcOffsetValue-arg1", "UTCOFFSET(TRUE)" };
        yield return new object[] { "UtcOffsetValueZone-arg1", "UTCOFFSET(TRUE, \"UTC\")" };
        yield return new object[] { "UtcOffsetValueZone-arg2", "UTCOFFSET(moment, TRUE)" };
        yield return new object[] { "ToUtc-arg1", "TOUTC(TRUE)" };
        yield return new object[] { "ToUtcZone-arg1", "TOUTC(TRUE, \"UTC\")" };
        yield return new object[] { "ToUtcZone-arg2", "TOUTC(moment, TRUE)" };
        yield return new object[] { "FromUtc-arg1", "FROMUTC(TRUE)" };
        yield return new object[] { "FromUtcZone-arg1", "FROMUTC(TRUE, \"UTC\")" };
        yield return new object[] { "FromUtcZone-arg2", "FROMUTC(moment, TRUE)" };
        yield return new object[] { "IsLeapYear-arg1", "ISLEAPYEAR(TRUE)" };
        yield return new object[] { "DaysInMonth-arg1", "DAYSINMONTH(TRUE, 2)" };
        yield return new object[] { "DaysInMonth-arg2", "DAYSINMONTH(2024, TRUE)" };
        yield return new object[] { "StartOfMonth-arg1", "STARTOFMONTH(TRUE)" };
        yield return new object[] { "EndOfMonth-arg1", "ENDOFMONTH(TRUE)" };
        yield return new object[] { "StartOfYear-arg1", "STARTOFYEAR(TRUE)" };
        yield return new object[] { "EndOfYear-arg1", "ENDOFYEAR(TRUE)" };
    }

    [Theory]
    [MemberData(nameof(WrongArguments))]
    public void EveryArgumentRejectsBooleanWithMatchingDiagnostics(string id, string call)
    {
        var results = new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode }.Select(mode =>
            new AxiomExecutionPipeline().Run(new ExecutionRequest("DIM moment AS DATETIME = \"2024-02-29 23:59:59\"\nDIM later AS DATETIME = \"2024-03-01 00:00:00\"\nPRINT " + call, mode: mode))).ToArray();
        foreach (var result in results)
        {
            Assert.False(result.Succeeded, id);
            Assert.False(result.UsedInterpreterFallback);
            Assert.NotEmpty(result.Diagnostics);
        }
        Assert.Equal(results[0].Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), results[1].Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void EveryRegisteredFormProducesExpectedValueInBothEngines(string id, string call, string expected, string expectedType)
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var source = "DIM moment AS DATETIME = \"2024-02-29 23:59:59\"\nDIM later AS DATETIME = \"2024-03-01 00:00:00\"\nPRINT " + call;
            var syntax = Parser.Parse(source, out var parsingDiagnostics);
            Assert.Empty(parsingDiagnostics);
            var program = Binder.Bind(syntax, out var bindingDiagnostics);
            Assert.Empty(bindingDiagnostics);
            var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last());
            var boundCall = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
            Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(boundCall.Target).Function.BuiltinId?.ToString());
            Assert.Equal(expectedType, boundCall.Type.Name.ToUpperInvariant());
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, runtimeClock: new DateTimeIndependentContractTests.ControlledClock(), mode: mode));
            Assert.True(result.Succeeded, id + " " + mode + " " + string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(new[] { expected }, result.Output);
        }
    }
}
