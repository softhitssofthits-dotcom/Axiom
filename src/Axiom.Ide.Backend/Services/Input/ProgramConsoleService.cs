using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Graphics;
using Axiom.Ide.ViewModels;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public sealed partial class ProgramConsoleService : ViewModelBase, IProgramConsoleService, IRuntimeCancellationProvider, IGraphicsWindowInputSink
{
    private readonly IIdeTraceService _traceService;
    private readonly Dispatcher _dispatcher;
    private readonly IIdeEventLogService _eventLogService;
    private readonly SessionManager _sessionManager;
    private readonly ConsoleOutputService _consoleOutput;
    private readonly InputStateService _inputState;
    private readonly InputPromptService _inputPrompt;
    private readonly GraphicsHostService _graphicsHostService;

    public ProgramConsoleService(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null,
        IScreenModeSizeProvider? screenModeSizeProvider = null,
        Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
        _sessionManager = new SessionManager(traceService, eventLogService);
        _consoleOutput = new ConsoleOutputService(traceService, eventLogService);
        _consoleOutput.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConsoleOutputService.OutputText))
            {
                OnPropertyChanged(nameof(OutputText));
            }
        };
        _inputState = new InputStateService();
        _inputPrompt = new InputPromptService(traceService, eventLogService);
        _inputPrompt.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InputPromptService.IsWaitingForInput))
            {
                OnPropertyChanged(nameof(IsWaitingForInput));
            }
            else if (e.PropertyName == nameof(InputPromptService.CurrentPromptText))
            {
                OnPropertyChanged(nameof(CurrentPromptText));
            }
            else if (e.PropertyName == nameof(InputPromptService.CurrentInputText))
            {
                OnPropertyChanged(nameof(CurrentInputText));
            }
        };
        _graphicsHostService = new GraphicsHostService(
            _inputState,
            _inputPrompt,
            _sessionManager,
            (action, priority) => InvokeOnDispatcher(action, priority),
            () => ResourceBaseDirectory,
            () => this,
            traceService,
            eventLogService,
            screenModeSizeProvider);
        _graphicsHostService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(GraphicsHostService.GraphicsText)
                or nameof(GraphicsHostService.GraphicsFrame)
                or nameof(GraphicsHostService.ScreenWindowHandle)
                or nameof(GraphicsHostService.IsScreenActive)
                or nameof(GraphicsHostService.IsCanvasActive)
                or nameof(GraphicsHostService.IsScreenOutputActive)
                or nameof(GraphicsHostService.ScreenMode)
                or nameof(GraphicsHostService.ScreenWidth)
                or nameof(GraphicsHostService.ScreenHeight))
            {
                OnPropertyChanged(e.PropertyName);
            }
        };
    }

    /// <summary>
    /// Графический хост — доступен для внешних сервисов.
    /// </summary>
    public GraphicsHostService GraphicsHost => _graphicsHostService;

    /// <summary>
    /// Менеджер сессий — доступен для внешних сервисов.
    /// </summary>
    public SessionManager SessionManager => _sessionManager;

    /// <summary>
    /// Сервис консольного вывода — доступен для внешних сервисов.
    /// </summary>
    public ConsoleOutputService ConsoleOutput => _consoleOutput;

    /// <summary>
    /// Сервис состояния ввода — доступен для внешних сервисов.
    /// </summary>
    public InputStateService InputState => _inputState;

    /// <summary>
    /// Сервис ввода пользователя — доступен для внешних сервисов.
    /// </summary>
    public InputPromptService InputPrompt => _inputPrompt;

    public string CurrentInputText
    {
        get => _inputPrompt.CurrentInputText;
        set
        {
            _inputPrompt.CurrentInputText = value;
            if (_inputState.Snapshot.IsScreenOutputActive && _inputPrompt.IsWaitingForInput)
            {
                _graphicsHostService.PublishFrame();
            }
        }
    }

    public string? ResourceBaseDirectory { get; set; }

    public string CurrentPromptText
    {
        get => _inputPrompt.CurrentPromptText;
        set
        {
            _inputPrompt.CurrentPromptText = value;
            if (_inputState.Snapshot.IsScreenOutputActive)
            {
                _graphicsHostService.PublishFrame();
            }
        }
    }

    public bool IsWaitingForInput
    {
        get => _inputPrompt.IsWaitingForInput;
        set
        {
            _inputPrompt.IsWaitingForInput = value;
            if (_inputState.Snapshot.IsScreenOutputActive)
            {
                _graphicsHostService.PublishFrame();
            }
        }
    }

    public string OutputText
    {
        get => _consoleOutput.OutputText;
        set => _consoleOutput.OutputText = value;
    }

    #region IGraphicsHost делегирование в GraphicsHostService

    public string GraphicsText => _graphicsHostService.GraphicsText;

    public ScreenFramebufferSnapshot? GraphicsFrame => _graphicsHostService.GraphicsFrame;

    public IntPtr ScreenWindowHandle => _graphicsHostService.ScreenWindowHandle;

    public bool IsScreenActive => _graphicsHostService.IsScreenActive;

    public bool IsCanvasActive => _graphicsHostService.IsCanvasActive;

    public bool IsScreenOutputActive => _graphicsHostService.IsScreenOutputActive;

    public int ScreenMode => _graphicsHostService.ScreenMode;

    public int ActiveScreenPage => _graphicsHostService.ActiveScreenPage;

    public int VisualScreenPage => _graphicsHostService.VisualScreenPage;

    public int ScreenWidth => _graphicsHostService.ScreenWidth;

    public int ScreenHeight => _graphicsHostService.ScreenHeight;

    public int CurrentForegroundColor => _graphicsHostService.CurrentForegroundColor;

    public int CurrentBackgroundColor => _graphicsHostService.CurrentBackgroundColor;

    public bool IsTextBackgroundTransparent => _graphicsHostService.IsTextBackgroundTransparent;

    public long CurrentGraphicsX => _graphicsHostService.CurrentGraphicsX;

    public long CurrentGraphicsY => _graphicsHostService.CurrentGraphicsY;

    public long CanvasCameraX => _graphicsHostService.CanvasCameraX;

    public long CanvasCameraY => _graphicsHostService.CanvasCameraY;

    public bool ViewportEnabled => _graphicsHostService.ViewportEnabled;

    public bool ViewportRelativeCoordinates => _graphicsHostService.ViewportRelativeCoordinates;

    public long ViewportLeft => _graphicsHostService.ViewportLeft;

    public long ViewportTop => _graphicsHostService.ViewportTop;

    public long ViewportRight => _graphicsHostService.ViewportRight;

    public long ViewportBottom => _graphicsHostService.ViewportBottom;

    public bool WindowEnabled => _graphicsHostService.WindowEnabled;

    public bool WindowUsesScreenCoordinates => _graphicsHostService.WindowUsesScreenCoordinates;

    public double WindowLeft => _graphicsHostService.WindowLeft;

    public double WindowTop => _graphicsHostService.WindowTop;

    public double WindowRight => _graphicsHostService.WindowRight;

    public double WindowBottom => _graphicsHostService.WindowBottom;

    public bool TryGetScreenTextScrollState(out ScreenTextScrollState state) =>
        _graphicsHostService.TryGetScreenTextScrollState(out state);

    public void SetScreenTextFirstVisibleRow(int firstVisibleRow) =>
        _graphicsHostService.SetScreenTextFirstVisibleRow(firstVisibleRow);

    public bool SetTextViewport(int topRow, int bottomRow) =>
        _graphicsHostService.SetTextViewport(topRow, bottomRow);

    public void ResetTextViewport() =>
        _graphicsHostService.ResetTextViewport();

    public void ActivateScreen(int mode) =>
        _graphicsHostService.ActivateScreen(mode);

    public void ActivateScreen(int mode, int activePage, int visualPage) =>
        _graphicsHostService.ActivateScreen(mode, activePage, visualPage);

    public void ActivateCanvas(long width, long height) =>
        _graphicsHostService.ActivateCanvas(width, height);

    public void SetGraphicsSmoothing(bool enabled) =>
        _graphicsHostService.SetGraphicsSmoothing(enabled);

    public void DeactivateScreen() =>
        _graphicsHostService.DeactivateScreen();

    public void BeginFrame() =>
        _graphicsHostService.BeginFrame();

    public void PresentFrame() =>
        _graphicsHostService.PresentFrame();

    public void ClearFrame(int color) =>
        _graphicsHostService.ClearFrame(color);

    public void SetCanvasCamera(long x, long y) =>
        _graphicsHostService.SetCanvasCamera(x, y);

    public void ResetCanvasCamera() =>
        _graphicsHostService.ResetCanvasCamera();

    public bool CopyScreenPage(int sourcePage, int destinationPage) =>
        _graphicsHostService.CopyScreenPage(sourcePage, destinationPage);

    public void ClearGraphics() =>
        _graphicsHostService.ClearGraphics();

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0) =>
        _graphicsHostService.SetTextColor(foreground, backgroundMode, background);

    public void LocateText(int row, int column) =>
        _graphicsHostService.LocateText(row, column);

    public void ResetPalette() =>
        _graphicsHostService.ResetPalette();

    public void SetPaletteColor(int index, int color) =>
        _graphicsHostService.SetPaletteColor(index, color);

    public RuntimeImageValue? LoadImage(string path) =>
        _graphicsHostService.LoadImage(path);

    public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight) =>
        _graphicsHostService.LoadSprite(path, frameWidth, frameHeight);

    public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0) =>
        _graphicsHostService.LoadTileset(path, tileWidth, tileHeight, margin, spacing);

    public RuntimeTilemapValue? LoadTilemap(string path) =>
        _graphicsHostService.LoadTilemap(path);

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height) =>
        _graphicsHostService.DrawImage(image, x, y, width, height);

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height) =>
        _graphicsHostService.DrawSprite(sprite, frame, x, y, width, height);

    public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height) =>
        _graphicsHostService.DrawImageRegion(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height);

    public void WriteGraphicsText(string text, bool newLine) =>
        _graphicsHostService.WriteGraphicsText(text, newLine);

    public string ReadGraphicsInput(InputRequest request) =>
        _graphicsHostService.ReadGraphicsInput(request);

    public void SetPixel(long x, long y, int color) =>
        _graphicsHostService.SetPixel(x, y, color);

    public int GetPixel(long x, long y) =>
        _graphicsHostService.GetPixel(x, y);

    public void SetAbsolutePixel(long x, long y, int color) =>
        _graphicsHostService.SetAbsolutePixel(x, y, color);

    public int GetAbsolutePixel(long x, long y) =>
        _graphicsHostService.GetAbsolutePixel(x, y);

    public int[]? CaptureAbsolutePixels(long x, long y, int width, int height) =>
        _graphicsHostService.CaptureAbsolutePixels(x, y, width, height);

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind) =>
        _graphicsHostService.DrawLine(x1, y1, x2, y2, color, kind);

    public void DrawCircle(long x, long y, double radius, int color, bool relativeCenter, double? startAngle, double? endAngle, double aspect, bool drawStartRadius, bool drawEndRadius) =>
        _graphicsHostService.DrawCircle(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);

    public void DrawCircle(long x, long y, long radius, int color) =>
        _graphicsHostService.DrawCircle(x, y, radius, color);

    public void SetGraphicsCurrentPosition(long x, long y) =>
        _graphicsHostService.SetGraphicsCurrentPosition(x, y);

    public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates) =>
        _graphicsHostService.SetViewport(left, top, right, bottom, fillColor, borderColor, useScreenCoordinates);

    public void ResetViewport() =>
        _graphicsHostService.ResetViewport();

    public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates) =>
        _graphicsHostService.SetWindow(left, top, right, bottom, useScreenCoordinates);

    public void ResetWindow() =>
        _graphicsHostService.ResetWindow();

    public void Paint(GraphicsPaintRequest request) =>
        _graphicsHostService.Paint(request);

    public void Paint(long x, long y, int fillColor, int borderColor) =>
        _graphicsHostService.Paint(x, y, fillColor, borderColor);

    public void DrawRect(long x, long y, long width, long height, int color) =>
        _graphicsHostService.DrawRect(x, y, width, height, color);

    public void FillRect(long x, long y, long width, long height, int color) =>
        _graphicsHostService.FillRect(x, y, width, height, color);

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color) =>
        _graphicsHostService.DrawCanvasLine(x1, y1, x2, y2, color);

    public void DrawCanvasCircle(long x, long y, long radius, int color) =>
        _graphicsHostService.DrawCanvasCircle(x, y, radius, color);

    public void FillCanvasCircle(long x, long y, long radius, int color) =>
        _graphicsHostService.FillCanvasCircle(x, y, radius, color);

    public void DrawCanvasText(string text, long x, long y, int color) =>
        _graphicsHostService.DrawCanvasText(text, x, y, color);

    #endregion

    #region IGraphicsWindowInputSink делегирование в InputStateService/InputPromptService

    bool IGraphicsWindowInputSink.IsWaitingForInput => _inputPrompt.IsWaitingForInput;

    void IGraphicsWindowInputSink.EnqueueKey(string keyText)
    {
        _inputState.EnqueueKey(keyText);
    }

    void IGraphicsWindowInputSink.SetKeyDown(string keyText)
    {
        _inputState.SetKeyDown(keyText);
    }

    void IGraphicsWindowInputSink.SetKeyUp(string keyText)
    {
        _inputState.SetKeyUp(keyText);
    }

    void IGraphicsWindowInputSink.SubmitInput()
    {
        _inputPrompt.SubmitInput(HandleInputSubmit, _inputState.Snapshot.IsScreenOutputActive);
    }

    void IGraphicsWindowInputSink.BackspaceInputCharacter()
    {
        _inputPrompt.BackspaceInputCharacter();
    }

    void IGraphicsWindowInputSink.AppendInputCharacter(char character)
    {
        _inputPrompt.AppendInputCharacter(character);
    }

    void IGraphicsWindowInputSink.EnqueueMouseButtonPress(int buttonNumber)
    {
        _inputState.EnqueueMouseButtonPress(buttonNumber);
    }

    void IGraphicsWindowInputSink.SetMouseButtonDown(int buttonNumber)
    {
        _inputState.SetMouseButtonDown(buttonNumber);
    }

    void IGraphicsWindowInputSink.EnqueueMouseButtonRelease(int buttonNumber)
    {
        _inputState.EnqueueMouseButtonRelease(buttonNumber);
    }

    void IGraphicsWindowInputSink.SetMouseButtonUp(int buttonNumber)
    {
        _inputState.SetMouseButtonUp(buttonNumber);
    }

    void IGraphicsWindowInputSink.EnqueueMouseMove()
    {
        _inputState.EnqueueMouseMove();
    }

    void IGraphicsWindowInputSink.EnqueueMouseWheel(int delta)
    {
        _inputState.EnqueueMouseWheel(delta);
    }

    void IGraphicsWindowInputSink.SetMousePosition(long x, long y)
    {
        _inputState.SetMousePosition(x, y);
    }

    #endregion

    #region Keyboard/Mouse input (IKeyboardInputProvider, IMouseInputProvider)

    public void EnqueueKey(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputState.EnqueueKey(keyText);
        _graphicsHostService.RecordKeyEnqueued();
    }

    public void SetKeyDown(string keyText)
    {
        _inputState.SetKeyDown(keyText);
    }

    public void SetKeyUp(string keyText)
    {
        _inputState.SetKeyUp(keyText);
    }

    public string ReadKey()
    {
        var keyText = _inputState.ReadKey();
        if (keyText.Length > 0)
        {
            _graphicsHostService.RecordKeyRead();
        }

        return keyText;
    }

    public bool IsKeyDown(string keyText)
    {
        return _inputState.IsKeyDown(keyText);
    }

    public void ClearKeyState()
    {
        _inputState.ClearKeyState();
    }

    public void SetMousePosition(long x, long y)
    {
        _inputState.SetMousePosition(x, y);
    }

    public void SetMouseButtonDown(int buttonNumber)
    {
        _inputState.SetMouseButtonDown(buttonNumber);
    }

    public void EnqueueMouseButtonPress(int buttonNumber)
    {
        _inputState.EnqueueMouseButtonPress(buttonNumber);
        _graphicsHostService.RecordMouseEventEnqueued(MouseInputEventKind.Press);
    }

    public void EnqueueMouseButtonRelease(int buttonNumber)
    {
        _inputState.EnqueueMouseButtonRelease(buttonNumber);
        _graphicsHostService.RecordMouseEventEnqueued(MouseInputEventKind.Release);
    }

    public void EnqueueMouseMove()
    {
        _inputState.EnqueueMouseMove();
        _graphicsHostService.RecordMouseEventEnqueued(MouseInputEventKind.Move);
    }

    public void EnqueueMouseWheel(int delta)
    {
        _inputState.EnqueueMouseWheel(delta);
        _graphicsHostService.RecordMouseEventEnqueued(MouseInputEventKind.Wheel);
    }

    public void SetMouseButtonUp(int buttonNumber)
    {
        _inputState.SetMouseButtonUp(buttonNumber);
    }

    public long GetMouseX()
    {
        return _inputState.GetMouseX();
    }

    public long GetMouseY()
    {
        return _inputState.GetMouseY();
    }

    public bool IsMouseButtonDown(int buttonNumber)
    {
        return _inputState.IsMouseButtonDown(buttonNumber);
    }

    public int ReadMouseButtonPress()
    {
        return _inputState.ReadMouseButtonPress();
    }

    public MouseInputEvent ReadMouseEvent()
    {
        var mouseEvent = _inputState.ReadMouseEvent();
        if (mouseEvent.Kind != MouseInputEventKind.None)
        {
            _graphicsHostService.RecordMouseEventRead(mouseEvent.Kind);
        }

        return mouseEvent;
    }

    public void ClearMouseState()
    {
        _inputState.ClearMouseState();
    }

    #endregion

    #region Session/Output

    public int OutputCheckpoint => _consoleOutput.OutputCheckpoint;

    public bool IsCancellationRequested
    {
        get => _sessionManager.IsCancellationRequested;
    }

    public void BeginSession()
    {
        InvokeOnDispatcher(ResetSessionState);
        _sessionManager.BeginSession(() => { });
    }

    public void EndSession()
    {
        _sessionManager.EndSession();
    }

    public void CancelPendingInput()
    {
        InvokeOnDispatcher(() =>
        {
            _sessionManager.IsCancellationRequested = true;
            _inputPrompt.CancelPendingFrame();
            _inputPrompt.ResetInputState();
        });
    }

    public void Clear()
    {
        InvokeOnDispatcher(ResetSessionState);
    }

    public void ClearOutput()
    {
        InvokeOnDispatcher(ClearOutputCore);
    }

    public string RequestInput(ProgramInputRequest request)
    {
        return _inputPrompt.RequestInput(request, HandleInputSubmit, _inputState.Snapshot.IsScreenOutputActive, OnInputWaitingChanged);
    }

    public void SubmitInput()
    {
        InvokeOnDispatcher(() => _inputPrompt.SubmitInput(HandleInputSubmit, _inputState.Snapshot.IsScreenOutputActive));
    }

    public void AppendInputCharacter(char character)
    {
        InvokeOnDispatcher(() => _inputPrompt.AppendInputCharacter(character));
    }

    public void BackspaceInputCharacter()
    {
        InvokeOnDispatcher(() => _inputPrompt.BackspaceInputCharacter());
    }

    public void WriteOutput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() =>
        {
            if (_inputState.Snapshot.IsScreenOutputActive)
            {
                _graphicsHostService.WriteGraphicsTextForOutput(text, newLine: true);
            }
            else
            {
                _consoleOutput.AppendTranscriptLine(text);
            }
        });
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        InvokeOnDispatcher(() => _consoleOutput.RestoreOutputCheckpoint(checkpoint));
    }

    #endregion

    #region Private methods

    private void HandleInputSubmit(string promptText, string inputText, bool isScreenOutputActive)
    {
        if (isScreenOutputActive)
        {
            _graphicsHostService.WriteGraphicsTextForOutput(promptText + inputText, newLine: true);
        }
        else
        {
            _consoleOutput.AppendTranscriptLine(promptText + inputText);
        }
    }

    private void OnInputWaitingChanged()
    {
        if (_inputState.Snapshot.IsScreenOutputActive)
        {
            _graphicsHostService.PublishFrame();
        }
    }

    private void ResetSessionState()
    {
        _inputPrompt.CancelPendingFrame();
        _sessionManager.Reset(() => { });
        _inputState.Reset();
        _graphicsHostService.ResetGraphicsState();
        _consoleOutput.Reset();
        _inputPrompt.ResetInputState();
        OnPropertyChanged(nameof(IsScreenActive));
        OnPropertyChanged(nameof(IsCanvasActive));
        OnPropertyChanged(nameof(IsScreenOutputActive));
        OnPropertyChanged(nameof(ScreenMode));
        OnPropertyChanged(nameof(ScreenWidth));
        OnPropertyChanged(nameof(ScreenHeight));
    }

    private void ClearOutputCore()
    {
        _consoleOutput.ClearOutput();
    }

    private void InvokeOnDispatcher(Action action, DispatcherPriority priority = DispatcherPriority.Send)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = _dispatcher;

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, priority);
    }

    #endregion

}
