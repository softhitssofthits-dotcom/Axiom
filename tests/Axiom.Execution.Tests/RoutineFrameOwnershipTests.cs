using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class RoutineFrameOwnershipTests
{
    [Fact]
    public void CompletedFrameDoesNotKeepLargeUncapturedLocalsAlive()
    {
        var writer = new MemoryProbeWriter();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
SUB HoldOne()
    DIM temporary AS STRING = REPEAT("x", 8000000)
END SUB
SUB HoldTwo()
    DIM temporary AS STRING = REPEAT("y", 8000000)
END SUB
PRINT "before"
HoldOne()
PRINT "first"
HoldTwo()
PRINT "second"
PRINT "done"
""", outputWriter: writer));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.True(writer.TryGetMemory("first", out var afterFirst));
        Assert.True(writer.TryGetMemory("second", out var afterSecond));

        var retainedBytes = afterSecond - afterFirst;
        Assert.True(
            retainedBytes < 4_000_000,
            $"Кэш кадров удержал {retainedBytes} байт между вызовами.");
    }

    [Fact]
    public void ReturnedClosureStillWorksAfterSourceFrameIsReleased()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Make() AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = 7
    FUNCTION ReadValue() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION
    RETURN ReadValue
END FUNCTION
DIM f AS FUNCTION() AS INTEGER
f = Make()
DIM ignored AS STRING = REPEAT("z", 4000000)
PRINT f()
PRINT f()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["8", "9"], result.Output);
    }

    [Fact]
    public void ByValRecordArgumentIsCopiedOncePerCall()
    {
        var writer = new AllocationProbeWriter();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("TYPE Box\r\n    items[1 TO 10000] AS INTEGER\r\nEND TYPE\r\nDIM shared AS Box\r\nSUB Observe(copy AS Box)\r\n    DIM marker AS INTEGER = copy.items[1]\r\nEND SUB\r\nPRINT \"start\"\r\nFOR i = 1 TO 30\r\n    Observe(shared)\r\nNEXT\r\nPRINT \"end\"", outputWriter: writer));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.True(writer.TryGetAllocations("start", out var start));
        Assert.True(writer.TryGetAllocations("end", out var end));

        var bytesPerCall = (end - start) / 30.0;
        Assert.True(
            bytesPerCall < 120_000,
            $"Подготовка BYVAL-аргумента выделила {bytesPerCall:F0} байт на вызов.");
    }

    private sealed class MemoryProbeWriter : IOutputWriter
    {
        private readonly Dictionary<string, long> _memory = new(StringComparer.Ordinal);

        public void WriteOutput(string text)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _memory[text] = GC.GetTotalMemory(forceFullCollection: true);
        }

        public bool TryGetMemory(string marker, out long bytes) => _memory.TryGetValue(marker, out bytes);
    }

    private sealed class AllocationProbeWriter : IOutputWriter
    {
        private readonly Dictionary<string, long> _allocations = new(StringComparer.Ordinal);

        public void WriteOutput(string text)
        {
            _allocations[text] = GC.GetAllocatedBytesForCurrentThread();
        }

        public bool TryGetAllocations(string marker, out long bytes) => _allocations.TryGetValue(marker, out bytes);
    }
}
