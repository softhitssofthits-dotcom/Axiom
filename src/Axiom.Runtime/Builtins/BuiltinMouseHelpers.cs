using Axiom.Core;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinMouseHelpers
{
    public static bool IsSupportedMouseButton(int buttonNumber) => buttonNumber is >= 1 and <= 3;

    public static bool IsSupportedMouseHandlerTarget(int buttonNumber, MouseInputEventKind eventKind) =>
        eventKind is MouseInputEventKind.Move or MouseInputEventKind.Wheel
            ? buttonNumber == 0
            : IsSupportedMouseButton(buttonNumber) && eventKind is MouseInputEventKind.Press or MouseInputEventKind.Release;

    public static bool IsSupportedMouseEvent(MouseInputEvent mouseEvent) =>
        mouseEvent.Kind == MouseInputEventKind.Move
            ? mouseEvent.ButtonNumber == 0
            : mouseEvent.Kind == MouseInputEventKind.Wheel
                ? mouseEvent.ButtonNumber != 0
                : IsSupportedMouseButton(mouseEvent.ButtonNumber) && mouseEvent.Kind is MouseInputEventKind.Press or MouseInputEventKind.Release;

    public static MouseHandlerKey GetMouseHandlerKey(MouseInputEvent mouseEvent) =>
        new(mouseEvent.Kind, mouseEvent.Kind is MouseInputEventKind.Move or MouseInputEventKind.Wheel ? 0 : mouseEvent.ButtonNumber);

    public static void SetMouseHandlerEnabled(
        Dictionary<MouseHandlerKey, MouseHandlerRegistration> handlers,
        int buttonNumber,
        MouseInputEventKind eventKind,
        bool isEnabled)
    {
        if (!IsSupportedMouseHandlerTarget(buttonNumber, eventKind)
            || !handlers.TryGetValue(new MouseHandlerKey(eventKind, buttonNumber), out var registration))
        {
            return;
        }

        handlers[new MouseHandlerKey(eventKind, buttonNumber)] = registration with { IsEnabled = isEnabled };
    }

    public static bool ConsumeMouseButtonPress(
        int buttonNumber,
        Queue<MouseInputEvent> bufferedEvents,
        Func<MouseInputEvent> readMouseEvent)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return false;
        }

        var deferredEvents = new List<MouseInputEvent>();
        var matched = false;

        while (TryReadMouseEvent(bufferedEvents, readMouseEvent, out var candidate))
        {
            if (!matched && candidate.Kind == MouseInputEventKind.Press && candidate.ButtonNumber == buttonNumber)
            {
                matched = true;
                continue;
            }

            deferredEvents.Add(candidate);
        }

        foreach (var deferredEvent in deferredEvents)
        {
            bufferedEvents.Enqueue(deferredEvent);
        }

        return matched;
    }

    public static bool TryReadMouseEvent(
        Queue<MouseInputEvent> bufferedEvents,
        Func<MouseInputEvent> readMouseEvent,
        out MouseInputEvent mouseEvent)
    {
        if (bufferedEvents.Count > 0)
        {
            mouseEvent = bufferedEvents.Dequeue();
            return true;
        }

        mouseEvent = readMouseEvent();
        return IsSupportedMouseEvent(mouseEvent);
    }

    public static long TranslateMouseXToRuntimeCoordinates(
        IGraphicsHost host,
        long absoluteMouseX,
        long absoluteMouseY)
    {
        if (!host.IsCanvasActive)
        {
            return absoluteMouseX;
        }

        return TryTranslateMouseAbsoluteToLogical(host, absoluteMouseX, absoluteMouseY, out var logicalX, out _)
            ? logicalX
            : -1;
    }

    public static long TranslateMouseYToRuntimeCoordinates(
        IGraphicsHost host,
        long absoluteMouseX,
        long absoluteMouseY)
    {
        if (!host.IsCanvasActive)
        {
            return absoluteMouseY;
        }

        return TryTranslateMouseAbsoluteToLogical(host, absoluteMouseX, absoluteMouseY, out _, out var logicalY)
            ? logicalY
            : -1;
    }

    public static bool TryTranslateMouseAbsoluteToLogical(
        IGraphicsHost host,
        long absoluteMouseX,
        long absoluteMouseY,
        out long logicalX,
        out long logicalY)
    {
        if (absoluteMouseX < 0 || absoluteMouseY < 0)
        {
            logicalX = -1;
            logicalY = -1;
            return false;
        }

        return GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(host, absoluteMouseX, absoluteMouseY, out logicalX, out logicalY);
    }

    public readonly record struct MouseHandlerKey(MouseInputEventKind EventKind, int ButtonNumber);
}

internal sealed record MouseHandlerRegistration(RuntimeFunctionValue Handler, bool PassEvent, bool IsEnabled);
