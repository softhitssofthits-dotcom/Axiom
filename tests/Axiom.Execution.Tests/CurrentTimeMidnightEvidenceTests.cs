using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class CurrentTimeMidnightEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void FixedMidnightSourceKeepsUtcLocalAndFractionalTimerDistinct(ExecutionMode mode)
    {
        var source = """
            PRINT DATE()
            PRINT TIME()
            PRINT NOW()
            PRINT UTCDATE()
            PRINT UTCTIME()
            PRINT UTCNOW()
            PRINT DATE("UTC")
            PRINT TIME("UTC")
            PRINT NOW("UTC")
            PRINT TIMER()
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, runtimeClock: new MidnightClock(), mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "2000-03-01", "00:00:00", "2000-03-01 00:00:00", "2000-02-29", "21:00:00", "2000-02-29 21:00:00", "2000-02-29", "21:00:00", "2000-02-29 21:00:00", "0.125" }, run.Output);
    }

    private sealed class MidnightClock : IRuntimeClock
    {
        public DateTimeOffset GetLocalNow() => new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(3)).AddMilliseconds(125);
        public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.CreateCustomTimeZone("AxiomFixed", TimeSpan.FromHours(3), "AxiomFixed", "AxiomFixed");
        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            timeZone = id.Equals("UTC", StringComparison.OrdinalIgnoreCase) ? TimeZoneInfo.Utc : GetLocalTimeZone();
            return id.Equals("UTC", StringComparison.OrdinalIgnoreCase) || id.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }
    }
}
