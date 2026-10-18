using Axiom.Ide.Graphics;

namespace Axiom.Ide.Backend.Tests;

public sealed class ScreenRendererGpuTests
{
    [Fact]
    public void SwitchingSmoothingRedrawsSamePublishedPixelsWithCurrentFilter()
    {
        using var surface = new ScreenRendererTestSurface(3, 3);
        var frame = new ScreenFramebufferSnapshot(2, 2,
            [0xFFFFFFFFu, 0xFF000000u, 0xFF000000u, 0xFF000000u], Smooth: true);
        surface.Renderer.DrawFrame(frame);
        Assert.Equal(0xFF404040u, surface.ReadPixels()[4]);
        surface.Renderer.DrawFrame(frame with { Smooth = false });
        Assert.Equal(0xFFFFFFFFu, surface.ReadPixels()[4]);
        surface.Renderer.DrawFrame(frame);
        Assert.Equal(0xFF404040u, surface.ReadPixels()[4]);
    }

    [Fact]
    public void NewSnapshotWithSameDimensionsUploadsNewPixels()
    {
        using var surface = new ScreenRendererTestSurface(3, 3);
        surface.Renderer.DrawFrame(new ScreenFramebufferSnapshot(1, 1, [0xFFFF0000u], true));
        Assert.All(surface.ReadPixels(), pixel => Assert.Equal(0xFFFF0000u, pixel));
        surface.Renderer.DrawFrame(new ScreenFramebufferSnapshot(1, 1, [0xFF0000FFu], true));
        Assert.All(surface.ReadPixels(), pixel => Assert.Equal(0xFF0000FFu, pixel));
    }

    [Fact]
    public void SourceDimensionsCanChangeWithoutRecreatingWindow()
    {
        using var surface = new ScreenRendererTestSurface(4, 4);
        surface.Renderer.DrawFrame(new ScreenFramebufferSnapshot(1, 1, [0xFF0000FFu], true));
        var frame = new ScreenFramebufferSnapshot(2, 1, [0xFFFF0000u, 0xFF00FF00u]);
        surface.Renderer.DrawFrame(frame);
        Assert.Equal(new uint[]
        {
            0xFF000000u, 0xFF000000u, 0xFF000000u, 0xFF000000u,
            0xFFFF0000u, 0xFFFF0000u, 0xFF00FF00u, 0xFF00FF00u,
            0xFFFF0000u, 0xFFFF0000u, 0xFF00FF00u, 0xFF00FF00u,
            0xFF000000u, 0xFF000000u, 0xFF000000u, 0xFF000000u
        }, surface.ReadPixels());
    }

    [Fact]
    public void ChangedSmoothedFramesDoNotAllocateOutputPixelArrays()
    {
        using var surface = new ScreenRendererTestSurface(1920, 1000);
        var first = new ScreenFramebufferSnapshot(640, 360, new uint[640 * 360], true);
        var second = new ScreenFramebufferSnapshot(640, 360, new uint[640 * 360], true);
        surface.Renderer.DrawFrame(first);
        surface.Renderer.DrawFrame(second);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10; index++)
            surface.Renderer.DrawFrame((index & 1) == 0 ? first : second);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocated < 64 * 1024, $"Вывод изменяющихся кадров выделил {allocated:N0} байт.");
    }
}
