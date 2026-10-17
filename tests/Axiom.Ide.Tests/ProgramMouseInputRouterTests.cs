using System.Windows;
using System.Windows.Input;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Tests;

public sealed class ProgramMouseInputRouterTests
{
    [Fact]
    public void HandlePreviewMouseMove_UpdatesRoundedCoordinates()
    {
        var sink = new TestProgramInputSink();

        ProgramMouseInputRouter.HandlePreviewMouseMove(sink, new Point(12.6, 24.4));

        Assert.Equal(13, sink.MouseX);
        Assert.Equal(24, sink.MouseY);
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), sink.ReadMouseEvent());
    }

    [Fact]
    public void HandlePreviewMouseDown_PublishesPositionAndPressedButtonAtomically()
    {
        var sink = new TestProgramInputSink();

        ProgramMouseInputRouter.HandlePreviewMouseDown(sink, new Point(10, 20), MouseButton.Middle);

        Assert.Equal(10, sink.MouseX);
        Assert.Equal(20, sink.MouseY);
        Assert.Contains(3, sink.PressedButtons);
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Press, 3), sink.ReadMouseEvent());
    }

    [Fact]
    public void HandlePreviewMouseUp_ReleasesMappedButton()
    {
        var sink = new TestProgramInputSink();
        sink.PressedButtons.Add(2);

        ProgramMouseInputRouter.HandlePreviewMouseUp(sink, new Point(5, 6), MouseButton.Right);

        Assert.Equal(5, sink.MouseX);
        Assert.Equal(6, sink.MouseY);
        Assert.DoesNotContain(2, sink.PressedButtons);
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Release, 2), sink.ReadMouseEvent());
    }

    [Fact]
    public void HandlePreviewMouseWheel_PreservesOriginalWheelUnits()
    {
        var sink = new TestProgramInputSink();

        ProgramMouseInputRouter.HandlePreviewMouseWheel(sink, new Point(7.2, 8.8), 240);

        Assert.Equal(7, sink.MouseX);
        Assert.Equal(9, sink.MouseY);
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Wheel, 240), sink.ReadMouseEvent());
    }

    [Fact]
    public void HandleMouseLeave_ClearsMouseStateAndAdvancesGeneration()
    {
        var sink = new TestProgramInputSink
        {
            MouseX = 100,
            MouseY = 200
        };
        sink.PressedButtons.Add(1);

        ProgramMouseInputRouter.HandleMouseLeave(sink);

        Assert.Equal(-1, sink.MouseX);
        Assert.Equal(-1, sink.MouseY);
        Assert.Empty(sink.PressedButtons);
        Assert.Equal(1, sink.ClearGeneration);
    }

    private sealed class TestProgramInputSink : IProgramConsoleService
    {
        public int OutputCheckpoint => 0;

        public string? ResourceBaseDirectory { get; set; }

        public long MouseX { get; set; } = -1;

        public long MouseY { get; set; } = -1;

        public int ClearGeneration { get; private set; }

        public HashSet<int> PressedButtons { get; } = [];

        public Queue<MouseInputEvent> MouseEvents { get; } = [];

        public void BeginSession()
        {
        }

        public void EndSession()
        {
        }

        public void Clear()
        {
        }

        public void ClearKeyState()
        {
        }

        public void ClearMouseState()
        {
            MouseX = -1;
            MouseY = -1;
            PressedButtons.Clear();
            ClearGeneration++;
        }

        public void ClearOutput()
        {
        }

        public void EnqueueKey(string keyText)
        {
        }

        public void EnqueueMouseButtonPress(int buttonNumber)
        {
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber));
        }

        public void EnqueueMouseButtonRelease(int buttonNumber)
        {
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
        }

        public void EnqueueMouseMove()
        {
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Move, 0));
        }

        public void EnqueueMouseWheel(int delta)
        {
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Wheel, delta));
        }

        public void PublishMouseMove(long x, long y)
        {
            MouseX = x;
            MouseY = y;
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Move, 0));
        }

        public void PublishMouseButtonPress(int buttonNumber, long x, long y)
        {
            MouseX = x;
            MouseY = y;
            PressedButtons.Add(buttonNumber);
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber));
        }

        public void PublishMouseButtonRelease(int buttonNumber, long x, long y)
        {
            MouseX = x;
            MouseY = y;
            PressedButtons.Remove(buttonNumber);
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
        }

        public void PublishMouseWheel(int delta, long x, long y)
        {
            MouseX = x;
            MouseY = y;
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Wheel, delta));
        }

        public void PublishMouseButtonReleaseKeepPosition(int buttonNumber)
        {
            PressedButtons.Remove(buttonNumber);
            MouseEvents.Enqueue(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
        }

        public long GetMouseX() => MouseX;

        public long GetMouseY() => MouseY;

        public void GetMousePosition(out long x, out long y)
        {
            x = MouseX;
            y = MouseY;
        }

        public long GetMouseClearGeneration() => ClearGeneration;

        public bool IsKeyDown(string keyText) => false;

        public bool IsMouseButtonDown(int buttonNumber) => PressedButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent() => MouseEvents.Count > 0 ? MouseEvents.Dequeue() : default;

        public string ReadKey() => string.Empty;

        public void RestoreOutputCheckpoint(int checkpoint)
        {
        }

        public string RequestInput(ProgramInputRequest request) => string.Empty;

        public void SetKeyDown(string keyText)
        {
        }

        public void SetKeyUp(string keyText)
        {
        }

        public void SetMouseButtonDown(int buttonNumber)
        {
            PressedButtons.Add(buttonNumber);
        }

        public void SetMouseButtonUp(int buttonNumber)
        {
            PressedButtons.Remove(buttonNumber);
        }

        public void SetMousePosition(long x, long y)
        {
            MouseX = x;
            MouseY = y;
        }

        public void WriteOutput(string text)
        {
        }
    }
}
