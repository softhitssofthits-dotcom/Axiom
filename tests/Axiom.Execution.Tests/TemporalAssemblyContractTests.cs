using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// Симметричная сборка и разбор DATETIME (DTA-012). Раньше DATETIME собирался
/// только через текст или через DATEADD, а TIME превращался в DATETIME с
/// минимальной датой. Теперь DATETIMESERIAL собирает значение из готовых DATE и
/// TIME, а DATEVALUE/TIMEVALUE принимают DATETIME напрямую, без промежуточного
/// текста и без неявного преобразования.
/// </summary>
public sealed class TemporalAssemblyContractTests
{
    [Fact]
    public void DateTimeSerialCombinesDateAndTime()
    {
        const string source = """
DIM dayValue AS DATE = "2024-02-29"
DIM clockTime AS TIME = "12:34:56"
PRINT DATETIMESERIAL(dayValue, clockTime)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:56"], result.Output);
    }

    [Fact]
    public void DateTimeSerialKeepsFractionalSeconds()
    {
        const string source = """
DIM dayValue AS DATE = DATESERIAL(2024, 2, 29)
DIM clockTime AS TIME = "12:34:56.125"
DIM moment AS DATETIME = DATETIMESERIAL(dayValue, clockTime)
PRINT moment
PRINT TIMEVALUE(moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:56.125", "12:34:56.125"], result.Output);
    }

    [Fact]
    public void TypedExtractionReturnsBothParts()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
DIM dayValue AS DATE = DATEVALUE(moment)
DIM clockTime AS TIME = TIMEVALUE(moment)
PRINT dayValue
PRINT clockTime
PRINT DATETIMESERIAL(dayValue, clockTime) = moment
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29", "12:34:56.125", "TRUE"], result.Output);
    }

    [Fact]
    public void TextFormsStillWork()
    {
        const string source = """
PRINT DATEVALUE("2024-02-29 12:34:56")
PRINT TIMEVALUE("2024-02-29 12:34:56")
PRINT DATEVALUE("2024-02-29")
PRINT TIMEVALUE("12:34:56")
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29", "12:34:56", "2024-02-29", "12:34:56"], result.Output);
    }

    [Fact]
    public void DateTimeSerialRoundTripsWithoutText()
    {
        const string source = """
DIM source AS DATETIME = "2026-04-20 14:05:06"
DIM rebuilt AS DATETIME = DATETIMESERIAL(DATEVALUE(source), TIMEVALUE(source))
PRINT rebuilt
PRINT rebuilt = source
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2026-04-20 14:05:06", "TRUE"], result.Output);
    }

    [Fact]
    public void DateTimeSerialRejectsFullDateTimeArgument()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56"
PRINT DATETIMESERIAL(moment, moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static d => d.Code == "MB2003");
        Assert.Empty(result.Output);
    }

    [Fact]
    public void DateTimeSerialRejectsWrongArityAndType()
    {
        var programs = new[]
        {
            """PRINT DATETIMESERIAL(DATESERIAL(2024, 2, 29))""",
            """
            DIM clockTime AS TIME = "12:34:56"
            PRINT DATETIMESERIAL(clockTime, clockTime)
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Fact]
    public void DateTimeSerialResultParticipatesInArithmetic()
    {
        const string source = """
DIM moment AS DATETIME = DATETIMESERIAL(DATESERIAL(2024, 2, 29), TIMESERIAL(23, 59, 59))
PRINT DATEADD("s", 1, moment)
PRINT DATEDIFF("h", DATETIMESERIAL(DATESERIAL(2024, 2, 29), TIMESERIAL(0, 0, 0)), moment)
PRINT DATEPART("d", moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-03-01 00:00:00", "23", "29"], result.Output);
    }
}
