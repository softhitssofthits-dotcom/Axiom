using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.ViewModels;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public sealed partial class ProgramConsoleService : ViewModelBase, IProgramConsoleService, IRuntimeCancellationProvider
{
    private static readonly TimeSpan GraphicsFrameCoalescingInterval = TimeSpan.FromMilliseconds(16);
    private const long GraphicsFrameSummaryInterval = 120;
    private const int ScreenTextCellColumns = 80;
    private const int ScreenTextCellRows = 25;

    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly IScreenModeSizeProvider _screenModeSizeProvider;
    private bool _isScreenActive;
    private bool _isCanvasActive;
    private bool _isScreenOutputActive;
    private long _canvasCameraX;
    private long _canvasCameraY;
    private int _screenMode;
    private int _screenWidth;
    private int _screenHeight;
    private int _activeScreenPage;
    private int _visualScreenPage;
    private string _graphicsText = string.Empty;
    private readonly ScreenTextBuffer _graphicsTextBuffer = new();
    private readonly StringBuilder _pendingGraphicsTraceLine = new();
    private int _graphicsForeground = 15;
    private int? _graphicsBackground;
    private int _graphicsLastBackgroundColor;
    private int _textViewportTop = 1;
    private int _textViewportBottom = ScreenTextCellRows;
    private bool _textViewportExplicit;
    private int _screenTextFirstVisibleRow = 1;
    private int _graphicsRow = 1;
    private int _graphicsColumn = 1;
    private bool _pendingGraphicsViewportScroll;
    private long _graphicsCurrentX;
    private long _graphicsCurrentY;
    private bool _viewportEnabled;
    private bool _viewportRelativeCoordinates;
    private long _viewportLeft;
    private long _viewportTop;
    private long _viewportRight = -1;
    private long _viewportBottom = -1;
    private bool _windowEnabled;
    private bool _windowUsesScreenCoordinates;
    private double _windowLeft;
    private double _windowTop;
    private double _windowRight;
    private double _windowBottom;
    private int _graphicsInputRequestCount;
    private ScreenFramebuffer? _framebuffer;
    private ScreenFramebuffer?[] _screenPages = new ScreenFramebuffer?[4];
    private ScreenFramebufferSnapshot? _graphicsFrame;
    private bool _graphicsVisualRefreshPending;
    private DispatcherTimer? _graphicsVisualRefreshTimer;
    private NativeGraphicsScreenWindow? _graphicsWindow;
    private bool _canvasFrameInProgress;
    private int _graphicsBatchDepth;
    private bool _graphicsBatchHasChanges;
    private DateTimeOffset? _graphicsFrameMetricsStartedAt;
    private DateTimeOffset? _lastGraphicsFramePublishedAt;
    private long _graphicsFramePublishCount;
    private long _graphicsImmediateFramePublishCount;
    private long _graphicsCoalescedFramePublishCount;
    private long _graphicsQueuedCommandCount;
    private long _graphicsQueuedCommandsSinceLastFrame;
    private long _graphicsMaxCommandsPerFrame;
    private double _graphicsTotalFrameIntervalMs;
    private double _graphicsMaxFrameIntervalMs;
    private long _graphicsKeysEnqueued;
    private long _graphicsKeysRead;
    private long _graphicsMouseEventsEnqueued;
    private long _graphicsMouseEventsRead;
    private long _graphicsMouseMoveEnqueued;
    private long _graphicsMousePressEnqueued;
    private long _graphicsMouseReleaseEnqueued;
    private long _graphicsMouseWheelEnqueued;
    private long _graphicsMouseMoveRead;
    private long _graphicsMousePressRead;
    private long _graphicsMouseReleaseRead;
    private long _graphicsMouseWheelRead;
    private long _graphicsMaxPendingKeys;
    private long _graphicsMaxPendingMouseEvents;
    private long _graphicsInputEventsAtLastSummary;
    private readonly SessionManager _sessionManager;
    private readonly ConsoleOutputService _consoleOutput;
    private readonly InputStateService _inputState;
    private readonly InputPromptService _inputPrompt;

    public ProgramConsoleService(
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null,
        IScreenModeSizeProvider? screenModeSizeProvider = null)
    {
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
        _screenModeSizeProvider = screenModeSizeProvider ?? ScreenModeSizeProvider.Instance;
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
    }

    /// <summary>
    /// РњРµРЅРµРґР¶РµСЂ СЃРµСЃСЃРёР№ вЂ” РґРѕСЃС‚СѓРїРµРЅ РґР»СЏ РІРЅРµС€РЅРёС… СЃРµСЂРІРёСЃРѕРІ.
    /// </summary>
    public SessionManager SessionManager => _sessionManager;

    /// <summary>
    /// РЎРµСЂРІРёСЃ РєРѕРЅСЃРѕР»СЊРЅРѕРіРѕ РІС‹РІРѕРґР° вЂ” РґРѕСЃС‚СѓРїРµРЅ РґР»СЏ РІРЅРµС€РЅРёС… СЃРµСЂРІРёСЃРѕРІ.
    /// </summary>
    public ConsoleOutputService ConsoleOutput => _consoleOutput;

    /// <summary>
    /// РЎРµСЂРІРёСЃ СЃРѕСЃС‚РѕСЏРЅРёСЏ РІРІРѕРґР° вЂ” РґРѕСЃС‚СѓРїРµРЅ РґР»СЏ РІРЅРµС€РЅРёС… СЃРµСЂРІРёСЃРѕРІ.
    /// </summary>
    public InputStateService InputState => _inputState;

    /// <summary>
    /// РЎРµСЂРІРёСЃ РІРІРѕРґР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ вЂ” РґРѕСЃС‚СѓРїРµРЅ РґР»СЏ РІРЅРµС€РЅРёС… СЃРµСЂРІРёСЃРѕРІ.
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
                PublishGraphicsFrame();
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
                PublishGraphicsFrame();
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
                PublishGraphicsFrame();
            }
        }
    }

    public string OutputText
    {
        get => _consoleOutput.OutputText;
        set => _consoleOutput.OutputText = value;
    }

    public string GraphicsText
    {
        get => _graphicsText;
        private set => SetProperty(ref _graphicsText, value);
    }

    public ScreenFramebufferSnapshot? GraphicsFrame
    {
        get => _graphicsFrame;
        private set => SetProperty(ref _graphicsFrame, value);
    }

    public IntPtr ScreenWindowHandle => _graphicsWindow?.Hwnd ?? IntPtr.Zero;

    public bool IsScreenActive => _inputState.Snapshot.IsScreenActive;

    public bool IsCanvasActive => _isCanvasActive;

    public bool IsScreenOutputActive => _inputState.Snapshot.IsScreenOutputActive;

    public int ScreenMode => _inputState.Snapshot.ScreenMode;

    public int ActiveScreenPage => _activeScreenPage;

    public int VisualScreenPage => _visualScreenPage;

    public int ScreenWidth => _inputState.Snapshot.ScreenWidth;

    public int ScreenHeight => _inputState.Snapshot.ScreenHeight;

    public bool TryGetScreenTextScrollState(out ScreenTextScrollState state)
    {
        if (!IsScreenZeroMode)
        {
            state = default;
            return false;
        }

        var totalRows = Math.Max(1, _graphicsTextBuffer.LineCount);
        state = new ScreenTextScrollState(_screenTextFirstVisibleRow, ScreenTextCellRows, totalRows);
        return true;
    }

    public void SetScreenTextFirstVisibleRow(int firstVisibleRow)
    {
        InvokeOnDispatcher(() =>
        {
            if (!TryGetScreenTextScrollState(out var state))
            {
                return;
            }

            var normalizedFirstVisibleRow = state.NormalizeFirstVisibleRow(firstVisibleRow);
            if (_screenTextFirstVisibleRow == normalizedFirstVisibleRow)
            {
                return;
            }

            _screenTextFirstVisibleRow = normalizedFirstVisibleRow;
            PublishGraphicsFrame();
        });
    }

    public bool SetTextViewport(int topRow, int bottomRow)
    {
        var applied = false;
        InvokeOnDispatcher(() => applied = TrySetTextViewportCore(topRow, bottomRow));
        return applied;
    }

    public void ResetTextViewport()
    {
        InvokeOnDispatcher(() =>
        {
            ResetTextViewportCore();
            PublishGraphicsFrame();
        });
    }

    public int CurrentForegroundColor => _graphicsForeground;

    public int CurrentBackgroundColor => _graphicsLastBackgroundColor;

    public bool IsTextBackgroundTransparent => !_graphicsBackground.HasValue;

    public long CurrentGraphicsX => _graphicsCurrentX;

    public long CurrentGraphicsY => _graphicsCurrentY;

    public long CanvasCameraX => _canvasCameraX;

    public long CanvasCameraY => _canvasCameraY;

    public bool ViewportEnabled => _viewportEnabled;

    public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

    public long ViewportLeft => _viewportLeft;

    public long ViewportTop => _viewportTop;

    public long ViewportRight => _viewportRight;

    public long ViewportBottom => _viewportBottom;

    public bool WindowEnabled => _windowEnabled;

    public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

    public double WindowLeft => _windowLeft;

    public double WindowTop => _windowTop;

    public double WindowRight => _windowRight;

    public double WindowBottom => _windowBottom;

    public int OutputCheckpoint => _consoleOutput.OutputCheckpoint;

    public bool IsCancellationRequested
    {
        get => _sessionManager.IsCancellationRequested;
    }

    public void BeginSession()
    {
        InvokeOnDispatcher(ResetSessionState);
        _sessionManager.BeginSession(() => { }); // reset already done above
    }

    public void EndSession()
    {
        _sessionManager.EndSession();
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
                WriteGraphicsTextCore(text, newLine: true);
                PublishGraphicsFrame();
            }
            else
            {
                AppendTranscriptLine(text);
            }
        });
    }

}
