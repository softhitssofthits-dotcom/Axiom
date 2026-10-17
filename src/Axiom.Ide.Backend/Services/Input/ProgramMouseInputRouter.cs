using System.Windows;
using System.Windows.Input;

namespace Axiom.Ide.Services.Input;

internal static class ProgramMouseInputRouter
{
    public static void HandlePreviewMouseMove(IProgramConsoleService inputSink, Point position)
    {
        ArgumentNullException.ThrowIfNull(inputSink);
        inputSink.PublishMouseMove(ToCoordinate(position.X), ToCoordinate(position.Y));
    }

    public static void HandlePreviewMouseDown(IProgramConsoleService inputSink, Point position, MouseButton button)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        if (TryMapButton(button, out var buttonNumber))
        {
            inputSink.PublishMouseButtonPress(buttonNumber, ToCoordinate(position.X), ToCoordinate(position.Y));
        }
        else
        {
            inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        }
    }

    public static void HandlePreviewMouseUp(IProgramConsoleService inputSink, Point position, MouseButton button)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        if (TryMapButton(button, out var buttonNumber))
        {
            inputSink.PublishMouseButtonRelease(buttonNumber, ToCoordinate(position.X), ToCoordinate(position.Y));
        }
        else
        {
            inputSink.SetMousePosition(ToCoordinate(position.X), ToCoordinate(position.Y));
        }
    }

    public static void HandlePreviewMouseWheel(IProgramConsoleService inputSink, Point position, int delta)
    {
        ArgumentNullException.ThrowIfNull(inputSink);
        inputSink.PublishMouseWheel(delta, ToCoordinate(position.X), ToCoordinate(position.Y));
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
