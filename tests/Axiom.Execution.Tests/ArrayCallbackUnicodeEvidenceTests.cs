namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackUnicodeEvidenceTests
{
    [Fact]
    public void CallbacksReceiveWholeVisibleStringsInOrder()
    {
        var source = "DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM mapped[,] AS STRING\r\nDIM filtered[] AS STRING\r\nDIM calls AS INTEGER\r\nDIM token AS STRING = LEFT(\"😀\", 1) + CHRW(0)\r\ngrid[-1, 5] = \"Внешний\"\r\ngrid[0, 6] = token\r\ngrid[0, 7] = \"é\"\r\ngrid[1, 6] = \"😀\"\r\ngrid[1, 7] = \"é\"\r\nPRINT ARRAYEVERY(grid[0 TO 1, 6 TO 7], IsToken)\r\nPRINT calls\r\ncalls = 0\r\nPRINT ARRAYSOME(grid[0 TO 1, 6 TO 7], IsToken)\r\nPRINT calls\r\ncalls = 0\r\nfiltered[] = ARRAYFILTER(grid[0 TO 1, 6 TO 7], IsToken)\r\nPRINT calls\r\nPRINT ARRAYLENGTH(filtered[])\r\nPRINT filtered[1] = token\r\ncalls = 0\r\nmapped[] = ARRAYMAP(grid[0 TO 1, 6 TO 7], Label)\r\nPRINT calls\r\nPRINT LBOUND(mapped[], 1)\r\nPRINT UBOUND(mapped[], 2)\r\nPRINT mapped[0, 6] = \"[\" + token + \"]\"\r\nPRINT mapped[0, 7] = \"[é]\"\r\nPRINT mapped[1, 6] = \"[😀]\"\r\nPRINT mapped[1, 7] = \"[é]\"\r\ncalls = 0\r\nPRINT ARRAYREDUCE(grid[0 TO 1, 6 TO 7], \"Начало:\", AppendValue) = \"Начало:\" + token + \"|é|😀|é|\"\r\nPRINT calls\r\nmapped[0, 6] = \"Замена\"\r\nfiltered[1] = \"Другая замена\"\r\nPRINT grid[0, 6] = token\r\nPRINT grid[-1, 5] = \"Внешний\"\r\nFUNCTION IsToken(value AS STRING) AS BOOLEAN\r\n    calls = calls + 1\r\n    RETURN value = token\r\nEND FUNCTION\r\nFUNCTION Label(value AS STRING) AS STRING\r\n    calls = calls + 1\r\n    RETURN \"[\" + value + \"]\"\r\nEND FUNCTION\r\nFUNCTION AppendValue(acc AS STRING, value AS STRING) AS STRING\r\n    calls = calls + 1\r\n    RETURN acc + value + \"|\"\r\nEND FUNCTION";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "FALSE", "2", "TRUE", "1", "4", "1", "TRUE", "4", "0", "7", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "4", "TRUE", "TRUE" }, run.Output);
    }
}
