namespace Axiom.Execution.Tests;

public sealed class SystemTimeZoneUnicodeEvidenceTests
{
    public static IEnumerable<object[]> InvalidZones()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var zone in new[] { "\"UTC\" + CHRW(0)", "\"UTC😀\"", "LEFT(\"😀\", 1)", "\"Зона-е́-несуществующая\"" })
        foreach (var function in new[] { "UTCOFFSET", "TOUTC", "FROMUTC" })
            yield return [mode, zone, function];
    }

    [Theory]
    [MemberData(nameof(InvalidZones))]
    public void SystemResolverRejectsWholeInvalidZoneName(ExecutionMode mode, string zone, string function)
    {
        var source = $"DIM moment AS DATETIME = \"2000-02-29 12:00:00\"\nTRY\nPRINT {function}(moment, {zone})\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT moment\nEND TRY";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "MB3007", "2000-02-29 12:00:00" }, run.Output);
    }
}
