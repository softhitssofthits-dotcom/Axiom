using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Runtime;

namespace Axiom.Ide.Services.Graphics;

public sealed partial class GraphicsHostService
{
    private List<uint>? _screenPointBatch;

    private CanvasRenderSession CreateScreenRenderer(int width, int height)
    {
        var renderer = new CanvasRenderSession(width, height, screen: true);
        var dispatcher = Dispatcher.CurrentDispatcher;
        renderer.Faulted += error => dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(_gpuRenderer, renderer)) return;
            _sessionManager.IsCancellationRequested = true;
            _inputPrompt.CancelPendingFrame();
            WriteGraphicsEvent("screen.device-error", error.Message);
            GraphicsError?.Invoke(error.Message);
        });
        return renderer;
    }

    private void DrawScreenLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
    {
        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2) ||
            !GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, x1, y1, out var ax1, out var ay1) ||
            !GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, x2, y2, out var ax2, out var ay2)) return;
        var viewport = GetEffectiveViewportCore();
        _gpuRenderer!.Enqueue(surface =>
        {
            if (kind == GraphicsLineKind.Normal)
                surface.DrawIndexedLine(ax1, ay1, ax2, ay2, color, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
            else
            {
                var left = Math.Min(ax1, ax2); var right = Math.Max(ax1, ax2);
                var top = Math.Min(ay1, ay2); var bottom = Math.Max(ay1, ay2);
                if (kind == GraphicsLineKind.FilledBox)
                    surface.FillIndexed(Math.Max(left, viewport.Left), Math.Max(top, viewport.Top),
                        Math.Min(right, viewport.Right), Math.Min(bottom, viewport.Bottom), color);
                else
                {
                    surface.DrawIndexedLine(left, top, right, top, color, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
                    surface.DrawIndexedLine(right, top, right, bottom, color, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
                    surface.DrawIndexedLine(right, bottom, left, bottom, color, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
                    surface.DrawIndexedLine(left, bottom, left, top, color, viewport.Left, viewport.Top, viewport.Right, viewport.Bottom);
                }
            }
        });
        SetGraphicsCurrentPositionCore(x2, y2);
    }

    private void DrawScreenCircle(long x, long y, double radius, int color, bool relativeCenter,
        double? start, double? end, double aspect, bool startRadius, bool endRadius)
    {
        var centerX = x; var centerY = y;
        var validCenter = relativeCenter
            ? GraphicsCoordinateValidation.TryResolvePoint(_graphicsCurrentX, _graphicsCurrentY, x, y, out centerX, out centerY)
            : GraphicsCoordinateValidation.IsSupportedCircleCenter(x, y);
        if (GraphicsCircleValidation.HasValidArguments(radius, start, end, aspect) && validCenter &&
            !_windowEnabled && aspect == 1 && start is null && end is null && !startRadius && !endRadius &&
            radius == Math.Truncate(radius) && radius <= 1024 &&
            GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(this, centerX, centerY, out var ax, out var ay) &&
            Math.Abs((double)ax) <= 32768 && Math.Abs((double)ay) <= 32768)
        {
            var viewport = GetEffectiveViewportCore();
            _gpuRenderer!.Enqueue(surface => surface.DrawIndexedCircle(ax, ay, (long)radius, color,
                viewport.Left, viewport.Top, viewport.Right, viewport.Bottom));
            SetGraphicsCurrentPositionCore(centerX, centerY);
            return;
        }
        // Сложная геометрия готовится прежним точным алгоритмом и передаётся
        // одним набором точек; CPU-кадр при этом не создаётся.
        _screenPointBatch = [];
        uint[] points;
        try
        {
            GraphicsHostDrawing.DrawCircle(this, x, y, radius, color, relativeCenter, start, end, aspect, startRadius, endRadius);
            points = _screenPointBatch.ToArray();
        }
        finally { _screenPointBatch = null; }
        _gpuRenderer!.Enqueue(surface => surface.DrawIndexedPoints(points, color));
    }

    private ScreenGpuTextCell[] CaptureScreenText()
    {
        var (first, rows, columns) = GetGraphicsTextViewport();
        var cells = new List<ScreenGpuTextCell>();
        for (var row = first - 1; row < _graphicsTextBuffer.Lines.Count && row < first - 1 + rows; row++)
        {
            var line = _graphicsTextBuffer.Lines[row];
            for (var column = 0; column < Math.Min(columns, line.Count); column++)
            {
                var cell = line[column];
                cells.Add(new ScreenGpuTextCell(column * 8, (row - first + 1) * 12, cell.Character, cell.Foreground, cell.Background));
            }
        }
        if (_inputPrompt.IsWaitingForInput)
        {
            var row = ResolveGraphicsTextBufferRow(_graphicsRow) - first;
            if (row >= 0 && row < rows)
            {
                var startX = Math.Max(0, _graphicsColumn - 1) * 8;
                var x = startX; var y = row * 12;
                foreach (var ch in _inputPrompt.CurrentPromptText + _inputPrompt.CurrentInputText)
                {
                    if (ch == '\r') continue;
                    if (ch == '\n') { x = startX; y += 12; continue; }
                    cells.Add(new ScreenGpuTextCell(x, y, ch, _graphicsForeground, _graphicsBackground));
                    x += 8;
                }
            }
        }
        return cells.ToArray();
    }
}
