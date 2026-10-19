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

    private static MouseInputEventKind ToMouseInputEventKind(int eventKind) => eventKind switch
    {
        1 => MouseInputEventKind.Release,
        2 => MouseInputEventKind.Move,
        3 => MouseInputEventKind.Wheel,
        _ => MouseInputEventKind.Press
    };
}
