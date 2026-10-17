using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

[Collection("SCREEN GPU native windows")]
public sealed class GraphicsAuditRepairTests
{
    [Fact]
    public void TilemapCellAndFileBudgetsFailBeforeAllocation()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md"))) root = root.Parent;
        Assert.NotNull(root);
        var directory = Path.Combine(root.FullName, ".artifacts", "graphics-repair", "resources");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "oversized.tmj");
        File.WriteAllText(path, """
            {"orientation":"orthogonal","width":1,"height":1,"tilewidth":1,"tileheight":1,
             "tilesets":[],"layers":[{"type":"tilelayer","width":16777217,"height":1,"data":[]}]}
            """);
        var failure = Assert.Throws<InvalidDataException>(() => Axiom.Runtime.RuntimeTilemapValue.LoadFromTiledJson(path, _ => null));
        Assert.Contains("cell budget", failure.Message);
        using (var stream = File.Create(path)) stream.SetLength(Axiom.Runtime.GraphicsResourceLimits.MaximumResourceFileBytes + 1);
        failure = Assert.Throws<InvalidDataException>(() => Axiom.Runtime.RuntimeTilemapValue.LoadFromTiledJson(path, _ => null));
        Assert.Contains("64 MiB", failure.Message);
        Assert.Null(new ProgramConsoleService().LoadImage(path));
    }
    [Fact]
    public void TextBufferRejectsDistantRowsBeforeAllocatingIntermediateLines()
    {
        var buffer = new ScreenTextBuffer();
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Write(100000, 1, "x", 15, null, 40));
        Assert.Equal(0, buffer.LineCount);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void ScreenTextHistoryRollsAtItsBudget(int mode)
    {
        var console = new ProgramConsoleService();
        try
        {
            console.ActivateScreen(mode);
            var field = typeof(Axiom.Ide.Services.Graphics.GraphicsHostService).GetField("_graphicsTextBuffer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var buffer = (ScreenTextBuffer)field.GetValue(console.GraphicsHost)!;
            buffer.EnsureRow(Axiom.Runtime.GraphicsResourceLimits.MaximumTextRows);
            console.LocateText(mode == 0 ? 25 : Axiom.Runtime.GraphicsResourceLimits.MaximumTextRows, 1);
            console.WriteGraphicsText("last", true);
            console.WriteGraphicsText("next", true);
            Assert.Equal(Axiom.Runtime.GraphicsResourceLimits.MaximumTextRows, buffer.LineCount);
        }
        finally { console.DeactivateScreen(); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BulkGpuWritePreservesIndicesAndPalette(bool canvas)
    {
        var console = new ProgramConsoleService();
        console.GraphicsHost.UseGpuCanvas = true;
        console.GraphicsHost.UseGpuScreen = true;
        try
        {
            if (canvas) console.ActivateCanvas(32, 32); else console.ActivateScreen(1);
            Assert.True(console.TryWriteAbsolutePixels(0, 0, 2, 2, [1, 2, 3, 4]));
            Assert.Equal(1, console.GetAbsolutePixel(0, 0));
            Assert.Equal(2, console.GetAbsolutePixel(1, 0));
            Assert.Equal(3, console.GetAbsolutePixel(0, 1));
            Assert.Equal(4, console.GetAbsolutePixel(1, 1));
        }
        finally { console.DeactivateScreen(); }
    }
    [Theory]
    [InlineData("А😀", "А")]
    [InlineData("Аé", "А")]
    [InlineData("А👨‍👩‍👧", "А")]
    [InlineData("Ая", "А")]
    [InlineData("", "")]
    public void BackspaceRemovesOneCompleteTextElement(string input, string expected)
    {
        var prompt = new InputPromptService { IsWaitingForInput = true, CurrentInputText = input };
        prompt.BackspaceInputCharacter();
        Assert.Equal(expected, prompt.CurrentInputText);
    }

    [Fact]
    public void FramebufferRejectsOverflowedDimensions() =>
        Assert.ThrowsAny<ArgumentException>(() => new ScreenFramebuffer(65536, 65536));

    [Fact]
    public void ScreenZeroToGpuRecreatesWindowWithCurrentRenderer()
    {
        var console = new ProgramConsoleService();
        console.GraphicsHost.UseGpuScreen = true;
        try
        {
            console.ActivateScreen(0);
            var field = typeof(Axiom.Ide.Services.Graphics.GraphicsHostService)
                .GetField("_graphicsWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var oldWindow = field.GetValue(console.GraphicsHost);
            console.ActivateScreen(1);
            var window = field.GetValue(console.GraphicsHost)!;
            Assert.NotSame(oldWindow, window);
            var renderer = window.GetType().GetField("_gpuRenderer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(window);
            Assert.NotNull(renderer);
            Assert.Same(console.GraphicsHost.GpuRenderer, renderer);
        }
        finally { console.DeactivateScreen(); }
    }
}
