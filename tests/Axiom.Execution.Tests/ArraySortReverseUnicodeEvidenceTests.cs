namespace Axiom.Execution.Tests;

public sealed class ArraySortReverseUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void SortAndReverseKeepWholeUtf16ValuesInsideView(ExecutionMode mode, bool explicitAxis)
    {
        var axis = explicitAxis ? ", 1" : "";
        var source = """
            DIM words(-3 TO 3) AS STRING
            DIM result() AS STRING
            DIM high AS STRING = LEFT("😀", 1)
            words(-3) = "Левая граница"
            words(-2) = CHRW(57344)
            words(-1) = "😀"
            words(0) = high
            words(1) = "é"
            words(2) = "é"
            words(3) = "Правая граница"
            """ + $"\nresult = ARRAYTOSORTED(words(-2 TO 2){axis})\n" + """
            PRINT LBOUND(result)
            PRINT UBOUND(result)
            PRINT result(-2) = "é"
            PRINT result(-1) = "é"
            PRINT result(0) = high
            PRINT result(1) = "😀"
            PRINT result(2) = CHRW(57344)
            PRINT words(-2) = CHRW(57344)
            """ + $"\nresult = ARRAYTOREVERSED(words(-2 TO 2){axis})\n" + """
            PRINT result(1) = "é"
            PRINT result(2) = "é"
            PRINT result(3) = high
            PRINT result(4) = "😀"
            PRINT result(5) = CHRW(57344)
            PRINT words(-2) = CHRW(57344)
            """ + $"\nARRAYSORT(words(-2 TO 2){axis})\n" + """
            PRINT words(-2) = "é"
            PRINT words(2) = CHRW(57344)
            """ + $"\nARRAYREVERSE(words(-2 TO 2){axis})\n" + """
            PRINT words(-2) = CHRW(57344)
            PRINT words(-1) = "😀"
            PRINT words(0) = high
            PRINT words(1) = "é"
            PRINT words(2) = "é"
            PRINT words(-3) = "Левая граница"
            PRINT words(3) = "Правая граница"
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "-2", "2" }.Concat(Enumerable.Repeat("TRUE", 21)), run.Output);
    }
}
