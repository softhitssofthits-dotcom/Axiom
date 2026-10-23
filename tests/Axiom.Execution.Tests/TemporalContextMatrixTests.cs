using Axiom.Execution;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

/// <summary>
/// Независимая матрица контекстов временных значений (DTA-014). Каждый контекст
/// проверяется отдельной программой, чтобы договор нельзя было подтвердить одним
/// удачным путём: CONST, интерполяция, поле TYPE, массив, IF, FOR, WHILE, DO,
/// SELECT CASE, аргумент BYVAL и RETURN.
/// </summary>
public sealed class TemporalContextMatrixTests
{
    private const string Fractional = "2024-02-29 12:34:56.125";

    [Fact]
    public void ConstantKeepsTemporalTypeAndPrecision()
    {
        const string source = """
CONST fixedMoment AS DATETIME = "2024-02-29 12:34:56.125"
PRINT fixedMoment
PRINT LEN(fixedMoment) = 23
""";

        var result = Run(source);

        Assert.Equal(["2024-02-29 12:34:56.125", "TRUE"], result.Output);
    }

    [Fact]
    public void InterpolationUsesCanonicalText()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
PRINT $"момент {moment}"
PRINT $"|{moment}|"
""";

        var result = Run(source);

        Assert.Equal(["момент 2024-02-29 12:34:56.125", "|2024-02-29 12:34:56.125|"], result.Output);
    }

    [Fact]
    public void RecordFieldAndArrayKeepPrecision()
    {
        const string source = "TYPE Entry\n    Moment AS DATETIME\nEND TYPE\n\nDIM item AS Entry\nitem.Moment = \"2024-02-29 12:34:56.125\"\nPRINT item.Moment\n\nDIM list[1 TO 2] AS DATETIME\nlist[1] = item.Moment\nPRINT list[1]\nPRINT list[1] = item.Moment";

        var result = Run(source);

        Assert.Equal(
            ["2024-02-29 12:34:56.125", "2024-02-29 12:34:56.125", "TRUE"],
            result.Output);
    }

    [Fact]
    public void ControlStructuresAcceptTemporalConditions()
    {
        const string source = """
DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
IF moment = "2024-02-29 12:34:56.125" THEN
    PRINT "IF ok"
END IF

FOR i = 1 TO 1
    PRINT YEAR(moment)
NEXT i

DIM flag AS BOOLEAN = TRUE
WHILE flag
    flag = FALSE
    PRINT "WHILE ok"
END WHILE

DO
    PRINT "DO ok"
LOOP WHILE FALSE
""";

        var result = Run(source);

        Assert.Equal(["IF ok", "2024", "WHILE ok", "DO ok"], result.Output);
    }

    [Fact]
    public void ProcedureArgumentsAndReturnKeepPrecision()
    {
        const string source = """
FUNCTION Shift(value AS DATETIME) AS DATETIME
    RETURN DATEADD("h", 1, value)
END FUNCTION

SUB Show(value AS DATETIME)
    PRINT value
END SUB

DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
PRINT Shift(moment)
Show(moment)
""";

        var result = Run(source);

        Assert.Equal(["2024-02-29 13:34:56.125", "2024-02-29 12:34:56.125"], result.Output);
    }

    [Fact]
    public void ByRefKeepsAddressableTypeAndValue()
    {
        const string source = """
SUB Bump(BYREF value AS DATETIME)
    value = DATEADD("s", 1, value)
END SUB

DIM moment AS DATETIME = "2024-02-29 12:34:56.125"
Bump(moment)
PRINT moment
""";

        var result = Run(source);

        Assert.Equal(["2024-02-29 12:34:57.125"], result.Output);
    }

    [Fact]
    public void SelectCaseWorksInsideLoopWithTemporalSelector()
    {
        const string source = "DIM days[1 TO 3] AS DATE\ndays[1] = \"2024-01-15\"\ndays[2] = \"2024-02-29\"\ndays[3] = \"2024-03-01\"\nFOR i = 1 TO 3\n    SELECT CASE days[i]\n        CASE \"2024-02-01\" TO \"2024-02-29\"\n            PRINT \"февраль\"\n        CASE IS >= \"2024-03-01\"\n            PRINT \"позже\"\n        CASE ELSE\n            PRINT \"раньше\"\n    END SELECT\nNEXT i";

        var result = Run(source);

        Assert.Equal(["раньше", "февраль", "позже"], result.Output);
    }

    [Fact]
    public void RoundTripHoldsForEveryTemporalKind()
    {
        const string source = $$"""
DIM moment AS DATETIME = "{{Fractional}}"
DIM momentText AS STRING = moment
DIM restoredMoment AS DATETIME = momentText
PRINT restoredMoment = moment

DIM clockTime AS TIME = "12:34:56.125"
DIM timeText AS STRING = clockTime
DIM restoredTime AS TIME = timeText
PRINT restoredTime = clockTime

DIM dayValue AS DATE = "2024-02-29"
DIM dateText AS STRING = dayValue
DIM restoredDate AS DATE = dateText
PRINT restoredDate = dayValue
""";

        var result = Run(source);

        Assert.Equal(["TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void ValuesWithSameVisiblePartRemainDistinct()
    {
        const string source = "DIM first AS DATETIME = \"2024-02-29 12:34:56.100\"\nDIM second AS DATETIME = \"2024-02-29 12:34:56.200\"\nPRINT first = second\nPRINT first < second\nDIM list[1 TO 2] AS DATETIME\nlist[1] = first\nlist[2] = second\nPRINT ARRAYMIN(list[])\nPRINT ARRAYMAX(list[])";

        var result = Run(source);

        Assert.Equal(
            ["FALSE", "TRUE", "2024-02-29 12:34:56.1", "2024-02-29 12:34:56.2"],
            result.Output);
    }

    private static ExecutionResult Run(string source)
    {
        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, inputProvider: new NoInputProvider()));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        return result;
    }

    private sealed class NoInputProvider : IInputProvider
    {
        public string ReadInput(InputRequest request) => throw new InvalidOperationException("INPUT не ожидался.");
    }
}
