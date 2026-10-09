using System.Globalization;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class RandomIntegerAlternativeEvidenceTests
{
    [Theory]
    [InlineData(0.0, "1")]
    [InlineData(0.5, "6")]
    [InlineData(0.9999999999999999, "10")]
    public void SmallIndexFormulaHandlesValidSourceEndpoints(double value, string expected)
    {
        foreach (var mode in Modes)
        {
            var run = Run("PRINT INT(RND()*10)+1", mode, new FixedRandom(value));
            Assert.True(run.Succeeded);
            Assert.Equal(expected, Assert.Single(run.Output));
        }
    }

    [Theory]
    [InlineData("DIM low AS INTEGER = -2147483648\nDIM high AS INTEGER = 2147483647\nPRINT INT(RND()*(high-low+1))+low")]
    [InlineData("DIM low AS LONG = -9223372036854775807 - 1\nDIM high AS LONG = 9223372036854775807\nPRINT INT(RND()*(high-low+1))+low")]
    [InlineData("DIM width AS LONG = 4294967296\nPRINT INT(RND()*width)")]
    public void CheckedWidthAndIntegerReturnAreRealLimits(string source)
    {
        foreach (var mode in Modes)
        {
            var run = Run(source, mode, new FixedRandom(0.5));
            Assert.False(run.Succeeded);
            Assert.Contains(run.Diagnostics, d => d.Code == "MB3008");
        }
    }

    [Theory]
    [InlineData(0.0, "-2147483648")]
    [InlineData(0.5, "0")]
    [InlineData(0.9999999999999999, "2147483647")]
    public void ExistingFloorAndLongConversionCoverFullIntegerDomain(double value, string expected)
    {
        const string source = """
DIM low AS LONG = -2147483648
DIM high AS LONG = 2147483647
DIM width AS LONG = high-low+1
PRINT CLNG(FLOOR(RND()*width))+low
""";
        foreach (var mode in Modes)
        {
            var run = Run(source, mode, new FixedRandom(value));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, Assert.Single(run.Output));
        }
    }

    [Fact]
    public void ConversionBeforeLargeOffsetPreservesAdjacentLongValues()
    {
        const string source = """
DIM low AS LONG = 9007199254740992
DIM width AS LONG = 2
PRINT CLNG(FLOOR(RND()*width)+low)
PRINT CLNG(FLOOR(RND()*width))+low
""";
        foreach (var mode in Modes)
        {
            var run = Run(source, mode, new FixedRandom(0.5, 0.5));
            Assert.True(run.Succeeded);
            Assert.Equal(new[] { "9007199254740992", "9007199254740993" }, run.Output);
        }
    }

    [Fact]
    public void EqualWeightDiscreteSourceDoesNotMakeThreeBucketsEqual()
    {
        const string source = "FOR i = 1 TO 8\nPRINT INT(RND()*3)\nNEXT i";
        foreach (var mode in Modes)
        {
            var run = Run(source, mode, new FixedRandom(Enumerable.Range(0, 8).Select(i => i / 8.0).ToArray()));
            Assert.True(run.Succeeded);
            Assert.Equal(new[] { 3, 3, 2 }, Enumerable.Range(0, 3).Select(i => run.Output.Count(x => x == i.ToString())).ToArray());
        }
    }

    [Fact]
    public void UserFunctionCanRejectUnevenTailWhenSourceGridIsKnown()
    {
        const string source = """
FUNCTION RandomThree() AS INTEGER
    DIM digit AS INTEGER
    DO
        digit = INT(RND()*8)
    LOOP WHILE digit >= 6
    RETURN (digit MOD 3)+1
END FUNCTION
PRINT RandomThree()
""";
        foreach (var mode in Modes)
        {
            var random = new FixedRandom(7.0/8, 6.0/8, 5.0/8);
            var run = Run(source, mode, random);
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal("3", Assert.Single(run.Output));
            Assert.Equal(3, random.Calls);
        }
    }

    [Fact]
    public void ReseedingRepeatsSmallIndexSequenceAcrossEngines()
    {
        const string source = """
RANDOMIZE 42
FOR i = 1 TO 10
    PRINT INT(RND()*10)+1
NEXT i
RANDOMIZE 42
FOR i = 1 TO 10
    PRINT INT(RND()*10)+1
NEXT i
""";
        var reference = new Random(42);
        var expected = Enumerable.Range(0, 10).Select(_ => ((int)Math.Floor(reference.NextDouble()*10)+1).ToString(CultureInfo.InvariantCulture)).ToArray();
        foreach (var mode in Modes)
        {
            var run = Run(source, mode, new SeededRandom());
            Assert.True(run.Succeeded);
            Assert.Equal(expected.Concat(expected), run.Output);
        }
    }

    private static ExecutionMode[] Modes => [ExecutionMode.Interpreter, ExecutionMode.Bytecode];
    private static ExecutionResult Run(string source, ExecutionMode mode, IRuntimeRandom random)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, runtimeRandom: random, mode: mode));
        Assert.False(run.UsedInterpreterFallback);
        return run;
    }

    private sealed class FixedRandom(params double[] values) : IRuntimeRandom
    {
        private readonly Queue<double> _values = new(values);
        public int Calls { get; private set; }
        public double NextRandomDouble() { Calls++; return _values.Dequeue(); }
        public void Reseed(int seed) { }
    }
    private sealed class SeededRandom : IRuntimeRandom
    {
        private Random _random = new(0);
        public double NextRandomDouble() => _random.NextDouble();
        public void Reseed(int seed) => _random = new Random(seed);
    }
}
