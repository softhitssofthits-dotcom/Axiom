namespace Axiom.Runtime.Tests;

public sealed class ScreenGraphicsRegressionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(170, 170)]
    [InlineData(85, 170)]
    public void PatternPaintWithBackgroundVisitsEachPixelOnce(int fill, int background)
    {
        const int width = 8, height = 6;
        var pixels = new int[width * height];
        var writes = new int[pixels.Length];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = ((background >> (7 - x)) & 1) == 0 ? 0 : 6;
        GraphicsFloodFill.Paint(GraphicsPaintRequest.CreatePattern(0, 0, [(byte)fill], 9, 6, 0, [(byte)background]),
            (x, y) => pixels[(int)y * width + (int)x],
            (x, y, color) =>
            {
                var offset = (int)y * width + (int)x;
                Assert.True(++writes[offset] <= 1, "Уже залитый пиксель не должен посещаться повторно.");
                pixels[offset] = color;
            },
            (x, y, w, h) => x >= 0 && y >= 0 && x < w && y < h, width, height);
        Assert.All(writes, count => Assert.Equal(1, count));
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                Assert.Equal(((fill >> (7 - x)) & 1) == 0 ? 0 : 6, pixels[y * width + x]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ShortCircleArcsKeepTheirCardinalPoint(int quadrant)
    {
        var pixels = new HashSet<(long X, long Y)>();
        var angle = quadrant * Math.PI / 2;
        GraphicsEllipseRasterizer.Rasterize(100, 100, 60, 60, false, angle, 0.01, 240, 240,
            (x, y) => pixels.Add((x, y)));
        var expected = quadrant switch { 0 => (160L, 100L), 1 => (100L, 40L), 2 => (40L, 100L), _ => (100L, 160L) };
        Assert.Contains(expected, pixels);
    }

    [Theory]
    [InlineData(GraphicsLineKind.Normal)]
    [InlineData(GraphicsLineKind.Box)]
    [InlineData(GraphicsLineKind.FilledBox)]
    public void InvisibleGeometryDoesNotIterateOverItsOriginalSize(GraphicsLineKind kind)
    {
        var host = new PixelHost();
        GraphicsHostDrawing.DrawLine(host, 1000, 1000, 1_000_000_000, 1_000_000_000, 12, kind);
        Assert.Empty(host.Pixels);
        Assert.Equal((1_000_000_000L, 1_000_000_000L), host.Position);
    }

    [Fact]
    public void ClippedLinesKeepOriginalBresenhamPixelsInEveryDirection()
    {
        for (var x1 = -5; x1 <= 13; x1 += 3)
        for (var y1 = -5; y1 <= 13; y1 += 3)
        for (var x2 = -5; x2 <= 13; x2 += 3)
        for (var y2 = -5; y2 <= 13; y2 += 3)
        {
            var host = new PixelHost();
            GraphicsHostDrawing.DrawLine(host, x1, y1, x2, y2, 12, GraphicsLineKind.Normal);
            var expected = OriginalLine(x1, y1, x2, y2).Where(p => p.X >= 2 && p.X <= 5 && p.Y >= 1 && p.Y <= 4).ToHashSet();
            Assert.True(expected.SetEquals(host.Pixels), $"Отсечение изменило линию ({x1},{y1})-({x2},{y2}).");
            Assert.Equal(((long)x2, (long)y2), host.Position);
        }
    }

    [Theory]
    [InlineData(GraphicsLineKind.Box)]
    [InlineData(GraphicsLineKind.FilledBox)]
    public void ClippedBoxesKeepEdgesAndFillInsideViewport(GraphicsLineKind kind)
    {
        foreach (var (x1, y1, x2, y2) in new[] { (-5, -5, 13, 13), (3, 2, 13, 13), (13, 13, 3, 2), (-5, 2, 4, 3) })
        {
            var host = new PixelHost();
            GraphicsHostDrawing.DrawLine(host, x1, y1, x2, y2, 12, kind);
            var left = Math.Min(x1, x2); var right = Math.Max(x1, x2);
            var top = Math.Min(y1, y2); var bottom = Math.Max(y1, y2);
            var expected = new HashSet<(long X, long Y)>();
            for (var y = 1; y <= 4; y++)
                for (var x = 2; x <= 5; x++)
                    if (x >= left && x <= right && y >= top && y <= bottom &&
                        (kind == GraphicsLineKind.FilledBox || x == left || x == right || y == top || y == bottom))
                        expected.Add((x, y));
            Assert.True(expected.SetEquals(host.Pixels));
            Assert.Equal(((long)x2, (long)y2), host.Position);
        }
    }

    private static IEnumerable<(long X, long Y)> OriginalLine(long x1, long y1, long x2, long y2)
    {
        var dx = Math.Abs(x2 - x1); var sx = x1 < x2 ? 1 : -1;
        var dy = -Math.Abs(y2 - y1); var sy = y1 < y2 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            yield return (x1, y1);
            if (x1 == x2 && y1 == y2) yield break;
            var doubled = 2 * error;
            if (doubled >= dy) { error += dy; x1 += sx; }
            if (doubled <= dx) { error += dx; y1 += sy; }
        }
    }

    private sealed class PixelHost : IGraphicsHost
    {
        private int _reads;
        public int ScreenWidth { get { Assert.True(++_reads < 10000, "Невидимая геометрия перебирается целиком."); return 8; } }
        public int ScreenHeight => 6;
        public bool ViewportEnabled => true;
        public bool ViewportRelativeCoordinates => false;
        public long ViewportLeft => 2;
        public long ViewportRight => 5;
        public long ViewportTop => 1;
        public long ViewportBottom => 4;
        public HashSet<(long X, long Y)> Pixels { get; } = [];
        public (long X, long Y) Position { get; private set; }
        public void SetAbsolutePixel(long x, long y, int color) => Pixels.Add((x, y));
        public void SetGraphicsCurrentPosition(long x, long y) => Position = (x, y);
    }
}
