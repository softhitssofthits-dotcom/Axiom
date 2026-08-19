using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_ReportsInvalidMultiInputHandling()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
""";

        var (_, _, result) = ExecuteProgram(text, "Ada");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDivisionByZero()
    {
        var (_, _, result) = ExecuteProgram("PRINT 10 / 0");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("PRINT 10 \\ 0")]
    [InlineData("PRINT 10 MOD 0")]
    public void Execute_ReportsDivisionByZeroForIntegralOperators(string source)
    {
        var (_, _, result) = ExecuteProgram(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(10, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_NestedRuntimeErrorReportsCallChain()
    {
        const string text = """
PRINT Outer()

FUNCTION Outer() AS DOUBLE
    RETURN Inner()

    FUNCTION Inner() AS DOUBLE
        RETURN 10 / 0
    END FUNCTION
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Contains("Цепочка вызовов", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("локальная функция 'Inner'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("функция 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("объявлена внутри функции 'Outer'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(7, diagnostic.Location.Line);
        Assert.Equal(19, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerArithmeticOverflow()
    {
        const string text = """
DIM value AS INTEGER = 2147483647
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsIntegerDivisionOverflow()
    {
        const string text = """
DIM value AS INTEGER = -2147483648
PRINT value \ -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongArithmeticOverflow()
    {
        const string text = """
DIM value AS LONG = 9223372036854775807
PRINT value + 1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsLongRemainderOverflow()
    {
        const string text = """
DIM value AS LONG = -9223372036854775807
value = value - 1
PRINT value MOD -1
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("LONG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDoubleArithmeticOverflow()
    {
        const string text = """
DIM value AS DOUBLE = VAL("1e308")
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DOUBLE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsDecimalArithmeticOverflow()
    {
        const string text = """
DIM value AS DECIMAL = 79228162514264337593543950335.0
PRINT value * 10
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3008");
        Assert.Contains("DECIMAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(13, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsArrayBoundsViolation()
    {
        const string text = """
DIM arr(1 TO 2) AS INTEGER
PRINT arr(3)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3002");
        Assert.Contains("arr", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsInvalidInputConversion()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var (_, _, result) = ExecuteProgram(text, "oops");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_ReportsCallStackOverflow()
    {
        const string text = """
Recurse()

SUB Recurse()
    Recurse()
END SUB
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3011");
        Assert.Contains("глубина стека", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurse", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_ReportsCallStackOverflowForMutualRecursion()
    {
        const string text = """
A(0)

FUNCTION A(n AS INTEGER) AS INTEGER
    RETURN B(n + 1)
END FUNCTION

FUNCTION B(n AS INTEGER) AS INTEGER
    RETURN A(n + 1)
END FUNCTION
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3011");
        Assert.Contains("глубина стека", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
