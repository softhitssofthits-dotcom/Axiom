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
    private void ExecuteRoutineCall(BytecodeCallFrame callerFrame, BytecodeInstruction instruction, int instructionIndex)
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

        _callStack.Push(CreateFrame(
            targetRoutine,
            arguments.Span,
            captures,
            ResolveLocation(callerFrame.Routine.Id, instructionIndex),
            byRefArgumentSlots,
            byValValuesAreSnapshots: true));
    }

    private void ExecuteFunctionValueCall(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
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
                builtinResult = InvokeBuiltinFunctionValue(functionValue, arguments.Span, location);
            }
            catch (VmCallbackSuspension suspension)
            {
                suspension.Completion = value =>
                {
                    if (returnsValue)
                    {
                        frame.EvaluationStack.PushUnchecked(value);
                    }
                };
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

        var calleeFrame = CreateFrame(
            routine,
            arguments.Span,
            GetFunctionCaptures(functionValue),
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
                CaptureSourceKind.Argument => callerFrame.Arguments[bindings[index].Slot],
                CaptureSourceKind.Local => callerFrame.GetOrCreateLocalSlot(bindings[index].Slot),
                CaptureSourceKind.Capture => callerFrame.Captures[bindings[index].Slot],
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
