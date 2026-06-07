using System.Windows.Threading;
using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Tests;

public sealed class ProgramConsoleServiceTests
{
    [Fact]
    public void RequestInput_ShowsConsolePromptAndEchoesSubmittedText()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.False(service.IsWaitingForInput);
        Assert.Equal("Name? Ada", service.OutputText);
    }

    [Fact]
    public void WriteOutput_AppendsConsoleTranscript()
    {
        var service = new ProgramConsoleService();

        service.BeginSession();
        service.WriteOutput("hello");
        service.WriteOutput("world");

        Assert.Equal($"hello{Environment.NewLine}world", service.OutputText);
    }

    [Fact]
    public void BeginSession_ClearsExistingConsoleState()
    {
        var service = new ProgramConsoleService();
        service.WriteOutput("old");

        service.BeginSession();

        Assert.Equal(string.Empty, service.OutputText);
        Assert.False(service.IsWaitingForInput);
        Assert.Equal(string.Empty, service.CurrentInputText);
    }

    [Fact]
    public void ReadKey_ReturnsQueuedKeyThenEmpty()
    {
        var service = new ProgramConsoleService();

        service.EnqueueKey("LEFT");

        Assert.Equal("LEFT", service.ReadKey());
        Assert.Equal(string.Empty, service.ReadKey());
    }

    [Fact]
    public void BeginSession_ClearsQueuedKeys()
    {
        var service = new ProgramConsoleService();

        service.EnqueueKey("A");
        service.BeginSession();

        Assert.Equal(string.Empty, service.ReadKey());
    }

    [Fact]
    public void IsKeyDown_TracksPressedKeysUntilReleased()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.SetKeyDown("ctrl");

        Assert.True(service.IsKeyDown("LEFT"));
        Assert.True(service.IsKeyDown("CTRL"));
        Assert.False(service.IsKeyDown("RIGHT"));

        service.SetKeyUp("LEFT");

        Assert.False(service.IsKeyDown("LEFT"));
        Assert.True(service.IsKeyDown("CTRL"));
    }

    [Fact]
    public void BeginSession_ClearsPressedKeys()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.BeginSession();

        Assert.False(service.IsKeyDown("LEFT"));
    }

    [Fact]
    public void ClearKeyState_ClearsPressedKeysWithoutConsumingQueue()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.EnqueueKey("LEFT");
        service.ClearKeyState();

        Assert.False(service.IsKeyDown("LEFT"));
        Assert.Equal("LEFT", service.ReadKey());
    }

    [Fact]
    public void MouseState_TracksPositionAndButtons()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(128, 256);
        service.EnqueueMouseButtonPress(1);
        service.EnqueueMouseButtonPress(3);
        service.SetMouseButtonDown(1);
        service.SetMouseButtonDown(3);

        Assert.Equal(128, service.GetMouseX());
        Assert.Equal(256, service.GetMouseY());
        Assert.True(service.IsMouseButtonDown(1));
        Assert.False(service.IsMouseButtonDown(2));
        Assert.True(service.IsMouseButtonDown(3));
        Assert.Equal(1, service.ReadMouseButtonPress());
        Assert.Equal(3, service.ReadMouseButtonPress());
        Assert.Equal(0, service.ReadMouseButtonPress());

        service.SetMouseButtonUp(1);

        Assert.False(service.IsMouseButtonDown(1));
        Assert.True(service.IsMouseButtonDown(3));
    }

    [Fact]
    public void MouseState_QueuesReleaseAndMoveEvents()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseMove();
        service.EnqueueMouseButtonRelease(1);
        service.EnqueueMouseWheel(-120);

        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Release, 1), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Wheel, -120), service.ReadMouseEvent());
        Assert.Equal(default, service.ReadMouseEvent());
    }

    [Fact]
    public void MouseState_CoalescesPendingMoveEventsWithoutDroppingButtons()
    {
        var service = new ProgramConsoleService();

        service.EnqueueMouseMove();
        service.EnqueueMouseMove();
        service.EnqueueMouseButtonPress(1);
        service.EnqueueMouseMove();
        service.EnqueueMouseMove();
        service.EnqueueMouseButtonRelease(1);

        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Press, 1), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), service.ReadMouseEvent());
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Release, 1), service.ReadMouseEvent());
        Assert.Equal(default, service.ReadMouseEvent());
    }

    [Fact]
    public void BeginSession_ClearsMouseState()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseButtonPress(2);
        service.SetMouseButtonDown(2);
        service.BeginSession();

        Assert.Equal(-1, service.GetMouseX());
        Assert.Equal(-1, service.GetMouseY());
        Assert.False(service.IsMouseButtonDown(2));
        Assert.Equal(0, service.ReadMouseButtonPress());
    }

    [Fact]
    public void ClearMouseState_ResetsPositionAndButtons()
    {
        var service = new ProgramConsoleService();

        service.SetMousePosition(10, 20);
        service.EnqueueMouseButtonPress(1);
        service.SetMouseButtonDown(1);
        service.ClearMouseState();

        Assert.Equal(-1, service.GetMouseX());
        Assert.Equal(-1, service.GetMouseY());
        Assert.False(service.IsMouseButtonDown(1));
        Assert.Equal(0, service.ReadMouseButtonPress());
    }

    [Fact]
    public async Task RuntimeInputState_CanBeReadFromRuntimeThread()
    {
        var service = new ProgramConsoleService();

        service.SetKeyDown("LEFT");
        service.EnqueueKey("LEFT");
        service.SetMousePosition(12, 34);
        service.SetMouseButtonDown(1);
        service.EnqueueMouseButtonPress(1);

        var result = await Task.Run(() => new
        {
            KeyDown = service.IsKeyDown("LEFT"),
            Key = service.ReadKey(),
            MouseX = service.GetMouseX(),
            MouseY = service.GetMouseY(),
            MouseButton = service.IsMouseButtonDown(1),
            MouseEvent = service.ReadMouseEvent()
        });

        Assert.True(result.KeyDown);
        Assert.Equal("LEFT", result.Key);
        Assert.Equal(12, result.MouseX);
        Assert.Equal(34, result.MouseY);
        Assert.True(result.MouseButton);
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Press, 1), result.MouseEvent);
    }

    [Fact]
    public void ActivateScreen_UsesGraphicsStateSeparateFromOutput()
    {
        var service = new ProgramConsoleService();

        service.WriteOutput("before");
        service.ActivateScreen(5);
        service.WriteGraphicsText("hello", newLine: true);

        Assert.True(service.IsScreenActive);
        Assert.Equal(5, service.ScreenMode);
        Assert.Equal(1280, service.ScreenWidth);
        Assert.Equal(720, service.ScreenHeight);
        Assert.Equal("before", service.OutputText);
        Assert.Equal($"hello{Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void ActivateScreen10_UsesProvidedBestFitSizeAndVisibleLocateGrid()
    {
        const int width = 1918;
        const int height = 1079;
        var service = new ProgramConsoleService(screenModeSizeProvider: new FixedScreenModeSizeProvider((width, height)));

        service.ActivateScreen(10);
        service.LocateText(height / ScreenTextRasterizer.CellHeight, width / ScreenTextRasterizer.CellWidth);
        service.WriteGraphicsText("#", newLine: false);

        Assert.True(service.IsScreenActive);
        Assert.Equal(10, service.ScreenMode);
        Assert.Equal(width, service.ScreenWidth);
        Assert.Equal(height, service.ScreenHeight);
        Assert.True(
            HasCellPixels(
                service.GraphicsFrame,
                row: height / ScreenTextRasterizer.CellHeight,
                column: width / ScreenTextRasterizer.CellWidth));
    }

    [Fact]
    public void IdeInputProvider_ProxiesGraphicsStateFromProgramConsoleService()
    {
        var service = new ProgramConsoleService();
        var inputProvider = new IdeInputProvider(service);

        service.ActivateScreen(1);
        service.SetTextColor(6, TextBackgroundMode.Color, 2);
        service.SetPixel(10, 20, 6);
        service.DrawLine(10, 20, 15, 24, 6, GraphicsLineKind.Normal);

        Assert.True(inputProvider.IsScreenActive);
        Assert.Equal(1, inputProvider.ScreenMode);
        Assert.Equal(6, inputProvider.CurrentForegroundColor);
        Assert.Equal(2, inputProvider.CurrentBackgroundColor);
        Assert.False(inputProvider.IsTextBackgroundTransparent);
        Assert.Equal(15, inputProvider.CurrentGraphicsX);
        Assert.Equal(24, inputProvider.CurrentGraphicsY);
        Assert.Equal(6, inputProvider.GetPixel(10, 20));
        Assert.Equal(6, inputProvider.GetPixel(15, 24));
    }

    [Fact]
    public void ActivateScreenZero_UsesNativeTextWindowWithoutEnablingGraphicsMode()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(0);
        service.WriteOutput("hello");

        Assert.False(service.IsScreenActive);
        Assert.True(service.IsScreenOutputActive);
        Assert.Equal(0, service.ScreenMode);
        Assert.Equal(640, service.ScreenWidth);
        Assert.Equal(300, service.ScreenHeight);
        Assert.Equal(string.Empty, service.OutputText);
        Assert.Equal($"hello{Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void ScreenZeroInput_DoesNotEchoPromptToOutputPane()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.ActivateScreen(0);
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.ReadGraphicsInput(new InputRequest("Name", 1));

        Assert.Equal("Ada", result);
        Assert.Equal(string.Empty, service.OutputText);
        Assert.Equal($"Name? Ada{Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void ScreenZeroInput_PublishesPromptAndTypedTextBeforeSubmission()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.ActivateScreen(0);
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            Assert.True(HasCellPixels(service.GraphicsFrame, row: 1, column: 1));

            service.CurrentInputText = "Ada";
            Assert.True(HasCellPixels(service.GraphicsFrame, row: 1, column: 1));
            Assert.False(HasCellPixels(service.GraphicsFrame, row: 25, column: 1));

            service.SubmitInput();
            Assert.False(HasCellPixels(service.GraphicsFrame, row: 2, column: 1));
        });

        var result = service.ReadGraphicsInput(new InputRequest("Name", 1));

        Assert.Equal("Ada", result);
        Assert.Equal(string.Empty, service.OutputText);
        Assert.Equal($"Name? Ada{Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void ScreenZero_ScrollsVisibleTextWhenNewLinePassesLastRow()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(0);
        for (var index = 1; index <= 27; index++)
        {
            service.WriteGraphicsText($"line {index:00}", newLine: true);
        }

        var lines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal(25, lines.Length);
        Assert.Equal("line 04", lines[0]);
        Assert.Equal("line 27", lines[23]);
        Assert.Equal(string.Empty, lines[24]);
        Assert.True(service.TryGetScreenTextScrollState(out var scrollState));
        Assert.Equal(4, scrollState.FirstVisibleRow);
        Assert.Equal(25, scrollState.VisibleRows);
        Assert.Equal(28, scrollState.TotalRows);
        Assert.True(HasCellPixels(service.GraphicsFrame, row: 24, column: 1));
        Assert.False(HasCellPixels(service.GraphicsFrame, row: 25, column: 1));
    }

    [Fact]
    public void ScreenZero_ManualScrollShowsOlderHistoryAndReturnsToBottom()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(0);
        for (var index = 1; index <= 27; index++)
        {
            service.WriteGraphicsText($"line {index:00}", newLine: true);
        }

        service.SetScreenTextFirstVisibleRow(1);
        var topLines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal("line 01", topLines[0]);
        Assert.Equal("line 25", topLines[24]);

        service.SetScreenTextFirstVisibleRow(4);
        var bottomLines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal("line 04", bottomLines[0]);
        Assert.Equal("line 27", bottomLines[23]);
        Assert.Equal(string.Empty, bottomLines[24]);
    }

    [Fact]
    public void ScreenZero_EmptyPrintOnLastRowScrollsToBlankLastRow()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(0);
        service.LocateText(25, 1);
        service.WriteGraphicsText(string.Empty, newLine: true);

        var lines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal(25, lines.Length);
        Assert.All(lines, line => Assert.Equal(string.Empty, line));
        Assert.False(HasCellPixels(service.GraphicsFrame, row: 25, column: 1));
    }

    [Fact]
    public void ScreenZero_ClipsLongTextWithoutVerticalScroll()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(0);
        service.WriteGraphicsText(new string('A', 100), newLine: false);

        Assert.Equal(80, service.GraphicsText.Length);
        Assert.Equal(new string('A', 80), service.GraphicsText);
    }

    [Fact]
    public void GraphicsScreen_DoesNotAutoScrollTextPastVisibleRows()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        for (var index = 1; index <= 18; index++)
        {
            service.WriteGraphicsText($"line {index:00}", newLine: true);
        }

        var lines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal(16, lines.Length);
        Assert.Equal("line 01", lines[0]);
        Assert.Equal("line 16", lines[15]);
        Assert.DoesNotContain("line 17", lines);
        Assert.True(HasCellPixels(service.GraphicsFrame, row: 1, column: 1));
        Assert.True(HasCellPixels(service.GraphicsFrame, row: 16, column: 1));
    }

    [Fact]
    public void GraphicsScreen_ViewPrintScrollsOnlyWithinConfiguredRows()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        Assert.True(service.SetTextViewport(10, 12));
        service.LocateText(16, 1);
        service.WriteGraphicsText("status", newLine: true);
        Assert.True(service.SetTextViewport(10, 12));
        for (var index = 1; index <= 5; index++)
        {
            service.WriteGraphicsText($"row{index}", newLine: true);
        }

        var lines = service.GraphicsText.Split(Environment.NewLine);

        Assert.Equal(16, lines.Length);
        Assert.Equal("row3", lines[9]);
        Assert.Equal("row4", lines[10]);
        Assert.Equal("row5", lines[11]);
        Assert.Equal("status", lines[15]);
    }

    [Fact]
    public void WriteGraphicsText_OverwritesAtLocatedCursor()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.WriteGraphicsText("event=120 x=300 y=200", newLine: true);
        service.LocateText(1, 1);
        service.WriteGraphicsText("event=1 x=7 y=9      ", newLine: true);

        Assert.Equal($"event=1 x=7 y=9      {Environment.NewLine}", service.GraphicsText);
    }

    [Fact]
    public void WriteGraphicsText_UpdatesFramebufferTextLayer()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.WriteGraphicsText("A", newLine: false);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Contains(frame.BgraPixels, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
    }

    [Fact]
    public void WriteGraphicsText_PreservesCellColorsAndTransparentBackground()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(3);
        service.SetTextColor(14, TextBackgroundMode.Color, 1);
        service.WriteGraphicsText("A", newLine: true);
        service.SetTextColor(15, TextBackgroundMode.Transparent);
        service.WriteGraphicsText("B", newLine: false);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var firstCell = GetCellPixels(frame, row: 1, column: 1);
        var secondLineFirstCell = GetCellPixels(frame, row: 2, column: 1);

        Assert.Contains(firstCell, pixel => pixel == ScreenFramebuffer.MapColorToBgra(14));
        Assert.Contains(firstCell, pixel => pixel == ScreenFramebuffer.MapColorToBgra(1));
        Assert.Contains(secondLineFirstCell, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
        Assert.DoesNotContain(secondLineFirstCell, pixel => pixel == ScreenFramebuffer.MapColorToBgra(1));
    }

    [Fact]
    public void ScreenColorPalette_UsesBasicBgraColorOrder()
    {
        Assert.Equal(0xFF0000AAu, ScreenFramebuffer.MapColorToBgra(1));
        Assert.Equal(0xFFAA0000u, ScreenFramebuffer.MapColorToBgra(4));
        Assert.Equal(0xFFFFFF55u, ScreenFramebuffer.MapColorToBgra(14));
    }

    [Fact]
    public void ScreenColorPalette_Supports256ColorIndices()
    {
        Assert.Equal(255, ScreenFramebuffer.NormalizeColorIndex(255));
        Assert.Equal(44, ScreenFramebuffer.NormalizeColorIndex(300));
        Assert.Equal(1, ScreenFramebuffer.NormalizeColorIndex(-257));
        Assert.NotEqual(ScreenFramebuffer.MapColorToBgra(16), ScreenFramebuffer.MapColorToBgra(17));
        Assert.NotEqual(ScreenFramebuffer.MapColorToBgra(231), ScreenFramebuffer.MapColorToBgra(255));
    }

    [Fact]
    public void ScreenColorPalette_SetAndResetRemapExistingPixelIndices()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.SetPixel(20, 40, 1);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var pixelIndex = (40 * frame.Width) + 20;
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(1), frame.BgraPixels[pixelIndex]);

        service.SetPaletteColor(1, 196);

        frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(196), frame.BgraPixels[pixelIndex]);
        Assert.Equal(1, service.GetPixel(20, 40));

        service.ResetPalette();

        frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(1), frame.BgraPixels[pixelIndex]);
    }

    [Fact]
    public void ScreenPages_CopyScreenPageCopiesPixelsAndPaletteToVisiblePage()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1, activePage: 1, visualPage: 0);
        service.SetPixel(20, 40, 1);
        service.SetPaletteColor(1, 196);

        Assert.True(service.CopyScreenPage(1, 0));

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var pixelIndex = (40 * frame.Width) + 20;
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(196), frame.BgraPixels[pixelIndex]);
        Assert.Equal(1, service.GetPixel(20, 40));
    }

    [Fact]
    public void LoadImage_DecodesRepositoryIcon()
    {
        var service = new ProgramConsoleService
        {
            ResourceBaseDirectory = FindRepositoryRoot()
        };

        var image = service.LoadImage("src/Axiom.Ide/Assets/axiom.ico");

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.Equal(image.Width * image.Height, image.BgraPixels.Length);
    }

    [Fact]
    public void LoadImage_ResolvesRelativePathAgainstResourceBaseDirectory()
    {
        var service = new ProgramConsoleService
        {
            ResourceBaseDirectory = Path.Combine(FindRepositoryRoot(), "samples", "graphics", "images")
        };

        var image = service.LoadImage("../../../src/Axiom.Ide/Assets/axiom.ico");

        Assert.NotNull(image);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
    }

    [Fact]
    public void LoadSprite_ResolvesRelativePathAndCountsFrames()
    {
        var service = new ProgramConsoleService
        {
            ResourceBaseDirectory = Path.Combine(FindRepositoryRoot(), "samples", "graphics", "sprites")
        };

        var sprite = service.LoadSprite("assets/walk_sheet.png", 16, 16);

        Assert.NotNull(sprite);
        Assert.Equal(16, sprite.FrameWidth);
        Assert.Equal(16, sprite.FrameHeight);
        Assert.Equal(4, sprite.FrameCount);
    }

    [Fact]
    public void DrawImage_ComposesTrueColorPixelsAndPointReturnsNearestPaletteIndex()
    {
        var service = new ProgramConsoleService();
        var image = new RuntimeImageValue(1, 1, [0x80FF0000u]);

        service.ActivateScreen(1);
        service.SetPixel(20, 40, 0);
        service.DrawImage(image, 20, 40, 1, 1);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var pixelIndex = (40 * frame.Width) + 20;
        Assert.Equal(0xFF800000u, frame.BgraPixels[pixelIndex]);
        Assert.Equal(88, service.GetPixel(20, 40));

        service.SetPixel(20, 40, 3);

        frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(3), frame.BgraPixels[pixelIndex]);
        Assert.Equal(3, service.GetPixel(20, 40));
    }

    [Fact]
    public void DrawSprite_ComposesSelectedFrameAndPointReturnsNearestPaletteIndex()
    {
        var service = new ProgramConsoleService();
        var image = new RuntimeImageValue(
            2,
            1,
            [
                0xFFFF0000u,
                0xFF00FF00u
            ]);
        var sprite = new RuntimeSpriteValue(image, 1, 1);

        service.ActivateScreen(1);
        service.DrawSprite(sprite, 1, 20, 40, 1, 1);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var pixelIndex = (40 * frame.Width) + 20;
        Assert.Equal(0xFF00FF00u, frame.BgraPixels[pixelIndex]);
        Assert.NotEqual(0, service.GetPixel(20, 40));
    }

    [Fact]
    public void ScreenPages_CopyScreenPageCopiesTrueColorLayer()
    {
        var service = new ProgramConsoleService();
        var image = new RuntimeImageValue(1, 1, [ScreenFramebuffer.MapColorToBgra(12)]);

        service.ActivateScreen(1, activePage: 1, visualPage: 0);
        service.DrawImage(image, 20, 40, 1, 1);

        Assert.True(service.CopyScreenPage(1, 0));

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var pixelIndex = (40 * frame.Width) + 20;
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[pixelIndex]);
        Assert.Equal(12, service.GetPixel(20, 40));
    }

    [Fact]
    public void Canvas_PublishesWorkingFrameOnlyAfterPresent()
    {
        var service = new ProgramConsoleService();

        service.ActivateCanvas(8, 8);
        var initialFrame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        service.FillRect(1, 1, 2, 2, 0xFF0000);

        var beforePresent = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Same(initialFrame, beforePresent);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0), beforePresent.BgraPixels[(1 * beforePresent.Width) + 1]);

        service.PresentFrame();

        var afterPresent = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0xFF0000), afterPresent.BgraPixels[(1 * afterPresent.Width) + 1]);
    }

    [Fact]
    public void Canvas_DrawImageAndSpriteComposeIntoModernFrame()
    {
        var service = new ProgramConsoleService();
        var image = new RuntimeImageValue(1, 1, [0xFFFF0000u]);
        var sprite = new RuntimeSpriteValue(
            new RuntimeImageValue(2, 1, [0xFFFF0000u, 0xFF00FF00u]),
            frameWidth: 1,
            frameHeight: 1);

        service.ActivateCanvas(8, 8);
        service.DrawImage(image, 1, 1, 1, 1);
        service.DrawSprite(sprite, 1, 3, 1, 1, 1);
        service.PresentFrame();

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(0xFFFF0000u, frame.BgraPixels[(1 * frame.Width) + 1]);
        Assert.Equal(0xFF00FF00u, frame.BgraPixels[(1 * frame.Width) + 3]);
    }

    [Fact]
    public void Canvas_DrawLineAndCirclesRenderIntoModernFrame()
    {
        var service = new ProgramConsoleService();

        service.ActivateCanvas(40, 40);
        service.DrawCanvasLine(0, 0, 12, 0, 0xFFFF00);
        service.DrawCanvasCircle(16, 16, 4, 0xFF0000);
        service.FillCanvasCircle(28, 28, 3, 0x00FF00);
        service.PresentFrame();

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0xFFFF00), frame.BgraPixels[0]);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0xFFFF00), frame.BgraPixels[10]);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0xFF0000), frame.BgraPixels[(12 * frame.Width) + 16]);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0x00FF00), frame.BgraPixels[(28 * frame.Width) + 28]);
    }

    [Fact]
    public void Canvas_CameraAndResetCameraSupportWorldSceneAndHud()
    {
        var service = new ProgramConsoleService();

        service.ActivateCanvas(64, 32);
        service.SetCanvasCamera(16, 16);
        service.FillRect(32, 32, 6, 6, 0xCC3300);
        service.DrawCanvasLine(16, 32, 32, 32, 0x3366FF);
        service.ResetCanvasCamera();
        service.FillRect(0, 0, 20, 12, 0x111111);
        service.DrawCanvasText("HUD", 1, 1, 0xFFFFFF);
        service.PresentFrame();

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0x3366FF), frame.BgraPixels[(16 * frame.Width) + 8]);
        Assert.Equal(ScreenFramebuffer.MapRgbToBgra(0xCC3300), frame.BgraPixels[(18 * frame.Width) + 18]);
        Assert.Contains(GetCellPixels(frame, row: 1, column: 1), pixel => pixel == ScreenFramebuffer.MapRgbToBgra(0xFFFFFF));
    }

    [Fact]
    public void WriteGraphicsText_RendersRussianLetters()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(3);
        service.WriteGraphicsText("Привет, экран", newLine: false);

        Assert.Equal("Привет, экран", service.GraphicsText);
        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        var firstCell = GetCellPixels(frame, row: 1, column: 1);
        var fallbackPixels = RenderCell('\u2603');

        Assert.Contains(firstCell, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
        Assert.NotEqual(fallbackPixels, firstCell);
    }

    [Theory]
    [InlineData('\u2588')]
    public void DrawGlyph_FillsFullBlockPseudoGraphicCell(char character)
    {
        var pixels = RenderCell(character);
        var foreground = ScreenFramebuffer.MapColorToBgra(15);

        Assert.All(pixels, pixel => Assert.Equal(foreground, pixel));
    }

    [Theory]
    [InlineData('\u2593')]
    [InlineData('\u2592')]
    [InlineData('\u2591')]
    public void DrawGlyph_RendersShadedPseudoGraphicCells(char character)
    {
        var pixels = new uint[ScreenTextRasterizer.CellWidth * ScreenTextRasterizer.CellHeight];
        var foreground = ScreenFramebuffer.MapColorToBgra(15);
        var background = ScreenFramebuffer.MapColorToBgra(0);

        ScreenTextRasterizer.DrawGlyph(
            pixels,
            ScreenTextRasterizer.CellWidth,
            ScreenTextRasterizer.CellHeight,
            0,
            0,
            character,
            foreground,
            0,
            0,
            ScreenTextRasterizer.CellWidth - 1L,
            ScreenTextRasterizer.CellHeight - 1L,
            background);

        Assert.Contains(foreground, pixels);
        Assert.Contains(background, pixels);
    }

    [Theory]
    [InlineData('\u2502')]
    [InlineData('\u2500')]
    [InlineData('\u250C')]
    [InlineData('\u2518')]
    [InlineData('\u2550')]
    [InlineData('\u256C')]
    public void DrawGlyph_RendersBoxDrawingCells(char character)
    {
        var pixels = RenderCell(character);
        var fallbackPixels = RenderCell('\u2603');

        Assert.Contains(pixels, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
        Assert.NotEqual(fallbackPixels, pixels);
    }

    [Theory]
    [InlineData('Є')]
    [InlineData('Ї')]
    [InlineData('Ў')]
    [InlineData('№')]
    [InlineData('√')]
    [InlineData('¤')]
    public void DrawGlyph_RendersRemainingCp866PrintableCells(char character)
    {
        var pixels = RenderCell(character);
        var fallbackPixels = RenderCell('\u2603');

        Assert.Contains(pixels, pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));
        Assert.NotEqual(fallbackPixels, pixels);
    }

    [Fact]
    public void ScreenRenderer_ScalesFrameToCurrentBackBuffer()
    {
        var frame = new ScreenFramebufferSnapshot(2, 2, [1u, 2u, 3u, 4u]);

        var scaled = Direct3D11SwapChainRenderer.ScaleFramePixels(frame, 4, 4);

        Assert.Equal(
            [
                1u, 1u, 2u, 2u,
                1u, 1u, 2u, 2u,
                3u, 3u, 4u, 4u,
                3u, 3u, 4u, 4u
            ],
            scaled);
    }

    [Fact]
    public void ScreenRenderer_PreservesFrameAspectRatioWhenBackBufferIsWider()
    {
        var frame = new ScreenFramebufferSnapshot(2, 2, [1u, 2u, 3u, 4u]);

        var scaled = Direct3D11SwapChainRenderer.ScaleFramePixels(frame, 6, 4);

        Assert.Equal(
            [
                0xFF000000u, 1u, 1u, 2u, 2u, 0xFF000000u,
                0xFF000000u, 1u, 1u, 2u, 2u, 0xFF000000u,
                0xFF000000u, 3u, 3u, 4u, 4u, 0xFF000000u,
                0xFF000000u, 3u, 3u, 4u, 4u, 0xFF000000u
            ],
            scaled);
    }

    [Fact]
    public void ScreenInput_UpdatesFramebufferInputLine()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.ActivateScreen(1);
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            service.CurrentInputText = "Ada";

            var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
            Assert.Contains(GetCellPixels(frame, row: 1, column: 1), pixel => pixel == ScreenFramebuffer.MapColorToBgra(15));

            service.SubmitInput();
        });

        var result = service.ReadGraphicsInput(new InputRequest("Name", 1));

        Assert.Equal("Ada", result);
    }

    private static uint[] GetCellPixels(ScreenFramebufferSnapshot frame, int row, int column)
    {
        var startX = (column - 1) * ScreenTextRasterizer.CellWidth;
        var startY = (row - 1) * ScreenTextRasterizer.CellHeight;
        var pixels = new List<uint>(ScreenTextRasterizer.CellWidth * ScreenTextRasterizer.CellHeight);

        for (var y = startY; y < startY + ScreenTextRasterizer.CellHeight; y++)
        {
            for (var x = startX; x < startX + ScreenTextRasterizer.CellWidth; x++)
            {
                pixels.Add(frame.BgraPixels[(y * frame.Width) + x]);
            }
        }

        return pixels.ToArray();
    }

    private static uint[] RenderCell(char character)
    {
        var pixels = new uint[ScreenTextRasterizer.CellWidth * ScreenTextRasterizer.CellHeight];
        ScreenTextRasterizer.DrawGlyph(
            pixels,
            ScreenTextRasterizer.CellWidth,
            ScreenTextRasterizer.CellHeight,
            0,
            0,
            character,
            ScreenFramebuffer.MapColorToBgra(15),
            0,
            0,
            ScreenTextRasterizer.CellWidth - 1L,
            ScreenTextRasterizer.CellHeight - 1L,
            null);
        return pixels;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found from the current test output directory.");
    }

    [Fact]
    public void DrawLine_RasterizesIntermediatePixels()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 4, 1, 12, GraphicsLineKind.Normal);

        Assert.Equal(12, service.GetPixel(1, 1));
        Assert.Equal(12, service.GetPixel(2, 1));
        Assert.Equal(12, service.GetPixel(3, 1));
        Assert.Equal(12, service.GetPixel(4, 1));
    }

    [Fact]
    public void DrawLine_DoesNotOverflowWhenErrorDoublingSaturates()
    {
        Assert.Equal(long.MaxValue, GraphicsLineMath.SaturatingDouble(long.MaxValue / 2 + 1));
        Assert.Equal(long.MinValue, GraphicsLineMath.SaturatingDouble(long.MinValue / 2 - 1));
        Assert.Equal(2468L, GraphicsLineMath.SaturatingDouble(1234));
        Assert.Equal(long.MaxValue, GraphicsLineMath.SaturatingAdd(long.MaxValue - 2, 10));
        Assert.Equal(long.MinValue, GraphicsLineMath.SaturatingAdd(long.MinValue + 2, -10));
        Assert.Equal(-55L, GraphicsLineMath.SaturatingAdd(-50, -5));
    }

    [Fact]
    public void DrawLineAndPaint_PreserveCurrentGraphicsPositionForDrawStyleScenarios()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(10, 10, 20, 10, 12, GraphicsLineKind.Normal);
        service.DrawLine(20, 10, 20, 20, 12, GraphicsLineKind.Normal);
        service.DrawLine(20, 20, 10, 20, 12, GraphicsLineKind.Normal);
        service.DrawLine(10, 20, 10, 10, 12, GraphicsLineKind.Normal);
        service.SetGraphicsCurrentPosition(15, 15);
        service.Paint(15, 15, 4, 12);

        Assert.Equal(15, service.CurrentGraphicsX);
        Assert.Equal(15, service.CurrentGraphicsY);
        Assert.Equal(12, service.GetPixel(15, 10));
        Assert.Equal(4, service.GetPixel(15, 15));
    }

    [Fact]
    public void DrawLine_UpdatesFramebufferSnapshot()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 4, 1, 12, GraphicsLineKind.Normal);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(320, frame.Width);
        Assert.Equal(200, frame.Height);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[(1 * frame.Width) + 1]);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(12), frame.BgraPixels[(1 * frame.Width) + 4]);
    }

    [Fact]
    public void DrawLine_FilledBoxUsesSingleClippedFill()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(220, 100, 360, 660, 11, GraphicsLineKind.FilledBox);

        var frame = Assert.IsType<ScreenFramebufferSnapshot>(service.GraphicsFrame);
        Assert.Equal(ScreenFramebuffer.MapColorToBgra(11), frame.BgraPixels[(150 * frame.Width) + 250]);
        Assert.Equal(11, service.GetPixel(250, 150));
        Assert.Equal(0, service.GetPixel(10, 10));
    }

    [Fact]
    public void DrawLine_ClipsPixelsOutsideScreenAndKeepsEndPosition()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(-10, -10, 3, 3, 10, GraphicsLineKind.Normal);

        Assert.Equal(10, service.GetPixel(0, 0));
        Assert.Equal(10, service.GetPixel(1, 1));
        Assert.Equal(10, service.GetPixel(2, 2));
        Assert.Equal(10, service.GetPixel(3, 3));
        Assert.Equal(3, service.CurrentGraphicsX);
        Assert.Equal(3, service.CurrentGraphicsY);
    }

    [Fact]
    public void DrawCircle_RasterizesCirclePoints()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(10, 10, 3, 14);

        Assert.Equal(14, service.GetPixel(13, 10));
        Assert.Equal(14, service.GetPixel(7, 10));
        Assert.Equal(14, service.GetPixel(10, 13));
        Assert.Equal(14, service.GetPixel(10, 7));
    }

    [Fact]
    public void DrawCircle_FillsPerimeterWithoutSegmentApproximation()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(10, 10, 4, 12);

        Assert.Equal(12, service.GetPixel(14, 10));
        Assert.Equal(12, service.GetPixel(6, 10));
        Assert.Equal(12, service.GetPixel(10, 14));
        Assert.Equal(12, service.GetPixel(10, 6));
        Assert.Equal(12, service.GetPixel(13, 12));
        Assert.Equal(12, service.GetPixel(7, 12));
    }

    [Fact]
    public void DrawCircle_IterationLimitTracksArcCoverageAndRadius()
    {
        var fullLimit = GraphicsEllipseRasterizer.ComputeMaxIterations(10_000, 10_000, fullEllipse: true, sweep: Math.Tau, screenWidth: 320, screenHeight: 200);
        var arcLimit = GraphicsEllipseRasterizer.ComputeMaxIterations(10_000, 10_000, fullEllipse: false, sweep: Math.PI / 2, screenWidth: 320, screenHeight: 200);
        var smallArcLimit = GraphicsEllipseRasterizer.ComputeMaxIterations(4, 4, fullEllipse: false, sweep: Math.PI / 8, screenWidth: 320, screenHeight: 200);

        Assert.Equal(16_640, fullLimit);
        Assert.Equal(10_000, arcLimit);
        Assert.Equal(8, smallArcLimit);
    }

    [Fact]
    public void DrawCircle_RasterizesArcAndAspect()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(20, 10, 4, 12, relativeCenter: false, startAngle: 0, endAngle: Math.PI, aspect: .5, drawStartRadius: false, drawEndRadius: false);

        Assert.Equal(12, service.GetPixel(24, 10));
        Assert.Equal(12, service.GetPixel(20, 8));
        Assert.Equal(12, service.GetPixel(16, 10));
        Assert.Equal(0, service.GetPixel(20, 12));
    }

    [Fact]
    public void DrawCircle_FullTurnAnglesRasterizeFullCircle()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(20, 10, 4, 12, relativeCenter: false, startAngle: 0d, endAngle: Math.Tau, aspect: 1d, drawStartRadius: false, drawEndRadius: false);

        Assert.Equal(12, service.GetPixel(24, 10));
        Assert.Equal(12, service.GetPixel(16, 10));
        Assert.Equal(12, service.GetPixel(20, 6));
        Assert.Equal(12, service.GetPixel(20, 14));
    }

    [Fact]
    public void GraphicsEllipseRasterizer_RasterizeClipsPlotPixelsToScreen()
    {
        var plotted = new List<(long X, long Y)>();

        GraphicsEllipseRasterizer.Rasterize(
            centerX: 5,
            centerY: 4,
            radiusX: 6,
            radiusY: 6,
            fullEllipse: true,
            arcStart: 0d,
            sweep: Math.Tau,
            screenWidth: 10,
            screenHeight: 8,
            plotPixel: (x, y) => plotted.Add((x, y)));

        Assert.NotEmpty(plotted);
        Assert.All(plotted, point =>
        {
            Assert.InRange(point.X, 0, 9);
            Assert.InRange(point.Y, 0, 7);
        });
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DrawCircle_IgnoresNonFiniteAngles(double invalidAngle)
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(20, 10, 4, 12, relativeCenter: false, startAngle: invalidAngle, endAngle: null, aspect: 1, drawStartRadius: false, drawEndRadius: false);
        service.DrawCircle(20, 10, 4, 12, relativeCenter: false, startAngle: null, endAngle: invalidAngle, aspect: 1, drawStartRadius: false, drawEndRadius: false);

        Assert.Equal(0, service.GetPixel(24, 10));
        Assert.Equal(0, service.GetPixel(20, 14));
        Assert.Equal(0, service.GetPixel(16, 10));
        Assert.Equal(0, service.GetPixel(20, 6));
    }

    [Fact]
    public void DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.SetPixel(10, 20, 7);
        service.DrawCircle(5, 6, 2, 14, relativeCenter: true, startAngle: 0, endAngle: Math.PI / 2, aspect: 1, drawStartRadius: true, drawEndRadius: false);

        Assert.Equal(14, service.GetPixel(15, 26));
        Assert.Equal(14, service.GetPixel(17, 26));
        Assert.Equal(14, service.GetPixel(15, 24));
        Assert.Equal(15, service.CurrentGraphicsX);
        Assert.Equal(26, service.CurrentGraphicsY);
    }

    [Fact]
    public void DrawCircle_LargeRadiusOutsideScreenStillUpdatesCenterPosition()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawCircle(160, 100, 10_000, 13);

        Assert.Equal(160, service.CurrentGraphicsX);
        Assert.Equal(100, service.CurrentGraphicsY);
    }

    [Fact]
    public void Paint_FillsLargeRectangleInteriorWithIterativeScanlinePath()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(20, 20, 120, 80, 9, GraphicsLineKind.Box);
        service.Paint(60, 40, 6, 9);

        Assert.Equal(9, service.GetPixel(20, 20));
        Assert.Equal(6, service.GetPixel(60, 40));
        Assert.Equal(6, service.GetPixel(119, 79));
        Assert.Equal(0, service.GetPixel(121, 81));
    }

    [Fact]
    public void Paint_FillsRegionWithoutRecoloringBorder()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 5, 5, 9, GraphicsLineKind.Box);
        service.Paint(3, 3, 6, 9);

        Assert.Equal(6, service.GetPixel(3, 3));
        Assert.Equal(9, service.GetPixel(1, 1));
        Assert.Equal(0, service.GetPixel(0, 0));
    }

    [Fact]
    public void Paint_StartOutsideScreenLeavesFramebufferAndPositionUnchanged()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.DrawLine(1, 1, 5, 5, 9, GraphicsLineKind.Box);
        service.SetGraphicsCurrentPosition(12, 13);
        service.Paint(-1, -1, 6, 9);

        Assert.Equal(0, service.GetPixel(3, 3));
        Assert.Equal(9, service.GetPixel(1, 1));
        Assert.Equal(-1, service.CurrentGraphicsX);
        Assert.Equal(-1, service.CurrentGraphicsY);
    }

    [Fact]
    public void Paint_PatternFillAndBackgroundPatternRepaintRegion()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.SetTextColor(6, TextBackgroundMode.Color, 1);
        service.DrawLine(1, 1, 4, 4, 9, GraphicsLineKind.Box);
        var oldPattern = new byte[] { 170, 85 };
        var newPattern = new byte[] { 85, 170 };

        service.Paint(GraphicsPaintRequest.CreatePattern(2, 2, oldPattern, 9, service.CurrentForegroundColor, service.CurrentBackgroundColor));
        Assert.Equal(6, service.GetPixel(2, 2));
        Assert.Equal(1, service.GetPixel(3, 2));
        Assert.Equal(1, service.GetPixel(2, 3));
        Assert.Equal(6, service.GetPixel(3, 3));

        service.Paint(GraphicsPaintRequest.CreatePattern(2, 2, newPattern, 9, service.CurrentForegroundColor, service.CurrentBackgroundColor, oldPattern));
        Assert.Equal(1, service.GetPixel(2, 2));
        Assert.Equal(6, service.GetPixel(3, 2));
        Assert.Equal(6, service.GetPixel(2, 3));
        Assert.Equal(1, service.GetPixel(3, 3));
        Assert.Equal(2, service.CurrentGraphicsX);
        Assert.Equal(2, service.CurrentGraphicsY);
    }

    [Fact]
    public void GraphicsState_TracksCurrentPositionAndColors()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.SetTextColor(6, TextBackgroundMode.Color, 2);
        service.SetPixel(10, 20, service.CurrentForegroundColor);
        service.SetPixel(service.CurrentGraphicsX + 5, service.CurrentGraphicsY + 4, service.CurrentForegroundColor);
        service.DrawLine(service.CurrentGraphicsX, service.CurrentGraphicsY, service.CurrentGraphicsX + 15, service.CurrentGraphicsY + 16, service.CurrentForegroundColor, GraphicsLineKind.Normal);

        Assert.Equal(6, service.CurrentForegroundColor);
        Assert.Equal(2, service.CurrentBackgroundColor);
        Assert.False(service.IsTextBackgroundTransparent);
        Assert.Equal(30, service.CurrentGraphicsX);
        Assert.Equal(40, service.CurrentGraphicsY);
        Assert.Equal(6, service.GetPixel(10, 20));
        Assert.Equal(6, service.GetPixel(15, 24));
    }

    [Fact]
    public void Viewport_AbsoluteAndRelativeCoordinatesTranslatePixelsAndPreservePhysicalPosition()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        Assert.True(service.SetViewport(10, 10, 20, 20, fillColor: 3, borderColor: 9, useScreenCoordinates: true));
        service.SetPixel(11, 11, 6);

        Assert.Equal(9, service.GetPixel(10, 10));
        Assert.Equal(6, service.GetPixel(11, 11));

        Assert.True(service.SetViewport(1, 1, 3, 3, fillColor: 2, borderColor: 8, useScreenCoordinates: false));
        Assert.True(service.ViewportEnabled);
        Assert.True(service.ViewportRelativeCoordinates);
        Assert.Equal(0, service.CurrentGraphicsX);
        Assert.Equal(0, service.CurrentGraphicsY);

        service.SetPixel(0, 0, 7);

        Assert.Equal(7, service.GetPixel(0, 0));
        Assert.Equal(8, service.GetPixel(2, 0));

        service.ResetViewport();

        Assert.False(service.ViewportEnabled);
        Assert.Equal(11, service.CurrentGraphicsX);
        Assert.Equal(11, service.CurrentGraphicsY);
        Assert.Equal(7, service.GetPixel(11, 11));
        Assert.Equal(8, service.GetPixel(13, 11));
        Assert.Equal(2, service.GetPixel(12, 12));
    }

    [Fact]
    public void Window_ScreenAndCartesianCoordinatesTranslatePixelsAndPreservePhysicalPosition()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        Assert.True(service.SetViewport(10, 10, 20, 20, fillColor: null, borderColor: null, useScreenCoordinates: true));
        Assert.True(service.SetWindow(100, 200, 110, 210, useScreenCoordinates: true));

        service.SetPixel(104, 205, 6);

        Assert.True(service.WindowEnabled);
        Assert.Equal(6, service.GetPixel(104, 205));
        Assert.Equal(14, ((IGraphicsHost)service).CurrentViewportX);
        Assert.Equal(15, ((IGraphicsHost)service).CurrentViewportY);
        Assert.Equal(104, service.CurrentGraphicsX);
        Assert.Equal(205, service.CurrentGraphicsY);
        Assert.Equal(6, service.GetAbsolutePixel(14, 15));

        service.ResetWindow();

        Assert.False(service.WindowEnabled);
        Assert.Equal(14, service.CurrentGraphicsX);
        Assert.Equal(15, service.CurrentGraphicsY);

        Assert.True(service.SetViewport(2, 3, 8, 9, fillColor: null, borderColor: null, useScreenCoordinates: false));
        Assert.Equal(2, service.CurrentGraphicsX);
        Assert.Equal(2, service.CurrentGraphicsY);

        Assert.True(service.SetWindow(100, 210, 110, 200, useScreenCoordinates: false));

        service.SetPixel(104, 205, 7);

        Assert.Equal(7, service.GetPixel(104, 205));
        Assert.Equal(2, ((IGraphicsHost)service).CurrentViewportX);
        Assert.Equal(3, ((IGraphicsHost)service).CurrentViewportY);
        Assert.Equal(104, service.CurrentGraphicsX);
        Assert.Equal(205, service.CurrentGraphicsY);
        Assert.Equal(7, service.GetAbsolutePixel(14, 16));

        service.ResetWindow();

        Assert.Equal(2, service.CurrentGraphicsX);
        Assert.Equal(3, service.CurrentGraphicsY);
    }

    [Fact]
    public void SetTextColor_TransparentBackgroundPreservesNumericBackgroundForPreset()
    {
        var service = new ProgramConsoleService();

        service.ActivateScreen(1);
        service.SetTextColor(6, TextBackgroundMode.Color, 2);
        service.SetTextColor(15, TextBackgroundMode.Transparent);

        Assert.Equal(15, service.CurrentForegroundColor);
        Assert.Equal(2, service.CurrentBackgroundColor);
        Assert.True(service.IsTextBackgroundTransparent);
    }

    [Fact]
    public void ClearOutput_ClearsTranscriptWithoutResettingPendingInput()
    {
        var service = new ProgramConsoleService();
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.WriteOutput("old");
        dispatcher.BeginInvoke(() =>
        {
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.ClearOutput();
            Assert.True(service.IsWaitingForInput);
            Assert.Equal("Name? ", service.CurrentPromptText);
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.Equal("Name? Ada", service.OutputText);
    }

    [Fact]
    public void ConsoleActions_WriteTraceEntries()
    {
        var traceService = new FakeIdeTraceService();
        var service = new ProgramConsoleService(traceService);
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.BeginSession();
        service.WriteOutput("hello");

        dispatcher.BeginInvoke(() =>
        {
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        var result = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Equal("Ada", result);
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.begin");
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.output" && entry.Message == "hello");
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.input.request" && entry.Message.Contains("prompt=\"Name?\"", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "console.input.submit" && entry.Message.Contains("Ada", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsoleActions_WriteStructuredEventLogEntries()
    {
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramConsoleService(eventLogService: eventLogService);
        var dispatcher = Dispatcher.CurrentDispatcher;

        service.BeginSession();
        service.WriteOutput("hello");

        dispatcher.BeginInvoke(() =>
        {
            service.CurrentInputText = "Ada";
            service.SubmitInput();
        });

        _ = service.RequestInput(new ProgramInputRequest(1, "Name", 1));

        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "console.session-begin"
                && entry.Category == "output");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "output.changed"
                && entry.TextPreview == "hello");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "input.requested"
                && entry.Category == "input");
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.EventName == "input.submitted"
                && entry.TextPreview == "Ada");
    }

    [Fact]
    public void ScreenActions_WriteGraphicsTraceAndEventLogEntries()
    {
        var traceService = new FakeIdeTraceService();
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramConsoleService(traceService, eventLogService);

        service.ActivateScreen(5);
        service.EnqueueKey("ESC");
        Assert.Equal("ESC", service.ReadKey());
        service.EnqueueMouseMove();
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Move, 0), service.ReadMouseEvent());
        service.DeactivateScreen();

        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.activated" && entry.Message.Contains("mode=5", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.deactivated" && entry.Message.Contains("mode=5", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.frame.summary" && entry.Message.Contains("final=True", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.input.summary"
            && entry.Message.Contains("keysEnqueued=1", StringComparison.Ordinal)
            && entry.Message.Contains("keysRead=1", StringComparison.Ordinal)
            && entry.Message.Contains("mouseEventsEnqueued=1", StringComparison.Ordinal)
            && entry.Message.Contains("mouseEventsRead=1", StringComparison.Ordinal));
        Assert.DoesNotContain(traceService.Entries, static entry => entry.Category == "screen.key.enqueued");
        Assert.DoesNotContain(traceService.Entries, static entry => entry.Category == "screen.key.read");
        Assert.DoesNotContain(traceService.Entries, static entry => entry.Category == "screen.mouse.enqueued");
        Assert.DoesNotContain(traceService.Entries, static entry => entry.Category == "screen.mouse.read");

        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.activated");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.frame.summary");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.input.summary");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.deactivated");
        Assert.DoesNotContain(eventLogService.Entries, static entry => entry.EventName == "screen.key.enqueued");
        Assert.DoesNotContain(eventLogService.Entries, static entry => entry.EventName == "screen.key.read");
        Assert.DoesNotContain(eventLogService.Entries, static entry => entry.EventName == "screen.mouse.enqueued");
        Assert.DoesNotContain(eventLogService.Entries, static entry => entry.EventName == "screen.mouse.read");
    }

    [Fact]
    public void ScreenOutput_WritesGraphicsOutputTraceAndEventLogEntries()
    {
        var traceService = new FakeIdeTraceService();
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramConsoleService(traceService, eventLogService);

        service.ActivateScreen(1);
        service.WriteGraphicsText("A", newLine: false);
        service.WriteGraphicsText("B", newLine: true);
        service.WriteGraphicsText("C", newLine: false);
        service.DeactivateScreen();

        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.output" && entry.Message == "AB");
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.output" && entry.Message == "C");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.output" && entry.TextPreview == "AB");
        Assert.Contains(eventLogService.Entries, static entry => entry.Category == "graphics" && entry.EventName == "screen.output" && entry.TextPreview == "C");
    }

    [Fact]
    public void DeactivateScreen_WritesFinalInputSummaryAfterPeriodicSummary()
    {
        var traceService = new FakeIdeTraceService();
        var service = new ProgramConsoleService(traceService);

        service.ActivateScreen(5);
        service.EnqueueKey("ESC");
        Assert.Equal("ESC", service.ReadKey());
        for (var i = 0; i < 119; i++)
        {
            service.ClearGraphics();
        }

        service.DeactivateScreen();

        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.input.summary"
            && entry.Message.Contains("final=False", StringComparison.Ordinal));
        Assert.Contains(traceService.Entries, static entry => entry.Category == "screen.input.summary"
            && entry.Message.Contains("final=True", StringComparison.Ordinal)
            && entry.Message.Contains("keysEnqueued=1", StringComparison.Ordinal)
            && entry.Message.Contains("keysRead=1", StringComparison.Ordinal));
    }

    private static bool HasCellPixels(ScreenFramebufferSnapshot? frame, int row, int column)
    {
        if (frame is null)
        {
            return false;
        }

        return GetCellPixels(frame, row, column).Any(static pixel => pixel != 0xFF000000);
    }

    private sealed class FakeIdeTraceService : IIdeTraceService
    {
        public List<(string Category, string Message)> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public void BeginSession(string sessionName)
        {
        }

        public void Write(string category, string message)
        {
            Entries.Add((category, message));
        }

        public void WriteException(string category, Exception exception, string? note = null)
        {
            Entries.Add((category, note ?? exception.Message));
        }
    }

    private sealed class FakeIdeEventLogService : IIdeEventLogService
    {
        public List<IdeEventLogEntry> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public string SessionId => "test";

        public void BeginSession(string sessionName)
        {
        }

        public void Write(IdeEventLogEntry entry)
        {
            Entries.Add(entry);
        }

        public void Flush()
        {
        }

        public int DeleteInactiveLogs() => 0;

        public void Dispose()
        {
        }
    }

    private sealed class FixedScreenModeSizeProvider : IScreenModeSizeProvider
    {
        private readonly (int Width, int Height) _screen10Size;

        public FixedScreenModeSizeProvider((int Width, int Height) screen10Size)
        {
            _screen10Size = screen10Size;
        }

        public (int Width, int Height) GetSize(int mode) => mode switch
        {
            0 => (640, 300),
            1 => (320, 200),
            2 => (640, 480),
            3 => (800, 600),
            4 => (1024, 768),
            5 => (1280, 720),
            6 => (1366, 768),
            7 => (1600, 900),
            8 => (1920, 1080),
            9 => (2560, 1440),
            10 => _screen10Size,
            _ => (0, 0)
        };
    }
}
