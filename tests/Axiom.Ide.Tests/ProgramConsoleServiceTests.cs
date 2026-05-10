using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Tests;

public sealed class ProgramConsoleServiceTests
{
    [Fact]
    public void RequestInput_ShowsConsolePromptAndEchoesSubmittedText()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.False(service.IsWaitingForInput);
        Assert.Equal("Name? Ada", service.OutputText);
    }

    [Fact]
    public void WriteOutput_AppendsConsoleTranscript()
    {
        var service = new ProgramConsoleService();

        service.BeginSession();
        service.WriteOutput("hello");
        service.WriteOutput("world");

        Assert.Equal($"hello{Environment.NewLine}world", service.OutputText);
    }

    [Fact]
    public void BeginSession_ClearsExistingConsoleState()
    {
        var service = new ProgramConsoleService();
        service.WriteOutput("old");

        service.BeginSession();

        Assert.Equal(string.Empty, service.OutputText);
        Assert.False(service.IsWaitingForInput);
        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void ReadKey_ReturnsQueuedKeyThenEmpty()
    {
        var service = new ProgramConsoleService();

        service.EnqueueKey("LEFT");

        Assert.Equal("LEFT", service.ReadKey());
        Assert.Equal(string.Empty, service.ReadKey());
    }

    [Fact]
    public void BeginSession_ClearsQueuedKeys()
    {
        var service = new ProgramConsoleService();

        service.EnqueueKey("A");
        service.BeginSession();

        Assert.Equal(string.Empty, service.ReadKey());
    }

    [Fact]
    public void IsKeyDown_TracksPressedKeysUntilReleased()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.SetKeyDown("ctrl");

        Assert.True(service.IsKeyDown("LEFT"));
        Assert.True(service.IsKeyDown("CTRL"));
        Assert.False(service.IsKeyDown("RIGHT"));

        service.SetKeyUp("LEFT");

        Assert.False(service.IsKeyDown("LEFT"));
        Assert.True(service.IsKeyDown("CTRL"));
    }

    [Fact]
    public void BeginSession_ClearsPressedKeys()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.BeginSession();

        Assert.False(service.IsKeyDown("LEFT"));
    }

    [Fact]
    public void ClearKeyState_ClearsPressedKeysWithoutConsumingQueue()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.EnqueueKey("LEFT");
        service.ClearKeyState();

        Assert.False(service.IsKeyDown("LEFT"));
        Assert.Equal("LEFT", service.ReadKey());
    }

    [Fact]
    public void MouseState_TracksPositionAndButtons()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(128, 256);
        service.EnqueueMouseButtonPress(1);
        service.EnqueueMouseButtonPress(3);
        service.SetMouseButtonDown(1);
        service.SetMouseButtonDown(3);

        Assert.Equal(128, service.GetMouseX());
        Assert.Equal(256, service.GetMouseY());
        Assert.True(service.IsMouseButtonDown(1));
        Assert.False(service.IsMouseButtonDown(2));
        Assert.True(service.IsMouseButtonDown(3));
        Assert.Equal(1, service.ReadMouseButtonPress());
        Assert.Equal(3, service.ReadMouseButtonPress());
        Assert.Equal(0, service.ReadMouseButtonPress());

        service.SetMouseButtonUp(1);

        Assert.False(service.IsMouseButtonDown(1));
        Assert.True(service.IsMouseButtonDown(3));
    }

    [Fact]
    public void MouseState_QueuesReleaseAndMoveEvents()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseMove();
        service.EnqueueMouseButtonRelease(1);
        service.EnqueueMouseWheel(-120);

        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Release, 1), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Wheel, -120), service.ReadMouseEvent());
        Assert.Equal(default, service.ReadMouseEvent());
    }

    [Fact]
    public void BeginSession_ClearsMouseState()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseButtonPress(2);
        service.SetMouseButtonDown(2);
        service.BeginSession();

        Assert.Equal(-1, service.GetMouseX());
        Assert.Equal(-1, service.GetMouseY());
        Assert.False(service.IsMouseButtonDown(2));
        Assert.Equal(0, service.ReadMouseButtonPress());
    }

    [Fact]
    public void ClearMouseState_ResetsPositionAndButtons()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseButtonPress(1);
        service.SetMouseButtonDown(1);
        service.ClearMouseState();

        Assert.Equal(-1, service.GetMouseX());
        Assert.Equal(-1, service.GetMouseY());
        Assert.False(service.IsMouseButtonDown(1));
        Assert.Equal(0, service.ReadMouseButtonPress());
    }

    [Fact]
    public void ActivateScreen_UsesGraphicsStateSeparateFromOutput()
    {
        var service = new ProgramConsoleService();

        service.WriteOutput("before");
        service.ActivateScreen(10);
        service.WriteGraphicsText("hello", newLine: true);

        Assert.True(service.IsScreenActive);
        Assert.Equal(10, service.ScreenMode);
        Assert.Equal(1280, service.ScreenWidth);
        Assert.Equal(720, service.ScreenHeight);
        Assert.Equal("before", service.OutputText);
        Assert.Equal($"hello{Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void WriteGraphicsText_OverwritesAtLocatedCursor()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.WriteGraphicsText("event=120 x=300 y=200", newLine: true);
        service.LocateText(1, 1);
        service.WriteGraphicsText("event=1 x=7 y=9      ", newLine: true);

        Assert.Equal($"event=1 x=7 y=9      {Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void WriteGraphicsText_UpdatesFramebufferTextLayer()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.WriteGraphicsText("A", newLine: false);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Contains(frame.BgraPixels, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
    }

    [Fact]
    public void ScreenInput_UpdatesFramebufferInputLine()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.ActivateScreen(1);
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            service.CurrentInputText = "Ada";

            var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
            var inputRowStart = (frame.Height - ScreenTextRasterizer.CellHeight) * frame.Width;
            var inputRowPixels = frame.BgraPixels.Skip(inputRowStart).Take(ScreenTextRasterizer.CellHeight * frame.Width);
            Assert.Contains(inputRowPixels, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));

            service.SubmitInput();
        });

        var result = service.ReadGraphicsInput(new InputRequest("Name", 1));

        Assert.Equal("Ada", result);
    }

    [Fact]
    public void DrawLine_RasterizesIntermediatePixels()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 4, 1, 12, GraphicsLineKind.Normal);

        Assert.Equal(12, service.GetPixel(1, 1));
        Assert.Equal(12, service.GetPixel(2, 1));
        Assert.Equal(12, service.GetPixel(3, 1));
        Assert.Equal(12, service.GetPixel(4, 1));
    }

    [Fact]
    public void DrawLine_UpdatesFramebufferSnapshot()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 4, 1, 12, GraphicsLineKind.Normal);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(320, frame.Width);
        Assert.Equal(200, frame.Height);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[(1 * frame.Width) + 1]);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[(1 * frame.Width) + 4]);
    }

    [Fact]
    public void DrawLine_FilledBoxUsesSingleClippedFill()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(220, 100, 360, 660, 11, GraphicsLineKind.FilledBox);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(11), frame.BgraPixels[(150 * frame.Width) + 250]);
        Assert.Equal(11, service.GetPixel(250, 150));
        Assert.Equal(0, service.GetPixel(10, 10));
    }

    [Fact]
    public void DrawCircle_RasterizesCirclePoints()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(10, 10, 3, 14);

        Assert.Equal(14, service.GetPixel(13, 10));
        Assert.Equal(14, service.GetPixel(7, 10));
        Assert.Equal(14, service.GetPixel(10, 13));
        Assert.Equal(14, service.GetPixel(10, 7));
    }

    [Fact]
    public void ClearOutput_ClearsTranscriptWithoutResettingPendingInput()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.WriteOutput("old");
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.ClearOutput();
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.Equal("Name? Ada", service.OutputText);
    }

    [Fact]
    public void ConsoleActions_WriteTraceEntries()
    {
        var traceService = new FakeIdeTraceService();
        var service = new ProgramConsoleService(traceService);
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.BeginSession();
        service.WriteOutput("hello");

        dispatcher.BeginInvoke(() =>
        {
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.begin");
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.output" && entry.Message == "hello");
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.input.request" && entry.Message.Contains("prompt=\"Name?\"", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.input.submit" && entry.Message.Contains("Ada", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsoleActions_WriteStructuredEventLogEntries()
    {
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramConsoleService(eventLogService: eventLogService);
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.BeginSession();
        service.WriteOutput("hello");

        dispatcher.BeginInvoke(() =>
        {
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        _ = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "console.session-begin"
                && entry.Category == "output");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "output.changed"
                && entry.TextPreview == "hello");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "input.requested"
                && entry.Category == "input");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "input.submitted"
                && entry.TextPreview == "Ada");
    }

    [Fact]
    public void ScreenActions_WriteGraphicsTraceAndEventLogEntries()
    {
        var traceService = new FakeIdeTraceService();
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramConsoleService(traceService, eventLogService);

        service.ActivateScreen(5);
        service.EnqueueKey("ESC");
        Assert.Equal("ESC", service.ReadKey());
        service.EnqueueMouseMove();
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), service.ReadMouseEvent());
        service.DeactivateScreen();

        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.activated" && entry.Message.Contains("mode=5", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.key.enqueued" && entry.Message.Contains("ESC", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.key.read" && entry.Message.Contains("ESC", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.mouse.enqueued" && entry.Message.Contains("Move", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.mouse.read" && entry.Message.Contains("Move", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.deactivated" && entry.Message.Contains("mode=5", StringComparison.Ordinal));

        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.activated");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.key.enqueued");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.key.read");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.mouse.enqueued");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.mouse.read");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.deactivated");
    }

    private sealed class FakeIdeTraceService : IIdeTraceService
    {
        public List<(string Category, string Message)> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public void BeginSession(string sessionName)
        {
        }

        public void Write(string category, string message)
        {
            Entries.Add((category, message));
        }

        public void WriteException(string category, Exception exception, string? note = null)
        {
            Entries.Add((category, note ?? exception.Message));
        }
    }

    private sealed class FakeIdeEventLogService : IIdeEventLogService
    {
        public List<IdeEventLogEntry> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public string SessionId => "test";

        public void BeginSession(string sessionName)
        {
        }

        public void Write(IdeEventLogEntry entry)
        {
            Entries.Add(entry);
        }

        public void Flush()
        {
        }

        public int DeleteInactiveLogs() => 0;

        public void Dispose()
        {
        }
    }
}
