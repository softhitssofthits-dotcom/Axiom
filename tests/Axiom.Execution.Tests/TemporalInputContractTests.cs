using Axiom.Execution;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

/// <summary>
/// Ввод временных значений через INPUT (DTA-009). Разбор строгий и совпадает с
/// неявным преобразованием строки: каноническая форма, без региональных вариантов
/// и без разделителя T. Неверное поле не меняет ни один получатель.
/// </summary>
public sealed class TemporalInputContractTests
{
    [Fact]
    public void SingleTemporalTargetsAcceptCanonicalText()
    {
        const string source = """
DIM dayValue AS DATE
DIM clockTime AS TIME
DIM moment AS DATETIME
INPUT dayValue
INPUT clockTime
INPUT moment
PRINT dayValue
PRINT clockTime
PRINT moment
PRINT YEAR(dayValue)
""";

        var result = Run(source, "2024-02-29", "12:34:56.125", "2024-02-29 12:34:56.125");

        Assert.Equal(
            ["2024-02-29", "12:34:56.125", "2024-02-29 12:34:56.125", "2024"],
            result.Output);
    }

    [Fact]
    public void MultipleTargetsAreParsedAtomically()
    {
        const string source = """
DIM dayValue AS DATE = "2020-01-01"
DIM clockTime AS TIME = "00:00:00"
DIM count AS INTEGER = 0
TRY
    INPUT dayValue, clockTime, count
CATCH err
    PRINT err.Code
END TRY
PRINT dayValue
PRINT clockTime
PRINT count
""";

        var result = Run(source, "2024-02-29, 12:34:56, 7");
        Assert.Equal(["2024-02-29", "12:34:56", "7"], result.Output);

        var rejected = Run(source, "2024-02-29, bad, 7");
        Assert.Equal(["MB3003", "2020-01-01", "00:00:00", "0"], rejected.Output);
    }

    [Fact]
    public void RegionalAndExchangeFormsAreRejected()
    {
        foreach (var text in new[] { "29.02.2024", "2024-2-29", "2024-02-29T12:34:56", "2024-02-30" })
        {
            var source = $$"""
                DIM dayValue AS DATE = "2020-01-01"
                TRY
                    INPUT dayValue
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT dayValue
                """;

            var result = Run(source, text);

            Assert.Equal(["MB3003", "2020-01-01"], result.Output);
        }
    }

    [Fact]
    public void FractionalSecondsAreKept()
    {
        const string source = """
DIM clockTime AS TIME
INPUT clockTime
PRINT clockTime
PRINT SECOND(clockTime)
""";

        var result = Run(source, "12:34:56.125");

        Assert.Equal(["12:34:56.125", "56"], result.Output);
    }

    [Fact]
    public void EmptyInputIsRejected()
    {
        const string source = """
DIM moment AS DATETIME = "2020-01-01 00:00:00"
TRY
    INPUT moment
CATCH err
    PRINT err.Code
END TRY
PRINT moment
""";

        var result = Run(source, "");

        Assert.Equal(["MB3003", "2020-01-01 00:00:00"], result.Output);
    }

    [Fact]
    public void ArraysAndRecordsRemainUnsupported()
    {
        var programs = new[]
        {
            """
            DIM values(1 TO 2) AS DATE
            INPUT values(1)
            """,
            """
            TYPE Entry
                Day AS DATE
            END TYPE
            DIM item AS Entry
            INPUT item.Day
            """,
        };

        foreach (var source in programs)
        {
            var result = new AxiomExecutionPipeline().Run(
                new ExecutionRequest(source, inputProvider: new QueueInputProvider("2024-02-29")));

            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    private sealed class QueueInputProvider : IInputProvider
    {
        private readonly Queue<string> _values;

        public QueueInputProvider(params string[] values) => _values = new Queue<string>(values);

        public string ReadInput(InputRequest request) => _values.Count > 0 ? _values.Dequeue() : string.Empty;
    }

    private static ExecutionResult Run(string source, params string[] inputs)
    {
        var result = new AxiomExecutionPipeline().Run(
            new ExecutionRequest(source, inputProvider: new QueueInputProvider(inputs)));

        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        return result;
    }
}
