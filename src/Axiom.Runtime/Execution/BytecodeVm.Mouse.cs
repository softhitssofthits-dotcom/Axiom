using Axiom.Bytecode;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private readonly Dictionary<MouseHandlerKey, MouseHandlerRegistration> _mouseHandlers = [];
    private readonly Queue<MouseInputEvent> _bufferedMouseEvents = [];
    private int _mouseHandlerDepth;

    private long ReadMouseX()
    {
        RequireScreenActive("MOUSEX", FallbackRuntimeLocation);
        return _host.GetMouseX();
    }

    private long ReadMouseY()
    {
        RequireScreenActive("MOUSEY", FallbackRuntimeLocation);
        return _host.GetMouseY();
    }

    private bool IsMouseButtonDown(int buttonNumber)
    {
        RequireScreenActive("MOUSEBUTTON", FallbackRuntimeLocation);
        return _host.IsMouseButtonDown(buttonNumber);
    }

    private void DispatchMouseHandlers(TextLocation location)
    {
        if (!_host.IsScreenActive)
        {
            return;
        }

        if (_mouseHandlerDepth > 0)
        {
            return;
        }

        var deferredEvents = new List<MouseInputEvent>();

        while (TryReadMouseEvent(out var mouseEvent))
        {
            var handlerKey = GetMouseHandlerKey(mouseEvent);
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

        if (!IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            return;
        }

        var passEvent = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
        _mouseHandlers[new MouseHandlerKey(eventKind, buttonNumber)] = new MouseHandlerRegistration(handlerValue!, passEvent, IsEnabled: true);
    }

    private void ExecuteUnregisterMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
        if (IsSupportedMouseHandlerTarget(buttonNumber, eventKind))
        {
            _mouseHandlers.Remove(new MouseHandlerKey(eventKind, buttonNumber));
        }
    }

    private void ExecutePauseMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
        SetMouseHandlerEnabled(buttonNumber, eventKind, isEnabled: false);
    }

    private void ExecuteResumeMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
        var eventKind = ToMouseInputEventKind(instruction.OperandCount > 0 ? instruction.GetOperand(0) : 0);
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
        RequireScreenActive("MOUSEPRESS", FallbackRuntimeLocation);

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

    private void InvokeMouseHandler(MouseHandlerRegistration registration, MouseInputEvent mouseEvent, TextLocation location)
    {
        _mouseHandlerDepth++;

        try
        {
            var arguments = registration.PassEvent
                ? new[]
                {
                    RuntimeValue.FromInteger(mouseEvent.ButtonNumber),
                    RuntimeValue.FromLong(ReadMouseX()),
                    RuntimeValue.FromLong(ReadMouseY())
                }
                : Array.Empty<RuntimeValue>();

            _ = InvokeBytecodeFunction(registration.Handler, arguments, location);
        }
        finally
        {
            _mouseHandlerDepth--;
        }
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

    private static MouseInputEventKind ToMouseInputEventKind(int eventKind) => eventKind switch
    {
        1 => MouseInputEventKind.Release,
        2 => MouseInputEventKind.Move,
        3 => MouseInputEventKind.Wheel,
        _ => MouseInputEventKind.Press
    };

    private static MouseHandlerKey GetMouseHandlerKey(MouseInputEvent mouseEvent) => new(mouseEvent.Kind, mouseEvent.Kind is MouseInputEventKind.Move or MouseInputEventKind.Wheel ? 0 : mouseEvent.ButtonNumber);

    private readonly record struct MouseHandlerKey(MouseInputEventKind EventKind, int ButtonNumber);

    private readonly record struct MouseHandlerRegistration(RuntimeFunctionValue Handler, bool PassEvent, bool IsEnabled);
}
