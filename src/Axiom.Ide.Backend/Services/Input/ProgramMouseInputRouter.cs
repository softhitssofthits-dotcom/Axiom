using System.Windows;
using System.Windows.Input;

namespace Axiom.Ide.Services.Input;

internal static class ProgramMouseInputRouter
{
    public static void HandlePreviewMouseMove(IProgramConsoleService inputSink, Point position)
    {
        ArgumentNullException.ThrowIfNull(inputSink);
        inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        inputSink.EnqueueMouseMove();
    }

    public static void HandlePreviewMouseDown(IProgramConsoleService inputSink, Point position, MouseButton button)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        if (TryMapButton(button, out var buttonNumber))
        {
            inputSink.EnqueueMouseButtonPress(buttonNumber);
            inputSink.SetMouseButtonDown(buttonNumber);
        }
    }

    public static void HandlePreviewMouseUp(IProgramConsoleService inputSink, Point position, MouseButton button)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        if (TryMapButton(button, out var buttonNumber))
        {
            inputSink.EnqueueMouseButtonRelease(buttonNumber);
            inputSink.SetMouseButtonUp(buttonNumber);
        }
    }

    public static void HandlePreviewMouseWheel(IProgramConsoleService inputSink, Point position, int delta)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        inputSink.EnqueueMouseWheel(delta);
    }

    public static void HandleMouseLeave(IProgramConsoleService inputSink)
    {
        ArgumentNullException.ThrowIfNull(inputSink);
        inputSink.ClearMouseState();
    }

    private static bool TryMapButton(MouseButton button, out int buttonNumber)
    {
        buttonNumber = button switch
        {
            MouseButton.Left => 1,
            MouseButton.Right => 2,
            MouseButton.Middle => 3,
            _ => 0
        };

        return buttonNumber != 0;
    }

    private static long ToCoordinate(double value) => (long)Math.Round(value, MidpointRounding.AwayFromZero);
}
