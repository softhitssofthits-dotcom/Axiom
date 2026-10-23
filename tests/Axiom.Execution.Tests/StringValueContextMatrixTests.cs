using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class StringValueContextMatrixTests
{
    [Fact]
    public void ArraysRecordsArgumentsReturnsAndInterpolationPreserveStringValues()
    {
        const string source = "TYPE Box\r\n    value AS STRING\r\nEND TYPE\r\nDIM text AS STRING = \"А😀\"\r\ntext = text + \"Б\"\r\nDIM saved AS Box\r\nsaved.value = text\r\nDIM items[-1 TO 0] AS STRING\r\nitems[-1] = text\r\nitems[0] = text\r\nPRINT Combine(text, Change())\r\nPRINT saved.value = items[-1]\r\nPRINT JOIN(items[], \"|\")\r\nPRINT $\"{saved.value}|{text}\"\r\nFUNCTION Change() AS STRING\r\n    text = text + \"В\"\r\n    RETURN \"!\"\r\nEND FUNCTION\r\nFUNCTION Combine(first AS STRING, second AS STRING) AS STRING\r\n    RETURN first + second\r\nEND FUNCTION";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "А😀Б!", "TRUE", "А😀Б|А😀Б", "А😀Б|А😀БВ" }, run.Output);
    }

    [Fact]
    public void StepBackAndReplayDoNotAppendTwice()
    {
        const string source = "DIM text AS STRING = \"A\"\ntext = text + \"B\"\nPRINT text";
        var output = new CapturedOutput();
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, outputWriter: output, sourceBreakpointLines: [3]));
        Assert.True(run.Stopped);
        Assert.True(run.CanStepBack);
        var back = run.StopState!.StepBack();
        Assert.True(back.Stopped);
        var forward = back.StopState!.Resume();
        Assert.True(forward.Stopped);
        var completed = forward.StopState!.Resume();
        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Equal(new[] { "AB" }, output.Lines);
    }

    private sealed class CapturedOutput : IReversibleOutputWriter
    {
        public List<string> Lines { get; } = [];
        public int OutputCheckpoint => Lines.Count;
        public void WriteOutput(string text) => Lines.Add(text);
        public void RestoreOutputCheckpoint(int checkpoint) => Lines.RemoveRange(checkpoint, Lines.Count - checkpoint);
    }
}
