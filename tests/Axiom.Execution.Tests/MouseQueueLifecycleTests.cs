using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class MouseQueueLifecycleTests
{
    [Fact]
    public void ClearMouseStateDiscardsEventsAlreadyDeferredByVm()
    {
        var host = new DeferredMouseHost(
            new MouseInputEvent(MouseInputEventKind.Press, 2),
            new MouseInputEvent(MouseInputEventKind.Press, 1))
        {
            // Очистка поверхности происходит между инструкциями, уже после
            // того как VM отложила событие другой кнопки.
            ClearWhenExhausted = true
        };
        var run = Run("""
            CANVAS 32, 32
            PRINT MOUSEPRESS(1)
            PRINT MOUSEPRESS(2)
            PRINT MOUSEPRESS(2)
            """, host, null);
        Check(run, ["TRUE", "FALSE", "FALSE"]);
    }

    [Fact]
    public void SurfaceClearGenerationDiscardsBufferedPress()
    {
        var host = new DeferredMouseHost(
            new MouseInputEvent(MouseInputEventKind.Press, 2),
            new MouseInputEvent(MouseInputEventKind.Press, 1))
        {
            ClearWhenExhausted = true
        };
        var run = Run("""
            CANVAS 32, 32
            PRINT MOUSEPRESS(1)
            PRINT MOUSEPRESS(1)
            """, host, null);
        Check(run, ["TRUE", "FALSE"]);
    }

    [Fact]
    public void HandlerSeesButtonPressedStateForItsOwnEvent()
    {
        var host = new DeferredMouseHost(new MouseInputEvent(MouseInputEventKind.Press, 1));
        host.PressButton(1);
        var run = Run("""
            CANVAS 32, 32
            DIM seen AS BOOLEAN
            ON MOUSE(1), Mark
            FOR i = 1 TO 4
            NEXT
            PRINT seen
            SUB Mark(button AS INTEGER, x AS LONG, y AS LONG)
            seen = MOUSEBUTTON(button)
            END SUB
            """, host, null);
        Check(run, ["TRUE"]);
    }

    [Fact]
    public void HandlerReceivesOneCoordinateSnapshotPerEvent()
    {
        var host = new DeferredMouseHost(new MouseInputEvent(MouseInputEventKind.Move, 0));
        var run = Run("""
            CANVAS 32, 32
            DIM xs AS INTEGER
            ON MOUSE("MOVE"), Mark
            FOR i = 1 TO 4
            NEXT
            PRINT xs
            SUB Mark(delta AS INTEGER, x AS LONG, y AS LONG)
            xs = xs + 1
            IF x <> y THEN xs = 99
            END SUB
            """, host, null);
        Check(run, ["1"]);
    }

    private static ExecutionResult Run(string source, DeferredMouseHost host, Action? before)
    {
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest(source,
            initialGraphicsContext: Axiom.Semantics.InitialGraphicsContext.Unknown));
        Assert.True(compilation.Succeeded, string.Join("\n", compilation.Diagnostics.Select(d => d.Message)));
        before?.Invoke();
        return pipeline.RunCompiled(compilation.CompiledProgram!, new ExecutionRequest(source, inputProvider: host));
    }

    private static void Check(ExecutionResult run, string[] expected)
    {
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(expected, run.Output);
    }

    private sealed class DeferredMouseHost : IInputProvider, IMouseInputProvider, IGraphicsHost
    {
        private readonly Queue<MouseInputEvent> _events;
        private readonly HashSet<int> _pressedButtons = [];
        private long _generation;
        private long _mouseX = 7;
        private long _mouseY = 7;
        private bool _cleared;

        public DeferredMouseHost(params MouseInputEvent[] events)
        {
            _events = new Queue<MouseInputEvent>(events);
        }

        public bool IsScreenActive => false;
        public bool IsCanvasActive => true;
        public bool IsScreenOutputActive => false;
        public int ScreenWidth => 32;
        public int ScreenHeight => 32;

        public void PressButton(int buttonNumber) => _pressedButtons.Add(buttonNumber);

        public bool ClearWhenExhausted { get; init; }

        public void ResetForNewSurface()
        {
            _events.Clear();
            _pressedButtons.Clear();
            _generation++;
        }

        public void ClearMouseState()
        {
            _events.Clear();
            _pressedButtons.Clear();
            _generation++;
        }

        public long GetMouseX() => _mouseX;
        public long GetMouseY() => _mouseY;
        public long GetMouseClearGeneration() => _generation;
        public bool IsMouseButtonDown(int buttonNumber) => _pressedButtons.Contains(buttonNumber);
        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent()
        {
            if (_events.Count == 0)
            {
                if (ClearWhenExhausted && !_cleared)
                {
                    _cleared = true;
                    ClearMouseState();
                }

                return default;
            }

            var mouseEvent = _events.Dequeue();
            if (mouseEvent.Kind == MouseInputEventKind.Press)
            {
                _pressedButtons.Add(mouseEvent.ButtonNumber);
            }
            else if (mouseEvent.Kind == MouseInputEventKind.Release)
            {
                _pressedButtons.Remove(mouseEvent.ButtonNumber);
            }

            return mouseEvent;
        }

        public string ReadInput(InputRequest request) => throw new InvalidOperationException();
    }
}
