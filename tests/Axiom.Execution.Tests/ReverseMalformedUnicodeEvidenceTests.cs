namespace Axiom.Execution.Tests;

public sealed class ReverseMalformedUnicodeEvidenceTests
{
    // Буквальные строки создаются внутри теста: сериализация xUnit не меняет суррогаты.
    private static (string Text, string Expected)[] Data =>
    [
        ("\ud800", "\ud800"),
        ("\udc00", "\udc00"),
        ("\udc00\ud800", "\ud800\udc00"),
        ("\ud800\ud800\udc00", "\ud800\udc00\ud800"),
        ("\udc00\ud800\udc00", "\ud800\udc00\udc00"),
        ("А\ud800😀\udc00Б", "Б\udc00😀\ud800А"),
        ("😀😎\ud800", "\ud800😎😀"),
        ("👨‍👩é", "́e👩‍👨")
    ];

    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        for (var index = 0; index < Data.Length; index++) yield return [mode, index];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ReverseHasLiteralExpectedUtf16Sequence(ExecutionMode mode, int index)
    {
        var (text, expected) = Data[index];
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            DIM original AS STRING = "{{text}}"
            DIM result AS STRING = REVERSE(original)
            PRINT result = "{{expected}}"
            PRINT LEN(result)
            PRINT original = "{{text}}"
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(new[] { "TRUE", expected.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), "TRUE" }, result.Output);
    }
}
