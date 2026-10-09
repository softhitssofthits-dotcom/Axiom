using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class DecimalToDoubleBoundaryEvidenceTests
{
    public static IEnumerable<object[]> IntegerCases()
    {
        var cases = new (string Input, string Expected)[]
        {
            ("9007199254740991", "9007199254740991"),
            ("9007199254740992", "9007199254740992"),
            ("9007199254740993", "9007199254740992"),
            ("9007199254740994", "9007199254740994"),
            ("9007199254740995", "9007199254740996"),
            ("9007199254740996", "9007199254740996"),
            ("9007199254740997", "9007199254740996")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var c in cases)
        foreach (var negative in new[] { false, true })
            yield return [mode, negative ? "-" + c.Input : c.Input, negative ? "-" + c.Expected : c.Expected];
    }

    [Theory]
    [MemberData(nameof(IntegerCases))]
    public void ExactDecimalIntegerRoundsToLiteralBinaryNeighbour(ExecutionMode mode, string input, string expected)
    {
        Check(mode, input, "PRINT CLNG(CDBL(value))\nPRINT value", [expected, input]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void MinimumDecimalFractionRemainsNonzeroAndPreservesSign(ExecutionMode mode, bool negative)
    {
        var input = (negative ? "-" : "") + "0.0000000000000000000000000001";
        Check(mode, input, """
            DIM converted AS DOUBLE = CDBL(value)
            PRINT converted <> 0
            PRINT SGN(converted)
            PRINT ABS(converted) < VAL("1e-27")
            PRINT value
            """, ["TRUE", negative ? "-1" : "1", "TRUE", input]);
    }

    private static void Check(ExecutionMode mode, string input, string operations, string[] expected)
    {
        var provider = new DecimalInput(input);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("DIM value AS DECIMAL\nINPUT value\n" + operations, inputProvider: provider, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(expected, result.Output);
    }

    private sealed class DecimalInput(string text) : IInputProvider
    {
        public int Calls { get; private set; }
        public string ReadInput(InputRequest request) { Calls++; return text; }
    }
}
