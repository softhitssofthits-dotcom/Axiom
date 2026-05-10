using System.Windows.Threading;
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
