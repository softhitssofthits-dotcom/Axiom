using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class TemporalStackSnapshotTests
{
    [Theory]
    [InlineData("DATE", "2024-02-29")]
    [InlineData("TIME", "12:34:56.0000001")]
    [InlineData("DATETIME", "2024-02-29 12:34:56.0000001")]
    [InlineData("UTCDATETIME", "2024-02-29T12:34:56.0000001Z")]
    public void PendingTemporalArgumentOnCallerStackSurvivesBackAndReplay(string type, string text)
    {
        var source = $"FUNCTION Identity(n AS INTEGER) AS INTEGER\nDIM result AS INTEGER = n\nRETURN result\nEND FUNCTION\nFUNCTION Echo(value AS {type}, ignored AS INTEGER) AS {type}\nRETURN value\nEND FUNCTION\nPRINT 0\nPRINT Echo(\"{text}\", Identity(1))";
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        var bound = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.DoesNotContain(binderDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var lowered = new BoundProgramLowerer().Lower(bound);
        Assert.True(lowered.Succeeded);
        var compiled = new BytecodeCompiler().Compile(lowered.Program!);
        Assert.True(compiled.Succeeded);
        var host = new BufferedHost();
        var stopped = BytecodeVm.Execute(compiled.Module!, host, breakpoints: BytecodeBreakpointSet.FromLines(3));
        Assert.True(stopped.Stopped);
        Assert.True(stopped.StopState!.CanStepBack);
        var previous = Assert.IsType<BytecodeVmStateSnapshot>(stopped.StopState.PreviousStateSnapshot);
        Assert.Contains(previous.CallFramesBottomToTop.SelectMany(f => f.EvaluationStackBottomToTop), value => value.ToDisplayString() == text && value.Type.Name == type);
        var backed = stopped.StopState.StepBack();
        Assert.Equal(2, backed.StoppedAt!.SourceLocation.Line);
        var replay = backed.StopState!.StepOver();
        Assert.Equal(3, replay.StoppedAt!.SourceLocation.Line);
        var completed = replay.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal(["0", text], host.Output);
    }

    private sealed class BufferedHost : IRuntimeHost, IReversibleOutputWriter
    {
        public List<string> Output { get; } = [];
        public int OutputCheckpoint => Output.Count;
        public void WriteOutput(string text) => Output.Add(text);
        public void RestoreOutputCheckpoint(int checkpoint) => Output.RemoveRange(checkpoint, Output.Count - checkpoint);
        public string ReadInput(InputRequest request) => "";
        public string ReadKey() => "";
        public bool IsKeyDown(string key) => false;
        public long GetMouseX() => -1;
        public long GetMouseY() => -1;
        public bool IsMouseButtonDown(int button) => false;
        public int ReadMouseButtonPress() => 0;
        public bool IsCancellationRequested => false;
        public DateTimeOffset GetLocalNow() => DateTimeOffset.UnixEpoch;
        public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Utc;
        public bool TryResolveTimeZone(string id, out TimeZoneInfo zone) { zone = TimeZoneInfo.Utc; return true; }
        public double NextRandomDouble() => 0;
        public void Reseed(int seed) { }
    }
}
