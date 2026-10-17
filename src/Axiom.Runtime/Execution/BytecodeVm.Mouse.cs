using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private const int MaxMouseEventsPerDispatch = 64;
    // Отложенные события VM тоже ограничены: очередь хоста может быть
    // очищена, но собственный буфер живёт до следующего опроса.
    private const int MaxBufferedMouseEvents = 4096;
    private readonly Dictionary<BuiltinMouseHelpers.MouseHandlerKey, MouseHandlerRegistration> _mouseHandlers = [];
    private readonly Queue<MouseInputEvent> _bufferedMouseEvents = [];
    private long _observedMouseClearGeneration;
    private int _mouseHandlerDepth;

    private long ReadMouseX()
    {
        RequireDrawableSurfaceActive("MOUSEX", FallbackRuntimeLocation);
        _host.GetMousePosition(out var mouseX, out var mouseY);
        return BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates(_host, mouseX, mouseY);
    }

    private long ReadMouseY()
    {
        RequireDrawableSurfaceActive("MOUSEY", FallbackRuntimeLocation);
        _host.GetMousePosition(out var mouseX, out var mouseY);
        return BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates(_host, mouseX, mouseY);
    }

    private bool IsMouseButtonDown(int buttonNumber)
    {
        RequireDrawableSurfaceActive("MOUSEBUTTON", FallbackRuntimeLocation);
        return _host.IsMouseButtonDown(buttonNumber);
    }

    private void DispatchMouseHandlers(TextLocation location)
    {
        if (!_host.IsScreenActive && !_host.IsCanvasActive)
        {
            return;
        }

        if (_mouseHandlerDepth > 0)
        {
            return;
        }

        DiscardBufferedEventsAfterClear();
        var deferredEvents = new List<MouseInputEvent>();
        var processedEvents = 0;
        // Повторно отложенные события не должны занимать весь бюджет опроса.
        var bufferedBudget = Math.Min(_bufferedMouseEvents.Count, MaxMouseEventsPerDispatch / 2);

        while (processedEvents < MaxMouseEventsPerDispatch)
        {
            MouseInputEvent mouseEvent;
            if (processedEvents < bufferedBudget)
                mouseEvent = _bufferedMouseEvents.Dequeue();
            else
            {
                mouseEvent = _host.ReadMouseEvent();
                if (!BuiltinMouseHelpers.IsSupportedMouseEvent(mouseEvent)) break;
            }
            processedEvents++;
            var handlerKey = BuiltinMouseHelpers.GetMouseHandlerKey(mouseEvent);
            if (!_mouseHandlers.TryGetValue(handlerKey, out var registration) || !registration.IsEnabled)
            {
                deferredEvents.Add(mouseEvent);
                continue;
            }

            InvokeMouseHandler(registration, mouseEvent, location);
        }

        foreach (var deferredEvent in deferredEvents)
        {
            if (_bufferedMouseEvents.Count >= MaxBufferedMouseEvents)
            {
                break;
            }

            _bufferedMouseEvents.Enqueue(deferredEvent);
        }
    }

    private void DiscardBufferedEventsAfterClear()
    {
        var generation = _host.GetMouseClearGeneration();
        if (generation == _observedMouseClearGeneration)
        {
            return;
        }

        _observedMouseClearGeneration = generation;
        _bufferedMouseEvents.Clear();
    }

    private void ExecuteRegisterMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var handlerValue = PopValue(frame, instructionIndex).AsFunction();
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 1 ? instruction.GetOperand(1) : 0);

        if (!BuiltinMouseHelpers.IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            return;
        }

        var passEvent = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
        _mouseHandlers[new BuiltinMouseHelpers.MouseHandlerKey(eventKind, buttonNumber)] = new MouseHandlerRegistration(handlerValue!, passEvent, IsEnabled: true);
    }

    private void ExecuteUnregisterMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
        if (BuiltinMouseHelpers.IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            _mouseHandlers.Remove(new BuiltinMouseHelpers.MouseHandlerKey(eventKind, buttonNumber));
        }
    }

    private void ExecutePauseMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
        BuiltinMouseHelpers.SetMouseHandlerEnabled(_mouseHandlers, buttonNumber, eventKind, isEnabled: false);
    }

    private void ExecuteResumeMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
        BuiltinMouseHelpers.SetMouseHandlerEnabled(_mouseHandlers, buttonNumber, eventKind, isEnabled: true);
    }

    private bool ConsumeMouseButtonPress(int buttonNumber)
    {
        RequireDrawableSurfaceActive("MOUSEPRESS", FallbackRuntimeLocation);
        DiscardBufferedEventsAfterClear();
        return BuiltinMouseHelpers.ConsumeMouseButtonPress(buttonNumber, _bufferedMouseEvents, _host.ReadMouseEvent);
    }

    private void InvokeMouseHandler(MouseHandlerRegistration registration, MouseInputEvent mouseEvent, TextLocation location)
    {
        _mouseHandlerDepth++;

        try
        {
            // Один снимок позиции на обработчик: X и Y не могут относиться
            // к разным событиям ввода.
            _host.GetMousePosition(out var absoluteMouseX, out var absoluteMouseY);
            var mouseX = BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates(_host, absoluteMouseX, absoluteMouseY);
            var mouseY = BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates(_host, absoluteMouseX, absoluteMouseY);
            var arguments = registration.PassEvent
                ? new[]
                {
                    RuntimeValue.FromInteger(mouseEvent.ButtonNumber),
                    RuntimeValue.FromLong(mouseX),
                    RuntimeValue.FromLong(mouseY)
                }
                : Array.Empty<RuntimeValue>();

            _ = InvokeBytecodeFunction(registration.Handler, arguments, location);
        }
        finally
        {
            _mouseHandlerDepth--;
        }
    }

    private static MouseInputEventKind ToMouseInputEventKind(int eventKind) => eventKind switch
    {
        1 => MouseInputEventKind.Release,
        2 => MouseInputEventKind.Move,
        3 => MouseInputEventKind.Wheel,
        _ => MouseInputEventKind.Press
    };
}
