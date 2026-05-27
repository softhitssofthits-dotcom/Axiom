using Axiom.Ide.Graphics;
using Axiom.Ide.Services.Input;
using System.Runtime.InteropServices;

namespace Axiom.Ide.Tests;

public sealed class NativeGraphicsScreenWindowTests
{
    private const int WmPaint = 0x000F;
    private const int WmMouseMove = 0x0200;

    [Theory]
    [InlineData(0x25)] // LEFT
    [InlineData(0x20)] // SPACE
    [InlineData(0x0D)] // ENTER
    public void ShouldEnqueueKeyDown_AllowsNamedKeys(int virtualKey)
    {
        Assert.True(NativeGraphicsScreenWindow.ShouldEnqueueKeyDown(virtualKey));
    }

    [Theory]
    [InlineData(0x41)] // A
    [InlineData(0x5A)] // Z
    [InlineData(0x30)] // 0
    [InlineData(0x39)] // 9
    public void ShouldEnqueueKeyDown_LeavesPrintableAlphanumericKeysForCharMessage(int virtualKey)
    {
        Assert.False(NativeGraphicsScreenWindow.ShouldEnqueueKeyDown(virtualKey));
    }

    [Theory]
    [InlineData('A', true)]
    [InlineData('я', true)]
    [InlineData(' ', false)]
    [InlineData('\r', false)]
    [InlineData('\b', false)]
    public void ShouldEnqueueChar_FiltersCharactersAlreadyRepresentedByNamedKeyDown(char character, bool expected)
    {
        Assert.Equal(expected, NativeGraphicsScreenWindow.ShouldEnqueueChar(character));
    }

    [Fact]
    public void WindowCloseMessage_DestroysWindowAndRaisesClosed()
    {
        var service = new ProgramConsoleService();
        using var window = new NativeGraphicsScreenWindow(service, 64, 48);
        var closed = false;
        window.Closed += (_, _) => closed = true;

        SendMessage(window.Hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);

        Assert.True(closed);
        Assert.Equal(IntPtr.Zero, window.Hwnd);
    }

    [Fact]
    public void MouseMoveMessage_KeepsLastKnownCoordinatesUntilAnotherMouseInputArrives()
    {
        var service = new ProgramConsoleService();
        using var window = new NativeGraphicsScreenWindow(service, 64, 48);

        SendMessage(window.Hwnd, WmMouseMove, IntPtr.Zero, MakeMouseLParam(11, 22));

        Assert.Equal(11, service.GetMouseX());
        Assert.Equal(22, service.GetMouseY());

        SendMessage(window.Hwnd, WmPaint, IntPtr.Zero, IntPtr.Zero);

        Assert.Equal(11, service.GetMouseX());
        Assert.Equal(22, service.GetMouseY());
    }

    private static IntPtr MakeMouseLParam(short x, short y) =>
        new((y << 16) | (ushort)x);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
