using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

/// <summary>
/// Wave 4 (2026-10-07): RANDOMIZE, CINT/CLNG/CDBL/CDEC, агрегаты массива,
/// операторы ANDALSO/ORELSE с коротким замыканием.
/// </summary>
public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_RandomizeMakesRndSequenceReproducible()
    {
        const string text = """
RANDOMIZE 42
PRINT RND()
RANDOMIZE 42
PRINT RND()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(2, host.Output.Count);
        Assert.Equal(host.Output[0], host.Output[1]);
    }

    [Fact]
    public void Execute_RandomizeWithoutSeedRuns()
    {
        var (_, _, result) = ExecuteProgram("RANDOMIZE\nPRINT RND() >= 0");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Execute_ConversionFunctionsTruncateTowardZero()
    {
        const string text = """
PRINT CINT(2.9)
PRINT CINT(-2.9)
PRINT CLNG(-2.9)
PRINT CDBL(7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "-2", "-2", "7"], host.Output);
    }

    [Fact]
    public void Execute_ConversionFunctionsReportOverflow()
    {
        const string text = """
DIM wide AS LONG
wide = 2147483648
PRINT CINT(wide)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("CINT", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_CdecRestoresDecimalPrecision()
    {
        const string text = """
DIM a AS DECIMAL
DIM b AS DECIMAL
a = CDEC(0.1)
b = CDEC(0.2)
PRINT a + b
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0.3"], host.Output);
    }

    [Fact]
    public void Execute_ArrayAggregatesSummarizeMinMaxAverage()
    {
        const string text = "DIM scores[1 TO 4] AS INTEGER\r\nscores[1] = 5\r\nscores[2] = 4\r\nscores[3] = 5\r\nscores[4] = 3\r\nPRINT ARRAYSUM(scores[])\r\nPRINT ARRAYMIN(scores[])\r\nPRINT ARRAYMAX(scores[])\r\nPRINT ARRAYAVERAGE(scores[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["17", "3", "5", "4.25"], host.Output);
    }

    [Fact]
    public void Execute_ArrayAggregatesWorkWithStringArrays()
    {
        const string text = "DIM names[1 TO 3] AS STRING\r\nnames[1] = \"банан\"\r\nnames[2] = \"apple\"\r\nnames[3] = \"Яблоко\"\r\nPRINT ARRAYMIN(names[])\r\nPRINT ARRAYMAX(names[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["apple", "банан"], host.Output);
    }

    [Fact]
    public void Execute_ArrayAggregateRejectsEmptyAndStringSum()
    {
        const string empty = "DIM none[1 TO 0] AS INTEGER\r\nPRINT ARRAYSUM(none[])";

        var (_, _, emptyResult) = ExecuteProgram(empty);

        Assert.False(emptyResult.Succeeded);
        Assert.Single(emptyResult.Diagnostics, static d => d.Code == "MB3007");

        const string strings = """
DIM names[1 TO 2] AS STRING
names[1] = "a"
PRINT ARRAYSUM(names[])
""";

        var diagnostics = GetBindingDiagnostics(strings);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ARRAYSUM", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_AndAlsoSkipsRightOperand()
    {
        const string text = "DIM a[1 TO 3] AS INTEGER\r\nDIM i AS INTEGER\r\ni = 5\r\nPRINT i <= 3 ANDALSO a[i] = 0\r\nPRINT \"reached\"";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "reached"], host.Output);
    }

    [Fact]
    public void Execute_OrElseEvaluatesRightOperandOnlyWhenNeeded()
    {
        const string text = "DIM a[1 TO 3] AS INTEGER\r\nDIM i AS INTEGER\r\ni = 5\r\nPRINT i <= 3 ORELSE a[i] = 0";

        var (_, _, result) = ExecuteProgram(text);

        // Левый операнд ложь, поэтому ORELSE обязан вычислить правый,
        // и тот честно сообщает о выходе за границы.
        Assert.False(result.Succeeded);
        Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
    }

    [Fact]
    public void Execute_AndAlsoOrElseTruthTable()
    {
        const string text = """
DIM f AS BOOLEAN
DIM t AS BOOLEAN
f = FALSE
t = TRUE
PRINT f ANDALSO t
PRINT f ORELSE t
PRINT t ANDALSO f
PRINT t ORELSE f
PRINT t ANDALSO t
PRINT f ANDALSO f
PRINT t ORELSE t
PRINT f ORELSE f
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "FALSE", "TRUE", "TRUE", "FALSE", "TRUE", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_AndAlsoRequiresBooleanOperands()
    {
        const string text = """
DIM n AS INTEGER
n = 1
PRINT n = 1 ANDALSO "строка"
""";

        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);

        var diagnostic = Assert.Single(binderDiagnostics, static d => d.Code == "MB2003");
        Assert.Contains("ANDALSO", diagnostic.Message, StringComparison.Ordinal);
    }
}
