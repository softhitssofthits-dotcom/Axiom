using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteStatementBoundary(int instructionIndex)
    {
        if (_keyboardHandlers.Count == 0 && _mouseHandlers.Count == 0)
        {
            return;
        }

        if (IsInputHandlerFrameActive())
        {
            return;
        }

        var location = ResolveLocation(CurrentFrame.Routine.Id, instructionIndex);
        var mouseDone = _mouseHandlers.Count == 0
            || (!_host.IsScreenActive && !_host.IsCanvasActive);
        var keysDone = _keyboardHandlers.Count == 0;
        var processedEvents = 0;
        var bufferedBudget = Math.Min(_bufferedMouseEvents.Count, MaxMouseEventsPerDispatch / 2);
        var deferredEvents = new List<MouseInputEvent>();
        var deferredKeys = new List<string>();

        if (!mouseDone)
        {
            DiscardBufferedEventsAfterClear();
        }

        void FlushMouseEvents()
        {
            foreach (var deferredEvent in deferredEvents)
            {
                if (_bufferedMouseEvents.Count >= MaxBufferedMouseEvents)
                {
                    break;
                }

                _bufferedMouseEvents.Enqueue(deferredEvent);
            }

            deferredEvents.Clear();
        }

        void FlushKeys()
        {
            foreach (var deferredKey in deferredKeys)
            {
                _bufferedKeys.Enqueue(deferredKey);
            }

            deferredKeys.Clear();
        }

        VmCallbackStep Next()
        {
            while (true)
            {
                if (!mouseDone)
                {
                    MouseInputEvent mouseEvent;
                    if (processedEvents < bufferedBudget)
                    {
                        mouseEvent = _bufferedMouseEvents.Dequeue();
                    }
                    else
                    {
                        mouseEvent = _host.ReadMouseEvent();
                        if (!BuiltinMouseHelpers.IsSupportedMouseEvent(mouseEvent))
                        {
                            FlushMouseEvents();
                            mouseDone = true;
                            continue;
                        }
                    }

                    processedEvents++;
                    var handlerKey = BuiltinMouseHelpers.GetMouseHandlerKey(mouseEvent);
                    if (!_mouseHandlers.TryGetValue(handlerKey, out var registration) || !registration.IsEnabled)
                    {
                        deferredEvents.Add(mouseEvent);
                        if (processedEvents >= MaxMouseEventsPerDispatch)
                        {
                            FlushMouseEvents();
                            mouseDone = true;
                        }

                        continue;
                    }

                    return new VmCallbackInvoke(
                        registration.Handler,
                        BuildMouseHandlerArguments(registration, mouseEvent),
                        location,
                        _ => Next(),
                        IsInputHandler: true);
                }

                if (!keysDone)
                {
                    if (!TryReadKeyboardKey(out var keyText))
                    {
                        FlushKeys();
                        keysDone = true;
                        continue;
                    }

                    var normalizedKey = KeyboardInputNormalizer.Normalize(keyText);
                    if (!TryGetKeyboardHandler(normalizedKey, out var registration))
                    {
                        deferredKeys.Add(keyText);
                        continue;
                    }

                    var handlerKeyText = registration.Chord.IsSingleKey
                        ? normalizedKey
                        : registration.Chord.CanonicalText;
                    var arguments = registration.PassKeyText
                        ? new[] { RuntimeValue.FromString(handlerKeyText) }
                        : Array.Empty<RuntimeValue>();

                    return new VmCallbackInvoke(
                        registration.Handler,
                        arguments,
                        location,
                        _ => Next(),
                        IsInputHandler: true);
                }

                return new VmCallbackComplete(RuntimeValue.Void);
            }
        }

        try
        {
            RunCallbackChain(Next());
        }
        catch (VmCallbackSuspension suspension)
        {
            suspension.Completion = _ => { };
            suspension.IsInputHandler = true;
            throw;
        }
    }

    private RuntimeValue[] BuildMouseHandlerArguments(
        MouseHandlerRegistration registration,
        MouseInputEvent mouseEvent)
    {
        if (!registration.PassEvent)
        {
            return Array.Empty<RuntimeValue>();
        }

        // Один снимок позиции на обработчик: X и Y не могут относиться
        // к разным событиям ввода.
        _host.GetMousePosition(out var absoluteMouseX, out var absoluteMouseY);
        var mouseX = BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates(_host, absoluteMouseX, absoluteMouseY);
        var mouseY = BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates(_host, absoluteMouseX, absoluteMouseY);

        return
        [
            RuntimeValue.FromInteger(mouseEvent.ButtonNumber),
            RuntimeValue.FromLong(mouseX),
            RuntimeValue.FromLong(mouseY)
        ];
    }
}
