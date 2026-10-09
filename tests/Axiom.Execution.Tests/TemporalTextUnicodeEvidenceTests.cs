namespace Axiom.Execution.Tests;

public sealed class TemporalTextUnicodeEvidenceTests
{
    public static IEnumerable<object[]> InvalidTexts()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var text in new[]
        {
            "\"2000-02-29\" + CHRW(0) + \"23:59:59\"",
            "\"2000-02-29 😀\"", "\"２０００-０２-２９\"",
            "\"٢٠٠٠-٠٢-٢٩\"", "\"2000−02−29\"",
            "\"23:59:59\" + LEFT(\"😀\", 1)", "\"23:59:59\" + CHRW(0) + \"x\"",
            "\"2000-02-29\" + CHRW(0)", "\"23:59:59\" + CHRW(0)",
            "\"2000-02-29 23:59:59\" + CHRW(0)",
            "\" 2000-02-29\"", "\"2000-02-29 \"", "\"23:59:59 \"",
            "\"2000-02-29\" + CHRW(160)"
        }) yield return [mode, text];
    }

    [Theory]
    [MemberData(nameof(InvalidTexts))]
    public void InvalidUnicodeTextIsRejectedAndPreservesAssignedValues(ExecutionMode mode, string text)
    {
        var source = "DIM d AS DATE = DATESERIAL(2000, 2, 29)\nDIM t AS TIME = TIMESERIAL(23, 59, 59)\nPRINT ISDATE(" + text + ")\nTRY\nd = DATEVALUE(" + text + ")\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT d\nTRY\nt = TIMEVALUE(" + text + ")\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT t";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "FALSE", "MB3007", "2000-02-29", "MB3007", "23:59:59" }, run.Output);
    }
}
