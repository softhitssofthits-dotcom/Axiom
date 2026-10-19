using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Backend.Tests;

[Collection("SCREEN GPU native windows")]
public sealed class ScreenGpuHostTests
{
    [Fact]
    public void ViewportBorderOnNonvisualPageDoesNotWriteAboveRectangle()
    {
        var gpu = new ProgramConsoleService(); gpu.GraphicsHost.UseGpuScreen = true;
        try
        {
            gpu.ActivateScreen(1);
            gpu.DrawLine(20, 60, 60, 100, 9, GraphicsLineKind.Box);
            gpu.Paint(GraphicsPaintRequest.CreatePattern(30, 70, [0], 9, 6, 0, [0]));
            gpu.Paint(GraphicsPaintRequest.CreatePattern(30, 70, [255], 9, 6, 0, [0]));
            gpu.DrawCircle(160, 100, 60, 12, false, 0, .01, 1, false, false);
            gpu.DrawCircle(160, 100, 30, 14);
            gpu.ActivateScreen(1, 1, 0);
            gpu.DrawLine(2, 3, 30, 45, 4, GraphicsLineKind.FilledBox);
            gpu.SetPaletteColor(4, 46);
            gpu.CopyScreenPage(1, 0);
            gpu.WriteGraphicsText("Страница", true);
            _ = gpu.GraphicsFrame;
            gpu.SetPaletteColor(9, 196);
            gpu.SetViewport(20, 40, 100, 120, 3, 9, true);
            Assert.Equal(0, gpu.GetAbsolutePixel(20, 0));
            Assert.Equal(9, gpu.GetAbsolutePixel(20, 40));
        }
        finally { gpu.DeactivateScreen(); }
    }

    [Fact]
    public void ScreenDrawingPagesTextAndPaletteMatchCpu()
    {
        var cpu = new ProgramConsoleService();
        var gpu = new ProgramConsoleService(); gpu.GraphicsHost.UseGpuScreen = true;
        try
        {
            foreach (var host in new[] { cpu, gpu })
            {
                host.ActivateScreen(1);
                host.SetPaletteColor(0, 17);
                host.SetPixel(5, 30, 12);
                host.DrawLine(-1_000_000_000, -1_000_000_000, 1_000_000_000, 1_000_000_000, 10, GraphicsLineKind.Normal);
                host.DrawLine(20, 60, 60, 100, 9, GraphicsLineKind.Box);
                host.SetTextColor(6, TextBackgroundMode.Color, 0);
                host.Paint(GraphicsPaintRequest.CreatePattern(30, 70, [0], 9, 6, 0, [0]));
                host.Paint(GraphicsPaintRequest.CreatePattern(30, 70, [255], 9, 6, 0, [0]));
                host.DrawCircle(160, 100, 60, 12, false, 0, .01, 1, false, false);
                host.DrawCircle(160, 100, 30, 14);
                var image = new RuntimeImageValue(2, 1, [0x80FF0000u, 0xFF00FF00u]);
                host.DrawImage(image, 260, 120, 6, 3);
                host.DrawSprite(new RuntimeSpriteValue(image, 1, 1), 1, 280, 120, 3, 3);
                host.WriteGraphicsText("Привет", true);
            }
            Equal();
            foreach (var host in new[] { cpu, gpu })
            {
                host.ActivateScreen(1, 1, 0);
                host.DrawLine(2, 3, 30, 45, 4, GraphicsLineKind.FilledBox);
                host.SetPaletteColor(4, 46);
                host.CopyScreenPage(1, 0);
                host.WriteGraphicsText("Страница", true);
            }
            Equal();
            foreach (var host in new[] { cpu, gpu })
            {
                host.SetPaletteColor(9, 196);
                host.SetViewport(20, 40, 100, 120, 3, 9, true);
                host.SetWindow(0, 0, 80, 80, true);
                host.DrawLine(-1_000_000_000, 40, 1_000_000_000, 40, 14, GraphicsLineKind.Normal);
                host.DrawCircle(40, 40, 12, 12, false, -.3, 2.5, .5, true, false);
                host.ResetViewport(); host.ResetWindow();
                host.CopyScreenPage(1, 0);
            }
            Equal();
            void Equal()
            {
                Assert.Equal(cpu.GraphicsFrame!.BgraPixels, gpu.GraphicsFrame!.BgraPixels);
                Assert.Equal(cpu.CurrentGraphicsX, gpu.CurrentGraphicsX);
                Assert.Equal(cpu.CurrentGraphicsY, gpu.CurrentGraphicsY);
                Assert.Equal(cpu.GetPixel(220, 100), gpu.GetPixel(220, 100));
            }
        }
        finally { cpu.DeactivateScreen(); gpu.DeactivateScreen(); }
    }

    [Fact]
    public void OrdinaryScreenFramesDoNotReadBackPixelsAndTransitionsReleaseResources()
    {
        var host = new ProgramConsoleService(); host.GraphicsHost.UseGpuScreen = true; host.GraphicsHost.UseGpuCanvas = true;
        try
        {
            host.ActivateScreen(1);
            for (var i = 0; i < 4; i++)
            {
                host.DrawLine(0, i, 200, i + 15, 12, GraphicsLineKind.Normal);
                host.DrawCircle(80, 80, 25, 14);
                host.WriteGraphicsText("GPU", true);
            }
            Assert.Equal(0, host.GraphicsHost.GpuRenderer!.Invoke(surface => surface.ReadbackBytes));
            host.ActivateCanvas(32, 32); Assert.True(host.IsCanvasActive);
            host.ActivateScreen(1); Assert.True(host.IsScreenActive);
            host.ActivateScreen(0); Assert.False(host.IsScreenActive);
            Assert.Null(host.GraphicsHost.GpuRenderer);
            host.ActivateScreen(1); host.DeactivateScreen(); Assert.Null(host.GraphicsHost.GpuRenderer);
        }
        finally { host.DeactivateScreen(); }
    }
}

[CollectionDefinition("SCREEN GPU native windows", DisableParallelization = true)]
public sealed class ScreenGpuNativeWindowCollection { }
