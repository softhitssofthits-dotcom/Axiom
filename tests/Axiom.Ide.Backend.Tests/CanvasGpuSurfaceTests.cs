using Axiom.Ide.Graphics;
using Axiom.Runtime;
using Vortice.Direct3D;
using SharpGen.Runtime;

namespace Axiom.Ide.Backend.Tests;

public sealed class CanvasGpuSurfaceTests
{
    [Fact]
    public void FailedCanvasReplacementReleasesOldWindowAndDoesNotKeepDisposedRenderer()
    {
        var console = new Axiom.Ide.Services.Input.ProgramConsoleService();
        console.GraphicsHost.UseGpuCanvas = true;
        try
        {
            console.ActivateCanvas(20, 20);
            var failure = Assert.Throws<GraphicsSurfaceException>(() => console.ActivateCanvas(20000, 20));
            Assert.IsType<ArgumentOutOfRangeException>(failure.InnerException);
            Assert.False(console.IsCanvasActive);
            Assert.Equal(IntPtr.Zero, console.ScreenWindowHandle);
            Assert.Null(console.GraphicsHost.CanvasRenderer);
            Assert.Null(console.GraphicsFrame);
            console.ActivateCanvas(10, 10);
            console.FillRect(0, 0, 10, 10, 0x00FF00); console.PresentFrame();
            Assert.All(console.GraphicsFrame!.BgraPixels, pixel => Assert.Equal(0xFF00FF00u, pixel));
        }
        finally { console.DeactivateScreen(); }
    }

    [Fact]
    public void NativeGpuPresentationFiltersPublishedTextureWithoutShowingWorkingFrame()
    {
        using var renderer = new CanvasRendererTestSurface(2, 2, 3, 3);
        var gpu = renderer.Surface;
        gpu.Clear(0); gpu.Fill(0, 0, 0, 0, 0xFFFFFF); gpu.Publish();
        gpu.DrawDisplayedFrame(true);
        Assert.Equal(0xFF404040u, renderer.ReadDisplayed()[4]);
        gpu.Clear(0xFF0000);
        gpu.DrawDisplayedFrame(false);
        Assert.Equal(0xFFFFFFFFu, renderer.ReadDisplayed()[4]);
        gpu.DrawDisplayedFrame(true);
        Assert.Equal(0xFF404040u, renderer.ReadDisplayed()[4]);
        gpu.Publish(); gpu.DrawDisplayedFrame(true);
        Assert.All(renderer.ReadDisplayed(), pixel => Assert.Equal(0xFFFF0000u, pixel));
    }

    [Fact]
    public void FailedHardwareCreationSelectsRealWarpDevice()
    {
        var drivers = new List<DriverType>();
        var selected = CanvasDeviceFactory.Select(false, driver =>
        {
            drivers.Add(driver);
            return driver == DriverType.Hardware
                ? new CanvasDeviceAttempt(new Result(unchecked((int)0x80004005)), null, null)
                : CanvasDeviceFactory.Create(driver);
        });
        using var device = selected.Device;
        using var context = selected.Context;
        Assert.Equal(new[] { DriverType.Hardware, DriverType.Warp }, drivers);
        Assert.Equal("WARP", selected.Name);
    }

    [Fact]
    public void FailureOfBothDevicesIsReportedInsteadOfCreatingBlankSurface()
    {
        var drivers = new List<DriverType>();
        Assert.ThrowsAny<Exception>(() => CanvasDeviceFactory.Select(false, driver =>
        {
            drivers.Add(driver);
            return new CanvasDeviceAttempt(new Result(unchecked((int)0x80004005)), null, null);
        }));
        Assert.Equal(new[] { DriverType.Hardware, DriverType.Warp }, drivers);
    }

    [Fact]
    public void OrdinaryHostFramesStayOnGpuWithoutReadback()
    {
        var console = new Axiom.Ide.Services.Input.ProgramConsoleService();
        console.GraphicsHost.UseGpuCanvas = true;
        try
        {
            console.ActivateCanvas(320, 180);
            for (var frame = 0; frame < 5; frame++)
            {
                console.BeginFrame();
                console.FillRect(frame, 5, 20, 20, 0xABCDEF);
                console.DrawCanvasText("Игры", 10, 40, 0xFFFFFF);
                console.PresentFrame();
            }
            var bytes = console.GraphicsHost.CanvasRenderer!.Invoke(surface => surface.ReadbackBytes);
            Assert.Equal(0, bytes);
        }
        finally { console.DeactivateScreen(); }
    }

    [Fact]
    public void IndexedRegionReadPreservesPsetIndicesAndRgbPaletteMapping()
    {
        using var gpu = new CanvasGpuSurface(12, 8);
        gpu.Clear(0);
        gpu.Fill(2, 2, 3, 3, 0xAABBCC);
        var rgb = (int)(ScreenFramebuffer.MapColorToBgra(200) & 0xffffff);
        gpu.Fill(4, 2, 4, 2, rgb, 200);
        var pixels = gpu.ReadWorkingIndices(1, 1, 5, 4);
        Assert.Equal(ScreenFramebuffer.FindNearestDefaultPaletteIndex(0xFFAABBCC), pixels[6]);
        Assert.Equal(200, pixels[8]);
        Assert.Equal(0, pixels[0]);
        Assert.All(gpu.ReadWorkingIndices(-20, -20, 3, 3), p => Assert.Equal(0, p));
    }

    [Fact]
    public void HostPaintUsesOneRegionAndPreservesRgbOutsideFilledArea()
    {
        var console = new Axiom.Ide.Services.Input.ProgramConsoleService();
        console.GraphicsHost.UseGpuCanvas = true;
        try
        {
            console.ActivateCanvas(40, 30);
            console.BeginFrame();
            console.ClearFrame(0x123456);
            for (var x = 2; x <= 12; x++) { console.SetAbsolutePixel(x, 2, 14); console.SetAbsolutePixel(x, 12, 14); }
            for (var y = 2; y <= 12; y++) { console.SetAbsolutePixel(2, y, 14); console.SetAbsolutePixel(12, y, 14); }
            console.Paint(3, 3, 12, 14);
            console.PresentFrame();
            var frame = console.GraphicsFrame!;
            Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[3 * 40 + 3]);
            Assert.Equal(ScreenFramebuffer.MapColorToBgra(14), frame.BgraPixels[2 * 40 + 2]);
            Assert.Equal(0xFF123456u, frame.BgraPixels[0]);
        }
        finally { console.DeactivateScreen(); }
    }

    [Fact]
    public void HugeLinesAreClippedWithoutWalkingInvisibleBillionsOfPixels()
    {
        using var gpu = new CanvasGpuSurface(20, 12);
        gpu.Clear(0);
        gpu.DrawLine(-1000000000, 5, 1000000000, 5, 0xFFFFFF, 2, 0, 15, 11);
        gpu.Publish();
        var pixels = gpu.ReadPublished();
        Assert.Equal(0xFF000000u, pixels[5 * 20 + 1]);
        Assert.Equal(0xFFFFFFFFu, pixels[5 * 20 + 2]);
        Assert.Equal(0xFFFFFFFFu, pixels[5 * 20 + 15]);
        Assert.Equal(0xFF000000u, pixels[5 * 20 + 16]);
    }

    [Fact]
    public async Task RenderWorkerReportsFailureAndDoesNotLeaveQueryWaitingForever()
    {
        using var session = new CanvasRenderSession(8, 8);
        var fault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Faulted += e => fault.TrySetResult(e);
        session.Enqueue(_ => throw new InvalidOperationException("Проверяемый отказ устройства"));
        await fault.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Throws<InvalidOperationException>(() => session.Invoke(surface => surface.GetPixel(0, 0)));
        session.DetachWindow();
    }

    [Fact]
    public void CirclePixelsMatchExistingRasterizerForEverySmallRadius()
    {
        using var gpu = new CanvasGpuSurface(140, 140);
        for (var radius = 1; radius <= 64; radius++)
        {
            var reference = new ScreenFramebuffer(140, 140);
            reference.ClearTrueColor(0);
            GraphicsEllipseRasterizer.Rasterize(70, 70, radius, radius, true, 0, Math.Tau, 140, 140,
                (x, y) => reference.SetTrueColorPixel(x, y, 0xFEDCBA));
            gpu.Clear(0);
            gpu.DrawCircle(70, 70, radius, 0xFEDCBA, false, 0, 0, 139, 139);
            gpu.Publish();
            Assert.Equal(reference.CreateSnapshot().BgraPixels, gpu.ReadPublished());
        }
    }

    [Fact]
    public void GlyphCompositionMatchesRussianTextAndKeepsBackground()
    {
        using var gpu = new CanvasGpuSurface(80, 30);
        var reference = new ScreenFramebuffer(80, 30);
        gpu.Clear(0x2468AC); reference.ClearTrueColor(0x2468AC);
        const string text = "Ёж 123\nИгры";
        gpu.DrawText(text, -2, 1, 0xEFABCD, 1, 2, 75, 25);
        reference.DrawTrueColorText(-2, 1, text, 0xEFABCD, 1, 2, 75, 25);
        gpu.Publish();
        Assert.Equal(reference.CreateSnapshot().BgraPixels, gpu.ReadPublished());
    }

    [Fact]
    public void HostUsesGpuWithCameraViewportAndPointWithoutPublishingWorkingPixels()
    {
        var console = new Axiom.Ide.Services.Input.ProgramConsoleService();
        console.GraphicsHost.UseGpuCanvas = true;
        try
        {
            console.ActivateCanvas(48, 32);
            Assert.NotEqual(IntPtr.Zero, console.ScreenWindowHandle);
            Assert.Contains(console.GraphicsHost.CanvasGraphicsDriver, new[] { "Hardware", "WARP" });
            var initial = console.GraphicsFrame!;
            console.BeginFrame();
            console.SetCanvasCamera(100, 50);
            console.FillRect(102, 52, 4, 3, 0x123456);
            console.SetPixel(108, 58, 12);
            Assert.Equal(12, console.GetPixel(108, 58));
            Assert.Same(initial, console.GraphicsFrame);
            console.PresentFrame();
            var frame = console.GraphicsFrame!;
            Assert.Equal(0xFF123456u, frame.BgraPixels[2 * frame.Width + 2]);
            Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[8 * frame.Width + 8]);
        }
        finally { console.DeactivateScreen(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImagesPreserveIntegerScalingAlphaAndPublishedBoundary(bool software)
    {
        using var gpu = new CanvasGpuSurface(17, 13, software);
        var reference = new ScreenFramebuffer(17, 13);
        var image = new RuntimeImageValue(3, 2,
            [0x80FF0000u, 0xFF00FF00u, 0x000000FFu, 0x7FABCDEFu, 0xFFFFFFFFu, 0xFF112233u]);
        reference.ClearTrueColor(0x123456);
        gpu.Clear(0x123456);
        gpu.Publish();
        var before = gpu.ReadPublished();
        reference.DrawImageRegion(image, 0, 0, 3, 2, -2, 3, 14, 7, 0, 0, 16, 12);
        gpu.DrawImageRegion(image, 0, 0, 3, 2, -2, 3, 14, 7, 0, 0, 16, 12);
        Assert.Equal(before, gpu.ReadPublished());
        gpu.Publish();
        Assert.Equal(reference.CreateSnapshot().BgraPixels, gpu.ReadPublished());
        Assert.Equal(1, gpu.UploadedImageCount);
        gpu.DrawImageRegion(image, 0, 0, 3, 2, 0, 0, 3, 2, 0, 0, 16, 12);
        Assert.Equal(1, gpu.UploadedImageCount);
        if (software) Assert.Equal("WARP", gpu.DriverName);
    }

    [Fact]
    public void FillClipsAndClearKeepsPublishedFrameUntilPublish()
    {
        using var gpu = new CanvasGpuSurface(8, 6);
        gpu.Clear(0x0000FF);
        gpu.Fill(-10, 2, 3, 100, 0xFF0000);
        gpu.Publish();
        var pixels = gpu.ReadPublished();
        Assert.Equal(0xFF0000FFu, pixels[0]);
        Assert.Equal(0xFFFF0000u, pixels[16]);
        Assert.Equal(0xFF0000FFu, pixels[20]);
        gpu.Clear(0);
        Assert.Equal(pixels, gpu.ReadPublished());
        gpu.Publish();
        Assert.All(gpu.ReadPublished(), pixel => Assert.Equal(0xFF000000u, pixel));
    }

    [Fact]
    public void ImageCacheIsBoundedAndEvictedTextureCanBeDrawnAgain()
    {
        using var gpu = new CanvasGpuSurface(2, 2);
        var first = new RuntimeImageValue(1, 1, [0xFF112233u]);
        gpu.DrawImageRegion(first, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1);
        for (var i = 0; i < 260; i++)
            gpu.DrawImageRegion(new RuntimeImageValue(1, 1, [0xFFFF0000u]), 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1);
        Assert.True(gpu.CachedImageCount <= 256);
        gpu.DrawImageRegion(first, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1);
        gpu.Publish();
        Assert.Equal(0xFF112233u, gpu.ReadPublished()[3]);
    }
}
