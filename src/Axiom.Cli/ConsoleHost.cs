using Axiom.Runtime;

namespace Axiom.Cli;

/// <summary>
/// Исключение пробрасывается консольным хостом при попытке программы войти в графический режим.
/// До стандартного механизма рантайм-диагностик из хоста добраться нельзя, поэтому CLI ловит
/// исключение на верхнем уровне и превращает его в понятную диагностику в stderr.
/// </summary>
internal sealed class ConsoleGraphicsNotSupportedException : Exception
{
    public ConsoleGraphicsNotSupportedException()
        : base("Графический режим не поддерживается при консольном запуске: операторы SCREEN/CANVAS и графические команды доступны в Axiom IDE (WPF).")
    {
    }
}

/// <summary>
/// Консольный хост исполнения: PRINT пишет в stdout, INPUT читает строку из stdin
/// (промпт печатается в stdout), часы и случайные числа — настоящие системные.
/// Клавиатура и мышь не реализуются сознательно: адаптер пайплайна деградирует их
/// сам (INKEY — пустая строка, KEYDOWN — FALSE, мышь — нули). Любая попытка войти
/// в графику завершается понятной диагностикой, а не тихим no-op.
/// </summary>
internal sealed class ConsoleHost : IInputProvider, IOutputWriter, IRuntimeClock, IRuntimeRandom, IRuntimeCancellationProvider, IGraphicsHost
{
    private readonly TextWriter _stdout;
    private readonly TextReader _stdin;
    private volatile bool _cancellationRequested;

    public ConsoleHost(TextWriter stdout, TextReader stdin)
    {
        _stdout = stdout;
        _stdin = stdin;
    }

    public void RequestCancellation() => _cancellationRequested = true;

    public bool IsCancellationRequested => _cancellationRequested;

    public void WriteOutput(string text) => _stdout.Write(text);

    public string ReadInput(InputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrEmpty(request.PromptText))
        {
            _stdout.Write(request.PromptText);
        }

        return _stdin.ReadLine() ?? string.Empty;
    }

    public DateTimeOffset GetLocalNow() => DateTimeOffset.Now;

    public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Local;

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
    {
        if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Local;
            return true;
        }

        if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Utc;
            return true;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out timeZone!);
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();

    // ConsoleHost не владеет состоянием генератора (Random.Shared), поэтому
    // RANDOMIZE в headless-режиме лишь фиксирует договор: Reseed недоступен.
    public void Reseed(int seed) { }

    // ---- Графика: вход в графический режим и любые мутации кадра недоступны в консоли ----

    public void ActivateScreen(int mode) => throw new ConsoleGraphicsNotSupportedException();

    public void ActivateCanvas(long width, long height) => throw new ConsoleGraphicsNotSupportedException();

    public void BeginFrame() => throw new ConsoleGraphicsNotSupportedException();

    public void PresentFrame() => throw new ConsoleGraphicsNotSupportedException();

    public void ClearFrame(int color) => throw new ConsoleGraphicsNotSupportedException();

    public void ClearGraphics() => throw new ConsoleGraphicsNotSupportedException();

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawCanvasCircle(long x, long y, long radius, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void FillCanvasCircle(long x, long y, long radius, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawCanvasText(string text, long x, long y, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawRect(long x, long y, long width, long height, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void FillRect(long x, long y, long width, long height, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long x, long y, int width, int height) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height) => throw new ConsoleGraphicsNotSupportedException();

    public void WriteGraphicsText(string text, bool newLine) => throw new ConsoleGraphicsNotSupportedException();

    public void SetPixel(long x, long y, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void SetAbsolutePixel(long x, long y, int color) => throw new ConsoleGraphicsNotSupportedException();

    public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0) => throw new ConsoleGraphicsNotSupportedException();

    public void LocateText(int row, int column) => throw new ConsoleGraphicsNotSupportedException();

    public void Paint(GraphicsPaintRequest request) => throw new ConsoleGraphicsNotSupportedException();

    public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind) => throw new ConsoleGraphicsNotSupportedException();

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
        => throw new ConsoleGraphicsNotSupportedException();

    public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates) => throw new ConsoleGraphicsNotSupportedException();

    public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates) => throw new ConsoleGraphicsNotSupportedException();

    public void ResetViewport() => throw new ConsoleGraphicsNotSupportedException();

    public void ResetWindow() => throw new ConsoleGraphicsNotSupportedException();

    public void ResetPalette() => throw new ConsoleGraphicsNotSupportedException();

    public void SetPaletteColor(int index, int color) => throw new ConsoleGraphicsNotSupportedException();

    public bool CopyScreenPage(int sourcePage, int destinationPage) => throw new ConsoleGraphicsNotSupportedException();

    public void DeactivateScreen()
    {
    }

    public void SetGraphicsCurrentPosition(long x, long y)
    {
    }

    public void SetCanvasCamera(long x, long y)
    {
    }

    public void ResetCanvasCamera()
    {
    }

    public void SetTextViewport(int topRow, int bottomRow) => throw new ConsoleGraphicsNotSupportedException();

    public void ResetTextViewport()
    {
    }

    // Загрузка ресурсов графики: null передаётся рантайму, который выдаёт свою штатную диагностику.
    public RuntimeImageValue? LoadImage(string path) => null;

    public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight) => null;

    public RuntimeTilesetValue? LoadTileset(string path, int tileWidth, int tileHeight, int margin = 0, int spacing = 0) => null;

    public RuntimeTilemapValue? LoadTilemap(string path) => null;
}
