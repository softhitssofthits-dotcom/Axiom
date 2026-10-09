using System.Reflection;
using Axiom.Core;
using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class NumericNonFiniteOperandTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DoubleMathOperandRejectsNonFiniteInputBeforeOperation(double value)
    {
        var type = typeof(RuntimeValue).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinNumericHelpers")!;
        var method = type.GetMethod("ToDoubleMathOperand", BindingFlags.Public | BindingFlags.Static)!;
        var location = new TextLocation(new TextSpan(9, 6), 2, 4);
        var reports = 0;
        Func<string, TextLocation, double> invalidState = (_, _) => throw new InvalidOperationException("Неожиданный тип");
        Action<string, string, string, TextLocation> argumentError = (function, argument, _, actualLocation) =>
        {
            Assert.Equal("SIN", function);
            Assert.Equal("value", argument);
            Assert.Equal(location, actualLocation);
            reports++;
        };
        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null,
            [RuntimeValue.FromDouble(value), location, "SIN", "value", invalidState, argumentError]));
        Assert.Equal("RuntimeFailureException", exception.InnerException!.GetType().Name);
        Assert.Equal(1, reports);
    }
}
