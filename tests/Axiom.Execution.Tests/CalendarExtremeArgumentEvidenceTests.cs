namespace Axiom.Execution.Tests;

public sealed class CalendarExtremeArgumentEvidenceTests
{
    public static IEnumerable<object[]> Calls()
    {

        foreach (var extreme in new[] { "2147483647", "(-2147483647 - 1)" })
        foreach (var call in new[]
        {
            $"DATESERIAL({extreme}, 1, 1)", $"DATESERIAL(2000, {extreme}, 1)",
            $"DATESERIAL(2000, 1, {extreme})", $"TIMESERIAL({extreme}, 0, 0)",
            $"TIMESERIAL(0, {extreme}, 0)", $"TIMESERIAL(0, 0, {extreme})",
            $"ISLEAPYEAR({extreme})", $"DAYSINMONTH({extreme}, 2)",
            $"DAYSINMONTH(2000, {extreme})"
        }) yield return [call];
    }

    [Theory]
    [MemberData(nameof(Calls))]
    public void IntegerExtremesAreCatchableWithoutNormalizingCalendar(string call)
    {
        var source = $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT \"Продолжение\"\nEND TRY";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "MB3007", "Продолжение" }, run.Output);
    }
}
