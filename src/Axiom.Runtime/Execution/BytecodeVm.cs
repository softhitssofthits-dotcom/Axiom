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
    private readonly Dictionary<int, VmInstruction[]> _decodedInstructions;
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
    private readonly VmValue[] _globalValuesBySlot;
    private readonly int?[] _globalFixedStringLengths;
    private readonly bool[] _hasGlobalSlot;
    private readonly Stack<BytecodeCallFrame> _callStack = new();

    private readonly Dictionary<int, BytecodeCallFrame> _cachedFrames = [];
    private readonly HashSet<RuntimeTextFile> _openFiles = [];
    private readonly StringBuilder _pendingOutput = new();
    // Кэш колбэка отчёта переполнения: метод-группа экземпляра аллоцирует
    // новый делегат при каждом преобразовании, кэш делает путь безаллокационным.
    private readonly Action<string, string, TextLocation> _reportOverflow;
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
        _reportOverflow = ReportRuntimeDiagnostic;

        // Начало монотонного измерения фиксируется при создании исполнителя,
        // поэтому ELAPSEDSECONDS() считает время именно этого запуска.
        _monotonicOriginSeconds = host.GetMonotonicSeconds();
        _instructionObserver = instructionObserver;
        _shouldStopBeforeInstruction = shouldStopBeforeInstruction;
        _breakpoints = breakpoints;
        _routines = module.Routines.ToDictionary(static routine => routine.Id);
        // Раскодированный поток строится для всех подпрограмм сразу: VM читает
        // инструкции из непрерывных массивов структур, исходный модуль остаётся
        // источником истины для валидации, сериализации и отладки. Слияние
        // применяется только к быстрому пути: отладочная VM получает неслитный
        // поток, чтобы поколоночные точки остановки и пошаговое исполнение
        // видели каждую исходную инструкцию (деоптимизация под отладкой).
        _decodedInstructions = module.Routines.ToDictionary(
            static routine => routine.Id,
            routine => DecodeInstructions(
                routine.InstructionArray,
                fuse: instructionObserver is null
                    && shouldStopBeforeInstruction is null
                    && breakpoints is null));
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
        _globalValuesBySlot = maxGlobalSlot >= 0 ? new VmValue[maxGlobalSlot + 1] : Array.Empty<VmValue>();
        _globalFixedStringLengths = maxGlobalSlot >= 0 ? new int?[maxGlobalSlot + 1] : Array.Empty<int?>();
        _hasGlobalSlot = maxGlobalSlot >= 0 ? new bool[maxGlobalSlot + 1] : Array.Empty<bool>();

        foreach (var global in module.Globals)
        {
            // Запись глобальной переменной создаётся её инструкцией объявления:
            // отложенная стратегия не должна превращаться в выделение всех полей-массивов
            // ещё до начала исполнения программы. Записи TYPE остаются пустым слотом
            // до первого чтения (EnsureGlobalValueCreated).
            _globalValuesBySlot[global.Slot] = global.Type is UserDefinedTypeSymbol
                ? default
                : VmValue.FromRuntime(RuntimeValue.Default(global.Type, global.FixedStringLength));
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

        RunInterpreterLoop();
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
            RunInterpreterLoop();
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

    /// <summary>
    /// Единый цикл исполнения. Режим выбирается на входе: отладчик возобновляет
    /// выполнение через ResumeFrom, поэтому каждый возобновлённый запуск заново
    /// выбирает между плотным путём и полным путём с наблюдением и снимками.
    /// </summary>
    private void RunInterpreterLoop()
    {
        while (_callStack.Count > 0 && _stopState is null)
        {
            if (CheckCancellationRequested())
            {
                break;
            }

            if (_canUseFastInstructionPath)
            {
                RunCurrentFrameFast();
            }
            else
            {
                ExecuteCurrentInstruction();
            }
        }
    }

    /// <summary>
    /// Плотный цикл исполнения текущего кадра без внешнего наблюдения. Кадр,
    /// поток инструкций и стек подняты в локальные переменные; вызов процедуры
    /// или возврат покидают цикл, а не переищут текущий кадр на инструкцию.
    /// Горячие тривиальные опкоды исполняются прямо в цикле, остальные
    /// передаются общему switch. Диагностики и границы перехвата те же, что у
    /// полного пути.
    /// </summary>
    private void RunCurrentFrameFast()
    {
        var frame = _callStack.Peek();
        var instructions = frame.DecodedInstructions;
        var instructionCount = instructions.Length;
        var stack = frame.EvaluationStack;
        var ip = frame.InstructionPointer;

        try
        {
            while (true)
            {
                if (CheckCancellationRequested())
                {
                    return;
                }

                if ((uint)ip >= (uint)instructionCount)
                {
                    ReportInvalidRuntimeState(
                        $"Instruction pointer {ip} вышел за пределы процедуры '{frame.Routine.Name}'.",
                        ResolveLocation(frame.Routine.Id, Math.Max(0, instructionCount - 1)));
                    throw new RuntimeFailureException();
                }

                var instruction = instructions[ip];
                frame.InstructionPointer = ip + 1;
                var currentOpcode = instruction.Opcode;

                // Слитные пары обрабатываются до общего свитча единым вызовом:
                // кейсы внутри свитча и развёрнутые обработчики в теле цикла
                // увеличивали разогрев метода (наблюдение O07-6). Управление
                // доходит до конца итерации, где ip синхронизируется
                // с frame.InstructionPointer, поэтому continue здесь нельзя.
                if (currentOpcode is Opcode.LoadLocalPair or Opcode.LoadLocalConst)
                {
                    ExecuteFusedLoadPair(frame, instruction, ip);
                }
                else
                {
                    switch (currentOpcode)
                    {
                    case Opcode.LoadLocal:
                        frame.PushLocalValueOntoEvaluationStack(instruction.A);
                        break;

                    case Opcode.StoreLocal:
                        var localSlot = instruction.A;
                        if (stack.TryGetImmediateInteger(out var immediateInteger)
                            && frame.TryStoreImmediateInteger(localSlot, immediateInteger))
                            stack.DiscardTop(1);
                        else if (stack.TryGetImmediateLong(out var immediateLong)
                            && frame.TryStoreImmediateLong(localSlot, immediateLong))
                            stack.DiscardTop(1);
                        else if (stack.TryGetImmediateDouble(out var immediateDouble)
                            && frame.TryStoreImmediateDouble(localSlot, immediateDouble))
                            stack.DiscardTop(1);
                        else
                            frame.StoreLocalValue(localSlot, PopValue(frame, ip));
                        break;

                    case Opcode.PushConst:
                        stack.PushUnchecked(ReadConstantUnchecked(instruction.A));
                        break;

                    case Opcode.PushIntegerZero:
                        // Непосредственный ноль сохраняет распакованное представление
                        // для последующих целочисленных операций и записи в слоты.
                        stack.PushInteger(0);
                        break;

                    case Opcode.PushIntegerOne:
                        stack.PushInteger(1);
                        break;

                    case Opcode.LoadGlobal:
                        PushGlobalValueOntoEvaluationStack(stack, instruction.A);
                        break;

                    case Opcode.StoreGlobal:
                        var globalSlot = instruction.A;
                        if (stack.TryGetImmediateInteger(out var storeGlobalInteger)
                            && TryStoreGlobalImmediateInteger(globalSlot, storeGlobalInteger))
                            stack.DiscardTop(1);
                        else if (stack.TryGetImmediateLong(out var storeGlobalLong)
                            && TryStoreGlobalImmediateLong(globalSlot, storeGlobalLong))
                            stack.DiscardTop(1);
                        else if (stack.TryGetImmediateDouble(out var storeGlobalDouble)
                            && TryStoreGlobalImmediateDouble(globalSlot, storeGlobalDouble))
                            stack.DiscardTop(1);
                        else
                            StoreGlobalUnchecked(globalSlot, PopValue(frame, ip));
                        break;

                    case Opcode.Pop:
                        _ = PopValue(frame, ip);
                        break;

                    case Opcode.Jump:
                        frame.InstructionPointer = instruction.A;
                        break;

                    case Opcode.Call:
                    case Opcode.CallByRef:
                        ExecuteRoutineCall(frame, instruction, ip);
                        if (_callStack.Peek() != frame)
                        {
                            return;
                        }

                        break;

                    case Opcode.CallValue:
                        ExecuteFunctionValueCall(frame, instruction, ip);
                        if (_callStack.Peek() != frame)
                        {
                            return;
                        }

                        break;

                    case Opcode.CallBuiltin:
                        ExecuteBuiltinCall(frame, instruction, ip);
                        if (_callStack.Peek() != frame)
                        {
                            return;
                        }

                        break;

                    case Opcode.Return:
                        ExecuteReturn(frame, ip);
                        if (_callStack.Count == 0 || _callStack.Peek() != frame)
                        {
                            return;
                        }

                        break;

                    case Opcode.EndFinally:
                        // Завершение finally может закончить возврат из процедуры
                        // и выгрузить текущий кадр: продолжать его цикл нельзя.
                        ExecuteEndFinally(frame);
                        if (_callStack.Count == 0 || _callStack.Peek() != frame)
                        {
                            return;
                        }

                        break;

                    case Opcode.LoadFunction:
                        stack.Push(LoadFunctionValue(frame, instruction.A, ip));
                        break;

                    case Opcode.LoadBuiltinFunction:
                        stack.Push(LoadBuiltinFunctionValue(frame, instruction.A, ip));
                        break;

                    case Opcode.AppendStringGlobal:
                        ExecuteAppendStringGlobal(frame, instruction, ip);
                        break;

                    case Opcode.FreshLocal:
                        frame.FreshLocal(instruction.A);
                        break;

                    case Opcode.ClearForEachItem:
                        frame.ClearForEachItem(instruction.A);
                        break;

                    case Opcode.BindArrayElement:
                        ExecuteBindArrayElement(frame, instruction);
                        break;

                    case Opcode.AppendStringLocal:
                        ExecuteAppendStringLocal(frame, instruction, ip);
                        break;

                    case Opcode.LoadArgument:
                        var argument = frame.Arguments[instruction.A];
                        if (argument.TryGetImmediateInteger(out var integerArgument))
                            stack.PushInteger(integerArgument);
                        else if (argument.TryGetImmediateDouble(out var doubleArgumentValue))
                            stack.PushDouble(doubleArgumentValue);
                        else
                            stack.Push(argument.Value);
                        break;

                    case Opcode.StoreArgument:
                        frame.Arguments[instruction.A].Store(PopValue(frame, ip));
                        break;

                    case Opcode.AppendStringArgument:
                        ExecuteAppendStringSlot(frame.Arguments[instruction.A], PopValue(frame, ip), instruction.B != 0);
                        break;

                    case Opcode.LoadCapture:
                        stack.Push(frame.Captures[instruction.A].Value);
                        break;

                    case Opcode.StoreCapture:
                        frame.Captures[instruction.A].Store(PopValue(frame, ip));
                        break;

                    case Opcode.AppendStringCapture:
                        ExecuteAppendStringSlot(frame.Captures[instruction.A], PopValue(frame, ip), instruction.B != 0);
                        break;

                    case Opcode.SnapshotValue:
                        stack.PushUnchecked(PopValue(frame, ip).CopyForStorage());
                        break;

                    case Opcode.Negate:
                        ExecuteNegateInstruction(frame, ip);
                        break;

                    case Opcode.Concat:
                        ExecuteBinaryInstruction(frame, instruction.Opcode, ip);
                        break;

                    case Opcode.Add:
                        ExecuteAddInstruction(frame, ip);
                        break;

                    case Opcode.AddLongInteger:
                        ExecuteAddLongIntegerInstruction(frame, ip);
                        break;

                    case Opcode.Subtract:
                        ExecuteSubtractInstruction(frame, ip);
                        break;

                    case Opcode.Power:
                        ExecuteBinaryInstruction(frame, instruction.Opcode, ip);
                        break;

                    case Opcode.IntegralDivide:
                        ExecuteIntegralDivideInstruction(frame, ip);
                        break;

                    case Opcode.Eqv:
                    case Opcode.Imp:
                        ExecuteIntegerLogicalInstruction(frame, instruction.Opcode, ip);
                        break;

                    case Opcode.Divide:
                        ExecuteDivideInstruction(frame, ip);
                        break;

                    case Opcode.Modulo:
                        ExecuteModuloInstruction(frame, ip);
                        break;

                    case Opcode.And:
                        ExecuteAndInstruction(frame, ip);
                        break;

                    case Opcode.Or:
                        ExecuteOrInstruction(frame, ip);
                        break;

                    case Opcode.Xor:
                        ExecuteXorInstruction(frame, ip);
                        break;

                    case Opcode.ShiftLeft:
                        ExecuteShiftLeftInstruction(frame, ip);
                        break;

                    case Opcode.ShiftRight:
                        ExecuteShiftRightInstruction(frame, ip);
                        break;

                    case Opcode.CompareEqual:
                        ExecuteCompareEqualInstruction(frame);
                        break;

                    case Opcode.CompareNotEqual:
                        ExecuteCompareNotEqualInstruction(frame, ip);
                        break;

                    case Opcode.CompareLess:
                        ExecuteCompareLessInstruction(frame, ip);
                        break;

                    case Opcode.CompareLessOrEqual:
                        ExecuteCompareLessOrEqualInstruction(frame, ip);
                        break;

                    case Opcode.Multiply:
                        ExecuteMultiplyInstruction(frame, ip);
                        break;

                    case Opcode.CompareGreater:
                        ExecuteCompareGreaterInstruction(frame, ip);
                        break;

                    case Opcode.CompareGreaterOrEqual:
                        ExecuteCompareGreaterOrEqualInstruction(frame, ip);
                        break;

                    case Opcode.Not:
                        ExecuteNotInstruction(frame, ip);
                        break;

                    case Opcode.Convert:
                        ExecuteConvert(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfEqual:
                        ExecuteJumpIfEqualInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfNotEqual:
                        ExecuteJumpIfNotEqualInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfLess:
                        ExecuteJumpIfLessInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfLessOrEqual:
                        ExecuteJumpIfLessOrEqualInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfGreater:
                        ExecuteJumpIfGreaterInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfGreaterOrEqual:
                        ExecuteJumpIfGreaterOrEqualInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfForDoneLocal:
                        ExecuteJumpIfForDoneLocalInstruction(frame, instruction, ip);
                        break;

                    case Opcode.JumpIfTrue:
                        ExecuteJumpIfBooleanInstruction(frame, instruction, invertCondition: false);
                        break;

                    case Opcode.JumpIfFalse:
                        ExecuteJumpIfBooleanInstruction(frame, instruction, invertCondition: true);
                        break;

                    case Opcode.OpenFile:
                        ExecuteOpenFile(frame, instruction, ip);
                        break;

                    case Opcode.CloseFile:
                        ExecuteCloseFile(frame, ip);
                        break;

                    case Opcode.RedimArray:
                        ExecuteRedimArray(frame, instruction, ip);
                        break;

                    case Opcode.NewArray:
                        ExecuteNewArray(frame, instruction, ip);
                        break;

                    case Opcode.GetArrayStructuralVersion:
                        ExecuteGetArrayStructuralVersion(frame, ip);
                        break;

                    case Opcode.EraseArray:
                        ExecuteEraseArray(frame, ip);
                        break;

                    case Opcode.LoadElement:
                        ExecuteLoadElement(frame, instruction, ip);
                        break;

                    case Opcode.LoadElement1D:
                        ExecuteLoadElement1D(frame, instruction, ip);
                        break;

                    case Opcode.LoadElement2D:
                        ExecuteLoadElement2D(frame, instruction, ip);
                        break;

                    case Opcode.StoreElement:
                        ExecuteStoreElement(frame, instruction, ip);
                        break;

                    case Opcode.CreateRecord:
                        ExecuteCreateRecord(frame, instruction, ip);
                        if (instruction.IsFreshRecordLocal)
                        {
                            // Точка отмены и исходный индекс второй инструкции
                            // остаются между созданием и записью в локальный слот.
                            ip++;
                            if (CheckCancellationRequested())
                                return;
                            frame.InstructionPointer = ip + 1;
                            var fresh = PopValue(frame, ip);
                            if (!frame.TryAdoptFreshFlatRecord(instruction.B, fresh))
                                frame.StoreLocalValue(instruction.B, fresh);
                        }
                        break;

                    case Opcode.LoadFieldOrdinal:
                    case Opcode.LoadField:
                        ExecuteLoadField(frame, instruction, ip);
                        break;

                    case Opcode.StoreFieldOrdinal:
                    case Opcode.StoreField:
                        ExecuteStoreField(frame, instruction, ip);
                        break;

                    case Opcode.ValidateTypeInvariant:
                        ValidateTypeInvariant(frame, instruction, ip);
                        break;

                    case Opcode.CreateDictionary:
                        ExecuteCreateDictionary(frame, instruction, ip);
                        break;

                    case Opcode.LoadDictionaryValue:
                        ExecuteLoadDictionaryValue(frame, instruction, ip);
                        break;

                    case Opcode.StoreDictionaryValue:
                        ExecuteStoreDictionaryValue(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryAdd:
                        ExecuteDictionaryAdd(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryExists:
                        ExecuteDictionaryExists(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryRemove:
                        ExecuteDictionaryRemove(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryKeys:
                        ExecuteDictionaryKeys(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryCount:
                        ExecuteDictionaryCount(frame, instruction, ip);
                        break;

                    case Opcode.DictionaryTryGetValue:
                        ExecuteDictionaryTryGetValue(frame, instruction, ip);
                        break;

                    case Opcode.CreateSet:
                        ExecuteCreateSet(frame, instruction, ip);
                        break;

                    case Opcode.SetAdd:
                        ExecuteSetAdd(frame, instruction, ip);
                        break;

                    case Opcode.SetContains:
                        ExecuteSetContains(frame, instruction, ip);
                        break;

                    case Opcode.SetRemove:
                        ExecuteSetRemove(frame, instruction, ip);
                        break;

                    case Opcode.SetCount:
                        ExecuteSetCount(frame, instruction, ip);
                        break;

                    case Opcode.SetToArray:
                        ExecuteSetToArray(frame, instruction, ip);
                        break;

                    case Opcode.SetUnionWith:
                        ExecuteSetUnionWith(frame, instruction, ip);
                        break;

                    case Opcode.SetIntersectWith:
                        ExecuteSetIntersectWith(frame, instruction, ip);
                        break;

                    case Opcode.ValidateForStep:
                        ValidateForStep(frame, ip);
                        break;

                    case Opcode.AdvanceForLoopLocal:
                        ExecuteAdvanceForLoopLocalInstruction(frame, instruction, ip);
                        break;

                    case Opcode.StepForLoopLocal:
                        ExecuteStepForLoopLocal(frame, instruction, ip);
                        break;

                    case Opcode.PrintValue:
                        AppendPendingOutput(PopValue(frame, ip));
                        break;

                    case Opcode.PrintUsing:
                        ExecutePrintUsing(frame, instruction, ip);
                        break;

                    case Opcode.PrintSeparator:
                        ExecutePrintSeparator(instruction.A, ip);
                        break;

                    case Opcode.PrintLine:
                        FlushPendingOutput(forceEmptyLine: true);
                        break;

                    case Opcode.Input:
                        ExecuteInput(frame, instruction, ip);
                        break;

                    case Opcode.Sleep:
                        ExecuteSleepInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.ReseedRandom:
                        ExecuteReseedRandomInstruction(frame, instruction, ip);
                        break;

                    case Opcode.FormatInterpolatedPart:
                        ExecuteFormatInterpolatedPart(frame, ip);
                        break;

                    case Opcode.Screen:
                        ExecuteScreenInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Canvas:
                        ExecuteCanvasInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Smooth:
                        _host.SetGraphicsSmoothing(instruction.A != 0);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.BeginFrame:
                        ExecuteBeginFrameInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Present:
                        ExecutePresentInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Camera:
                        ExecuteCameraInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Viewport:
                        ExecuteViewportInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.ResetCamera:
                        ExecuteResetCameraInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.View:
                        ExecuteViewInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.ViewPrint:
                        ExecuteViewPrintInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Window:
                        ExecuteWindowInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Cls:
                        ExecuteClsInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Color:
                        ExecuteColorInstruction(frame, instruction, ip);
                        break;

                    case Opcode.Locate:
                        ExecuteLocateInstruction(frame, ip);
                        break;

                    case Opcode.Palette:
                        ExecutePaletteInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.PCopy:
                        ExecutePCopyInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.PSet:
                        ExecutePSetInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Get:
                        ExecuteGetInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Put:
                        ExecutePutInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Line:
                        ExecuteLineInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Circle:
                        ExecuteCircleInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Paint:
                        ExecutePaintInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.Draw:
                        ExecuteDrawInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawRect:
                        ExecuteDrawRectInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.FillRect:
                        ExecuteFillRectInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawLine:
                        ExecuteDrawLineInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawCircle:
                        ExecuteDrawCircleInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.FillCircle:
                        ExecuteFillCircleInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawImage:
                        ExecuteDrawImageInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawSprite:
                        ExecuteDrawSpriteInstruction(frame, instruction, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawTile:
                        ExecuteDrawTileInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawTilemap:
                        ExecuteDrawTilemapInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawTileLayer:
                        ExecuteDrawTileLayerInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.SetMapTile:
                        ExecuteSetMapTileInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.DrawText:
                        ExecuteDrawTextInstruction(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.StatementBoundary:
                        ExecuteStatementBoundary(ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.RegisterKeyHandler:
                        ExecuteRegisterKeyHandler(frame, ip, instruction);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.UnregisterKeyHandler:
                        ExecuteUnregisterKeyHandler(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.PauseKeyHandler:
                        ExecutePauseKeyHandler(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.ResumeKeyHandler:
                        ExecuteResumeKeyHandler(frame, ip);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.RegisterMouseHandler:
                        ExecuteRegisterMouseHandler(frame, ip, instruction);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.UnregisterMouseHandler:
                        ExecuteUnregisterMouseHandler(frame, ip, instruction);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.PauseMouseHandler:
                        ExecutePauseMouseHandler(frame, ip, instruction);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.ResumeMouseHandler:
                        ExecuteResumeMouseHandler(frame, ip, instruction);
                        InvalidateReverseExecutionHistory();
                        break;

                    case Opcode.PushErrorHandler:
                        ExecutePushErrorHandler(frame, instruction);
                        break;

                    case Opcode.PopErrorHandler:
                        _ = frame.TryPopErrorHandler(out _);
                        break;

                    case Opcode.RaiseError:
                        ExecuteRaiseError(frame, instruction, ip);
                        break;

                    default:
                        ReportInvalidRuntimeState(
                            $"Bytecode VM пока не поддерживает opcode '{instruction.Opcode}'.",
                            ResolveLocation(frame.Routine.Id, ip));
                        throw new RuntimeFailureException();
                    }
                }

                ip = frame.InstructionPointer;
            }
        }
        catch (ArrayAllocationFailureException ex)
        {
            HandleArrayRuntimeFailure(ex, frame, ip, 0);
        }
        catch (SetEntryLimitException ex)
        {
            HandleSetRuntimeFailure(ex, frame, ip, 0);
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
            HandleStringResourceFailure(ex.Message, frame, ip, 0);
        }
        catch (OutOfMemoryException)
        {
            HandleStringResourceFailure("Недостаточно памяти для результата операции.", frame, ip, 0);
        }
        catch (ExpiredArrayViewException ex)
        {
            HandleArrayRuntimeFailure(ex, frame, ip, 0);
        }
        catch (EvaluationStackUnderflowException ex)
        {
            ReportInvalidRuntimeState(ex.Message, ResolveLocation(frame.Routine.Id, ip));
            throw new RuntimeFailureException();
        }
        catch (VmCallbackSuspension suspension)
        {
            PushSuspendedCallback(suspension);
            return;
        }
        // Повреждённый указатель — ошибка VM, а не исключение программы:
        // отладочный путь тоже проверяет его вне пользовательского CATCH.
        catch (RuntimeFailureException) when ((uint)ip < (uint)instructionCount && TryHandleRuntimeFailure(0))
        {
            return;
        }
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

    private static VmInstruction[] DecodeInstructions(BytecodeInstruction[] source, bool fuse)
    {
        var decoded = new VmInstruction[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            decoded[index] = new VmInstruction(source[index]);
        }

        if (!fuse)
        {
            return decoded;
        }

        // Созданная запись принадлежит стеку до следующего StoreLocal. Второй
        // слот остаётся нетронутым на случай прямого перехода к нему.
        for (var index = 0; index + 1 < source.Length; index++)
        {
            if (source[index].Opcode == Opcode.CreateRecord && source[index].OperandCount == 1
                && source[index + 1].Opcode == Opcode.StoreLocal && source[index + 1].OperandCount == 1)
            {
                decoded[index] = VmInstruction.CreateFreshRecordLocal(source[index].GetOperand(0), source[index + 1].GetOperand(0));
                index++;
            }
        }

        // Слияние тройки управления FOR (AdvanceForLoopLocal → Jump → JumpIfForDoneLocal)
        // в одну суперинструкцию: декодированный поток внутренний, публичный байт-код
        // не меняется. Проверка на верхушке цикла остаётся для первого входа; слитная
        // инструкция сама выполняет инкремент, проверку завершения и переход.
        for (var index = 0; index + 1 < decoded.Length; index++)
        {
            if (decoded[index].Opcode != Opcode.AdvanceForLoopLocal
                || decoded[index + 1].Opcode != Opcode.Jump)
            {
                continue;
            }

            var backTarget = decoded[index + 1].A;
            if (backTarget < 0 || backTarget >= decoded.Length
                || decoded[backTarget].Opcode != Opcode.JumpIfForDoneLocal)
            {
                continue;
            }

            var check = decoded[backTarget];
            if (check.A != decoded[index].A
                || check.C != decoded[index].B
                || backTarget + 1 >= decoded.Length)
            {
                continue;
            }

            decoded[index] = new VmInstruction(
                Opcode.StepForLoopLocal,
                check.A,
                check.B,
                check.C,
                backTarget + 1,
                check.D);
        }

        // Слияние пар тривиальных стековых операций (аналог суперинструкций
        // LOAD_FAST__LOAD_FAST и LOAD_FAST__LOAD_CONST CPython 3.11+): одна
        // диспетчеризация вместо двух. Замена выполняется на месте без сжатия
        // потока, поэтому адреса переходов не сдвигаются, а вторая инструкция
        // пары остаётся в слоте и остаётся корректной точкой прямого перехода.
        // Обход слева направо: слитая пара пропускает оба слота.
        for (var index = 0; index + 1 < decoded.Length; index++)
        {
            if (decoded[index].Opcode != Opcode.LoadLocal)
            {
                continue;
            }

            var nextOpcode = decoded[index + 1].Opcode;
            if (nextOpcode == Opcode.LoadLocal)
            {
                decoded[index] = new VmInstruction(
                    Opcode.LoadLocalPair,
                    decoded[index].A,
                    decoded[index + 1].A,
                    0,
                    0,
                    0);
                index++;
            }
            else if (nextOpcode == Opcode.PushConst)
            {
                decoded[index] = new VmInstruction(
                    Opcode.LoadLocalConst,
                    decoded[index].A,
                    decoded[index + 1].A,
                    0,
                    0,
                    0);
                index++;
            }
        }

        // Специализация формы чтения элемента 1D/2D без диапазонов: размерность
        // и маска срезов — статические операнды, адаптивность не требуется.
        // Все проверки границ, выделения массива и корректности значения
        // остаются в общем пути исполнения; отладочные VM получают общий
        // LoadElement (гейт fuse выше).
        for (var index = 0; index < decoded.Length; index++)
        {
            if (decoded[index].Opcode != Opcode.LoadElement
                || decoded[index].GetOperand(1) is not (1 or 2)
                || decoded[index].GetOperand(2) != 0)
            {
                continue;
            }

            decoded[index] = new VmInstruction(
                decoded[index].B == 1 ? Opcode.LoadElement1D : Opcode.LoadElement2D,
                decoded[index].A,
                decoded[index].B,
                decoded[index].C,
                0,
                0);
        }

        return decoded;
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
        if (_cachedFrames.Remove(routine.Id, out var cachedFrame)
            && cachedFrame.Arguments.Length == routine.ParameterCount)
        {
            // Собственные byval-ячейки кадра сбрасываются на новые значения;
            // иностранные BYREF-слоты подставляются без изменения и при выходе
            // кадра не возвращаются в пул, поскольку не помечены повторными.
            FillArgumentSlots(cachedFrame.Arguments, arguments, byRefArgumentSlots, byValValuesAreSnapshots);
            cachedFrame.Reset(cachedFrame.Arguments, captures, callSiteLocation, template.DefaultLocals);
            return cachedFrame;
        }

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

        VmValue[] locals;
        int?[] localFixedStringLengths;

        if (routine.LocalSlotCount == 0)
        {
            locals = Array.Empty<VmValue>();
            localFixedStringLengths = Array.Empty<int?>();
        }
        else
        {
            locals = (VmValue[])template.DefaultLocals.Clone();
            localFixedStringLengths = template.LocalFixedStringLengths;
        }

        return new BytecodeCallFrame(
            routine,
            argumentSlots,
            locals,
            localFixedStringLengths,
            captures,
            callSiteLocation,
            routine.MaxEvaluationStackDepth,
            _decodedInstructions[routine.Id]);
    }

    private static BytecodeVariableSlot[] CreateArgumentSlots(
        BytecodeRoutine routine,
        ReadOnlySpan<RuntimeValue> arguments,
        BytecodeVariableSlot[]? byRefArgumentSlots,
        bool byValValuesAreSnapshots)
    {
        if (routine.ParameterCount == 0)
        {
            return Array.Empty<BytecodeVariableSlot>();
        }

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
                ? BytecodeVariableSlot.AdoptReusable(value)
                : BytecodeVariableSlot.AdoptReusable(value.CopyForStorage());
            byValIndex++;
        }

        return argumentSlots;
    }

    // Заполняет массив ячеек кэшированного кадра на месте: собственные ячейки
    // byval-аргументов сбрасываются на новые значения, иностранные BYREF-слоты
    // подставляются без изменения. Ячейка без признака повторного использования
    // (глобальный accessor, локальная или помеченная вышедшей ячейка) не
    // сбрасывается и заменяется новой.
    private static void FillArgumentSlots(
        BytecodeVariableSlot[] argumentSlots,
        ReadOnlySpan<RuntimeValue> arguments,
        BytecodeVariableSlot[]? byRefArgumentSlots,
        bool byValValuesAreSnapshots)
    {
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
            if (!byValValuesAreSnapshots)
            {
                value = value.CopyForStorage();
            }

            var slot = argumentSlots[index];
            argumentSlots[index] = slot is not null && slot.TryResetValue(value)
                ? slot
                : BytecodeVariableSlot.AdoptReusable(value);
            byValIndex++;
        }
    }

    private void ExecuteCurrentInstruction(int runtimeFailureBoundaryDepth = 0)
    {
        var frame = CurrentFrame;
        var instructions = frame.DecodedInstructions;
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
        var stateBeforeInstruction = SelectReversibleSnapshot(canCheckStopFirst, shouldCaptureReversibleState, snapshot);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BytecodeVmStateSnapshot? SelectReversibleSnapshot(bool canCheckStopFirst, bool shouldCaptureReversibleState, BytecodeVmExecutionSnapshot? snapshot)
    {
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
        return canReusePrevious
            ? previousSnapshot
            : shouldCaptureReversibleState ? TryCreateReversibleStateSnapshot(snapshot!) : null;
    }

    private void ExecuteInstruction(BytecodeCallFrame frame, VmInstruction instruction, int instructionIndex)
    {
        switch (instruction.Opcode)
        {
            case Opcode.PushConst:
                frame.EvaluationStack.PushUnchecked(ReadConstantUnchecked(instruction.GetOperand(0)));
                break;

            case Opcode.PushIntegerZero:
                // Непосредственный ноль сохраняет распакованное представление
                // для последующих целочисленных операций и записи в слоты.
                frame.EvaluationStack.PushInteger(0);
                break;

            case Opcode.PushIntegerOne:
                frame.EvaluationStack.PushInteger(1);
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

            case Opcode.LoadLocalPair:
                // Отладочный путь исполняет ту же слитную форму одним шагом:
                // указатель пропускает второй слот пары (см. DecodeInstructions).
                frame.PushLocalValueOntoEvaluationStack(instruction.GetOperand(0));
                frame.PushLocalValueOntoEvaluationStack(instruction.GetOperand(1));
                frame.InstructionPointer = instructionIndex + 2;
                break;

            case Opcode.LoadLocalConst:
                frame.PushLocalValueOntoEvaluationStack(instruction.GetOperand(0));
                frame.EvaluationStack.PushUnchecked(ReadConstantUnchecked(instruction.GetOperand(1)));
                frame.InstructionPointer = instructionIndex + 2;
                break;

            case Opcode.StoreLocal:
                var localSlot = instruction.GetOperand(0);
                if (frame.EvaluationStack.TryGetImmediateInteger(out var immediateInteger)
                    && frame.TryStoreImmediateInteger(localSlot, immediateInteger))
                    frame.EvaluationStack.DiscardTop(1);
                else if (frame.EvaluationStack.TryGetImmediateLong(out var immediateLong)
                    && frame.TryStoreImmediateLong(localSlot, immediateLong))
                    frame.EvaluationStack.DiscardTop(1);
                else if (frame.EvaluationStack.TryGetImmediateDouble(out var immediateDouble)
                    && frame.TryStoreImmediateDouble(localSlot, immediateDouble))
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
                else if (argument.TryGetImmediateDouble(out var doubleArgumentValue))
                    frame.EvaluationStack.PushDouble(doubleArgumentValue);
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

            case Opcode.LoadElement1D:
                ExecuteLoadElement1D(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadElement2D:
                ExecuteLoadElement2D(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreElement:
                ExecuteStoreElement(frame, instruction, instructionIndex);
                break;

            case Opcode.CreateRecord:
                ExecuteCreateRecord(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadFieldOrdinal:
            case Opcode.LoadField:
                ExecuteLoadField(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreFieldOrdinal:
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

            case Opcode.StepForLoopLocal:
                ExecuteStepForLoopLocal(frame, instruction, instructionIndex);
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

    private void ExecutePrintUsing(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
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

    private void ValidateTypeInvariant(BytecodeCallFrame frame,  VmInstruction instruction, int instructionIndex)
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

    private IReadOnlyList<RuntimeCallChainFrame>? CreateConstructorChainFrames( VmInstruction instruction, TextLocation callSite)
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
        // В наблюдаемом и отладочном режимах сохраняем прежнее владение ячейками.
        frame.ReleaseReferences(keepReusableIntegerSlots: !NeedsExecutionSnapshot());
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
    private readonly record struct RoutineFrameTemplate(VmValue[] DefaultLocals, int?[] LocalFixedStringLengths);

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
