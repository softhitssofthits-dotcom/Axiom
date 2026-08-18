using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private const int MaxMouseEventsPerDispatch = 64;
    private readonly Dictionary<BuiltinMouseHelpers.MouseHandlerKey, MouseHandlerRegistration> _mouseHandlers = [];
    private readonly Queue<MouseInputEvent> _bufferedMouseEvents = [];
    private int _mouseHandlerDepth;

    private long ReadMouseX()
    {
        RequireDrawableSurfaceActive("MOUSEX", FallbackRuntimeLocation);
        return BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates(_host, _host.GetMouseX(), _host.GetMouseY());
    }

    private long ReadMouseY()
    {
        RequireDrawableSurfaceActive("MOUSEY", FallbackRuntimeLocation);
        return BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates(_host, _host.GetMouseX(), _host.GetMouseY());
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

        while (processedEvents < MaxMouseEventsPerDispatch && BuiltinMouseHelpers.TryReadMouseEvent(_bufferedMouseEvents, _host.ReadMouseEvent, out var mouseEvent))
        {
            processedEvents++;
            var handlerKey = BuiltinMouseHelpers.GetMouseHandlerKey(mouseEvent);
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
        if (!BuiltinMouseHelpers.IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            return;
        }

        _mouseHandlers[new BuiltinMouseHelpers.MouseHandlerKey(eventKind, buttonNumber)] = new MouseHandlerRegistration(handler, passEvent, IsEnabled: true);
    }

    private void UnregisterMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        if (BuiltinMouseHelpers.IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            _mouseHandlers.Remove(new BuiltinMouseHelpers.MouseHandlerKey(eventKind, buttonNumber));
        }
    }

    private void PauseMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        BuiltinMouseHelpers.SetMouseHandlerEnabled(_mouseHandlers, buttonNumber, eventKind, isEnabled: false);
    }

    private void ResumeMouseHandler(int buttonNumber, MouseInputEventKind eventKind)
    {
        BuiltinMouseHelpers.SetMouseHandlerEnabled(_mouseHandlers, buttonNumber, eventKind, isEnabled: true);
    }

    private bool ConsumeMouseButtonPress(int buttonNumber)
    {
        RequireDrawableSurfaceActive("MOUSEPRESS", FallbackRuntimeLocation);
        return BuiltinMouseHelpers.ConsumeMouseButtonPress(buttonNumber, _bufferedMouseEvents, _host.ReadMouseEvent);
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
}
