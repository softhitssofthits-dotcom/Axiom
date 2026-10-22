using System.Reflection;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Graphics;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Backend.Tests;

[Collection("SCREEN GPU native windows")]
public sealed class ScreenImageDrawingContextTests
{
    private const uint Red = 0xFFFF0000u;
    private const uint Green = 0xFF00FF00u;
    private const uint Blue = 0xFF0000FFu;
    private const uint White = 0xFFFFFFFFu;
    private const uint Magenta = 0xFFFF00FFu;
    private static readonly RuntimeImageValue Image = new(2, 2, [Red, Green, Blue, White]);
    private static readonly RuntimeSpriteValue Sprite = new(
        new RuntimeImageValue(4, 2, [Magenta, Magenta, Red, Green, Magenta, Magenta, Blue, White]), 2, 2);
    private static readonly RuntimeImageValue RegionImage = new(4, 4,
        [Magenta, Magenta, Magenta, Magenta, Magenta, Red, Green, Magenta,
         Magenta, Blue, White, Magenta, Magenta, Magenta, Magenta, Magenta]);

    public enum DrawingCommand { Image, Sprite, ImageRegion }

    public static TheoryData<bool, DrawingCommand> DrawingCases => new()
    {
        { false, DrawingCommand.Image }, { true, DrawingCommand.Image },
        { false, DrawingCommand.Sprite }, { true, DrawingCommand.Sprite },
        { false, DrawingCommand.ImageRegion }, { true, DrawingCommand.ImageRegion }
    };

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void ViewAndWindowTranslateOriginWithoutScalingImageSize(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 30, 40, null, null, false));
            Assert.True(service.SetWindow(100, 200, 110, 210, true));
            Draw(service, command, 105, 205);
            Assert.True(service.SetWindow(100, 200, 110, 210, false));
            Draw(service, command, 105, 202);
            service.ResetWindow();
            Draw(service, command, 0, 0);
            Assert.True(service.SetViewport(10, 20, 30, 40, null, null, true));
            Draw(service, command, 12, 22);

            AssertFrame(service, 320, 200,
                (20, 30, Red), (21, 30, Green), (20, 31, Blue), (21, 31, White),
                (20, 36, Red), (21, 36, Green), (20, 37, Blue), (21, 37, White),
                (10, 20, Red), (11, 20, Green), (10, 21, Blue), (11, 21, White),
                (12, 22, Red), (13, 22, Green), (12, 23, Blue), (13, 23, White));
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void OriginOutsideRelativeViewStillDrawsItsVisiblePart(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 13, 23, null, null, false));
            Draw(service, command, -1, -1, 4, 4);

            AssertFrame(service, 320, 200,
                (10, 20, Red), (11, 20, Green), (12, 20, Green),
                (10, 21, Blue), (11, 21, White), (12, 21, White),
                (10, 22, Blue), (11, 22, White), (12, 22, White));
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void ExternalAndExtremeDestinationsLeaveScreenUnchanged(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 13, 23, null, null, false));
            Draw(service, command, -3, 0);
            Draw(service, command, 0, -3);
            Draw(service, command, 4, 0);
            Draw(service, command, 0, 4);
            Draw(service, command, long.MaxValue, long.MinValue);
            Draw(service, command, long.MinValue, long.MaxValue);

            AssertFrame(service, 320, 200);
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void WindowProjectionOverflowSkipsDrawingBeforeDestinationValidation(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 30, 40, null, null, false));
            Assert.True(service.SetWindow(0, 0, 1, 1, true));
            var before = service.GraphicsFrame;
            Draw(service, command, long.MaxValue, 0, 0, 2);
            Draw(service, command, 0, long.MinValue, 2, 0);

            Assert.Same(before, service.GraphicsFrame);
            AssertFrame(service, 320, 200);
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void EmptyViewportSkipsDrawingBeforeDestinationValidation(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 30, 40, null, null, false));
            // VIEW отвергает пустую область; здесь проверяется защитный путь службы.
            SetHostField(service, "_viewportRight", 9L);
            var before = service.GraphicsFrame;
            Draw(service, command, 0, 0, 0, 0);

            Assert.Same(before, service.GraphicsFrame);
            AssertFrame(service, 320, 200);
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [MemberData(nameof(DrawingCases))]
    public void CanvasUsesCameraAndPublishesOnlyOnPresent(bool useGpu, DrawingCommand command)
    {
        var service = CreateService(useGpu);
        service.GraphicsHost.UseNativeCanvasWindow = false;
        try
        {
            service.ActivateCanvas(16, 12);
            Assert.False(service.IsScreenActive);
            Assert.True(service.SetViewport(4, 3, 9, 8, null, null, true));
            service.SetCanvasCamera(100, 200);
            var before = service.GraphicsFrame;
            Draw(service, command, 100, 200);

            Assert.Same(before, service.GraphicsFrame);
            service.PresentFrame();
            AssertFrame(service, 16, 12,
                (4, 3, Red), (5, 3, Green), (4, 4, Blue), (5, 4, White));
        }
        finally { service.DeactivateScreen(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InactiveScreenSkipsDestinationAndSpriteFrameValidation(bool screenZero)
    {
        var service = new ProgramConsoleService();
        try
        {
            if (screenZero) service.ActivateScreen(0);
            Assert.False(service.IsScreenActive);
            var before = service.GraphicsFrame;

            service.DrawImage(Image, 0, 0, 0, 0);
            service.DrawSprite(Sprite, -1, 0, 0, 0, 0);
            service.DrawImageRegion(RegionImage, 0, 0, 0, 0, 0, 0, 0, 0);

            Assert.Same(before, service.GraphicsFrame);
        }
        finally { service.DeactivateScreen(); }
    }

    [Fact]
    public void MissingScreenSurfaceSkipsDestinationAndSpriteFrameValidation()
    {
        var service = new ProgramConsoleService();
        try
        {
            service.ActivateScreen(1);
            SetHostField(service, "_framebuffer", null);
            var before = service.GraphicsFrame;

            service.DrawImage(Image, 0, 0, 0, 0);
            service.DrawSprite(Sprite, -1, 0, 0, 0, 0);
            service.DrawImageRegion(RegionImage, 0, 0, 0, 0, 0, 0, 0, 0);

            Assert.Same(before, service.GraphicsFrame);
        }
        finally { service.DeactivateScreen(); }
    }

    [Fact]
    public void NullResourcesAreRejectedBeforeScreenStateIsChecked()
    {
        var service = new ProgramConsoleService();
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => service.DrawImage(null!, 0, 0, 0, 0)).ParamName);
        Assert.Equal("sprite", Assert.Throws<ArgumentNullException>(() => service.DrawSprite(null!, -1, 0, 0, 0, 0)).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => service.DrawImageRegion(null!, 0, 0, 0, 0, 0, 0, 0, 0)).ParamName);
    }

    [Theory]
    [InlineData(DrawingCommand.Image)]
    [InlineData(DrawingCommand.Sprite)]
    [InlineData(DrawingCommand.ImageRegion)]
    public void CpuKeepsDestinationErrorEvenWhenImageIsOutsideView(DrawingCommand command)
    {
        var service = new ProgramConsoleService();
        try
        {
            service.ActivateScreen(1);
            Assert.True(service.SetViewport(10, 20, 13, 23, null, null, false));

            var error = Assert.Throws<ArgumentOutOfRangeException>(() => Draw(service, command, -5, -5, 0, 2));

            Assert.Equal("width", error.ParamName);
            AssertFrame(service, 320, 200);
        }
        finally { service.DeactivateScreen(); }
    }

    private static ProgramConsoleService CreateService(bool useGpu)
    {
        var service = new ProgramConsoleService();
        service.GraphicsHost.UseGpuScreen = useGpu;
        service.GraphicsHost.UseGpuCanvas = useGpu;
        return service;
    }

    private static void Draw(ProgramConsoleService service, DrawingCommand command, long x, long y, int width = 2, int height = 2)
    {
        switch (command)
        {
            case DrawingCommand.Image: service.DrawImage(Image, x, y, width, height); break;
            case DrawingCommand.Sprite: service.DrawSprite(Sprite, 1, x, y, width, height); break;
            case DrawingCommand.ImageRegion: service.DrawImageRegion(RegionImage, 1, 1, 2, 2, x, y, width, height); break;
            default: throw new ArgumentOutOfRangeException(nameof(command));
        }
    }

    private static void AssertFrame(ProgramConsoleService service, int width, int height, params (int X, int Y, uint Color)[] pixels)
    {
        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(width, frame.Width);
        Assert.Equal(height, frame.Height);
        var expected = new uint[width * height];
        Array.Fill(expected, 0xFF000000u);
        foreach (var (x, y, color) in pixels) expected[y * width + x] = color;
        Assert.Equal(expected, frame.BgraPixels);
    }

    private static void SetHostField(ProgramConsoleService service, string name, object? value) =>
        typeof(GraphicsHostService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(service.GraphicsHost, value);
}
