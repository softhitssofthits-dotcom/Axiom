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
    private const int MaxCallDepth = 200;

    private static readonly TextLocation FallbackRuntimeLocation = new(new TextSpan(0, 0), 1, 1);

    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<int, BytecodeRoutine> _routines;
    private readonly Dictionary<FunctionSymbol, BytecodeRoutine> _routinesBySymbol;
    private readonly Dictionary<int, RoutineFrameTemplate> _frameTemplatesByRoutineId;
    private readonly Dictionary<int, BuiltinBinding> _builtins;
    private readonly Dictionary<FunctionSymbol, BuiltinBinding> _builtinsByFunction;
    private readonly Dictionary<BuiltinFunctionId, BuiltinBinding> _builtinsByCanonicalId;
    private readonly Dictionary<int, BuiltinExecution> _builtinExecutions;
    private readonly BuiltinExecutionInfo[] _builtinExecutionInfosById;
    private readonly Dictionary<int, BytecodeGlobal> _globalMetadata;
    private readonly Dictionary<(int RoutineId, int InstructionIndex), TextLocation> _sourceLocationCache = [];
    private readonly Dictionary<(int RoutineId, int InstructionIndex), InputInstructionPlan> _inputInstructionCache = [];
    private readonly Dictionary<(int RoutineId, int InstructionIndex), TypeFieldSymbol> _recordFieldCache = [];
    private readonly Dictionary<string, UserDefinedTypeSymbol> _recordTypeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DictionaryTypeSymbol> _dictionaryTypeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SetTypeSymbol> _setTypeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TileTransformCacheKey, RuntimeImageValue> _tileTransformCache = [];
    private readonly Dictionary<(int CallerRoutineId, int TargetRoutineId), CaptureBinding[]> _captureBindingCache = [];
    private readonly RuntimeValue?[] _constantValues;
    private readonly RuntimeValue?[] _globalsBySlot;
    private readonly int?[] _globalFixedStringLengths;
    private readonly bool[] _hasGlobalSlot;
    private readonly Stack<BytecodeCallFrame> _callStack = new();
    private readonly Dictionary<int, BytecodeCallFrame> _cachedFrames = [];
    private readonly HashSet<RuntimeTextFile> _openFiles = [];
    private readonly StringBuilder _pendingOutput = new();
    private readonly IRuntimeHost _host;
    private readonly double _monotonicOriginSeconds;
    private readonly BytecodeModule _module;
    private readonly Action<BytecodeVmExecutionSnapshot>? _instructionObserver;
    private readonly Func<BytecodeVmExecutionSnapshot, bool>? _shouldStopBeforeInstruction;
    private readonly BytecodeBreakpointSet? _breakpoints;
    private BytecodeVmStopState? _stopState;
    private BytecodeVmExecutionSnapshot? _skipStopOnceAt;
    private BytecodeVmStateSnapshot? _previousReversibleStateSnapshot;
    private Diagnostic? _lastRuntimeFailureDiagnostic;
    private string? _lastRuntimeErrorMessage;
    private SourceLine? _suppressLineBreakpointsUntilDifferentLine;
    private DebugStepBoundary? _debugStepBoundary;
    private bool _canUseFastInstructionPath;
    private bool _hasPendingOutput;
    private bool _cancelled;

    private BytecodeVm(
        BytecodeModule module,
        IRuntimeHost host,
        Action<BytecodeVmExecutionSnapshot>? instructionObserver,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction,
        BytecodeBreakpointSet? breakpoints)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(host);

        _module = module;
        _host = host;

        // Начало монотонного измерения фиксируется при создании исполнителя,
        // поэтому ELAPSEDSECONDS() считает время именно этого запуска.
        _monotonicOriginSeconds = host.GetMonotonicSeconds();
        _instructionObserver = instructionObserver;
        _shouldStopBeforeInstruction = shouldStopBeforeInstruction;
        _breakpoints = breakpoints;
        _routines = module.Routines.ToDictionary(static routine => routine.Id);
        _routinesBySymbol = module.Routines
            .Where(static routine => routine.Symbol is not null)
            .ToDictionary(static routine => routine.Symbol!);
        _frameTemplatesByRoutineId = module.Routines.ToDictionary(
            static routine => routine.Id,
            static routine => CreateRoutineFrameTemplate(routine));
        _builtins = module.Builtins.ToDictionary(static builtin => builtin.Id);
        _builtinsByFunction = module.Builtins
            .Where(static builtin => builtin.Function is not null)
            .ToDictionary(static builtin => builtin.Function!);
        _builtinsByCanonicalId = module.Builtins
            .GroupBy(static builtin => builtin.CanonicalId)
            .ToDictionary(static group => group.Key, static group => group.First());
        _builtinExecutions = module.Builtins.ToDictionary(
            static builtin => builtin.Id,
            static builtin => CreateBuiltinExecution(builtin));
        _builtinExecutionInfosById = CreateBuiltinExecutionInfosById(module.Builtins, _builtinExecutions);
        _globalMetadata = module.Globals.ToDictionary(static global => global.Slot);
        _constantValues = module.ConstantPool.Entries.Select(TryCreateRuntimeConstant).ToArray();
        var maxGlobalSlot = module.Globals.Count == 0 ? -1 : module.Globals.Max(static global => global.Slot);
        _globalsBySlot = maxGlobalSlot >= 0 ? new RuntimeValue[maxGlobalSlot + 1] : Array.Empty<RuntimeValue>();
        _globalFixedStringLengths = maxGlobalSlot >= 0 ? new int?[maxGlobalSlot + 1] : Array.Empty<int?>();
        _hasGlobalSlot = maxGlobalSlot >= 0 ? new bool[maxGlobalSlot + 1] : Array.Empty<bool>();

        foreach (var global in module.Globals)
        {
            // Запись глобальной переменной создаётся её инструкцией объявления:
            // отложенная стратегия не должна превращаться в выделение всех полей-массивов
            // ещё до начала исполнения программы.
            var initialValue = global.Type is UserDefinedTypeSymbol
                ? RuntimeValue.Void
                : RuntimeValue.Default(global.Type, global.FixedStringLength);
            _globalsBySlot[global.Slot] = initialValue;
            _globalFixedStringLengths[global.Slot] = global.FixedStringLength;
            _hasGlobalSlot[global.Slot] = true;
        }

        BuildRecordTypeCache(module);
        RefreshFastInstructionPathFlag();
    }

    public static RuntimeResult Execute(
        BytecodeModule module,
        IRuntimeHost? host = null,
        Action<BytecodeVmExecutionSnapshot>? instructionObserver = null,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction = null,
        BytecodeBreakpointSet? breakpoints = null)
    {
        BytecodeVm vm;
        try
        {
            vm = new BytecodeVm(
            module,
            host ?? NullRuntimeHost.Instance,
            instructionObserver,
            shouldStopBeforeInstruction,
            breakpoints);
        }
        catch (StringResourceLimitException error)
        {
            var diagnostics = new DiagnosticBag();
            diagnostics.Report(DiagnosticCodes.BuiltinArgument, error.Message, FallbackRuntimeLocation);
            return new RuntimeResult(diagnostics);
        }

        try
        {
            vm.ExecuteModule();
        }
        catch (RuntimeFailureException)
        {
        }
        finally
        {
            vm.FinishRun();
        }

        return new RuntimeResult(vm._diagnostics, vm._stopState, vm.GetPendingOutputPreview(), vm._cancelled);
    }

    private BytecodeCallFrame CurrentFrame => _callStack.Peek();

    private void ExecuteModule()
    {
        using var copyCancellation = RuntimeValueCopier.WithCancellation(_host);
        if (!_routines.TryGetValue(_module.EntryPointRoutineId, out var entryPoint))
        {
            ReportInvalidRuntimeState(
                $"Не найдена точка входа VM с routine-id {_module.EntryPointRoutineId}.",
                FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        _callStack.Push(CreateFrame(
            entryPoint,
            Array.Empty<RuntimeValue>(),
            Array.Empty<BytecodeVariableSlot>(),
            callSiteLocation: entryPoint.Location));

        while (_callStack.Count > 0 && _stopState is null)
        {
            if (CheckCancellationRequested())
            {
                break;
            }

            ExecuteCurrentInstruction();
        }
    }

    internal RuntimeResult ResumeFrom(BytecodeVmStopState stopState)
    {
        return ResumeFrom(stopState, DebugStepMode.None);
    }

    internal RuntimeResult StepOverFrom(BytecodeVmStopState stopState)
    {
        return ResumeFrom(stopState, DebugStepMode.Over);
    }

    internal RuntimeResult StepIntoFrom(BytecodeVmStopState stopState)
    {
        return ResumeFrom(stopState, DebugStepMode.Into);
    }

    internal RuntimeResult StepOutFrom(BytecodeVmStopState stopState)
    {
        return ResumeFrom(stopState, DebugStepMode.Out);
    }

    internal RuntimeResult StepBackFrom(BytecodeVmStopState stopState)
    {
        ArgumentNullException.ThrowIfNull(stopState);

        if (!ReferenceEquals(_stopState, stopState))
        {
            throw new InvalidOperationException("Stopped bytecode VM state does not belong to the current suspended run.");
        }

        if (FindStepBackTargetSnapshot(stopState.Snapshot, stopState.PreviousStateSnapshot) is not { } previousStateSnapshot)
        {
            throw new InvalidOperationException("Stopped bytecode VM run has no previous reversible state.");
        }

        RestoreStateSnapshot(previousStateSnapshot);
        _stopState = new BytecodeVmStopState(
            previousStateSnapshot.ExecutionSnapshot,
            this,
            previousStateSnapshot.PreviousStateSnapshot);

        return new RuntimeResult(_diagnostics, _stopState, GetPendingOutputPreview());
    }

    internal bool CanStepBackFrom(BytecodeVmStopState stopState)
    {
        ArgumentNullException.ThrowIfNull(stopState);
        if (HasActiveCallbackContinuation())
        {
            return false;
        }

        return FindStepBackTargetSnapshot(stopState.Snapshot, stopState.PreviousStateSnapshot) is not null;
    }

    private RuntimeResult ResumeFrom(BytecodeVmStopState stopState, DebugStepMode stepMode)
    {
        using var copyCancellation = RuntimeValueCopier.WithCancellation(_host);
        ArgumentNullException.ThrowIfNull(stopState);

        if (!ReferenceEquals(_stopState, stopState))
        {
            throw new InvalidOperationException("Stopped bytecode VM state does not belong to the current suspended run.");
        }

        _skipStopOnceAt = stopState.Snapshot;
        _suppressLineBreakpointsUntilDifferentLine = SourceLine.From(stopState.Snapshot.SourceLocation);
        _debugStepBoundary = stepMode == DebugStepMode.None
            ? null
            : new DebugStepBoundary(stepMode, SourceLine.From(stopState.Snapshot.SourceLocation), stopState.Snapshot.CallDepth);
        _stopState = null;
        RefreshFastInstructionPathFlag();

        try
        {
            while (_callStack.Count > 0 && _stopState is null)
            {
                if (CheckCancellationRequested())
                {
                    break;
                }

                ExecuteCurrentInstruction();
            }
        }
        catch (RuntimeFailureException)
        {
        }
        finally
        {
            FinishRun();
        }

        return new RuntimeResult(_diagnostics, _stopState, GetPendingOutputPreview(), _cancelled);
    }

    private bool CheckCancellationRequested()
    {
        if (!_host.IsCancellationRequested)
        {
            return false;
        }

        CancelRun();
        return true;
    }

    /// <summary>
    /// Прекращает выполнение по запросу отмены. Тем же путём завершается
    /// длительная операция коллекции, прерванная внутри собственного обхода.
    /// </summary>
    private void CancelRun()
    {
        _cancelled = true;
        _stopState = null;
        foreach (var frame in _callStack)
        {
            frame.ClearCallbackState();
        }

        _callStack.Clear();
    }

    private static BytecodeVmStateSnapshot? FindStepBackTargetSnapshot(
        BytecodeVmExecutionSnapshot currentSnapshot,
        BytecodeVmStateSnapshot? candidate)
    {
        // Сначала уходим с текущей исходной строки: цель отката — предыдущая
        // строка, а не та же самая.
        while (candidate is not null
            && candidate.ExecutionSnapshot.SourceLocation.Line == currentSnapshot.SourceLocation.Line)
        {
            candidate = candidate.PreviousStateSnapshot;
        }

        if (candidate is null)
        {
            return null;
        }

        // Затем возвращаемся к первому снимку найденной строки, то есть к
        // состоянию перед её первой инструкцией. Иначе откат останавливался на
        // служебной инструкции в конце строки (например, Pop после операции) и
        // уже содержал её результат: строка выглядела выполненной до отката.
        var targetLine = candidate.ExecutionSnapshot.SourceLocation.Line;
        while (candidate.PreviousStateSnapshot is { } earlier
            && earlier.ExecutionSnapshot.SourceLocation.Line == targetLine)
        {
            candidate = earlier;
        }

        return candidate;
    }

    private static bool IsBuiltinReverseExecutionSafe(BuiltinFunctionId builtinId)
    {
        return builtinId switch
        {
            BuiltinFunctionId.InKey => false,
            BuiltinFunctionId.KeyDown => false,
            BuiltinFunctionId.KeyPress => false,
            BuiltinFunctionId.MouseX => false,
            BuiltinFunctionId.MouseY => false,
            BuiltinFunctionId.MouseButton => false,
            BuiltinFunctionId.MousePress => false,
            BuiltinFunctionId.Rnd => false,
            BuiltinFunctionId.Date => false,
            BuiltinFunctionId.DateInZone => false,
            BuiltinFunctionId.Time => false,
            BuiltinFunctionId.TimeInZone => false,
            BuiltinFunctionId.Now => false,
            BuiltinFunctionId.NowInZone => false,
            BuiltinFunctionId.UtcDate => false,
            BuiltinFunctionId.UtcTime => false,
            BuiltinFunctionId.UtcNow => false,
            BuiltinFunctionId.NowInstant => false,
            BuiltinFunctionId.Timer => false,
            BuiltinFunctionId.ElapsedSecondsSinceStart => false,
            BuiltinFunctionId.UtcOffset => false,
            BuiltinFunctionId.FileExists => false,
            BuiltinFunctionId.DirExists => false,
            BuiltinFunctionId.PathExists => false,
            BuiltinFunctionId.ReadFile => false,
            BuiltinFunctionId.ReadAllLines => false,
            BuiltinFunctionId.WriteFile => false,
            BuiltinFunctionId.AppendFile => false,
            BuiltinFunctionId.ReadFileEncoding => false,
            BuiltinFunctionId.ReadAllLinesEncoding => false,
            BuiltinFunctionId.WriteFileEncoding => false,
            BuiltinFunctionId.AppendFileEncoding => false,
            BuiltinFunctionId.FileSize => false,
            BuiltinFunctionId.FileDate => false,
            BuiltinFunctionId.FileCreateDate => false,
            BuiltinFunctionId.FileAccessDate => false,
            BuiltinFunctionId.FileDateUtc => false,
            BuiltinFunctionId.FileCreateDateUtc => false,
            BuiltinFunctionId.FileAccessDateUtc => false,
            BuiltinFunctionId.DirDate => false,
            BuiltinFunctionId.DirDateUtc => false,
            BuiltinFunctionId.GetFiles => false,
            BuiltinFunctionId.GetFilesPattern => false,
            BuiltinFunctionId.GetDirs => false,
            BuiltinFunctionId.GetDirsPattern => false,
            BuiltinFunctionId.CombinePath => false,
            BuiltinFunctionId.FileName => false,
            BuiltinFunctionId.DirName => false,
            BuiltinFunctionId.ExtName => false,
            BuiltinFunctionId.FullPath => false,
            BuiltinFunctionId.PathRoot => false,
            BuiltinFunctionId.IsAbsolutePath => false,
            BuiltinFunctionId.MakeDir => false,
            BuiltinFunctionId.DeleteFile => false,
            BuiltinFunctionId.CopyFile => false,
            BuiltinFunctionId.CopyFileOverwrite => false,
            BuiltinFunctionId.MoveFile => false,
            BuiltinFunctionId.MoveFileOverwrite => false,
            BuiltinFunctionId.RenameFile => false,
            BuiltinFunctionId.DeleteDir => false,
            BuiltinFunctionId.DeleteDirRecursive => false,
            BuiltinFunctionId.CopyDir => false,
            BuiltinFunctionId.CopyDirOverwrite => false,
            BuiltinFunctionId.MoveDir => false,
            BuiltinFunctionId.RenameDir => false,
            BuiltinFunctionId.CurrentDir => false,
            BuiltinFunctionId.ChangeDir => false,
            BuiltinFunctionId.DirSize => false,
            BuiltinFunctionId.FreeSpace => false,
            BuiltinFunctionId.TotalFreeSpace => false,
            BuiltinFunctionId.TotalSpace => false,
            BuiltinFunctionId.IsReadOnly => false,
            BuiltinFunctionId.IsHidden => false,
            BuiltinFunctionId.DriveName => false,
            BuiltinFunctionId.DriveFormat => false,
            BuiltinFunctionId.DriveType => false,
            BuiltinFunctionId.MakeTempFile => false,
            BuiltinFunctionId.MakeTempDir => false,
            BuiltinFunctionId.IsOpen => false,
            BuiltinFunctionId.FilePath => false,
            BuiltinFunctionId.FileMode => false,
            BuiltinFunctionId.FilePos => false,
            BuiltinFunctionId.FileLength => false,
            BuiltinFunctionId.LineNumber => false,
            BuiltinFunctionId.Eof => false,
            BuiltinFunctionId.ReadToEnd => false,
            BuiltinFunctionId.ReadText => false,
            BuiltinFunctionId.ReadLine => false,
            BuiltinFunctionId.ReadLines => false,
            BuiltinFunctionId.SkipLine => false,
            BuiltinFunctionId.Write => false,
            BuiltinFunctionId.WriteLine => false,
            BuiltinFunctionId.NewLine => false,
            BuiltinFunctionId.Flush => false,
            BuiltinFunctionId.SeekFile => false,
            BuiltinFunctionId.Rewind => false,
            _ => true
        };
    }

    private BytecodeCallFrame CreateFrame(
        BytecodeRoutine routine,
        ReadOnlySpan<RuntimeValue> arguments,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        BytecodeVariableSlot[]? byRefArgumentSlots = null,
        bool byValValuesAreSnapshots = false)
    {
        var template = _frameTemplatesByRoutineId[routine.Id];
        var argumentSlots = CreateArgumentSlots(routine, arguments, byRefArgumentSlots, byValValuesAreSnapshots);
        return CreateFrameWithSlots(routine, argumentSlots, captures, callSiteLocation, template);
    }

    private BytecodeCallFrame CreateFrameWithSlots(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] argumentSlots,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        RoutineFrameTemplate template)
    {
        if (_cachedFrames.Remove(routine.Id, out var cachedFrame))
        {
            cachedFrame.Reset(argumentSlots, captures, callSiteLocation, template.DefaultLocals);
            return cachedFrame;
        }

        RuntimeValue[] locals;
        int?[] localFixedStringLengths;

        if (routine.LocalSlotCount == 0)
        {
            locals = Array.Empty<RuntimeValue>();
            localFixedStringLengths = Array.Empty<int?>();
        }
        else
        {
            locals = (RuntimeValue[])template.DefaultLocals.Clone();
            localFixedStringLengths = template.LocalFixedStringLengths;
        }

        return new BytecodeCallFrame(
            routine,
            argumentSlots,
            locals,
            localFixedStringLengths,
            captures,
            callSiteLocation,
            routine.MaxEvaluationStackDepth);
    }

    private static BytecodeVariableSlot[] CreateArgumentSlots(
        BytecodeRoutine routine,
        ReadOnlySpan<RuntimeValue> arguments,
        BytecodeVariableSlot[]? byRefArgumentSlots,
        bool byValValuesAreSnapshots)
    {
        var argumentSlots = new BytecodeVariableSlot[routine.ParameterCount];
        var byValIndex = 0;

        for (var index = 0; index < argumentSlots.Length; index++)
        {
            if (byRefArgumentSlots is not null
                && index < byRefArgumentSlots.Length
                && byRefArgumentSlots[index] is not null)
            {
                argumentSlots[index] = byRefArgumentSlots[index];
                continue;
            }

            var value = byValIndex < arguments.Length ? arguments[byValIndex] : RuntimeValue.Void;
            argumentSlots[index] = byValValuesAreSnapshots
                ? BytecodeVariableSlot.Adopt(value)
                : BytecodeVariableSlot.Adopt(value.CopyForStorage());
            byValIndex++;
        }

        return argumentSlots;
    }

    private void ExecuteCurrentInstruction(int runtimeFailureBoundaryDepth = 0)
    {
        var frame = CurrentFrame;
        var instructions = frame.Routine.InstructionArray;
        var instructionCount = instructions.Length;

        if (frame.InstructionPointer < 0 || frame.InstructionPointer >= instructionCount)
        {
            ReportInvalidRuntimeState(
                $"Instruction pointer {frame.InstructionPointer} вышел за пределы процедуры '{frame.Routine.Name}'.",
                ResolveLocation(frame.Routine.Id, Math.Max(0, instructionCount - 1)));
            throw new RuntimeFailureException();
        }

        var instructionIndex = frame.InstructionPointer;
        var instruction = instructions[instructionIndex];
        if (CanUseFastInstructionPath())
        {
            frame.InstructionPointer++;
            try
            {
                ExecuteInstruction(frame, instruction, instructionIndex);
            }
            catch (ArrayAllocationFailureException ex)
            {
                HandleArrayRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
            }
            catch (SetEntryLimitException ex)
            {
                HandleSetRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
            }
            catch (Exception ex) when (ex is SetOperationCancelledException or ValueCopyCancelledException)
            {
                // Операция прервана внутри собственного обхода: получатель не
                // изменён, выполнение прекращается как при обычной отмене.
                CancelRun();
                return;
            }
            catch (StringResourceLimitException ex)
            {
                HandleStringResourceFailure(ex.Message, frame, instructionIndex, runtimeFailureBoundaryDepth);
            }
            catch (OutOfMemoryException)
            {
                HandleStringResourceFailure("Недостаточно памяти для результата операции.", frame, instructionIndex, runtimeFailureBoundaryDepth);
            }
            catch (ExpiredArrayViewException ex)
            {
                HandleArrayRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
            }
            catch (EvaluationStackUnderflowException ex)
            {
                ReportInvalidRuntimeState(ex.Message, ResolveLocation(frame.Routine.Id, instructionIndex));
                throw new RuntimeFailureException();
            }
            catch (VmCallbackSuspension suspension)
            {
                PushSuspendedCallback(suspension);
            }
            catch (RuntimeFailureException) when (TryHandleRuntimeFailure(runtimeFailureBoundaryDepth))
            {
            }
            return;
        }

        var needsExecutionSnapshot = NeedsExecutionSnapshot();
        var snapshot = needsExecutionSnapshot
            ? CreateExecutionSnapshot(frame, instructionIndex, instruction)
            : null;
        // Без внешних обратных вызовов проверка остановки не меняет состояние программы.
        // Снимок перед остановленной инструкцией всё равно не используется для отката.
        var canCheckStopFirst = _instructionObserver is null && _shouldStopBeforeInstruction is null;
        if (canCheckStopFirst && snapshot is not null && ShouldStopBefore(snapshot))
        {
            _stopState = new BytecodeVmStopState(snapshot, this, _previousReversibleStateSnapshot);
            return;
        }

        var shouldCaptureReversibleState = ShouldCaptureReversibleStateHistory();
        var previousSnapshot = _previousReversibleStateSnapshot;
        var previousInstruction = previousSnapshot?.ExecutionSnapshot;
        // Откат выбирает первую инструкцию предыдущей строки. После загрузки
        // неизменяемого 0 или 1 более ранний снимок той же строки достаточен.
        var canReusePrevious = canCheckStopFirst && shouldCaptureReversibleState
            && snapshot is not null && previousInstruction is not null
            && previousInstruction.RoutineId == snapshot.RoutineId
            && previousInstruction.CallDepth == snapshot.CallDepth
            && previousInstruction.InstructionIndex + 1 == snapshot.InstructionIndex
            && previousInstruction.Opcode is Opcode.PushIntegerZero or Opcode.PushIntegerOne
            && SourceLine.From(previousInstruction.SourceLocation) == SourceLine.From(snapshot.SourceLocation)
            && _openFiles.Count == 0 && !HasActiveCallbackContinuation();
        var stateBeforeInstruction = canReusePrevious
            ? previousSnapshot
            : shouldCaptureReversibleState ? TryCreateReversibleStateSnapshot(snapshot!) : null;
        if (snapshot is not null)
        {
            _instructionObserver?.Invoke(snapshot);
        }

        if (!canCheckStopFirst && snapshot is not null && ShouldStopBefore(snapshot))
        {
            _stopState = new BytecodeVmStopState(snapshot, this, _previousReversibleStateSnapshot);
            return;
        }

        _previousReversibleStateSnapshot = stateBeforeInstruction;
        frame.InstructionPointer++;
        try
        {
            ExecuteInstruction(frame, instruction, instructionIndex);
        }
        catch (ArrayAllocationFailureException ex)
        {
            HandleArrayRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
        }
        catch (SetEntryLimitException ex)
        {
            HandleSetRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
        }
        catch (Exception ex) when (ex is SetOperationCancelledException or ValueCopyCancelledException)
        {
            CancelRun();
            return;
        }
        catch (StringResourceLimitException ex)
        {
            HandleStringResourceFailure(ex.Message, frame, instructionIndex, runtimeFailureBoundaryDepth);
        }
        catch (OutOfMemoryException)
        {
            HandleStringResourceFailure("Недостаточно памяти для результата операции.", frame, instructionIndex, runtimeFailureBoundaryDepth);
        }
        catch (ExpiredArrayViewException ex)
        {
            HandleArrayRuntimeFailure(ex, frame, instructionIndex, runtimeFailureBoundaryDepth);
        }
        catch (EvaluationStackUnderflowException ex)
        {
            ReportInvalidRuntimeState(ex.Message, ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }
        catch (VmCallbackSuspension suspension)
        {
            PushSuspendedCallback(suspension);
        }
        catch (RuntimeFailureException) when (TryHandleRuntimeFailure(runtimeFailureBoundaryDepth))
        {
        }
    }

    private void ExecuteInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        switch (instruction.Opcode)
        {
            case Opcode.PushConst:
                frame.EvaluationStack.PushUnchecked(ReadConstantUnchecked(instruction.GetOperand(0)));
                break;

            case Opcode.PushIntegerZero:
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromInteger(0));
                break;

            case Opcode.PushIntegerOne:
                frame.EvaluationStack.PushUnchecked(RuntimeValue.FromInteger(1));
                break;

            case Opcode.LoadFunction:
                frame.EvaluationStack.Push(LoadFunctionValue(frame, instruction.GetOperand(0), instructionIndex));
                break;

            case Opcode.LoadBuiltinFunction:
                frame.EvaluationStack.Push(LoadBuiltinFunctionValue(frame, instruction.GetOperand(0), instructionIndex));
                break;

            case Opcode.LoadGlobal:
                frame.EvaluationStack.Push(LoadGlobalUnchecked(instruction.GetOperand(0)));
                break;

            case Opcode.StoreGlobal:
                StoreGlobalUnchecked(instruction.GetOperand(0), PopValue(frame, instructionIndex));
                break;

            case Opcode.AppendStringGlobal:
                ExecuteAppendStringGlobal(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadLocal:
                frame.PushLocalValueOntoEvaluationStack(instruction.GetOperand(0));
                break;

            case Opcode.StoreLocal:
                var localSlot = instruction.GetOperand(0);
                if (frame.EvaluationStack.TryGetImmediateInteger(out var immediateInteger)
                    && frame.TryStoreImmediateInteger(localSlot, immediateInteger))
                    frame.EvaluationStack.DiscardTop(1);
                else
                    frame.StoreLocalValue(localSlot, PopValue(frame, instructionIndex));
                break;

            case Opcode.FreshLocal:
                frame.FreshLocal(instruction.GetOperand(0));
                break;

            case Opcode.ClearForEachItem:
                frame.ClearForEachItem(instruction.GetOperand(0));
                break;

            case Opcode.BindArrayElement:
                ExecuteBindArrayElement(frame, instruction);
                break;

            case Opcode.AppendStringLocal:
                ExecuteAppendStringLocal(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadArgument:
                var argument = frame.Arguments[instruction.GetOperand(0)];
                if (argument.TryGetImmediateInteger(out var integerArgument))
                    frame.EvaluationStack.PushInteger(integerArgument);
                else
                    frame.EvaluationStack.Push(argument.Value);
                break;

            case Opcode.StoreArgument:
                frame.Arguments[instruction.GetOperand(0)].Store(PopValue(frame, instructionIndex));
                break;

            case Opcode.AppendStringArgument:
                ExecuteAppendStringSlot(frame.Arguments[instruction.GetOperand(0)], PopValue(frame, instructionIndex), instruction.GetOperand(1) != 0);
                break;

            case Opcode.LoadCapture:
                frame.EvaluationStack.Push(frame.Captures[instruction.GetOperand(0)].Value);
                break;

            case Opcode.StoreCapture:
                frame.Captures[instruction.GetOperand(0)].Store(PopValue(frame, instructionIndex));
                break;

            case Opcode.AppendStringCapture:
                ExecuteAppendStringSlot(frame.Captures[instruction.GetOperand(0)], PopValue(frame, instructionIndex), instruction.GetOperand(1) != 0);
                break;

            case Opcode.Pop:
                _ = PopValue(frame, instructionIndex);
                break;

            case Opcode.SnapshotValue:
                frame.EvaluationStack.PushUnchecked(PopValue(frame, instructionIndex).CopyForStorage());
                break;

            case Opcode.Negate:
                ExecuteNegateInstruction(frame, instructionIndex);
                break;

            case Opcode.Concat:
                ExecuteBinaryInstruction(frame, instruction.Opcode, instructionIndex);
                break;

            case Opcode.Add:
                ExecuteAddInstruction(frame, instructionIndex);
                break;

            case Opcode.AddLongInteger:
                ExecuteAddLongIntegerInstruction(frame, instructionIndex);
                break;

            case Opcode.Subtract:
                ExecuteSubtractInstruction(frame, instructionIndex);
                break;

            case Opcode.Power:
                ExecuteBinaryInstruction(frame, instruction.Opcode, instructionIndex);
                break;

            case Opcode.IntegralDivide:
                ExecuteIntegralDivideInstruction(frame, instructionIndex);
                break;

            case Opcode.Eqv:
            case Opcode.Imp:
                ExecuteIntegerLogicalInstruction(frame, instruction.Opcode, instructionIndex);
                break;

            case Opcode.Divide:
                ExecuteDivideInstruction(frame, instructionIndex);
                break;

            case Opcode.Modulo:
                ExecuteModuloInstruction(frame, instructionIndex);
                break;

            case Opcode.And:
                ExecuteAndInstruction(frame, instructionIndex);
                break;

            case Opcode.Or:
                ExecuteOrInstruction(frame, instructionIndex);
                break;

            case Opcode.Xor:
                ExecuteXorInstruction(frame, instructionIndex);
                break;

            case Opcode.ShiftLeft:
                ExecuteShiftLeftInstruction(frame, instructionIndex);
                break;

            case Opcode.ShiftRight:
                ExecuteShiftRightInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareEqual:
                ExecuteCompareEqualInstruction(frame);
                break;

            case Opcode.CompareNotEqual:
                ExecuteCompareNotEqualInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareLess:
                ExecuteCompareLessInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareLessOrEqual:
                ExecuteCompareLessOrEqualInstruction(frame, instructionIndex);
                break;

            case Opcode.Multiply:
                ExecuteMultiplyInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareGreater:
                ExecuteCompareGreaterInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareGreaterOrEqual:
                ExecuteCompareGreaterOrEqualInstruction(frame, instructionIndex);
                break;

            case Opcode.Not:
                ExecuteNotInstruction(frame, instructionIndex);
                break;

            case Opcode.Convert:
                ExecuteConvert(frame, instruction, instructionIndex);
                break;

            case Opcode.Jump:
                frame.InstructionPointer = instruction.GetOperand(0);
                break;

            case Opcode.JumpIfEqual:
                ExecuteJumpIfEqualInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfNotEqual:
                ExecuteJumpIfNotEqualInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfLess:
                ExecuteJumpIfLessInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfLessOrEqual:
                ExecuteJumpIfLessOrEqualInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfGreater:
                ExecuteJumpIfGreaterInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfGreaterOrEqual:
                ExecuteJumpIfGreaterOrEqualInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfForDoneLocal:
                ExecuteJumpIfForDoneLocalInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.JumpIfTrue:
                ExecuteJumpIfBooleanInstruction(frame, instruction, invertCondition: false);
                break;

            case Opcode.JumpIfFalse:
                ExecuteJumpIfBooleanInstruction(frame, instruction, invertCondition: true);
                break;

            case Opcode.Call:
            case Opcode.CallByRef:
                ExecuteRoutineCall(frame, instruction, instructionIndex);
                break;

            case Opcode.CallValue:
                ExecuteFunctionValueCall(frame, instruction, instructionIndex);
                break;

            case Opcode.CallBuiltin:
                ExecuteBuiltinCall(frame, instruction, instructionIndex);
                break;

            case Opcode.OpenFile:
                ExecuteOpenFile(frame, instruction, instructionIndex);
                break;

            case Opcode.CloseFile:
                ExecuteCloseFile(frame, instructionIndex);
                break;

            case Opcode.RedimArray:
                ExecuteRedimArray(frame, instruction, instructionIndex);
                break;

            case Opcode.NewArray:
                ExecuteNewArray(frame, instruction, instructionIndex);
                break;

            case Opcode.GetArrayStructuralVersion:
                ExecuteGetArrayStructuralVersion(frame, instructionIndex);
                break;

            case Opcode.EraseArray:
                ExecuteEraseArray(frame, instructionIndex);
                break;

            case Opcode.LoadElement:
                ExecuteLoadElement(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreElement:
                ExecuteStoreElement(frame, instruction, instructionIndex);
                break;

            case Opcode.CreateRecord:
                ExecuteCreateRecord(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadField:
                ExecuteLoadField(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreField:
                ExecuteStoreField(frame, instruction, instructionIndex);
                break;

            case Opcode.ValidateTypeInvariant:
                ValidateTypeInvariant(frame, instruction, instructionIndex);
                break;

            case Opcode.CreateDictionary:
                ExecuteCreateDictionary(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadDictionaryValue:
                ExecuteLoadDictionaryValue(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreDictionaryValue:
                ExecuteStoreDictionaryValue(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryAdd:
                ExecuteDictionaryAdd(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryExists:
                ExecuteDictionaryExists(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryRemove:
                ExecuteDictionaryRemove(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryKeys:
                ExecuteDictionaryKeys(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryCount:
                ExecuteDictionaryCount(frame, instruction, instructionIndex);
                break;

            case Opcode.DictionaryTryGetValue:
                ExecuteDictionaryTryGetValue(frame, instruction, instructionIndex);
                break;

            case Opcode.CreateSet:
                ExecuteCreateSet(frame, instruction, instructionIndex);
                break;

            case Opcode.SetAdd:
                ExecuteSetAdd(frame, instruction, instructionIndex);
                break;

            case Opcode.SetContains:
                ExecuteSetContains(frame, instruction, instructionIndex);
                break;

            case Opcode.SetRemove:
                ExecuteSetRemove(frame, instruction, instructionIndex);
                break;

            case Opcode.SetCount:
                ExecuteSetCount(frame, instruction, instructionIndex);
                break;

            case Opcode.SetToArray:
                ExecuteSetToArray(frame, instruction, instructionIndex);
                break;

            case Opcode.SetUnionWith:
                ExecuteSetUnionWith(frame, instruction, instructionIndex);
                break;

            case Opcode.SetIntersectWith:
                ExecuteSetIntersectWith(frame, instruction, instructionIndex);
                break;

            case Opcode.ValidateForStep:
                ValidateForStep(frame, instructionIndex);
                break;

            case Opcode.AdvanceForLoopLocal:
                ExecuteAdvanceForLoopLocalInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.PrintValue:
                AppendPendingOutput(PopValue(frame, instructionIndex));
                break;

            case Opcode.PrintUsing:
                ExecutePrintUsing(frame, instruction, instructionIndex);
                break;

            case Opcode.PrintSeparator:
                ExecutePrintSeparator(instruction.GetOperand(0), instructionIndex);
                break;

            case Opcode.PrintLine:
                FlushPendingOutput(forceEmptyLine: true);
                break;

            case Opcode.Input:
                ExecuteInput(frame, instruction, instructionIndex);
                break;

            case Opcode.Sleep:
                ExecuteSleepInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.ReseedRandom:
                ExecuteReseedRandomInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.FormatInterpolatedPart:
                ExecuteFormatInterpolatedPart(frame, instructionIndex);
                break;

            case Opcode.Screen:
                ExecuteScreenInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Canvas:
                ExecuteCanvasInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Smooth:
                _host.SetGraphicsSmoothing(instruction.GetOperand(0) != 0);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.BeginFrame:
                ExecuteBeginFrameInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Present:
                ExecutePresentInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Camera:
                ExecuteCameraInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Viewport:
                ExecuteViewportInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.ResetCamera:
                ExecuteResetCameraInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.View:
                ExecuteViewInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.ViewPrint:
                ExecuteViewPrintInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Window:
                ExecuteWindowInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Cls:
                ExecuteClsInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Color:
                ExecuteColorInstruction(frame, instruction, instructionIndex);
                break;

            case Opcode.Locate:
                ExecuteLocateInstruction(frame, instructionIndex);
                break;

            case Opcode.Palette:
                ExecutePaletteInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.PCopy:
                ExecutePCopyInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.PSet:
                ExecutePSetInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Get:
                ExecuteGetInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Put:
                ExecutePutInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Line:
                ExecuteLineInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Circle:
                ExecuteCircleInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Paint:
                ExecutePaintInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.Draw:
                ExecuteDrawInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawRect:
                ExecuteDrawRectInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.FillRect:
                ExecuteFillRectInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawLine:
                ExecuteDrawLineInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawCircle:
                ExecuteDrawCircleInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.FillCircle:
                ExecuteFillCircleInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawImage:
                ExecuteDrawImageInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawSprite:
                ExecuteDrawSpriteInstruction(frame, instruction, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawTile:
                ExecuteDrawTileInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawTilemap:
                ExecuteDrawTilemapInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawTileLayer:
                ExecuteDrawTileLayerInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.SetMapTile:
                ExecuteSetMapTileInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.DrawText:
                ExecuteDrawTextInstruction(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.StatementBoundary:
                ExecuteStatementBoundary(instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.RegisterKeyHandler:
                ExecuteRegisterKeyHandler(frame, instructionIndex, instruction);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.UnregisterKeyHandler:
                ExecuteUnregisterKeyHandler(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.PauseKeyHandler:
                ExecutePauseKeyHandler(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.ResumeKeyHandler:
                ExecuteResumeKeyHandler(frame, instructionIndex);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.RegisterMouseHandler:
                ExecuteRegisterMouseHandler(frame, instructionIndex, instruction);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.UnregisterMouseHandler:
                ExecuteUnregisterMouseHandler(frame, instructionIndex, instruction);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.PauseMouseHandler:
                ExecutePauseMouseHandler(frame, instructionIndex, instruction);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.ResumeMouseHandler:
                ExecuteResumeMouseHandler(frame, instructionIndex, instruction);
                InvalidateReverseExecutionHistory();
                break;

            case Opcode.PushErrorHandler:
                ExecutePushErrorHandler(frame, instruction);
                break;

            case Opcode.PopErrorHandler:
                _ = frame.TryPopErrorHandler(out _);
                break;

            case Opcode.RaiseError:
                ExecuteRaiseError(frame, instruction, instructionIndex);
                break;

            case Opcode.EndFinally:
                ExecuteEndFinally(frame);
                break;

            case Opcode.Return:
                ExecuteReturn(frame, instructionIndex);
                break;

            default:
                ReportInvalidRuntimeState(
                    $"Bytecode VM пока не поддерживает opcode '{instruction.Opcode}'.",
                    ResolveLocation(frame.Routine.Id, instructionIndex));
                throw new RuntimeFailureException();
        }
    }

    private bool CanUseFastInstructionPath()
    {
        return _canUseFastInstructionPath;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefreshFastInstructionPathFlag()
    {
        _canUseFastInstructionPath = _instructionObserver is null
            && _breakpoints is null
            && _shouldStopBeforeInstruction is null
            && _debugStepBoundary is null
            && _skipStopOnceAt is null
            && _suppressLineBreakpointsUntilDifferentLine is null;
    }

    private void ExecutePrintSeparator(int separatorKind, int instructionIndex)
    {
        switch (separatorKind)
        {
            case 0:
                _hasPendingOutput = true;
                break;

            case 1:
                AppendPrintSeparator();
                break;

            default:
                ReportInvalidRuntimeState(
                    $"Неизвестный вид разделителя PRINT: {separatorKind}.",
                    ResolveCurrentLocation(instructionIndex));
                throw new RuntimeFailureException();
        }
    }

    private void ExecutePrintUsing(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var valueCount = instruction.GetOperand(0);
        var separatorKind = instruction.GetOperand(1);
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var stack = frame.EvaluationStack;
        var rentedValues = valueCount > 0 ? ArrayPool<RuntimeValue>.Shared.Rent(valueCount) : Array.Empty<RuntimeValue>();

        try
        {
            for (var index = valueCount - 1; index >= 0; index--)
            {
                rentedValues[index] = stack.Pop();
            }

            var format = stack.Pop().AsString();
            AppendFormattedUsing(format, rentedValues.AsSpan(0, valueCount), location);
        }
        finally
        {
            if (valueCount > 0)
            {
                ArrayPool<RuntimeValue>.Shared.Return(rentedValues, clearArray: true);
            }
        }

        switch (separatorKind)
        {
            case 0:
                FlushPendingOutput(forceEmptyLine: false);
                break;

            case 1:
                _hasPendingOutput = true;
                break;

            case 2:
                AppendPrintSeparator();
                break;

            default:
                ReportInvalidRuntimeState(
                    $"Неизвестный вид завершающего разделителя PRINT USING: {separatorKind}.",
                    location);
                throw new RuntimeFailureException();
        }
    }

    private void ValidateForStep(BytecodeCallFrame frame, int instructionIndex)
    {
        var stepValue = PopValue(frame, instructionIndex);
        if (!IsZero(stepValue))
        {
            return;
        }

        ReportRuntimeDiagnostic(
            DiagnosticCodes.StepZero,
            "В цикле FOR недопустим STEP 0.",
            ResolveLocation(frame.Routine.Id, instructionIndex));
        throw new RuntimeFailureException();
    }

    private void ValidateTypeInvariant(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var result = PopValue(frame, instructionIndex);
        if (result.Type != TypeSymbol.Boolean)
        {
            ReportInvalidRuntimeState(
                "Проверка инварианта TYPE ожидала значение BOOLEAN.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        if (result.AsBoolean())
        {
            return;
        }

        var typeName = ReadConstantUnchecked(instruction.GetOperand(0)).AsString();
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var constructorChainFrames = CreateConstructorChainFrames(instruction, location);
        if (constructorChainFrames is null)
        {
            ReportInvalidRuntimeState(
                $"Инвариант TYPE '{typeName}' вернул FALSE после завершения конструктора.",
                location);
        }
        else
        {
            ReportInvalidRuntimeStateWithChain(
                $"Инвариант TYPE '{typeName}' вернул FALSE после завершения конструктора.",
                location,
                constructorChainFrames);
        }

        throw new RuntimeFailureException();
    }

    private IReadOnlyList<RuntimeCallChainFrame>? CreateConstructorChainFrames(BytecodeInstruction instruction, TextLocation callSite)
    {
        // Кадр конструктора к моменту проверки инварианта уже выгружен, поэтому
        // цепочка строится по операнду инструкции, сохраняя имя конструктора
        // и место его вызова в диагностике.
        if (instruction.OperandCount < 2
            || !_routines.TryGetValue(instruction.GetOperand(1), out var constructorRoutine))
        {
            return null;
        }

        return [new RuntimeCallChainFrame(constructorRoutine.Symbol, constructorRoutine.Name, callSite)];
    }

    private void ExecuteReturn(BytecodeCallFrame frame, int instructionIndex)
    {
        if (frame.Routine.ResultType == TypeSymbol.Integer && frame.ErrorHandlerCount == 0
            && !frame.HasCallbackContinuation && frame.EvaluationStack.TryGetImmediateInteger(out var integerResult))
        {
            var discard = frame.DiscardReturnValue;
            frame.EvaluationStack.DiscardTop(1);
            _callStack.Pop();
            CacheReusableFrame(frame);
            if (_callStack.Count > 0 && !discard)
                CurrentFrame.EvaluationStack.PushInteger(integerResult);
            return;
        }

        RuntimeValue returnValue = RuntimeValue.Void;

        if (frame.Routine.ResultType != TypeSymbol.Void)
        {
            if (frame.EvaluationStack.Count == 0)
            {
                ReportRuntimeDiagnostic(
                    DiagnosticCodes.RuntimeMissingReturnValue,
                    $"Функция '{frame.Routine.Name}' завершилась без RETURN со значением.",
                    ResolveLocation(frame.Routine.Id, instructionIndex));
                throw new RuntimeFailureException();
            }

            returnValue = frame.EvaluationStack.Pop().CopyForStorage();
        }

        if (TryEnterFinallyForReturn(frame, returnValue))
        {
            return;
        }

        CompleteReturn(frame, returnValue);
    }

    private bool TryEnterFinallyForReturn(BytecodeCallFrame frame, RuntimeValue returnValue)
    {
        var handlers = frame.ErrorHandlers;
        var selectedIndex = -1;

        for (var index = handlers.Count - 1; index >= 0; index--)
        {
            var candidate = handlers[index];
            if (candidate.FinallyTarget >= 0 && candidate.State != BytecodeErrorHandlerState.ExecutingFinally)
            {
                selectedIndex = index;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            return false;
        }

        // Обработчики, вложенные в покидаемый регион, больше не должны
        // участвовать в завершении кадра.
        while (frame.ErrorHandlerCount - 1 > selectedIndex)
        {
            _ = frame.TryPopErrorHandler(out _);
        }

        var handler = frame.PeekErrorHandler()!;
        frame.EvaluationStack.Clear();
        handler.State = BytecodeErrorHandlerState.ExecutingFinally;
        handler.HasPendingReturn = true;
        handler.PendingReturnValue = returnValue;
        frame.InstructionPointer = handler.FinallyTarget;
        return true;
    }

    private void CompleteReturn(BytecodeCallFrame frame, RuntimeValue returnValue)
    {
        var callbackContinuation = frame.CallbackContinuation;
        var callbackCompletion = frame.CallbackCompletion ?? (_ => { });
        var discardReturnValue = frame.DiscardReturnValue;

        _callStack.Pop();
        CacheReusableFrame(frame);

        if (callbackContinuation is not null)
        {
            ResumeCallbackChain(callbackContinuation(returnValue), callbackCompletion);
            return;
        }

        if (_callStack.Count == 0)
        {
            return;
        }

        if (frame.Routine.ResultType != TypeSymbol.Void && !discardReturnValue)
        {
            CurrentFrame.EvaluationStack.Push(returnValue);
        }
    }

    private void CacheReusableFrame(BytecodeCallFrame frame)
    {
        frame.ReleaseReferences();
        _cachedFrames.TryAdd(frame.Routine.Id, frame);
    }

    private TextLocation ResolveCurrentLocation(int instructionIndex)
    {
        var frame = CurrentFrame;
        return ResolveLocation(frame.Routine.Id, instructionIndex);
    }

    private readonly record struct SourceLine(string? Document, int Line)
    {
        public static SourceLine From(TextLocation location) => new(location.Document, location.Line);
    }

    private readonly record struct DebugStepBoundary(DebugStepMode Mode, SourceLine StartLine, int StartCallDepth);
    private readonly record struct RoutineFrameTemplate(RuntimeValue[] DefaultLocals, int?[] LocalFixedStringLengths);

    private readonly record struct CaptureBinding(CaptureSourceKind Kind, int Slot);

    private enum CaptureSourceKind : byte
    {
        Argument,
        Local,
        Capture
    }

    private enum DebugStepMode
    {
        None,
        Into,
        Over,
        Out
    }
}
