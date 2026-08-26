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
using Axiom.Ide.Services.Input;
using Axiom.Ide.ViewModels;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Graphics;

/// <summary>
/// Управляет всей графикой: SCREEN, CANVAS, drawing primitives, текст, изображения, палитра, viewport, window, frame publishing, metrics.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class GraphicsHostService : ViewModelBase, IGraphicsHost
{
    private static readonly TimeSpan GraphicsFrameCoalescingInterval = TimeSpan.FromMilliseconds(16);
    private const long GraphicsFrameSummaryInterval = 120;
    private const int ScreenTextCellColumns = 80;
    private const int ScreenTextCellRows = 25;

    private readonly IIdeTraceService _traceService;
    private readonly IIdeEventLogService _eventLogService;
    private readonly IScreenModeSizeProvider _screenModeSizeProvider;
    private readonly InputStateService _inputState;
    private readonly InputPromptService _inputPrompt;
    private readonly SessionManager _sessionManager;
    private readonly Action<Action, DispatcherPriority> _invokeOnDispatcher;
    private readonly Func<string?> _getResourceBaseDirectory;
    private readonly Func<IGraphicsWindowInputSink> _getInputSink;

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

    public GraphicsHostService(
        InputStateService inputState,
        InputPromptService inputPrompt,
        SessionManager sessionManager,
        Action<Action, DispatcherPriority> invokeOnDispatcher,
        Func<string?> getResourceBaseDirectory,
        Func<IGraphicsWindowInputSink> getInputSink,
        IIdeTraceService? traceService = null,
        IIdeEventLogService? eventLogService = null,
        IScreenModeSizeProvider? screenModeSizeProvider = null)
    {
        _inputState = inputState;
        _inputPrompt = inputPrompt;
        _sessionManager = sessionManager;
        _invokeOnDispatcher = invokeOnDispatcher;
        _getResourceBaseDirectory = getResourceBaseDirectory;
        _getInputSink = getInputSink;
        _traceService = traceService ?? NullIdeTraceService.Instance;
        _eventLogService = eventLogService ?? NullIdeEventLogService.Instance;
        _screenModeSizeProvider = screenModeSizeProvider ?? ScreenModeSizeProvider.Instance;
    }

    #region Свойства IGraphicsHost

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

    #endregion

    #region Свойства для фасада

    public string? ResourceBaseDirectory
    {
        get => _getResourceBaseDirectory();
    }

    #endregion

    #region Screen/Canvas lifecycle

    public void ActivateScreen(int mode)
    {
        ActivateScreen(mode, activePage: 0, visualPage: 0);
    }

    public void ActivateScreen(int mode, int activePage, int visualPage)
    {
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsFrameSummary(final: true);
            WriteGraphicsInputSummary(final: true);

            var (width, height) = _screenModeSizeProvider.GetSize(mode);
            if (activePage is < 0 or > 3 || visualPage is < 0 or > 3)
            {
                return;
            }

            var preservePages = _screenMode == mode && _screenWidth == width && _screenHeight == height && _screenPages.Length == 4;
            _isScreenActive = mode != 0;
            _isCanvasActive = false;
            _isScreenOutputActive = true;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _screenMode = mode;
            _screenWidth = width;
            _screenHeight = height;
            _activeScreenPage = _isScreenActive ? activePage : 0;
            _visualScreenPage = _isScreenActive ? visualPage : 0;
            _inputState.SetScreenState(_isScreenActive, _isScreenOutputActive, _screenMode, _screenWidth, _screenHeight);
            ResetViewportCore();
            ResetWindowCore();
            if (!_isScreenActive || !preservePages)
            {
                _screenPages = new ScreenFramebuffer?[4];
            }

            if (_isScreenActive)
            {
                for (var page = 0; page < _screenPages.Length; page++)
                {
                    _screenPages[page] ??= new ScreenFramebuffer(width, height);
                }

                _framebuffer = _screenPages[_activeScreenPage];
            }
            else
            {
                _framebuffer = new ScreenFramebuffer(width, height);
            }

            _graphicsTextBuffer.Clear();
            _pendingGraphicsTraceLine.Clear();
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            _graphicsForeground = 15;
            _graphicsBackground = null;
            _graphicsLastBackgroundColor = 0;
            ResetTextViewportCore(resetCursor: false);
            _screenTextFirstVisibleRow = 1;
            _graphicsRow = 1;
            _graphicsColumn = 1;
            _pendingGraphicsViewportScroll = false;
            _graphicsCurrentX = 0;
            _graphicsCurrentY = 0;
            ResetGraphicsFrameMetrics();
            PublishGraphicsFrame();
            RaiseGraphicsPropertyChanged();
            EnsureGraphicsWindow();
            WriteGraphicsEvent(
                "screen.activated",
                $"mode={mode} | size={width}x{height}");
        });
    }

    public void ActivateCanvas(long width, long height)
    {
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsFrameSummary(final: true);
            WriteGraphicsInputSummary(final: true);

            if (width <= 0 || height <= 0 || width > int.MaxValue || height > int.MaxValue)
            {
                return;
            }

            _isScreenActive = false;
            _isCanvasActive = true;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = (int)width;
            _screenHeight = (int)height;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _canvasFrameInProgress = false;
            _inputState.SetScreenState(isScreenActive: false, isScreenOutputActive: false, _screenMode, _screenWidth, _screenHeight);
            ResetViewportCore();
            ResetWindowCore();
            _screenPages = new ScreenFramebuffer?[4];
            _framebuffer = new ScreenFramebuffer(_screenWidth, _screenHeight);
            _framebuffer.ClearTrueColor(0);
            _graphicsTextBuffer.Clear();
            _pendingGraphicsTraceLine.Clear();
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            _graphicsForeground = 15;
            _graphicsBackground = null;
            _graphicsLastBackgroundColor = 0;
            ResetTextViewportCore(resetCursor: false);
            _screenTextFirstVisibleRow = 1;
            _graphicsRow = 1;
            _graphicsColumn = 1;
            _pendingGraphicsViewportScroll = false;
            _graphicsCurrentX = 0;
            _graphicsCurrentY = 0;
            ResetGraphicsFrameMetrics();
            PublishGraphicsFrame();
            RaiseGraphicsPropertyChanged();
            EnsureGraphicsWindow();
            WriteGraphicsEvent(
                "canvas.activated",
                $"size={width}x{height}");
        });
    }

    public void DeactivateScreen()
    {
        InvokeOnDispatcher(() =>
        {
            var previousMode = _screenMode;
            var previousWidth = _screenWidth;
            var previousHeight = _screenHeight;
            _isScreenActive = false;
            var wasCanvasActive = _isCanvasActive;
            _isCanvasActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _canvasFrameInProgress = false;
            _inputState.SetScreenState(_isScreenActive, _isScreenOutputActive, _screenMode, _screenWidth, _screenHeight);
            ResetViewportCore();
            ResetWindowCore();
            FlushPendingGraphicsTraceLine(logEmpty: false);
            _framebuffer = null;
            _screenPages = new ScreenFramebuffer?[4];
            GraphicsFrame = null;
            _graphicsTextBuffer.Clear();
            ResetTextViewportCore(resetCursor: false);
            _screenTextFirstVisibleRow = 1;
            GraphicsText = string.Empty;
            CancelScheduledGraphicsVisualRefresh();
            WriteGraphicsFrameSummary(final: true);
            WriteGraphicsInputSummary(final: true);
            RaiseGraphicsPropertyChanged();
            CloseGraphicsWindow();
            WriteGraphicsEvent(
                wasCanvasActive ? "canvas.deactivated" : "screen.deactivated",
                $"mode={previousMode} | size={previousWidth}x{previousHeight}");
        });
    }

    public void BeginFrame()
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            _canvasFrameInProgress = true;
            CancelScheduledGraphicsVisualRefresh();
            _framebuffer.ClearTrueColor(0);
        });
    }

    public void PresentFrame()
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive)
            {
                return;
            }

            _canvasFrameInProgress = false;
            CancelScheduledGraphicsVisualRefresh();
            PublishGraphicsFrame();
        });
    }

    public void ClearFrame(int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            _framebuffer.ClearTrueColor(color);
        });
    }

    public void SetCanvasCamera(long x, long y)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive)
            {
                return;
            }

            _canvasCameraX = x;
            _canvasCameraY = y;
        });
    }

    public void ResetCanvasCamera()
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive)
            {
                return;
            }

            _canvasCameraX = 0;
            _canvasCameraY = 0;
        });
    }

    public bool CopyScreenPage(int sourcePage, int destinationPage)
    {
        var copied = false;
        InvokeOnDispatcher(() =>
        {
            if (!_isScreenActive || sourcePage is < 0 or > 3 || destinationPage is < 0 or > 3)
            {
                copied = false;
                return;
            }

            var source = _screenPages[sourcePage];
            var destination = _screenPages[destinationPage];
            if (source is null || destination is null)
            {
                copied = false;
                return;
            }

            destination.CopyFrom(source);
            if (destinationPage == _activeScreenPage)
            {
                _framebuffer = destination;
            }

            copied = true;
            PublishGraphicsFrame();
        });

        return copied;
    }

    #endregion

    #region Drawing primitives

    public void DrawRect(long x, long y, long width, long height, int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            var left = TranslateCanvasX(x, viewport);
            var top = TranslateCanvasY(y, viewport);
            DrawAbsoluteLineCore(left, top, left + width - 1, top, color, trueColor: true, viewport);
            DrawAbsoluteLineCore(left + width - 1, top, left + width - 1, top + height - 1, color, trueColor: true, viewport);
            DrawAbsoluteLineCore(left + width - 1, top + height - 1, left, top + height - 1, color, trueColor: true, viewport);
            DrawAbsoluteLineCore(left, top + height - 1, left, top, color, trueColor: true, viewport);
        });
    }

    public void FillRect(long x, long y, long width, long height, int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            var left = TranslateCanvasX(x, viewport);
            var top = TranslateCanvasY(y, viewport);
            var clippedLeft = Math.Max(left, viewport.Left);
            var clippedTop = Math.Max(top, viewport.Top);
            var clippedRight = Math.Min(left + width - 1, viewport.Right);
            var clippedBottom = Math.Min(top + height - 1, viewport.Bottom);
            _framebuffer.FillTrueColorRectangle(clippedLeft, clippedTop, clippedRight, clippedBottom, color);
        });
    }

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            DrawAbsoluteLineCore(
                TranslateCanvasX(x1, viewport),
                TranslateCanvasY(y1, viewport),
                TranslateCanvasX(x2, viewport),
                TranslateCanvasY(y2, viewport),
                color,
                trueColor: true,
                viewport);
        });
    }

    public void DrawCanvasCircle(long x, long y, long radius, int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            DrawCanvasCircleCore(x, y, radius, color, filled: false);
        });
    }

    public void FillCanvasCircle(long x, long y, long radius, int color)
    {
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            DrawCanvasCircleCore(x, y, radius, color, filled: true);
        });
    }

    public void DrawCanvasText(string text, long x, long y, int color)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() =>
        {
            if (!_isCanvasActive || _framebuffer is null)
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            _framebuffer.DrawTrueColorText(
                TranslateCanvasX(x, viewport),
                TranslateCanvasY(y, viewport),
                text,
                color,
                viewport.Left,
                viewport.Top,
                viewport.Right,
                viewport.Bottom);
        });
    }

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        InvokeOnDispatcher(() =>
        {
            GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawCircle(
        long x,
        long y,
        double radius,
        int color,
        bool relativeCenter,
        double? startAngle,
        double? endAngle,
        double aspect,
        bool drawStartRadius,
        bool drawEndRadius)
    {
        InvokeOnDispatcher(() =>
        {
            GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawCircle(long x, long y, long radius, int color)
    {
        DrawCircle(x, y, radius, color, relativeCenter: false, startAngle: null, endAngle: null, aspect: 1d, drawStartRadius: false, drawEndRadius: false);
    }

    public void Paint(GraphicsPaintRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        InvokeOnDispatcher(() =>
        {
            RunBatchedGraphicsUpdate(() => GraphicsHostDrawing.Paint(this, request));
            SetGraphicsCurrentPositionCore(request.X, request.Y);
        });
    }

    public void Paint(long x, long y, int fillColor, int borderColor)
    {
        Paint(GraphicsPaintRequest.CreateSolid(x, y, fillColor, borderColor));
    }

    #endregion

    #region Pixel operations

    public void SetPixel(long x, long y, int color)
    {
        InvokeOnDispatcher(() =>
        {
            SetLogicalPixelCore(x, y, color);
            SetGraphicsCurrentPositionCore(x, y);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public int GetPixel(long x, long y)
    {
        var color = 0;
        InvokeOnDispatcher(() => color = GetLogicalPixelCore(x, y), DispatcherPriority.Background);
        return color;
    }

    public void SetAbsolutePixel(long x, long y, int color)
    {
        InvokeOnDispatcher(() => SetPixelCore(x, y, color));
    }

    public int GetAbsolutePixel(long x, long y)
    {
        var color = 0;
        InvokeOnDispatcher(() => color = GetPixelCore(x, y), DispatcherPriority.Background);
        return color;
    }

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        InvokeOnDispatcher(() => SetGraphicsCurrentPositionCore(x, y));
    }

    #endregion

    #region Viewport/Window

    public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
    {
        var applied = false;
        InvokeOnDispatcher(() =>
        {
            applied = TrySetViewportCore(left, top, right, bottom, fillColor, borderColor, useScreenCoordinates);
            if (applied)
            {
                ScheduleGraphicsVisualRefresh();
            }
        });

        return applied;
    }

    public void ResetViewport()
    {
        InvokeOnDispatcher(() =>
        {
            ResetViewportCore(preserveCurrentPosition: true);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
    {
        var applied = false;
        InvokeOnDispatcher(() =>
        {
            applied = TrySetWindowCore(left, top, right, bottom, useScreenCoordinates);
            if (applied)
            {
                ScheduleGraphicsVisualRefresh();
            }
        });

        return applied;
    }

    public void ResetWindow()
    {
        InvokeOnDispatcher(() =>
        {
            ResetWindowCore(preserveCurrentPosition: true);
            ScheduleGraphicsVisualRefresh();
        });
    }

    #endregion

    #region Text subsystem

    public void ClearGraphics()
    {
        InvokeOnDispatcher(() =>
        {
            FlushPendingGraphicsTraceLine(logEmpty: false);
            GraphicsText = string.Empty;
            _graphicsTextBuffer.Clear();
            _screenTextFirstVisibleRow = 1;
            _framebuffer?.Clear();
            _graphicsRow = _textViewportTop;
            _graphicsColumn = 1;
            _pendingGraphicsViewportScroll = false;
            _graphicsCurrentX = 0;
            _graphicsCurrentY = 0;
            PublishGraphicsFrame();
            CancelScheduledGraphicsVisualRefresh();
        });
    }

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsForeground = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _graphicsBackground = background;
                    _graphicsLastBackgroundColor = background;
                    break;
                case TextBackgroundMode.Transparent:
                    _graphicsBackground = null;
                    break;
            }

            PublishGraphicsFrame();
        });
    }

    public void LocateText(int row, int column)
    {
        InvokeOnDispatcher(() =>
        {
            _graphicsRow = Math.Max(1, row);
            _graphicsColumn = Math.Max(1, column);
            _pendingGraphicsViewportScroll = false;
        });
    }

    public void WriteGraphicsText(string text, bool newLine)
    {
        ArgumentNullException.ThrowIfNull(text);
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsTextCore(text, newLine);
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

    #endregion

    #region Image/Sprite/Tileset/Tilemap

    public RuntimeImageValue? LoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var resolvedPath = ResolveResourcePath(path);
            if (!File.Exists(resolvedPath))
            {
                return null;
            }

            using var stream = File.OpenRead(resolvedPath);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                return null;
            }

            BitmapSource source = decoder.Frames[0];
            if (source.Format != PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0d);
            }

            var width = source.PixelWidth;
            var height = source.PixelHeight;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var pixels = new uint[checked(width * height)];
            source.CopyPixels(pixels, width * sizeof(uint), offset: 0);
            return new RuntimeImageValue(width, height, pixels);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or OverflowException)
        {
            return null;
        }
    }

    public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return null;
        }

        var image = LoadImage(path);
        return image is null ? null : new RuntimeSpriteValue(image, frameWidth, frameHeight);
    }

    public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0)
    {
        if (tileWidth <= 0 || tileHeight <= 0 || margin < 0 || spacing < 0)
        {
            return null;
        }

        var image = LoadImage(path);
        if (image is null)
        {
            return null;
        }

        try
        {
            return new RuntimeTilesetValue(image, tileWidth, tileHeight, margin, spacing);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    public RuntimeTilemapValue? LoadTilemap(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var resolvedPath = ResolveResourcePath(path);
            if (!File.Exists(resolvedPath))
            {
                return null;
            }

            return RuntimeTilemapValue.LoadFromTiledJson(
                resolvedPath,
                staticResolvedPath => LoadImageFromResolvedPath(staticResolvedPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or InvalidDataException or JsonException or OverflowException)
        {
            return null;
        }
    }

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        InvokeOnDispatcher(() =>
        {
            if (_isCanvasActive)
            {
                DrawImageOnCanvas(image, x, y, width, height);
                return;
            }

            if (!_inputState.Snapshot.IsScreenActive || _framebuffer is null)
            {
                return;
            }

            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            if (viewport.IsEmpty)
            {
                return;
            }

            _framebuffer.DrawImage(image, absoluteX, absoluteY, width, height, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(sprite);
        InvokeOnDispatcher(() =>
        {
            if (_isCanvasActive)
            {
                DrawSpriteOnCanvas(sprite, frame, x, y, width, height);
                return;
            }

            if (!_inputState.Snapshot.IsScreenActive || _framebuffer is null)
            {
                return;
            }

            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            if (viewport.IsEmpty)
            {
                return;
            }

            _framebuffer.DrawSprite(sprite, frame, absoluteX, absoluteY, width, height, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
            ScheduleGraphicsVisualRefresh();
        });
    }

    public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        InvokeOnDispatcher(() =>
        {
            if (_isCanvasActive)
            {
                DrawImageRegionOnCanvas(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height);
                return;
            }

            if (!_inputState.Snapshot.IsScreenActive || _framebuffer is null)
            {
                return;
            }

            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            var viewport = GetEffectiveViewportCore();
            if (viewport.IsEmpty)
            {
                return;
            }

            _framebuffer.DrawImageRegion(image, sourceX, sourceY, sourceWidth, sourceHeight, absoluteX, absoluteY, width, height, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
            ScheduleGraphicsVisualRefresh();
        });
    }

    #endregion

    #region Palette

    public void ResetPalette()
    {
        InvokeOnDispatcher(() =>
        {
            _framebuffer?.ResetPalette();
            PublishGraphicsFrame();
        });
    }

    public void SetPaletteColor(int index, int color)
    {
        InvokeOnDispatcher(() =>
        {
            _framebuffer?.SetPaletteColor(index, color);
            PublishGraphicsFrame();
        });
    }

    #endregion

    #region Graphics input

    public string ReadGraphicsInput(InputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _graphicsInputRequestCount++;
        return _inputPrompt.RequestInput(
            new ProgramInputRequest(_graphicsInputRequestCount, request.PromptText, request.ValueCount),
            (promptText, inputText, isScreenOutputActive) =>
            {
                if (isScreenOutputActive)
                {
                    WriteGraphicsTextCore(promptText + inputText, newLine: true);
                    PublishGraphicsFrame();
                }
            },
            _inputState.Snapshot.IsScreenOutputActive,
            () => PublishGraphicsFrame());
    }

    #endregion

    #region Сброс состояния

    /// <summary>
    /// Сбросить всё графическое состояние (вызывается фасадом при ResetSessionState).
    /// </summary>
    public void ResetGraphicsState()
    {
        _isScreenActive = false;
        _isScreenOutputActive = false;
        _isCanvasActive = false;
        _screenMode = 0;
        _screenWidth = 0;
        _screenHeight = 0;
        _activeScreenPage = 0;
        _visualScreenPage = 0;
        _canvasCameraX = 0;
        _canvasCameraY = 0;
        _framebuffer = null;
        _screenPages = new ScreenFramebuffer?[4];
        GraphicsFrame = null;
        _graphicsTextBuffer.Clear();
        _pendingGraphicsTraceLine.Clear();
        GraphicsText = string.Empty;
        CancelScheduledGraphicsVisualRefresh();
        _graphicsForeground = 15;
        _graphicsBackground = null;
        _graphicsLastBackgroundColor = 0;
        ResetTextViewportCore(resetCursor: false);
        _screenTextFirstVisibleRow = 1;
        _graphicsRow = 1;
        _graphicsColumn = 1;
        _pendingGraphicsViewportScroll = false;
        _graphicsCurrentX = 0;
        _graphicsCurrentY = 0;
        ResetViewportCore();
        ResetWindowCore();
        CloseGraphicsWindow();
    }

    #endregion

    #region Методы для фасада (вызываются из ProgramConsoleService)

    /// <summary>
    /// Записать текст в графический буфер (вызывается фасадом из WriteOutput и HandleInputSubmit).
    /// </summary>
    public void WriteGraphicsTextForOutput(string text, bool newLine)
    {
        InvokeOnDispatcher(() =>
        {
            WriteGraphicsTextCore(text, newLine);
            PublishGraphicsFrame();
        });
    }

    /// <summary>
    /// Опубликовать кадр (вызывается фасадом после изменения состояния ввода).
    /// </summary>
    public void PublishFrame()
    {
        PublishGraphicsFrame();
    }

    /// <summary>
    /// Записать событие графики (вызывается фасадом).
    /// </summary>
    public void WriteEvent(string eventName, string message)
    {
        WriteGraphicsEvent(eventName, message);
    }

    /// <summary>
    /// Записать метрику нажатия клавиши (вызывается фасадом из EnqueueKey/ReadKey).
    /// </summary>
    public void RecordKeyEnqueued()
    {
        if (_inputState.Snapshot.IsScreenActive)
        {
            _graphicsKeysEnqueued++;
            _graphicsMaxPendingKeys = Math.Max(_graphicsMaxPendingKeys, _inputState.Snapshot.PendingKeyCount);
        }
    }

    /// <summary>
    /// Записать метрику чтения клавиши (вызывается фасадом из ReadKey).
    /// </summary>
    public void RecordKeyRead()
    {
        if (_inputState.Snapshot.IsScreenActive)
        {
            _graphicsKeysRead++;
        }
    }

    /// <summary>
    /// Записать метрику события мыши (вызывается фасадом из EnqueueMouse*).
    /// </summary>
    public void RecordMouseEventEnqueued(MouseInputEventKind eventKind)
    {
        if (!_inputState.Snapshot.IsScreenActive)
        {
            return;
        }

        _graphicsMouseEventsEnqueued++;
        switch (eventKind)
        {
            case MouseInputEventKind.Move:
                _graphicsMouseMoveEnqueued++;
                break;
            case MouseInputEventKind.Press:
                _graphicsMousePressEnqueued++;
                break;
            case MouseInputEventKind.Release:
                _graphicsMouseReleaseEnqueued++;
                break;
            case MouseInputEventKind.Wheel:
                _graphicsMouseWheelEnqueued++;
                break;
        }
    }

    /// <summary>
    /// Записать метрику чтения события мыши (вызывается фасадом из ReadMouseEvent).
    /// </summary>
    public void RecordMouseEventRead(MouseInputEventKind eventKind)
    {
        if (!_inputState.Snapshot.IsScreenActive)
        {
            return;
        }

        _graphicsMouseEventsRead++;
        switch (eventKind)
        {
            case MouseInputEventKind.Move:
                _graphicsMouseMoveRead++;
                break;
            case MouseInputEventKind.Press:
                _graphicsMousePressRead++;
                break;
            case MouseInputEventKind.Release:
                _graphicsMouseReleaseRead++;
                break;
            case MouseInputEventKind.Wheel:
                _graphicsMouseWheelRead++;
                break;
        }
    }

    #endregion

    #region Private: Image helpers

    private void DrawImageOnCanvas(RuntimeImageValue image, long x, long y, int width, int height)
    {
        if (_framebuffer is null || _screenWidth <= 0 || _screenHeight <= 0)
        {
            return;
        }

        var viewport = GetEffectiveViewportCore();
        _framebuffer.DrawImage(
            image,
            TranslateCanvasX(x, viewport),
            TranslateCanvasY(y, viewport),
            width,
            height,
            viewport.Left,
            viewport.Top,
            viewport.Right,
            viewport.Bottom);
    }

    private void DrawSpriteOnCanvas(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
        if (_framebuffer is null || _screenWidth <= 0 || _screenHeight <= 0)
        {
            return;
        }

        var viewport = GetEffectiveViewportCore();
        _framebuffer.DrawSprite(
            sprite,
            frame,
            TranslateCanvasX(x, viewport),
            TranslateCanvasY(y, viewport),
            width,
            height,
            viewport.Left,
            viewport.Top,
            viewport.Right,
            viewport.Bottom);
    }

    private void DrawImageRegionOnCanvas(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
    {
        if (_framebuffer is null || _screenWidth <= 0 || _screenHeight <= 0)
        {
            return;
        }

        var viewport = GetEffectiveViewportCore();
        _framebuffer.DrawImageRegion(
            image,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
            TranslateCanvasX(x, viewport),
            TranslateCanvasY(y, viewport),
            width,
            height,
            viewport.Left,
            viewport.Top,
            viewport.Right,
            viewport.Bottom);
    }

    private RuntimeImageValue? LoadImageFromResolvedPath(string resolvedPath)
    {
        try
        {
            if (!File.Exists(resolvedPath))
            {
                return null;
            }

            using var stream = File.OpenRead(resolvedPath);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                return null;
            }

            BitmapSource source = decoder.Frames[0];
            if (source.Format != PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0d);
            }

            var width = source.PixelWidth;
            var height = source.PixelHeight;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var pixels = new uint[checked(width * height)];
            source.CopyPixels(pixels, width * sizeof(uint), offset: 0);
            return new RuntimeImageValue(width, height, pixels);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or OverflowException)
        {
            return null;
        }
    }

    private string ResolveResourcePath(string path)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(path);
        if (Path.IsPathFullyQualified(expandedPath))
        {
            return expandedPath;
        }

        var baseDirectory = string.IsNullOrWhiteSpace(ResourceBaseDirectory)
            ? Environment.CurrentDirectory
            : ResourceBaseDirectory!;
        return Path.GetFullPath(Path.Combine(baseDirectory, expandedPath));
    }

    #endregion

    #region Private: Text helpers

    private void WriteGraphicsTextCore(string text, bool newLine)
    {
        var followScreenZeroBottom = IsScreenZeroMode && IsViewingScreenZeroBottom();
        NormalizeGraphicsViewportCursorBeforeWrite();
        var targetRow = ResolveGraphicsTextBufferRow(_graphicsRow);
        _graphicsTextBuffer.Write(
            targetRow,
            _graphicsColumn,
            text,
            _graphicsForeground,
            _graphicsBackground,
            IsScreenZeroMode ? ScreenTextCellColumns : GetVisibleTextColumns());
        _pendingGraphicsTraceLine.Append(text);

        if (newLine)
        {
            _graphicsColumn = 1;
            if (IsScreenZeroMode)
            {
                AdvanceScreenZeroCursorAfterNewLine(targetRow, followScreenZeroBottom);
            }
            else if (IsScreenActive)
            {
                AdvanceGraphicsViewportCursorAfterNewLine();
            }
            else
            {
                _graphicsRow++;
                _graphicsTextBuffer.EnsureRow(_graphicsRow);
                _pendingGraphicsViewportScroll = false;
            }

            FlushPendingGraphicsTraceLine(logEmpty: true);
        }
        else
        {
            _graphicsColumn += text.Length;
        }

    }

    private void AdvanceScreenZeroCursorAfterNewLine(int targetRow, bool followScreenZeroBottom)
    {
        var nextRow = targetRow + 1;
        _graphicsTextBuffer.EnsureRow(nextRow);
        if (_graphicsRow < ScreenTextCellRows)
        {
            _graphicsRow++;
        }

        if (followScreenZeroBottom)
        {
            _screenTextFirstVisibleRow = GetScreenZeroBottomFirstVisibleRow();
        }
    }

    private void AdvanceGraphicsViewportCursorAfterNewLine()
    {
        if (!_textViewportExplicit)
        {
            _graphicsRow++;
            _graphicsTextBuffer.EnsureRow(_graphicsRow);
            _pendingGraphicsViewportScroll = false;
            return;
        }

        if (_graphicsRow >= _textViewportTop && _graphicsRow <= _textViewportBottom)
        {
            if (_graphicsRow < _textViewportBottom)
            {
                _graphicsRow++;
                _graphicsTextBuffer.EnsureRow(_graphicsRow);
                _pendingGraphicsViewportScroll = false;
                return;
            }

            _graphicsRow = _textViewportBottom + 1;
            _graphicsTextBuffer.EnsureRow(_graphicsRow);
            _pendingGraphicsViewportScroll = true;
            return;
        }

        _graphicsRow++;
        _graphicsTextBuffer.EnsureRow(_graphicsRow);
        _pendingGraphicsViewportScroll = false;
    }

    private void NormalizeGraphicsViewportCursorBeforeWrite()
    {
        if (!_pendingGraphicsViewportScroll || !IsScreenActive)
        {
            return;
        }

        _graphicsTextBuffer.ScrollRowsUp(_textViewportTop, _textViewportBottom);
        _graphicsTextBuffer.EnsureRow(_textViewportBottom);
        _graphicsRow = _textViewportBottom;
        _graphicsColumn = 1;
        _pendingGraphicsViewportScroll = false;
    }

    private bool TrySetTextViewportCore(int topRow, int bottomRow)
    {
        if (!IsScreenActive)
        {
            return false;
        }

        var visibleRows = GetVisibleTextRows();
        if (topRow < 1 || bottomRow < topRow || bottomRow > visibleRows)
        {
            return false;
        }

        _textViewportTop = topRow;
        _textViewportBottom = bottomRow;
        _textViewportExplicit = true;
        _graphicsRow = topRow;
        _graphicsColumn = 1;
        _pendingGraphicsViewportScroll = false;
        PublishGraphicsFrame();
        return true;
    }

    private void ResetTextViewportCore(bool resetCursor = true)
    {
        _textViewportTop = 1;
        _textViewportBottom = IsScreenZeroMode ? ScreenTextCellRows : GetVisibleTextRows();
        _textViewportExplicit = false;
        _pendingGraphicsViewportScroll = false;
        if (resetCursor)
        {
            _graphicsRow = _textViewportTop;
            _graphicsColumn = 1;
        }
    }

    private string BuildVisibleGraphicsText()
    {
        var (firstVisibleRow, visibleRows, visibleColumns) = GetGraphicsTextViewport();
        return _graphicsTextBuffer.ToPlainText(firstVisibleRow, visibleRows, visibleColumns);
    }

    private (int FirstVisibleRow, int VisibleRows, int VisibleColumns) GetGraphicsTextViewport()
    {
        if (IsScreenZeroMode)
        {
            return (_screenTextFirstVisibleRow, ScreenTextCellRows, ScreenTextCellColumns);
        }

        var visibleRows = GetVisibleTextRows();
        var visibleColumns = GetVisibleTextColumns();
        return (1, visibleRows, visibleColumns);
    }

    private int ResolveGraphicsTextBufferRow(int logicalRow)
    {
        logicalRow = Math.Max(1, logicalRow);
        if (!IsScreenZeroMode)
        {
            return logicalRow;
        }

        return GetScreenZeroLiveWindowFirstRow() + logicalRow - 1;
    }

    private int GetScreenZeroLiveWindowFirstRow() =>
        Math.Max(1, _graphicsTextBuffer.LineCount - ScreenTextCellRows + 1);

    private int GetScreenZeroBottomFirstVisibleRow() =>
        Math.Max(1, Math.Max(1, _graphicsTextBuffer.LineCount) - ScreenTextCellRows + 1);

    private int GetVisibleTextRows() =>
        Math.Max(1, _screenHeight / ScreenTextRasterizer.CellHeight);

    private int GetVisibleTextColumns() =>
        Math.Max(1, _screenWidth / ScreenTextRasterizer.CellWidth);

    private bool IsViewingScreenZeroBottom() =>
        _screenTextFirstVisibleRow >= GetScreenZeroBottomFirstVisibleRow();

    private bool IsScreenZeroMode =>
        _isScreenOutputActive && !_isScreenActive && _screenMode == 0;

    #endregion

    #region Private: Pixel helpers

    private void SetPixelCore(long x, long y, int color)
    {
        if (!IsInsideScreen(x, y))
        {
            return;
        }

        _framebuffer?.SetPixel(x, y, color);
        _graphicsBatchHasChanges = true;
    }

    private void SetTrueColorPixelCore(long x, long y, int color)
    {
        if (!IsInsideScreen(x, y))
        {
            return;
        }

        _framebuffer?.SetTrueColorPixel(x, y, color);
        _graphicsBatchHasChanges = true;
    }

    private void SetLogicalPixelCore(long x, long y, int color)
    {
        if (_isCanvasActive)
        {
            var viewport = GetEffectiveViewportCore();
            var canvasAbsoluteX = TranslateCanvasX(x, viewport);
            var canvasAbsoluteY = TranslateCanvasY(y, viewport);
            if (!viewport.IsInsideAbsolute(canvasAbsoluteX, canvasAbsoluteY))
            {
                return;
            }

            SetPixelCore(canvasAbsoluteX, canvasAbsoluteY, color);
            return;
        }

        if (!TryTranslateLogicalToAbsoluteCore(x, y, out var absoluteX, out var absoluteY))
        {
            return;
        }

        SetPixelCore(absoluteX, absoluteY, color);
    }

    private void SetGraphicsCurrentPositionCore(long x, long y)
    {
        _graphicsCurrentX = x;
        _graphicsCurrentY = y;
    }

    private int GetPixelCore(long x, long y)
    {
        return _framebuffer?.GetPixel(x, y) ?? 0;
    }

    private int GetLogicalPixelCore(long x, long y)
    {
        if (_isCanvasActive)
        {
            var viewport = GetEffectiveViewportCore();
            var canvasAbsoluteX = TranslateCanvasX(x, viewport);
            var canvasAbsoluteY = TranslateCanvasY(y, viewport);
            if (!viewport.IsInsideAbsolute(canvasAbsoluteX, canvasAbsoluteY))
            {
                return 0;
            }

            return GetPixelCore(canvasAbsoluteX, canvasAbsoluteY);
        }

        return TryTranslateLogicalToAbsoluteCore(x, y, out var absoluteX, out var absoluteY)
            ? GetPixelCore(absoluteX, absoluteY)
            : 0;
    }

    #endregion

    #region Private: Drawing core helpers

    private void DrawLineCore(long x1, long y1, long x2, long y2, int color)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            SetLogicalPixelCore(x1, y1, color);
            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var doubledError = GraphicsLineMath.SaturatingDouble(error);
            if (doubledError >= dy)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dy);
                x1 += sx;
            }

            if (doubledError <= dx)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dx);
                y1 += sy;
            }
        }

        SetGraphicsCurrentPositionCore(x2, y2);
    }

    private void DrawAbsoluteLineCore(long x1, long y1, long x2, long y2, int color, bool trueColor = false, GraphicsViewport? clipViewport = null)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;
        var viewport = clipViewport ?? new GraphicsViewport(0, 0, _screenWidth - 1L, _screenHeight - 1L, UsesRelativeCoordinates: false);

        while (true)
        {
            if (viewport.IsInsideAbsolute(x1, y1))
            {
                if (trueColor)
                {
                    SetTrueColorPixelCore(x1, y1, color);
                }
                else
                {
                    SetPixelCore(x1, y1, color);
                }
            }

            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var doubledError = GraphicsLineMath.SaturatingDouble(error);
            if (doubledError >= dy)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dy);
                x1 += sx;
            }

            if (doubledError <= dx)
            {
                error = GraphicsLineMath.SaturatingAdd(error, dx);
                y1 += sy;
            }
        }
    }

    private void DrawCanvasCircleCore(long worldX, long worldY, long radius, int color, bool filled)
    {
        if (_framebuffer is null || _screenWidth <= 0 || _screenHeight <= 0 || radius < 0)
        {
            return;
        }

        var viewport = GetEffectiveViewportCore();
        var centerX = TranslateCanvasX(worldX, viewport);
        var centerY = TranslateCanvasY(worldY, viewport);
        if (radius == 0)
        {
            if (viewport.IsInsideAbsolute(centerX, centerY))
            {
                SetTrueColorPixelCore(centerX, centerY, color);
            }
            return;
        }

        if (filled)
        {
            var radiusSquared = (double)radius * radius;
            for (long dy = -radius; dy <= radius; dy++)
            {
                var span = (long)Math.Floor(Math.Sqrt(Math.Max(0d, radiusSquared - ((double)dy * dy))));
                var rowY = centerY + dy;
                if (rowY < viewport.Top || rowY > viewport.Bottom)
                {
                    continue;
                }

                _framebuffer.FillTrueColorRectangle(
                    Math.Max(centerX - span, viewport.Left),
                    rowY,
                    Math.Min(centerX + span, viewport.Right),
                    rowY,
                    color);
            }

            _graphicsBatchHasChanges = true;
            return;
        }

        GraphicsEllipseRasterizer.Rasterize(
            centerX,
            centerY,
            radius,
            radius,
            true,
            0d,
            Math.Tau,
            _screenWidth,
            _screenHeight,
            (x, y) =>
            {
                if (viewport.IsInsideAbsolute(x, y))
                {
                    SetTrueColorPixelCore(x, y, color);
                }
            });
    }

    private void DrawBoxCore(long x1, long y1, long x2, long y2, int color, bool filled)
    {
        var left = Math.Min(x1, x2);
        var right = Math.Max(x1, x2);
        var top = Math.Min(y1, y2);
        var bottom = Math.Max(y1, y2);

        if (filled)
        {
            AddFillCore(left, top, right, bottom, color);

            return;
        }

        DrawLineCore(left, top, right, top, color);
        DrawLineCore(right, top, right, bottom, color);
        DrawLineCore(right, bottom, left, bottom, color);
        DrawLineCore(left, bottom, left, top, color);
    }

    private void DrawCircleCore(
        long centerX,
        long centerY,
        double radius,
        int color,
        bool relativeCenter,
        double? startAngle,
        double? endAngle,
        double aspect,
        bool drawStartRadius,
        bool drawEndRadius)
    {
        if (!GraphicsCircleValidation.HasValidArguments(radius, startAngle, endAngle, aspect))
        {
            return;
        }

        if (relativeCenter)
        {
            if (!GraphicsCoordinateValidation.TryResolvePoint(_graphicsCurrentX, _graphicsCurrentY, centerX, centerY, out centerX, out centerY))
            {
                return;
            }
        }
        else if (!GraphicsCoordinateValidation.IsSupportedCircleCenter(centerX, centerY))
        {
            return;
        }

        SetGraphicsCurrentPositionCore(centerX, centerY);

        var radiusX = aspect <= 1d ? radius : radius / aspect;
        var radiusY = aspect <= 1d ? radius * aspect : radius;

        if (radiusX <= 0 || radiusY <= 0)
        {
            SetLogicalPixelCore(centerX, centerY, color);
            return;
        }

        var arcStart = NormalizeCircleAngle(startAngle) ?? 0d;
        var normalizedEnd = NormalizeCircleAngle(endAngle);
        var fullCircle = !startAngle.HasValue && !endAngle.HasValue;
        if (!fullCircle && startAngle.HasValue && normalizedEnd.HasValue)
        {
            fullCircle = Math.Abs(arcStart - normalizedEnd.Value) <= 1e-9;
        }

        var sweep = fullCircle ? Math.Tau : (normalizedEnd ?? Math.Tau) - arcStart;
        if (!fullCircle && sweep <= 0d)
        {
            sweep += Math.Tau;
        }

        var startPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart);

        if (drawStartRadius)
        {
            DrawLineCore(centerX, centerY, startPoint.X, startPoint.Y, color);
        }

        GraphicsEllipseRasterizer.Rasterize(
            centerX,
            centerY,
            radiusX,
            radiusY,
            fullCircle,
            arcStart,
            sweep,
            GetRasterWidthCore(),
            GetRasterHeightCore(),
            (x, y) => SetLogicalPixelCore(x, y, color));

        if (drawEndRadius)
        {
            var endPoint = GraphicsEllipseRasterizer.GetEllipsePoint(centerX, centerY, radiusX, radiusY, arcStart + sweep);
            DrawLineCore(centerX, centerY, endPoint.X, endPoint.Y, color);
        }

        SetGraphicsCurrentPositionCore(centerX, centerY);
    }

    private static double? NormalizeCircleAngle(double? angle) =>
        angle switch
        {
            null => null,
            < 0 => GraphicsEllipseRasterizer.NormalizeAngle(-angle.Value),
            _ => GraphicsEllipseRasterizer.NormalizeAngle(angle.Value)
        };

    private int GetRasterWidthCore()
    {
        var viewport = GetEffectiveViewportCore();
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Width))
            : _screenWidth;
    }

    private int GetRasterHeightCore()
    {
        var viewport = GetEffectiveViewportCore();
        return viewport.UsesRelativeCoordinates
            ? checked((int)Math.Max(0, viewport.Height))
            : _screenHeight;
    }

    private void AddFillCore(long left, long top, long right, long bottom, int color)
    {
        if (!_inputState.Snapshot.IsScreenActive)
        {
            return;
        }

        var clippedLeft = Math.Max(0, left);
        var clippedTop = Math.Max(0, top);
        var clippedRight = Math.Min(_screenWidth - 1L, right);
        var clippedBottom = Math.Min(_screenHeight - 1L, bottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        _framebuffer?.FillRectangle(clippedLeft, clippedTop, clippedRight, clippedBottom, color);
    }

    #endregion

    #region Private: Coordinate helpers

    private long TranslateCanvasX(long x, GraphicsViewport viewport) =>
        GraphicsLineMath.SaturatingAdd(viewport.Left, x - _canvasCameraX);

    private long TranslateCanvasY(long y, GraphicsViewport viewport) =>
        GraphicsLineMath.SaturatingAdd(viewport.Top, y - _canvasCameraY);

    private bool IsInsideScreen(long x, long y) =>
        (_isScreenActive || _isCanvasActive) &&
        x >= 0 &&
        y >= 0 &&
        x < _screenWidth &&
        y < _screenHeight;

    private GraphicsViewport GetEffectiveViewportCore()
    {
        if (_viewportEnabled)
        {
            return new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates);
        }

        var right = _screenWidth > 0 ? _screenWidth - 1L : -1L;
        var bottom = _screenHeight > 0 ? _screenHeight - 1L : -1L;
        return new GraphicsViewport(0, 0, right, bottom, UsesRelativeCoordinates: false);
    }

    private bool TryTranslateLogicalToAbsoluteCore(long x, long y, out long absoluteX, out long absoluteY)
    {
        return GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out absoluteX, out absoluteY);
    }

    private (long X, long Y) ResolveCurrentAbsolutePositionCore()
    {
        return GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY)
            ? (absoluteX, absoluteY)
            : (0L, 0L);
    }

    #endregion

    #region Private: Viewport/Window core

    private void ResetViewportCore(bool preserveCurrentPosition = false)
    {
        var absolutePosition = preserveCurrentPosition ? ResolveCurrentAbsolutePositionCore() : (0L, 0L);
        _viewportEnabled = false;
        _viewportRelativeCoordinates = false;
        _viewportLeft = 0;
        _viewportTop = 0;
        _viewportRight = _screenWidth > 0 ? _screenWidth - 1L : -1L;
        _viewportBottom = _screenHeight > 0 ? _screenHeight - 1L : -1L;
        if (preserveCurrentPosition)
        {
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, absolutePosition.Item1, absolutePosition.Item2, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        }
    }

    private bool TrySetViewportCore(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
    {
        if (_screenWidth <= 0 || _screenHeight <= 0 || left > right || top > bottom)
        {
            return false;
        }

        var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
        var baseTop = _viewportEnabled ? _viewportTop : 0L;
        var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
        var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
        var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
        var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
        if (!IsInsideScreen(absoluteLeft, absoluteTop) || !IsInsideScreen(absoluteRight, absoluteBottom))
        {
            return false;
        }

        var previousAbsolutePosition = ResolveCurrentAbsolutePositionCore();
        RunBatchedGraphicsUpdate(() =>
        {
            if (fillColor.HasValue)
            {
                _framebuffer?.FillRectangle(absoluteLeft, absoluteTop, absoluteRight, absoluteBottom, fillColor.Value);
                _graphicsBatchHasChanges = true;
            }

            if (borderColor.HasValue)
            {
                DrawAbsoluteLineCore(absoluteLeft, absoluteTop, absoluteRight, absoluteTop, borderColor.Value);
                DrawAbsoluteLineCore(absoluteRight, absoluteTop, absoluteRight, absoluteBottom, borderColor.Value);
                DrawAbsoluteLineCore(absoluteRight, absoluteBottom, absoluteLeft, absoluteBottom, borderColor.Value);
                DrawAbsoluteLineCore(absoluteLeft, absoluteBottom, absoluteLeft, absoluteTop, borderColor.Value);
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, previousAbsolutePosition.X, previousAbsolutePosition.Y, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        });

        return true;
    }

    private void ResetWindowCore(bool preserveCurrentPosition = false)
    {
        var absolutePosition = preserveCurrentPosition ? ResolveCurrentAbsolutePositionCore() : (0L, 0L);
        _windowEnabled = false;
        _windowUsesScreenCoordinates = false;
        _windowLeft = 0d;
        _windowTop = 0d;
        _windowRight = 0d;
        _windowBottom = 0d;
        if (preserveCurrentPosition)
        {
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, absolutePosition.Item1, absolutePosition.Item2, out var logicalX, out var logicalY))
            {
                SetGraphicsCurrentPositionCore(logicalX, logicalY);
            }
        }
    }

    private bool TrySetWindowCore(double left, double top, double right, double bottom, bool useScreenCoordinates)
    {
        var viewport = GetEffectiveViewportCore();
        if (_screenWidth <= 0 || _screenHeight <= 0 || viewport.Width < 2 || viewport.Height < 2)
        {
            return false;
        }

        if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
        {
            return false;
        }

        var previousAbsolutePosition = ResolveCurrentAbsolutePositionCore();
        _windowEnabled = true;
        _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
        _windowLeft = window.Left;
        _windowTop = window.Top;
        _windowRight = window.Right;
        _windowBottom = window.Bottom;
        if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, previousAbsolutePosition.X, previousAbsolutePosition.Y, out var logicalX, out var logicalY))
        {
            SetGraphicsCurrentPositionCore(logicalX, logicalY);
        }

        return true;
    }

    #endregion

    #region Private: Frame publishing

    private void RefreshGraphicsVisuals()
    {
        if (_isCanvasActive && _canvasFrameInProgress)
        {
            return;
        }

        PublishGraphicsFrame(coalesced: true);
    }

    private void PublishGraphicsFrame(bool coalesced = false)
    {
        var (firstVisibleRow, visibleRows, visibleColumns) = GetGraphicsTextViewport();
        var inputRow = ResolveGraphicsTextBufferRow(_graphicsRow);
        GraphicsText = BuildVisibleGraphicsText();
        var displayFramebuffer = _isScreenActive ? _screenPages[_visualScreenPage] : _framebuffer;
        var frame = displayFramebuffer?.CreateSnapshot(
            _graphicsTextBuffer,
            _graphicsForeground,
            _graphicsBackground,
            _inputPrompt.IsWaitingForInput ? _inputPrompt.CurrentPromptText + _inputPrompt.CurrentInputText : null,
            inputRow,
            _graphicsColumn,
            firstVisibleRow,
            visibleRows,
            visibleColumns);
        GraphicsFrame = frame;
        _graphicsWindow?.PresentFrame(frame);
        RecordGraphicsFramePublished(coalesced);
    }

    private void ScheduleGraphicsVisualRefresh()
    {
        RecordGraphicsCommandQueued();

        if (_isCanvasActive && _canvasFrameInProgress)
        {
            return;
        }

        if (Application.Current is null)
        {
            RefreshGraphicsVisuals();
            return;
        }

        if (_graphicsVisualRefreshPending)
        {
            return;
        }

        _graphicsVisualRefreshPending = true;
        var dispatcher = Application.Current.Dispatcher;
        _graphicsVisualRefreshTimer?.Stop();
        _graphicsVisualRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = GraphicsFrameCoalescingInterval
        };
        _graphicsVisualRefreshTimer.Tick += (_, _) =>
        {
            _graphicsVisualRefreshTimer?.Stop();
            _graphicsVisualRefreshTimer = null;
            _graphicsVisualRefreshPending = false;
            RefreshGraphicsVisuals();
        };
        _graphicsVisualRefreshTimer.Start();
    }

    private void CancelScheduledGraphicsVisualRefresh()
    {
        _graphicsVisualRefreshTimer?.Stop();
        _graphicsVisualRefreshTimer = null;
        _graphicsVisualRefreshPending = false;
    }

    private void RunBatchedGraphicsUpdate(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _graphicsBatchDepth++;
        try
        {
            action();
        }
        finally
        {
            _graphicsBatchDepth--;
            if (_graphicsBatchDepth == 0 && _graphicsBatchHasChanges)
            {
                _graphicsBatchHasChanges = false;
                ScheduleGraphicsVisualRefresh();
            }
        }
    }

    #endregion

    #region Private: Graphics window lifecycle

    private void EnsureGraphicsWindow()
    {
        if (_graphicsWindow is not null)
        {
            _graphicsWindow.Activate();
            return;
        }

        _graphicsWindow = new NativeGraphicsScreenWindow(this, _getInputSink(), _screenWidth, _screenHeight);
        _graphicsWindow.Closed += OnGraphicsWindowClosed;
        _graphicsWindow.Show();
        _graphicsWindow.PresentFrame(GraphicsFrame);
    }

    private void CloseGraphicsWindow()
    {
        if (_graphicsWindow is null)
        {
            return;
        }

        var window = _graphicsWindow;
        _graphicsWindow = null;
        window.Closed -= OnGraphicsWindowClosed;
        window.Close();
    }

    private void OnGraphicsWindowClosed(object? sender, EventArgs e)
    {
        if (_graphicsWindow is null)
        {
            return;
        }

        _graphicsWindow.Closed -= OnGraphicsWindowClosed;
        _graphicsWindow = null;
        if (!_sessionManager.IsSessionActive)
        {
            return;
        }

        _sessionManager.IsCancellationRequested = true;
        WriteGraphicsEvent("screen.closed-by-user", "SCREEN window was closed by the user; run cancellation requested.");
    }

    #endregion

    #region Private: Metrics

    private void ResetGraphicsFrameMetrics()
    {
        _graphicsFrameMetricsStartedAt = DateTimeOffset.UtcNow;
        _lastGraphicsFramePublishedAt = null;
        _graphicsFramePublishCount = 0;
        _graphicsImmediateFramePublishCount = 0;
        _graphicsCoalescedFramePublishCount = 0;
        _graphicsQueuedCommandCount = 0;
        _graphicsQueuedCommandsSinceLastFrame = 0;
        _graphicsMaxCommandsPerFrame = 0;
        _graphicsTotalFrameIntervalMs = 0;
        _graphicsMaxFrameIntervalMs = 0;
        _graphicsKeysEnqueued = 0;
        _graphicsKeysRead = 0;
        _graphicsMouseEventsEnqueued = 0;
        _graphicsMouseEventsRead = 0;
        _graphicsMouseMoveEnqueued = 0;
        _graphicsMousePressEnqueued = 0;
        _graphicsMouseReleaseEnqueued = 0;
        _graphicsMouseWheelEnqueued = 0;
        _graphicsMouseMoveRead = 0;
        _graphicsMousePressRead = 0;
        _graphicsMouseReleaseRead = 0;
        _graphicsMouseWheelRead = 0;
        _graphicsMaxPendingKeys = 0;
        _graphicsMaxPendingMouseEvents = 0;
        _graphicsInputEventsAtLastSummary = 0;
    }

    private void RecordGraphicsCommandQueued()
    {
        if (!_inputState.Snapshot.IsScreenOutputActive)
        {
            return;
        }

        _graphicsQueuedCommandCount++;
        _graphicsQueuedCommandsSinceLastFrame++;
    }

    private void RecordGraphicsFramePublished(bool coalesced)
    {
        if (!_inputState.Snapshot.IsScreenOutputActive)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_lastGraphicsFramePublishedAt is not null)
        {
            var intervalMs = (now - _lastGraphicsFramePublishedAt.Value).TotalMilliseconds;
            _graphicsTotalFrameIntervalMs += intervalMs;
            _graphicsMaxFrameIntervalMs = Math.Max(_graphicsMaxFrameIntervalMs, intervalMs);
        }

        _lastGraphicsFramePublishedAt = now;
        _graphicsFramePublishCount++;
        if (coalesced)
        {
            _graphicsCoalescedFramePublishCount++;
        }
        else
        {
            _graphicsImmediateFramePublishCount++;
        }

        _graphicsMaxCommandsPerFrame = Math.Max(_graphicsMaxCommandsPerFrame, _graphicsQueuedCommandsSinceLastFrame);
        _graphicsQueuedCommandsSinceLastFrame = 0;

        if (_graphicsFramePublishCount % GraphicsFrameSummaryInterval == 0)
        {
            WriteGraphicsFrameSummary(final: false);
            WriteGraphicsInputSummary(final: false);
        }
    }

    private void WriteGraphicsFrameSummary(bool final)
    {
        if (_graphicsFramePublishCount == 0)
        {
            return;
        }

        var intervalCount = Math.Max(0, _graphicsFramePublishCount - 1);
        var averageIntervalMs = intervalCount == 0 ? 0 : _graphicsTotalFrameIntervalMs / intervalCount;
        var elapsedMs = _graphicsFrameMetricsStartedAt is null
            ? 0
            : (DateTimeOffset.UtcNow - _graphicsFrameMetricsStartedAt.Value).TotalMilliseconds;

        WriteGraphicsEvent(
            "screen.frame.summary",
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"final={final} | frames={_graphicsFramePublishCount} | immediateFrames={_graphicsImmediateFramePublishCount} | coalescedFrames={_graphicsCoalescedFramePublishCount} | commands={_graphicsQueuedCommandCount} | maxCommandsPerFrame={_graphicsMaxCommandsPerFrame} | avgIntervalMs={averageIntervalMs:F2} | maxIntervalMs={_graphicsMaxFrameIntervalMs:F2} | elapsedMs={elapsedMs:F2}"));
    }

    private void WriteGraphicsInputSummary(bool final)
    {
        var inputEvents =
            _graphicsKeysEnqueued
            + _graphicsKeysRead
            + _graphicsMouseEventsEnqueued
            + _graphicsMouseEventsRead;
        if (inputEvents == 0 || (!final && inputEvents == _graphicsInputEventsAtLastSummary))
        {
            return;
        }

        _graphicsInputEventsAtLastSummary = inputEvents;
        WriteGraphicsEvent(
            "screen.input.summary",
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"final={final} | keysEnqueued={_graphicsKeysEnqueued} | keysRead={_graphicsKeysRead} | mouseEventsEnqueued={_graphicsMouseEventsEnqueued} | mouseEventsRead={_graphicsMouseEventsRead} | moveEnqueued={_graphicsMouseMoveEnqueued} | moveRead={_graphicsMouseMoveRead} | pressEnqueued={_graphicsMousePressEnqueued} | pressRead={_graphicsMousePressRead} | releaseEnqueued={_graphicsMouseReleaseEnqueued} | releaseRead={_graphicsMouseReleaseRead} | wheelEnqueued={_graphicsMouseWheelEnqueued} | wheelRead={_graphicsMouseWheelRead} | maxPendingKeys={_graphicsMaxPendingKeys} | maxPendingMouseEvents={_graphicsMaxPendingMouseEvents}"));
    }

    #endregion

    #region Private: Tracing

    private void WriteGraphicsEvent(string eventName, string message)
    {
        _traceService.Write(eventName, message);
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "graphics",
            EventName = eventName,
            Source = nameof(GraphicsHostService),
            TextPreview = message
        });
    }

    private void WriteGraphicsOutputTrace(string line)
    {
        _traceService.Write("screen.output", SanitizeForLog(line));
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "graphics",
            EventName = "screen.output",
            Source = nameof(GraphicsHostService),
            TextLength = line.Length,
            TextPreview = line
        });
    }

    private void FlushPendingGraphicsTraceLine(bool logEmpty)
    {
        if (_pendingGraphicsTraceLine.Length == 0 && !logEmpty)
        {
            return;
        }

        WriteGraphicsOutputTrace(_pendingGraphicsTraceLine.ToString());
        _pendingGraphicsTraceLine.Clear();
    }

    private static string SanitizeForLog(string text)
    {
        return text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();
    }

    #endregion

    #region Private: Dispatcher

    private void InvokeOnDispatcher(Action action, DispatcherPriority priority = DispatcherPriority.Send)
    {
        _invokeOnDispatcher(action, priority);
    }

    #endregion

    #region Private: Property change

    private void RaiseGraphicsPropertyChanged()
    {
        OnPropertyChanged(nameof(IsScreenActive));
        OnPropertyChanged(nameof(IsCanvasActive));
        OnPropertyChanged(nameof(IsScreenOutputActive));
        OnPropertyChanged(nameof(ScreenMode));
        OnPropertyChanged(nameof(ScreenWidth));
        OnPropertyChanged(nameof(ScreenHeight));
    }

    #endregion
}
