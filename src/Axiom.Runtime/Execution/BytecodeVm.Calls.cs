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

        if (!_prepared.TryGetRoutine(targetRoutineId, out var targetRoutine))
        {
            ReportInvalidRuntimeState(
                $"Не найдена вызываемая процедура с routine-id {targetRoutineId}.",
                ResolveLocation(callerFrame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        // Захваты и ячейки BYREF разрешаются до снятия BYVAL-значений: они
        // читают слоты вызывателя, но не его стек вычисления.
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        var byRefArgumentSlots = instruction.Opcode == Opcode.CallByRef
            ? ResolveByRefArgumentSlots(callerFrame, instruction)
            : null;
        var callLocation = ResolveLocation(callerFrame.Routine.Id, instructionIndex);

        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(targetRoutine.Name, callLocation);
            throw new RuntimeFailureException();
        }

        PushCalleeFrame(targetRoutine, callerFrame.EvaluationStack, byValArgumentCount, byRefArgumentSlots,
            captures, callLocation, discardReturnValue: false);
    }

    /// <summary>
    /// Универсальное создание кадра вызова по слитной раскладке слотов
    /// (localsplus). BYVAL-значения копируются со стека вызывателя в первые
    /// слоты локального массива кадра тегированными значениями — один цикл
    /// обслуживает любые сочетания INTEGER, LONG, DOUBLE и любую арность без
    /// комбинаторики специализированных путей, — а ячейки BYREF занимают
    /// общие слоты тех же позиций. Пул кадров устраняет аллокацию контейнера.
    /// </summary>
    private void PushCalleeFrame(
        BytecodeRoutine routine,
        BytecodeEvaluationStack callerStack,
        int byValArgumentCount,
        BytecodeVariableSlot?[]? byRefArgumentSlots,
        BytecodeVariableSlot[] captures,
        TextLocation callLocation,
        bool discardReturnValue)
    {
        var template = _prepared.GetFrameTemplate(routine.Id);
        if (_framePool.TryTake(routine.Id, out var cachedFrame))
        {
            cachedFrame.Reset(captures, callLocation, template.DefaultLocals);
            cachedFrame.InstallParametersFromStack(callerStack, byValArgumentCount, byRefArgumentSlots);
            cachedFrame.DiscardReturnValue = discardReturnValue;
            _callStack.Push(cachedFrame);
            return;
        }

        var locals = routine.LocalSlotCount == 0
            ? Array.Empty<VmValue>()
            : (VmValue[])template.DefaultLocals.Clone();
        var frame = new BytecodeCallFrame(
            routine,
            locals,
            template.LocalFixedStringLengths,
            captures,
            callLocation,
            routine.MaxEvaluationStackDepth,
            _prepared.GetDecodedInstructions(routine.Id, _fuseDecodedInstructions));
        frame.InstallParametersFromStack(callerStack, byValArgumentCount, byRefArgumentSlots);
        frame.DiscardReturnValue = discardReturnValue;
        _callStack.Push(frame);
    }

    private static BytecodeVariableSlot MarkSlotEscaped(BytecodeVariableSlot slot)
    {
        slot.MarkEscaped();
        return slot;
    }

    private void ExecuteFunctionValueCall(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
    {
        var argumentCount = instruction.GetOperand(0);
        var returnsValue = instruction.GetOperand(1) != 0;
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        // Функциональное значение лежит ПОД аргументами (стек [цель, арг...]).
        // Цель проверяется на вершине аргументного блока, затем универсальный
        // путь снимает BYVAL-аргументы, и последней снимается сама цель.
        if (frame.EvaluationStack.Count <= argumentCount)
        {
            ReportInvalidRuntimeState(
                "Стек вычисления не содержит функционального значения и его аргументов.",
                location);
            throw new RuntimeFailureException();
        }

        var target = frame.EvaluationStack.PeekUnchecked(argumentCount);
        var functionValue = target.AsFunction();

        if (functionValue is null)
        {
            ReportEmptyFunctionValue(location);
            throw new RuntimeFailureException();
        }

        if (functionValue.Function.IsBuiltIn)
        {
            using var arguments = RentArguments(frame, argumentCount, instructionIndex);
            _ = PopValue(frame, instructionIndex);
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
        PushCalleeFrame(routine, frame.EvaluationStack, argumentCount, null, functionCaptures, location,
            discardReturnValue: !returnsValue);
        _ = PopValue(frame, instructionIndex);
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
                // Слитая раскладка: параметр вызывателя живёт в общем слоте
                // наравне с локальной переменной.
                CaptureSourceKind.Argument => MarkSlotEscaped(callerFrame.GetOrCreateLocalSlot(bindings[index].Slot)),
                CaptureSourceKind.Local => MarkSlotEscaped(callerFrame.GetOrCreateLocalSlot(bindings[index].Slot)),
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
        if (_captureBindingCache is not null && _captureBindingCache.TryGetValue(cacheKey, out var cachedBindings))
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

        (_captureBindingCache ??= [])[cacheKey] = bindings;
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
