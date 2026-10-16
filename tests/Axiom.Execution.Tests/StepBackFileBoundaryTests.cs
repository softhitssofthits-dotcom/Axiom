using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class StepBackFileBoundaryTests
{
    [Theory]
    [InlineData("WRITEFILE", false)]
    [InlineData("APPENDFILE", false)]
    [InlineData("WRITEFILE", true)]
    [InlineData("APPENDFILE", true)]
    public void StepBackDoesNotCrossOrReplayCompletedFileWrite(string function, bool explicitEncoding)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "stepback-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var file = Path.Combine(directory, "marker.txt");
            File.WriteAllText(file, "Начало");
            var encoding = explicitEncoding ? ", \"UTF-8\"" : "";
            var source = $"DIM count AS INTEGER = 0\nDIM done AS BOOLEAN = {function}(\"marker.txt\", \"X\"{encoding})\ncount = count + 1\nPRINT count\n";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source,
                sourceBreakpointLines: [4], resourceBaseDirectory: directory));
            Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Code + ": " + d.Message)));
            Assert.True(result.Stopped);
            Assert.Empty(result.Diagnostics);
            var expected = function == "APPENDFILE" ? "НачалоX" : "X";
            Assert.Equal(expected, File.ReadAllText(file));
            var stop = result.StopState!;
            var count = 0;
            while (stop.CanStepBack && count++ < 20)
            {
                var backed = stop.StepBack();
                Assert.True(backed.Stopped);
                Assert.True(backed.StoppedAt!.SourceLocation.Line >= 2, "StepBack пересёк уже выполненную запись");
                Assert.Equal(expected, File.ReadAllText(file));
                stop = backed.StopState!;
            }
            Assert.True(count < 20, "История StepBack не достигла границы файловой операции");
            Assert.False(stop.CanStepBack);
            Assert.Equal(2, stop.Snapshot.SourceLocation.Line);
            using (var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var stepped = stop.StepOver();
                Assert.True(stepped.Succeeded);
                Assert.Empty(stepped.Diagnostics);
                Assert.True(stepped.Stopped);
                Assert.Equal(3, stepped.StoppedAt!.SourceLocation.Line);
                var completed = stepped.StopState!.Resume();
                Assert.True(completed.Succeeded);
                Assert.Empty(completed.Diagnostics);
            }
            Assert.Equal(expected, File.ReadAllText(file));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void OpenFileDisablesStepBackAndRejectedBackDoesNotPreventClose()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "stepback-open-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = "DIM f AS FILE\nOPEN \"marker.txt\" FOR APPEND AS f\nDIM done AS BOOLEAN = WRITE(f, \"X\")\nDIM count AS INTEGER = 1\nCLOSE f\nPRINT count\n";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source,
                sourceBreakpointLines: [4], resourceBaseDirectory: directory));
            Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Code + ": " + d.Message)));
            Assert.True(result.Stopped);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.CanStepBack);
            Assert.Throws<InvalidOperationException>(() => result.StopState!.StepBack());
            var completed = result.StopState!.Resume();
            Assert.True(completed.Succeeded);
            Assert.Empty(completed.Diagnostics);
            Assert.Equal("X", File.ReadAllText(Path.Combine(directory, "marker.txt")));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
