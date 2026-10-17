namespace Axiom.Runtime.Tests;

public sealed class GraphicsAuditRepairTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(16385, 1)]
    [InlineData(4097, 4096)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void ResourceLimitsRejectBeforeAllocation(int width, int height)
    {
        Assert.False(GraphicsResourceLimits.IsSupported(width, height));
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphicsResourceLimits.Validate(width, height));
    }

    [Fact]
    public void ResourceLimitsAcceptBoundaryWithoutAllocatingTheSurface()
    {
        Assert.Equal(16777216, GraphicsResourceLimits.Validate(16384, 1024));
        Assert.Equal(16777216, GraphicsResourceLimits.Validate(4096, 4096));
    }

    [Fact]
    public void CancelledTilemapLoadDoesNotOpenAFileOrLoadImages()
    {
        Assert.Throws<OperationCanceledException>(() => RuntimeTilemapValue.LoadFromTiledJson("unused.tmj",
            _ => throw new InvalidOperationException("Загрузка изображения после отмены."), () => true));
    }

    [Fact]
    public void CancelledFloodFillStopsBeforeTraversingTheSurface()
    {
        var writes = 0;
        GraphicsFloodFill.Paint(GraphicsPaintRequest.CreatePattern(0, 0, [0], 9, 6, 0),
            (_, _) => 0, (_, _, _) => writes++,
            (x, y, w, h) => x >= 0 && y >= 0 && x < w && y < h, 1024, 1024,
            () => writes >= 1024);
        Assert.Equal(1024, writes);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void LargeEllipsesKeepEveryCardinalTangentAndFilterOtherArcs(int side)
    {
        const long rx = 1000000, ry = 2000000;
        var cx = side switch { 0 => -rx, 2 => rx, _ => 10 };
        var cy = side switch { 1 => ry, 3 => -ry, _ => 10 };
        var tangent = side switch { 0 or 2 => (0L, 10L), _ => (10L, 0L) };
        var angle = side * Math.PI / 2;
        var pixels = new HashSet<(long X, long Y)>();
        GraphicsEllipseRasterizer.Rasterize(cx, cy, rx, ry, false, angle, 0.01, 32, 32,
            (x, y) => pixels.Add((x, y)));
        Assert.Contains(tangent, pixels);
        pixels.Clear();
        GraphicsEllipseRasterizer.Rasterize(cx, cy, rx, ry, false, (angle + Math.PI) % Math.Tau, 0.01, 32, 32,
            (x, y) => pixels.Add((x, y)));
        Assert.Empty(pixels);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PatternPaintCrossesPixelsAlreadyMatchingThePattern(int seedX)
    {
        var pixels = new int[64];
        var writes = new int[64];
        GraphicsFloodFill.Paint(GraphicsPaintRequest.CreatePattern(seedX, 0, [170, 85], 9, 6, 0),
            (x, y) => pixels[y * 8 + x],
            (x, y, c) => { pixels[y * 8 + x] = c; writes[y * 8 + x]++; },
            (x, y, w, h) => x >= 0 && y >= 0 && x < w && y < h, 8, 8);
        Assert.Equal(32, pixels.Count(c => c == 6));
        Assert.All(writes, n => Assert.Equal(1, n));
    }

    [Theory]
    [InlineData(10000)]
    [InlineData(1000000)]
    public void LargeCircleKeepsTheVisibleTangent(long radius)
    {
        var pixels = new HashSet<(long X, long Y)>();
        GraphicsEllipseRasterizer.Rasterize(-radius, 10, radius, radius, true, 0, Math.Tau, 32, 32,
            (x, y) => pixels.Add((x, y)));
        Assert.Contains((0L, 10L), pixels);
        Assert.All(pixels, p => Assert.True(p.X >= 0 && p.Y >= 0 && p.X < 32 && p.Y < 32));
    }

    [Fact]
    public void ImageRejectsOverflowedAreaBeforeAcceptingPixels() =>
        Assert.ThrowsAny<ArgumentException>(() => new RuntimeImageValue(65536, 65536, Array.Empty<uint>()));
}
