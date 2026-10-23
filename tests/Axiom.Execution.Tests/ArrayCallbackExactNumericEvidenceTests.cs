namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackExactNumericEvidenceTests
{
    [Fact]
    public void NumericCallbacksKeepExactValuesAndPreserveResultOnOverflow()
    {
        var source = "DIM longs[5 TO 6] AS LONG\r\nDIM mappedLongs[] AS LONG\r\nDIM amounts[7 TO 8] AS DECIMAL\r\nDIM mappedAmounts[] AS DECIMAL\r\nDIM calls AS INTEGER\r\nlongs[5] = 9007199254740992\r\nlongs[6] = 9007199254740993\r\nmappedLongs[] = ARRAYMAP(longs[], KeepLong)\r\nPRINT mappedLongs[5]\r\nPRINT mappedLongs[6]\r\nPRINT ARRAYREDUCE(longs[], CLNG(0), SumLong)\r\namounts[7] = 79228162514264337593543950335.0\r\namounts[8] = 79228162514264337593543950334.0\r\nmappedAmounts[] = ARRAYMAP(amounts[], KeepDecimal)\r\nPRINT mappedAmounts[7]\r\nPRINT mappedAmounts[8]\r\nPRINT ARRAYREDUCE(amounts[], CDEC(0), Difference)\r\ncalls = 0\r\nTRY\r\n    mappedAmounts[] = ARRAYMAP(amounts[], OverflowDecimal)\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT calls\r\nPRINT mappedAmounts[7]\r\nPRINT mappedAmounts[8]\r\nPRINT amounts[7]\r\nFUNCTION KeepLong(value AS LONG) AS LONG\r\n    RETURN value\r\nEND FUNCTION\r\nFUNCTION SumLong(acc AS LONG, value AS LONG) AS LONG\r\n    RETURN acc + value\r\nEND FUNCTION\r\nFUNCTION KeepDecimal(value AS DECIMAL) AS DECIMAL\r\n    RETURN value\r\nEND FUNCTION\r\nFUNCTION Difference(acc AS DECIMAL, value AS DECIMAL) AS DECIMAL\r\n    RETURN value - acc\r\nEND FUNCTION\r\nFUNCTION OverflowDecimal(value AS DECIMAL) AS DECIMAL\r\n    calls = calls + 1\r\n    RETURN value + CDEC(1)\r\nEND FUNCTION";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "9007199254740992", "9007199254740993", "18014398509481985", "79228162514264337593543950335", "79228162514264337593543950334", "-1", "MB3008", "1", "79228162514264337593543950335", "79228162514264337593543950334", "79228162514264337593543950335" }, run.Output);
    }
}
