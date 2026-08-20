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
using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Сервис графики: SCREEN, canvas, рисование, пиксели, палитра, спрайты, тайлсеты.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class GraphicsHostService : IGraphicsHost
{
    private static readonly TimeSpan GraphicsFrameCoalescingInterval = TimeSpan.FromMilliseconds(16);
    private const long GraphicsFrameSummaryInterval = 120;
    private const int ScreenTextCellColumns = 80;
    private const int ScreenTextCellRows = 25;

    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly IScreenModeSizeProvider _screenModeSizeProvider;
    private readonly InputStateService _inputState;
    private readonly ConsoleOutputService _consoleOutput;
    private readonly InputPromptService _inputPrompt;
    private readonly Action<string> _appendTranscriptLine;

    // Screen state
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

    // Text rendering
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

    // Drawing state
    private long _graphicsCurrentX;
    private long _graphicsCurrentY;

    // Viewport
    private bool _viewportEnabled;
    private bool _viewportRelativeCoordinates;
    private long _viewportLeft;
    private long _viewportTop;
    private long _viewportRight = -1;
    private long _viewportBottom = -1;

    // Window
    private bool _windowEnabled;
    private bool _windowUsesScreenCoordinates;
    private double _windowLeft;
    private double _windowTop;
    private double _windowRight;
    private double _windowBottom;

    // Graphics input
    private int _graphicsInputRequestCount;

    // Framebuffer
    private ScreenFramebuffer? _framebuffer;
    private ScreenFramebuffer?[] _screenPages = new ScreenFramebuffer?[4];
    private ScreenFramebufferSnapshot? _graphicsFrame;
    private bool _graphicsVisualRefreshPending;
    private DispatcherTimer? _graphicsVisualRefreshTimer;
    private NativeGraphicsScreenWindow? _graphicsWindow;
    private bool _canvasFrameInProgress;
    private int _graphicsBatchDepth;
    private bool _graphicsBatchHasChanges;

    // Metrics
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

    public GraphicsHostService(
        IIdeTraceService traceService,
        IIdeEventLogService eventLogService,
        IScreenModeSizeProvider screenModeSizeProvider,
        InputStateService inputState,
        ConsoleOutputService consoleOutput,
        InputPromptService inputPrompt,
        Action<string> appendTranscriptLine)
    {
        _traceService = traceService;
        _eventLogService = eventLogService;
        _screenModeSizeProvider = screenModeSizeProvider;
        _inputState = inputState;
        _consoleOutput = consoleOutput;
        _inputPrompt = inputPrompt;
        _appendTranscriptLine = appendTranscriptLine;
    }

    #region IGraphicsHost Properties

    public bool IsScreenActive => _isScreenActive;
    public bool IsCanvasActive => _isCanvasActive;
    public bool IsScreenOutputActive => _isScreenOutputActive;
    public int ScreenMode => _screenMode;
    public int ActiveScreenPage => _activeScreenPage;
    public int VisualScreenPage => _visualScreenPage;
    public int ScreenWidth => _screenWidth;
    public int ScreenHeight => _screenHeight;
    public string GraphicsText => _graphicsText;
    public ScreenFramebufferSnapshot? GraphicsFrame => _graphicsFrame;
    public IntPtr ScreenWindowHandle => _graphicsWindow?.Handle ?? IntPtr.Zero;

    public int CurrentForegroundColor => _graphicsForeground;
    public int CurrentBackgroundColor => _graphicsBackground ?? _graphicsLastBackgroundColor;
    public bool IsTextBackgroundTransparent => _graphicsBackground is null;
    public long CurrentGraphicsX => _graphicsCurrentX;
    public long CurrentGraphicsY => _graphicsCurrentY;
    public long CanvasCameraX => _canvasCameraX;
    public long CanvasCameraY => _canvasCameraY;

    public long CurrentViewportX => _viewportEnabled ? _viewportLeft : 0;
    public long CurrentViewportY => _viewportEnabled ? _viewportTop : 0;
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

    #endregion

    // The actual graphics methods will be moved here from ProgramConsoleService.Graphics.cs
    // This is a placeholder - the full implementation will be added in the next step
}
