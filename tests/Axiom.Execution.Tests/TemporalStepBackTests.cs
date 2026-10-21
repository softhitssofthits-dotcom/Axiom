using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class TemporalStepBackTests
{
    public static IEnumerable<object[]> TemporalValues()
    {
        yield return ["DATE", "2024-02-29", "2024-03-01"];
        yield return ["TIME", "12:34:56.0000001", "12:34:56.0000002"];
        yield return ["DATETIME", "2024-02-29 12:34:56.0000001", "2024-02-29 12:34:56.0000002"];
        yield return ["UTCDATETIME", "2024-02-29T12:34:56.0000001Z", "2024-02-29T12:34:56.0000002Z"];
    }

    [Theory]
    [MemberData(nameof(TemporalValues))]
    public void GlobalTemporalValueIsRestoredBeforeReplay(string type, string before, string after)
    {
        var source = $"DIM value AS {type} = \"{before}\"\nvalue = Change(value)\nPRINT value\nFUNCTION Change(argument AS {type}) AS {type}\nPRINT argument\nRETURN \"{after}\"\nEND FUNCTION";
        var (stopped, output) = Run(source, 3);
        Assert.True(stopped.CanStepBack);
        var backed = stopped.StopState!.StepBack();
        Assert.Equal(2, backed.StoppedAt!.SourceLocation.Line);
        var replay = backed.StopState!.StepOver();
        Assert.Equal(3, replay.StoppedAt!.SourceLocation.Line);
        var completed = replay.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal([before, after], output.Lines);
    }

    [Theory]
    [MemberData(nameof(TemporalValues))]
    public void LocalAndArgumentValuesAreRestoredBeforeReplay(string type, string before, string after)
    {
        var source = $"FUNCTION Work(argument AS {type}) AS {type}\nDIM value AS {type} = argument\nvalue = Change(value)\nPRINT argument\nRETURN value\nEND FUNCTION\nFUNCTION Change(argument AS {type}) AS {type}\nPRINT argument\nRETURN \"{after}\"\nEND FUNCTION\nPRINT Work(\"{before}\")";
        var (stopped, output) = Run(source, 4);
        Assert.True(stopped.CanStepBack);
        var backed = stopped.StopState!.StepBack();
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        var replay = backed.StopState!.StepOver();
        Assert.Equal(4, replay.StoppedAt!.SourceLocation.Line);
        var completed = replay.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal([before, before, after], output.Lines);
    }

    [Theory]
    [MemberData(nameof(TemporalValues))]
    public void RecordFieldIsRestoredBeforeReplay(string type, string before, string after)
    {
        var source = $"TYPE Box\nValue AS {type}\nEND TYPE\nDIM item AS Box\nitem.Value = \"{before}\"\nitem.Value = Change(item.Value)\nPRINT item.Value\nFUNCTION Change(argument AS {type}) AS {type}\nPRINT argument\nRETURN \"{after}\"\nEND FUNCTION";
        var (stopped, output) = Run(source, 7);
        Assert.True(stopped.CanStepBack);
        var backed = stopped.StopState!.StepBack();
        Assert.Equal(6, backed.StoppedAt!.SourceLocation.Line);
        var replay = backed.StopState!.StepOver();
        var completed = replay.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal([before, after], output.Lines);
    }

    [Theory]
    [InlineData("NOWINSTANT()", true)]
    [InlineData("ELAPSEDSECONDS()", false)]
    [InlineData("UTCOFFSET()", true)]
    public void ClockBoundaryCannotBeCrossedOrReadAgain(string expression, bool calendar)
    {
        var clock = new CountingClock();
        var type = expression == "NOWINSTANT()" ? "UTCDATETIME" : "DOUBLE";
        var source = $"DIM count AS INTEGER = 0\nDIM observed AS {type} = {expression}\ncount = count + 1\ncount = count + 1\nPRINT count";
        var (stopped, output) = Run(source, 5, clock);
        var stop = stopped.StopState!;
        var lines = new List<int>();
        for (var guard = 0; stop.CanStepBack && guard < 20; guard++)
        {
            var back = stop.StepBack();
            lines.Add(back.StoppedAt!.SourceLocation.Line);
            Assert.True(back.StoppedAt.SourceLocation.Line >= 2, "История пересекла чтение часов");
            stop = back.StopState!;
        }
        Assert.Contains(4, lines);
        Assert.Contains(3, lines);
        Assert.Equal(2, stop.Snapshot.SourceLocation.Line);
        Assert.False(stop.CanStepBack);
        Assert.Throws<InvalidOperationException>(() => stop.StepBack());
        var resumed = stop.Resume();
        Assert.True(resumed.Stopped);
        Assert.Equal(5, resumed.StoppedAt!.SourceLocation.Line);
        var completed = resumed.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal(["2"], output.Lines);
        Assert.Equal(calendar ? 1 : 0, clock.CalendarReads);
        Assert.Equal(calendar ? 1 : 2, clock.MonotonicReads);
    }

    [Theory]
    [InlineData("FILEDATEUTC")]
    [InlineData("FILECREATEDATEUTC")]
    [InlineData("FILEACCESSEDDATEUTC")]
    [InlineData("DIRDATEUTC")]
    public void MetadataBoundaryDoesNotReplayExternalObservation(string function)
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "stepback-temporal-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var file = Path.Combine(directory, "marker.txt");
            File.WriteAllText(file, "test");
            var path = function == "DIRDATEUTC" ? directory : file;
            var source = $"DIM count AS INTEGER = 0\nDIM observed AS UTCDATETIME = {function}(\"{path}\")\ncount = count + 1\ncount = count + 1\nPRINT observed\nPRINT count";
            var (stopped, output) = Run(source, 5);
            var initial = function switch { "FILEDATEUTC" => File.GetLastWriteTimeUtc(file), "FILECREATEDATEUTC" => File.GetCreationTimeUtc(file), "FILEACCESSEDDATEUTC" => File.GetLastAccessTimeUtc(file), _ => Directory.GetLastWriteTimeUtc(directory) };
            var stop = stopped.StopState!;
            var count = 0;
            while (stop.CanStepBack && count++ < 20)
            {
                var back = stop.StepBack();
                Assert.True(back.StoppedAt!.SourceLocation.Line >= 2, "История пересекла чтение метаданных");
                stop = back.StopState!;
            }
            Assert.True(count >= 2 && count < 20);
            Assert.Equal(2, stop.Snapshot.SourceLocation.Line);
            var changed = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(file, changed);File.SetCreationTimeUtc(file, changed);File.SetLastAccessTimeUtc(file, changed);
            Directory.SetLastWriteTimeUtc(directory, changed);
            var resumed = stop.Resume();
            Assert.True(resumed.Stopped);
            Assert.Equal(5, resumed.StoppedAt!.SourceLocation.Line);
            var completed = resumed.StopState!.Resume();
            Assert.True(completed.Succeeded);
            Assert.Equal([RuntimeValue.FromUtcDateTime(initial).ToDisplayString(), "2"], output.Lines);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("UTCDATETIME", "INSTANTVALUE(\"2024-02-29T12:00:00.0000001Z\")", "2024-02-29T12:00:00.0000001Z")]
    [InlineData("UTCDATETIME", "TOINSTANT(DATETIMEVALUE(\"2024-02-29T12:00:00.0000001\"), \"UTC\")", "2024-02-29T12:00:00.0000001Z")]
    [InlineData("DATETIME", "FROMINSTANT(INSTANTVALUE(\"2024-02-29T12:00:00.0000001Z\"), \"UTC\")", "2024-02-29 12:00:00.0000001")]
    [InlineData("DOUBLE", "ELAPSEDSECONDS(INSTANTVALUE(\"2024-02-29T12:00:00Z\"), INSTANTVALUE(\"2024-02-29T12:00:10Z\"))", "10")]
    [InlineData("INTEGER", "UTCOFFSET(DATETIMEVALUE(\"2024-02-29T12:00:00\"), \"UTC\")", "0")]
    public void PureTemporalOperationsRemainReversible(string type, string expression, string expected)
    {
        var source = $"DIM marker AS INTEGER = 0\nDIM value AS {type} = {expression}\nmarker = marker + 1\nPRINT value\nPRINT marker";
        var (stopped, output) = Run(source, 4);
        var stop = stopped.StopState!;
        var lines = new List<int>();
        while (stop.CanStepBack && lines.Count < 20)
        {
            var back = stop.StepBack();
            lines.Add(back.StoppedAt!.SourceLocation.Line);
            stop = back.StopState!;
        }
        Assert.Contains(1, lines);
        Assert.True(lines.Count < 20);
        var resumed = stop.Resume();
        Assert.True(resumed.Stopped);
        var completed = resumed.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal([expected, "1"], output.Lines);
    }

    [Theory]
    [InlineData("ELAPSEDSECONDS", "DOUBLE", false)]
    [InlineData("NOWINSTANT", "UTCDATETIME", true)]
    public void IndirectClockCallHasSameBoundary(string name, string type, bool calendar)
    {
        var clock = new CountingClock();
        var source = $"DIM reader AS FUNCTION() AS {type}\nreader = {name}\nDIM count AS INTEGER = 0\nDIM observed AS {type} = reader()\ncount = count + 1\ncount = count + 1\nPRINT count";
        var (stopped, output) = Run(source, 7, clock);
        var stop = stopped.StopState!;
        var steps = 0;
        while (stop.CanStepBack && steps++ < 20)
        {
            var back = stop.StepBack();
            Assert.True(back.StoppedAt!.SourceLocation.Line >= 4, "Косвенный вызов пересёк чтение часов");
            stop = back.StopState!;
        }
        Assert.True(steps >= 2 && steps < 20);
        Assert.Equal(4, stop.Snapshot.SourceLocation.Line);
        var resumed = stop.Resume();
        var completed = resumed.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal(["2"], output.Lines);
        Assert.Equal(calendar ? 1 : 0, clock.CalendarReads);
        Assert.Equal(calendar ? 1 : 2, clock.MonotonicReads);
    }

    [Fact]
    public void IndirectPureConversionRemainsReversible()
    {
        const string source = """
DIM parser AS FUNCTION(STRING) AS UTCDATETIME
parser = INSTANTVALUE
DIM count AS INTEGER = 0
DIM value AS UTCDATETIME = parser("2024-02-29T12:00:00.0000001Z")
count = count + 1
PRINT value
""";
        var (stopped, output) = Run(source, 6);
        var stop = stopped.StopState!;
        var lines = new List<int>();
        while (stop.CanStepBack && lines.Count < 20)
        {
            var back = stop.StepBack();lines.Add(back.StoppedAt!.SourceLocation.Line);stop = back.StopState!;
        }
        Assert.Contains(2, lines);
        var resumed = stop.Resume();
        var completed = resumed.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.Equal(["2024-02-29T12:00:00.0000001Z"], output.Lines);
    }

    private sealed class BufferWriter : IReversibleOutputWriter
    {
        public List<string> Lines { get; } = [];
        public int OutputCheckpoint => Lines.Count;
        public void WriteOutput(string text) => Lines.Add(text);
        public void RestoreOutputCheckpoint(int checkpoint) => Lines.RemoveRange(checkpoint, Lines.Count - checkpoint);
    }

    private sealed class CountingClock : IRuntimeClock
    {
        public int CalendarReads { get; private set; }
        public int MonotonicReads { get; private set; }
        public DateTimeOffset GetLocalNow() { CalendarReads++; return new(2024, 2, 29, 12, 0, 0, TimeSpan.Zero); }
        public double GetMonotonicSeconds() => 1000 + 10 * MonotonicReads++;
        public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Utc;
        public bool TryResolveTimeZone(string id, out TimeZoneInfo zone) { zone = TimeZoneInfo.Utc; return true; }
    }

    private static (ExecutionResult, BufferWriter) Run(string source, int line, IRuntimeClock? clock = null)
    {
        var output = new BufferWriter();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, sourceBreakpointLines: [line], runtimeClock: clock, outputWriter: output));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.True(result.Stopped);
        Assert.Equal(line, result.StoppedAt!.SourceLocation.Line);
        return (result, output);
    }
}
