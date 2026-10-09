using Axiom.Core;
using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class IntegerConversionFiniteTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteValue_ReportsLanguageOverflow(double value)
    {
        var location = new TextLocation(new TextSpan(7, 4), 2, 3);
        var codes = new List<string>();
        var exception = Record.Exception(() => RuntimeArithmeticHelpers.ToInt32Checked(
            value, location, "Проверка преобразования", (code, message, actualLocation) =>
            {
                codes.Add(code);
                Assert.Equal(location, actualLocation);
            }));
        Assert.NotNull(exception);
        Assert.Equal("RuntimeFailureException", exception.GetType().Name);
        Assert.Equal(["MB3008"], codes);
    }

    [Theory]
    [InlineData(0d, 0)]
    [InlineData(2.9d, 2)]
    [InlineData(-2.9d, -2)]
    [InlineData(2147483647d, int.MaxValue)]
    [InlineData(-2147483648d, int.MinValue)]
    public void FiniteRepresentableValue_PreservesTruncation(double value, int expected)
    {
        var location = new TextLocation(new TextSpan(0, 1), 1, 1);
        Assert.Equal(expected, RuntimeArithmeticHelpers.ToInt32Checked(
            value, location, "Проверка преобразования", (_, _, _) => Assert.Fail("Неожиданная диагностика")));
    }
}
