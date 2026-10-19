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
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Twice(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
DIM values(1 TO 3) AS INTEGER
values(1) = 1
values(2) = 2
values(3) = 3
DIM mapped AS INTEGER()
mapped = ARRAYMAP(values, Twice)
PRINT mapped(1)
PRINT mapped(2)
PRINT mapped(3)
""", outputWriter: output, sourceBreakpointLines: [2])));

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
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Slow(value AS INTEGER) AS INTEGER
    DIM total AS INTEGER = 0
    FOR i = 1 TO 1000
        total = total + 1
    NEXT
    RETURN value
END FUNCTION
DIM values(1 TO 200) AS INTEGER
FOR i = 1 TO 200
    values(i) = i
NEXT
DIM mapped AS INTEGER()
mapped = ARRAYMAP(values, Slow)
PRINT mapped(200)
""", cancellationProvider: new CancelAfterChecks(256)));

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
        var (result, timedOut) = RunWithWatchdog(() => new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Twice(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
DIM values(1 TO 1) AS INTEGER
values(1) = 1
DIM mapped AS INTEGER()
mapped = ARRAYMAP(values, Twice)
PRINT mapped(1)
""", outputWriter: output, sourceBreakpointLines: [2])));

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
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Make(seed AS INTEGER) AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = seed
    FUNCTION NextValue() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION
    RETURN NextValue
END FUNCTION
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
first = Make(10)
second = Make(100)
PRINT first()
PRINT second()
PRINT first()

SUB Edit(items AS INTEGER ())
    items(1) = 20
    DIM replacement(1 TO 1) AS INTEGER
    replacement(1) = 99
    items = replacement
END SUB
DIM data(1 TO 1) AS INTEGER
data(1) = 10
Edit(data)
PRINT data(1)

FUNCTION Deep(n AS INTEGER) AS INTEGER
    IF n = 0 THEN RETURN 0
    RETURN Deep(n - 1) + 1
END FUNCTION
PRINT Deep(5)
"""));

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
