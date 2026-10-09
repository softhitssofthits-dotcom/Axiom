using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class PowDotNetContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        var values = new (double Base, double Exponent)[]
        {
            (2, 3), (2, -1074), (2, -1075), (2, -1076),
            (-2, 3), (-2, 4), (9, 0.5), (0, 0),
            (-0.0, 3), (-0.0, 2), (1.0000000000000002, 1000),
            (double.MaxValue, 0.5), (double.Epsilon, 1),
            (-2, 0.5), (2, 1024), (0, -1), (-0.0, -3)
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var pair in values) yield return [mode, pair.Base, pair.Exponent];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void UsesDotNetResultAndPreservesAxiomDiagnostics(ExecutionMode mode, double value, double exponent)
    {
        var expected = Math.Pow(value, exponent);
        var source = $$"""
            TRY
                PRINT POW(VAL("{{value.ToString("R", CultureInfo.InvariantCulture)}}"), VAL("{{exponent.ToString("R", CultureInfo.InvariantCulture)}}"))
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(3, run.Output.Count);
        Assert.Equal("Завершено", run.Output[1]);
        Assert.Equal("Продолжение", run.Output[2]);
        if (double.IsNaN(expected)) Assert.Equal("MB3007", run.Output[0]);
        else if (double.IsInfinity(expected)) Assert.Equal("MB3008", run.Output[0]);
        else
        {
            var actual = double.Parse(run.Output[0], CultureInfo.InvariantCulture);
            Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
        }
    }
}
