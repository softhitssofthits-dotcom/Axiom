namespace Axiom.Execution.Tests;

public sealed class ArrayFindBinaryUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void BinarySearchUsesSortedUtf16ValuesAndExactLongs(ExecutionMode mode)
    {
        Check(mode, """
            DIM words(-3 TO 3) AS STRING
            DIM high AS STRING = LEFT("😀", 1)
            words(-3) = ""
            words(-2) = CHRW(0)
            words(-1) = "é"
            words(0) = "é"
            words(1) = high
            words(2) = "😀"
            words(3) = CHRW(57344)
            PRINT ARRAYBINARYSEARCH(words, "")
            PRINT ARRAYBINARYSEARCH(words, CHRW(0), 1)
            PRINT ARRAYBINARYSEARCH(words, "é")
            PRINT ARRAYBINARYSEARCH(words, "é", 1)
            PRINT ARRAYBINARYSEARCH(words, high)
            PRINT ARRAYBINARYSEARCH(words, "😀", 1)
            PRINT ARRAYBINARYSEARCH(words, CHRW(57344))
            PRINT ARRAYBINARYSEARCH(words, RIGHT("😀", 1))
            DIM numbers(5 TO 8) AS LONG
            numbers(5) = 9007199254740992
            numbers(6) = 9007199254740993
            numbers(7) = 9007199254740993
            numbers(8) = 9223372036854775807
            PRINT ARRAYBINARYSEARCH(numbers, 9007199254740993)
            PRINT ARRAYBINARYSEARCH(numbers, 9007199254740993, 1)
            PRINT ARRAYBINARYSEARCH(numbers, 9223372036854775806)
            PRINT ARRAYBINARYSEARCH(numbers, 9223372036854775807, 1)
            PRINT numbers(6)
            """, ["-3", "-2", "-1", "0", "1", "2", "3", "-4", "6", "6", "4", "8", "9007199254740993"]);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void FindAllReturnsVisibleCoordinatesForWholeStrings(ExecutionMode mode)
    {
        Check(mode, """
            DIM grid(-1 TO 1, 5 TO 7) AS STRING
            DIM hits(,) AS INTEGER
            DIM target AS STRING = LEFT("😀", 1) + CHRW(0)
            grid(-1, 5) = target
            grid(0, 6) = target
            grid(1, 7) = target
            grid(0, 7) = "😀"
            hits = ARRAYFINDALL(grid(0 TO 1, 6 TO 7), target)
            PRINT ARRAYRANK(hits)
            PRINT LBOUND(hits, 1)
            PRINT UBOUND(hits, 1)
            PRINT LBOUND(hits, 2)
            PRINT UBOUND(hits, 2)
            PRINT hits(1, 1)
            PRINT hits(1, 2)
            PRINT hits(2, 1)
            PRINT hits(2, 2)
            hits = ARRAYFINDALL(grid, RIGHT("😀", 1))
            PRINT ARRAYLENGTH(hits)
            PRINT grid(-1, 5) = target
            PRINT grid(0, 7) = "😀"
            DIM words(-2 TO 0) AS STRING
            DIM indices() AS INTEGER
            words(-2) = "é"
            words(-1) = "é"
            words(0) = "é"
            indices = ARRAYFINDALL(words, "é")
            PRINT indices(1)
            PRINT indices(2)
            """, ["2", "1", "2", "1", "2", "0", "6", "1", "7", "0", "TRUE", "TRUE", "-2", "0"]);
    }

    private static void Check(ExecutionMode mode, string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
