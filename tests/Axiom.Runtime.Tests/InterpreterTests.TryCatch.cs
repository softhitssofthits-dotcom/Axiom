using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_TryCatchHandlesRuntimeFailureAndExposesErrorFields()
    {
        const string text = """
TRY
    PRINT 10 \ 0
    PRINT "unreachable"
CATCH err
    PRINT err.Code
    PRINT err.Line
    PRINT err.Column
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3001", "2", "14", "after"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsAfterNormalBody()
    {
        const string text = """
TRY
    PRINT "body"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["body", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_TryCatchFinallyRunsFinallyAfterHandledFailure()
    {
        const string text = """
TRY
    PRINT 10 \ 0
CATCH err
    PRINT "caught"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["caught", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementCanBeCaughtByTryCatch()
    {
        const string text = """
TRY
    ERROR "APP001", "Manual failure"
CATCH err
    PRINT err.Code
    PRINT err.Message
    PRINT err.Line
FINALLY
    PRINT "cleanup"
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["APP001", "Manual failure", "2", "cleanup", "after"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementUsesDefaultMessage()
    {
        const string text = """
TRY
    ERROR "APP002"
CATCH err
    PRINT err.Message
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Пользовательская ошибка APP002."], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementRethrowsCaughtRuntimeError()
    {
        const string text = """
TRY
    TRY
        ERROR "APP003", "Original failure"
    CATCH err
        PRINT "inner"
        ERROR err
    END TRY
CATCH outerErr
    PRINT outerErr.Code
    PRINT outerErr.Message
    PRINT outerErr.Line
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["inner", "APP003", "Original failure", "3"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsBeforeUnhandledFailureEscapes()
    {
        const string text = """
TRY
    PRINT 10 \ 0
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Equal(["cleanup"], host.Output);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_TryFinallyRunsWhenCatchBodyRethrows()
    {
        const string text = """
TRY
    TRY
        ERROR "APP010", "Original failure"
    CATCH inner
        ERROR inner
    FINALLY
        PRINT "inner-cleanup"
    END TRY
CATCH outer
    PRINT outer.Code
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["inner-cleanup", "APP010"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsWhenCatchBodyRaisesNewError()
    {
        const string text = """
TRY
    ERROR "APP011"
CATCH err
    ERROR "APP012", "Replacement failure"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Equal(["cleanup"], host.Output);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "APP012");
        Assert.Contains("Replacement failure", diagnostic.Message, StringComparison.Ordinal);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(string text, TestRuntimeHost host)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeys(string text, params string[] keys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], keys: keys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithPressedKeys(string text, params string[] pressedKeys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], pressedKeys: pressedKeys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithKeysAndPressedKeys(
        string text,
        IEnumerable<string> keys,
        IEnumerable<string> pressedKeys)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], keys: keys, pressedKeys: pressedKeys);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramWithMouseState(
        string text,
        long mouseX = -1,
        long mouseY = -1,
        IEnumerable<int>? pressedMouseButtons = null,
        IEnumerable<int>? mousePresses = null,
        IEnumerable<MouseInputEvent>? mouseEvents = null)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost([], mouseX: mouseX, mouseY: mouseY, pressedMouseButtons: pressedMouseButtons, mousePresses: mousePresses, mouseEvents: mouseEvents);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, TimeZoneInfo.Local, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        params string[] inputs)
    {
        return ExecuteProgramAt(text, localNow, localTimeZone, namedTimeZones: null, inputs);
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgramAt(
        string text,
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones,
        params string[] inputs)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var host = new TestRuntimeHost(inputs, localNow, localTimeZone, namedTimeZones);
        var result = Interpreter.Execute(program, host);
        return (program, host, result);
    }

    private static string ToBasicString(string value)
    {
        return value.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static string SetRepairSamplePaths(string text, string sourcePath, string workPath)
    {
        const string sourceDeclarationPrefix = "DIM sourcePath AS STRING = ";
        const string workDeclarationPrefix = "DIM workPath AS STRING = ";
        const string timingDeclarationPrefix = "DIM showTiming AS BOOLEAN = ";
        var sourceReplacement = sourceDeclarationPrefix + "\"" + ToBasicString(sourcePath) + "\"";
        var workReplacement = workDeclarationPrefix + "\"" + ToBasicString(workPath) + "\"";
        var timingReplacement = timingDeclarationPrefix + "FALSE";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sourcePathSet = false;
        var workPathSet = false;
        var timingSet = false;

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith(sourceDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = sourceReplacement;
                sourcePathSet = true;
                continue;
            }

            if (lines[index].StartsWith(workDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = workReplacement;
                workPathSet = true;
                continue;
            }

            if (lines[index].StartsWith(timingDeclarationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = timingReplacement;
                timingSet = true;
            }
        }

        if (!sourcePathSet || !workPathSet || !timingSet)
        {
            throw new InvalidOperationException("Could not locate source, work path, or timing declarations in repair sample.");
        }

        return string.Join("\n", lines);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Axiom.sln.");
    }

    private static string CreateUtf8Cp1251Mojibake(string text, int passes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passes);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var utf8 = Encoding.UTF8;
        var windows1251 = Encoding.GetEncoding(1251);
        var current = text;

        for (var i = 0; i < passes; i++)
        {
            current = windows1251.GetString(utf8.GetBytes(current));
        }

        return current;
    }

    private static TimeZoneInfo CreateTestSeasonalTimeZone()
    {
        var daylightDelta = TimeSpan.FromHours(1);
        var daylightTransitionStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            5,
            DayOfWeek.Sunday);
        var daylightTransitionEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0),
            10,
            5,
            DayOfWeek.Sunday);
        var adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2099, 12, 31),
            daylightDelta,
            daylightTransitionStart,
            daylightTransitionEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Axiom Runtime Tests Seasonal",
            TimeSpan.FromHours(2),
            "Axiom Runtime Tests Seasonal",
            "Axiom Runtime Tests Standard",
            "Axiom Runtime Tests Daylight",
            [adjustmentRule],
            disableDaylightSavingTime: false);
    }

    private static TimeZoneInfo CreateTestFixedTimeZone(string id, int offsetHours)
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            id,
            TimeSpan.FromHours(offsetHours),
            id,
            id);
    }

    private static IReadOnlyDictionary<string, TimeZoneInfo> CreateNamedTimeZones(params TimeZoneInfo[] timeZones)
    {
        return timeZones.ToDictionary(static timeZone => timeZone.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static RuntimeImageValue CreateTestImage(int width, int height)
    {
        var pixels = Enumerable.Repeat(0xFF0000FFu, width * height).ToArray();
        return new RuntimeImageValue(width, height, pixels);
    }

    private static RuntimeSpriteValue CreateTestSprite(int imageWidth, int imageHeight, int frameWidth, int frameHeight) =>
        new(CreateTestImage(imageWidth, imageHeight), frameWidth, frameHeight);

    private static RuntimeTilemapValue CreateTestTilemap(RuntimeImageValue image)
    {
        var tileset = new RuntimeTilesetValue(image, tileWidth: 16, tileHeight: 8);
        var cells = new[]
        {
            new RuntimeTilemapCell(1, 1, RuntimeTilemapFlipFlags.None),
            new RuntimeTilemapCell(1, 2, RuntimeTilemapFlipFlags.None),
            default,
            new RuntimeTilemapCell(1, 3, RuntimeTilemapFlipFlags.None)
        };
        var visible = new RuntimeTilemapLayer("visible", RuntimeTilemapLayerKind.Tile, 2, 2, visible: true, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells);
        var hidden = new RuntimeTilemapLayer("hidden", RuntimeTilemapLayerKind.Tile, 2, 2, visible: false, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells);
        var objects = new RuntimeTilemapLayer("objects", RuntimeTilemapLayerKind.Object, 2, 2, visible: true, opacity: 1d, offsetX: 0, offsetY: 0, parallaxX: 1d, parallaxY: 1d, cells: null);
        return new RuntimeTilemapValue(
            2,
            2,
            16,
            8,
            RuntimeTilemapRenderOrder.RightDown,
            [new RuntimeTilemapTilesetReference(1, tileset)],
            [visible, hidden, objects]);
    }

    private sealed class TestRuntimeHost : IRuntimeHost
    {
        private const int VisibleTextColumns = 40;
        private const int VisibleTextRows = 16;
        private readonly Queue<string> _inputs;
        private readonly Queue<string> _keys;
        private readonly Queue<int> _mousePresses;
        private readonly Queue<MouseInputEvent> _mouseEvents;
        private readonly HashSet<string> _pressedKeys;
        private readonly HashSet<int> _pressedMouseButtons;
        private Random _random = new();
        private readonly DateTimeOffset _localNow;
        private readonly TimeZoneInfo _localTimeZone;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _namedTimeZones;
        private readonly long _mouseX;
        private readonly long _mouseY;
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private readonly Dictionary<int, Dictionary<(long X, long Y), int>> _screenPages = [];
        private readonly int _cancelAfterReadKeyCalls;
        private readonly int _cancelAfterCancellationPolls;
        private int _readKeyCalls;
        private int _cancellationPolls;
        private bool _isScreenActive;
        private bool _isCanvasActive;
        private long _canvasWidth;
        private long _canvasHeight;
        private long _canvasCameraX;
        private long _canvasCameraY;
        private int _activeScreenPage;
        private int _visualScreenPage;
        private int _foreground = 15;
        private int _background;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;
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
        private readonly List<string> _visibleTextLines = [];
        private int _textCursorRow = 1;
        private int _textCursorColumn = 1;
        private int _textViewportTop = 1;
        private int _textViewportBottom = VisibleTextRows;
        private bool _pendingTextViewportScroll;

        public TestRuntimeHost(
            IEnumerable<string> inputs,
            DateTimeOffset? localNow = null,
            TimeZoneInfo? localTimeZone = null,
            IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null,
            IEnumerable<string>? keys = null,
            IEnumerable<string>? pressedKeys = null,
            long mouseX = -1,
            long mouseY = -1,
            IEnumerable<int>? pressedMouseButtons = null,
            IEnumerable<int>? mousePresses = null,
            IEnumerable<MouseInputEvent>? mouseEvents = null,
            int cancelAfterReadKeyCalls = int.MaxValue,
            int cancelAfterCancellationPolls = int.MaxValue)
        {
            _inputs = new Queue<string>(inputs);
            _keys = new Queue<string>(keys ?? []);
            _mousePresses = new Queue<int>(mousePresses ?? []);
            _mouseEvents = new Queue<MouseInputEvent>(mouseEvents ?? (mousePresses ?? []).Select(static buttonNumber => new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
            _pressedKeys = new HashSet<string>(
                (pressedKeys ?? []).Select(KeyboardInputNormalizer.Normalize),
                StringComparer.Ordinal);
            _pressedMouseButtons = new HashSet<int>(pressedMouseButtons ?? []);
            _localNow = localNow ?? DateTimeOffset.Now;
            _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;
            _namedTimeZones = namedTimeZones ?? new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
            _mouseX = mouseX;
            _mouseY = mouseY;
            _cancelAfterReadKeyCalls = cancelAfterReadKeyCalls;
            _cancelAfterCancellationPolls = cancelAfterCancellationPolls;
            _isScreenActive = mouseX >= 0 || mouseY >= 0 || _pressedMouseButtons.Count > 0 || _mouseEvents.Count > 0 || _mousePresses.Count > 0;
        }

        public List<InputRequest> InputRequests { get; } = [];

        public List<string> Output { get; } = [];

        public List<CircleCall> Circles { get; } = [];

        public List<LineCall> Lines { get; } = [];

        public List<GraphicsPaintRequest> PaintRequests { get; } = [];

        public List<(int Index, int Color)> PaletteColors { get; } = [];

        public Dictionary<string, RuntimeImageValue> Images { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, RuntimeSpriteValue> Sprites { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, RuntimeTilemapValue> Tilemaps { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<DrawImageCall> DrawImages { get; } = [];

        public List<DrawImageRegionCall> DrawImageRegions { get; } = [];

        public List<DrawSpriteCall> DrawSprites { get; } = [];

        public List<(long Width, long Height)> CanvasActivations { get; } = [];

        public List<int> FrameClears { get; } = [];

        public List<DrawRectCall> DrawRects { get; } = [];

        public List<FillRectCall> FillRects { get; } = [];

        public List<CanvasCameraCall> CanvasCameraCalls { get; } = [];

        public List<CanvasLineCall> CanvasDrawLines { get; } = [];

        public List<CanvasCircleCall> CanvasDrawCircles { get; } = [];

        public List<CanvasCircleCall> CanvasFillCircles { get; } = [];

        public List<CanvasTextCall> CanvasDrawTexts { get; } = [];

        public int BeginFrameCount { get; private set; }

        public int PresentFrameCount { get; private set; }

        public int ResetCanvasCameraCount { get; private set; }

        public int PaletteResets { get; private set; }

        public string GetVisibleTextLine(int row)
        {
            if (row < 1 || row > VisibleTextRows)
            {
                return string.Empty;
            }

            return row <= _visibleTextLines.Count ? _visibleTextLines[row - 1] : string.Empty;
        }

        public string ReadInput(InputRequest request)
        {
            InputRequests.Add(request);
            return _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;
        }

        public string ReadKey()
        {
            _readKeyCalls++;
            return _keys.Count > 0 ? _keys.Dequeue() : string.Empty;
        }

        public bool IsKeyDown(string keyText) => _pressedKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => _mouseX;

        public long GetMouseY() => _mouseY;

        public bool IsMouseButtonDown(int buttonNumber) => _pressedMouseButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress() => _mousePresses.Count > 0 ? _mousePresses.Dequeue() : 0;

        public MouseInputEvent ReadMouseEvent() => _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;

        public bool IsScreenActive => _isScreenActive;

        public bool IsCanvasActive => _isCanvasActive;

        public int ActiveScreenPage => _activeScreenPage;

        public int VisualScreenPage => _visualScreenPage;

        public int ScreenMode => _isScreenActive ? 1 : 0;

        public int ScreenWidth => _isScreenActive ? 320 : _isCanvasActive ? (int)_canvasWidth : 0;

        public int ScreenHeight => _isScreenActive ? 200 : _isCanvasActive ? (int)_canvasHeight : 0;

        public int CurrentForegroundColor => _foreground;

        public int CurrentBackgroundColor => _background;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

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

        public void ActivateScreen(int mode)
        {
            ActivateScreen(mode, activePage: 0, visualPage: 0);
        }

        public void ActivateScreen(int mode, int activePage, int visualPage)
        {
            _isScreenActive = mode != 0;
            _isCanvasActive = false;
            _canvasWidth = 0;
            _canvasHeight = 0;
            _activeScreenPage = _isScreenActive ? activePage : 0;
            _visualScreenPage = _isScreenActive ? visualPage : 0;
            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages.TryAdd(_visualScreenPage, []);
                _pixels.Clear();
                foreach (var item in _screenPages[_activeScreenPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            _visibleTextLines.Clear();
            _textCursorRow = 1;
            _textCursorColumn = 1;
        }

        public void ActivateCanvas(long width, long height)
        {
            _isScreenActive = false;
            _isCanvasActive = true;
            _canvasWidth = width;
            _canvasHeight = height;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            _pixels.Clear();
            _screenPages.Clear();
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            CanvasActivations.Add((width, height));
        }

        public void BeginFrame()
        {
            BeginFrameCount++;
        }

        public void PresentFrame()
        {
            PresentFrameCount++;
        }

        public void ClearFrame(int color)
        {
            FrameClears.Add(color);
            if (!_isCanvasActive)
            {
                return;
            }

            var paletteIndex = MapCanvasColorToPaletteIndex(color);
            _pixels.Clear();
            for (var y = 0L; y < _canvasHeight; y++)
            {
                for (var x = 0L; x < _canvasWidth; x++)
                {
                    _pixels[(x, y)] = paletteIndex;
                }
            }
        }

        public void DrawRect(long x, long y, long width, long height, int color)
        {
            DrawRects.Add(new DrawRectCall(x, y, width, height, color));
        }

        public void FillRect(long x, long y, long width, long height, int color)
        {
            FillRects.Add(new FillRectCall(x, y, width, height, color));
            if (_isCanvasActive)
            {
                RasterizeCanvasFillRect(x, y, width, height, color);
            }
        }

        public void SetCanvasCamera(long x, long y)
        {
            CanvasCameraCalls.Add(new CanvasCameraCall(x, y));
            _canvasCameraX = x;
            _canvasCameraY = y;
        }

        public void ResetCanvasCamera()
        {
            ResetCanvasCameraCount++;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
        }

        public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
        {
            CanvasDrawLines.Add(new CanvasLineCall(x1, y1, x2, y2, color));
        }

        public void DrawCanvasCircle(long x, long y, long radius, int color)
        {
            CanvasDrawCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void FillCanvasCircle(long x, long y, long radius, int color)
        {
            CanvasFillCircles.Add(new CanvasCircleCall(x, y, radius, color));
        }

        public void DrawCanvasText(string text, long x, long y, int color)
        {
            CanvasDrawTexts.Add(new CanvasTextCall(text, x, y, color));
        }

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isCanvasActive = false;
            _canvasWidth = 0;
            _canvasHeight = 0;
            _canvasCameraX = 0;
            _canvasCameraY = 0;
            _activeScreenPage = 0;
            _visualScreenPage = 0;
            ResetViewport();
            ResetWindow();
            ResetTextViewport();
            _visibleTextLines.Clear();
        }

        public void ClearGraphics()
        {
            _pixels.Clear();
            if (_isScreenActive)
            {
                _screenPages[_activeScreenPage] = [];
            }
            _visibleTextLines.Clear();
            _textCursorRow = _textViewportTop;
            _textCursorColumn = 1;
        }

        public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
        {
            _foreground = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _background = background;
                    _isTextBackgroundTransparent = false;
                    break;
                case TextBackgroundMode.Transparent:
                    _isTextBackgroundTransparent = true;
                    break;
                default:
                    break;
            }
        }

        public void LocateText(int row, int column)
        {
            _textCursorRow = Math.Max(1, row);
            _textCursorColumn = Math.Max(1, column);
            _pendingTextViewportScroll = false;
        }

        public void ResetPalette()
        {
            PaletteResets++;
        }

        public void SetPaletteColor(int index, int color)
        {
            PaletteColors.Add((index, color));
        }

        public RuntimeImageValue? LoadImage(string path) => Images.TryGetValue(path, out var image) ? image : null;

        public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight) =>
            Sprites.TryGetValue(path, out var sprite)
                ? sprite
                : Images.TryGetValue(path, out var image)
                    ? new RuntimeSpriteValue(image, frameWidth, frameHeight)
                    : null;

        public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
        {
            DrawImages.Add(new DrawImageCall(image, x, y, width, height));
        }

        public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height)
        {
            DrawImageRegions.Add(new DrawImageRegionCall(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height));
        }

        public RuntimeTilemapValue? LoadTilemap(string path) => Tilemaps.TryGetValue(path, out var tilemap) ? tilemap : null;

        public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
        {
            DrawSprites.Add(new DrawSpriteCall(sprite, frame, x, y, width, height));
        }

        public bool SetTextViewport(int topRow, int bottomRow)
        {
            if (!_isScreenActive || topRow < 1 || bottomRow < topRow || bottomRow > VisibleTextRows)
            {
                return false;
            }

            _textViewportTop = topRow;
            _textViewportBottom = bottomRow;
            _textCursorRow = topRow;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
            return true;
        }

        public void ResetTextViewport()
        {
            _textViewportTop = 1;
            _textViewportBottom = VisibleTextRows;
            _textCursorRow = 1;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
        }

        public void SetPixel(long x, long y, int color)
        {
            if (_isCanvasActive)
            {
                if (!TryTranslateCanvasWorldRectangleToScreen(x, y, 1, 1, out var left, out var top, out _, out _))
                {
                    return;
                }

                _pixels[(left, top)] = Math.Abs(color) % 256;
                _currentGraphicsX = x;
                _currentGraphicsY = y;
                return;
            }

            if (!TryTranslateLogicalToAbsolute(x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            _pixels[(absoluteX, absoluteY)] = color;
            if (_isScreenActive)
            {
                _screenPages.TryAdd(_activeScreenPage, []);
                _screenPages[_activeScreenPage][(absoluteX, absoluteY)] = color;
            }
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetAbsolutePixel(long x, long y, int color)
        {
            if (IsInsideScreen(x, y))
            {
                _pixels[(x, y)] = color;
                if (_isScreenActive)
                {
                    _screenPages.TryAdd(_activeScreenPage, []);
                    _screenPages[_activeScreenPage][(x, y)] = color;
                }
            }
        }

        public bool CopyScreenPage(int sourcePage, int destinationPage)
        {
            if (!_isScreenActive || sourcePage is < 0 or > 3 || destinationPage is < 0 or > 3)
            {
                return false;
            }

            _screenPages.TryAdd(sourcePage, []);
            _screenPages[destinationPage] = new Dictionary<(long X, long Y), int>(_screenPages[sourcePage]);
            if (destinationPage == _activeScreenPage)
            {
                _pixels.Clear();
                foreach (var item in _screenPages[destinationPage])
                {
                    _pixels[item.Key] = item.Value;
                }
            }

            return true;
        }

        public int GetPagePixel(int page, long x, long y) =>
            _screenPages.TryGetValue(page, out var pixels) && pixels.TryGetValue((x, y), out var color) ? color : 0;

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public int GetPixel(long x, long y)
        {
            if (_isCanvasActive)
            {
                return _pixels.TryGetValue((x, y), out var canvasColor) ? canvasColor : 0;
            }

            if (!TryTranslateLogicalToAbsolute(x, y, out var absoluteX, out var absoluteY))
            {
                return 0;
            }

            return _pixels.TryGetValue((absoluteX, absoluteY), out var color) ? color : 0;
        }

        public int GetAbsolutePixel(long x, long y) =>
            _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
        {
            if ((!_isScreenActive && !_isCanvasActive) || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideActiveGraphicsSurface(absoluteLeft, absoluteTop) || !IsInsideActiveGraphicsSurface(absoluteRight, absoluteBottom))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            if (fillColor.HasValue)
            {
                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    for (var x = absoluteLeft; x <= absoluteRight; x++)
                    {
                        _pixels[(x, y)] = fillColor.Value;
                    }
                }
            }

            if (borderColor.HasValue)
            {
                for (var x = absoluteLeft; x <= absoluteRight; x++)
                {
                    _pixels[(x, absoluteTop)] = borderColor.Value;
                    _pixels[(x, absoluteBottom)] = borderColor.Value;
                }

                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    _pixels[(absoluteLeft, y)] = borderColor.Value;
                    _pixels[(absoluteRight, y)] = borderColor.Value;
                }
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
            return true;
        }

        public void ResetViewport()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _viewportEnabled = false;
            _viewportRelativeCoordinates = false;
            _viewportLeft = 0;
            _viewportTop = 0;
            _viewportRight = ScreenWidth - 1L;
            _viewportBottom = ScreenHeight - 1L;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
        {
            var viewport = _viewportEnabled
                ? new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates)
                : new GraphicsViewport(0, 0, ScreenWidth - 1L, ScreenHeight - 1L, UsesRelativeCoordinates: false);
            if (!_isScreenActive || viewport.Width < 2 || viewport.Height < 2)
            {
                return false;
            }

            if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = true;
            _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
            _windowLeft = window.Left;
            _windowTop = window.Top;
            _windowRight = window.Right;
            _windowBottom = window.Bottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }

            return true;
        }

        public void ResetWindow()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = false;
            _windowUsesScreenCoordinates = false;
            _windowLeft = 0d;
            _windowTop = 0d;
            _windowRight = 0d;
            _windowBottom = 0d;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
        {
            Lines.Add(new LineCall(x1, y1, x2, y2, color, kind));
            GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
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
            Circles.Add(new CircleCall(x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius));
            GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, startAngle, endAngle, aspect, drawStartRadius, drawEndRadius);
        }

        public void Paint(GraphicsPaintRequest request)
        {
            PaintRequests.Add(request);
            GraphicsHostDrawing.Paint(this, request);
            _currentGraphicsX = request.X;
            _currentGraphicsY = request.Y;
        }

        public void WriteGraphicsText(string text, bool newLine)
        {
            NormalizeViewportCursorBeforeWrite();
            WriteVisibleText(text);
            if (newLine)
            {
                Output.Add(text);
                AdvanceTextCursorAfterNewLine();
            }
            else
            {
                _textCursorColumn += text.Length;
            }
        }

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }

        public bool IsCancellationRequested =>
            _readKeyCalls >= _cancelAfterReadKeyCalls
            || ++_cancellationPolls >= _cancelAfterCancellationPolls;

        bool IRuntimeCancellationProvider.IsCancellationRequested => IsCancellationRequested;

        public DateTimeOffset GetLocalNow() => _localNow;

        public TimeZoneInfo GetLocalTimeZone() => _localTimeZone;

        public double NextRandomDouble() => _random.NextDouble();

        public void Reseed(int seed) => _random = new Random(seed);

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = _localTimeZone;
                return true;
            }

            if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = TimeZoneInfo.Utc;
                return true;
            }

            return _namedTimeZones.TryGetValue(id, out timeZone!);
        }

        private bool TryTranslateLogicalToAbsolute(long x, long y, out long absoluteX, out long absoluteY)
            => GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out absoluteX, out absoluteY);

        private void WriteVisibleText(string text)
        {
            if (_textCursorRow < 1 || _textCursorRow > VisibleTextRows)
            {
                return;
            }

            EnsureVisibleTextRow(_textCursorRow);
            var line = _visibleTextLines[_textCursorRow - 1];
            var columnIndex = Math.Max(0, _textCursorColumn - 1);
            if (columnIndex >= VisibleTextColumns)
            {
                return;
            }

            if (line.Length < columnIndex)
            {
                line = line.PadRight(columnIndex);
            }

            var buffer = new StringBuilder(line);
            while (buffer.Length < columnIndex)
            {
                buffer.Append(' ');
            }

            var visibleLength = Math.Min(text.Length, VisibleTextColumns - columnIndex);
            for (var index = 0; index < visibleLength; index++)
            {
                var targetIndex = columnIndex + index;
                if (targetIndex < buffer.Length)
                {
                    buffer[targetIndex] = text[index];
                }
                else
                {
                    buffer.Append(text[index]);
                }
            }

            _visibleTextLines[_textCursorRow - 1] = buffer.ToString().TrimEnd();
        }

        private void AdvanceTextCursorAfterNewLine()
        {
            _textCursorColumn = 1;
            if (_textCursorRow >= _textViewportTop && _textCursorRow <= _textViewportBottom)
            {
                if (_textCursorRow < _textViewportBottom)
                {
                    _textCursorRow++;
                    _pendingTextViewportScroll = false;
                    return;
                }

                _textCursorRow = _textViewportBottom + 1;
                _pendingTextViewportScroll = true;
                return;
            }

            _textCursorRow++;
            _pendingTextViewportScroll = false;
        }

        private void ScrollViewportUp()
        {
            EnsureVisibleTextRow(_textViewportBottom);
            for (var row = _textViewportTop; row < _textViewportBottom; row++)
            {
                _visibleTextLines[row - 1] = _visibleTextLines[row];
            }

            _visibleTextLines[_textViewportBottom - 1] = string.Empty;
        }

        private void EnsureVisibleTextRow(int row)
        {
            while (_visibleTextLines.Count < row)
            {
                _visibleTextLines.Add(string.Empty);
            }
        }

        private void NormalizeViewportCursorBeforeWrite()
        {
            if (!_pendingTextViewportScroll)
            {
                return;
            }

            ScrollViewportUp();
            _textCursorRow = _textViewportBottom;
            _textCursorColumn = 1;
            _pendingTextViewportScroll = false;
        }

        private (long X, long Y) ResolveCurrentAbsolutePosition()
            => GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY)
                ? (absoluteX, absoluteY)
                : (0L, 0L);

        private void RasterizeCanvasFillRect(long x, long y, long width, long height, int color)
        {
            if (!TryTranslateCanvasWorldRectangleToScreen(x, y, width, height, out var left, out var top, out var right, out var bottom))
            {
                return;
            }

            var paletteIndex = MapCanvasColorToPaletteIndex(color);
            for (var pixelY = top; pixelY <= bottom; pixelY++)
            {
                for (var pixelX = left; pixelX <= right; pixelX++)
                {
                    _pixels[(pixelX, pixelY)] = paletteIndex;
                }
            }
        }

        private bool TryTranslateCanvasWorldRectangleToScreen(long x, long y, long width, long height, out long left, out long top, out long right, out long bottom)
        {
            left = 0;
            top = 0;
            right = -1;
            bottom = -1;
            if (!_isCanvasActive || width <= 0 || height <= 0)
            {
                return false;
            }

            var viewportLeft = _viewportEnabled ? _viewportLeft : 0L;
            var viewportTop = _viewportEnabled ? _viewportTop : 0L;
            var viewportRight = _viewportEnabled ? _viewportRight : _canvasWidth - 1;
            var viewportBottom = _viewportEnabled ? _viewportBottom : _canvasHeight - 1;
            left = viewportLeft + (x - _canvasCameraX);
            top = viewportTop + (y - _canvasCameraY);
            right = left + width - 1;
            bottom = top + height - 1;
            left = Math.Max(left, viewportLeft);
            top = Math.Max(top, viewportTop);
            right = Math.Min(right, viewportRight);
            bottom = Math.Min(bottom, viewportBottom);
            left = Math.Max(left, 0);
            top = Math.Max(top, 0);
            right = Math.Min(right, _canvasWidth - 1);
            bottom = Math.Min(bottom, _canvasHeight - 1);
            return left <= right && top <= bottom;
        }

        private static int MapCanvasColorToPaletteIndex(int color)
        {
            var red = (color >> 16) & 0xFF;
            var green = (color >> 8) & 0xFF;
            var blue = color & 0xFF;
            var bestIndex = 0;
            var bestDistance = int.MaxValue;

            for (var index = 0; index < 256; index++)
            {
                var (paletteRed, paletteGreen, paletteBlue) = MapPaletteIndexToRgb(index);
                var distance =
                    Square(red - paletteRed) +
                    Square(green - paletteGreen) +
                    Square(blue - paletteBlue);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = index;
                if (distance == 0)
                {
                    break;
                }
            }

            return bestIndex;
        }

        private static (int Red, int Green, int Blue) MapPaletteIndexToRgb(int colorIndex)
        {
            var normalized = Math.Abs(colorIndex) % 256;
            return normalized switch
            {
                0 => (0, 0, 0),
                1 => (0, 0, 170),
                2 => (0, 170, 0),
                3 => (0, 170, 170),
                4 => (170, 0, 0),
                5 => (170, 0, 170),
                6 => (170, 85, 0),
                7 => (170, 170, 170),
                8 => (85, 85, 85),
                9 => (85, 85, 255),
                10 => (85, 255, 85),
                11 => (85, 255, 255),
                12 => (255, 85, 85),
                13 => (255, 85, 255),
                14 => (255, 255, 85),
                15 => (255, 255, 255),
                >= 16 and <= 231 => MapRgbCubeIndexToRgb(normalized),
                _ => MapGrayIndexToRgb(normalized)
            };
        }

        private static (int Red, int Green, int Blue) MapRgbCubeIndexToRgb(int colorIndex)
        {
            var cubeIndex = colorIndex - 16;
            var red = cubeIndex / 36;
            var green = cubeIndex / 6 % 6;
            var blue = cubeIndex % 6;
            return (MapRgbCubeComponent(red), MapRgbCubeComponent(green), MapRgbCubeComponent(blue));
        }

        private static (int Red, int Green, int Blue) MapGrayIndexToRgb(int colorIndex)
        {
            var component = 8 + ((colorIndex - 232) * 10);
            return (component, component, component);
        }

        private static int MapRgbCubeComponent(int component) => component == 0 ? 0 : 55 + (component * 40);

        private static int Square(int value) => value * value;

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < ScreenWidth && y < ScreenHeight;

        private bool IsInsideActiveGraphicsSurface(long x, long y) => x >= 0 && y >= 0 && x < ScreenWidth && y < ScreenHeight;
    }

    private sealed record CircleCall(
        long X,
        long Y,
        double Radius,
        int Color,
        bool RelativeCenter,
        double? StartAngle,
        double? EndAngle,
        double Aspect,
        bool DrawStartRadius,
        bool DrawEndRadius);

    private sealed record LineCall(
        long X1,
        long Y1,
        long X2,
        long Y2,
        int Color,
        GraphicsLineKind Kind);

    private sealed record DrawImageCall(
        RuntimeImageValue Image,
        long X,
        long Y,
        int Width,
        int Height);

    private sealed record DrawImageRegionCall(
        RuntimeImageValue Image,
        int SourceX,
        int SourceY,
        int SourceWidth,
        int SourceHeight,
        long X,
        long Y,
        int Width,
        int Height);

    private sealed record DrawSpriteCall(
        RuntimeSpriteValue Sprite,
        int Frame,
        long X,
        long Y,
        int Width,
        int Height);

    private sealed record DrawRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record FillRectCall(long X, long Y, long Width, long Height, int Color);

    private sealed record CanvasCameraCall(long X, long Y);

    private sealed record CanvasLineCall(long X1, long Y1, long X2, long Y2, int Color);

    private sealed record CanvasCircleCall(long X, long Y, long Radius, int Color);

    private sealed record CanvasTextCall(string Text, long X, long Y, int Color);
}
