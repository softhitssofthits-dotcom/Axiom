using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class StringValueContextMatrixTests
{
    [Fact]
    public void ArraysRecordsArgumentsReturnsAndInterpolationPreserveStringValues()
    {
        const string source = """
TYPE Box
    value AS STRING
END TYPE
DIM text AS STRING = "А😀"
text = text + "Б"
DIM saved AS Box
saved.value = text
DIM items(-1 TO 0) AS STRING
items(-1) = text
items(0) = text
PRINT Combine(text, Change())
PRINT saved.value = items(-1)
PRINT JOIN(items, "|")
PRINT $"{saved.value}|{text}"
FUNCTION Change() AS STRING
    text = text + "В"
    RETURN "!"
END FUNCTION
FUNCTION Combine(first AS STRING, second AS STRING) AS STRING
    RETURN first + second
END FUNCTION
""";
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
