using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private const int MaxMouseEventsPerDispatch = 64;
    private readonly Dictionary<MouseHandlerKey, MouseHandlerRegistration> _mouseHandlers = [];
    private readonly Queue<MouseInputEvent> _bufferedMouseEvents = [];
    private int _mouseHandlerDepth;

    private long ReadMouseX()
    {
        RequireDrawableSurfaceActive("MOUSEX", FallbackRuntimeLocation);
        return TranslateMouseXToRuntimeCoordinates(_host.GetMouseX(), _host.GetMouseY());
    }

    private long ReadMouseY()
    {
        RequireDrawableSurfaceActive("MOUSEY", FallbackRuntimeLocation);
        return TranslateMouseYToRuntimeCoordinates(_host.GetMouseX(), _host.GetMouseY());
    }

    private bool IsMouseButtonDown(int buttonNumber)
    {
        RequireDrawableSurfaceActive("MOUSEBUTTON", FallbackRuntimeLocation);
        return _host.IsMouseButtonDown(buttonNumber);
    }

    private void DispatchMouseHandlers(TextLocation callSiteLocation)
    {
        if (_mouseHandlers.Count == 0)
        {
            return;
        }

        if (!_host.IsScreenActive && !_host.IsCanvasActive)
        {
            return;
        }

        if (_mouseHandlerDepth > 0)
        {
            return;
        }

        var deferredEvents = new List<MouseInputEvent>();
        var processedEvents = 0;

        while (processedEvents < MaxMouseEventsPerDispatch && TryReadMouseEvent(out var mouseEvent))
        {
            processedEvents++;
            var handlerKey = GetMouseHandlerKey(mouseEvent);
            if (!_mouseHandlers.TryGetValue(handlerKey, out var registration) || !registration.IsEnabled)
            {
                deferredEvents.Add(mouseEvent);
                continue;
            }

            InvokeMouseHandler(registration, mouseEvent, callSiteLocation);
        }

        foreach (var deferredEvent in deferredEvents)
        {
            _bufferedMouseEvents.Enqueue(deferredEvent);
        }
    }

    private void RegisterMouseHandler(int buttonNumber, MouseInputEventKind eventKind, RuntimeFunctionValue handler, bool passEvent)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            return;
        }

        _mouseHandlers[new MouseHandlerKey(eventKind, buttonNumber)] = new MouseHandlerRegistration(handler, passEvent, IsEnabled: true);
    }

    private void UnregisterMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        if (IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            _mouseHandlers.Remove(new MouseHandlerKey(eventKind, buttonNumber));
        }
    }

    private void PauseMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        SetMouseHandlerEnabled(buttonNumber, eventKind, isEnabled: false);
    }

    private void ResumeMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        SetMouseHandlerEnabled(buttonNumber, eventKind, isEnabled: true);
    }

    private void SetMouseHandlerEnabled(int buttonNumber, MouseInputEventKind eventKind, bool isEnabled)
    {
        if (!IsSupportedMouseHandlerTarget(buttonNumber, eventKind)
            || !_mouseHandlers.TryGetValue(new MouseHandlerKey(eventKind, buttonNumber), out var registration))
        {
            return;
        }

        _mouseHandlers[new MouseHandlerKey(eventKind, buttonNumber)] = registration with { IsEnabled = isEnabled };
    }

    private bool ConsumeMouseButtonPress(int buttonNumber)
    {
        RequireDrawableSurfaceActive("MOUSEPRESS", FallbackRuntimeLocation);

        if (!IsSupportedMouseButton(buttonNumber))
        {
            return false;
        }

        var deferredEvents = new List<MouseInputEvent>();
        var matched = false;

        while (TryReadMouseEvent(out var candidate))
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
            _bufferedMouseEvents.Enqueue(deferredEvent);
        }

        return matched;
    }

    private bool TryReadMouseEvent(out MouseInputEvent mouseEvent)
    {
        if (_bufferedMouseEvents.Count > 0)
        {
            mouseEvent = _bufferedMouseEvents.Dequeue();
            return true;
        }

        mouseEvent = _host.ReadMouseEvent();
        return IsSupportedMouseEvent(mouseEvent);
    }

    private void InvokeMouseHandler(MouseHandlerRegistration registration, MouseInputEvent mouseEvent, TextLocation callSiteLocation)
    {
        _mouseHandlerDepth++;

        try
        {
            var mouseX = ReadMouseX();
            var mouseY = ReadMouseY();
            var arguments = registration.PassEvent
                ? new[]
                {
                    RuntimeValue.FromInteger(mouseEvent.ButtonNumber),
                    RuntimeValue.FromLong(mouseX),
                    RuntimeValue.FromLong(mouseY)
                }
                : Array.Empty<RuntimeValue>();

            _ = InvokeUserRoutine(registration.Handler, arguments, callSiteLocation);
        }
        finally
        {
            _mouseHandlerDepth--;
        }
    }

    private long TranslateMouseXToRuntimeCoordinates(long absoluteMouseX, long absoluteMouseY)
    {
        if (!_host.IsCanvasActive)
        {
            return absoluteMouseX;
        }

        return TryTranslateMouseAbsoluteToLogical(absoluteMouseX, absoluteMouseY, out var logicalX, out _)
            ? logicalX
            : -1;
    }

    private long TranslateMouseYToRuntimeCoordinates(long absoluteMouseX, long absoluteMouseY)
    {
        if (!_host.IsCanvasActive)
        {
            return absoluteMouseY;
        }

        return TryTranslateMouseAbsoluteToLogical(absoluteMouseX, absoluteMouseY, out _, out var logicalY)
            ? logicalY
            : -1;
    }

    private bool TryTranslateMouseAbsoluteToLogical(long absoluteMouseX, long absoluteMouseY, out long logicalX, out long logicalY)
    {
        if (absoluteMouseX < 0 || absoluteMouseY < 0)
        {
            logicalX = -1;
            logicalY = -1;
            return false;
        }

        return GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(_host, absoluteMouseX, absoluteMouseY, out logicalX, out logicalY);
    }

    private static bool IsSupportedMouseButton(int buttonNumber) => buttonNumber is >= 1 and <= 3;

    private static bool IsSupportedMouseHandlerTarget(int buttonNumber, MouseInputEventKind eventKind) =>
        eventKind is MouseInputEventKind.Move or MouseInputEventKind.Wheel
            ? buttonNumber == 0
            : IsSupportedMouseButton(buttonNumber) && eventKind is MouseInputEventKind.Press or MouseInputEventKind.Release;

    private static bool IsSupportedMouseEvent(MouseInputEvent mouseEvent) =>
        mouseEvent.Kind == MouseInputEventKind.Move
            ? mouseEvent.ButtonNumber == 0
            : mouseEvent.Kind == MouseInputEventKind.Wheel
                ? mouseEvent.ButtonNumber != 0
                : IsSupportedMouseButton(mouseEvent.ButtonNumber) && mouseEvent.Kind is MouseInputEventKind.Press or MouseInputEventKind.Release;

    private static MouseHandlerKey GetMouseHandlerKey(MouseInputEvent mouseEvent) => new(mouseEvent.Kind, mouseEvent.Kind is MouseInputEventKind.Move or MouseInputEventKind.Wheel ? 0 : mouseEvent.ButtonNumber);

    private readonly record struct MouseHandlerKey(MouseInputEventKind EventKind, int ButtonNumber);

    private readonly record struct MouseHandlerRegistration(RuntimeFunctionValue Handler, bool PassEvent, bool IsEnabled);
}
