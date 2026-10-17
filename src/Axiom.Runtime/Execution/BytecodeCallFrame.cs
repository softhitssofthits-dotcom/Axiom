using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;
using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

internal sealed class BytecodeCallFrame
{
    private IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot>? _visibleVariables;
    private Dictionary<int, RuntimeValue>? _cachedFunctionValuesByRoutineId;
    private Dictionary<int, BytecodeVariableSlot[]>? _cachedCapturedSlotsByRoutineId;
    private BytecodeVariableSlot[]? _byRefArgumentTargets;
    private readonly List<BytecodeErrorHandlerFrame> _errorHandlers = [];
    private readonly RuntimeValue[] _locals;
    private readonly int?[] _localFixedStringLengths;
    private BytecodeVariableSlot?[]? _sharedLocals;
    private bool _hasSharedLocals;

    public BytecodeCallFrame(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] arguments,
        RuntimeValue[] locals,
        int?[] localFixedStringLengths,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        BytecodeVariableSlot[]? byRefArgumentTargets = null,
        int evaluationStackCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(locals);
        ArgumentNullException.ThrowIfNull(localFixedStringLengths);
        ArgumentNullException.ThrowIfNull(captures);

        Routine = routine;
        Arguments = arguments;
        _locals = locals;
        _localFixedStringLengths = localFixedStringLengths;
        _sharedLocals = locals.Length == 0 ? null : new BytecodeVariableSlot?[locals.Length];
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        _byRefArgumentTargets = byRefArgumentTargets;
        EvaluationStack = new BytecodeEvaluationStack(evaluationStackCapacity);
    }

    public BytecodeRoutine Routine { get; }

    public BytecodeVariableSlot[] Arguments { get; }

    public BytecodeVariableSlot[] Captures { get; private set; }

    public TextLocation CallSiteLocation { get; private set; }

    public BytecodeVariableSlot[]? ByRefArgumentTargets => _byRefArgumentTargets;

    public BytecodeEvaluationStack EvaluationStack { get; }

    public int LocalCount => _locals.Length;

    public IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot> VisibleVariables =>
        _visibleVariables ??= CreateVisibleVariables(this);

    public int InstructionPointer { get; set; }

    public int ErrorHandlerCount => _errorHandlers.Count;

    public void Reset(
        BytecodeVariableSlot[] arguments,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        BytecodeVariableSlot[]? byRefArgumentTargets,
        RuntimeValue[] defaultLocals)
    {
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        _byRefArgumentTargets = byRefArgumentTargets;
        InstructionPointer = 0;
        _visibleVariables = null;
        _cachedFunctionValuesByRoutineId?.Clear();
        _cachedCapturedSlotsByRoutineId?.Clear();
        EvaluationStack.Clear();
        _errorHandlers.Clear();

        if (_locals.Length > 0)
        {
            Array.Copy(defaultLocals, _locals, _locals.Length);

            if (_hasSharedLocals && _sharedLocals is not null)
            {
                Array.Clear(_sharedLocals, 0, _sharedLocals.Length);
            }
        }

        _hasSharedLocals = false;

        for (var index = 0; index < Arguments.Length; index++)
        {
            Arguments[index] = arguments[index];
        }
    }

    public void PushErrorHandler(BytecodeErrorHandlerFrame handler)
    {
        _errorHandlers.Add(handler);
    }

    public BytecodeErrorHandlerFrame? PeekErrorHandler()
    {
        return _errorHandlers.Count == 0 ? null : _errorHandlers[^1];
    }

    public bool TryPopErrorHandler(out BytecodeErrorHandlerFrame handler)
    {
        if (_errorHandlers.Count == 0)
        {
            handler = null!;
            return false;
        }

        var index = _errorHandlers.Count - 1;
        handler = _errorHandlers[index];
        _errorHandlers.RemoveAt(index);
        return true;
    }

    public IReadOnlyList<BytecodeErrorHandlerFrame> ErrorHandlers => _errorHandlers;

    public bool TryResolveVisibleVariable(VariableSymbol variable, out BytecodeVariableSlot slot)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return ((Dictionary<VariableSymbol, BytecodeVariableSlot>)VisibleVariables).TryGetValue(variable, out slot!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue LoadLocalValue(int slot)
    {
        if (!_hasSharedLocals)
        {
            var local = _locals[slot];
            return local.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : local;
        }

        var sharedLocals = _sharedLocals!;
        var value = sharedLocals[slot]?.Value ?? _locals[slot];
        return value.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushLocalValueOntoEvaluationStack(int slot)
    {
        if (!_hasSharedLocals)
        {
            var local = _locals[slot];
            EvaluationStack.PushUnchecked(local.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : local);
            return;
        }

        var sharedLocals = _sharedLocals!;
        var value = sharedLocals[slot]?.Value ?? _locals[slot];
        EvaluationStack.PushUnchecked(value.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : value);
    }

    /// <summary>
    /// Возвращает текущее значение слота без создания записи. Нужен снимкам
    /// состояния: наблюдение не должно менять исполняемое состояние.
    /// </summary>
    public RuntimeValue PeekLocalValue(int slot)
    {
        if (!_hasSharedLocals)
        {
            return _locals[slot];
        }

        var sharedLocals = _sharedLocals!;
        return sharedLocals[slot]?.Value ?? _locals[slot];
    }

    /// <summary>
    /// Создаёт запись локальной переменной при первом чтении, если объявление
    /// ещё не исполнялось. Служебные слоты без объявленного типа остаются VOID.
    /// </summary>
    private RuntimeValue EnsureDefaultLocalValue(int slot)
    {
        var variables = Routine.LocalVariables;
        if (slot >= variables.Count || variables[slot].Type is not UserDefinedTypeSymbol recordType)
        {
            return RuntimeValue.Void;
        }

        var created = RuntimeValue.Default(recordType, _localFixedStringLengths[slot]);

        if (_hasSharedLocals && _sharedLocals![slot] is { } sharedSlot)
        {
            sharedSlot.Value = created;
        }
        else
        {
            _locals[slot] = created;
        }

        return created;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreLocalValue(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            var fixedStringLength = _localFixedStringLengths[slot];
            _locals[slot] = fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
                ? value
                : BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength);
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.Store(value);
            return;
        }

        var localFixedStringLength = _localFixedStringLengths[slot];
        _locals[slot] = localFixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
            ? value
            : BytecodeVariableSlot.NormalizeForStorage(value, localFixedStringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreAppendStringLocalValue(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            var fixedStringLength = _localFixedStringLengths[slot];
            _locals[slot] = fixedStringLength is null
                ? value
                : BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength);
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.StoreAppendStringValue(value);
            return;
        }

        var localFixedStringLength = _localFixedStringLengths[slot];
        _locals[slot] = localFixedStringLength is null
            ? value
            : BytecodeVariableSlot.NormalizeForStorage(value, localFixedStringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LoadForLoopLocalValues(int currentSlot, int endSlot, int stepSlot, out RuntimeValue current, out RuntimeValue end, out RuntimeValue step)
    {
        if (!_hasSharedLocals)
        {
            var locals = _locals;
            current = locals[currentSlot];
            end = locals[endSlot];
            step = locals[stepSlot];
            return;
        }

        var sharedLocals = _sharedLocals!;
        current = sharedLocals[currentSlot]?.Value ?? _locals[currentSlot];
        end = sharedLocals[endSlot]?.Value ?? _locals[endSlot];
        step = sharedLocals[stepSlot]?.Value ?? _locals[stepSlot];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LoadForLoopAdvanceValues(int currentSlot, int stepSlot, out RuntimeValue current, out RuntimeValue step)
    {
        if (!_hasSharedLocals)
        {
            var locals = _locals;
            current = locals[currentSlot];
            step = locals[stepSlot];
            return;
        }

        var sharedLocals = _sharedLocals!;
        current = sharedLocals[currentSlot]?.Value ?? _locals[currentSlot];
        step = sharedLocals[stepSlot]?.Value ?? _locals[stepSlot];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreNumericLocalValueUnchecked(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            _locals[slot] = value;
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.Value = value;
            return;
        }

        _locals[slot] = value;
    }

    public BytecodeVariableSlot GetOrCreateLocalSlot(int slot)
    {
        _sharedLocals ??= new BytecodeVariableSlot?[_locals.Length];

        var sharedSlot = _sharedLocals[slot];
        if (sharedSlot is not null)
        {
            return sharedSlot;
        }

        sharedSlot = BytecodeVariableSlot.Adopt(_locals[slot], _localFixedStringLengths[slot]);
        _sharedLocals[slot] = sharedSlot;
        _hasSharedLocals = true;
        return sharedSlot;
    }

    public int? GetLocalFixedStringLength(int slot)
    {
        return _localFixedStringLengths[slot];
    }

    public bool TryGetCachedFunctionValue(int routineId, out RuntimeValue value)
    {
        if (_cachedFunctionValuesByRoutineId is not null
            && _cachedFunctionValuesByRoutineId.TryGetValue(routineId, out value!))
        {
            return true;
        }

        value = RuntimeValue.Void;
        return false;
    }

    public void CacheFunctionValue(int routineId, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        (_cachedFunctionValuesByRoutineId ??= []).TryAdd(routineId, value);
    }

    public bool TryGetCachedCapturedSlots(int routineId, out BytecodeVariableSlot[] captures)
    {
        if (_cachedCapturedSlotsByRoutineId is not null
            && _cachedCapturedSlotsByRoutineId.TryGetValue(routineId, out captures!))
        {
            return true;
        }

        captures = Array.Empty<BytecodeVariableSlot>();
        return false;
    }

    public void CacheCapturedSlots(int routineId, BytecodeVariableSlot[] captures)
    {
        ArgumentNullException.ThrowIfNull(captures);
        (_cachedCapturedSlotsByRoutineId ??= []).TryAdd(routineId, captures);
    }

    private static Dictionary<VariableSymbol, BytecodeVariableSlot> CreateVisibleVariables(BytecodeCallFrame frame)
    {
        var visibleVariables = new Dictionary<VariableSymbol, BytecodeVariableSlot>();
        var routine = frame.Routine;
        var arguments = frame.Arguments;
        var captures = frame.Captures;

        for (var index = 0; index < routine.ParameterVariables.Count && index < arguments.Length; index++)
        {
            visibleVariables[routine.ParameterVariables[index]] = arguments[index];
        }

        for (var index = 0; index < routine.LocalVariables.Count && index < frame.LocalCount; index++)
        {
            visibleVariables[routine.LocalVariables[index]] = frame.GetOrCreateLocalSlot(index);
        }

        for (var index = 0; index < routine.CapturedVariables.Count && index < captures.Length; index++)
        {
            visibleVariables[routine.CapturedVariables[index]] = captures[index];
        }

        return visibleVariables;
    }
}
