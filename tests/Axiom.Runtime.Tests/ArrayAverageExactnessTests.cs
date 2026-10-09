using System.Numerics;
using System.Reflection;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class ArrayAverageExactnessTests
{
    private static readonly Func<RuntimeArrayValue, TextLocation, Action<string, TextLocation>, Action<string, TextLocation>, RuntimeValue> Average =
        typeof(RuntimeArrayValue).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinArrayAggregateHelpers", true)!
            .GetMethod("Average", BindingFlags.Public | BindingFlags.Static)!
            .CreateDelegate<Func<RuntimeArrayValue, TextLocation, Action<string, TextLocation>, Action<string, TextLocation>, RuntimeValue>>();

    [Fact]
    public void AverageMatchesIndependentRationalRoundingAcrossMixedExponentsAndPermutations()
    {
        var random = new Random(1126);
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var size = attempt % 8 == 0
                ? (attempt / 8 % 4) switch { 0 => 127, 1 => 128, 2 => 129, _ => 1000 }
                : random.Next(1, 70);
            var values = new double[size];
            for (var i = 0; i < values.Length; i++)
            {
                var exponent = attempt % 5 == 0 ? random.Next(0, 3) : random.Next(0, 2047);
                var mantissa = (ulong)random.NextInt64() & 0x000fffffffffffffUL;
                var bits = ((ulong)exponent << 52) | mantissa;
                if (random.Next(2) == 0) bits |= 1UL << 63;
                values[i] = BitConverter.UInt64BitsToDouble(bits);
                if (i > 0 && i % 3 == 1) values[i] = -values[i - 1];
            }
            var expected = ReferenceAverage(values);
            AssertBits(expected, Invoke(values));
            random.Shuffle(values);
            AssertBits(expected, Invoke(values));
        }
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(double.MaxValue)]
    [InlineData(-double.MaxValue)]
    public void SameExponentAccumulationExceedsUInt64WithoutLosingBits(double value)
    {
        var values = Enumerable.Repeat(value, 8193).ToArray();
        AssertBits(value, Invoke(values));
    }

    [Fact]
    public void AveragePreservesSubnormalTieRoundingAndSignedUnderflow()
    {
        foreach (var values in new[]
        {
            new[] { double.Epsilon, 0.0 }, new[] { -double.Epsilon, 0.0 },
            new[] { double.Epsilon, 2 * double.Epsilon },
            new[] { -double.Epsilon, -2 * double.Epsilon },
            new[] { double.MaxValue, -double.MaxValue, double.Epsilon },
            new[] { 0.0, -0.0 }
        }) AssertBits(ReferenceAverage(values), Invoke(values));
    }

    private static double Invoke(double[] values)
    {
        var array = new RuntimeArrayValue(TypeSymbol.Double, null, 1, values.Length);
        for (var i = 0; i < values.Length; i++) array.SetValue(i + 1, RuntimeValue.FromDouble(values[i]));
        Action<string, TextLocation> fail = (message, _) => throw new InvalidOperationException(message);
        return Average(array, new TextLocation(new TextSpan(0, 1), 1, 1), fail, fail).AsDouble();
    }

    // Эталон ищет соседние DOUBLE двоичным поиском и сравнивает точные расстояния.
    // Он не использует группировку показателей или формулу округления реализации.
    private static double ReferenceAverage(double[] values)
    {
        var sum = values.Aggregate(BigInteger.Zero, (acc, value) => acc + Units(BitConverter.DoubleToUInt64Bits(value)));
        var magnitude = BigInteger.Abs(sum);
        ulong lower = 0, upper = 0x7fefffffffffffffUL;
        while (lower < upper)
        {
            var middle = lower + (upper - lower + 1) / 2;
            if (Units(middle) * values.Length <= magnitude) lower = middle;
            else upper = middle - 1;
        }
        var selected = lower;
        if (lower < 0x7fefffffffffffffUL)
        {
            var lowDistance = magnitude - Units(lower) * values.Length;
            var highDistance = Units(lower + 1) * values.Length - magnitude;
            if (highDistance < lowDistance || (highDistance == lowDistance && (lower & 1) != 0)) selected++;
        }
        if (sum.Sign < 0) selected |= 1UL << 63;
        return BitConverter.UInt64BitsToDouble(selected);
    }

    private static BigInteger Units(ulong bits)
    {
        var exponent = (int)((bits >> 52) & 0x7ff);
        var mantissa = bits & 0x000fffffffffffffUL;
        if (exponent != 0) mantissa |= 1UL << 52;
        var units = new BigInteger(mantissa) << Math.Max(0, exponent - 1);
        return (bits >> 63) == 0 ? units : -units;
    }

    private static void AssertBits(double expected, double actual) =>
        Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(actual));
}
