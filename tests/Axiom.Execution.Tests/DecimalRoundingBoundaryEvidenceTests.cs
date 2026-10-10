using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class DecimalRoundingBoundaryEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Input, string Floor, string Ceil, string Round, string Fix)[]
        {
            ("0.4999999999999999999999999999", "0", "1", "0", "0"),
            ("0.5", "0", "1", "1", "0"),
            ("0.5000000000000000000000000001", "0", "1", "1", "0"),
            ("-0.4999999999999999999999999999", "-1", "0", "0", "0"),
            ("-0.5", "-1", "0", "-1", "0"),
            ("-0.5000000000000000000000000001", "-1", "0", "-1", "0"),
            ("0.0000000000000000000000000001", "0", "1", "0", "0"),
            ("-0.0000000000000000000000000001", "-1", "0", "0", "0"),
            ("9007199254740993.5", "9007199254740993", "9007199254740994", "9007199254740994", "9007199254740993"),
            ("-9007199254740993.5", "-9007199254740994", "-9007199254740993", "-9007199254740994", "-9007199254740993"),
            ("79228162514264337593543950335", "79228162514264337593543950335", "79228162514264337593543950335", "79228162514264337593543950335", "79228162514264337593543950335"),
            ("-79228162514264337593543950335", "-79228162514264337593543950335", "-79228162514264337593543950335", "-79228162514264337593543950335", "-79228162514264337593543950335")
        };

        foreach (var c in cases) yield return [c.Input, c.Floor, c.Ceil, c.Round, c.Fix];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ExactDecimalInputRetainsRoundingBoundary(string input, string floor, string ceil, string round, string fix)
    {
        var provider = new DecimalInput(input);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM value AS DECIMAL
            INPUT value
            PRINT CDEC(value)
            PRINT FLOOR(value)
            PRINT CEIL(value)
            PRINT ROUND(value)
            PRINT FIX(value)
            PRINT SGN(value)
            """, inputProvider: provider));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(new[] { input, floor, ceil, round, fix, input.StartsWith('-') ? "-1" : "1" }, result.Output);
    }

    private sealed class DecimalInput(string text) : IInputProvider
    {
        public int Calls { get; private set; }
        public string ReadInput(InputRequest request) { Calls++; return text; }
    }
}
