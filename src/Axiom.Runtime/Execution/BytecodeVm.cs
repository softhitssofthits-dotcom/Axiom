using System.Buffers;
using System.Globalization;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private const string DivisionByZeroCode = "MB3001";
    private const string ArrayBoundsCode = "MB3002";
    private const string InputConversionCode = "MB3003";
    private const string StepZeroCode = "MB3004";
    private const string InvalidRuntimeStateCode = "MB3005";
    private const string MissingReturnValueCode = "MB3006";
    private const string BuiltinArgumentCode = "MB3007";
    private const string NumericOverflowCode = "MB3008";
    private const string EmptyFunctionValueCode = "MB3009";
    private static readonly TextLocation FallbackRuntimeLocation = new(new TextSpan(0, 0), 1, 1);

    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<int, BytecodeRoutine> _routines;
    private readonly Dictionary<FunctionSymbol, BytecodeRoutine> _routinesBySymbol;
    private readonly Dictionary<int, BuiltinBinding> _builtins;
    private readonly Dictionary<FunctionSymbol, BuiltinBinding> _builtinsByFunction;
    private readonly Dictionary<int, BuiltinExecution> _builtinExecutions;
    private readonly Dictionary<int, BytecodeGlobal> _globalMetadata;
    private readonly Dictionary<(int RoutineId, int InstructionIndex), TextLocation> _sourceLocationCache = [];
    private readonly RuntimeValue?[] _constantValues;
    private readonly Dictionary<int, RuntimeValue> _globals = [];
    private readonly RuntimeValue?[] _globalsBySlot;
    private readonly int?[] _globalFixedStringLengths;
    private readonly bool[] _hasGlobalSlot;
    private readonly Stack<BytecodeCallFrame> _callStack = new();
    private readonly HashSet<RuntimeTextFile> _openFiles = [];
    private readonly StringBuilder _pendingOutput = new();
    private readonly Random _random = new();
    private readonly IRuntimeHost _host;
    private readonly BytecodeModule _module;
    private readonly Action<BytecodeVmExecutionSnapshot>? _instructionObserver;
    private readonly Func<BytecodeVmExecutionSnapshot, bool>? _shouldStopBeforeInstruction;
    private readonly BytecodeBreakpointSet? _breakpoints;
    private BytecodeVmStopState? _stopState;
    private BytecodeVmExecutionSnapshot? _skipStopOnceAt;
    private BytecodeVmStateSnapshot? _previousReversibleStateSnapshot;
    private int? _suppressLineBreakpointsUntilDifferentLine;
    private DebugStepBoundary? _debugStepBoundary;
    private bool _hasPendingOutput;

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
        _instructionObserver = instructionObserver;
        _shouldStopBeforeInstruction = shouldStopBeforeInstruction;
        _breakpoints = breakpoints;
        _routines = module.Routines.ToDictionary(static routine => routine.Id);
        _routinesBySymbol = module.Routines
            .Where(static routine => routine.Symbol is not null)
            .ToDictionary(static routine => routine.Symbol!);
        _builtins = module.Builtins.ToDictionary(static builtin => builtin.Id);
        _builtinsByFunction = module.Builtins
            .Where(static builtin => builtin.Function is not null)
            .ToDictionary(static builtin => builtin.Function!);
        _builtinExecutions = module.Builtins.ToDictionary(
            static builtin => builtin.Id,
            static builtin => CreateBuiltinExecution(builtin));
        _globalMetadata = module.Globals.ToDictionary(static global => global.Slot);
        _constantValues = module.ConstantPool.Entries.Select(TryCreateRuntimeConstant).ToArray();
        var maxGlobalSlot = module.Globals.Count == 0 ? -1 : module.Globals.Max(static global => global.Slot);
        _globalsBySlot = maxGlobalSlot >= 0 ? new RuntimeValue[maxGlobalSlot + 1] : Array.Empty<RuntimeValue>();
        _globalFixedStringLengths = maxGlobalSlot >= 0 ? new int?[maxGlobalSlot + 1] : Array.Empty<int?>();
        _hasGlobalSlot = maxGlobalSlot >= 0 ? new bool[maxGlobalSlot + 1] : Array.Empty<bool>();

        foreach (var global in module.Globals)
        {
            var initialValue = RuntimeValue.Default(global.Type, global.FixedStringLength);
            _globals[global.Slot] = initialValue;
            _globalsBySlot[global.Slot] = initialValue;
            _globalFixedStringLengths[global.Slot] = global.FixedStringLength;
            _hasGlobalSlot[global.Slot] = true;
        }
    }

    public static RuntimeResult Execute(
        BytecodeModule module,
        IRuntimeHost? host = null,
        Action<BytecodeVmExecutionSnapshot>? instructionObserver = null,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction = null,
        BytecodeBreakpointSet? breakpoints = null)
    {
        var vm = new BytecodeVm(
            module,
            host ?? NullRuntimeHost.Instance,
            instructionObserver,
            shouldStopBeforeInstruction,
            breakpoints);

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

        return new RuntimeResult(vm._diagnostics, vm._stopState, vm.GetPendingOutputPreview());
    }

    private BytecodeCallFrame CurrentFrame => _callStack.Peek();

    private void ExecuteModule()
    {
        if (!_routines.TryGetValue(_module.EntryPointRoutineId, out var entryPoint))
        {
            ReportInvalidRuntimeState(
                $"Не найдена точка входа VM с routine-id {_module.EntryPointRoutineId}.",
                FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        _callStack.Push(CreateFrame(entryPoint, Array.Empty<RuntimeValue>(), Array.Empty<BytecodeVariableSlot>(), callSiteLocation: entryPoint.Location));

        while (_callStack.Count > 0 && _stopState is null)
        {
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
        return FindStepBackTargetSnapshot(stopState.Snapshot, stopState.PreviousStateSnapshot) is not null;
    }

    private RuntimeResult ResumeFrom(BytecodeVmStopState stopState, DebugStepMode stepMode)
    {
        ArgumentNullException.ThrowIfNull(stopState);

        if (!ReferenceEquals(_stopState, stopState))
        {
            throw new InvalidOperationException("Stopped bytecode VM state does not belong to the current suspended run.");
        }

        _skipStopOnceAt = stopState.Snapshot;
        _suppressLineBreakpointsUntilDifferentLine = stopState.Snapshot.SourceLocation.Line;
        _debugStepBoundary = stepMode == DebugStepMode.None
            ? null
            : new DebugStepBoundary(stepMode, stopState.Snapshot.SourceLocation.Line, stopState.Snapshot.CallDepth);
        _stopState = null;

        try
        {
            while (_callStack.Count > 0 && _stopState is null)
            {
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

        return new RuntimeResult(_diagnostics, _stopState, GetPendingOutputPreview());
    }

    private static BytecodeVmStateSnapshot? FindStepBackTargetSnapshot(
        BytecodeVmExecutionSnapshot currentSnapshot,
        BytecodeVmStateSnapshot? candidate)
    {
        while (candidate is not null
            && candidate.ExecutionSnapshot.SourceLocation.Line == currentSnapshot.SourceLocation.Line)
        {
            candidate = candidate.PreviousStateSnapshot;
        }

        return candidate;
    }

    private static bool IsBuiltinReverseExecutionSafe(BuiltinFunctionId builtinId)
    {
        return builtinId switch
        {
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
            BuiltinFunctionId.Timer => false,
            BuiltinFunctionId.FileExists => false,
            BuiltinFunctionId.DirExists => false,
            BuiltinFunctionId.PathExists => false,
            BuiltinFunctionId.ReadFile => false,
            BuiltinFunctionId.ReadAllLines => false,
            BuiltinFunctionId.WriteFile => false,
            BuiltinFunctionId.AppendFile => false,
            BuiltinFunctionId.FileSize => false,
            BuiltinFunctionId.FileDate => false,
            BuiltinFunctionId.FileCreateDate => false,
            BuiltinFunctionId.FileAccessDate => false,
            BuiltinFunctionId.DirDate => false,
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
        TextLocation callSiteLocation)
    {
        var argumentSlots = new BytecodeVariableSlot[routine.ParameterCount];
        for (var index = 0; index < argumentSlots.Length; index++)
        {
            var value = index < arguments.Length ? arguments[index].CopyForStorage() : RuntimeValue.Void;
            argumentSlots[index] = new BytecodeVariableSlot(value);
        }

        var locals = new RuntimeValue[routine.LocalSlotCount];
        var localFixedStringLengths = new int?[routine.LocalSlotCount];
        for (var index = 0; index < locals.Length; index++)
        {
            var variable = index < routine.LocalVariables.Count ? routine.LocalVariables[index] : null;
            var initialValue = variable is null
                ? RuntimeValue.Void
                : RuntimeValue.Default(variable.Type, variable.FixedStringLength);
            locals[index] = initialValue;
            localFixedStringLengths[index] = variable?.FixedStringLength;
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

    private void ExecuteCurrentInstruction()
    {
        var frame = CurrentFrame;

        if (frame.InstructionPointer < 0 || frame.InstructionPointer >= frame.Routine.Instructions.Count)
        {
            ReportInvalidRuntimeState(
                $"Instruction pointer {frame.InstructionPointer} вышел за пределы процедуры '{frame.Routine.Name}'.",
                ResolveLocation(frame.Routine.Id, Math.Max(0, frame.Routine.Instructions.Count - 1)));
            throw new RuntimeFailureException();
        }

        var instructionIndex = frame.InstructionPointer;
        var instruction = frame.Routine.Instructions[instructionIndex];
        var needsExecutionSnapshot = NeedsExecutionSnapshot();
        var snapshot = needsExecutionSnapshot
            ? CreateExecutionSnapshot(frame, instructionIndex, instruction)
            : null;
        var shouldCaptureReversibleState = ShouldCaptureReversibleStateHistory();
        var stateBeforeInstruction = shouldCaptureReversibleState
            ? TryCreateReversibleStateSnapshot(snapshot!)
            : null;
        if (snapshot is not null)
        {
            _instructionObserver?.Invoke(snapshot);
        }

        if (snapshot is not null && ShouldStopBefore(snapshot))
        {
            _stopState = new BytecodeVmStopState(snapshot, this, _previousReversibleStateSnapshot);
            return;
        }

        _previousReversibleStateSnapshot = stateBeforeInstruction;
        frame.InstructionPointer++;

        switch (instruction.Opcode)
        {
            case Opcode.PushConst:
                frame.EvaluationStack.Push(ReadConstantUnchecked(instruction.GetOperand(0)));
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

            case Opcode.LoadLocal:
                frame.EvaluationStack.Push(frame.LoadLocalValue(instruction.GetOperand(0)));
                break;

            case Opcode.StoreLocal:
                frame.StoreLocalValue(instruction.GetOperand(0), PopValue(frame, instructionIndex));
                break;

            case Opcode.LoadArgument:
                frame.EvaluationStack.Push(frame.Arguments[instruction.GetOperand(0)].Value);
                break;

            case Opcode.StoreArgument:
                frame.Arguments[instruction.GetOperand(0)].Store(PopValue(frame, instructionIndex));
                break;

            case Opcode.LoadCapture:
                frame.EvaluationStack.Push(frame.Captures[instruction.GetOperand(0)].Value);
                break;

            case Opcode.StoreCapture:
                frame.Captures[instruction.GetOperand(0)].Store(PopValue(frame, instructionIndex));
                break;

            case Opcode.Pop:
                _ = PopValue(frame, instructionIndex);
                break;

            case Opcode.Negate:
                frame.EvaluationStack.Push(EvaluateNegate(PopValue(frame, instructionIndex), ResolveLocation(frame.Routine.Id, instructionIndex)));
                break;

            case Opcode.Add:
                ExecuteAddInstruction(frame, instructionIndex);
                break;

            case Opcode.AddLongInteger:
                ExecuteAddLongIntegerInstruction(frame, instructionIndex);
                break;

            case Opcode.Subtract:
            case Opcode.Power:
            case Opcode.Divide:
            case Opcode.IntegralDivide:
            case Opcode.Modulo:
            case Opcode.And:
            case Opcode.Or:
            case Opcode.Xor:
            case Opcode.Eqv:
            case Opcode.Imp:
            case Opcode.ShiftLeft:
            case Opcode.ShiftRight:
            case Opcode.CompareEqual:
            case Opcode.CompareNotEqual:
            case Opcode.CompareLess:
            case Opcode.CompareLessOrEqual:
            case Opcode.CompareGreaterOrEqual:
                ExecuteBinaryInstruction(frame, instruction.Opcode, instructionIndex);
                break;

            case Opcode.Multiply:
                ExecuteMultiplyInstruction(frame, instructionIndex);
                break;

            case Opcode.CompareGreater:
                ExecuteCompareGreaterInstruction(frame, instructionIndex);
                break;

            case Opcode.Not:
                frame.EvaluationStack.Push(EvaluateNot(PopValue(frame, instructionIndex), ResolveLocation(frame.Routine.Id, instructionIndex)));
                break;

            case Opcode.Convert:
                ExecuteConvert(frame, instruction, instructionIndex);
                break;

            case Opcode.Jump:
                frame.InstructionPointer = instruction.GetOperand(0);
                break;

            case Opcode.JumpIfLess:
                ExecuteComparisonJumpInstruction(frame, instruction, instructionIndex, ComparisonBranchKind.Less);
                break;

            case Opcode.JumpIfLessOrEqual:
                ExecuteComparisonJumpInstruction(frame, instruction, instructionIndex, ComparisonBranchKind.LessOrEqual);
                break;

            case Opcode.JumpIfGreater:
                ExecuteComparisonJumpInstruction(frame, instruction, instructionIndex, ComparisonBranchKind.Greater);
                break;

            case Opcode.JumpIfGreaterOrEqual:
                ExecuteComparisonJumpInstruction(frame, instruction, instructionIndex, ComparisonBranchKind.GreaterOrEqual);
                break;

            case Opcode.JumpIfTrue:
                if (PopValue(frame, instructionIndex).AsBoolean())
                {
                    frame.InstructionPointer = instruction.GetOperand(0);
                }

                break;

            case Opcode.JumpIfFalse:
                if (!PopValue(frame, instructionIndex).AsBoolean())
                {
                    frame.InstructionPointer = instruction.GetOperand(0);
                }

                break;

            case Opcode.Call:
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

            case Opcode.EraseArray:
                ExecuteEraseArray(frame, instructionIndex);
                break;

            case Opcode.LoadElement:
                ExecuteLoadElement(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreElement:
                ExecuteStoreElement(frame, instruction, instructionIndex);
                break;

            case Opcode.LoadField:
                ExecuteLoadField(frame, instruction, instructionIndex);
                break;

            case Opcode.StoreField:
                ExecuteStoreField(frame, instruction, instructionIndex);
                break;

            case Opcode.ValidateForStep:
                ValidateForStep(frame, instructionIndex);
                break;

            case Opcode.PrintValue:
                AppendPendingOutput(PopValue(frame, instructionIndex).ToDisplayString());
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

    private void ExecuteBinaryInstruction(BytecodeCallFrame frame, Opcode opcode, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();

        if (TryEvaluateFastBinaryInstruction(opcode, left, right, out var fastResult))
        {
            stack.Push(fastResult);
            return;
        }

        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        stack.Push(opcode switch
        {
            Opcode.Add => EvaluateAdd(left, right, location),
            Opcode.Subtract => EvaluateSubtract(left, right, location),
            Opcode.Multiply => EvaluateMultiply(left, right, location),
            Opcode.Power => EvaluatePower(left, right, location),
            Opcode.Divide => EvaluateDivide(left, right, location),
            Opcode.IntegralDivide => EvaluateIntegralDivide(left, right, location),
            Opcode.Modulo => EvaluateModulo(left, right, location),
            Opcode.And => EvaluateAnd(left, right, location),
            Opcode.Or => EvaluateOr(left, right, location),
            Opcode.Xor => EvaluateXor(left, right, location),
            Opcode.Eqv => EvaluateEqv(left, right, location),
            Opcode.Imp => EvaluateImp(left, right, location),
            Opcode.ShiftLeft => EvaluateShiftLeft(left, right, location),
            Opcode.ShiftRight => EvaluateShiftRight(left, right, location),
            Opcode.CompareEqual => RuntimeValue.FromBoolean(ValuesEqual(left, right)),
            Opcode.CompareNotEqual => RuntimeValue.FromBoolean(!ValuesEqual(left, right)),
            Opcode.CompareLess => RuntimeValue.FromBoolean(Compare(left, right, location) < 0),
            Opcode.CompareLessOrEqual => RuntimeValue.FromBoolean(Compare(left, right, location) <= 0),
            Opcode.CompareGreater => RuntimeValue.FromBoolean(Compare(left, right, location) > 0),
            Opcode.CompareGreaterOrEqual => RuntimeValue.FromBoolean(Compare(left, right, location) >= 0),
            _ => throw new InvalidOperationException($"Unsupported binary opcode '{opcode}'.")
        });
    }

    private void ExecuteAddInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();

        if (TryEvaluateHotAdd(left, right, out var result))
        {
            stack.Push(result);
            return;
        }

        stack.Push(EvaluateAdd(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteAddLongIntegerInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();

        if (left.Type == TypeSymbol.Long && right.Type == TypeSymbol.Integer)
        {
            try
            {
                stack.Push(RuntimeValue.FromLong(checked(left.AsLong() + right.AsInteger())));
                return;
            }
            catch (OverflowException)
            {
            }
        }

        stack.Push(EvaluateAdd(
            ConvertValue(right, BytecodeScalarTypeCode.Long, ResolveLocation(frame.Routine.Id, instructionIndex)),
            left,
            ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteComparisonJumpInstruction(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        ComparisonBranchKind branchKind)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();
        var comparisonOpcode = branchKind switch
        {
            ComparisonBranchKind.Less => Opcode.CompareLess,
            ComparisonBranchKind.LessOrEqual => Opcode.CompareLessOrEqual,
            ComparisonBranchKind.Greater => Opcode.CompareGreater,
            ComparisonBranchKind.GreaterOrEqual => Opcode.CompareGreaterOrEqual,
            _ => throw new InvalidOperationException($"Unsupported comparison branch kind '{branchKind}'.")
        };

        if (TryEvaluateFastComparison(comparisonOpcode, left, right, out var result))
        {
            if (result.AsBoolean())
            {
                frame.InstructionPointer = instruction.GetOperand(0);
            }

            return;
        }

        var comparison = Compare(left, right, ResolveLocation(frame.Routine.Id, instructionIndex));
        var shouldJump = branchKind switch
        {
            ComparisonBranchKind.Less => comparison < 0,
            ComparisonBranchKind.LessOrEqual => comparison <= 0,
            ComparisonBranchKind.Greater => comparison > 0,
            ComparisonBranchKind.GreaterOrEqual => comparison >= 0,
            _ => false
        };

        if (shouldJump)
        {
            frame.InstructionPointer = instruction.GetOperand(0);
        }
    }

    private void ExecuteMultiplyInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();

        if (TryEvaluateHotMultiply(left, right, out var result))
        {
            stack.Push(result);
            return;
        }

        stack.Push(EvaluateMultiply(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteCompareGreaterInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();

        if (TryEvaluateHotCompareGreater(left, right, out var result))
        {
            stack.Push(result);
            return;
        }

        stack.Push(RuntimeValue.FromBoolean(
            Compare(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)) > 0));
    }

    private static bool TryEvaluateFastBinaryInstruction(
        Opcode opcode,
        RuntimeValue left,
        RuntimeValue right,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (left.Type != right.Type)
        {
            return false;
        }

        if (opcode is Opcode.CompareEqual or Opcode.CompareNotEqual)
        {
            var equal = ValuesEqual(left, right);
            result = RuntimeValue.FromBoolean(opcode == Opcode.CompareEqual ? equal : !equal);
            return true;
        }

        if (TryEvaluateFastComparison(opcode, left, right, out result))
        {
            return true;
        }

        if (left.Type == TypeSymbol.Boolean)
        {
            return TryEvaluateFastBooleanBinaryInstruction(opcode, left.AsBoolean(), right.AsBoolean(), out result);
        }

        if (left.Type == TypeSymbol.Integer)
        {
            return TryEvaluateFastIntegerBinaryInstruction(opcode, left.AsInteger(), right.AsInteger(), out result);
        }

        if (left.Type == TypeSymbol.Long)
        {
            return TryEvaluateFastLongBinaryInstruction(opcode, left.AsLong(), right.AsLong(), out result);
        }

        if (left.Type == TypeSymbol.String && opcode == Opcode.Add)
        {
            result = RuntimeValue.FromString(left.AsString() + right.AsString());
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotAdd(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(left.AsInteger() + right.AsInteger()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(left.AsLong() + right.AsLong()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Double)
        {
            result = TryCreateFiniteDouble(left.AsDouble() + right.AsDouble());
            return result != RuntimeValue.Void;
        }

        if (type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(left.AsDecimal() + right.AsDecimal()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.String)
        {
            result = RuntimeValue.FromString(left.AsString() + right.AsString());
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotMultiply(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(left.AsInteger() * right.AsInteger()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(left.AsLong() * right.AsLong()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Double)
        {
            result = TryCreateFiniteDouble(left.AsDouble() * right.AsDouble());
            return result != RuntimeValue.Void;
        }

        if (type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(left.AsDecimal() * right.AsDecimal()));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryEvaluateHotCompareGreater(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (left.Type != right.Type)
        {
            return false;
        }

        result = left.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromBoolean(left.AsInteger() > right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromBoolean(left.AsLong() > right.AsLong()),
            var type when type == TypeSymbol.Double => RuntimeValue.FromBoolean(left.AsDouble() > right.AsDouble()),
            var type when type == TypeSymbol.Decimal => RuntimeValue.FromBoolean(left.AsDecimal() > right.AsDecimal()),
            var type when type == TypeSymbol.String => RuntimeValue.FromBoolean(
                string.Compare(left.AsString(), right.AsString(), StringComparison.Ordinal) > 0),
            _ => RuntimeValue.Void
        };

        return result != RuntimeValue.Void;
    }

    private static RuntimeValue TryCreateFiniteDouble(double value) =>
        double.IsFinite(value)
            ? RuntimeValue.FromDouble(value)
            : RuntimeValue.Void;

    private static bool TryEvaluateFastBooleanBinaryInstruction(
        Opcode opcode,
        bool left,
        bool right,
        out RuntimeValue result)
    {
        result = opcode switch
        {
            Opcode.And => RuntimeValue.FromBoolean(left && right),
            Opcode.Or => RuntimeValue.FromBoolean(left || right),
            Opcode.Xor => RuntimeValue.FromBoolean(left ^ right),
            Opcode.Eqv => RuntimeValue.FromBoolean(left == right),
            Opcode.Imp => RuntimeValue.FromBoolean(!left || right),
            _ => RuntimeValue.Void
        };

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastIntegerBinaryInstruction(
        Opcode opcode,
        int left,
        int right,
        out RuntimeValue result)
    {
        try
        {
            result = opcode switch
            {
                Opcode.Add => RuntimeValue.FromInteger(checked(left + right)),
                Opcode.Subtract => RuntimeValue.FromInteger(checked(left - right)),
                Opcode.Multiply => RuntimeValue.FromInteger(checked(left * right)),
                Opcode.And => RuntimeValue.FromInteger(left & right),
                Opcode.Or => RuntimeValue.FromInteger(left | right),
                Opcode.Xor => RuntimeValue.FromInteger(left ^ right),
                Opcode.Eqv => RuntimeValue.FromInteger(~(left ^ right)),
                Opcode.Imp => RuntimeValue.FromInteger((~left) | right),
                Opcode.ShiftLeft => RuntimeValue.FromInteger(left << right),
                Opcode.ShiftRight => RuntimeValue.FromInteger(left >> right),
                _ => RuntimeValue.Void
            };
        }
        catch (OverflowException)
        {
            result = RuntimeValue.Void;
            return false;
        }

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastLongBinaryInstruction(
        Opcode opcode,
        long left,
        long right,
        out RuntimeValue result)
    {
        try
        {
            result = opcode switch
            {
                Opcode.Add => RuntimeValue.FromLong(checked(left + right)),
                Opcode.Subtract => RuntimeValue.FromLong(checked(left - right)),
                Opcode.Multiply => RuntimeValue.FromLong(checked(left * right)),
                Opcode.And => RuntimeValue.FromLong(left & right),
                Opcode.Or => RuntimeValue.FromLong(left | right),
                Opcode.Xor => RuntimeValue.FromLong(left ^ right),
                Opcode.Eqv => RuntimeValue.FromLong(~(left ^ right)),
                Opcode.Imp => RuntimeValue.FromLong((~left) | right),
                Opcode.ShiftLeft => RuntimeValue.FromLong(left << unchecked((int)right)),
                Opcode.ShiftRight => RuntimeValue.FromLong(left >> unchecked((int)right)),
                _ => RuntimeValue.Void
            };
        }
        catch (OverflowException)
        {
            result = RuntimeValue.Void;
            return false;
        }

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastComparison(
        Opcode opcode,
        RuntimeValue left,
        RuntimeValue right,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (opcode is not (Opcode.CompareLess or Opcode.CompareLessOrEqual or Opcode.CompareGreater or Opcode.CompareGreaterOrEqual))
        {
            return false;
        }

        var comparison = left.Type switch
        {
            var type when type == TypeSymbol.Integer => left.AsInteger().CompareTo(right.AsInteger()),
            var type when type == TypeSymbol.Long => left.AsLong().CompareTo(right.AsLong()),
            var type when type == TypeSymbol.Double => left.AsDouble().CompareTo(right.AsDouble()),
            var type when type == TypeSymbol.Decimal => left.AsDecimal().CompareTo(right.AsDecimal()),
            var type when type == TypeSymbol.String => string.Compare(left.AsString(), right.AsString(), StringComparison.Ordinal),
            _ => (int?)null
        };

        if (comparison is null)
        {
            return false;
        }

        result = RuntimeValue.FromBoolean(opcode switch
        {
            Opcode.CompareLess => comparison < 0,
            Opcode.CompareLessOrEqual => comparison <= 0,
            Opcode.CompareGreater => comparison > 0,
            Opcode.CompareGreaterOrEqual => comparison >= 0,
            _ => throw new InvalidOperationException($"Unsupported comparison opcode '{opcode}'.")
        });
        return true;
    }

    private RuntimeValue PopValue(BytecodeCallFrame frame, int instructionIndex)
    {
        if (frame.EvaluationStack.Count == 0)
        {
            ReportInvalidRuntimeState(
                $"Стек вычисления опустел в процедуре '{frame.Routine.Name}'.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        return frame.EvaluationStack.Pop();
    }

    private RuntimeValue ReadConstant(int constantIndex)
    {
        if (constantIndex < 0 || constantIndex >= _constantValues.Length)
        {
            ReportInvalidRuntimeState($"Не найдена константа с индексом {constantIndex}.", FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        var value = _constantValues[constantIndex];
        if (value is not null)
        {
            return value;
        }

        var constant = _module.ConstantPool[constantIndex];
        return ReportInvalidRuntimeState<RuntimeValue>(
            $"Константа вида '{constant.Kind}' не поддерживается Bytecode VM.",
            FallbackRuntimeLocation);
    }

    private RuntimeValue ReadConstantUnchecked(int constantIndex)
    {
        return _constantValues[constantIndex]!;
    }

    private static RuntimeValue? TryCreateRuntimeConstant(BytecodeConstant constant)
    {
        return constant.Kind switch
        {
            BytecodeConstantKind.Integer => RuntimeValue.FromInteger((int)constant.Value!),
            BytecodeConstantKind.Long => RuntimeValue.FromLong((long)constant.Value!),
            BytecodeConstantKind.Double => RuntimeValue.FromDouble((double)constant.Value!),
            BytecodeConstantKind.Decimal => RuntimeValue.FromDecimal((decimal)constant.Value!),
            BytecodeConstantKind.String => RuntimeValue.FromString((string)constant.Value!),
            BytecodeConstantKind.Boolean => RuntimeValue.FromBoolean((bool)constant.Value!),
            BytecodeConstantKind.Null => RuntimeValue.Void,
            _ => null
        };
    }

    private string ReadDebugName(int constantIndex)
    {
        if (constantIndex < 0 || constantIndex >= _module.ConstantPool.Count)
        {
            return "<array>";
        }

        var constant = _module.ConstantPool[constantIndex];
        return constant.Kind == BytecodeConstantKind.String
            ? (string)constant.Value!
            : "<array>";
    }

    private RuntimeValue LoadGlobal(int slot)
    {
        if ((uint)slot < (uint)_globalsBySlot.Length && _hasGlobalSlot[slot])
        {
            return _globalsBySlot[slot]!;
        }

        return ReportInvalidRuntimeState<RuntimeValue>($"Не найден глобальный слот {slot}.", FallbackRuntimeLocation);
    }

    private RuntimeValue LoadGlobalUnchecked(int slot)
    {
        return _globalsBySlot[slot]!;
    }

    private RuntimeValue LoadFunctionValue(BytecodeCallFrame frame, int routineId, int instructionIndex)
    {
        if (!_routines.TryGetValue(routineId, out var routine) || routine.Symbol is null)
        {
            ReportInvalidRuntimeState(
                $"Не найдена процедура для значения функции с routine-id {routineId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        var captures = ResolveCapturedSlots(frame, routine, instructionIndex);
        return RuntimeValue.FromBytecodeFunction(routine.Symbol, captures);
    }

    private RuntimeValue LoadBuiltinFunctionValue(BytecodeCallFrame frame, int builtinId, int instructionIndex)
    {
        if (!_builtins.TryGetValue(builtinId, out var builtin) || builtin.Function is null)
        {
            ReportInvalidRuntimeState(
                $"Не найдена встроенная функция для значения функции с built-in-id {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromBytecodeBuiltinFunction(builtin.Function, builtin.Id);
    }

    private void StoreGlobal(int slot, RuntimeValue value)
    {
        if ((uint)slot >= (uint)_globalsBySlot.Length || !_hasGlobalSlot[slot])
        {
            ReportInvalidRuntimeState($"Не найден глобальный слот {slot}.", FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        var storedValue = value.CopyForStorage(GetGlobalFixedStringLength(slot));
        _globals[slot] = storedValue;
        _globalsBySlot[slot] = storedValue;
    }

    private void StoreGlobalUnchecked(int slot, RuntimeValue value)
    {
        var storedValue = value.CopyForStorage(_globalFixedStringLengths[slot]);
        _globals[slot] = storedValue;
        _globalsBySlot[slot] = storedValue;
    }

    private static RuntimeValue LoadLocal(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.LocalCount)
        {
            throw CreateInvalidSlotException(frame, "локальный", slot, instructionIndex);
        }

        return frame.LoadLocalValue(slot);
    }

    private static void StoreLocal(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.LocalCount)
        {
            throw CreateInvalidSlotException(frame, "локальный", slot, instructionIndex);
        }

        frame.StoreLocalValue(slot, value);
    }

    private static RuntimeValue LoadArgument(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Arguments.Length)
        {
            throw CreateInvalidSlotException(frame, "аргументный", slot, instructionIndex);
        }

        return frame.Arguments[slot].Value;
    }

    private static void StoreArgument(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Arguments.Length)
        {
            throw CreateInvalidSlotException(frame, "аргументный", slot, instructionIndex);
        }

        frame.Arguments[slot].Store(value);
    }

    private static RuntimeValue LoadCapture(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Captures.Length)
        {
            throw CreateInvalidSlotException(frame, "захваченному", slot, instructionIndex);
        }

        return frame.Captures[slot].Value;
    }

    private static void StoreCapture(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Captures.Length)
        {
            throw CreateInvalidSlotException(frame, "захваченному", slot, instructionIndex);
        }

        frame.Captures[slot].Store(value);
    }

    private int? GetGlobalFixedStringLength(int slot)
    {
        return (uint)slot < (uint)_globalFixedStringLengths.Length && _hasGlobalSlot[slot]
            ? _globalFixedStringLengths[slot]
            : null;
    }

    private static InvalidOperationException CreateInvalidSlotException(
        BytecodeCallFrame frame,
        string kind,
        int slot,
        int instructionIndex)
    {
        return new InvalidOperationException(
            $"Процедура '{frame.Routine.Name}' обратилась к несуществующему {kind} слоту {slot} на инструкции {instructionIndex}.");
    }

    private void ExecuteRoutineCall(BytecodeCallFrame callerFrame, BytecodeInstruction instruction, int instructionIndex)
    {
        var targetRoutineId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);

        if (!_routines.TryGetValue(targetRoutineId, out var targetRoutine))
        {
            ReportInvalidRuntimeState(
                $"Не найдена вызываемая процедура с routine-id {targetRoutineId}.",
                ResolveLocation(callerFrame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        using var arguments = RentArguments(callerFrame, argumentCount, instructionIndex);
        var captures = ResolveCapturedSlots(callerFrame, targetRoutine, instructionIndex);
        _callStack.Push(CreateFrame(targetRoutine, arguments.Span, captures, ResolveLocation(callerFrame.Routine.Id, instructionIndex)));
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

        var result = InvokeBytecodeFunction(functionValue, arguments.Span, location);
        if (returnsValue)
        {
            frame.EvaluationStack.Push(result);
        }
    }

    private BytecodeVariableSlot[] ResolveCapturedSlots(BytecodeCallFrame callerFrame, BytecodeRoutine targetRoutine, int instructionIndex)
    {
        if (targetRoutine.CapturedVariables.Count == 0)
        {
            return Array.Empty<BytecodeVariableSlot>();
        }

        var captures = new BytecodeVariableSlot[targetRoutine.CapturedVariables.Count];

        for (var index = 0; index < targetRoutine.CapturedVariables.Count; index++)
        {
            var variable = targetRoutine.CapturedVariables[index];
            if (callerFrame.TryResolveVisibleVariable(variable, out var slot))
            {
                captures[index] = slot;
                continue;
            }

            ReportInvalidRuntimeState(
                $"Процедура '{callerFrame.Routine.Name}' не смогла передать захваченную переменную '{variable.Name}' в '{targetRoutine.Name}'.",
                ResolveLocation(callerFrame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        return captures;
    }

    private void ExecuteBuiltinCall(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var builtinId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);

        if (!_builtins.TryGetValue(builtinId, out var builtin))
        {
            ReportInvalidRuntimeState(
                $"Не найден built-in с идентификатором {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        var execution = _builtinExecutions[builtin.Id];
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var result = ExecuteBuiltin(frame, execution, argumentCount, instructionIndex, location);

        if (builtin.ReturnsValue)
        {
            frame.EvaluationStack.Push(result);
        }

        if (!IsBuiltinReverseExecutionSafe(builtin.CanonicalId))
        {
            InvalidateReverseExecutionHistory();
        }
    }

    private RuntimeValue ExecuteBuiltin(
        BytecodeCallFrame frame,
        BuiltinExecution execution,
        int argumentCount,
        int instructionIndex,
        TextLocation location)
    {
        switch (execution.Arity)
        {
            case BuiltinExecutionArity.Nullary:
                return execution.Zero!(this, location);

            case BuiltinExecutionArity.Unary:
                return execution.One!(this, PopValue(frame, instructionIndex), location);

            case BuiltinExecutionArity.Binary:
            {
                var right = PopValue(frame, instructionIndex);
                var left = PopValue(frame, instructionIndex);
                return execution.Two!(this, left, right, location);
            }

            case BuiltinExecutionArity.Ternary:
            {
                var third = PopValue(frame, instructionIndex);
                var second = PopValue(frame, instructionIndex);
                var first = PopValue(frame, instructionIndex);
                return execution.Three!(this, first, second, third, location);
            }

            default:
                using (var arguments = RentArguments(frame, argumentCount, instructionIndex))
                {
                    return execution.Many!(this, arguments.Span, location);
                }
        }
    }

    private RuntimeValue InvokeBytecodeFunction(
        RuntimeFunctionValue functionValue,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation callSiteLocation)
    {
        var function = functionValue.Function;

        if (function.IsBuiltIn)
        {
            if (function.BuiltinId is not { } builtinId)
            {
                return ReportInvalidRuntimeState<RuntimeValue>(
                    $"Встроенная функция '{function.Name}' не содержит канонического идентификатора.",
                    callSiteLocation);
            }

            var builtin = functionValue.BytecodeBuiltinBindingId is { } builtinBindingId && _builtins.TryGetValue(builtinBindingId, out var directBuiltin)
                ? directBuiltin
                : _builtinsByFunction.TryGetValue(function, out var mappedBuiltin)
                    ? mappedBuiltin
                    : _builtins.Values.FirstOrDefault(candidate => candidate.CanonicalId == builtinId);
            if (builtin is null)
            {
                return ReportInvalidRuntimeState<RuntimeValue>(
                    $"Для значения встроенной функции '{function.Name}' отсутствует запись в таблице built-in.",
                    callSiteLocation);
            }

            return ExecuteBuiltinFromArguments(builtin, arguments, callSiteLocation);
        }

        if (!_routinesBySymbol.TryGetValue(function, out var routine))
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для вызова '{function.Name}' отсутствует байткодная процедура.",
                callSiteLocation);
        }

        var captures = functionValue.BytecodeCapturedSlots switch
        {
            null => Array.Empty<BytecodeVariableSlot>(),
            BytecodeVariableSlot[] captureArray => captureArray,
            _ => functionValue.BytecodeCapturedSlots.ToArray()
        };
        var callerFrame = CurrentFrame;
        var callerStackDepth = callerFrame.EvaluationStack.Count;
        var callStackDepth = _callStack.Count;

        _callStack.Push(CreateFrame(routine, arguments, captures, callSiteLocation));

        while (_callStack.Count > callStackDepth)
        {
            ExecuteCurrentInstruction();
        }

        if (routine.ResultType == TypeSymbol.Void)
        {
            return RuntimeValue.Void;
        }

        if (callerFrame.EvaluationStack.Count <= callerStackDepth)
        {
            ReportRuntimeDiagnostic(
                MissingReturnValueCode,
                $"Функция '{routine.Name}' завершилась без значения RETURN.",
                callSiteLocation);
            throw new RuntimeFailureException();
        }

        return callerFrame.EvaluationStack.Pop();
    }

    private RuntimeValue ExecuteBuiltinFromArguments(
        BuiltinBinding builtin,
        ReadOnlySpan<RuntimeValue> arguments,
        TextLocation location)
    {
        if (!_builtinExecutions.TryGetValue(builtin.Id, out var execution))
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для built-in '{builtin.Name}' отсутствует исполняемый дескриптор Bytecode VM.",
                location);
        }

        return execution.Arity switch
        {
            BuiltinExecutionArity.Nullary => execution.Zero!(this, location),
            BuiltinExecutionArity.Unary => execution.One!(this, arguments[0], location),
            BuiltinExecutionArity.Binary => execution.Two!(this, arguments[0], arguments[1], location),
            BuiltinExecutionArity.Ternary => execution.Three!(this, arguments[0], arguments[1], arguments[2], location),
            _ => execution.Many!(this, arguments, location)
        };
    }

    private static PooledArgumentBuffer RentArguments(BytecodeCallFrame frame, int argumentCount, int instructionIndex)
    {
        if (argumentCount == 0)
        {
            return PooledArgumentBuffer.Empty;
        }

        var buffer = ArrayPool<RuntimeValue>.Shared.Rent(argumentCount);
        var stack = frame.EvaluationStack;

        for (var index = argumentCount - 1; index >= 0; index--)
        {
            if (stack.Count == 0)
            {
                ArrayPool<RuntimeValue>.Shared.Return(buffer, clearArray: true);
                throw new InvalidOperationException(
                    $"Стек вычисления опустел при снятии аргументов на инструкции {instructionIndex} процедуры '{frame.Routine.Name}'.");
            }

            buffer[index] = stack.Pop();
        }

        return new PooledArgumentBuffer(buffer, argumentCount, pooled: true);
    }

    private readonly struct PooledArgumentBuffer : IDisposable
    {
        public static PooledArgumentBuffer Empty { get; } = new(Array.Empty<RuntimeValue>(), 0, pooled: false);

        private readonly RuntimeValue[] _buffer;
        private readonly int _count;
        private readonly bool _pooled;

        public PooledArgumentBuffer(RuntimeValue[] buffer, int count, bool pooled)
        {
            _buffer = buffer;
            _count = count;
            _pooled = pooled;
        }

        public ReadOnlySpan<RuntimeValue> Span => _buffer.AsSpan(0, _count);

        public void Dispose()
        {
            if (_pooled)
            {
                ArrayPool<RuntimeValue>.Shared.Return(_buffer, clearArray: true);
            }
        }
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
        var values = new (RuntimeValue Value, TextLocation Location)[valueCount];
        var stack = frame.EvaluationStack;

        for (var index = valueCount - 1; index >= 0; index--)
        {
            values[index] = (stack.Pop(), location);
        }

        var format = stack.Pop().AsString();
        AppendPendingOutput(FormatUsing(format, values, location));

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
            StepZeroCode,
            "В цикле FOR недопустим STEP 0.",
            ResolveLocation(frame.Routine.Id, instructionIndex));
        throw new RuntimeFailureException();
    }

    private void ExecuteReturn(BytecodeCallFrame frame, int instructionIndex)
    {
        RuntimeValue returnValue = RuntimeValue.Void;

        if (frame.Routine.ResultType != TypeSymbol.Void)
        {
            if (frame.EvaluationStack.Count == 0)
            {
                ReportRuntimeDiagnostic(
                    MissingReturnValueCode,
                    $"Функция '{frame.Routine.Name}' завершилась без значения RETURN.",
                    ResolveLocation(frame.Routine.Id, instructionIndex));
                throw new RuntimeFailureException();
            }

            returnValue = frame.EvaluationStack.Pop().CopyForStorage();
        }

        _callStack.Pop();

        if (_callStack.Count == 0)
        {
            return;
        }

        if (frame.Routine.ResultType != TypeSymbol.Void)
        {
            CurrentFrame.EvaluationStack.Push(returnValue);
        }
    }

    private TextLocation ResolveCurrentLocation(int instructionIndex)
    {
        var frame = CurrentFrame;
        return ResolveLocation(frame.Routine.Id, instructionIndex);
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
            return true;
        }

        if (ShouldStopForDebugStep(snapshot))
        {
            _debugStepBoundary = null;
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
        return skip;
    }

    private bool ShouldStopForBreakpoint(BytecodeVmExecutionSnapshot snapshot)
    {
        if (_breakpoints is null)
        {
            return false;
        }

        if (_suppressLineBreakpointsUntilDifferentLine == snapshot.SourceLocation.Line)
        {
            return _breakpoints.ShouldStopBeforePositionBreakpoint(snapshot);
        }

        _suppressLineBreakpointsUntilDifferentLine = null;
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
            DebugStepMode.Into => snapshot.SourceLocation.Line != boundary.StartLine,
            DebugStepMode.Over => snapshot.CallDepth <= boundary.StartCallDepth
                && snapshot.SourceLocation.Line != boundary.StartLine,
            DebugStepMode.Out => snapshot.CallDepth < boundary.StartCallDepth,
            _ => false
        };
    }

    private void FinishRun()
    {
        if (_stopState is not null)
        {
            return;
        }

        _debugStepBoundary = null;
        CloseAllOpenFiles();
        FlushPendingOutput(forceEmptyLine: false);
    }

    private TextLocation ResolveLocation(int routineId, int instructionIndex)
    {
        var cacheKey = (routineId, instructionIndex);
        if (_sourceLocationCache.TryGetValue(cacheKey, out var cachedLocation))
        {
            return cachedLocation;
        }

        var resolvedLocation = FallbackRuntimeLocation;
        if (_module.DebugInfo is not null
            && _module.DebugInfo.TryGetLocation(routineId, instructionIndex, out var location))
        {
            resolvedLocation = location;
        }
        else if (_routines.TryGetValue(routineId, out var routine))
        {
            resolvedLocation = routine.Location;
        }

        _sourceLocationCache[cacheKey] = resolvedLocation;
        return resolvedLocation;
    }

    private void AppendPendingOutput(string text)
    {
        _hasPendingOutput = true;
        _pendingOutput.Append(text);
    }

    private void AppendPrintSeparator()
    {
        _hasPendingOutput = true;
        _pendingOutput.Append(' ');
    }

    private string GetPendingOutputPreview()
    {
        return _hasPendingOutput
            ? _pendingOutput.ToString()
            : string.Empty;
    }

    private void FlushPendingOutput(bool forceEmptyLine)
    {
        if (!_hasPendingOutput)
        {
            if (forceEmptyLine)
            {
                _host.WriteOutput(string.Empty);
                InvalidateReverseExecutionHistoryIfOutputIsNotReversible();
            }

            return;
        }

        _host.WriteOutput(_pendingOutput.ToString());
        _pendingOutput.Clear();
        _hasPendingOutput = false;
        InvalidateReverseExecutionHistoryIfOutputIsNotReversible();
    }

    private BytecodeVmStateSnapshot? TryCreateReversibleStateSnapshot(BytecodeVmExecutionSnapshot executionSnapshot)
    {
        if (_openFiles.Count > 0)
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

        _globals.Clear();
        Array.Clear(_globalsBySlot);
        foreach (var (slot, value) in snapshot.Globals)
        {
            var clonedValue = CloneSnapshotValue(value, GetGlobalFixedStringLength(slot), sharedValues);
            _globals[slot] = clonedValue;
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
    }

    private static BytecodeVariableSlot[] CreateSlotsFromSnapshots(
        IReadOnlyList<BytecodeVariableSlotSnapshot> snapshots,
        Dictionary<object, object> sharedValues)
    {
        var slots = new BytecodeVariableSlot[snapshots.Count];

        for (var index = 0; index < snapshots.Count; index++)
        {
            var snapshot = snapshots[index];
            slots[index] = new BytecodeVariableSlot(
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
            captures[index] = new BytecodeVariableSlot(
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

        if (value.Type is ArrayTypeSymbol arrayType)
        {
            var array = value.AsArray();
            if (!sharedValues.TryGetValue(array, out var clonedArray))
            {
                clonedArray = array.Clone(fixedStringLength);
                sharedValues[array] = clonedArray;
            }

            return RuntimeValue.FromArray(arrayType, (RuntimeArrayValue)clonedArray);
        }

        if (value.Type is UserDefinedTypeSymbol userType)
        {
            var record = value.AsRecord();
            if (!sharedValues.TryGetValue(record, out var clonedRecord))
            {
                clonedRecord = record.Clone();
                sharedValues[record] = clonedRecord;
            }

            return RuntimeValue.FromRecord(userType, (RuntimeRecordValue)clonedRecord);
        }

        return value.CopyForStorage(fixedStringLength);
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
            var value = frame.LoadLocalValue(index);
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
                clonedArray = array.Clone(fixedStringLength);
                sharedValues[array] = clonedArray;
            }

            snapshot = RuntimeValue.FromArray(arrayType, (RuntimeArrayValue)clonedArray);
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

    private RuntimeValue EvaluateNegate(RuntimeValue operand, TextLocation location)
    {
        return operand.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(-operand.AsInteger()),
                "Результат унарного '-'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(-operand.AsLong()),
                "Результат унарного '-'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                -operand.AsDouble(),
                "Результат унарного '-'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(-operand.AsDecimal()),
                "Результат унарного '-'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Унарный '-' не поддерживает тип '{operand.Type.Name}'.",
                location)
        };
    }

    private void ExecuteConvert(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var value = PopValue(frame, instructionIndex);
        var typeCode = (BytecodeScalarTypeCode)instruction.GetOperand(0);
        frame.EvaluationStack.Push(ConvertValue(value, typeCode, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private RuntimeValue ConvertValue(RuntimeValue value, BytecodeScalarTypeCode targetType, TextLocation location)
    {
        if (targetType == BytecodeScalarTypeCode.Integer && value.Type == TypeSymbol.Integer)
        {
            return value;
        }

        if (targetType == BytecodeScalarTypeCode.Long)
        {
            if (value.Type == TypeSymbol.Long)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromLong(value.AsInteger());
            }
        }

        if (targetType == BytecodeScalarTypeCode.Double)
        {
            if (value.Type == TypeSymbol.Double)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromDouble(value.AsInteger());
            }

            if (value.Type == TypeSymbol.Long)
            {
                return RuntimeValue.FromDouble(value.AsLong());
            }

            if (value.Type == TypeSymbol.Decimal)
            {
                return RuntimeValue.FromDouble((double)value.AsDecimal());
            }
        }

        if (targetType == BytecodeScalarTypeCode.Decimal)
        {
            if (value.Type == TypeSymbol.Decimal)
            {
                return value;
            }

            if (value.Type == TypeSymbol.Integer)
            {
                return RuntimeValue.FromDecimal(value.AsInteger());
            }

            if (value.Type == TypeSymbol.Long)
            {
                return RuntimeValue.FromDecimal(value.AsLong());
            }
        }

        return ReportInvalidRuntimeState<RuntimeValue>(
            $"РќРµРїРѕРґРґРµСЂР¶РёРІР°РµРјРѕРµ РїСЂРµРѕР±СЂР°Р·РѕРІР°РЅРёРµ VM: {value.Type.Name} -> {targetType}.",
            location);
    }

    private static bool IsZero(RuntimeValue value)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger() == 0,
            var type when type == TypeSymbol.Long => value.AsLong() == 0L,
            var type when type == TypeSymbol.Double => value.AsDouble() == 0d,
            var type when type == TypeSymbol.Decimal => value.AsDecimal() == 0m,
            _ => false
        };
    }

    private RuntimeValue EvaluateNot(RuntimeValue operand, TextLocation location)
    {
        return operand.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!operand.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(~operand.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(~operand.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Оператор NOT не поддерживает тип '{operand.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateAdd(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.String => RuntimeValue.FromString(left.AsString() + right.AsString()),
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() + right.AsInteger()),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() + right.AsLong()),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() + right.AsDouble(),
                "Результат операции '+'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() + right.AsDecimal()),
                "Результат операции '+'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '+' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateSubtract(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() - right.AsInteger()),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() - right.AsLong()),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() - right.AsDouble(),
                "Результат операции '-'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() - right.AsDecimal()),
                "Результат операции '-'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '-' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateMultiply(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(left.AsInteger() * right.AsInteger()),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(left.AsLong() * right.AsLong()),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                left.AsDouble() * right.AsDouble(),
                "Результат операции '*'",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => checked(left.AsDecimal() * right.AsDecimal()),
                "Результат операции '*'",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция '*' не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluatePower(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return EnsureFiniteDoubleResult(
            Math.Pow(left.AsDouble(), right.AsDouble()),
            "Результат операции '^'",
            location);
    }

    private RuntimeValue EvaluateDivide(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type == TypeSymbol.Decimal)
        {
            var divisorDecimal = right.AsDecimal();

            if (divisorDecimal == 0m)
            {
                ReportDivisionByZero(location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedDecimalOperation(
                () => left.AsDecimal() / divisorDecimal,
                "Результат операции '/'",
                location);
        }

        var divisor = right.AsDouble();
        if (divisor == 0d)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EnsureFiniteDoubleResult(left.AsDouble() / divisor, "Результат операции '/'", location);
    }

    private RuntimeValue EvaluateIntegralDivide(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type == TypeSymbol.Long)
        {
            var divisor = right.AsLong();
            if (divisor == 0L)
            {
                ReportDivisionByZero(location);
                throw new RuntimeFailureException();
            }

            return EvaluateCheckedLongOperation(
                () => checked(left.AsLong() / divisor),
                "Результат операции '\\'",
                location);
        }

        var integerDivisor = right.AsInteger();
        if (integerDivisor == 0)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(
            () => checked(left.AsInteger() / integerDivisor),
            "Результат операции '\\'",
            location);
    }

    private RuntimeValue EvaluateModulo(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateIntegerModulo(left.AsInteger(), right.AsInteger(), location),
            var type when type == TypeSymbol.Long => EvaluateLongModulo(left.AsLong(), right.AsLong(), location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Операция MOD не поддерживает тип '{left.Type.Name}'.",
                location)
        };
    }

    private RuntimeValue EvaluateIntegerModulo(int left, int right, TextLocation location)
    {
        if (right == 0)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedIntegerOperation(() => checked(left % right), "Результат операции 'MOD'", location);
    }

    private RuntimeValue EvaluateLongModulo(long left, long right, TextLocation location)
    {
        if (right == 0L)
        {
            ReportDivisionByZero(location);
            throw new RuntimeFailureException();
        }

        return EvaluateCheckedLongOperation(() => checked(left % right), "Результат операции 'MOD'", location);
    }

    private RuntimeValue EvaluateShiftLeft(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() << GetShiftCount(right)),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() << GetShiftCount(right)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция << не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateShiftRight(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() >> GetShiftCount(right)),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() >> GetShiftCount(right)),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция >> не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private static int GetShiftCount(RuntimeValue value)
    {
        return value.Type == TypeSymbol.Long
            ? unchecked((int)value.AsLong())
            : value.AsInteger();
    }

    private RuntimeValue EvaluateAnd(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() && right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() & right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() & right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция AND не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateOr(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() || right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() | right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() | right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция OR не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateXor(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() ^ right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(left.AsInteger() ^ right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(left.AsLong() ^ right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция XOR не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateEqv(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(left.AsBoolean() == right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger(~(left.AsInteger() ^ right.AsInteger())),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong(~(left.AsLong() ^ right.AsLong())),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция EQV не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateImp(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        return left.Type switch
        {
            var type when type == TypeSymbol.Boolean => RuntimeValue.FromBoolean(!left.AsBoolean() || right.AsBoolean()),
            var type when type == TypeSymbol.Integer => RuntimeValue.FromInteger((~left.AsInteger()) | right.AsInteger()),
            var type when type == TypeSymbol.Long => RuntimeValue.FromLong((~left.AsLong()) | right.AsLong()),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"Операция IMP не поддерживает тип '{left.Type.Name}'.", location)
        };
    }

    private static bool ValuesEqual(RuntimeValue left, RuntimeValue right)
    {
        if (left.Type == TypeSymbol.Integer)
        {
            return left.AsInteger() == right.AsInteger();
        }

        if (left.Type == TypeSymbol.Long)
        {
            return left.AsLong() == right.AsLong();
        }

        if (left.Type == TypeSymbol.Double)
        {
            return left.AsDouble() == right.AsDouble();
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            return left.AsDecimal() == right.AsDecimal();
        }

        if (left.Type == TypeSymbol.String)
        {
            return string.Equals(left.AsString(), right.AsString(), StringComparison.Ordinal);
        }

        if (left.Type == TypeSymbol.Boolean)
        {
            return left.AsBoolean() == right.AsBoolean();
        }

        return false;
    }

    private int Compare(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (left.Type == TypeSymbol.Integer)
        {
            return left.AsInteger().CompareTo(right.AsInteger());
        }

        if (left.Type == TypeSymbol.Long)
        {
            return left.AsLong().CompareTo(right.AsLong());
        }

        if (left.Type == TypeSymbol.Double)
        {
            return left.AsDouble().CompareTo(right.AsDouble());
        }

        if (left.Type == TypeSymbol.Decimal)
        {
            return left.AsDecimal().CompareTo(right.AsDecimal());
        }

        if (left.Type == TypeSymbol.String)
        {
            return string.Compare(left.AsString(), right.AsString(), StringComparison.Ordinal);
        }

        return ReportInvalidRuntimeState<int>(
            $"Сравнение не поддерживается для типа '{left.Type.Name}'.",
            location);
    }

    private RuntimeValue EvaluateCheckedIntegerOperation(Func<int> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromInteger(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Integer, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCheckedLongOperation(Func<long> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromLong(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Long, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCheckedDecimalOperation(Func<decimal> operation, string detail, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromDecimal(operation());
        }
        catch (OverflowException)
        {
            ReportNumericOverflow(detail, TypeSymbol.Decimal, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EnsureFiniteDoubleResult(double value, string detail, TextLocation location)
    {
        if (!double.IsFinite(value))
        {
            ReportNumericOverflow(detail, TypeSymbol.Double, location);
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromDouble(value);
    }

    private void ReportDivisionByZero(TextLocation location)
    {
        ReportRuntimeDiagnostic(DivisionByZeroCode, "Деление на ноль.", location);
    }

    private void ReportNumericOverflow(string detail, TypeSymbol type, TextLocation location)
    {
        ReportRuntimeDiagnostic(NumericOverflowCode, $"{detail} выходит за диапазон {type.Name}.", location);
    }

    private void ReportEmptyFunctionValue(TextLocation location)
    {
        ReportRuntimeDiagnostic(
            EmptyFunctionValueCode,
            "Значение функционального типа не содержит функции.",
            location);
    }

    private void ReportBuiltInArgumentError(string functionName, string argumentName, string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Функция {functionName}: аргумент '{argumentName}' {detail}",
            location);
    }

    private void ReportInvalidRuntimeState(string message, TextLocation? location = null)
    {
        ReportRuntimeDiagnostic(InvalidRuntimeStateCode, message, location ?? FallbackRuntimeLocation);
    }

    private T ReportInvalidRuntimeState<T>(string message, TextLocation? location = null)
    {
        ReportInvalidRuntimeState(message, location);
        throw new RuntimeFailureException();
    }

    private void ReportRuntimeDiagnostic(string code, string message, TextLocation location)
    {
        _diagnostics.Report(code, AppendCallChain(message), location);
    }

    private string AppendCallChain(string message)
    {
        if (_callStack.Count <= 1)
        {
            return message;
        }

        var builder = new StringBuilder(message);
        builder.AppendLine();
        builder.AppendLine("Цепочка вызовов:");

        foreach (var frame in _callStack)
        {
            builder.Append("- процедура '");
            builder.Append(frame.Routine.Name);
            builder.Append("', вызов в ");
            builder.Append(frame.CallSiteLocation.Line);
            builder.Append(':');
            builder.Append(frame.CallSiteLocation.Column);
            builder.AppendLine(".");
        }

        return builder.ToString().TrimEnd();
    }

    private readonly record struct DebugStepBoundary(DebugStepMode Mode, int StartLine, int StartCallDepth);

    private enum ComparisonBranchKind
    {
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    }

    private enum DebugStepMode
    {
        None,
        Into,
        Over,
        Out
    }
}
