using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteRoutineCall(BytecodeCallFrame callerFrame,  VmInstruction instruction, int instructionIndex)
    {
        var targetRoutineId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);
        var byValArgumentCount = instruction.Opcode == Opcode.CallByRef ? instruction.GetOperand(2) : argumentCount;

        if (!_routines.TryGetValue(targetRoutineId, out var targetRoutine))
        {
            ReportInvalidRuntimeState(
                $"Не найдена вызываемая процедура с routine-id {targetRoutineId}.",
                ResolveLocation(callerFrame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        if (instruction.Opcode == Opcode.Call && argumentCount == 1
            && targetRoutine.ParameterCount == 1 && targetRoutine.ParameterVariables.Count == 1
            && targetRoutine.ParameterVariables[0].Type == TypeSymbol.Integer
            && callerFrame.EvaluationStack.TryGetImmediateInteger(out var integerArgument))
        {
            callerFrame.EvaluationStack.DiscardTop(1);
            var directCaptures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
            var callLocation = ResolveLocation(callerFrame.Routine.Id, instructionIndex);
            if (_callStack.Count >= MaxCallDepth)
            {
                ReportCallStackOverflow(targetRoutine.Name, callLocation);
                throw new RuntimeFailureException();
            }

            var template = _frameTemplatesByRoutineId[targetRoutine.Id];
            if (_cachedFrames.Remove(targetRoutine.Id, out var cachedFrame))
            {
                // Контейнер кадра возвращён после освобождения; саму ячейку
                // создаём заново, поскольку прежняя могла уйти в замыкание.
                cachedFrame.Arguments[0] = ReuseIntegerSlot(cachedFrame.Arguments[0], integerArgument);
                cachedFrame.Reset(cachedFrame.Arguments, directCaptures, callLocation, template.DefaultLocals);
                _callStack.Push(cachedFrame);
            }
            else
            {
                var slots = new[] { BytecodeVariableSlot.AdoptInteger(integerArgument) };
                _callStack.Push(CreateFrameWithSlots(targetRoutine, slots, directCaptures, callLocation, template));
            }
            return;
        }

        if (instruction.Opcode == Opcode.Call && argumentCount == 1
            && targetRoutine.ParameterCount == 1 && callerFrame.EvaluationStack.Count > 0)
        {
            ExecuteSingleValueCall(callerFrame, targetRoutine, instructionIndex);
            return;
        }

        if (instruction.Opcode == Opcode.Call && argumentCount == 2
            && targetRoutine.ParameterCount == 2 && targetRoutine.ParameterVariables.Count == 2
            && targetRoutine.ParameterVariables[0].Type == TypeSymbol.Integer
            && targetRoutine.ParameterVariables[1].Type == TypeSymbol.Integer
            && callerFrame.EvaluationStack.TryGetIntegerOperands(out var firstInteger, out var secondInteger))
        {
            ExecuteTwoIntegerCall(callerFrame, targetRoutine, instructionIndex, firstInteger, secondInteger);
            return;
        }

        if (instruction.Opcode == Opcode.Call && argumentCount == 3
            && targetRoutine.ParameterCount == 3 && targetRoutine.ParameterVariables.Count == 3
            && targetRoutine.ParameterVariables[0].Type == TypeSymbol.Integer
            && targetRoutine.ParameterVariables[1].Type == TypeSymbol.Integer
            && targetRoutine.ParameterVariables[2].Type == TypeSymbol.Integer
            && callerFrame.EvaluationStack.TryGetImmediateInteger(out var firstOfThree, 2)
            && callerFrame.EvaluationStack.TryGetIntegerOperands(out var secondOfThree, out var thirdOfThree))
        {
            ExecuteThreeIntegerCall(callerFrame, targetRoutine, instructionIndex, firstOfThree, secondOfThree, thirdOfThree);
            return;
        }

        using var arguments = RentArguments(callerFrame, byValArgumentCount, instructionIndex);
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        var byRefArgumentSlots = instruction.Opcode == Opcode.CallByRef
            ? ResolveByRefArgumentSlots(callerFrame, instruction)
            : null;

        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(targetRoutine.Name, ResolveLocation(callerFrame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        if (byValArgumentCount == 0 && byRefArgumentSlots is { Length: > 0 }
            && byRefArgumentSlots.Length == targetRoutine.ParameterCount
            && HasCompleteByRefSlots(byRefArgumentSlots))
        {
            var template = _frameTemplatesByRoutineId[targetRoutine.Id];
            _callStack.Push(CreateFrameWithSlots(targetRoutine, byRefArgumentSlots, captures,
                ResolveLocation(callerFrame.Routine.Id, instructionIndex), template));
            return;
        }

        // Массив ссылочных аргументов принадлежит этому вызову. Для корректной
        // смешанной сигнатуры заполняем его пропуски готовыми снимками BYVAL.
        if (byValArgumentCount > 0 && byRefArgumentSlots is { Length: > 0 }
            && byRefArgumentSlots.Length == targetRoutine.ParameterCount
            && CountEmptyArgumentSlots(byRefArgumentSlots) == arguments.Span.Length)
        {
            var byValIndex = 0;
            for (var index = 0; index < byRefArgumentSlots.Length; index++)
            {
                if (byRefArgumentSlots[index] is null)
                    byRefArgumentSlots[index] = BytecodeVariableSlot.Adopt(arguments.Span[byValIndex++]);
            }
            var template = _frameTemplatesByRoutineId[targetRoutine.Id];
            _callStack.Push(CreateFrameWithSlots(targetRoutine, byRefArgumentSlots, captures,
                ResolveLocation(callerFrame.Routine.Id, instructionIndex), template));
            return;
        }

        _callStack.Push(CreateFrame(
            targetRoutine,
            arguments.Span,
            captures,
            ResolveLocation(callerFrame.Routine.Id, instructionIndex),
            byRefArgumentSlots,
            byValValuesAreSnapshots: true));
    }

    private static int CountEmptyArgumentSlots(BytecodeVariableSlot[] slots)
    {
        var count = 0;
        foreach (var slot in slots)
            if (slot is null) count++;
        return count;
    }

    private static BytecodeVariableSlot MarkSlotEscaped(BytecodeVariableSlot slot)
    {
        slot.MarkEscaped();
        return slot;
    }

    private static BytecodeVariableSlot ReuseIntegerSlot(BytecodeVariableSlot? slot, int value)
        => slot is not null && slot.TryResetInteger(value) ? slot : BytecodeVariableSlot.AdoptInteger(value);

    private static BytecodeVariableSlot ReuseValueSlot(BytecodeVariableSlot? slot, RuntimeValue value)
        => slot is not null && slot.TryResetValue(value) ? slot : BytecodeVariableSlot.AdoptReusable(value);

    private static bool HasCompleteByRefSlots(BytecodeVariableSlot[] slots)
    {
        for (var index = 0; index < slots.Length; index++)
        {
            if (slots[index] is null)
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteTwoIntegerCall(BytecodeCallFrame callerFrame, BytecodeRoutine targetRoutine,
        int instructionIndex, int first, int second)
    {
        callerFrame.EvaluationStack.DiscardTop(2);
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        var location = ResolveLocation(callerFrame.Routine.Id, instructionIndex);
        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(targetRoutine.Name, location);
            throw new RuntimeFailureException();
        }

        var template = _frameTemplatesByRoutineId[targetRoutine.Id];
        if (_cachedFrames.Remove(targetRoutine.Id, out var cachedFrame))
        {
            cachedFrame.Arguments[0] = ReuseIntegerSlot(cachedFrame.Arguments[0], first);
            cachedFrame.Arguments[1] = ReuseIntegerSlot(cachedFrame.Arguments[1], second);
            cachedFrame.Reset(cachedFrame.Arguments, captures, location, template.DefaultLocals);
            _callStack.Push(cachedFrame);
        }
        else
        {
            _callStack.Push(CreateFrameWithSlots(targetRoutine, [BytecodeVariableSlot.AdoptInteger(first), BytecodeVariableSlot.AdoptInteger(second)], captures, location, template));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteThreeIntegerCall(BytecodeCallFrame callerFrame, BytecodeRoutine targetRoutine,
        int instructionIndex, int first, int second, int third)
    {
        callerFrame.EvaluationStack.DiscardTop(3);
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        var location = ResolveLocation(callerFrame.Routine.Id, instructionIndex);
        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(targetRoutine.Name, location);
            throw new RuntimeFailureException();
        }

        var template = _frameTemplatesByRoutineId[targetRoutine.Id];
        if (_cachedFrames.Remove(targetRoutine.Id, out var cachedFrame))
        {
            cachedFrame.Arguments[0] = ReuseIntegerSlot(cachedFrame.Arguments[0], first);
            cachedFrame.Arguments[1] = ReuseIntegerSlot(cachedFrame.Arguments[1], second);
            cachedFrame.Arguments[2] = ReuseIntegerSlot(cachedFrame.Arguments[2], third);
            cachedFrame.Reset(cachedFrame.Arguments, captures, location, template.DefaultLocals);
            _callStack.Push(cachedFrame);
        }
        else
        {
            _callStack.Push(CreateFrameWithSlots(targetRoutine, [BytecodeVariableSlot.AdoptInteger(first), BytecodeVariableSlot.AdoptInteger(second), BytecodeVariableSlot.AdoptInteger(third)], captures, location, template));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteSingleValueCall(BytecodeCallFrame callerFrame, BytecodeRoutine targetRoutine, int instructionIndex)
    {
        // Значение уже зафиксировано компилятором в момент вычисления аргумента.
        // Повторная ячейка допустима только пока она не ушла в замыкание или BYREF.
        var value = callerFrame.EvaluationStack.Pop();
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        var location = ResolveLocation(callerFrame.Routine.Id, instructionIndex);
        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(targetRoutine.Name, location);
            throw new RuntimeFailureException();
        }

        var template = _frameTemplatesByRoutineId[targetRoutine.Id];
        if (_cachedFrames.Remove(targetRoutine.Id, out var cachedFrame))
        {
            cachedFrame.Arguments[0] = ReuseValueSlot(cachedFrame.Arguments[0], value);
            cachedFrame.Reset(cachedFrame.Arguments, captures, location, template.DefaultLocals);
            _callStack.Push(cachedFrame);
        }
        else
        {
            _callStack.Push(CreateFrameWithSlots(targetRoutine, [BytecodeVariableSlot.AdoptReusable(value)], captures, location, template));
        }
    }

    private void ExecuteFunctionValueCall(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var argumentCount = instruction.GetOperand(0);
        var returnsValue = instruction.GetOperand(1) != 0;
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        using var arguments = RentArguments(frame, argumentCount, instructionIndex);
        var target = PopValue(frame, instructionIndex);
        var functionValue = target.AsFunction();

        if (functionValue is null)
        {
            ReportEmptyFunctionValue(location);
            throw new RuntimeFailureException();
        }

        if (functionValue.Function.IsBuiltIn)
        {
            RuntimeValue builtinResult;
            try
            {
                builtinResult = InvokeBuiltinFunctionValue(functionValue, arguments.Span, location, true, out var callbackStep);
                if (callbackStep is VmCallbackComplete complete)
                {
                    if (returnsValue) frame.EvaluationStack.Push(complete.Value);
                    return;
                }
                if (callbackStep is not null)
                {
                    ResumeCallbackChain(callbackStep, returnsValue ? CreateCallbackCompletion(frame) : static _ => { });
                    return;
                }
            }
            catch (VmCallbackSuspension suspension)
            {
                SetBuiltinCompletion(suspension, frame, returnsValue);
                throw;
            }

            if (returnsValue)
            {
                frame.EvaluationStack.Push(builtinResult);
            }

            return;
        }

        if (!TryResolveFunctionRoutine(functionValue, location, out var routine))
        {
            throw new RuntimeFailureException();
        }

        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(functionValue.Function.Name, location);
            throw new RuntimeFailureException();
        }

        var functionCaptures = GetFunctionCaptures(functionValue);
        if (argumentCount == 1 && routine.ParameterCount == 1
            && _cachedFrames.Remove(routine.Id, out var cachedFrame))
        {
            // Повторная ячейка допустима, пока прежняя не ушла в замыкание.
            cachedFrame.Arguments[0] = ReuseValueSlot(cachedFrame.Arguments[0], arguments.Span[0]);
            var template = _frameTemplatesByRoutineId[routine.Id];
            cachedFrame.Reset(cachedFrame.Arguments, functionCaptures, location, template.DefaultLocals);
            cachedFrame.DiscardReturnValue = !returnsValue;
            _callStack.Push(cachedFrame);
            return;
        }

        var calleeFrame = CreateFrame(
            routine,
            arguments.Span,
            functionCaptures,
            location,
            byValValuesAreSnapshots: true);
        calleeFrame.DiscardReturnValue = !returnsValue;
        _callStack.Push(calleeFrame);
    }

    private BytecodeVariableSlot[] ResolveCapturedSlots(BytecodeCallFrame callerFrame, BytecodeRoutine targetRoutine, int instructionIndex)
    {
        if (targetRoutine.CapturedVariables.Count == 0)
        {
            return Array.Empty<BytecodeVariableSlot>();
        }

        if (callerFrame.TryGetCachedCapturedSlots(targetRoutine.Id, out var cachedCaptures))
        {
            return cachedCaptures;
        }

        var bindings = ResolveCaptureBindings(callerFrame.Routine, targetRoutine, instructionIndex);
        var captures = new BytecodeVariableSlot[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            captures[index] = bindings[index].Kind switch
            {
                CaptureSourceKind.Argument => MarkSlotEscaped(callerFrame.Arguments[bindings[index].Slot]),
                CaptureSourceKind.Local => callerFrame.GetOrCreateLocalSlot(bindings[index].Slot),
                CaptureSourceKind.Capture => MarkSlotEscaped(callerFrame.Captures[bindings[index].Slot]),
                _ => throw new InvalidOperationException($"Unsupported capture source kind '{bindings[index].Kind}'.")
            };
        }

        callerFrame.CacheCapturedSlots(targetRoutine.Id, captures);
        return captures;
    }

    private CaptureBinding[] ResolveCaptureBindings(BytecodeRoutine callerRoutine, BytecodeRoutine targetRoutine, int instructionIndex)
    {
        var cacheKey = (callerRoutine.Id, targetRoutine.Id);
        if (_captureBindingCache.TryGetValue(cacheKey, out var cachedBindings))
        {
            return cachedBindings;
        }

        var bindings = new CaptureBinding[targetRoutine.CapturedVariables.Count];

        for (var index = 0; index < targetRoutine.CapturedVariables.Count; index++)
        {
            var variable = targetRoutine.CapturedVariables[index];
            if (TryResolveCaptureBinding(callerRoutine, variable, out var binding))
            {
                bindings[index] = binding;
                continue;
            }

            ReportInvalidRuntimeState(
                $"Процедура '{callerRoutine.Name}' не смогла передать захваченную переменную '{variable.Name}' в '{targetRoutine.Name}'.",
                ResolveLocation(callerRoutine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        _captureBindingCache[cacheKey] = bindings;
        return bindings;
    }

    private static bool TryResolveCaptureBinding(BytecodeRoutine callerRoutine, VariableSymbol variable, out CaptureBinding binding)
    {
        if (TryFindVariableSlot(callerRoutine.ParameterVariables, variable, out var slot))
        {
            binding = new CaptureBinding(CaptureSourceKind.Argument, slot);
            return true;
        }

        if (TryFindVariableSlot(callerRoutine.LocalVariables, variable, out slot))
        {
            binding = new CaptureBinding(CaptureSourceKind.Local, slot);
            return true;
        }

        if (TryFindVariableSlot(callerRoutine.CapturedVariables, variable, out slot))
        {
            binding = new CaptureBinding(CaptureSourceKind.Capture, slot);
            return true;
        }

        binding = default;
        return false;
    }

    private static bool TryFindVariableSlot(IReadOnlyList<VariableSymbol> variables, VariableSymbol target, out int slot)
    {
        for (var index = 0; index < variables.Count; index++)
        {
            if (ReferenceEquals(variables[index], target))
            {
                slot = index;
                return true;
            }
        }

        slot = -1;
        return false;
    }

}
