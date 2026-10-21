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
        var visibleVariables = new Dictionary<VariableSymbol, BytecodeVariableSlot>();

        foreach (var frameSnapshot in snapshot.CallFramesBottomToTop)
        {
            var arguments = CreateSlotsFromSnapshots(frameSnapshot.Arguments, sharedValues);
            var (locals, localFixedStringLengths) = CreateLocalStateFromSnapshots(frameSnapshot.Locals, sharedValues);
            var captures = RestoreCapturedSlots(frameSnapshot, visibleVariables, sharedValues);
            var frame = new BytecodeCallFrame(
                frameSnapshot.Routine,
                arguments,
                locals,
                localFixedStringLengths,
                captures,
                frameSnapshot.CallSiteLocation,
                frameSnapshot.Routine.MaxEvaluationStackDepth)
            {
                InstructionPointer = frameSnapshot.InstructionPointer
            };

            foreach (var value in frameSnapshot.EvaluationStackBottomToTop)
            {
                frame.EvaluationStack.Push(CloneSnapshotValue(value, fixedStringLength: null, sharedValues));
            }

            _callStack.Push(frame);
            AddVisibleVariables(frame.Routine.ParameterVariables, arguments, visibleVariables);
            AddVisibleLocalVariables(frame, visibleVariables);
            AddVisibleVariables(frame.Routine.CapturedVariables, captures, visibleVariables);
        }

        _pendingOutput.Clear();
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
            var snapshot = snapshots[index];
            slots[index] = BytecodeVariableSlot.Adopt(
                CloneSnapshotValue(snapshot.Value, snapshot.FixedStringLength, sharedValues),
                snapshot.FixedStringLength);
        }

        return slots;
    }

    private static BytecodeVariableSlot[] RestoreCapturedSlots(
        BytecodeCallFrameSnapshot frameSnapshot,
        IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot> visibleVariables,
        Dictionary<object, object> sharedValues)
    {
        var captures = new BytecodeVariableSlot[frameSnapshot.Captures.Count];

        for (var index = 0; index < captures.Length; index++)
        {
            var variable = index < frameSnapshot.Routine.CapturedVariables.Count
                ? frameSnapshot.Routine.CapturedVariables[index]
                : null;
            if (variable is not null && visibleVariables.TryGetValue(variable, out var visibleSlot))
            {
                captures[index] = visibleSlot;
                continue;
            }

            var snapshot = frameSnapshot.Captures[index];
            captures[index] = BytecodeVariableSlot.Adopt(
                CloneSnapshotValue(snapshot.Value, snapshot.FixedStringLength, sharedValues),
                snapshot.FixedStringLength);
        }

        return captures;
    }

    private static (RuntimeValue[] Values, int?[] FixedStringLengths) CreateLocalStateFromSnapshots(
        IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots,
        Dictionary<object, object> sharedValues)
    {
        var values = new RuntimeValue[snapshots.Count];
        var fixedStringLengths = new int?[snapshots.Count];

        for (var index = 0; index < snapshots.Count; index++)
        {
            var snapshot = snapshots[index];
            values[index] = CloneSnapshotValue(snapshot.Value, snapshot.FixedStringLength, sharedValues);
            fixedStringLengths[index] = snapshot.FixedStringLength;
        }

        return (values, fixedStringLengths);
    }

    private static RuntimeValue CloneSnapshotValue(
        RuntimeValue value,
        int? fixedStringLength,
        Dictionary<object, object> sharedValues)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(sharedValues);

        return RuntimeSnapshotFactory.CreateValueSnapshot(value, fixedStringLength, sharedValues);
    }

    private static void AddVisibleVariables(
        IReadOnlyList<VariableSymbol> variables,
        IReadOnlyList<BytecodeVariableSlot> slots,
        Dictionary<VariableSymbol, BytecodeVariableSlot> visibleVariables)
    {
        for (var index = 0; index < variables.Count && index < slots.Count; index++)
        {
            visibleVariables[variables[index]] = slots[index];
        }
    }

    private static void AddVisibleLocalVariables(
        BytecodeCallFrame frame,
        Dictionary<VariableSymbol, BytecodeVariableSlot> visibleVariables)
    {
        var variables = frame.Routine.LocalVariables;
        for (var index = 0; index < variables.Count && index < frame.LocalCount; index++)
        {
            visibleVariables[variables[index]] = frame.GetOrCreateLocalSlot(index);
        }
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

        return new BytecodeCallFrameSnapshot(
            frame.Routine,
            argumentSnapshots,
            localSnapshots,
            captureSnapshots,
            frame.CallSiteLocation,
            evaluationStackSnapshot,
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
            var slot = slots[index];
            if (!TryCreateValueSnapshot(slot.Value, slot.FixedStringLength, sharedValues, out var valueSnapshot))
            {
                snapshots = result;
                return false;
            }

            result[index] = new BytecodeVariableSlotSnapshot(valueSnapshot, slot.FixedStringLength);
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
            var value = frame.PeekLocalValue(index);
            var fixedStringLength = frame.GetLocalFixedStringLength(index);
            if (!TryCreateValueSnapshot(value, fixedStringLength, sharedValues, out var valueSnapshot))
            {
                snapshots = result;
                return false;
            }

            result[index] = new BytecodeVariableSlotSnapshot(valueSnapshot, fixedStringLength);
        }

        snapshots = result;
        return true;
    }

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
        out RuntimeValue snapshot)
    {
        if (value.Type == TypeSymbol.Integer
            || value.Type == TypeSymbol.Long
            || value.Type == TypeSymbol.Double
            || value.Type == TypeSymbol.Decimal
            || value.Type == TypeSymbol.String
            || value.Type == TypeSymbol.Boolean
            || value.Type == TypeSymbol.Date
            || value.Type == TypeSymbol.Time
            || value.Type == TypeSymbol.DateTime
            || value.Type == TypeSymbol.UtcDateTime
            || value.Type == TypeSymbol.Void)
        {
            snapshot = value.CopyForStorage(fixedStringLength);
            return true;
        }

        if (value.Type is ArrayTypeSymbol arrayType)
        {
            var array = value.AsArray();
            if (!sharedValues.TryGetValue(array, out var clonedArray))
            {
                // Снимок должен быть независимым от исполняемого графа состояний:
                // Clone выполняет правила присваивания, а не копирование состояния.
                var snapshotArray = array.CreateEmptyForSnapshot();
                sharedValues[array] = snapshotArray;
                array.FillSnapshotInto(
                    snapshotArray,
                    element => RuntimeSnapshotFactory.CreateValueSnapshot(element, array.FixedStringLength, sharedValues));
                clonedArray = snapshotArray;
            }

            snapshot = RuntimeValue.FromArray(arrayType, (RuntimeArrayValue)clonedArray);
            return true;
        }

        if (value.Type is UserDefinedTypeSymbol recordType)
        {
            snapshot = RuntimeSnapshotFactory.CreateValueSnapshot(value, fixedStringLength, sharedValues);
            return true;
        }

        if (value.Type is FunctionTypeSymbol functionType)
        {
            var functionValue = value.AsFunction();
            if (functionValue is null)
            {
                snapshot = RuntimeValue.FromFunction(functionType, value: null);
                return true;
            }

            if (functionValue.Function.IsBuiltIn)
            {
                snapshot = functionValue.BytecodeBuiltinBindingId is { } builtinBindingId
                    ? RuntimeValue.FromBytecodeBuiltinFunction(functionValue.Function, builtinBindingId)
                    : RuntimeValue.FromBytecodeFunction(functionValue.Function, []);
                return true;
            }

            if (functionValue.BytecodeCapturedSlots is { Count: 0 })
            {
                snapshot = RuntimeValue.FromBytecodeFunction(functionValue.Function, []);
                return true;
            }
        }

        snapshot = RuntimeValue.Void;
        return false;
    }

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
