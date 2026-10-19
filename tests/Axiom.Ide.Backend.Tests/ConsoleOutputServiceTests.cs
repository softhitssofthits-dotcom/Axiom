using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Backend.Tests;

public sealed class ConsoleOutputServiceTests
{
    [Fact]
    public void LeadingEmptyLinesHaveCheckpointsAndSurviveRestore()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("");
        Assert.Equal(1, service.OutputCheckpoint);
        service.AppendTranscriptLine("");
        service.AppendTranscriptLine("X");
        Assert.Equal(3, service.OutputCheckpoint);
        Assert.Equal(Environment.NewLine + Environment.NewLine + "X", service.OutputText);
        service.RestoreOutputCheckpoint(1);
        Assert.Equal(1, service.OutputCheckpoint);
        service.AppendTranscriptLine("Y");
        Assert.Equal(Environment.NewLine + "Y", service.OutputText);
        service.ClearOutput();
        Assert.Equal(0, service.OutputCheckpoint);
    }

    [Fact]
    public void OutputText_InitiallyEmpty()
    {
        var service = new ConsoleOutputService();

        Assert.Equal(string.Empty, service.OutputText);
    }

    [Fact]
    public void AppendTranscriptLine_AddsLine()
    {
        var service = new ConsoleOutputService();

        service.AppendTranscriptLine("hello");

        Assert.Equal("hello", service.OutputText);
    }

    [Fact]
    public void OutputText_ReusesSnapshotUntilHistoryChanges()
    {
        var service = new ConsoleOutputService();

        var first = service.OutputText;
        var second = service.OutputText;
        Assert.Same(first, second);

        service.AppendTranscriptLine("hello");
        var changed = service.OutputText;
        Assert.Equal("hello", changed);
        Assert.NotSame(first, changed);
        Assert.Same(changed, service.OutputText);
    }

    [Fact]
    public void AppendTranscriptLine_AppendsMultipleLines()
    {
        var service = new ConsoleOutputService();

        service.AppendTranscriptLine("line1");
        service.AppendTranscriptLine("line2");

        Assert.Equal($"line1{Environment.NewLine}line2", service.OutputText);
    }

    [Fact]
    public void OutputCheckpoint_ReturnsLineCount()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("line1");
        service.AppendTranscriptLine("line2");

        Assert.Equal(2, service.OutputCheckpoint);
    }

    [Fact]
    public void OutputCheckpoint_ReturnsZeroWhenEmpty()
    {
        var service = new ConsoleOutputService();

        Assert.Equal(0, service.OutputCheckpoint);
    }

    [Fact]
    public void ClearOutput_ClearsText()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("hello");

        service.ClearOutput();

        Assert.Equal(string.Empty, service.OutputText);
    }

    [Fact]
    public void ClearOutput_DoesNothingWhenEmpty()
    {
        var service = new ConsoleOutputService();

        service.ClearOutput();

        Assert.Equal(string.Empty, service.OutputText);
    }

    [Fact]
    public void RestoreOutputCheckpoint_TruncatesToCheckpoint()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("line1");
        service.AppendTranscriptLine("line2");
        service.AppendTranscriptLine("line3");

        service.RestoreOutputCheckpoint(2);

        Assert.Equal($"line1{Environment.NewLine}line2", service.OutputText);
    }

    [Fact]
    public void RestoreOutputCheckpoint_ToZeroClearsAll()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("line1");

        service.RestoreOutputCheckpoint(0);

        Assert.Equal(string.Empty, service.OutputText);
    }

    [Fact]
    public void RestoreOutputCheckpoint_ThrowsOnNegative()
    {
        var service = new ConsoleOutputService();

        Assert.Throws<ArgumentOutOfRangeException>(() => service.RestoreOutputCheckpoint(-1));
    }

    [Fact]
    public void RestoreOutputCheckpoint_ThrowsOnOutOfRange()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("line1");

        Assert.Throws<ArgumentOutOfRangeException>(() => service.RestoreOutputCheckpoint(5));
    }

    [Fact]
    public void Reset_ClearsText()
    {
        var service = new ConsoleOutputService();
        service.AppendTranscriptLine("hello");

        service.Reset();

        Assert.Equal(string.Empty, service.OutputText);
    }
}
