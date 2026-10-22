using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class BytecodeVmStringFailurePathTests
{
    [Theory]
    [InlineData(false, "SPACE(16777217)")]
    [InlineData(true, "SPACE(16777217)")]
    [InlineData(false, "REPEAT(\"ab\", 8388609)")]
    [InlineData(true, "REPEAT(\"ab\", 8388609)")]
    [InlineData(false, "REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"x\", 2000))")]
    [InlineData(true, "REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"x\", 2000))")]
    public void UnhandledLimitHasExactLocationAndStopsExecution(bool observed, string expression)
    {
        var source = $"PRINT \"before\"\nPRINT {expression}\nPRINT \"unreachable\"";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.False(result.Succeeded);
        Assert.False(result.Stopped);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
        Assert.Equal(["before"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
        if (observed)
        {
            Assert.Equal(2, snapshots[^1].SourceLocation.Line);
            Assert.Equal(7, snapshots[^1].SourceLocation.Column);
        }
    }

    [Theory]
    [InlineData(false, "SPACE(16777217)")]
    [InlineData(true, "SPACE(16777217)")]
    [InlineData(false, "REPEAT(\"ab\", 8388609)")]
    [InlineData(true, "REPEAT(\"ab\", 8388609)")]
    [InlineData(false, "REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"x\", 2000))")]
    [InlineData(true, "REGEXREPLACE(REPEAT(\"a\", 10000), \"a\", REPEAT(\"x\", 2000))")]
    public void CatchAndFinallyPreserveDestinationAndContinue(bool observed, string expression)
    {
        var source = $$"""
DIM text AS STRING = "kept"
TRY
    text = {{expression}}
    PRINT "unreachable"
CATCH failure
    PRINT failure.Code
    PRINT failure.Line
    PRINT failure.Column
FINALLY
    PRINT "finally"
END TRY
PRINT text
PRINT "after"
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.True(result.Succeeded);
        Assert.False(result.Stopped);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3007", "3", "12", "finally", "kept", "after"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
        if (observed)
        {
            Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 3 && snapshot.SourceLocation.Column == 12);
            Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 13);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailureUnwindsRoutineFinallyBeforeCallerCatch(bool observed)
    {
        const string source = """
TRY
    Fail()
    PRINT "unreachable"
CATCH failure
    PRINT failure.Code
    PRINT failure.Line
    PRINT failure.Column
FINALLY
    PRINT "outer finally"
END TRY
PRINT "after"

SUB Fail()
    TRY
        PRINT SPACE(16777217)
    FINALLY
        PRINT "inner finally"
    END TRY
END SUB
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["inner finally", "MB3007", "15", "15", "outer finally", "after"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
        if (observed)
        {
            Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 15 && snapshot.CallDepth == 2);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnhandledHostOutOfMemoryHasExactLocation(bool observed)
    {
        const string source = "PRINT \"before\"\nPRINT NOW()\nPRINT \"unreachable\"";

        var (host, result, snapshots) = Execute(source, observed, throwFromClock: true);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
        Assert.Equal(["before"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HostOutOfMemoryRunsCatchAndFinallyAndContinues(bool observed)
    {
        const string source = """
TRY
    PRINT NOW()
    PRINT "unreachable"
CATCH failure
    PRINT failure.Code
    PRINT failure.Line
    PRINT failure.Column
FINALLY
    PRINT "finally"
END TRY
PRINT "after"
""";

        var (host, result, snapshots) = Execute(source, observed, throwFromClock: true);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3007", "2", "11", "finally", "after"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    private static (BufferedHost Host, RuntimeResult Result, List<BytecodeVmExecutionSnapshot> Snapshots) Execute(
        string source,
        bool observed,
        bool throwFromClock = false)
    {
        var compilation = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.True(compilation.Succeeded, string.Join(" | ", compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var host = new BufferedHost(throwFromClock);
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        var result = BytecodeVm.Execute(compilation.CompiledProgram!.BytecodeModule!, host, observed ? snapshots.Add : null);
        return (host, result, snapshots);
    }

    private sealed class BufferedHost(bool throwFromClock) : IRuntimeHost
    {
        public List<string> Output { get; } = [];
        public void WriteOutput(string text) => Output.Add(text);
        public string ReadInput(InputRequest request) => string.Empty;
        public string ReadKey() => string.Empty;
        public bool IsKeyDown(string keyText) => false;
        public long GetMouseX() => -1;
        public long GetMouseY() => -1;
        public bool IsMouseButtonDown(int buttonNumber) => false;
        public int ReadMouseButtonPress() => 0;
        public bool IsCancellationRequested => false;
        public DateTimeOffset GetLocalNow() => throwFromClock ? throw new OutOfMemoryException() : DateTimeOffset.UnixEpoch;
        public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Utc;
        public bool TryResolveTimeZone(string id, out TimeZoneInfo zone) { zone = TimeZoneInfo.Utc; return true; }
        public double NextRandomDouble() => 0;
        public void Reseed(int seed) { }
    }
}
