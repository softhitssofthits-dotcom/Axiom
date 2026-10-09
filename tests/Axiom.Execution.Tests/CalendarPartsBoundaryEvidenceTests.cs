namespace Axiom.Execution.Tests;

public sealed class CalendarPartsBoundaryEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExtremeDatesAndCanonicalNamesHaveLiteralResults(ExecutionMode mode)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            var source = """
                PRINT YEAR(DATESERIAL(1, 1, 1))
                PRINT YEAR(DATESERIAL(9999, 12, 31))
                PRINT MONTH(DATESERIAL(9999, 12, 31))
                PRINT DAY(DATESERIAL(9999, 12, 31))
                PRINT WEEKDAY(DATESERIAL(1, 1, 1))
                PRINT WEEKDAY(DATESERIAL(9999, 12, 31))
                PRINT HOUR(TIMESERIAL(0, 0, 0))
                PRINT MINUTE(TIMESERIAL(23, 59, 59))
                PRINT SECOND(TIMESERIAL(23, 59, 59))
                PRINT MONTHNAME(1)
                PRINT MONTHNAME(12)
                PRINT WEEKDAYNAME(1)
                PRINT WEEKDAYNAME(7)
                PRINT STARTOFMONTH(DATESERIAL(1, 1, 31))
                PRINT ENDOFMONTH(DATESERIAL(9999, 12, 1))
                PRINT STARTOFYEAR(DATESERIAL(1, 12, 31))
                PRINT ENDOFYEAR(DATESERIAL(9999, 1, 1))
                TRY
                    PRINT MONTHNAME(2147483647)
                CATCH err
                    PRINT err.Code
                END TRY
                TRY
                    PRINT WEEKDAYNAME((-2147483647 - 1))
                CATCH err
                    PRINT err.Code
                END TRY
                """;
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Empty(run.Diagnostics);
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
            Assert.Equal(new[] { "1", "9999", "12", "31", "1", "5", "0", "59", "59", "January", "December", "Monday", "Sunday", "0001-01-01", "9999-12-31", "0001-01-01", "9999-12-31", "MB3007", "MB3007" }, run.Output);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }
}
