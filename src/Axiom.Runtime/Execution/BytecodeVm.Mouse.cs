using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
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
            _bufferedMouseEvents.Enqueue(deferredEvent);
        }
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
        return BuiltinMouseHelpers.ConsumeMouseButtonPress(buttonNumber, _bufferedMouseEvents, _host.ReadMouseEvent);
    }

    private void InvokeMouseHandler(MouseHandlerRegistration registration, MouseInputEvent mouseEvent, TextLocation location)
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
