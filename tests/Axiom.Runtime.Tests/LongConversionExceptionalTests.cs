using System.Reflection;
using Axiom.Core;
using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class LongConversionExceptionalTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(9223372036854775808d)]
    [InlineData(-9223372036854777856d)]
    public void Clng_NonRepresentableDoubleReportsLanguageOverflow(double value)
    {
        // После исправления VAL неконечные значения недостижимы этим путём
        // языка. Проверяем внутреннее преобразование независимо от VAL.
        var type = typeof(RuntimeValue).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinNumericHelpers")!;
        var method = type.GetMethod("CheckedDoubleToLong", BindingFlags.NonPublic | BindingFlags.Static)!;
        var location = new TextLocation(new TextSpan(0, 1), 1, 1);
        var codes = new List<string>();
        Action<string, string, TextLocation> report = (code, _, actualLocation) =>
        {
            codes.Add(code);
            Assert.Equal(location, actualLocation);
        };
        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [value, "Проверка CLNG", location, report]));
        Assert.Equal("RuntimeFailureException", exception.InnerException!.GetType().Name);
        Assert.Equal(["MB3008"], codes);
    }
}
