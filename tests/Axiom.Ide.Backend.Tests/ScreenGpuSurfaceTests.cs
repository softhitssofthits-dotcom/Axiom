using Axiom.Ide.Graphics;
using Axiom.Runtime;

namespace Axiom.Ide.Backend.Tests;

public sealed class ScreenGpuSurfaceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ViewportBorderLinesStayWithinTheirAbsoluteRectangle(bool warp)
    {
        using var gpu = new CanvasGpuSurface(320, 200, forceSoftware: warp);
        gpu.EnableScreenPages();
        gpu.SetScreenPalette(9, 196);
        gpu.FillIndexed(20, 40, 100, 120, 3);
        gpu.DrawIndexedLine(20, 40, 100, 40, 9, 0, 0, 319, 199);
        gpu.DrawIndexedLine(100, 40, 100, 120, 9, 0, 0, 319, 199);
        gpu.DrawIndexedLine(100, 120, 20, 120, 9, 0, 0, 319, 199);
        gpu.DrawIndexedLine(20, 120, 20, 40, 9, 0, 0, 319, 199);
        gpu.PublishScreen([]);
        var pixels = gpu.ReadPublished();
        for (var y = 0; y < 200; y++)
            for (var x = 0; x < 320; x++)
            {
                var inside = x >= 20 && x <= 100 && y >= 40 && y <= 120;
                var border = inside && (x == 20 || x == 100 || y == 40 || y == 120);
                var expected = border ? 0xFFFF0000u : ScreenFramebuffer.MapColorToBgra(inside ? 3 : 0);
                Assert.True(pixels[y * 320 + x] == expected, $"({x},{y}): {pixels[y * 320 + x]:X8}, ожидалось {expected:X8}");
            }
    }

    [Fact]
    public void UnchangedTextLayerIsReusedAndPaletteChangesInvalidateIt()
    {
        using var gpu = new CanvasGpuSurface(24, 24, true);
        gpu.EnableScreenPages();
        ScreenGpuTextCell[] text = [new(0, 0, 'Ж', 12, null)];
        gpu.PublishScreen(text);
        gpu.FillIndexed(0, 0, 23, 23, 4); gpu.PublishScreen(text);
        Assert.Equal(1, gpu.ScreenTextCompositionCount);
        gpu.SetScreenPalette(12, 46); gpu.PublishScreen(text);
        Assert.Equal(2, gpu.ScreenTextCompositionCount);
        var expected = new ScreenFramebuffer(24, 24);
        expected.FillRectangle(0, 0, 23, 23, 4); expected.SetPaletteColor(12, 46);
        var buffer = new ScreenTextBuffer(); buffer.Write(1, 1, "Ж", 12, null);
        Assert.Equal(expected.CreateSnapshot(buffer, 12, null).BgraPixels, gpu.ReadPublished());
    }

    [Fact]
    public void ExtremeTransformedLineKeepsVisiblePixelsWithoutSignedProductOverflow()
    {
        using var gpu = new CanvasGpuSurface(8, 8, true);
        gpu.EnableScreenPages();
        gpu.DrawIndexedLine(long.MinValue + 1, long.MinValue + 1, long.MaxValue - 1, long.MaxValue - 1, 12, 0, 0, 7, 7);
        Assert.Equal(12, gpu.GetPixel(5, 5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PagesPaletteAndCopyPreserveIndicesAndTrueColor(bool warp)
    {
        using var gpu = new CanvasGpuSurface(16, 12, forceSoftware: warp);
        gpu.EnableScreenPages();
        Assert.Equal(1, gpu.AllocatedScreenPages);
        gpu.SelectScreenPages(1, 0);
        gpu.FillIndexed(2, 3, 2, 3, 12);
        gpu.Fill(3, 3, 3, 3, 0x123456);
        gpu.SetScreenPalette(12, 46);
        gpu.PublishScreen([]);
        Assert.Equal(12, gpu.GetPixel(2, 3));
        Assert.All(gpu.ReadPublished(), p => Assert.Equal(0xFF000000u, p));
        gpu.CopyScreenPage(1, 0);
        gpu.PublishScreen([]);
        var pixels = gpu.ReadPublished();
        Assert.Equal(0xFF00FF00u, pixels[3 * 16 + 2]);
        Assert.Equal(0xFF123456u, pixels[3 * 16 + 3]);
        gpu.SelectScreenPages(0, 0);
        gpu.ResetScreenPalette();
        gpu.PublishScreen([]);
        pixels = gpu.ReadPublished();
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), pixels[3 * 16 + 2]);
        Assert.Equal(0xFF123456u, pixels[3 * 16 + 3]);
        Assert.Equal(2, gpu.AllocatedScreenPages);
    }

    [Fact]
    public void TextIsComposedOnVisiblePageWithoutChangingPagePixelsOrReadingFrames()
    {
        using var gpu = new CanvasGpuSurface(24, 24, true);
        gpu.EnableScreenPages();
        gpu.SelectScreenPages(1, 0);
        gpu.FillIndexed(0, 0, 23, 23, 4);
        gpu.PublishScreen([new ScreenGpuTextCell(0, 0, 'Ж', 15, 1)]);
        Assert.Equal(0, gpu.ReadbackBytes);
        var expected = new ScreenFramebuffer(24, 24);
        var text = new ScreenTextBuffer(); text.Write(1, 1, "Ж", 15, 1);
        Assert.Equal(expected.CreateSnapshot(text, 15, null).BgraPixels, gpu.ReadPublished());
        Assert.Equal(4, gpu.GetPixel(0, 0));
        gpu.CopyScreenPage(1, 0); gpu.PublishScreen([]);
        Assert.All(gpu.ReadPublished(), p => Assert.Equal(ScreenFramebuffer.MapColorToBgra(4), p));
    }

    [Fact]
    public void PaletteRemapBeforeAlphaBlendingMatchesCpu()
    {
        using var gpu = new CanvasGpuSurface(8, 8, true);
        gpu.EnableScreenPages(); gpu.SetScreenPalette(4, 46);
        gpu.FillIndexed(0, 0, 7, 7, 4);
        var image = new RuntimeImageValue(1, 1, [0x80FF0000u]);
        gpu.DrawImageRegion(image, 0, 0, 1, 1, 2, 2, 1, 1, 0, 0, 7, 7);
        gpu.SetScreenPalette(4, 21); gpu.PublishScreen([]);
        var expected = new ScreenFramebuffer(8, 8);
        expected.SetPaletteColor(4, 46); expected.FillRectangle(0, 0, 7, 7, 4);
        expected.DrawImage(image, 2, 2, 1, 1, 0, 0, 7, 7);
        expected.SetPaletteColor(4, 21);
        Assert.Equal(expected.CreateSnapshot().BgraPixels, gpu.ReadPublished());
    }
}
