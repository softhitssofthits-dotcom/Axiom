using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// Агрегаты и поиск по массивам временных элементов (DTA-010). Порядковое сравнение
/// DATE, TIME и DATETIME уже поддержано обычными операторами, но ARRAYMIN/ARRAYMAX
/// отвергали такие элементы. Теперь агрегаты используют тот же порядок без
/// перевода в строки, а SORT, поиск и DISTINCT сохраняют точность значений.
/// </summary>
public sealed class TemporalArrayAggregateContractTests
{
    [Fact]
    public void MinAndMaxOrderDatesChronologically()
    {
        const string source = """
DIM days(1 TO 4) AS DATE
days(1) = "2024-02-29"
days(2) = "2024-01-15"
days(3) = "2024-03-01"
days(4) = "2023-12-31"
PRINT ARRAYMIN(days)
PRINT ARRAYMAX(days)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2023-12-31", "2024-03-01"], result.Output);
    }

    [Fact]
    public void MinAndMaxOrderTimesWithinDay()
    {
        const string source = """
DIM times(1 TO 3) AS TIME
times(1) = "23:59:59"
times(2) = "00:00:01"
times(3) = "12:00:00"
PRINT ARRAYMIN(times)
PRINT ARRAYMAX(times)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["00:00:01", "23:59:59"], result.Output);
    }

    [Fact]
    public void MinAndMaxCompareFractionalSeconds()
    {
        const string source = """
DIM moments(1 TO 3) AS DATETIME
moments(1) = "2024-02-29 12:34:56.125"
moments(2) = "2024-02-29 12:34:56.100"
moments(3) = "2024-02-29 12:34:56.900"
PRINT ARRAYMIN(moments)
PRINT ARRAYMAX(moments)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 12:34:56.1", "2024-02-29 12:34:56.9"], result.Output);
    }

    [Fact]
    public void SingleElementArrayReturnsThatElement()
    {
        const string source = """
DIM days(1 TO 1) AS DATE
days(1) = "2024-02-29"
PRINT ARRAYMIN(days)
PRINT ARRAYMAX(days)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29", "2024-02-29"], result.Output);
    }

    [Fact]
    public void SortContainsAndDistinctKeepTemporalValues()
    {
        const string source = """
DIM days(1 TO 3) AS DATE
days(1) = "2024-02-29"
days(2) = "2024-01-15"
days(3) = "2024-02-29"
DIM ordered() AS DATE
ordered = ARRAYTOSORTED(days)
PRINT ordered(1)
PRINT ordered(3)
PRINT ARRAYCONTAINS(days, "2024-01-15")
PRINT ARRAYCONTAINS(days, "2024-01-16")
DIM unique() AS DATE
unique = ARRAYDISTINCT(days)
PRINT ARRAYLENGTH(unique)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-01-15", "2024-02-29", "TRUE", "FALSE", "2"], result.Output);
    }

    [Fact]
    public void NumericAndStringAggregatesKeepPreviousRules()
    {
        const string source = """
DIM numbers(1 TO 3) AS INTEGER
numbers(1) = 5
numbers(2) = -2
numbers(3) = 9
PRINT ARRAYMIN(numbers)
PRINT ARRAYMAX(numbers)

DIM words(1 TO 2) AS STRING
words(1) = "яблоко"
words(2) = "банан"
PRINT ARRAYMIN(words)
PRINT ARRAYMAX(words)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["-2", "9", "банан", "яблоко"], result.Output);
    }

    [Fact]
    public void UnsupportedElementTypeIsStillRejected()
    {
        const string source = """
DIM flags(1 TO 2) AS BOOLEAN
flags(1) = TRUE
flags(2) = FALSE
PRINT ARRAYMIN(flags)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2003", diagnostic.Code);
        Assert.Contains("BOOLEAN", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyArrayReportsArgumentError()
    {
        const string source = """
DIM days(1 TO 0) AS DATE
PRINT ARRAYMIN(days)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }
}
