using Axiom.Runtime;
using System.Runtime.ExceptionServices;

namespace Axiom.Execution.Tests;

public sealed class RoutineCallControlContractTests
{
    [Fact]
    public void IndirectFunctionValueLoopObservesCancellation()
    {
        var cancellation = new CancelAfterChecks(64);

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            CountUpThroughFunctionValue(),
            cancellationProvider: cancellation));

        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
        Assert.Empty(result.Output);
    }

    [Fact]
    public void IndirectFunctionValueCancellationBypassesUserCatch()
    {
        var output = new CollectingOutputWriter();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION CountUp() AS INTEGER
    DIM total AS INTEGER = 0
    FOR i = 1 TO 100000
        total = total + 1
    NEXT
    RETURN total
END FUNCTION
DIM op AS FUNCTION() AS INTEGER
op = CountUp
TRY
    PRINT op()
CATCH e
    PRINT "caught"
END TRY
PRINT "after"
""", outputWriter: output, cancellationProvider: new CancelAfterChecks(64)));

        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
        Assert.DoesNotContain("caught", output.Output);
    }

    [Fact]
    public void IndirectFunctionValueBreakpointStopsAndResumesWithSingleResult()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    RETURN 42
END FUNCTION
DIM action AS FUNCTION() AS INTEGER
action = Work
PRINT action()
""", outputWriter: output, sourceBreakpointLines: [2])));

        Assert.False(timedOut, "Косвенный вызов не вернул управление после точки останова.");
        Assert.True(result.Stopped);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Empty(output.Output);

        var resumed = result.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["42"], output.Output);
    }

    [Fact]
    public void IndirectFunctionValueBreakpointAllowsSteppingInside()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    DIM value AS INTEGER = 40
    value = value + 2
    RETURN value
END FUNCTION
DIM action AS FUNCTION() AS INTEGER
action = Work
PRINT action()
""", outputWriter: output, sourceBreakpointLines: [2])));

        Assert.False(timedOut);
        Assert.True(result.Stopped);

        var stepped = result.StopState!.StepInto();

        Assert.True(stepped.Stopped);
        Assert.Equal(3, stepped.StoppedAt!.SourceLocation.Line);

        var resumed = stepped.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["42"], output.Output);
    }

    [Fact]
    public void ArrayMapCallbackBreakpointStopsInsideCallbackAndResumesAllElements()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("FUNCTION Twice(value AS INTEGER) AS INTEGER\r\n    RETURN value * 2\r\nEND FUNCTION\r\nDIM values[1 TO 3] AS INTEGER\r\nvalues[1] = 1\r\nvalues[2] = 2\r\nvalues[3] = 3\r\nDIM mapped[] AS INTEGER\r\nmapped[] = ARRAYMAP(values[], Twice)\r\nPRINT mapped[1]\r\nPRINT mapped[2]\r\nPRINT mapped[3]", outputWriter: output, sourceBreakpointLines: [2])));

        Assert.False(timedOut, "Обратный вызов ARRAYMAP не вернул управление после точки останова.");
        Assert.True(result.Stopped);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);

        var resumed = result.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["2", "4", "6"], output.Output);
    }

    [Fact]
    public void ArrayMapCallbackLoopObservesCancellation()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("FUNCTION Slow(value AS INTEGER) AS INTEGER\r\n    DIM total AS INTEGER = 0\r\n    FOR i = 1 TO 1000\r\n        total = total + 1\r\n    NEXT\r\n    RETURN value\r\nEND FUNCTION\r\nDIM values[1 TO 200] AS INTEGER\r\nFOR i = 1 TO 200\r\n    values[i] = i\r\nNEXT\r\nDIM mapped[] AS INTEGER\r\nmapped[] = ARRAYMAP(values[], Slow)\r\nPRINT mapped[200]", cancellationProvider: new CancelAfterChecks(256)));

        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
    }

    [Fact]
    public void OnKeyHandlerBreakpointStopsInsideHandlerAndResumesDispatch()
    {
        var output = new CollectingOutputWriter();
        var input = new QueueKeyboardProvider("LEFT");
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
PRINT "ready"
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1
PRINT "after"

SUB HandleLeft()
    PRINT "handler"
END SUB
""", inputProvider: input, outputWriter: output, sourceBreakpointLines: [7])));

        Assert.False(timedOut, "Обработчик ON KEY не вернул управление после точки останова.");
        Assert.True(result.Stopped);
        Assert.Equal(7, result.StoppedAt!.SourceLocation.Line);

        var resumed = result.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["ready", "handler", "after"], output.Output);
    }

    [Fact]
    public void IndirectFunctionValueBreakpointAllowsSteppingOverInsideCallee()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    DIM value AS INTEGER = 40
    value = value + 2
    RETURN value
END FUNCTION
DIM action AS FUNCTION() AS INTEGER
action = Work
PRINT action()
""", outputWriter: output, sourceBreakpointLines: [2])));

        Assert.False(timedOut);
        Assert.True(result.Stopped);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);

        var stepped = result.StopState!.StepOver();

        Assert.True(stepped.Stopped);
        Assert.Equal(3, stepped.StoppedAt!.SourceLocation.Line);

        var resumed = stepped.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["42"], output.Output);
    }

    [Fact]
    public void IndirectFunctionValueBreakpointAllowsSteppingOutOfTheCallee()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    DIM value AS INTEGER = 42
    RETURN value
END FUNCTION
DIM action AS FUNCTION() AS INTEGER
action = Work
PRINT action()
""", outputWriter: output, sourceBreakpointLines: [3])));

        Assert.False(timedOut);
        Assert.True(result.Stopped);

        var stepped = result.StopState!.StepOut();

        Assert.True(stepped.Stopped);
        Assert.Equal(7, stepped.StoppedAt!.SourceLocation.Line);
        Assert.Empty(output.Output);

        var resumed = stepped.StopState!.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["42"], output.Output);
    }

    [Fact]
    public void ArrayMapCallbackBreakpointDoesNotOfferStepBackAcrossContinuation()
    {
        var output = new CollectingOutputWriter();
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("FUNCTION Twice(value AS INTEGER) AS INTEGER\r\n    RETURN value * 2\r\nEND FUNCTION\r\nDIM values[1 TO 1] AS INTEGER\r\nvalues[1] = 1\r\nDIM mapped[] AS INTEGER\r\nmapped[] = ARRAYMAP(values[], Twice)\r\nPRINT mapped[1]", outputWriter: output, sourceBreakpointLines: [2])));

        Assert.False(timedOut);
        Assert.True(result.Stopped);
        Assert.False(result.StopState!.CanStepBack);
        Assert.Throws<InvalidOperationException>(() => result.StopState.StepBack());

        var resumed = result.StopState.Resume();

        Assert.False(resumed.Stopped);
        Assert.Equal(["2"], output.Output);
    }

    [Fact]
    public void PositiveRoutineContractsRemainStable()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("FUNCTION Make(seed AS INTEGER) AS FUNCTION() AS INTEGER\r\n    DIM current AS INTEGER = seed\r\n    FUNCTION NextValue() AS INTEGER\r\n        current = current + 1\r\n        RETURN current\r\n    END FUNCTION\r\n    RETURN NextValue\r\nEND FUNCTION\r\nDIM first AS FUNCTION() AS INTEGER\r\nDIM second AS FUNCTION() AS INTEGER\r\nfirst = Make(10)\r\nsecond = Make(100)\r\nPRINT first()\r\nPRINT second()\r\nPRINT first()\r\n\r\nSUB Edit(items[] AS INTEGER )\r\n    items[1] = 20\r\n    DIM replacement[1 TO 1] AS INTEGER\r\n    replacement[1] = 99\r\n    items[] = replacement[]\r\nEND SUB\r\nDIM data[1 TO 1] AS INTEGER\r\ndata[1] = 10\r\nEdit(data[])\r\nPRINT data[1]\r\n\r\nFUNCTION Deep(n AS INTEGER) AS INTEGER\r\n    IF n = 0 THEN RETURN 0\r\n    RETURN Deep(n - 1) + 1\r\nEND FUNCTION\r\nPRINT Deep(5)"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["11", "101", "12", "20", "5"], result.Output);
    }

    private static string CountUpThroughFunctionValue() => """
FUNCTION CountUp() AS INTEGER
    DIM total AS INTEGER = 0
    FOR i = 1 TO 100000
        total = total + 1
    NEXT
    RETURN total
END FUNCTION
DIM op AS FUNCTION() AS INTEGER
op = CountUp
PRINT op()
""";

    private static (ExecutionResult Result, bool TimedOut) RunWithWatchdog(Func<ExecutionResult> run)
    {
        const int timeoutMilliseconds = 15000;
        ExecutionResult? result = null;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = run();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        {
            IsBackground = true,
            Name = "routine-call-control-watchdog"
        };

        thread.Start();
        if (!thread.Join(timeoutMilliseconds))
        {
            return (null!, true);
        }

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return (result!, false);
    }

    private sealed class CancelAfterChecks(int limit) : IRuntimeCancellationProvider
    {
        private int _checks;

        public bool IsCancellationRequested => ++_checks >= limit;
    }

    private sealed class CollectingOutputWriter : IOutputWriter
    {
        public List<string> Output { get; } = [];

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }
    }

    private sealed class QueueKeyboardProvider(params string[] keys) : IInputProvider, IKeyboardInputProvider
    {
        private readonly Queue<string> _events = new(keys);

        public string ReadInput(InputRequest request) => throw new InvalidOperationException("Блокирующий ввод не ожидается.");

        public string ReadKey() => _events.TryDequeue(out var value) ? value : string.Empty;

        public bool IsKeyDown(string keyText) => false;
    }
}
