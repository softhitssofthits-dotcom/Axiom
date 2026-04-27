using System.Windows.Threading;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Input;

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
