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
    internal BytecodeVmForEachItem? GetCurrentForEachItem(int activeRoutineId)
    {
        var activeFrameFound = false;
        foreach (var frame in _callStack)
        {
            if (!activeFrameFound)
            {
                if (frame.Routine.Id != activeRoutineId)
                {
                    continue;
                }

                activeFrameFound = true;
            }

            BytecodeVmForEachItem? current = null;
            var localVariables = frame.Routine.LocalVariables;
            for (var localIndex = 0; localIndex < localVariables.Count && localIndex < frame.LocalCount; localIndex++)
            {
                var slot = frame.GetExistingLocalSlot(localIndex);
                if (slot is not { IsForEachItem: true }
                    || !IsForEachItemInstructionScopeActive(frame, localIndex))
                {
                    continue;
                }

                string value;
                try
                {
                    value = slot.Value.ToDisplayString();
                }
                catch (Exception exception) when (exception is ExpiredArrayViewException or InvalidOperationException)
                {
                    value = "‹недоступно›";
                }

                var variable = localVariables[localIndex];
                current = new BytecodeVmForEachItem(variable.Name, variable.Type.Name, value);
            }

            if (current is not null)
            {
                return current;
            }
        }

        return null;
    }

    private static bool IsForEachItemInstructionScopeActive(BytecodeCallFrame frame, int localSlot)
    {
        var instructions = frame.Routine.InstructionArray;
        var instructionPointer = frame.InstructionPointer;

        for (var start = 0; start < instructions.Length; start++)
        {
            var instruction = instructions[start];
            if (instruction.Opcode is not (Opcode.FreshLocal or Opcode.BindArrayElement)
                || instruction.GetOperand(0) != localSlot)
            {
                continue;
            }

            for (var end = start + 1; end < instructions.Length; end++)
            {
                var clear = instructions[end];
                if (clear.Opcode != Opcode.ClearForEachItem || clear.GetOperand(0) != localSlot)
                {
                    continue;
                }

                if (instructionPointer >= start && instructionPointer <= end)
                {
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private BytecodeVmExecutionSnapshot CreateExecutionSnapshot(
        BytecodeCallFrame frame,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        return new BytecodeVmExecutionSnapshot(
            frame.Routine.Id,
            frame.Routine.Name,
            instructionIndex,
            instruction.Opcode,
            ResolveLocation(frame.Routine.Id, instructionIndex),
            _callStack.Count);
    }

    private bool ShouldStopBefore(BytecodeVmExecutionSnapshot snapshot)
    {
        if (ShouldSkipStopOnceAt(snapshot))
        {
            return false;
        }

        if (ShouldStopForBreakpoint(snapshot))
        {
            _debugStepBoundary = null;
            RefreshFastInstructionPathFlag();
            return true;
        }

        if (ShouldStopForDebugStep(snapshot))
        {
            _debugStepBoundary = null;
            RefreshFastInstructionPathFlag();
            return true;
        }

        return _shouldStopBeforeInstruction?.Invoke(snapshot) == true;
    }

    private bool ShouldSkipStopOnceAt(BytecodeVmExecutionSnapshot snapshot)
    {
        if (_skipStopOnceAt is null)
        {
            return false;
        }

        var skip =
            _skipStopOnceAt.RoutineId == snapshot.RoutineId
            && _skipStopOnceAt.InstructionIndex == snapshot.InstructionIndex;

        _skipStopOnceAt = null;
        RefreshFastInstructionPathFlag();
        return skip;
    }

    private bool ShouldStopForBreakpoint(BytecodeVmExecutionSnapshot snapshot)
    {
        if (_breakpoints is null)
        {
            return false;
        }

        if (_suppressLineBreakpointsUntilDifferentLine == SourceLine.From(snapshot.SourceLocation))
        {
            return _breakpoints.ShouldStopBeforePositionBreakpoint(snapshot);
        }

        _suppressLineBreakpointsUntilDifferentLine = null;
        RefreshFastInstructionPathFlag();
        return _breakpoints.ShouldStopBefore(snapshot);
    }

    private bool ShouldStopForDebugStep(BytecodeVmExecutionSnapshot snapshot)
    {
        if (_debugStepBoundary is not { } boundary)
        {
            return false;
        }

        return boundary.Mode switch
        {
            DebugStepMode.Into => SourceLine.From(snapshot.SourceLocation) != boundary.StartLine,
            DebugStepMode.Over => snapshot.CallDepth <= boundary.StartCallDepth
                && SourceLine.From(snapshot.SourceLocation) != boundary.StartLine,
            DebugStepMode.Out => snapshot.CallDepth < boundary.StartCallDepth,
            _ => false
        };
    }


    private BytecodeVmStateSnapshot? TryCreateReversibleStateSnapshot(BytecodeVmExecutionSnapshot executionSnapshot)
    {
        if (_openFiles.Count > 0 || HasActiveCallbackContinuation())
        {
            return null;
        }

        var sharedValues = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

        if (!TryCreateGlobalSnapshots(sharedValues, out var globals))
        {
            return null;
        }

        var frames = new List<BytecodeCallFrameSnapshot>();
        foreach (var frame in _callStack.Reverse())
        {
            var frameSnapshot = TryCreateCallFrameSnapshot(frame, sharedValues);
            if (frameSnapshot is null)
            {
                return null;
            }

            frames.Add(frameSnapshot);
        }

        return new BytecodeVmStateSnapshot(
            executionSnapshot,
            globals,
            frames,
            _diagnostics.ToArray(),
            _lastRuntimeFailureDiagnostic,
            _lastRuntimeErrorMessage,
            _pendingOutput.ToString(),
            _hasPendingOutput,
            GetOutputCheckpoint(),
            _previousReversibleStateSnapshot);
    }

    private bool ShouldCaptureReversibleStateHistory()
    {
        return _breakpoints is not null
            || _shouldStopBeforeInstruction is not null
            || _debugStepBoundary is not null;
    }

    private bool NeedsExecutionSnapshot()
    {
        return _instructionObserver is not null
            || _breakpoints is not null
            || _shouldStopBeforeInstruction is not null
            || _debugStepBoundary is not null
            || _skipStopOnceAt is not null
            || _suppressLineBreakpointsUntilDifferentLine is not null;
    }

    private void RestoreStateSnapshot(BytecodeVmStateSnapshot snapshot)
    {
        var sharedValues = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);

        Array.Clear(_globalsBySlot);
        foreach (var (slot, value) in snapshot.Globals)
        {
            var clonedValue = CloneSnapshotValue(value, GetGlobalFixedStringLength(slot), sharedValues);
            if ((uint)slot < (uint)_globalsBySlot.Length)
            {
                _globalsBySlot[slot] = clonedValue;
            }
        }

        _callStack.Clear();

        foreach (var frameSnapshot in snapshot.CallFramesBottomToTop)
        {
            var arguments = CreateSlotsFromSnapshots(frameSnapshot.Arguments, sharedValues);
            var (locals, localFixedStringLengths, localSlots) = CreateLocalStateFromSnapshots(frameSnapshot.Locals, sharedValues);
            var frame = new BytecodeCallFrame(
                frameSnapshot.Routine,
                arguments,
                locals,
                localFixedStringLengths,
                [],
                frameSnapshot.CallSiteLocation,
                frameSnapshot.Routine.MaxEvaluationStackDepth)
            {
                InstructionPointer = frameSnapshot.InstructionPointer
            };

            for (var index = 0; index < localSlots.Length; index++)
            {
                frame.ReplaceLocalSlot(index, localSlots[index]);
            }

            var captures = RestoreCapturedSlots(frameSnapshot, sharedValues);
            frame.SetCapturedSlots(captures);

            foreach (var savedHandler in frameSnapshot.ErrorHandlers)
            {
                var handler = savedHandler.Clone();
                if (handler.PendingReturnValue is { } returnValue)
                {
                    handler.PendingReturnValue = CloneSnapshotValue(returnValue, fixedStringLength: null, sharedValues);
                }

                frame.PushErrorHandler(handler);
            }

            foreach (var value in frameSnapshot.EvaluationStackBottomToTop)
            {
                frame.EvaluationStack.Push(CloneSnapshotValue(value, fixedStringLength: null, sharedValues));
            }

            _callStack.Push(frame);
        }

        _pendingOutput.Clear();
        foreach (var diagnostic in _diagnostics.ToArray())
        {
            _diagnostics.Remove(diagnostic);
        }
        _diagnostics.AddRange(snapshot.Diagnostics);
        _lastRuntimeFailureDiagnostic = snapshot.LastRuntimeFailureDiagnostic;
        _lastRuntimeErrorMessage = snapshot.LastRuntimeErrorMessage;
        _pendingOutput.Append(snapshot.PendingOutput);
        _hasPendingOutput = snapshot.HasPendingOutput;
        RestoreOutputCheckpoint(snapshot.OutputCheckpoint);
        _previousReversibleStateSnapshot = snapshot.PreviousStateSnapshot;
        _skipStopOnceAt = null;
        _suppressLineBreakpointsUntilDifferentLine = null;
        _debugStepBoundary = null;
        RefreshFastInstructionPathFlag();
    }

    private static BytecodeVariableSlot[] CreateSlotsFromSnapshots(
        IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots,
        Dictionary<object, object> sharedValues)
    {
        var slots = new BytecodeVariableSlot[snapshots.Count];

        for (var index = 0; index < snapshots.Count; index++)
        {
            slots[index] = CloneSnapshotSlot(snapshots[index].Slot, sharedValues);
        }

        return slots;
    }

    private static BytecodeVariableSlot[] RestoreCapturedSlots(
        BytecodeCallFrameSnapshot frameSnapshot,
        Dictionary<object, object> sharedValues)
    {
        var captures = new BytecodeVariableSlot[frameSnapshot.Captures.Count];

        for (var index = 0; index < captures.Length; index++)
        {
            // A symbol identifies a declaration, not a particular invocation's
            // storage cell. Restore the captured slot identity carried by the
            // snapshot instead of rebinding by VariableSymbol.
            captures[index] = CloneSnapshotSlot(frameSnapshot.Captures[index].Slot, sharedValues);
        }

        return captures;
    }

    private static (RuntimeValue[] Values, int?[] FixedStringLengths, BytecodeVariableSlot[] Slots) CreateLocalStateFromSnapshots(
        IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots,
        Dictionary<object, object> sharedValues)
    {
        var values = new RuntimeValue[snapshots.Count];
        var fixedStringLengths = new int?[snapshots.Count];
        var slots = new BytecodeVariableSlot[snapshots.Count];

        for (var index = 0; index < snapshots.Count; index++)
        {
            slots[index] = CloneSnapshotSlot(snapshots[index].Slot, sharedValues);
            values[index] = RuntimeValue.Void;
            fixedStringLengths[index] = snapshots[index].Slot.FixedStringLength;
        }

        return (values, fixedStringLengths, slots);
    }

    private static BytecodeVariableSlot CloneSnapshotSlot(
        BytecodeVariableSlot snapshotSlot,
        Dictionary<object, object> sharedValues)
    {
        if (!RuntimeSnapshotFactory.TryCreateSlotSnapshot(snapshotSlot, sharedValues, out var restoredSlot))
        {
            throw new InvalidOperationException("Снимок содержит слот, который нельзя восстановить.");
        }

        return restoredSlot;
    }

    private static RuntimeValue CloneSnapshotValue(
        RuntimeValue value,
        int? fixedStringLength,
        Dictionary<object, object> sharedValues)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(sharedValues);

        if (!RuntimeSnapshotFactory.TryCreateValueSnapshot(value, fixedStringLength, sharedValues, out var snapshot))
        {
            throw new InvalidOperationException("Снимок содержит значение, которое нельзя восстановить.");
        }

        return snapshot;
    }

    private bool TryCreateGlobalSnapshots(
        Dictionary<object, object> sharedValues,
        out IReadOnlyDictionary<int, RuntimeValue> globals)
    {
        var snapshots = new Dictionary<int, RuntimeValue>();

        foreach (var global in _module.Globals)
        {
            var slot = global.Slot;
            var value = _globalsBySlot[slot]!;
            if (!TryCreateValueSnapshot(value, GetGlobalFixedStringLength(slot), sharedValues, out var valueSnapshot))
            {
                globals = snapshots;
                return false;
            }

            snapshots[slot] = valueSnapshot;
        }

        globals = snapshots;
        return true;
    }

    private static BytecodeCallFrameSnapshot? TryCreateCallFrameSnapshot(
        BytecodeCallFrame frame,
        Dictionary<object, object> sharedValues)
    {
        if (!TryCreateSlotSnapshots(frame.Arguments, sharedValues, out var argumentSnapshots)
            || !TryCreateLocalSnapshots(frame, sharedValues, out var localSnapshots)
            || !TryCreateSlotSnapshots(frame.Captures, sharedValues, out var captureSnapshots)
            || !TryCreateEvaluationStackSnapshot(frame.EvaluationStack, sharedValues, out var evaluationStackSnapshot))
        {
            return null;
        }

        var handlers = new List<BytecodeErrorHandlerFrame>(frame.ErrorHandlers.Count);
        foreach (var sourceHandler in frame.ErrorHandlers)
        {
            var handler = sourceHandler.Clone();
            if (handler.PendingReturnValue is { } returnValue)
            {
                if (!TryCreateValueSnapshot(returnValue, fixedStringLength: null, sharedValues, out var returnSnapshot))
                {
                    return null;
                }

                handler.PendingReturnValue = returnSnapshot;
            }

            handlers.Add(handler);
        }

        return new BytecodeCallFrameSnapshot(
            frame.Routine,
            argumentSnapshots,
            localSnapshots,
            captureSnapshots,
            frame.CallSiteLocation,
            evaluationStackSnapshot,
            handlers,
            frame.InstructionPointer);
    }

    private static bool TryCreateSlotSnapshots(
        IReadOnlyList<BytecodeVariableSlot> slots,
        Dictionary<object, object> sharedValues,
        out IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots)
    {
        var result = new BytecodeVariableSlotSnapshot[slots.Count];

        for (var index = 0; index < slots.Count; index++)
        {
            if (!TryCreateSlotForSnapshot(slots[index], sharedValues, out var snapshotSlot))
            {
                snapshots = result;
                return false;
            }

            result[index] = new BytecodeVariableSlotSnapshot(snapshotSlot);
        }

        snapshots = result;
        return true;
    }

    private static bool TryCreateLocalSnapshots(
        BytecodeCallFrame frame,
        Dictionary<object, object> sharedValues,
        out IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots)
    {
        var result = new BytecodeVariableSlotSnapshot[frame.LocalCount];

        for (var index = 0; index < frame.LocalCount; index++)
        {
            var existingSlot = frame.GetExistingLocalSlot(index);
            BytecodeVariableSlot snapshotSlot;
            if (existingSlot is not null)
            {
                if (!TryCreateSlotForSnapshot(existingSlot, sharedValues, out snapshotSlot))
                {
                    snapshots = result;
                    return false;
                }
            }
            else
            {
                var value = frame.PeekLocalValue(index);
                var fixedStringLength = frame.GetLocalFixedStringLength(index);
                if (!TryCreateValueSnapshot(value, fixedStringLength, sharedValues, out var valueSnapshot))
                {
                    snapshots = result;
                    return false;
                }

                snapshotSlot = BytecodeVariableSlot.Adopt(valueSnapshot, fixedStringLength);
            }

            result[index] = new BytecodeVariableSlotSnapshot(snapshotSlot);
        }

        snapshots = result;
        return true;
    }

    private static bool TryCreateSlotForSnapshot(
        BytecodeVariableSlot sourceSlot,
        Dictionary<object, object> sharedValues,
        out BytecodeVariableSlot snapshotSlot) =>
        RuntimeSnapshotFactory.TryCreateSlotSnapshot(sourceSlot, sharedValues, out snapshotSlot);

    private static bool TryCreateEvaluationStackSnapshot(
        BytecodeEvaluationStack evaluationStack,
        Dictionary<object, object> sharedValues,
        out IReadOnlyList<RuntimeValue> snapshots)
    {
        var valuesBottomToTop = evaluationStack.ToBottomToTopArray();
        var result = new RuntimeValue[valuesBottomToTop.Length];

        for (var index = 0; index < valuesBottomToTop.Length; index++)
        {
            if (!TryCreateValueSnapshot(valuesBottomToTop[index], fixedStringLength: null, sharedValues, out var valueSnapshot))
            {
                snapshots = result;
                return false;
            }

            result[index] = valueSnapshot;
        }

        snapshots = result;
        return true;
    }

    private static bool TryCreateValueSnapshot(
        RuntimeValue value,
        int? fixedStringLength,
        Dictionary<object, object> sharedValues,
        out RuntimeValue snapshot) =>
        RuntimeSnapshotFactory.TryCreateValueSnapshot(value, fixedStringLength, sharedValues, out snapshot);

    private void InvalidateReverseExecutionHistory()
    {
        _previousReversibleStateSnapshot = null;
    }

    private void InvalidateReverseExecutionHistoryIfOutputIsNotReversible()
    {
        if (_host is not IReversibleOutputWriter)
        {
            InvalidateReverseExecutionHistory();
        }
    }

    private int? GetOutputCheckpoint()
    {
        return _host is IReversibleOutputWriter reversibleOutputWriter
            ? reversibleOutputWriter.OutputCheckpoint
            : null;
    }

    private void RestoreOutputCheckpoint(int? checkpoint)
    {
        if (checkpoint is null)
        {
            return;
        }

        if (_host is IReversibleOutputWriter reversibleOutputWriter)
        {
            reversibleOutputWriter.RestoreOutputCheckpoint(checkpoint.Value);
        }
    }

}
