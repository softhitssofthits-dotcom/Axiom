namespace Axiom.Execution.Tests;

public sealed class UnicodeSearchBoundaryEvidenceTests
{
    // Позиции заданы буквально в UTF-16; ожидания не используют поиск CLR.
    private static (string Call, string Expected)[] Data =>
    [
        ("INSTR(2, \"А😀Б😀\", \"😀\")", "2"),
        ("INSTR(3, \"А😀Б😀\", \"😀\")", "5"),
        ("INSTR(6, \"А😀Б😀\", \"😀\")", "0"),
        ("INSTRREV(2, \"А😀Б😀\", \"😀\")", "0"),
        ("INSTRREV(3, \"А😀Б😀\", \"😀\")", "2"),
        ("INSTRREV(5, \"А😀Б😀\", \"😀\")", "2"),
        ("INSTRREV(6, \"А😀Б😀\", \"😀\")", "5"),
        ("INSTR(\"А😀Б😀\", \"\ud83d\")", "2"),
        ("INSTR(3, \"А😀Б😀\", \"\ude00\")", "3"),
        ("INSTRREV(5, \"А😀Б😀\", \"\ud83d\")", "5"),
        ("INSTRREV(5, \"А😀Б😀\", \"\ude00\")", "3"),
        ("INSTR(\"\ud800А\udc00\ud800\", \"\ud800\")", "1"),
        ("INSTRREV(\"\ud800А\udc00\ud800\", \"\ud800\")", "4"),
        ("INSTR(\"\udc00\ud800\", \"\ud800\udc00\")", "0"),
        ("INSTRREV(\"\udc00\ud800\", \"\ud800\udc00\")", "0"),
        ("INSTR(3, \"А😀Б\", \"\")", "3"),
        ("INSTRREV(3, \"А😀Б\", \"\")", "4"),
        ("INSTR(5, \"А😀Б\", \"\")", "5"),
        ("INSTR(6, \"А😀Б\", \"\")", "0"),
        ("INSTRREV(2147483647, \"А😀Б\", \"\")", "5"),
        ("INSTR(\"\ud800\", \"\")", "1"),
        ("INSTRREV(\"\ud800\", \"\")", "2"),
        ("INSTR(\"é\", \"é\")", "0"),
        ("INSTRREV(\"é\", \"é\")", "0")
    ];

    public static IEnumerable<object[]> Cases()
    {

        for (var index = 0; index < Data.Length; index++) yield return [index];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LiteralUtf16SearchBoundariesHaveExpectedPositions(int index)
    {
        var (call, expected) = Data[index];
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(new[] { expected }, result.Output);
    }
}
