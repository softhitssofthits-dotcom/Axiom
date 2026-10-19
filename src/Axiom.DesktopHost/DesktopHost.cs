using System.IO;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Axiom.Execution;
using Axiom.Ide.Contracts;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.DesktopHost;

// Состояние сессии меняется только в Dispatcher. VM работает на отдельном потоке;
// чтение stdin остаётся независимым от VM, включая ожидание INPUT.
internal sealed class DesktopHost : IDisposable
{
    private readonly Application _application;
    private readonly ProgramConsoleService _console;
    private readonly ProgramRunService _runner;
    private readonly FramePipe? _frames;
    private readonly object _writeLock = new();
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? _sessionId;
    private string? _graphicsSessionId;
    private string _surfaceKind = "screen";
    private int _documentRequests;
    private long _sequence;
    private long _stopCounter;
    private long _inputCounter;
    private string? _stopId;
    private string? _inputRequestId;
    private IProgramDebuggerState? _debugger;
    private ManualCancellationProvider? _cancel;
    private sealed record CancellationRegistration(string SessionId, ManualCancellationProvider Provider);
    private CancellationRegistration? _cancellationRegistration;
    private bool _working;
    private volatile bool _shutdown;
    private Task? _worker;
    private readonly DispatcherTimer _outputTimer;
    private string _publishedOutput = "";
    private long _outputVersion;
    private string? _pendingOutputVersion;

    public DesktopHost(Application application, string? framePipe = null)
    {
        _application = application;
        _console = new ProgramConsoleService(dispatcher: application.Dispatcher);
        _console.GraphicsHost.UseNativeCanvasWindow = true;
        _console.GraphicsHost.UseGpuCanvas = true;
        _console.GraphicsHost.UseGpuScreen = true;
        _console.GraphicsHost.GraphicsCancellationRequested = () => Volatile.Read(ref _cancellationRegistration)?.Provider.IsCancellationRequested == true;
        _console.GraphicsHost.GraphicsError += message =>
        {
            Event("graphics.error", new { code = "GRAPHICS_DEVICE_ERROR", message });
            Cancel();
        };
        _runner = new ProgramRunService(new AxiomExecutionPipeline(), _console);
        _outputTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background,
            (_, _) => PublishOutput(), application.Dispatcher);
        if (framePipe is not null) _frames = new FramePipe(framePipe, error => {
            Console.Error.WriteLine(error);
            _ = application.Dispatcher.BeginInvoke(() => {
                Event("graphics.error", new { code = "HOST_DISCONNECTED", message = "Потеряна связь с графической панелью." });
                Cancel();
            });
        });
        _console.PropertyChanged += (_, e) =>
        {
            if (_sessionId is not null && e.PropertyName is nameof(ProgramConsoleService.GraphicsFrame) or nameof(ProgramConsoleService.ScreenWindowHandle) or nameof(ProgramConsoleService.IsScreenActive) or nameof(ProgramConsoleService.IsCanvasActive))
            {
                // При деактивации флаги уже сброшены; closed относится к прежней поверхности.
                if (_console.IsCanvasActive) _surfaceKind = "canvas";
                else if (_console.IsScreenOutputActive) _surfaceKind = "screen";
                Event("graphics.surface", new { kind = _surfaceKind,
                    state = _console.IsScreenOutputActive || _console.IsCanvasActive ? "changed" : "closed",
                    logicalWidth = _console.ScreenWidth, logicalHeight = _console.ScreenHeight,
                    windowHandle = _console.ScreenWindowHandle.ToInt64().ToString(),
                    windowVisible = _console.GraphicsHost.ScreenWindowVisible,
                    graphicsDriver = _console.GraphicsHost.CanvasGraphicsDriver,
                    smooth = _console.GraphicsHost.GraphicsSmoothingEnabled });
                if (_console.IsCanvasActive && !_console.GraphicsHost.UseNativeCanvasWindow && _console.GraphicsFrame is { } frame)
                    _frames?.Publish(_sessionId!, frame);
            }
            if (e.PropertyName != nameof(ProgramConsoleService.IsWaitingForInput) || _sessionId is null) return;
            if (_console.IsWaitingForInput)
            {
                _inputRequestId = (++_inputCounter).ToString();
                Event("execution.state", new { state = "WaitingForInput" });
                Event("input.request", new { inputRequestId = _inputRequestId, prompt = _console.CurrentPromptText,
                    outputText = _console.OutputText, outputLineCount = _console.OutputCheckpoint, target = _console.IsScreenOutputActive ? "screen" : "console" });
            }
            else
            {
                var completedRequestId = _inputRequestId;
                _inputRequestId = null;
                if (completedRequestId is not null)
                    Event("input.completed", new { inputRequestId = completedRequestId });
            }
        };
    }

    public void Start()
    {
        using var stampStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("host-build.json")
            ?? throw new InvalidOperationException("Отсутствует подпись сборки служебного процесса.");
        using var stamp = JsonDocument.Parse(stampStream);
        Event("host.ready", new { executionEngine = "BytecodeVm", hostVersion = "1",
            sourceRevision = String(stamp.RootElement, "sourceRevision"), sourceFingerprint = String(stamp.RootElement, "sourceFingerprint"),
            capabilities = new[] { "compile", "analyse", "run", "input", "screen", "canvas", "debug", "stepBack" } });
        _ = Task.Run(ReadRequests);
    }

    private async Task ReadRequests()
    {
        try
        {
            using var input = Console.OpenStandardInput();
            using var line = new MemoryStream();
            var buffer = new byte[8192];
            bool oversized = false;
            while (!_shutdown)
            {
                int count = await input.ReadAsync(buffer);
                if (count == 0) break;
                for (int i = 0; i < count; i++)
                {
                    if (buffer[i] == 10)
                    {
                        if (oversized) Write(new { protocolVersion = 1, kind = "response", id = (string?)null,
                            ok = false, error = new { code = "MESSAGE_TOO_LARGE", message = "Сообщение превышает 16 MiB." } });
                        else
                        {
                            try
                            {
                                using var document = JsonDocument.Parse(line.ToArray());
                                var request = document.RootElement.Clone();
                                var registration = Volatile.Read(ref _cancellationRegistration);
                                if (registration is not null && String(request, "method") == "execution.stop" &&
                                    String(request, "sessionId") == registration.SessionId)
                                    registration.Provider.Cancel();
                                _ = _application.Dispatcher.BeginInvoke(() => Handle(request));
                            }
                            catch (JsonException e) { Console.Error.WriteLine("Некорректный JSON: " + e.Message); }
                        }
                        line.SetLength(0); oversized = false;
                    }
                    else if (!oversized)
                    {
                        if (line.Length >= IdeProtocol.MaximumMessageBytes) { oversized = true; line.SetLength(0); }
                        else line.WriteByte(buffer[i]);
                    }
                }
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e); }
        finally
        {
            Volatile.Read(ref _cancellationRegistration)?.Provider.Cancel();
            _ = _application.Dispatcher.BeginInvoke(Shutdown);
        }
    }

    private void Handle(JsonElement request)
    {
        string? id = null;
        try
        {
            id = String(request, "id");
            if (id is null || id.Length is 0 or > 128) throw new HostError("INVALID_REQUEST", "Недопустимый идентификатор запроса.");
            if (!request.TryGetProperty("protocolVersion", out var version) || version.GetInt32() != IdeProtocol.Version)
                throw new HostError("PROTOCOL_VERSION_MISMATCH", "Несовместимая версия протокола.");
            if (String(request, "kind") != "request") throw new HostError("INVALID_REQUEST", "Ожидался запрос.");
            string method = String(request, "method") ?? "";
            var payload = request.TryGetProperty("payload", out var data) ? data : default;
            switch (method)
            {
                case "host.ping": Respond(id, new { alive = true }); break;
                case "host.shutdown": Respond(id, new { accepted = true }); Shutdown(); break;
                case "execution.start": StartExecution(id, String(request, "sessionId"), payload); break;
                case "execution.stop":
                    RequireSession(request); Cancel(); Respond(id, new { accepted = true }); break;
                case "graphics.key":
                    RequireSession(request);
                    // Клавиатура нужна и текстовым программам: INKEY, KEYDOWN, KEYPRESS и SLEEP.
                    string keyAction = String(payload, "action") ?? "";
                    string key = String(payload, "key") ?? "";
                    string keyText = String(payload, "text") ?? "";
                    if (keyAction is not ("down" or "up" or "clear") ||
                        (keyAction != "clear" && (key.Length is 0 or > 32)) || keyText.Length > 32)
                        throw new HostError("INVALID_REQUEST", "Недопустимое событие клавиатуры.");
                    if (keyAction == "clear") _console.ClearKeyState();
                    else if (keyAction == "up") _console.SetKeyUp(key);
                    else
                    {
                        _console.SetKeyDown(key);
                        if (!_console.IsWaitingForInput && keyText.Length > 0) _console.EnqueueKey(keyText);
                    }
                    Respond(id, new { accepted = true }); break;
                case "graphics.ack":
                    // Последний кадр может быть показан после execution.finished.
                    if (_graphicsSessionId is null || String(request, "sessionId") != _graphicsSessionId)
                        throw new HostError("INVALID_STATE", "Графическая сессия уже недействительна.");
                    if (_frames?.Acknowledge(String(payload, "frameId") ?? "") != true)
                        throw new HostError("INVALID_STATE", "Кадр уже подтверждён или недействителен.");
                    Respond(id, new { accepted = true }); break;
                case "graphics.mouse":
                    RequireSession(request); HandleMouse(payload); Respond(id, new { accepted = true }); break;
                case "output.ack":
                    RequireSession(request);
                    if (String(payload, "outputVersion") != _pendingOutputVersion || _pendingOutputVersion is null)
                        throw new HostError("INVALID_STATE", "Версия вывода уже недействительна.");
                    _pendingOutputVersion = null; Respond(id, new { accepted = true }); break;
                case "input.submit":
                    RequireSession(request);
                    if (_inputRequestId is null || String(payload, "inputRequestId") != _inputRequestId || !_console.IsWaitingForInput)
                        throw new HostError("INVALID_INPUT", "Запрос ввода уже завершён.");
                    _console.CurrentInputText = String(payload, "text") ?? throw new HostError("INVALID_REQUEST", "Отсутствует текст ввода.");
                    _console.SubmitInput(); Respond(id, new { accepted = true }); break;
                case "debug.continue": case "debug.stepOver": case "debug.stepInto": case "debug.stepOut": case "debug.stepBack":
                    RequireSession(request);
                    if (_working || _debugger is null || _stopId is null || String(payload, "stopId") != _stopId)
                        throw new HostError("INVALID_STOP", "Отладочная остановка уже недействительна.");
                    var debugger = _debugger;
                    _stopId = null; _debugger = null; _working = true;
                    Respond(id, new { accepted = true });
                    Event("execution.state", new { state = "Running" });
                    Work(() => method switch {
                        "debug.stepOver" => _runner.StepOver(debugger), "debug.stepInto" => _runner.StepInto(debugger),
                        "debug.stepOut" => _runner.StepOut(debugger), "debug.stepBack" => _runner.StepBack(debugger),
                        _ => _runner.Continue(debugger) });
                    break;
                case "file.save":
                    var fileRequestId = id;
                    _ = Task.Run(() => {
                        try { Respond(fileRequestId, FileCommands.Save(payload)); }
                        catch (Exception e) { Error(fileRequestId, "FILE_SAVE_FAILED", e.Message); }
                    });
                    break;
                case "language.metadata":
                    using (var metadata = JsonDocument.Parse(Axiom.Semantics.AxiomLanguageMetadata.ToJson()))
                        Respond(id, metadata.RootElement.Clone());
                    break;
                case "document.compile": case "document.analyse": case "document.hover": case "document.navigation": case "document.format": case "document.editTransition": case "document.normalizeIdentifiers":
                    if (_sessionId is not null && method == "document.compile") throw new HostError("BUSY", "Программа ещё выполняется.");
                    if (_documentRequests >= 8) throw new HostError("BUSY", "Слишком много запросов документа.");
                    _documentRequests++;
                    var requestId = id;
                    _ = Task.Run(() => {
                        try { Respond(requestId, DocumentCommands.Execute(method, payload)); }
                        catch (Exception e) { Error(requestId, "INVALID_DOCUMENT", e.Message); }
                        finally { _ = _application.Dispatcher.BeginInvoke(() => _documentRequests--); }
                    });
                    break;
                default: throw new HostError("UNKNOWN_METHOD", "Неизвестная команда: " + method);
            }
        }
        catch (HostError e) { Error(id, e.Code, e.Message); }
        catch (Exception e) { Error(id, "INVALID_REQUEST", e.Message); }
    }

    private void StartExecution(string id, string? session, JsonElement payload)
    {
        if (_sessionId is not null) throw new HostError("BUSY", "Программа ещё выполняется.");
        if (string.IsNullOrWhiteSpace(session) || session.Length > 128) throw new HostError("INVALID_SESSION", "Недопустимая сессия.");
        string source = String(payload, "sourceText") ?? throw new HostError("INVALID_REQUEST", "Отсутствует исходный текст.");
        string file = String(payload, "sourceFilePath") ?? "";
        if (!Path.IsPathFullyQualified(file) || !File.Exists(file)) throw new HostError("INVALID_DOCUMENT", "Перед запуском сохраните документ.");
        string launch = String(payload, "launchKind") ?? "";
        if (launch is not ("run" or "debug")) throw new HostError("INVALID_REQUEST", "Недопустимый вид запуска.");
        int[] breakpoints = payload.TryGetProperty("breakpointLines", out var points) ? points.EnumerateArray().Select(x => x.GetInt32()).ToArray() : [];
        bool stopFirst = launch == "debug" && payload.TryGetProperty("stopBeforeFirstInstruction", out var stop) && stop.GetBoolean();
        _frames?.Reset();
        _publishedOutput = ""; _pendingOutputVersion = null;
        _sessionId = session; _graphicsSessionId = session; _sequence = 0; _cancel = new ManualCancellationProvider(); _working = true;
        Volatile.Write(ref _cancellationRegistration, new CancellationRegistration(session, _cancel));
        Respond(id, new { accepted = true }); Event("execution.state", new { state = "Compiling" });
        Work(() => _runner.Run(source, launch == "debug" ? breakpoints : [], stopFirst, file, cancellationProvider: _cancel));
    }

    private void Work(Func<ProgramRunResult> action)
    {
        _worker = Task.Run(() =>
        {
            try
            {
                var result = action();
                _application.Dispatcher.BeginInvoke(() => Complete(result));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                _application.Dispatcher.BeginInvoke(() => {
                    Event("execution.finished", new { status = "InfrastructureFailed", outputText = _console.OutputText, outputLineCount = _console.OutputCheckpoint, actualEngine = (string?)null });
                    Finish();
                });
            }
        });
    }

    private void Complete(ProgramRunResult result)
    {
        _working = false;
        if (result.DebuggerState is { } debugger && _cancel?.IsCancellationRequested != true)
        {
            _debugger = debugger; _stopId = (++_stopCounter).ToString();
            Event("execution.state", new { state = "Paused" });
            Event("debug.paused", new { stopId = _stopId, debugger.StopInfo.Line, debugger.StopInfo.Column,
                debugger.StopInfo.Opcode, debugger.StopInfo.CallDepth, debugger.StopInfo.CanStepBack, outputText = _console.OutputText, outputLineCount = _console.OutputCheckpoint });
            return;
        }
        var status = result.Status switch {
            ProgramRunStatus.Success => "Succeeded", ProgramRunStatus.Cancelled => "Cancelled",
            ProgramRunStatus.SyntaxFailure or ProgramRunStatus.SemanticFailure or ProgramRunStatus.BackendFailure => "CompilationFailed",
            ProgramRunStatus.RuntimeFailure => "RuntimeFailed", _ => "InfrastructureFailed" };
        Event("execution.finished", new { status, outputText = _console.OutputText, outputLineCount = _console.OutputCheckpoint, actualEngine = result.ActualEngine?.ToString(),
            diagnostics = result.Diagnostics, durationMilliseconds = result.TotalDuration.TotalMilliseconds });
        Finish();
    }

    private void Finish()
    {
        Volatile.Write(ref _cancellationRegistration, null);
        _sessionId = null; _debugger = null; _stopId = null; _inputRequestId = null; _working = false;
        if (_shutdown) _application.Shutdown();
    }

    private void PublishOutput()
    {
        if (_sessionId is null || _pendingOutputVersion is not null || _console.OutputText == _publishedOutput) return;
        string text = _console.OutputText;
        _pendingOutputVersion = (++_outputVersion).ToString();
        bool append = text.StartsWith(_publishedOutput, StringComparison.Ordinal);
        Event(append ? "output.append" : "output.replace", new { outputVersion = _pendingOutputVersion,
            text = append ? text[_publishedOutput.Length..] : text, outputLineCount = _console.OutputCheckpoint });
        _publishedOutput = text;
    }

    private void RequireSession(JsonElement request)
    {
        if (_sessionId is null || String(request, "sessionId") != _sessionId)
            throw new HostError("INVALID_SESSION", "Указанная сессия уже завершена.");
    }

    private void HandleMouse(JsonElement payload)
    {
        if (!_console.IsCanvasActive) throw new HostError("INVALID_STATE", "Холст не активен.");
        string action = String(payload, "action") ?? "";
        if (action == "clear") { _console.ClearMouseState(); return; }
        if (action is not ("move" or "down" or "up" or "wheel") ||
            !payload.TryGetProperty("x", out var xValue) || !xValue.TryGetInt64(out long x) ||
            !payload.TryGetProperty("y", out var yValue) || !yValue.TryGetInt64(out long y))
            throw new HostError("INVALID_REQUEST", "Недопустимые координаты мыши.");
        int button = 0, delta = 0;
        if (action is "down" or "up" && (!payload.TryGetProperty("button", out var buttonValue) ||
            !buttonValue.TryGetInt32(out button) || button is < 1 or > 3))
            throw new HostError("INVALID_REQUEST", "Недопустимая кнопка мыши.");
        if (action == "wheel" && (!payload.TryGetProperty("delta", out var deltaValue) || !deltaValue.TryGetInt32(out delta)))
            throw new HostError("INVALID_REQUEST", "Недопустимое движение колеса мыши.");
        // Из окна приходят пиксели холста. Камеру и VIEWPORT применяет VM при чтении.
        // Позиция, состояние кнопки и событие публикуются одной операцией.
        switch (action)
        {
            case "move": _console.PublishMouseMove(x, y); break;
            case "down": _console.PublishMouseButtonPress(button, x, y); break;
            case "up": _console.PublishMouseButtonRelease(button, x, y); break;
            case "wheel": _console.PublishMouseWheel(delta, x, y); break;
        }
    }

    private void Cancel()
    {
        if (_console.IsCanvasActive) _surfaceKind = "canvas";
        else if (_console.IsScreenOutputActive) _surfaceKind = "screen";
        _cancel?.Cancel();
        if (_console.IsWaitingForInput) _console.CancelPendingInput();
        _inputRequestId = null;
        if (_console.IsCanvasActive) _console.DeactivateScreen();
        if (_debugger is not null && !_working)
        {
            _debugger = null;
            _console.EndSession(); _console.DeactivateScreen();
            Event("execution.finished", new { status = "Cancelled", outputText = _console.OutputText, outputLineCount = _console.OutputCheckpoint, actualEngine = "BytecodeVm", diagnostics = Array.Empty<object>() });
            Finish();
        }
    }

    public void Shutdown()
    {
        if (_shutdown) return;
        _shutdown = true; Cancel();
        if (!_working) { _console.DeactivateScreen(); _application.Shutdown(); }
    }

    private static string? String(JsonElement element, string property) => element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private void Respond(string id, object payload) => Write(new { protocolVersion = 1, kind = "response", id, ok = true, payload });
    private void Error(string? id, string code, string message) => Write(new { protocolVersion = 1, kind = "response", id, ok = false, error = new { code, message } });
    private void Event(string name, object payload) => Write(new { protocolVersion = 1, kind = "event", @event = name,
        sessionId = _sessionId, sequence = (++_sequence).ToString(), payload });
    private void Write(object message)
    {
        string json = JsonSerializer.Serialize(message, _json);
        lock (_writeLock) { Console.Out.WriteLine(json); Console.Out.Flush(); }
    }
    public void Dispose() { _outputTimer.Stop(); _frames?.Dispose(); }
    private sealed class HostError(string code, string message) : Exception(message) { public string Code { get; } = code; }
}
